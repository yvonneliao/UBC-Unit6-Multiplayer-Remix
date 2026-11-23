using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class myRespawnableReward : NetworkBehaviour
{
    [SerializeField] private float _respawnDelay = 5f;
    [SerializeField] private float _spinSpeed = 90f;

    private readonly NetworkVariable<bool> _isAvailable = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    public override void OnNetworkSpawn()
    {
        _isAvailable.OnValueChanged += _onAvailabilityChanged;
        _applyAvailability(_isAvailable.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isAvailable.OnValueChanged -= _onAvailabilityChanged;
    }

    private void Update()
    {
        if (!IsSpawned || !_isAvailable.Value)
            return;

        transform.Rotate(Vector3.up, -_spinSpeed * Time.deltaTime, Space.World);
    }

    public bool _tryCollect()
    {
        if (!IsServer || !IsSpawned || !_isAvailable.Value)
            return false;

        _isAvailable.Value = false;
        StartCoroutine(_respawnRoutine());
        return true;
    }

    private IEnumerator _respawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);

        if (!IsServer || !IsSpawned)
            yield break;

        transform.SetPositionAndRotation(_initialPosition, _initialRotation);
        _isAvailable.Value = true;
    }

    private void _onAvailabilityChanged(bool _previous, bool _current)
    {
        _applyAvailability(_current);
    }

    private void _applyAvailability(bool _available)
    {
        if (_available)
        {
            transform.SetPositionAndRotation(_initialPosition, _initialRotation);
            transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.World);
        }

        if (_renderers != null)
        {
            foreach (Renderer _renderer in _renderers)
            {
                if (_renderer != null)
                    _renderer.enabled = _available;
            }
        }

        if (_colliders != null)
        {
            foreach (Collider _collider in _colliders)
            {
                if (_collider != null)
                    _collider.enabled = _available;
            }
        }
    }
}
