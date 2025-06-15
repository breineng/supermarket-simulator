using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Data
{
    [Serializable]
    public class SaveGameData
    {
        public string Version = "1.0.0";
        public DateTime SaveDate;
        public float PlayTime;
        
        // Скриншот сохранения (путь к файлу относительно папки сохранений)
        public string ScreenshotPath;
        
        // Данные игрока
        public PlayerSaveData PlayerData;
        
        // Состояние магазина
        public StoreSaveData StoreData;
        
        // Активные заказы и доставки
        public List<OrderSaveData> ActiveOrders;
        
        // Статистика
        public StatsSaveData Statistics;
        
        // Настройки игры
        public GameSettingsSaveData Settings;
        
        // Лицензии на товары (для будущей системы)
        public List<string> UnlockedLicenses;
    }
    
    [Serializable]
    public class PlayerSaveData
    {
        public float Money;
        public Vector3 Position;
        public Vector3 Rotation;
        public Dictionary<string, float> CustomPrices; // Кастомные цены на товары
        
        // Коробка в руках игрока
        public PlayerHeldBoxData HeldBox;
        
        // Для будущей системы внешнего вида
        public CharacterAppearanceData Appearance;
    }
    
    [Serializable]
    public class CharacterAppearanceData
    {
        public int Gender; // 0 - мужской, 1 - женский
        public int ClothingIndex;
        public Color ShirtColor;
        public Color PantsColor;
    }
    
    [Serializable]
    public class PlayerHeldBoxData
    {
        public string ProductType; // ID товара в коробке (может быть null для пустой коробки)
        public int ItemCount; // Количество товаров в коробке
        public bool IsOpen; // Открыта ли коробка
    }
    
    [Serializable]
    public class StoreSaveData
    {
        public string SupermarketName = "СУПЕРМАРКЕТ"; // Название супермаркета по умолчанию
        public List<PlacedObjectData> PlacedObjects; // Размещенная мебель
        public List<ShelfSaveData> Shelves; // Состояние полок
        public List<BoxSaveData> Boxes; // Коробки в магазине
        public List<CustomerSaveData> Customers; // Активные покупатели в магазине
    }
    
    [Serializable]
    public class PlacedObjectData
    {
        public string PrefabName;
        public Vector3 Position;
        public Quaternion Rotation;
        public string ObjectType; // "Shelf", "CashDesk", etc.
    }
    
    [Serializable]
    public class ShelfSaveData
    {
        public int ShelfId; // Индекс полки в менеджере
        public string ProductType; // ID продукта
        public int ItemCount; // Количество товаров
        public List<ShelfLevelData> Levels; // Пока не используется
    }
    
    [Serializable]
    public class ShelfLevelData
    {
        public int Level;
        public string ProductType; // ID продукта на этом уровне
        public int ItemCount;
    }
    
    [Serializable]
    public class BoxSaveData
    {
        public string ProductType;
        public int ItemCount;
        public Vector3 Position;
        public bool IsOpen;
    }
    
    [Serializable]
    public class OrderSaveData
    {
        public string OrderId;
        public DateTime OrderTime;
        public float DeliveryTime; // Оставшееся время до доставки
        public float TotalCost;
        public List<OrderItemData> Items;
        public OrderStatus Status;
    }
    
    [Serializable]
    public enum OrderStatus
    {
        Pending,
        InTransit,
        Delivered,
        Cancelled
    }
    
    [Serializable]
    public class OrderItemData
    {
        public string ProductType;
        public int Quantity;
        public float PricePerUnit;
    }
    
    [Serializable]
    public class StatsSaveData
    {
        public float TotalRevenue;
        public float TotalExpenses;
        public int TotalCustomersServed;
        public int TotalItemsSold;
        public Dictionary<string, int> ProductSales; // Продажи по типам товаров
        public int CurrentDay;
        public float CurrentDayRevenue;
        public int CurrentDayCustomers;
    }
    
    [Serializable]
    public class GameSettingsSaveData
    {
        public float MasterVolume;
        public float MusicVolume;
        public float SFXVolume;
        public float MouseSensitivity;
        public int GraphicsQuality;
        public bool AutoSaveEnabled;
        public float AutoSaveInterval;
    }

    // Данные покупателя
    [System.Serializable]
    public class CustomerSaveData
    {
        // Базовая информация
        public string CustomerName;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Money;
        
        // Внешность
        public int Gender; // CharacterAppearanceConfig.Gender как int
        public float[] TopClothingColor = new float[4]; // RGBA
        public float[] BottomClothingColor = new float[4]; // RGBA  
        public float[] ShoesColor = new float[4]; // RGBA
        
        // Состояние и движение
        public int CurrentState; // CustomerState как int
        public float StateTimer;
        public Vector3 TargetPosition;
        public bool HasTarget;
        
        // Список покупок и прогресс
        public List<ShoppingItemSaveData> ShoppingList;
        
        // Состояние в очереди
        public bool IsInQueue;
        public int QueuePosition;
        public string CashDeskId; // ID кассы, если стоит в очереди
        public Vector3 CashDeskPosition; // Позиция кассы для поиска ближайшей
        public Vector3 QueueWorldPosition;
        
        // Флаги анимации и процессов
        public bool PickupAnimationPlayed;
        public bool PayAnimationPlayed;
        public bool PaymentProcessed;
        
        // Текущая цель (полка/касса)
        public string TargetShelfId; // Имя GameObject полки или null
        public int CurrentShoppingItemIndex; // Индекс в списке покупок или -1
        
        // Данные для состояния выхода
        public Vector3 PersonalExitPosition;
        public bool HasPersonalExitPosition;
        
        // Настройки навигации
        public int AvoidancePriority = 50;
        
        // Данные для уличных прогулок
        public string CurrentWaypointId; // ID текущего waypoint или null
        public float WaypointWaitTimer; // Время ожидания на waypoint
        public bool IsWaitingAtWaypoint; // Ждет ли клиент на waypoint
    }

    // Данные элемента списка покупок
    [System.Serializable]
    public class ShoppingItemSaveData
    {
        public string ProductId; // ID продукта
        public int DesiredQuantity;
        public int CollectedQuantity;
    }
} 