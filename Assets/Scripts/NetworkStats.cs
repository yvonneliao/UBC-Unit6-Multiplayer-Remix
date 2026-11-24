using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkStats : NetworkBehaviour
{
    public int MaxHealth = 100;
    public float RespawnDelay = 5f;
    [SerializeField] private int _damageScoreLoss = 20;

    [Header("Score Popup")]
    [SerializeField] private myScorePopup _scorePopupPrefab;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Lives = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public ParticleSystem smoke_vfx = null;

    [Header("Elimination")]
    [SerializeField] private GameObject _pumpkinPrefab;
    [SerializeField] private GameObject[] _tankVisualsToHide;
    [SerializeField] private int _pumpkinRewardPoints = 300;
    private GameObject _spawnedPumpkin;
    private bool _pumpkinCollected;

    PlayerStatusUI status_ui;

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            Scoreboard scoreboard = FindFirstObjectByType<Scoreboard>(FindObjectsInactive.Include);

            if (scoreboard != null)
            {
                scoreboard.RegisterPlayer(this);
            }
        }

        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
            IsAlive.Value = true;
            StartCoroutine(_initialSpawnRoutine());
        }

        if (Lives.Value <= 0 && !IsAlive.Value)
        {
            _applyPumpkinVisuals();
        }


        if (!IsOwner || !IsClient)
        {
            Debug.Log("Failed to bind local UI");
            return;
        }

        status_ui = GameObject.FindFirstObjectByType<PlayerStatusUI>(FindObjectsInactive.Include);
        if (status_ui != null)
        {
            Debug.Log("Successfully Bound UI Status Bar");
            status_ui.gameObject.SetActive(true);
            status_ui.Bind(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            Scoreboard scoreboard = FindFirstObjectByType<Scoreboard>(FindObjectsInactive.Include);

            if (scoreboard != null)
            {
                scoreboard.UnregisterPlayer(OwnerClientId);
            }
        }

        if (!IsOwner)
            return;

        if (status_ui != null)
        {
            status_ui.Unbind();
        }
    }

    public void TakeDamage(float amount, ulong shooterId)
    {
        if (!IsServer || !IsAlive.Value || shooterId == OwnerClientId)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (TryGetComponent(out myFoodDropper _dropper))
        {
            _dropper._dropRandomFood(OwnerClientId);
        }

        if (CurrentHealth.Value == 0)
        {
            IsAlive.Value = false;
            Lives.Value = Mathf.Max(0, Lives.Value - 1);
            AwardKill(100, shooterId);

            if (Lives.Value > 0)
            {
                ToggleSmokeRpc(true);
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                _turnIntoPumpkinRpc();
            }
        }
    }

    private void AwardKill(int amount, ulong shooterId)
    {
        if (!IsServer) 
            return;

        // - Find the client that fired the projectile
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out NetworkClient shooter))
        {
            Debug.Log("Shooter Disconnected");
            return;
        }

        // - Get the shooter's player object
        NetworkObject playerObject = shooter.PlayerObject;

        if (playerObject == null) return;
        
        // - Server updates the score directly.
        if (playerObject.TryGetComponent(out NetworkStats shooterStats))
        {
            shooterStats._modifyScoreWithPopup(amount, _getTopSurfacePosition());
        }

        Debug.Log($"Awarded {amount} points to Client {shooterId}");
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(RespawnDelay);

        if (!IsSpawned || Lives.Value <= 0) yield break;

        CurrentHealth.Value = MaxHealth;
        IsAlive.Value = true;
        ToggleSmokeRpc(false);
    }

    [Rpc(SendTo.Everyone)]
    private void _turnIntoPumpkinRpc()
    {
        _applyPumpkinVisuals();
    }

    private void _applyPumpkinVisuals()
    {
        if (smoke_vfx != null)
        {
            smoke_vfx.Stop();
            smoke_vfx.gameObject.SetActive(false);
        }

        if (_tankVisualsToHide != null)
        {
            foreach (GameObject _visual in _tankVisualsToHide)
            {
                if (_visual != null)
                    _visual.SetActive(false);
            }
        }

        if (TryGetComponent(out myCharacterSelector _selector))
        {
            _selector._hideCharacter();
        }

        if (_pumpkinPrefab != null && _spawnedPumpkin == null && !_pumpkinCollected)
        {
            _spawnedPumpkin = Instantiate(_pumpkinPrefab, transform);
            _spawnedPumpkin.transform.localPosition = Vector3.zero;
            _spawnedPumpkin.transform.localRotation = Quaternion.identity;
        }
    }

    public bool _tryCollectPumpkin()
    {
        if (!IsServer || _spawnedPumpkin == null || _pumpkinCollected)
            return false;

        if (IsAlive.Value || Lives.Value > 0)
            return false;

        _pumpkinCollected = true;
        _hidePumpkinRpc();
        return true;
    }

    public int _getPumpkinRewardPoints()
    {
        return _pumpkinRewardPoints;
    }

    [Rpc(SendTo.Everyone)]
    private void _hidePumpkinRpc()
    {
        if (_spawnedPumpkin != null)
        {
            Destroy(_spawnedPumpkin);
            _spawnedPumpkin = null;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ToggleSmokeRpc(bool state)
    {
        if (state)
        {
            smoke_vfx.gameObject.SetActive(true);
            smoke_vfx.Play();
        }

        else
        {
            smoke_vfx.Stop();
            smoke_vfx.gameObject.SetActive(false);            
        }
    }

   

    private IEnumerator _initialSpawnRoutine()
    {
        yield return null;

        if (!IsSpawned || SpawnManager.Instance == null)
            yield break;

        SpawnManager.Instance.GetSpawn(out Vector3 _spawnPosition, out Quaternion _spawnRotation);
        transform.SetPositionAndRotation(_spawnPosition, transform.rotation);
        TeleportOwnerRpc(_spawnPosition, transform.rotation);
    }

    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 newPosition, Quaternion newRotation)
    {
        if (TryGetComponent(out NetworkTransform _netTransform))
        {
            _netTransform.Teleport(newPosition, newRotation, transform.localScale);
        }
        else
        {
            transform.SetPositionAndRotation(newPosition, newRotation);
        }

        StartCoroutine(_rotateTowardCenterRoutine(2f));
    }

    private IEnumerator _rotateTowardCenterRoutine(float _duration)
    {
        Vector3 _direction = Vector3.zero - transform.position;
        _direction.y = 0f;

        if (_direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion _startRotation = transform.rotation;
        Quaternion _targetRotation = Quaternion.LookRotation(_direction.normalized, Vector3.up);

        float _elapsed = 0f;
        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float _t = Mathf.Clamp01(_elapsed / _duration);
            transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, _t);
            yield return null;
        }

        transform.rotation = _targetRotation;
    }


    public void AddScore(int amount)
    {
        if (!IsServer)
            return;

        Score.Value += amount;
    }

    public void _modifyScore(int _delta)
    {
        if (!IsServer)
            return;

        Score.Value = Mathf.Max(0, Score.Value + _delta);
    }

    public void _modifyScoreWithPopup(int _delta, Vector3 _popupPosition, bool _moveDownward = false, bool _useCustomColor = false, Color _customColor = default)
    {
        if (!IsServer)
            return;

        _modifyScore(_delta);
        _spawnScorePopupRpc(_delta, _popupPosition, _moveDownward, _useCustomColor, _customColor);
    }

    [Rpc(SendTo.Everyone)]
    public void _spawnScorePopupRpc(int _delta, Vector3 _popupPosition, bool _moveDownward, bool _useCustomColor = false, Color _customColor = default)
    {
        _spawnLocalPopup(_delta, _popupPosition, _moveDownward, _useCustomColor, _customColor);
    }

    public void _modifyScoreWithOwnerPopup(int _delta, Vector3 _popupPosition, bool _moveDownward = false, bool _useCustomColor = false, Color _customColor = default)
    {
        if (!IsServer)
            return;

        _modifyScore(_delta);
        _spawnOwnerScorePopupRpc(_delta, _popupPosition, _moveDownward, _useCustomColor, _customColor);
    }

    [Rpc(SendTo.Owner)]
    public void _spawnOwnerScorePopupRpc(int _delta, Vector3 _popupPosition, bool _moveDownward, bool _useCustomColor = false, Color _customColor = default)
    {
        _spawnLocalPopup(_delta, _popupPosition, _moveDownward, _useCustomColor, _customColor);
    }

    private void _spawnLocalPopup(int _delta, Vector3 _popupPosition, bool _moveDownward, bool _useCustomColor, Color _customColor)
    {
        if (_scorePopupPrefab == null || _delta == 0)
            return;

        myScorePopup _popup = Instantiate(_scorePopupPrefab, _popupPosition, Quaternion.identity);
        _popup._initialize(_delta, _moveDownward, _useCustomColor, _customColor);
    }

    private Vector3 _getTopSurfacePosition()
    {
        Collider _col = GetComponentInChildren<Collider>();
        if (_col == null)
            return transform.position + Vector3.up * 1.5f;

        Bounds _bounds = _col.bounds;
        return new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z);
    }

    public void LoseLife()
    {
        if (!IsServer)
            return;

        Lives.Value = Mathf.Max(0, Lives.Value - 1);
    }
}
