# Intro to NetCode for GameObjects

> The following tutorial was created using Unity 2022.3.0 and NetCode for GameObjects 1.4.0

## Setup

1. Open Package Manager
1. Install [**NetCode for GameObjects**](https://docs-multiplayer.unity3d.com/netcode/current/about/)
1. Setup test scene
    1. Place 3d plane at `0, 0, 0`
    1. Change camera position to `0, 10, -10` with a rotation of `45, 0, 0`
1. Create new GameObject with the name `NetworkManager`

    1. Add the **NetworkManager** component
    1. Click **Select transport...** in the new **NetworkManager** component and select UnityTransport
    1. Add a new script called `NetworkManagerEventButtons` to the `NetworkManager` GameObject with the following code:

        ```csharp
        using Unity.Netcode;
        using UnityEngine;

        public class NetworkManagerEventButtons : MonoBehaviour
        {

            private void OnGUI()
            {
                if (GUI.Button(new Rect(10, 10, 100, 30), "Start Host"))
                {
                    NetworkManager.Singleton.StartHost();
                }

                if (GUI.Button(new Rect(10, 50, 100, 30), "Start Client"))
                {
                    NetworkManager.Singleton.StartClient();
                }
            }

        }
        ```

> ⚠️ **Note:** If you want to test this over your local network, you must first get the local IP of the machine you are running the host on (ex: `192.168.x.x`) and put it into the **Address** property of the **NetworkTransport** component found on the `NetworkManager` GameObject. You must also enable **Allow Remote Connections?**.

## Basic Network Movement

1. Create a 3d cube with the name `Player`

    1. Add a **NetworkObject** component
    1. Add a new script called `ClientNetworkTransform` to the `Player` prefab with the following code:

        ```csharp
        using Unity.Netcode.Components;

        public class ClientNetworkTransform : NetworkTransform
        {

            protected override bool OnIsServerAuthoritative()
            {
                return false;
            }

        }
        ```

    1. In the new `ClientNetworkTransform` component, toggle off all checkboxes in **Syncing** for **Rotation** and **Scale**.
    1. Add a new script called `PlayerController` to the `Player` GameObject with the following code:

        ```csharp
        using Unity.Netcode;
        using UnityEngine;

        public class PlayerController : NetworkBehaviour
        {

            private const int speed = 10;

            public override void OnNetworkSpawn()
            {
                if (!IsOwner) return;

                gameObject.transform.position = new Vector3(Random.Range(5, -5), 0.5f, Random.Range(5, -5));
            }

            private void Update()
            {
                if (!IsOwner) return;

                var movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

                gameObject.transform.position += movement * speed * Time.deltaTime;
            }

        }
        ```

    1. Drag the `Player` GameObject into the **Assets** panel, creating a Prefab
    1. Delete the `Player` GameObject from the scene

1. Click on the `NetworkManager` GameObject
    1. Drag the `Player` Prefab into the **Player Prefab** property
1. Click on the `DefaultNetworkPrefabs` asset in the **Assets** panel and confirm the `Player` prefab is in the list.
1. Open **Project Settings**
    1. Navigate to **Player** > **Resolutions and Presentation**
    1. Change **Fullscreen Mode** to **Windowed**
1. **Build and Run** the app
1. When the app starts up, press **Start Host**
1. Back in the Unity Editor, press play and then **Start Client**

## Syncing Variables

1. Remove the `ClientNetworkTransform` from the `Player` prefab.
1. Add a new script called `PlayerNetworkTransform` to the `Player` prefab with the following code:

    ```csharp
    using Unity.Netcode;
    using UnityEngine;

    public class PlayerNetworkTransform : NetworkBehaviour
    {

        private readonly NetworkVariable<Vector3> _networkPosition = new(writePerm : NetworkVariableWritePermission.Owner);

        private void Update()
        {
            if (IsOwner)
            {
                _networkPosition.Value = gameObject.transform.position;
            }
            else
            {
                gameObject.transform.position = _networkPosition.Value;
            }
        }

    }
    ```

> ⚠️ **Note:** The movement will appear janky now that we are modifying the position directly. The previous component, `ClientNetworkTransform` extends from the `NetworkTransform` component which smooths out the movement. You can add you own position smoothing here.

## Syncing Custom Serializable Structs

1. Open the `Player` prefab and add a **UI** > **Text - TextMeshPro** component as a child.
    1. Change the `Canvas` GameObject **Render Mode** to **World Space**
    1. Change the **Position** of the `Canvas` GameObject to `0`, `1.25`, `0` for the `x`, `y`, and `z` respectively.
    1. Change the **Width** and **Height** of the `Canvas` GameObject to `200` and `50` respectively.
    1. Change the **Scale** of the `Canvas` GameObject to `0.025` for the `x`, `y`, and `z`.
    1. Align the text center vertically and horizontally.
    1. Chang the color of the text to a dark color.
1. Add a new script called `PlayerNetworkInformation` to the `Player` prefab with the following code:

    ```csharp
    using TMPro;
    using Unity.Collections;
    using Unity.Netcode;
    using UnityEngine;

    public class PlayerNetworkInformation : NetworkBehaviour
    {

        [SerializeField]
        private TextMeshProUGUI _playerNameTextMeshProUGUI;

        private readonly NetworkVariable<PlayerInformation> _playerInformation =
            new(writePerm : NetworkVariableWritePermission.Owner);

        private void Start()
        {
            if (!IsOwner) return;

            _playerInformation.Value = new PlayerInformation { name = $"Player {OwnerClientId}" };
        }

        private void Update()
        {
            _playerNameTextMeshProUGUI.text = _playerInformation.Value.name.ToString();
        }

        internal struct PlayerInformation : INetworkSerializable
        {

            internal FixedString32Bytes name;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref name);
            }

        }

    }
    ```

    > ⚠️ **Note:** We are using **FixedString32Bytes** here instead of **string** as a **string** throws an [error](https://gist.github.com/neogeek/b13a7a04c21c1ca254c9b7d9ba642426) when used with **INetworkSerializable**.

1. Assign the **Player Name Text Mesh Pro UGUI** to the text GameObject in the `Player` prefab.

## Writing to Variables on the Server

1. Replace the contents of the `PlayerNetworkInformation` class with the following code:

    ```csharp
    using TMPro;
    using Unity.Collections;
    using Unity.Netcode;
    using UnityEngine;

    public class PlayerNetworkInformation : NetworkBehaviour
    {

        [SerializeField]
        private TextMeshProUGUI _playerNameTextMeshProUGUI;

        private readonly NetworkVariable<PlayerInformation> _playerInformation =
            new(writePerm : NetworkVariableWritePermission.Server);

        private void Start()
        {
            if (!IsOwner) return;

            var playerInformation = new PlayerInformation { name = $"Player {OwnerClientId}" };

            if (IsHost || IsServer)
            {
                _playerInformation.Value = playerInformation;
            }
            else
            {
                StorePlayerInformationServerRpc(playerInformation);
            }
        }

        [ServerRpc]
        private void StorePlayerInformationServerRpc(PlayerInformation playerInformation)
        {
            _playerInformation.Value = playerInformation;
        }

        private void Update()
        {
            _playerNameTextMeshProUGUI.text = _playerInformation.Value.name.ToString();
        }

        internal struct PlayerInformation : INetworkSerializable
        {

            internal FixedString32Bytes name;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref name);
            }

        }

    }
    ```

## Spawning Objects

1. Add a new script called `ObjectSpawner` to the `Player` prefab with the following code:

    ```csharp
    using Unity.Netcode;
    using UnityEngine;

    public class ObjectSpawner : NetworkBehaviour
    {

        [SerializeField]
        private GameObject _prefab;

        private Plane _groundPlane;

        private Camera _mainCamera;

        private void Start()
        {
            _groundPlane = new Plane(Vector3.up, Vector3.zero);

            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsOwner || !Input.GetMouseButton(0)) return;

            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!_groundPlane.Raycast(ray, out var distance)) return;

            var position = ray.GetPoint(distance);

            if (IsHost || IsServer)
            {
                var spawnedGameObject = Instantiate(_prefab, position, Quaternion.identity);

                spawnedGameObject.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                SpawnPrefabAtPositionServerRpc(position, Quaternion.identity);
            }
        }

        [ServerRpc]
        private void SpawnPrefabAtPositionServerRpc(Vector3 position, Quaternion direction)
        {
            var spawnedGameObject = Instantiate(_prefab, position, direction);

            spawnedGameObject.GetComponent<NetworkObject>().Spawn();
        }

    }
    ```

1. Create 3d sphere with the name `Sphere`

    1. Add a **NetworkObject** component
    1. Add a new script called `SphereController` to the sphere with the following code:

        ```csharp
        using Unity.Netcode;
        using UnityEngine;

        public class SphereController : NetworkBehaviour
        {

            private float _startTime;

            private void OnEnable()
            {
                _startTime = Time.time;
            }

            private void Update()
            {
                if (!IsOwner) return;

                if (Time.time < _startTime + 1f) return;

                if (IsHost || IsServer)
                {
                    gameObject.GetComponent<NetworkObject>().Despawn();

                    Destroy(gameObject);
                }
                else
                {
                    DestroyServerRpc();
                }
            }

            [ServerRpc]
            public void DestroyServerRpc()
            {
                gameObject.GetComponent<NetworkObject>().Despawn();

                Destroy(gameObject);
            }

        }
        ```

    1. Drag the `Sphere` GameObject into the **Assets** panel, creating a Prefab
    1. Delete the `Sphere` GameObject from the scene

1. Click on the `Player` prefab

    1. Drag the `Sphere` Prefab into the **Prefab** property of the `ObjectSpawner` component

1. Click on the `DefaultNetworkPrefabs` asset in the **Assets** panel and confirm the `Sphere` prefab is in the list.

## NetworkObject Pooling

1. Create an empty GameObject with the name `NetworkObjectPool`
1. Add a **NetworkObject** component
1. Add a new script called `NetworkObjectPoolController` to the empty GameObject with the following code:

    ```csharp
    using System;
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.Pool;

    public class NetworkObjectPoolController : NetworkBehaviour
    {

        [Serializable]
        public struct PooledPrefab
        {

            public GameObject prefab;

            public int count;

        }

        [SerializeField]
        private List<PooledPrefab> _pooledPrefabs;

        private readonly Dictionary<GameObject, ObjectPool<NetworkObject>> _pooledObjects = new();

        private void PopulatePool(PooledPrefab pooledPrefab)
        {
            _pooledObjects.Add(pooledPrefab.prefab,
                new ObjectPool<NetworkObject>(() => Instantiate(pooledPrefab.prefab).GetComponent<NetworkObject>(),
                    networkObject => networkObject.gameObject.SetActive(true),
                    networkObject => networkObject.gameObject.SetActive(false),
                    networkObject => Destroy(networkObject.gameObject),
                    defaultCapacity : pooledPrefab.count));

            var prewarmObjects = new HashSet<NetworkObject>();

            for (var i = 0; i < pooledPrefab.count; i++)
            {
                prewarmObjects.Add(_pooledObjects[pooledPrefab.prefab].Get());
            }

            foreach (var prewarmObject in prewarmObjects)
            {
                _pooledObjects[pooledPrefab.prefab].Release(prewarmObject);
            }

            NetworkManager.Singleton.PrefabHandler.AddHandler(pooledPrefab.prefab,
                new PooledPrefabInstanceHandler(pooledPrefab.prefab, this));

        }

        public override void OnNetworkSpawn()
        {
            foreach (var pooledPrefab in _pooledPrefabs)
            {
                PopulatePool(pooledPrefab);
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (var pooledPrefab in _pooledPrefabs)
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(pooledPrefab.prefab);

                _pooledObjects[pooledPrefab.prefab].Clear();

            }

            _pooledObjects.Clear();
        }

        public NetworkObject Retrieve(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pooledObjects.ContainsKey(prefab)) return null;

            var spawnedGameObject = _pooledObjects[prefab].Get();

            spawnedGameObject.transform.position = position;
            spawnedGameObject.transform.rotation = rotation;

            return spawnedGameObject.GetComponent<NetworkObject>();
        }

        public void Release(GameObject prefab, NetworkObject networkObject)
        {
            _pooledObjects[prefab].Release(networkObject);
        }

        private sealed class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
        {

            private readonly GameObject _prefab;

            private readonly NetworkObjectPoolController _pool;

            public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPoolController pool)
            {
                _prefab = prefab;
                _pool = pool;
            }

            NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position,
                Quaternion rotation)
            {
                return _pool.Retrieve(_prefab, position, rotation);
            }

            void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
            {
                _pool.Release(_prefab, networkObject);
            }

        }

    }
    ```

1. **(Optional)** If you would like to show debug information for the pooled objects, add the following to the `NetworkObjectPoolController` class.

    ```csharp
    #if UNITY_EDITOR
        private void OnGUI()
        {

            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 0, 100, 0) });

            GUILayout.Label($"Current FPS: {Math.Round(Time.frameCount / Time.time, 2)}");

            foreach (var pooledPrefab in _pooledPrefabs)
            {

                GUILayout.Label($"Pooled Prefab: {pooledPrefab.prefab.name}");

                if (_pooledObjects.ContainsKey(pooledPrefab.prefab))
                {
                    GUILayout.Label($"↳ Total: {_pooledObjects[pooledPrefab.prefab].CountAll}" +
                                    $" Active: {_pooledObjects[pooledPrefab.prefab].CountActive}" +
                                    $" Inactive: {_pooledObjects[pooledPrefab.prefab].CountInactive}");
                }
            }

            GUILayout.EndVertical();

        }
    #endif
    ```

1. Add a new item to the **Pooled Prefabs** property of the `NetworkObjectPoolController` component
    1. Add the `Sphere` prefab
    1. Change the count to 1000
1. Edit the `ObjectSpawner` script, changing the contents of the file to the following:

    ```csharp
    using Unity.Netcode;
    using UnityEngine;

    public class ObjectSpawner : NetworkBehaviour
    {

        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private NetworkObjectPoolController _networkObjectPoolController;

        private Plane _groundPlane;

        private Camera _mainCamera;

        private void Awake()
        {
            _networkObjectPoolController = FindObjectOfType<NetworkObjectPoolController>();
        }

        private void Start()
        {
            _groundPlane = new Plane(Vector3.up, Vector3.zero);

            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsOwner || !Input.GetMouseButton(0)) return;

            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!_groundPlane.Raycast(ray, out var distance)) return;

            var position = ray.GetPoint(distance);

            if (IsHost || IsServer)
            {
                var networkObject = _networkObjectPoolController.Retrieve(_prefab, position, Quaternion.identity);

                networkObject.Spawn();
            }
            else
            {
                SpawnPrefabAtPositionServerRpc(position, Quaternion.identity);
            }
        }

        [ServerRpc]
        private void SpawnPrefabAtPositionServerRpc(Vector3 position, Quaternion direction)
        {
            var networkObject = _networkObjectPoolController.Retrieve(_prefab, position, direction);

            networkObject.Spawn();
        }

    }
    ```

1. Edit the `SphereController` script, changing the contents of the file to the following:

    ```csharp
    using Unity.Netcode;
    using UnityEngine;

    public class SphereController : NetworkBehaviour
    {

        private float _startTime;

        private void OnEnable()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Time.time < _startTime + 1f) return;

            if (IsHost || IsServer)
            {
                gameObject.GetComponent<NetworkObject>().Despawn();
            }
            else
            {
                DestroyServerRpc();
            }
        }

        [ServerRpc]
        public void DestroyServerRpc()
        {
            gameObject.GetComponent<NetworkObject>().Despawn();
        }

    }
    ```
