// NameSelector.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private List<int> currentSelections;

    void Start()
    {
        if (database == null)
        {
            Debug.LogError("¡CONFIGURACIÓN CRÍTICA FALTANTE! El campo 'Database' del NameSelector no tiene ningún CustomizationDatabaseSO asignado. Arrástralo en el Inspector.", this.gameObject);
            confirmButton.interactable = false; // Desactivar para prevenir errores
            return;
        }
        if (database.customizationParts.Count == 0)
        {
            Debug.LogError("¡CONFIGURACIÓN CRÍTICA FALTANTE! El asset 'CustomizationDatabaseSO' asignado está vacío. Ve al asset y añade las partes del cuerpo (Body, Eyes, etc.) a la lista 'Customization Parts'.", this.gameObject);
            confirmButton.interactable = false; // Desactivar para prevenir errores
            return;
        }

        namePanel.SetActive(true);
        lobbyPanel.SetActive(false);

        currentSelections = new List<int>();
        for (int i = 0; i < database.customizationParts.Count; i++)
        {
            currentSelections.Add(0);
            int index = i;
            nextButtons[i].onClick.AddListener(() => NextOption(index));
            previousButtons[i].onClick.AddListener(() => PreviousOption(index));
        }

        confirmButton.onClick.AddListener(OnConfirm);
        nameInputField.onValueChanged.AddListener(ValidateInput);

        UpdateAllUI();
        ValidateInput("");
    }

    public void NextOption(int partIndex)
    {
        currentSelections[partIndex]++;
        if (currentSelections[partIndex] >= database.customizationParts[partIndex].skinOptionNames.Count)
        {
            currentSelections[partIndex] = 0;
        }
        UpdateUIForPart(partIndex);
    }

    public void PreviousOption(int partIndex)
    {
        currentSelections[partIndex]--;
        if (currentSelections[partIndex] < 0)
        {
            currentSelections[partIndex] = database.customizationParts[partIndex].skinOptionNames.Count - 1;
        }
        UpdateUIForPart(partIndex);
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