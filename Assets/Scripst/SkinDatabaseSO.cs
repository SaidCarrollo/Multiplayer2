// CustomizationDatabaseSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Customization Database", menuName = "Customization/Database")]
public class CustomizationDatabaseSO : ScriptableObject
{
    public List<CustomizationPartSO> customizationParts;
}