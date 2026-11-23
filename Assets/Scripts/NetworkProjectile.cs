using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    [Header("Projectile Variables")]
    public float speed = 18f;
    public float lifetime = 4f;
    public float radius = 0.125f;
    public int damage = 25;
    private float timeRemaining;

    [SerializeField] GameObject explosion_vfx;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining = lifetime;
            Debug.Log("PROJECITLE SPAWNED: " + NetworkObjectId + " | " + OwnerClientId);
        }    
            
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned)
            return;

        timeRemaining -= Time.fixedDeltaTime;

        if (timeRemaining <= 0)
        {
            Debug.Log("DESPAWNING");
            NetworkObject.Despawn();
            // - GameObject.Destroy();
            return;
        }

        float distance = speed * Time.fixedDeltaTime;

        // - Handle Collision Detection (COMPLETELY SERVER SIDED)
        if (_tryGetHit(distance, out Collider _hitCollider))
        {
            NetworkObject hitNetworkObject = _hitCollider.GetComponentInParent<NetworkObject>();

            Debug.LogWarning(
                $"PROJECTILE COLLISION\n" +
                $"Source NetworkId: {NetworkObjectId}\n" +
                $"Hit Object: {_hitCollider.name}\n" +
                $"Hit NetworkId: " +
                $"{(hitNetworkObject != null ? hitNetworkObject.NetworkObjectId.ToString() : "NONE")}\n" +
                $"Same Object: {hitNetworkObject == NetworkObject}"
            );

            NetworkStats _hitPlayer = _hitCollider.GetComponentInParent<NetworkStats>();
            if (_hitPlayer != null)
            {
                _awardShooter(100, _getTopSurface(_hitCollider), false);
                _hitPlayer.TakeDamage(damage, OwnerClientId);
            }
            else
            {
                myScoreValue _scoreObj = _hitCollider.GetComponentInParent<myScoreValue>();
                if (_scoreObj != null)
                {
                    _scoreObj._getPopupData(_hitCollider, out Vector3 _popupPos, out bool _moveDownward);
                    _awardShooter(_scoreObj._getPoints(), _popupPos, _moveDownward);
                }
                else if (_hitCollider.CompareTag("Reward"))
                {
                    _awardShooter(100, _getTopSurface(_hitCollider), false);
                }
            }

            Debug.Log("SPHERECAST HIT: " + _hitCollider.name);
            TriggerExplosion();
            NetworkObject.Despawn();
            return;
        }

        transform.position += transform.forward * distance;        
    }

    private bool _tryGetHit(float _distance, out Collider _hitCollider)
    {
        Collider[] _overlaps = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider _col in _overlaps)
        {
            if (_isValidHit(_col))
            {
                _hitCollider = _col;
                return true;
            }
        }

        RaycastHit[] _hits = Physics.SphereCastAll(transform.position, radius, transform.forward, _distance);
        float _closestDist = float.MaxValue;
        _hitCollider = null;

        foreach (RaycastHit _hit in _hits)
        {
            if (_hit.distance < _closestDist && _isValidHit(_hit.collider))
            {
                _closestDist = _hit.distance;
                _hitCollider = _hit.collider;
            }
        }

        return _hitCollider != null;
    }

    private bool _isValidHit(Collider _col)
    {
        if (_col == null || _col.transform.IsChildOf(transform) || _col.isTrigger)
            return false;

        NetworkStats _stats = _col.GetComponentInParent<NetworkStats>();
        if (_stats != null && _stats.OwnerClientId == OwnerClientId)
            return false;

        return true;
    }

    private void _awardShooter(int _points, Vector3 _popupPos, bool _moveDownward)
    {
        if (!IsServer || _points == 0)
            return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out NetworkClient _shooter)
            && _shooter.PlayerObject != null
            && _shooter.PlayerObject.TryGetComponent(out NetworkStats _shooterStats))
        {
            _shooterStats._modifyScoreWithPopup(_points, _popupPos, _moveDownward);
        }
    }

    private Vector3 _getTopSurface(Collider _col)
    {
        Bounds _bounds = _col.bounds;
        return new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z);
    }

    private void TriggerExplosion()
    {
        if (!IsServer)
            return;

        PlayExplosionRpc(transform.position);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayExplosionRpc(Vector3 position)
    {
        GameObject explosion = Instantiate(explosion_vfx, position, Quaternion.identity);
        Destroy(explosion, 5f);
    }
}
