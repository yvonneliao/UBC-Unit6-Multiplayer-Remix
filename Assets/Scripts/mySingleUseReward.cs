using Unity.Netcode;
using UnityEngine;

public class mySingleUseReward : NetworkBehaviour
{
    [SerializeField] private float _pickupDelay = 0.5f;

    private bool _collected;
    private float _spawnTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _spawnTime = Time.time;
            _collected = false;
        }
    }

    public bool _tryCollect()
    {
        if (!IsServer || !IsSpawned || _collected)
            return false;

        if (Time.time < _spawnTime + _pickupDelay)
            return false;

        _collected = true;
        NetworkObject.Despawn(true);
        return true;
    }
}
