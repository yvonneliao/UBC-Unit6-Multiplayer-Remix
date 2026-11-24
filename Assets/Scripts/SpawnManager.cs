using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float _maxSpawnRadius = 9.5f;
    [SerializeField] float _minSpawnRadius = 6f;
    [SerializeField] float _spawnY = -0.27f;

    [Header("Clearance Check")]
    [SerializeField] private float _clearRadius = 2.5f;
    [SerializeField] private float _checkCenterOffsetY = 0.8f;
    [SerializeField] private float _angleJitter = 30f;
    [SerializeField] private int _maxSpawnAttempts = 25;

    private readonly List<int> _availableIndices = new List<int>();

    private void Awake()
    {
        Instance = this;
        _arrangeSpawnPoints();
        _refillIndices();
    }

    private void _arrangeSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        float _baseAngle = Random.Range(0f, 360f);
        float _step = 360f / spawnPoints.Length;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            float _sectorAngle = _baseAngle + _step * i;
            spawnPoints[i].position = _findClearPosition(_sectorAngle);
        }
    }

    private Vector3 _findClearPosition(float _baseAngleDeg)
    {
        Vector3 _fallback = Vector3.zero;

        for (int _attempt = 0; _attempt < _maxSpawnAttempts; _attempt++)
        {
            float _jitter = _attempt == 0 ? 0f : Random.Range(-_angleJitter, _angleJitter);
            float _angleRad = (_baseAngleDeg + _jitter) * Mathf.Deg2Rad;
            float _radius = Random.Range(_minSpawnRadius, _maxSpawnRadius);

            Vector3 _candidate = new Vector3(
                Mathf.Cos(_angleRad) * _radius,
                _spawnY,
                Mathf.Sin(_angleRad) * _radius);

            if (_attempt == 0)
                _fallback = _candidate;

            if (_isPositionClear(_candidate))
                return _candidate;
        }

        return _fallback;
    }

    private bool _isPositionClear(Vector3 _pos)
    {
        Vector3 _center = _pos + Vector3.up * _checkCenterOffsetY;
        Collider[] _hits = Physics.OverlapSphere(_center, _clearRadius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider _col in _hits)
        {
            if (_col == null)
                continue;

            if (_col.CompareTag("Reward") || _col.CompareTag("Obstacle"))
                return false;

            if (_col.GetComponentInParent<myScoreValue>() != null
                || _col.GetComponentInParent<myRespawnableReward>() != null
                || _col.GetComponentInParent<myQuestionTransform>() != null
                || _col.GetComponentInParent<mySingleUseReward>() != null
                || _col.GetComponentInParent<NetworkStats>() != null)
            {
                return false;
            }
        }

        return true;
    }

    private void _refillIndices()
    {
        _availableIndices.Clear();
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            _availableIndices.Add(i);
        }
    }

    public void GetSpawn(out Vector3 position, out Quaternion rotation)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }

        if (_availableIndices.Count == 0)
        {
            _refillIndices();
        }

        int _pick = Random.Range(0, _availableIndices.Count);
        int _spawnIndex = _availableIndices[_pick];
        _availableIndices.RemoveAt(_pick);

        Transform selected = spawnPoints[_spawnIndex];
        position = selected.position;

        if (!_isPositionClear(position))
        {
            float _angle = Mathf.Atan2(position.z, position.x) * Mathf.Rad2Deg;
            position = _findClearPosition(_angle);
        }

        rotation = selected.rotation;
    }
}
