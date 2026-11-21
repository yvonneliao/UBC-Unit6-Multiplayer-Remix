using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class myCharacterSelector : NetworkBehaviour
{
    [SerializeField] private Transform _turretPivot;
    [SerializeField] private GameObject[] _characterPrefabs;

    private readonly NetworkVariable<int> _characterIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private static readonly List<int> _availableIndices = new List<int>();
    private GameObject _spawnedCharacter;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void _resetPool()
    {
        _availableIndices.Clear();
    }

    public override void OnNetworkSpawn()
    {
        _characterIndex.OnValueChanged += _onCharacterIndexChanged;

        if (IsServer)
        {
            _characterIndex.Value = _pickUniqueIndex();
        }

        if (_characterIndex.Value >= 0)
        {
            _spawnCharacter(_characterIndex.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        _characterIndex.OnValueChanged -= _onCharacterIndexChanged;

        if (IsServer && _characterIndex.Value >= 0 && !_availableIndices.Contains(_characterIndex.Value))
        {
            _availableIndices.Add(_characterIndex.Value);
        }
    }

    private int _pickUniqueIndex()
    {
        if (_characterPrefabs == null || _characterPrefabs.Length == 0)
            return -1;

        if (_availableIndices.Count == 0)
        {
            for (int i = 0; i < _characterPrefabs.Length; i++)
            {
                _availableIndices.Add(i);
            }
        }

        int _pick = Random.Range(0, _availableIndices.Count);
        int _index = _availableIndices[_pick];
        _availableIndices.RemoveAt(_pick);
        return _index;
    }

    private void _onCharacterIndexChanged(int _oldValue, int _newValue)
    {
        if (_newValue >= 0)
        {
            _spawnCharacter(_newValue);
        }
    }

    private void _spawnCharacter(int _index)
    {
        if (_spawnedCharacter != null || _turretPivot == null)
            return;

        if (_characterPrefabs == null || _index < 0 || _index >= _characterPrefabs.Length)
            return;

        GameObject _prefab = _characterPrefabs[_index];
        if (_prefab == null)
            return;

        _spawnedCharacter = Instantiate(_prefab, _turretPivot);
        _spawnedCharacter.transform.localPosition = Vector3.zero;
        _spawnedCharacter.transform.localRotation = Quaternion.identity;
    }
}
