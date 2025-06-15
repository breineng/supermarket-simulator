using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameConfiguration", menuName = "Supermarket/Game Configuration/Main Game Configuration")]
public class GameConfiguration : ScriptableObject
{
    [Header("Product Catalog")]
    public List<ProductConfig> AllProducts;

    // Сюда можно будет добавить другие списки конфигураций:
    // public List<ShelfConfig> AllShelves;
    // public List<CustomerConfig> CustomerTypes;
    // И т.д.

    // Можно добавить глобальные игровые параметры, например:
    // public float InitialPlayerMoney;
    // public int MaxCustomersAtOnce;
} 