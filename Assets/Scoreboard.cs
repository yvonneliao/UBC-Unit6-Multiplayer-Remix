using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    [SerializeField] private Transform rowContainer;
    [SerializeField] private PlayerStatusUI rowPrefab;

    private readonly Dictionary<ulong, PlayerStatusUI> rows = new();
    private readonly Dictionary<ulong, NetworkStats> players = new();

    public void RegisterPlayer(NetworkStats player)
    {
        ulong id = player.OwnerClientId;

        // - Prevent duplicate registrations
        if (players.ContainsKey(id))
            return;

        players.Add(id, player);

        // - Create local UI row
        PlayerStatusUI row = Instantiate(rowPrefab, rowContainer);
        rows.Add(id, row);

        // - Subscribe to network changes
        player.CurrentHealth.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);
        player.Lives.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);
        player.Score.OnValueChanged += (oldValue, newValue) => RefreshPlayer(id);

        RefreshPlayer(id);
    }

    public void UnregisterPlayer(ulong id)
    {
        if (!players.ContainsKey(id))
            return;

        players.Remove(id);

        if (rows.TryGetValue(id, out PlayerStatusUI row))
        {
            Destroy(row.gameObject);
            rows.Remove(id);
        }
    }

    private void RefreshPlayer(ulong id)
    {
        if (!players.TryGetValue(id, out NetworkStats player))
            return;

        if (!rows.TryGetValue(id, out PlayerStatusUI row))
            return;

        row.UpdateContent($"{id}", (int)player.CurrentHealth.Value, player.Lives.Value, player.Score.Value);
    }
}
