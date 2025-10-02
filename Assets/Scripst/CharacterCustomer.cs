using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CharacterCustomizer : NetworkBehaviour
{
    [SerializeField] public CustomizationDatabaseSO customizationDatabase; // Cambiado a public
    [SerializeField] public List<Transform> partParents; // Cambiado a public

    public NetworkVariable<PlayerAppearanceData> appearanceData = new(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool customizationApplied = false;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"CharacterCustomizer OnNetworkSpawn para cliente {OwnerClientId} - IsOwner: {IsOwner}, IsHost: {IsHost}");

        // Solo suscribirse a cambios si es un objeto de red real
        if (IsSpawned)
        {
            appearanceData.OnValueChanged += OnAppearanceDataChanged;
        }

        // Aplicar personalizaci�n inicial si hay datos
        if (!string.IsNullOrEmpty(appearanceData.Value.selectedIndices.ToString()))
        {
            Debug.Log($"Aplicando personalizaci�n inicial para {(IsSpawned ? "jugador en red" : "preview")}: {appearanceData.Value.selectedIndices}");
            ApplyCustomization(appearanceData.Value);
            customizationApplied = true;
        }
        else
        {
            Debug.LogWarning($"No hay datos de personalizaci�n iniciales para {(IsSpawned ? "jugador en red" : "preview")}");
        }
    }

    private void Start()
    {
        // Para el preview (que no es un NetworkObject), aplicar personalizaci�n si no se ha aplicado
        if (!IsSpawned && !customizationApplied)
        {
            Debug.Log("CharacterCustomizer iniciado para preview");
            // El preview se manejar� mediante llamadas directas a ApplyCustomization
        }
    }

    private void OnAppearanceDataChanged(PlayerAppearanceData previous, PlayerAppearanceData current)
    {
        Debug.Log($"Datos de apariencia cambiados para cliente {OwnerClientId}: {current.selectedIndices}");

        // Evitar aplicar duplicados si ya se aplic�
        if (!customizationApplied || !current.Equals(previous))
        {
            ApplyCustomization(current);
            customizationApplied = true;
        }
    }

    public void ApplyCustomization(PlayerAppearanceData data)
    {
        string indicesStrValue = data.selectedIndices.ToString();

        if (string.IsNullOrEmpty(indicesStrValue))
        {
            Debug.LogWarning($"{(IsSpawned ? $"Cliente {OwnerClientId}" : "Preview")}: No hay datos de personalizaci�n. Usando apariencia por defecto.");
            return;
        }

        Debug.Log($"Aplicando personalizaci�n para {(IsSpawned ? $"cliente {OwnerClientId}" : "preview")} con �ndices: {indicesStrValue}");

        string[] indicesStr = indicesStrValue.Split(',');

        // Verificar consistencia de datos
        if (partParents == null || partParents.Count == 0)
        {
            Debug.LogError($"ERROR: partParents no est� asignado o est� vac�o");
            return;
        }

        if (indicesStr.Length != partParents.Count)
        {
            Debug.LogError($"ERROR: Se recibieron {indicesStr.Length} �ndices pero se esperaban {partParents.Count}. Datos: {indicesStrValue}");
            return;
        }

        // Aplicar personalizaci�n a cada parte
        for (int i = 0; i < partParents.Count; i++)
        {
            if (partParents[i] == null)
            {
                Debug.LogWarning($"PartParents[{i}] es null");
                continue;
            }

            if (int.TryParse(indicesStr[i], out int selectedIndex))
            {
                ApplyPartCustomization(i, selectedIndex);
            }
            else
            {
                Debug.LogError($"No se pudo parsear el �ndice: {indicesStr[i]}");
            }
        }

        Debug.Log($"Personalizaci�n aplicada correctamente para {(IsSpawned ? $"cliente {OwnerClientId}" : "preview")}");
        customizationApplied = true;
    }

    private void ApplyPartCustomization(int partIndex, int selectedIndex)
    {
        Transform partParent = partParents[partIndex];

        // Verificar que el �ndice est� dentro del rango
        if (selectedIndex < 0 || selectedIndex >= partParent.childCount)
        {
            Debug.LogWarning($"�ndice {selectedIndex} fuera de rango para parte {partIndex} (rango: 0-{partParent.childCount - 1}). Usando 0.");
            selectedIndex = 0;
        }

        for (int j = 0; j < partParent.childCount; j++)
        {
            GameObject child = partParent.GetChild(j).gameObject;
            if (child != null)
            {
                child.SetActive(j == selectedIndex);
            }
        }
    }

    public override void OnDestroy()
    {
        if (appearanceData != null && IsSpawned)
        {
            appearanceData.OnValueChanged -= OnAppearanceDataChanged;
        }
        base.OnDestroy();
    }
}