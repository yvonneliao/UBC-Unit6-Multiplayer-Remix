using UnityEngine;
using System;
using System.Threading.Tasks;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR

#endif

public class RelayLauncher : MonoBehaviour
{
    // ============================================
    [Header("Relay Settings")]
    [SerializeField] int maxPlayers = 4;
    private const string ConnectionType = "dtls";
    private bool isBusy = false;

    public string JoinCode { get; private set; }

    [Header("UI")]
    [SerializeField] TMP_InputField codeInputField;
    [SerializeField] private GameObject _startHostButton;
    [SerializeField] private GameObject _startClientButton;
    // ============================================

    private void Start()
    {
        bool _isHostPlayer = true;
#if UNITY_EDITOR
        _isHostPlayer = Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
#endif
        if (_startHostButton != null)
            _startHostButton.SetActive(_isHostPlayer);

        if (_startClientButton != null)
            _startClientButton.SetActive(!_isHostPlayer);
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            // - Authentication 
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void StartHost()
    {
        if (isBusy || NetworkManager.Singleton.IsListening)
            return;

        isBusy = true;

        try
        {
            // - Only authenticate the player when they try to connect to network through UI
            await InitializeServices();

            int maxConnections = maxPlayers - 1;

            // - Reserves Relay connection slots for clients
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // - Configures Unity Transport to communicate through Relay
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));

            // - Generates the code other players can use when starting server
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host started successfully.");
                Debug.Log("Relay Join Code: " + JoinCode);
            }
            else
            {
                Debug.LogError("Failed to start host.");
            }
        }

        catch (Exception e)
        {
            Debug.LogError("Relay Host Error: " + e.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

    public async void JoinGame()
    {
        if (isBusy || NetworkManager.Singleton.IsListening)
            return;

        if (string.IsNullOrEmpty(codeInputField.text))
        {
            Debug.Log("Please enter a join code.");
            return;
        }

        isBusy = true;

        try
        {
            // - Only authenticate the player when they try to connect to network through UI
            await InitializeServices();

            string joinCode = codeInputField.text.Trim().ToUpperInvariant();

            // - Retrieves the allocation information for the given join code
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // - Configures Unity Transport to communicate through Relay
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client connected successfully.");
            }
            else
            {
                Debug.LogError("Failed to start client.");
            }
        }

        catch (Exception e)
        {
            Debug.LogError("Relay Join Error: " + e.Message);
        }
        finally
        {
            isBusy = false;
        }
    }

}
