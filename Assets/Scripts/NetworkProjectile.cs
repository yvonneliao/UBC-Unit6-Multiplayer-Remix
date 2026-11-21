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
        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, distance))
        {
            NetworkObject hitNetworkObject =
            hit.collider.GetComponentInParent<NetworkObject>();

            Debug.LogWarning(
                $"PROJECTILE COLLISION\n" +
                $"Source NetworkId: {NetworkObjectId}\n" +
                $"Hit Object: {hit.collider.name}\n" +
                $"Hit NetworkId: " +
                $"{(hitNetworkObject != null ? hitNetworkObject.NetworkObjectId.ToString() : "NONE")}\n" +
                $"Same Object: {hitNetworkObject == NetworkObject}"
            );


            NetworkStats _hitPlayer = hit.collider.GetComponentInParent<NetworkStats>();
            if (_hitPlayer != null)
            {
                if (_hitPlayer.OwnerClientId == OwnerClientId)
                {
                    transform.position += transform.forward * distance;
                    return;
                }

                _awardShooter(100);
                _hitPlayer.TakeDamage(damage, OwnerClientId);
            }
            else
            {
                myScoreValue _scoreObj = hit.collider.GetComponentInParent<myScoreValue>();
                if (_scoreObj != null)
                {
                    _awardShooter(_scoreObj._getPoints());
                }
                else if (hit.collider.CompareTag("Reward"))
                {
                    _awardShooter(100);
                }
            }

            Debug.Log("SPHERECAST HIT: " + hit.collider.name);
            TriggerExplosion();
            NetworkObject.Despawn();
            return;
        }

        transform.position += transform.forward * distance;        
    }

    private void _awardShooter(int _points)
    {
        if (!IsServer || _points == 0)
            return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out NetworkClient _shooter)
            && _shooter.PlayerObject != null
            && _shooter.PlayerObject.TryGetComponent(out NetworkStats _shooterStats))
        {
            _shooterStats._modifyScore(_points);
        }
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
