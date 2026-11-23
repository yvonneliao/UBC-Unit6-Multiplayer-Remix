using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class myTankCollisionScore : NetworkBehaviour
{
    [SerializeField] private float _hitCooldown = 1f;
    [SerializeField] private int _defaultRewardPoints = 10;
    [SerializeField] private int _defaultObstaclePenalty = -10;

    private NetworkStats _stats;
    private readonly Dictionary<int, float> _cooldowns = new Dictionary<int, float>();

    public override void OnNetworkSpawn()
    {
        _stats = GetComponent<NetworkStats>();
    }

    private void OnTriggerEnter(Collider _other)
    {
        _handleHit(_other);
    }

    private void OnCollisionEnter(Collision _collision)
    {
        _handleHit(_collision.collider);
    }

    private void _handleHit(Collider _other)
    {
        if (!IsServer || !IsSpawned || _other == null)
            return;

        if (_stats == null)
            _stats = GetComponent<NetworkStats>();

        if (_stats == null || !_stats.IsAlive.Value)
            return;

        NetworkStats _otherStats = _other.GetComponentInParent<NetworkStats>();
        if (_otherStats != null)
        {
            if (_otherStats != _stats && _other.CompareTag("Reward") && _otherStats._tryCollectPumpkin())
            {
                _stats._modifyScoreWithPopup(_otherStats._getPumpkinRewardPoints(), _getTopSurface(_other));
            }
            return;
        }

        if (!_tryGetHitData(_other, out int _points, out int _targetId, out Vector3 _popupPos, out bool _moveDownward))
            return;

        if (_cooldowns.TryGetValue(_targetId, out float _readyTime) && Time.time < _readyTime)
            return;

        _cooldowns[_targetId] = Time.time + _hitCooldown;
        _stats._modifyScoreWithPopup(_points, _popupPos, _moveDownward);
    }

    private bool _tryGetHitData(Collider _other, out int _points, out int _targetId, out Vector3 _popupPos, out bool _moveDownward)
    {
        myScoreValue _scoreObj = _other.GetComponent<myScoreValue>();
        if (_scoreObj == null)
            _scoreObj = _other.GetComponentInParent<myScoreValue>();

        if (_scoreObj != null)
        {
            _points = _scoreObj._getPoints();
            _targetId = _scoreObj.gameObject.GetInstanceID();
            _scoreObj._getPopupData(_other, out _popupPos, out _moveDownward);
            return true;
        }

        if (_other.CompareTag("Reward"))
        {
            _points = _defaultRewardPoints;
            _targetId = _other.gameObject.GetInstanceID();
            _popupPos = _getTopSurface(_other);
            _moveDownward = false;
            return true;
        }

        if (_other.CompareTag("Obstacle"))
        {
            _points = _defaultObstaclePenalty;
            _targetId = _other.gameObject.GetInstanceID();
            _popupPos = _getTopSurface(_other);
            _moveDownward = false;
            return true;
        }

        _points = 0;
        _targetId = 0;
        _popupPos = Vector3.zero;
        _moveDownward = false;
        return false;
    }

    private Vector3 _getTopSurface(Collider _col)
    {
        Bounds _bounds = _col.bounds;
        return new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z);
    }
}
