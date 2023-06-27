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
