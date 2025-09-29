// CustomizationPartSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Customization Part", menuName = "Customization/Part")]
public class CustomizationPartSO : ScriptableObject
{
    public string partName; // Ejemplo: "Body", "Eyes", "Gloves"

    public List<string> skinOptionNames; 
}