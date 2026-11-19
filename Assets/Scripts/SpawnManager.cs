using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float _maxSpawnRadius = 9.5f;
    [SerializeField] float _minSpawnRadius = 6f;
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

            float _angleRad = (_baseAngle + _step * i) * Mathf.Deg2Rad;
            float _radius = Random.Range(_minSpawnRadius, _maxSpawnRadius);
            float _y = spawnPoints[i].position.y;

            spawnPoints[i].position = new Vector3(
                Mathf.Cos(_angleRad) * _radius,
                _y,
                Mathf.Sin(_angleRad) * _radius
            );
        }
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
        rotation = selected.rotation;
    }

}
