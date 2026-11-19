using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] Transform[] spawnPoints;
    private readonly List<int> _availableIndices = new List<int>();

    private void Awake()
    {
        Instance = this;
        _refillIndices();
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
