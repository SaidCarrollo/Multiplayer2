using System.Collections.Generic;
using UnityEngine;

public class GameDataPersistence : MonoBehaviour
{
    public static GameDataPersistence Instance { get; private set; }

    public GameObject playerPrefab;

    private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();
    private Dictionary<ulong, PlayerAppearanceData> playerAppearances = new Dictionary<ulong, PlayerAppearanceData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("GameDataPersistence inicializado");
    }

    public void SetPlayerData(ulong clientId, string name, PlayerAppearanceData appearance)
    {
        playerNames[clientId] = name;
        playerAppearances[clientId] = appearance;

        Debug.Log($"Datos guardados para cliente {clientId}: {name}, Apariencia: {appearance.selectedIndices}");
    }

    public string GetPlayerName(ulong clientId)
    {
        if (playerNames.TryGetValue(clientId, out string name))
        {
            return name;
        }
        return $"Player_{clientId}";
    }

    public PlayerAppearanceData GetPlayerAppearance(ulong clientId)
    {
        if (playerAppearances.TryGetValue(clientId, out PlayerAppearanceData appearance))
        {
            return appearance;
        }

        // Devolver apariencia por defecto si no se encuentra
        Debug.LogWarning($"No se encontró apariencia para cliente {clientId}, usando valores por defecto");
        return new PlayerAppearanceData { selectedIndices = "0,0,0,0" };
    }

    public void ClearData()
    {
        playerNames.Clear();
        playerAppearances.Clear();
        Debug.Log("Datos de GameDataPersistence limpiados");
    }
}