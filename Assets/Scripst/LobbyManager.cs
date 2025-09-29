// LobbyManager.cs
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;

// Estructura actualizada para sincronizar los datos del jugador en el lobby
public struct LobbyPlayerState : INetworkSerializable, System.IEquatable<LobbyPlayerState>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;
    public PlayerAppearanceData Appearance;
    public bool HasConfirmedDetails; // <- AÑADE ESTA LÍNEA

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref Appearance);
        serializer.SerializeValue(ref HasConfirmedDetails); // <- AÑADE ESTA LÍNEA
    }

    public bool Equals(LobbyPlayerState other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               IsReady == other.IsReady &&
               Appearance.Equals(other.Appearance) &&
               HasConfirmedDetails == other.HasConfirmedDetails; // <- AÑADE ESTA LÍNEA
    }
}

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }
    public NetworkList<LobbyPlayerState> lobbyPlayers;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        lobbyPlayers = new NetworkList<LobbyPlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Esta es la forma CORRECTA de añadir al Host.
            // Se añade a sí mismo a la lista cuando el servidor arranca.
            lobbyPlayers.Add(new LobbyPlayerState
            {
                ClientId = NetworkManager.Singleton.LocalClientId,
                PlayerName = "Connecting...", // Se actualizará después
                IsReady = false,
                HasConfirmedDetails = false // Asumiendo que sigues usando la solución anterior
            });

            // Suscribirse a los eventos DESPUÉS de manejar al Host.
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        // ¡IMPORTANTE! Este 'if' evita que el Host se añada por segunda vez.
        if (!IsServer || clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        if (lobbyPlayers.Count >= 5) return;

        // Los nuevos clientes se añaden aquí.
        lobbyPlayers.Add(new LobbyPlayerState
        {
            ClientId = clientId,
            PlayerName = "Connecting...",
            IsReady = false,
            HasConfirmedDetails = false // El cliente deberá confirmar sus detalles
        });
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }


    private void HandleClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ClientId == clientId)
                {
                    lobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                // Actualizar el estado del jugador
                LobbyPlayerState updatedState = lobbyPlayers[i];
                updatedState.IsReady = !updatedState.IsReady;
                lobbyPlayers[i] = updatedState;
            }
        }
    }

    public void StartGame()
    {
        if (!IsHost) return;
        foreach (var player in lobbyPlayers) { if (!player.IsReady) return; }

        // Guardamos los datos de todos los jugadores en el objeto persistente
        foreach (var player in lobbyPlayers)
        {
            GameDataPersistence.Instance.SetPlayerData(player.ClientId, player.PlayerName.ToString(), player.Appearance);
        }

        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerDetailsServerRpc(string newName, PlayerAppearanceData appearanceData, ServerRpcParams serverRpcParams = default)
    {
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
                break;
            }
        }
    }
}