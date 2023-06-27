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
