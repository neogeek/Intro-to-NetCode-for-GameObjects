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
