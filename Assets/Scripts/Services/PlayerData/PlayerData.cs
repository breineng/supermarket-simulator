using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public float Money;
    
    // Character appearance data
    public string CharacterGender = "Male"; // Default gender
    public int ClothingIndex = 0; // Default clothing variant
    public Color ShirtColor = Color.white; // Default shirt color
    public Color PantsColor = Color.black; // Default pants color
    
    // Сюда можно добавить другие данные игрока:
    // например, текущий уровень магазина, разблокированные предметы, статистика и т.д.
    // public int StoreLevel;
    // public List<string> UnlockedItemIDs;

    public PlayerData()
    {
        Money = 1000; // Начальные деньги
        // StoreLevel = 1;
        // UnlockedItemIDs = new List<string>();
    }
} 