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
