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
