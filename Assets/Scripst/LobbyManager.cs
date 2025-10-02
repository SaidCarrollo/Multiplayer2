using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LobbyPlayerState : INetworkSerializable, System.IEquatable<LobbyPlayerState>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;
    public PlayerAppearanceData Appearance;
    public bool HasConfirmedDetails;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref Appearance);
        serializer.SerializeValue(ref HasConfirmedDetails);
    }

    public bool Equals(LobbyPlayerState other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               IsReady == other.IsReady &&
               Appearance.Equals(other.Appearance) &&
               HasConfirmedDetails == other.HasConfirmedDetails;
    }
}

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }
    public NetworkList<LobbyPlayerState> lobbyPlayers;

    private bool isShuttingDown = false;
    private bool isInitialized = false;
    private HashSet<ulong> connectedClients = new HashSet<ulong>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        lobbyPlayers = new NetworkList<LobbyPlayerState>();
        isInitialized = true;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"LobbyManager OnNetworkSpawn - IsServer: {IsServer}, IsHost: {IsHost}, IsClient: {IsClient}");

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Solo añadir al host si no está ya en la lista
            if (!connectedClients.Contains(NetworkManager.Singleton.LocalClientId))
            {
                AddPlayerToList(NetworkManager.Singleton.LocalClientId);
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || isShuttingDown || !isInitialized) return;

        Debug.Log($"Cliente conectado: {clientId}, ¿Es el host?: {clientId == NetworkManager.Singleton.LocalClientId}");

        // Verificar si el cliente ya está en la lista para evitar duplicados
        if (!connectedClients.Contains(clientId))
        {
            AddPlayerToList(clientId);
        }
        else
        {
            Debug.LogWarning($"El cliente {clientId} ya está en la lista, evitando duplicado");
        }
    }

    private void AddPlayerToList(ulong clientId)
    {
        if (!isInitialized) return;

        connectedClients.Add(clientId);

        lobbyPlayers.Add(new LobbyPlayerState
        {
            ClientId = clientId,
            PlayerName = "Connecting...",
            IsReady = false,
            HasConfirmedDetails = false
        });

        Debug.Log($"Jugador añadido a la lista: {clientId}. Total jugadores: {lobbyPlayers.Count}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer || isShuttingDown || !isInitialized) return;

        try
        {
            connectedClients.Remove(clientId);
            StartCoroutine(RemovePlayerAfterFrame(clientId));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error durante desconexión del cliente {clientId}: {e.Message}");
        }
    }

    private IEnumerator RemovePlayerAfterFrame(ulong clientId)
    {
        yield return null;

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                lobbyPlayers.RemoveAt(i);
                Debug.Log($"Jugador removido de la lista: {clientId}. Total jugadores: {lobbyPlayers.Count}");
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (isShuttingDown || !isInitialized) return;

        ulong clientId = serverRpcParams.Receive.SenderClientId;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                LobbyPlayerState updatedState = lobbyPlayers[i];
                updatedState.IsReady = !updatedState.IsReady;
                lobbyPlayers[i] = updatedState;
                Debug.Log($"Jugador {clientId} cambió estado de ready a: {updatedState.IsReady}");
            }
        }
    }

    public void StartGame()
    {
        if (!IsHost || isShuttingDown || !isInitialized) return;

        Debug.Log("Intentando iniciar juego...");

        // Verificar que todos los jugadores estén listos
        bool allReady = true;
        foreach (var player in lobbyPlayers)
        {
            if (!player.IsReady)
            {
                allReady = false;
                Debug.LogWarning($"Jugador {player.ClientId} no está listo");
            }
        }

        if (!allReady)
        {
            Debug.LogWarning("No todos los jugadores están listos");
            return;
        }

        Debug.Log("Todos los jugadores están listos, guardando datos...");

        // Guardar datos de jugadores
        foreach (var player in lobbyPlayers)
        {
            string playerName = player.PlayerName.ToString();
            Debug.Log($"Guardando datos para cliente {player.ClientId}: {playerName}, Apariencia: {player.Appearance.selectedIndices}");
            GameDataPersistence.Instance.SetPlayerData(player.ClientId, playerName, player.Appearance);
        }

        // Usar SceneTransitionManager si existe, si no, cargar directamente
        var transitionManager = FindObjectOfType<SceneTransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.LoadGameSceneServerRpc();
        }
        else
        {
            Debug.Log("Cargando escena directamente...");
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerDetailsServerRpc(string newName, PlayerAppearanceData appearanceData, ServerRpcParams serverRpcParams = default)
    {
        if (isShuttingDown || !isInitialized) return;

        ulong clientId = serverRpcParams.Receive.SenderClientId;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                var updatedState = lobbyPlayers[i];
                updatedState.PlayerName = newName;
                updatedState.Appearance = appearanceData;
                updatedState.HasConfirmedDetails = true;
                lobbyPlayers[i] = updatedState;

                Debug.Log($"Detalles actualizados para cliente {clientId}: {newName}, Apariencia: {appearanceData.selectedIndices}");
                break;
            }
        }
    }

    public override void OnDestroy()
    {
        isShuttingDown = true;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnDestroy();
    }

    public bool IsReady()
    {
        return isInitialized && !isShuttingDown;
    }
}