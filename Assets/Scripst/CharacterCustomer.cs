// CharacterCustomizer.cs
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CharacterCustomizer : NetworkBehaviour
{
    [SerializeField] private CustomizationDatabaseSO customizationDatabase;
    [SerializeField] private List<Transform> partParents;

    public NetworkVariable<PlayerAppearanceData> appearanceData = new(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {

        appearanceData.OnValueChanged += (prev, current) => ApplyCustomization(current);

        if (!string.IsNullOrEmpty(appearanceData.Value.selectedIndices.ToString()))
        {
            ApplyCustomization(appearanceData.Value);
        }
    }


    public void ApplyCustomization(PlayerAppearanceData data)
    {
        string indicesStrValue = data.selectedIndices.ToString();

        // MODIFICACIÓN: Hacemos el mensaje más descriptivo.
        if (string.IsNullOrEmpty(indicesStrValue))
        {
            // Este nuevo mensaje te dirá inmediatamente que los datos no llegaron desde el lobby.
            Debug.LogWarning($"Client {OwnerClientId}: No customization data was provided from the lobby. Appearance will be default.");
            return;
        }

        Debug.Log($"Applying customization for client {OwnerClientId} with indices: {indicesStrValue}");

        string[] indicesStr = indicesStrValue.Split(',');
        if (indicesStr.Length != partParents.Count)
        {
            // Este error también es muy útil para diagnosticar problemas de configuración.
            Debug.LogError($"CRITICAL MISMATCH on Client {OwnerClientId}: Received {indicesStr.Length} customization indices, but the prefab has {partParents.Count} parts to customize. Check your database and prefab configuration.");
            return;
        }

        // --- NUEVO LOG DE DIAGNÓSTICO ---
        Debug.Log($"Received {indicesStr.Length} indices. Expecting {partParents.Count} parts.");

        if (indicesStr.Length != partParents.Count)
        {
            Debug.LogError("¡ERROR CRÍTICO! El número de índices de personalización no coincide con el número de partes del personaje. No se puede aplicar la apariencia.");
            return;
        }

        for (int i = 0; i < partParents.Count; i++)
        {
            if (int.TryParse(indicesStr[i], out int selectedIndex))
            {
                for (int j = 0; j < partParents[i].childCount; j++)
                {
                    partParents[i].GetChild(j).gameObject.SetActive(j == selectedIndex);
                }
            }
        }
    }
}