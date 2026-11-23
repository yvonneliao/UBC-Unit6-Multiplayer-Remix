using Unity.Netcode;
using UnityEngine;

public class myQuestionTransform : NetworkBehaviour
{
    [SerializeField] private GameObject _cheesePrefab;

    private bool _triggered;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[myQuestionTransform] OnNetworkSpawn: {name} | IsServer={IsServer} | IsSpawned={IsSpawned} | pos={transform.position}");
    }

    private void OnTriggerEnter(Collider _other)
    {
        Debug.Log($"[myQuestionTransform] OnTriggerEnter: {name} hit by {_other.name}");
        _tryTransform(_other);
    }

    private void OnCollisionEnter(Collision _collision)
    {
        Debug.Log($"[myQuestionTransform] OnCollisionEnter: {name} hit by {_collision.collider.name}");
        _tryTransform(_collision.collider);
    }

    private void _tryTransform(Collider _other)
    {
        if (!IsServer || !IsSpawned || _triggered || _other == null)
        {
            Debug.Log($"[myQuestionTransform] Ignored: IsServer={IsServer}, IsSpawned={IsSpawned}, _triggered={_triggered}");
            return;
        }

        NetworkStats _stats = _other.GetComponentInParent<NetworkStats>();
        if (_stats == null || !_stats.IsAlive.Value)
        {
            Debug.Log($"[myQuestionTransform] No alive NetworkStats on {_other.name}");
            return;
        }

        _triggered = true;

        if (_cheesePrefab != null)
        {
            GameObject _cheeseObj = Instantiate(_cheesePrefab, transform.position, transform.rotation);
            if (_cheeseObj.TryGetComponent(out NetworkObject _cheeseNetObj))
            {
                _cheeseNetObj.Spawn(true);
                Debug.Log($"[myQuestionTransform] Spawned cheese at {transform.position}");
            }
        }
        else
        {
            Debug.LogWarning("[myQuestionTransform] _cheesePrefab is null!");
        }

        NetworkObject.Despawn(true);
    }
}
