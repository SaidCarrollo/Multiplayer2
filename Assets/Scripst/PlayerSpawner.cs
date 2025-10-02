using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    private Dictionary<ulong, NetworkObject> playerObjects = new Dictionary<ulong, NetworkObject>();
    private bool playersSpawned = false;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"PlayerSpawner OnNetworkSpawn - IsServer: {IsServer}, IsHost: {IsHost}");

        if (!IsServer) return;

        Debug.Log("PlayerSpawner iniciado en servidor");

        // Esperar a que NetworkManager esté completamente listo
        StartCoroutine(SpawnPlayersAfterDelay());
    }

    private System.Collections.IEnumerator SpawnPlayersAfterDelay()
    {
        // Esperar varios frames para asegurar que todo esté inicializado
        yield return new WaitForSeconds(1f);

        if (playersSpawned) yield break;

        Debug.Log($"Spawneando jugadores. Clientes conectados: {NetworkManager.Singleton.ConnectedClientsList.Count}");

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (!playerObjects.ContainsKey(client.ClientId))
            {
                SpawnPlayerForClient(client.ClientId);
            }
        }

        playersSpawned = true;
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (playerObjects.ContainsKey(clientId))
        {
            Debug.LogWarning($"El jugador para el cliente {clientId} ya existe");
            return;
        }

        GameObject playerPrefab = GameDataPersistence.Instance.playerPrefab;
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab no encontrado en GameDataPersistence");
            return;
        }

        Debug.Log($"Spawneando jugador para cliente {clientId}");

        // Crear instancia del jugador
        Vector3 spawnPosition = GetSpawnPosition(clientId);
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("El prefab del jugador no tiene NetworkObject");
            Destroy(playerInstance);
            return;
        }

        // Spawnear el objeto de red
        networkObject.SpawnAsPlayerObject(clientId, true);
        playerObjects[clientId] = networkObject;

        // Configurar apariencia - IMPORTANTE: Esperar un frame para que NetworkVariable se sincronice
        StartCoroutine(ApplyAppearanceAfterDelay(clientId, playerInstance));
    }

    private System.Collections.IEnumerator ApplyAppearanceAfterDelay(ulong clientId, GameObject playerInstance)
    {
        yield return new WaitForSeconds(0.5f);

        PlayerAppearanceData appearance = GameDataPersistence.Instance.GetPlayerAppearance(clientId);
        CharacterCustomizer customizer = playerInstance.GetComponent<CharacterCustomizer>();

        if (customizer != null)
        {
            Debug.Log($"Aplicando apariencia para cliente {clientId}: {appearance.selectedIndices}");

            // Para el host, aplicar directamente ya que somos el servidor
            if (IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                customizer.appearanceData.Value = appearance;
                customizer.ApplyCustomization(appearance);
            }
            else
            {
                // Para clientes remotos, asignar el NetworkVariable
                customizer.appearanceData.Value = appearance;
            }
        }
        else
        {
            Debug.LogWarning($"CharacterCustomizer no encontrado para cliente {clientId}");
        }
    }

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        // Calcular índice del jugador basado en su posición en la lista de clientes
        int playerIndex = 0;
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
        {
            if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientId)
            {
                playerIndex = i;
                break;
            }
        }

        float spacing = 3f;
        return new Vector3(playerIndex * spacing, 0, 0);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDestroyPlayerServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        DestroyPlayer(clientId);
    }

    private void DestroyPlayer(ulong clientId)
    {
        if (!IsServer) return;

        if (playerObjects.ContainsKey(clientId))
        {
            NetworkObject playerObj = playerObjects[clientId];
            if (playerObj != null && playerObj.IsSpawned)
            {
                playerObj.Despawn(true);
            }
            playerObjects.Remove(clientId);
            Debug.Log($"Jugador destruido para cliente {clientId}");
        }
    }

    public override void OnDestroy()
    {
        // Limpiar todos los objetos de jugador
        if (IsServer)
        {
            foreach (var kvp in playerObjects)
            {
                if (kvp.Value != null && kvp.Value.IsSpawned)
                {
                    kvp.Value.Despawn(true);
                }
            }
            playerObjects.Clear();
        }

        base.OnDestroy();
    }
}