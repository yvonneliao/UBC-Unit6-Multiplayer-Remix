using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkStats : NetworkBehaviour
{
    public int MaxHealth = 100;
    public float RespawnDelay = 5f;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Lives = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public ParticleSystem smoke_vfx = null;  

    PlayerStatusUI status_ui;

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            Scoreboard scoreboard = FindFirstObjectByType<Scoreboard>(FindObjectsInactive.Include);

            if (scoreboard != null)
            {
                scoreboard.RegisterPlayer(this);
            }
        }

        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
            IsAlive.Value = true;
        }


        if (!IsOwner || !IsClient)
        {
            Debug.Log("Failed to bind local UI");
            return;
        }

        status_ui = GameObject.FindFirstObjectByType<PlayerStatusUI>(FindObjectsInactive.Include);
        if (status_ui != null)
        {
            Debug.Log("Successfully Bound UI Status Bar");
            status_ui.gameObject.SetActive(true);
            status_ui.Bind(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            Scoreboard scoreboard = FindFirstObjectByType<Scoreboard>(FindObjectsInactive.Include);

            if (scoreboard != null)
            {
                scoreboard.UnregisterPlayer(OwnerClientId);
            }
        }

        if (!IsOwner)
            return;

        if (status_ui != null)
        {
            status_ui.Unbind();
        }
    }

    public void TakeDamage(float amount, ulong shooterId)
    {
        if (!IsServer || !IsAlive.Value)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (CurrentHealth.Value == 0)
        {
            ToggleSmokeRpc(true);
            IsAlive.Value = false;
            AwardKill(100, shooterId);
            StartCoroutine(RespawnRoutine());
        }
    }

    private void AwardKill(int amount, ulong shooterId)
    {
        if (!IsServer) 
            return;

        // - Find the client that fired the projectile
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out NetworkClient shooter))
        {
            Debug.Log("Shooter Disconnected");
            return;
        }

        // - Get the shooter's player object
        NetworkObject playerObject = shooter.PlayerObject;

        if (playerObject == null) return;
        
        // - Server updates the score directly.
        AddScore(100);

        Debug.Log($"Awarded {amount} points to Client {shooterId}");
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(RespawnDelay);

        if (!IsSpawned) yield break;

        if (Lives.Value <= 0) yield break;

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
        Lives.Value -= 1;
        ToggleSmokeRpc(false);

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

   

    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 newPosition, Quaternion newRotation)
    {
        transform.SetPositionAndRotation(newPosition, newRotation);
    }


    public void AddScore(int amount)
    {
        if (!IsServer)
            return;

        Score.Value += amount;
    }

    public void LoseLife()
    {
        if (!IsServer)
            return;

        Lives.Value = Mathf.Max(0, Lives.Value - 1);
    }
}
