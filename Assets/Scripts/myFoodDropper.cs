using Unity.Netcode;
using UnityEngine;

public class myFoodDropper : NetworkBehaviour
{
    [SerializeField] private NetworkObject[] _foodPrefabs;
    [SerializeField] private float _spawnHeightOffset = 1.5f;
    [SerializeField] private float _upwardForce = 5f;
    [SerializeField] private float _outwardForce = 4f;

    public void _dropRandomFood(ulong _dropperClientId)
    {
        if (!IsServer || _foodPrefabs == null || _foodPrefabs.Length == 0)
            return;

        int _index = Random.Range(0, _foodPrefabs.Length);
        NetworkObject _prefab = _foodPrefabs[_index];
        if (_prefab == null)
            return;

        Vector2 _randomCircle = Random.insideUnitCircle.normalized;
        if (_randomCircle.sqrMagnitude < 0.01f)
            _randomCircle = Vector2.right;

        Vector3 _spawnPos = transform.position + Vector3.up * _spawnHeightOffset;
        NetworkObject _foodInstance = Instantiate(_prefab, _spawnPos, Random.rotation);
        _foodInstance.Spawn();

        if (_foodInstance.TryGetComponent(out myFoodPickup _pickup))
        {
            _pickup._setDropper(_dropperClientId);
        }

        if (_foodInstance.TryGetComponent(out Rigidbody _rb))
        {
            Vector3 _impulse = new Vector3(_randomCircle.x, 0f, _randomCircle.y) * _outwardForce
                               + Vector3.up * _upwardForce;
            _rb.AddForce(_impulse, ForceMode.Impulse);
        }
    }
}
