using System;
using Unity.Netcode;
using UnityEngine;

public class mySingleUseReward : NetworkBehaviour
{
    [SerializeField] private float _pickupDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip _collectSfx;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

    private bool _collected;
    private float _spawnTime;
    private Action _onCollected;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _spawnTime = Time.time;
            _collected = false;
        }
    }

    public void _setOnCollected(Action _callback)
    {
        _onCollected = _callback;
    }

    public bool _tryCollect()
    {
        if (!IsServer || !IsSpawned || _collected)
            return false;

        if (Time.time < _spawnTime + _pickupDelay)
            return false;

        _collected = true;
        _onCollected?.Invoke();
        _playCollectSfxRpc(transform.position);
        NetworkObject.Despawn(true);
        return true;
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
}
