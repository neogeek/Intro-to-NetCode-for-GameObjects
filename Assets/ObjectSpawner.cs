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
