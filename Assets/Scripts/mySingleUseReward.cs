using Unity.Netcode;

public class mySingleUseReward : NetworkBehaviour
{
    private bool _collected;

    public bool _tryCollect()
    {
        if (!IsServer || !IsSpawned || _collected)
            return false;

        _collected = true;
        NetworkObject.Despawn(true);
        return true;
    }
}
