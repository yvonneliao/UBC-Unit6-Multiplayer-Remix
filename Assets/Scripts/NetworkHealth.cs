using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    public int MaxHealth = 100;
    public float RespawnDelay = 5f;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public ParticleSystem smoke_vfx = null;  

    PlayerStatusUI status_ui;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
            IsAlive.Value = true;
        }

        if (IsOwner)
        {
            status_ui = GameObject.FindFirstObjectByType<PlayerStatusUI>(FindObjectsInactive.Include);
            if (status_ui != null)
            {
                Debug.Log("Successfully Bound UI Status Bar");
                status_ui.gameObject.SetActive(true);                              
                status_ui.Bind(GetComponent<NetworkStats>());
            }
        }
        
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        if (status_ui != null)
        {
            status_ui.Unbind();
        }
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer || !IsAlive.Value)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (CurrentHealth.Value == 0)
        {
            ToggleSmokeRpc(true);
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
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

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(RespawnDelay);

        if (!IsSpawned) yield break;

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.GetSpawn(out spawnPosition, out spawnRotation);
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        TeleportOwnerRpc(spawnPosition, spawnRotation);

        CurrentHealth.Value = MaxHealth;
        IsAlive.Value = true;
        ToggleSmokeRpc(false);

    }

    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 newPosition, Quaternion newRotation)
    {
        transform.SetPositionAndRotation(newPosition, newRotation);
    }
}
