using System;
using UnityEngine;
using Supermarket.Data;
using Newtonsoft.Json.Linq;

namespace Supermarket.Services.PlayerData
{
    public static class SaveGameMigration
    {
        // Текущая версия сохранений
        public const string CURRENT_VERSION = "1.0.0";
        
        // Миграция сохранения до актуальной версии
        public static SaveGameData MigrateSaveData(string jsonData)
        {
            try
            {
                JObject saveObject = JObject.Parse(jsonData);
                string version = saveObject["Version"]?.ToString() ?? "0.0.0";
                
                // Применяем миграции последовательно
                if (CompareVersions(version, "1.0.0") < 0)
                {
                    saveObject = MigrateToV1_0_0(saveObject);
                }
                
                // Добавляем будущие миграции здесь
                // if (CompareVersions(version, "1.1.0") < 0)
                // {
                //     saveObject = MigrateToV1_1_0(saveObject);
                // }
                
                // Десериализуем обновленный объект
                return saveObject.ToObject<SaveGameData>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to migrate save data: {e.Message}");
                return null;
            }
        }
        
        // Миграция к версии 1.0.0 (базовая версия)
        private static JObject MigrateToV1_0_0(JObject saveObject)
        {
            // Добавляем отсутствующие поля
            if (saveObject["Version"] == null)
            {
                saveObject["Version"] = "1.0.0";
            }
            
            // Добавляем поле скриншота если отсутствует
            if (saveObject["ScreenshotPath"] == null)
            {
                saveObject["ScreenshotPath"] = null;
            }
            
            // Добавляем структуру данных игрока, если отсутствует
            if (saveObject["PlayerData"] == null)
            {
                saveObject["PlayerData"] = new JObject
                {
                    ["Money"] = 1000f,
                    ["Position"] = JObject.FromObject(Vector3.zero),
                    ["Rotation"] = JObject.FromObject(Vector3.zero),
                    ["CustomPrices"] = new JObject()
                };
            }
            
            // Добавляем структуру магазина
            if (saveObject["StoreData"] == null)
            {
                saveObject["StoreData"] = new JObject
                {
                    ["PlacedObjects"] = new JArray(),
                    ["Shelves"] = new JArray(),
                    ["Boxes"] = new JArray()
                };
            }
            
            // Добавляем статистику
            if (saveObject["Statistics"] == null)
            {
                saveObject["Statistics"] = new JObject
                {
                    ["TotalRevenue"] = 0f,
                    ["TotalExpenses"] = 0f,
                    ["TotalCustomersServed"] = 0,
                    ["TotalItemsSold"] = 0,
                    ["ProductSales"] = new JObject(),
                    ["CurrentDay"] = 1,
                    ["CurrentDayRevenue"] = 0f,
                    ["CurrentDayCustomers"] = 0
                };
            }
            
            // Добавляем настройки
            if (saveObject["Settings"] == null)
            {
                saveObject["Settings"] = new JObject
                {
                    ["MasterVolume"] = 1.0f,
                    ["MusicVolume"] = 0.7f,
                    ["SFXVolume"] = 1.0f,
                    ["MouseSensitivity"] = 1.0f,
                    ["GraphicsQuality"] = 2,
                    ["AutoSaveEnabled"] = true,
                    ["AutoSaveInterval"] = 300f // 5 минут
                };
            }
            
            // Добавляем пустые списки
            if (saveObject["ActiveOrders"] == null)
            {
                saveObject["ActiveOrders"] = new JArray();
            }
            
            if (saveObject["UnlockedLicenses"] == null)
            {
                saveObject["UnlockedLicenses"] = new JArray("basic_products"); // Базовая лицензия
            }
            
            Debug.Log("Migrated save to version 1.0.0");
            return saveObject;
        }
        
        // Сравнение версий: -1 если v1 < v2, 0 если равны, 1 если v1 > v2
        private static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');
            
            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                int num1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int num2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;
                
                if (num1 < num2) return -1;
                if (num1 > num2) return 1;
            }
            
            return 0;
        }
        
        // Проверка совместимости версии
        public static bool IsVersionCompatible(string version)
        {
            // Определяем минимальную поддерживаемую версию
            const string MIN_SUPPORTED_VERSION = "0.9.0";
            
            return CompareVersions(version, MIN_SUPPORTED_VERSION) >= 0;
        }
    }
} 