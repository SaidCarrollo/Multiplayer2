// LobbyUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private List<PlayerPanel> playerPanels; // Asigna 5 paneles en el Inspector

    private LobbyManager lobbyManager;

    void Start()
    {
        lobbyManager = LobbyManager.Instance;
        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager no encontrado.");
            return;
        }

        // Suscribirse a los cambios en la lista de jugadores
        lobbyManager.lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;

        readyButton.onClick.AddListener(OnReadyClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        // Ocultar el botón de empezar juego si no somos el host
        startGameButton.gameObject.SetActive(false);
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool allPlayersReady = lobbyManager.lobbyPlayers.Count > 0;

        for (int i = 0; i < playerPanels.Count; i++)
        {
            if (i < lobbyManager.lobbyPlayers.Count)
            {
                var player = lobbyManager.lobbyPlayers[i];
                playerPanels[i].ActivatePanel(player.PlayerName.ToString());
                playerPanels[i].SetReady(player.IsReady);

                if (!player.IsReady)
                {
                    allPlayersReady = false;
                }
            }
            else
            {
                playerPanels[i].DeactivatePanel();
            }
        }

        // Lógica para el botón de empezar juego
        if (NetworkManager.Singleton.IsHost)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.interactable = allPlayersReady;
        }
    }

    private void OnReadyClicked()
    {
        lobbyManager.ToggleReadyServerRpc();
    }



    private void OnStartGameClicked()
    {
        lobbyManager.StartGame();
    }

    private void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.lobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
        }
    }
}