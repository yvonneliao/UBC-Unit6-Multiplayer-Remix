using Unity.Netcode;
using UnityEngine;

public class myFoodPickup : NetworkBehaviour
{
    [SerializeField] private int _scoreValue = 200;
    [SerializeField] private float _pickupDelay = 0.4f;
    [SerializeField] private float _lifetime = 25f;

    [Header("Audio")]
    [SerializeField] private AudioClip _collectSfx;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

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
        _stats._modifyScoreWithPopup(_scoreValue, _getTopSurface());
        _playCollectSfxRpc(transform.position);
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.Everyone)]
    private void _playCollectSfxRpc(Vector3 _position)
    {
        if (_collectSfx != null)
        {
            GameObject _sfxObj = new GameObject("FoodPickupSfx");
            AudioSource _source = _sfxObj.AddComponent<AudioSource>();
            _source.clip = _collectSfx;
            _source.volume = _sfxVolume;
            _source.spatialBlend = 0f;
            _source.Play();
            Destroy(_sfxObj, _collectSfx.length);
        }
    }

    private Vector3 _getTopSurface()
    {
        Collider _col = GetComponentInChildren<Collider>();
        if (_col == null)
            return transform.position;

        Bounds _bounds = _col.bounds;
        return new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z);
    }
}
