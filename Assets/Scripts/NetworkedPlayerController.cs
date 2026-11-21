using UnityEngine;
using UnityEngine.InputSystem;

using Unity.Netcode;
using UnityEditor.PackageManager;

public class NetworkedPlayerController : NetworkBehaviour
{
    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference shootAction;
    public InputActionReference turretRotationAction;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 100f;
    public float turretRotationSpeed = 150f;
    public Transform turretPivot;

    [Header("Shooting")]
    public Transform muzzlePoint;
    public NetworkObject projectilePrefab;
    public float fireCooldown = 0.8f;
    private float nextShotTime;

    [Header("Components")]
    public GameObject cam;
    [SerializeField] private MeshRenderer _bodyRenderer;
    private NetworkStats stats;
    private Rigidbody _rb;
    [SerializeField] private Material[] _playerMaterials;

    // - Networked Variables
    private readonly NetworkVariable<float> turretAngle = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        stats = GetComponent<NetworkStats>();
        _rb = GetComponent<Rigidbody>();
        if (cam != null)
        {
            cam.SetActive(IsOwner);
        }
    }

    public void _applyPlayerColor(int _index)
    {
        if (_playerMaterials == null || _playerMaterials.Length == 0 || _index < 0)
            return;

        MeshRenderer _renderer = _bodyRenderer != null ? _bodyRenderer : GetComponentInChildren<MeshRenderer>();
        if (_renderer != null)
        {
            _renderer.material = _playerMaterials[_index % _playerMaterials.Length];
        }
    }

    private void Start()
    {
        if (stats == null)
        {
            stats = GetComponent<NetworkStats>();
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (IsOwner && stats.IsAlive.Value)
        {
            HandleTurret();
            HandleShooting();
        }

        if (!IsOwner && turretPivot != null)
        {
            turretPivot.localRotation = Quaternion.Euler(0f, turretAngle.Value, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner || stats == null || !stats.IsAlive.Value)
            return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        float move = moveInput.y;
        float rot = moveInput.x;

        if (_rb != null && !_rb.isKinematic)
        {
            _rb.MovePosition(_rb.position + transform.forward * move * moveSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, rot * rotationSpeed * Time.fixedDeltaTime, 0f));
        }
        else
        {
            transform.position += transform.forward * move * moveSpeed * Time.fixedDeltaTime;
            transform.Rotate(transform.up * rot * rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void HandleTurret()
    {
        if (turretPivot == null)
            return;

        // - Up Direction * turn input * rotation speed * Time/s
        float turn = turretRotationAction.action.ReadValue<float>();
        turretPivot.transform.Rotate(transform.up * turn * rotationSpeed * Time.deltaTime);

        // - Set the network variable value
        UpdateTurretRpc(turretPivot.localEulerAngles.y);
    }

    [Rpc(SendTo.Server)]
    private void UpdateTurretRpc(float angle)
    {
        angle = Mathf.Repeat(angle, 360f);
        turretAngle.Value = angle;
        turretPivot.localRotation = Quaternion.Euler(0, angle, 0);
    }

    private void HandleShooting()
    {
        if (shootAction.action.WasPressedThisFrame() && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireCooldown;
            // - Shoot our shot
            ShootRpc(OwnerClientId);
        }
    }

    [Rpc(SendTo.Server)]
    private void ShootRpc(ulong ownerId)
    {
        if (stats == null || !stats.IsAlive.Value)
            return;

        if (muzzlePoint == null || projectilePrefab == null)
            return;

        NetworkObject projectile = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
        //projectile.Spawn();
        projectile.SpawnWithOwnership(ownerId);
    }
}
