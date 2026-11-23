using Unity.Netcode;
using UnityEngine;

public class myQuestionTransform : NetworkBehaviour
{
    [SerializeField] private GameObject _cheesePrefab;

    private bool _triggered;

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
        if (!IsServer || !IsSpawned || _triggered || _other == null)
            return;

        NetworkStats _stats = _other.GetComponentInParent<NetworkStats>();
        if (_stats == null || !_stats.IsAlive.Value)
            return;

        _triggered = true;

        if (_cheesePrefab != null)
        {
            GameObject _cheeseObj = Instantiate(_cheesePrefab, transform.position, transform.rotation);
            if (_cheeseObj.TryGetComponent(out NetworkObject _cheeseNetObj))
            {
                _cheeseNetObj.Spawn(true);
            }
        }

        NetworkObject.Despawn(true);
    }
}
