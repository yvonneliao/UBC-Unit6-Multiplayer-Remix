using Unity.Netcode.Components;
using UnityEngine;

public class OwnerNetworkTransform : NetworkTransform
{
    public bool ToggleServerAuthoritative = false;

    protected override bool OnIsServerAuthoritative()
    {
        return ToggleServerAuthoritative;
    }
}
