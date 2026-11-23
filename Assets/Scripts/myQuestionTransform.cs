using Unity.Netcode;
using UnityEngine;

public class myQuestionTransform : NetworkBehaviour
{
    [SerializeField] private GameObject _cheesePrefab;
    [SerializeField] private float _upwardForce = 4f;
    [SerializeField] private float _outwardForce = 3f;

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
            GameObject _cheeseObj = Instantiate(_cheesePrefab, transform.position, Random.rotation);
            if (_cheeseObj.TryGetComponent(out NetworkObject _cheeseNetObj))
            {
                _cheeseNetObj.Spawn(true);
            }

            if (_cheeseObj.TryGetComponent(out Rigidbody _rb))
            {
                Vector2 _randomCircle = Random.insideUnitCircle.normalized;
                if (_randomCircle.sqrMagnitude < 0.01f)
                    _randomCircle = Vector2.right;

                Vector3 _impulse = new Vector3(_randomCircle.x, 0f, _randomCircle.y) * _outwardForce
                                   + Vector3.up * _upwardForce;
                _rb.AddForce(_impulse, ForceMode.Impulse);
            }
        }

        NetworkObject.Despawn(false);
    }
}
