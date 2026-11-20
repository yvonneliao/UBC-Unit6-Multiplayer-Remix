using Unity.Netcode;
using UnityEngine;

public class myFoodPickup : NetworkBehaviour
{
    [SerializeField] private int _scoreValue = 20;
    [SerializeField] private float _pickupDelay = 0.4f;
    [SerializeField] private float _lifetime = 25f;

    private ulong _dropperClientId;
    private bool _hasDropper;
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

    public void _setDropper(ulong _clientId)
    {
        _dropperClientId = _clientId;
        _hasDropper = true;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || _collected)
            return;

        if (Time.time >= _spawnTime + _lifetime)
        {
            NetworkObject.Despawn();
        }
    }

    private void OnTriggerEnter(Collider _other)
    {
        _tryPickup(_other);
    }

    private void OnCollisionEnter(Collision _collision)
    {
        _tryPickup(_collision.collider);
    }

    private void _tryPickup(Collider _other)
    {
        if (!IsServer || !IsSpawned || _collected)
            return;

        if (Time.time < _spawnTime + _pickupDelay)
            return;

        NetworkStats _stats = _other.GetComponentInParent<NetworkStats>();
        if (_stats == null || !_stats.IsAlive.Value)
            return;

        if (_hasDropper && _stats.OwnerClientId == _dropperClientId)
            return;

        _collected = true;
        _stats.AddScore(_scoreValue);
        NetworkObject.Despawn();
    }
}
