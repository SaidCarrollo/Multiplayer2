// GameDataPersistence.cs
using System.Collections.Generic;
using UnityEngine;

public class GameDataPersistence : MonoBehaviour
{
    public static GameDataPersistence Instance { get; private set; }

    // Asigna tus prefabs de jugador en el Inspector
    public GameObject playerPrefab; // Un solo prefab de jugador

    // Diccionarios para guardar los datos de cada jugador
    private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();
    private Dictionary<ulong, PlayerAppearanceData> playerAppearances = new Dictionary<ulong, PlayerAppearanceData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Métodos para guardar los datos
    public void SetPlayerData(ulong clientId, string name, PlayerAppearanceData appearance)
    {
        playerNames[clientId] = name;
        playerAppearances[clientId] = appearance;
    }

    // Métodos para obtener los datos
    public string GetPlayerName(ulong clientId) => playerNames.GetValueOrDefault(clientId, "Player");
    public PlayerAppearanceData GetPlayerAppearance(ulong clientId) => playerAppearances.GetValueOrDefault(clientId);
}