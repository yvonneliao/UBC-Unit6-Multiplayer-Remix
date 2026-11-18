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


            if (hit.collider.TryGetComponent(out NetworkStats health))
            {
                health.TakeDamage(damage, OwnerClientId);
            }

            else
            {
                NetworkStats parentHealth = hit.collider.GetComponentInParent<NetworkStats>();

                if (parentHealth != null)
                {
                    parentHealth.TakeDamage(damage, OwnerClientId);
                }
            }

            Debug.Log("SPHERECAST HIT: " + hit.collider.name);
            TriggerExplosion();
            NetworkObject.Despawn();
            return;
        }

        transform.position += transform.forward * distance;        
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
