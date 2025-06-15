using UnityEngine;
using System.Collections.Generic;
using Supermarket.Data;

[System.Serializable]
public class CustomerData
{
    public string CustomerName;
    public float MoveSpeed = 3.5f;
    public float Patience = 100f; // Терпение покупателя (для очередей)
    public float Money;
    
    // Внешность и пол
    public CharacterAppearanceConfig.Gender Gender;
    public Color TopClothingColor;
    public Color BottomClothingColor;
    public Color ShoesColor;
    
    // Список желаемых товаров (что покупатель хочет купить)
    public List<ShoppingItem> ShoppingList;
    
    // Текущее состояние покупателя
    public CustomerState CurrentState = CustomerState.StreetWalking;
    
    public CustomerData(string name, float money)
    {
        CustomerName = name;
        Money = money;
        ShoppingList = new List<ShoppingItem>();
    }
}

[System.Serializable]
public class ShoppingItem
{
    public ProductConfig Product;
    public int DesiredQuantity;
    public int CollectedQuantity = 0;
    public bool UnavailableInStore = false; // Новое поле для отслеживания недоступности товара
    public float PurchasePrice = 0f; // Цена, по которой товар был взят с полки (фиксируется при взятии)
    
    public ShoppingItem(ProductConfig product, int quantity)
    {
        Product = product;
        DesiredQuantity = quantity;
        CollectedQuantity = 0;
        UnavailableInStore = false;
        PurchasePrice = 0f;
    }
    
    public bool IsComplete => CollectedQuantity >= DesiredQuantity;
    public bool CanContinueShopping => !IsComplete && !UnavailableInStore; // Можно ли продолжать искать этот товар
}

public enum CustomerState
{
    StreetWalking,      // Гуляет по улице по waypoints
    ConsideringStore,   // Думает, зайти ли в магазин
    Entering,           // Входит в магазин
    Shopping,           // Ищет товары
    GoingToShelf,       // Идет к полке
    TakingItem,         // Берет товар с полки
    GoingToCashier,     // Идет к кассе
    JoiningQueue,       // Встает в очередь
    WaitingInQueue,     // Ждет в очереди
    PlacingItemsOnBelt, // Выкладывает товары на ленту
    Paying,             // Оплачивает покупки
    Leaving             // Уходит из магазина
} 