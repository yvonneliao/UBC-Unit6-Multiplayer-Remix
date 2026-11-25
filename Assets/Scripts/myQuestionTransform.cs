using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class myQuestionTransform : NetworkBehaviour
{
    [SerializeField] private GameObject[] _foodPrefabs;
    [SerializeField] private float _respawnDelay = 8f;
    [SerializeField] private float _upwardForce = 0f;
    [SerializeField] private float _outwardForce = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip _transformSfx;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

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

    private void OnTriggerEnter(Collider _other)
    {
        _tryTransform(_other);
    }

    private void OnCollisionEnter(Collision _collision)
    {
        _tryTransform(_collision.collider);
    }

    private void _tryTransform(Collider _other)
    {
        if (!IsServer || !IsSpawned || !_isAvailable.Value || _other == null)
            return;

        NetworkStats _stats = _other.GetComponentInParent<NetworkStats>();
        if (_stats == null || !_stats.IsAlive.Value)
            return;

        _isAvailable.Value = false;

        bool _waitingForPickup = false;

        if (_foodPrefabs != null && _foodPrefabs.Length > 0)
        {
            GameObject _selectedPrefab = _foodPrefabs[Random.Range(0, _foodPrefabs.Length)];
            if (_selectedPrefab != null)
            {
                GameObject _foodObj = Instantiate(_selectedPrefab, transform.position, Random.rotation);
                if (_foodObj.TryGetComponent(out NetworkObject _foodNetObj))
                {
                    _foodNetObj.Spawn(true);
                }

                if (_foodObj.TryGetComponent(out mySingleUseReward _singleUse))
                {
                    _singleUse._setOnCollected(_handleFoodCollected);
                    _waitingForPickup = true;
                }

                if (_foodObj.TryGetComponent(out Rigidbody _rb) && (_upwardForce != 0f || _outwardForce != 0f))
                {
                    Vector2 _randomCircle = Random.insideUnitCircle.normalized;
                    if (_randomCircle.sqrMagnitude < 0.01f)
                        _randomCircle = Vector2.right;

                    Vector3 _impulse = new Vector3(_randomCircle.x, 0f, _randomCircle.y) * _outwardForce
                                       + Vector3.up * _upwardForce;
                    _rb.AddForce(_impulse, ForceMode.Impulse);
                }
            }
        }

        if (!_waitingForPickup)
        {
            StartCoroutine(_respawnRoutine());
        }
    }

    private void _handleFoodCollected()
    {
        if (!IsServer || !IsSpawned)
            return;

        StartCoroutine(_respawnRoutine());
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
        if (_previous && !_current && _transformSfx != null)
        {
            GameObject _sfxObj = new GameObject("QuestionMarkSfx");
            AudioSource _source = _sfxObj.AddComponent<AudioSource>();
            _source.clip = _transformSfx;
            _source.volume = _sfxVolume;
            _source.spatialBlend = 0f;
            _source.Play();
            Destroy(_sfxObj, _transformSfx.length);
        }

        _applyAvailability(_current);
    }

    private void _applyAvailability(bool _available)
    {
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
