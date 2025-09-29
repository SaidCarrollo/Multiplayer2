using Unity.Netcode;
using Unity.Collections;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {
        Instance = this;
        lobbyPlayers = new NetworkList<LobbyPlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Añadir al host/servidor a la lista de jugadores
            AddPlayerToList(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        AddPlayerToList(clientId);
    }

    private void AddPlayerToList(ulong clientId)
    {
        lobbyPlayers.Add(new LobbyPlayerState
        {
            ClientId = clientId,
            PlayerName = "Connecting...",
            IsReady = false,
            HasConfirmedDetails = false
        });
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                lobbyPlayers.RemoveAt(i);
                break;
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