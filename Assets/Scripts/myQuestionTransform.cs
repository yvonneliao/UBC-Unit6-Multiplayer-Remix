using Unity.Netcode;
using UnityEngine;

public class myQuestionTransform : NetworkBehaviour
{
    [SerializeField] private GameObject _cheesePrefab;
    [SerializeField] private float _cheeseHeightOffset = 0.5f;

    private bool _triggered;

    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
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
        if (!IsServer || !IsSpawned || _triggered || _other == null)
            return;

        NetworkStats _stats = _other.GetComponentInParent<NetworkStats>();
        if (_stats == null || !_stats.IsAlive.Value)
            return;

        _triggered = true;

        if (_cheesePrefab != null)
        {
            Vector3 _spawnPos = new Vector3(
                transform.position.x,
                _stats.transform.position.y + _cheeseHeightOffset,
                transform.position.z);

            GameObject _cheeseObj = Instantiate(_cheesePrefab, _spawnPos, Quaternion.identity);
            if (_cheeseObj.TryGetComponent(out NetworkObject _cheeseNetObj))
            {
                _cheeseNetObj.Spawn(true);
            }
        }

        NetworkObject.Despawn(false);
    }
}
