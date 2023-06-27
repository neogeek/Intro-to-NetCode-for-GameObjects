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
