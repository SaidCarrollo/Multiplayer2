// PlayerSpawner.cs
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            SpawnPlayerForClient(client.Key);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        GameObject playerPrefab = GameDataPersistence.Instance.playerPrefab;
        if (playerPrefab == null) return;

        // 1. Creamos la instancia del jugador
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        // 2. Obtenemos los datos y el script customizer
        PlayerAppearanceData appearance = GameDataPersistence.Instance.GetPlayerAppearance(clientId);
        CharacterCustomizer customizer = playerInstance.GetComponent<CharacterCustomizer>();

        // 3. Asignamos el valor a la variable de red para que los clientes la reciban
        customizer.appearanceData.Value = appearance;

        // 4. ¡EL ARREGLO! Como el Host no recibe la notificación, llamamos al método manualmente
        customizer.ApplyCustomization(appearance);
    }
}