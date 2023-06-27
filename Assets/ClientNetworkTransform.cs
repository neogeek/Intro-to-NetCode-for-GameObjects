using Unity.Netcode.Components;

public class ClientNetworkTransform : NetworkTransform
{

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

}
