using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Sessions")]
    public ISession CurrentSession { get; private set; }

    public event Action OnLobbyUpdated;

    private bool isBusy;

    // ================ READY STATUS IMPLEMENTATION ====================
    private const string ReadyKey = "Ready";
    public bool GetPlayerReady(IReadOnlyPlayer player)
    {
        if (player.Properties.TryGetValue(ReadyKey, out PlayerProperty readyProperty))
        {
            return readyProperty.Value == "true";
        }

        return false;
    }

    private bool isUpdatingReady;
    public async void SetReady(bool isReady)
    {
        if (CurrentSession == null || isUpdatingReady)
            return;

        isUpdatingReady = true;

        try
        {
            // - Sets the current player's ready status in the session properties.
            // - The ready status is stored as a string ("true" or "false") for visibility to other players in the lobby.
            CurrentSession.CurrentPlayer.SetProperty(ReadyKey, new PlayerProperty(value: isReady ? "true" : "false", visibility: VisibilityPropertyOptions.Member));

            // - Saves the current player's data to the session, ensuring that the ready status is updated for all players in the lobby.
            await CurrentSession.SaveCurrentPlayerDataAsync();
            Debug.Log("Ready status updated: " + isReady);

            OnLobbyUpdated?.Invoke();
        }

        catch (Exception e)
        {
            Debug.LogError("Failed to update ready status: " + e.Message);
        }
        finally
        {
            isUpdatingReady = false;
        }
    }

    private void OnPlayerPropertiesChanged()
    {
        Debug.Log("Player properties updated.");
        OnLobbyUpdated?.Invoke();
    }

    public bool AllPlayersReady()
    {
        if (CurrentSession == null)
            return false;

        // - Require at least two players to start
        if (CurrentSession.Players.Count < 2)
            return false;

        foreach (var player in CurrentSession.Players)
        {
            if (!GetPlayerReady(player))
                return false;
        }

        return true;
    }

    // ================ READY STATUS IMPLEMENTATION ====================

    // - Singleton pattern to ensure only one instance of SessionManager exists
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {

           Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateLobby()
    {
        if (isBusy || CurrentSession != null)
            return;

        isBusy = true;

        try
        {
            await InitializeServices();

            var options = new SessionOptions
            {
                MaxPlayers = 4
            }.WithRelayNetwork();

            // - Create a new session (lobby)
            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            SubscribeToSession();

            Debug.Log("Lobby Created!");
            Debug.Log("Join Code: " + CurrentSession.Code);

            OnLobbyUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
        }
        finally
        {
            isBusy = false;
        }
    }

    public async void JoinLobby(string joinCode)
    {
        if (isBusy || CurrentSession != null)
            return;

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.Log("Please enter a join code.");
            return;
        }

        isBusy = true;

        try
        {
            await InitializeServices();

            joinCode = joinCode.Trim().ToUpperInvariant();
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            SubscribeToSession();

            Debug.Log("Joined lobby successfully");

            OnLobbyUpdated?.Invoke();
        }

        catch (Exception e)
        {
            Debug.LogError("Join Loby Error: " + e.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    private void SubscribeToSession()
    {
        CurrentSession.PlayerJoined += OnPlayerJoined;
        CurrentSession.PlayerLeaving += OnPlayerLeft;

        // ================ READY STATUS IMPLEMENTATION ====================
        CurrentSession.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
        // ================ READY STATUS IMPLEMENTATION ====================
    }

    private void OnPlayerJoined(string playerId)
    {
        Debug.Log($"Player {playerId} joined the lobby.");
        OnLobbyUpdated?.Invoke();
    }

    private void OnPlayerLeft(string playerId)
    {
        Debug.Log($"Player {playerId} left the lobby.");
        OnLobbyUpdated?.Invoke();
    }

    public async void LeaveLobby()
    {
        if (CurrentSession == null || isBusy)
            return;

        isBusy = true;

        try
        {
            await CurrentSession.LeaveAsync();
            CurrentSession = null;
            OnLobbyUpdated?.Invoke();
        }

        catch (Exception e)
        {
            Debug.LogError("Leave Lobby Error: " + e);
        }

        finally
        {
            isBusy = false;
        }
    }

    public void StartMatch()
    {
        if (CurrentSession == null)
            return;

        if (!NetworkManager.Singleton.IsHost)
            return;

        // ================ READY STATUS IMPLEMENTATION ====================
        if (!AllPlayersReady())
        {
            Debug.LogWarning("Cannot start: Not all players are ready.");
            return;
        }
        // ================ READY STATUS IMPLEMENTATION ====================

        NetworkManager.Singleton.SceneManager.LoadScene("TankArena", LoadSceneMode.Single);
    }

}
