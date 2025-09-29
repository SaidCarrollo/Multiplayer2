using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

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

        for (int i = 0; i < nextButtons.Count; i++)
        {
            int index = i;
            nextButtons[i].onClick.AddListener(() => NextOption(index));
            previousButtons[i].onClick.AddListener(() => PreviousOption(index));
        }
    }

    private void OnEnable()
    {
        SetupPreview();
        UpdateAllUI();
    }

    private void OnDisable()
    {
        CleanUpPreview();
    }

    private void SetupPreview()
    {
        if (playerPrefab == null || previewParent == null) return;

        GameObject previewInstance = Instantiate(playerPrefab, previewParent);


        if (previewInstance.TryGetComponent<NetworkObject>(out var netObj))
        {
            Destroy(netObj);
        }

        // El resto del código para ajustar la capa y obtener el customizer funciona igual.
        foreach (Transform child in previewInstance.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = previewParent.gameObject.layer;
        }

        previewCustomizer = previewInstance.GetComponent<CharacterCustomizer>();
        UpdatePreviewAppearance();
    }


    private void CleanUpPreview()
    {
        if (previewCustomizer != null)
        {
            Destroy(previewCustomizer.gameObject);
            previewCustomizer = null;
        }
    }

    private void UpdatePreviewAppearance()
    {
        if (previewCustomizer == null) return;

        PlayerAppearanceData data = new PlayerAppearanceData
        {
            selectedIndices = string.Join(",", currentSelections)
        };

        previewCustomizer.ApplyCustomization(data);
    }

    public void NextOption(int partIndex)
    {
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
        var part = database.customizationParts[partIndex];
        optionTexts[partIndex].text = $"{part.partName}: {currentSelections[partIndex] + 1} / {part.skinOptionNames.Count}";
    }

    private void UpdateAllUI()
    {
        for (int i = 0; i < currentSelections.Count; i++) UpdateUIForPart(i);
    }

    private void ValidateInput(string text)
    {
        confirmButton.interactable = !string.IsNullOrWhiteSpace(nameInputField.text);
    }

    private void OnConfirm()
    {
        string playerName = nameInputField.text;
        PlayerAppearanceData data = new PlayerAppearanceData
        {
            selectedIndices = string.Join(",", currentSelections)
        };

        LobbyManager.Instance.UpdatePlayerDetailsServerRpc(playerName, data);

        namePanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }
}