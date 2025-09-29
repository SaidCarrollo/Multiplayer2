// PlayerPanel.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Image readyCheckmark;

    public void SetPlayerName(string name)
    {
        playerNameText.text = name;
    }

    public void SetReady(bool isReady)
    {
        readyCheckmark.enabled = isReady;
    }

    public void ActivatePanel(string name)
    {
        gameObject.SetActive(true);
        SetPlayerName(name);
        SetReady(false);
    }

    public void DeactivatePanel()
    {
        gameObject.SetActive(false);
        SetPlayerName("");
        SetReady(false);
    }
}