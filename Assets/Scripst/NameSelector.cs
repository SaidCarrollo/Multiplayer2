using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;

public class NameSelector : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject namePanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("UI Components")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private CustomizationDatabaseSO database;
    [SerializeField] private List<TMP_Text> optionTexts;
    [SerializeField] private List<Button> nextButtons;
    [SerializeField] private List<Button> previousButtons;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform previewParent;

    private List<int> currentSelections;
    private CharacterCustomizer previewCustomizer;
    private GameObject previewInstance;

    void Awake()
    {
        if (database == null)
        {
            Debug.LogError("¡La base de datos de personalización no está asignada en el Inspector!", this);
            return;
        }

        currentSelections = new List<int>();
        for (int i = 0; i < database.customizationParts.Count; i++)
        {
            currentSelections.Add(0);
        }
    }

    void Start()
    {
        if (database == null) return;

        confirmButton.onClick.AddListener(OnConfirm);
        nameInputField.onValueChanged.AddListener(ValidateInput);

        CreatePreviewInstance();
        InitializeUI();
    }

    private void CreatePreviewInstance()
    {
        if (playerPrefab == null) return;

        // Instanciar preview
        previewInstance = Instantiate(playerPrefab, previewParent);

        // Remover componentes de red para el preview
        RemoveNetworkComponents(previewInstance);

        previewCustomizer = previewInstance.GetComponent<CharacterCustomizer>();

        // ASIGNAR LA BASE DE DATOS AL PREVIEW - ESTO ES LO QUE FALTABA
        if (previewCustomizer != null)
        {
            // Buscar la base de datos en el proyecto si no está asignada
            if (previewCustomizer.customizationDatabase == null)
            {
                previewCustomizer.customizationDatabase = database;
                Debug.Log("Base de datos asignada al preview customizer");
            }

            // También podemos forzar la inicialización del preview
            InitializePreviewCustomizer();
        }
        else
        {
            Debug.LogError("No se encontró CharacterCustomizer en el preview");
        }

        UpdatePreviewAppearance();
    }

    private void InitializePreviewCustomizer()
    {
        // Asegurarnos de que el preview customizer tenga todas las referencias necesarias
        if (previewCustomizer.partParents == null || previewCustomizer.partParents.Count == 0)
        {
            Debug.LogWarning("El preview customizer no tiene partParents asignados. Buscando automáticamente...");

            // Buscar transforms que podrían ser los partParents
            List<Transform> foundParts = new List<Transform>();
            foreach (Transform child in previewInstance.transform)
            {
                // Asumiendo que los partParents son hijos directos con múltiples opciones
                if (child.childCount > 1)
                {
                    foundParts.Add(child);
                    Debug.Log($"Encontrado posible partParent: {child.name} con {child.childCount} hijos");
                }
            }

            if (foundParts.Count >= database.customizationParts.Count)
            {
                previewCustomizer.partParents = foundParts;
                Debug.Log($"Se asignaron {foundParts.Count} partParents al preview");
            }
        }
    }

    private void RemoveNetworkComponents(GameObject obj)
    {
        // Remover NetworkObject si existe
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            DestroyImmediate(netObj);
        }

        // Remover NetworkTransform si existe
        NetworkTransform netTransform = obj.GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            DestroyImmediate(netTransform);
        }

        // Remover NetworkBehaviour excepto CharacterCustomizer
        NetworkBehaviour[] netBehaviours = obj.GetComponents<NetworkBehaviour>();
        foreach (var behaviour in netBehaviours)
        {
            if (behaviour != null && behaviour != previewCustomizer)
            {
                DestroyImmediate(behaviour);
            }
        }

        // También remover cualquier otro componente de Netcode
        NetworkAnimator netAnimator = obj.GetComponent<NetworkAnimator>();
        if (netAnimator != null)
        {
            DestroyImmediate(netAnimator);
        }
    }

    private void InitializeUI()
    {
        for (int i = 0; i < nextButtons.Count; i++)
        {
            int index = i;
            nextButtons[i].onClick.AddListener(() => NextOption(index));
            previousButtons[i].onClick.AddListener(() => PreviousOption(index));
        }
        UpdateAllUI();
        ValidateInput(nameInputField.text);
    }

    private void UpdatePreviewAppearance()
    {
        if (previewCustomizer == null)
        {
            Debug.LogWarning("PreviewCustomizer es null, no se puede actualizar apariencia");
            return;
        }

        string indices = string.Join(",", currentSelections);
        Debug.Log($"Actualizando preview con índices: {indices}");

        PlayerAppearanceData data = new PlayerAppearanceData
        {
            selectedIndices = indices
        };

        // Aplicar directamente al customizer del preview
        previewCustomizer.ApplyCustomization(data);
    }

    public void NextOption(int partIndex)
    {
        if (partIndex >= currentSelections.Count)
        {
            Debug.LogWarning($"Índice de parte {partIndex} fuera de rango");
            return;
        }

        currentSelections[partIndex]++;
        if (currentSelections[partIndex] >= database.customizationParts[partIndex].skinOptionNames.Count)
        {
            currentSelections[partIndex] = 0;
        }
        UpdateUIForPart(partIndex);
        UpdatePreviewAppearance();
    }

    public void PreviousOption(int partIndex)
    {
        if (partIndex >= currentSelections.Count)
        {
            Debug.LogWarning($"Índice de parte {partIndex} fuera de rango");
            return;
        }

        currentSelections[partIndex]--;
        if (currentSelections[partIndex] < 0)
        {
            currentSelections[partIndex] = database.customizationParts[partIndex].skinOptionNames.Count - 1;
        }
        UpdateUIForPart(partIndex);
        UpdatePreviewAppearance();
    }

    private void UpdateUIForPart(int partIndex)
    {
        if (partIndex >= optionTexts.Count)
        {
            Debug.LogWarning($"Índice de UI {partIndex} fuera de rango");
            return;
        }

        var part = database.customizationParts[partIndex];
        optionTexts[partIndex].text = $"{part.partName}: {currentSelections[partIndex] + 1} / {part.skinOptionNames.Count}";
    }

    private void UpdateAllUI()
    {
        for (int i = 0; i < currentSelections.Count && i < optionTexts.Count; i++)
            UpdateUIForPart(i);
    }

    private void ValidateInput(string text)
    {
        confirmButton.interactable = !string.IsNullOrWhiteSpace(nameInputField.text);
    }

    private void OnConfirm()
    {
        string playerName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Nombre de jugador vacío");
            return;
        }

        PlayerAppearanceData data = new PlayerAppearanceData
        {
            selectedIndices = string.Join(",", currentSelections)
        };

        Debug.Log($"Enviando datos de personalización: {data.selectedIndices}");

        // Enviar datos al servidor
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.UpdatePlayerDetailsServerRpc(playerName, data);
        }
        else
        {
            Debug.LogError("LobbyManager.Instance es null");
        }

        // Cambiar paneles
        namePanel.SetActive(false);
        lobbyPanel.SetActive(true);

        // Limpiar preview
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    private void OnDestroy()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
    }
}