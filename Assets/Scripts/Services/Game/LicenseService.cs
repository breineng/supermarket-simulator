using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Models;
using Supermarket.Services.PlayerData;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Сервис управления лицензиями на товары
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly IPlayerDataService _playerDataService;
        private readonly IGameConfigService _gameConfigService;
        private readonly List<ProductLicense> _allLicenses;
        private readonly HashSet<string> _purchasedLicenseIds;
        
        public event Action<ProductLicense> OnLicensePurchased;
        public event Action<string> OnProductUnlocked;
        
        public LicenseService(IPlayerDataService playerDataService, IGameConfigService gameConfigService)
        {
            _playerDataService = playerDataService;
            _gameConfigService = gameConfigService;
            _allLicenses = new List<ProductLicense>();
            _purchasedLicenseIds = new HashSet<string>();
            
            LoadLicensesFromConfig();
            InitializeStarterLicenses();
        }
        
        /// <summary>
        /// Загрузка лицензий из конфигурации
        /// </summary>
        private void LoadLicensesFromConfig()
        {
            var configLicenses = _gameConfigService.GetAllLicenses();
            _allLicenses.Clear();
            _allLicenses.AddRange(configLicenses);
            
            Debug.Log($"LicenseService: Loaded {_allLicenses.Count} licenses from configuration");
            
            // Если нет лицензий в конфигурации, создаем базовую стартовую лицензию
            if (_allLicenses.Count == 0)
            {
                Debug.LogWarning("LicenseService: No licenses found in configuration, creating default starter license");
                CreateDefaultStarterLicense();
            }
        }
        
        /// <summary>
        /// Создание базовой стартовой лицензии (fallback)
        /// </summary>
        private void CreateDefaultStarterLicense()
        {
            // Получаем все товары, которые можно заказать и не являются мебелью
            var orderableProducts = _gameConfigService.GetAllProducts()
                .Where(p => p.CanBeOrdered && p.ObjectCategory == PlaceableObjectType.Goods)
                .Select(p => p.ProductID)
                .ToList();
                
            if (orderableProducts.Count > 0)
            {
                var defaultLicense = new ProductLicense(
                    "default_starter_pack",
                    "Стартовый набор",
                    "Базовые товары для начала бизнеса",
                    0f,
                    orderableProducts,
                    true
                );
                
                _allLicenses.Add(defaultLicense);
                Debug.Log($"LicenseService: Created default starter license with {orderableProducts.Count} products");
            }
        }
        
        public void InitializeStarterLicenses()
        {
            // Автоматически разблокируем стартовые лицензии
            foreach (var license in _allLicenses.Where(l => l.IsStarterLicense))
            {
                _purchasedLicenseIds.Add(license.LicenseId);
                Debug.Log($"LicenseService: Starter license '{license.LicenseName}' unlocked");
                
                // Уведомляем о разблокированных товарах
                foreach (var productId in license.ProductIds)
                {
                    OnProductUnlocked?.Invoke(productId);
                }
            }
        }
        
        public List<ProductLicense> GetAllLicenses()
        {
            return new List<ProductLicense>(_allLicenses);
        }
        
        public List<ProductLicense> GetAvailableLicenses()
        {
            return _allLicenses.Where(l => !_purchasedLicenseIds.Contains(l.LicenseId)).ToList();
        }
        
        public List<ProductLicense> GetPurchasedLicenses()
        {
            return _allLicenses.Where(l => _purchasedLicenseIds.Contains(l.LicenseId)).ToList();
        }
        
        public bool IsLicensePurchased(string licenseId)
        {
            return _purchasedLicenseIds.Contains(licenseId);
        }
        
        public bool IsProductUnlocked(string productId)
        {
            // Проверяем, есть ли товар в какой-либо купленной лицензии
            foreach (var licenseId in _purchasedLicenseIds)
            {
                var license = GetLicense(licenseId);
                if (license != null && license.ContainsProduct(productId))
                {
                    return true;
                }
            }
            return false;
        }
        
        public bool PurchaseLicense(string licenseId)
        {
            // Проверяем, не куплена ли уже
            if (IsLicensePurchased(licenseId))
            {
                Debug.LogWarning($"LicenseService: License '{licenseId}' already purchased");
                return false;
            }
            
            var license = GetLicense(licenseId);
            if (license == null)
            {
                Debug.LogError($"LicenseService: License '{licenseId}' not found");
                return false;
            }
            
            // Проверяем, хватает ли денег
            if (_playerDataService.GetMoney() < license.Price)
            {
                Debug.Log($"LicenseService: Not enough money to purchase '{license.LicenseName}'. Need: ${license.Price}, Have: ${_playerDataService.GetMoney()}");
                return false;
            }
            
            // Списываем деньги
            _playerDataService.AdjustMoney(-license.Price);
            
            // Добавляем лицензию
            _purchasedLicenseIds.Add(licenseId);
            
            // Уведомляем о покупке
            OnLicensePurchased?.Invoke(license);
            
            // Уведомляем о разблокированных товарах
            foreach (var productId in license.ProductIds)
            {
                OnProductUnlocked?.Invoke(productId);
            }
            
            Debug.Log($"LicenseService: Successfully purchased license '{license.LicenseName}' for ${license.Price}");
            return true;
        }
        
        public ProductLicense GetLicense(string licenseId)
        {
            return _allLicenses.FirstOrDefault(l => l.LicenseId == licenseId);
        }
        
        public List<string> GetUnlockedProductIds()
        {
            var unlockedProducts = new HashSet<string>();
            
            foreach (var licenseId in _purchasedLicenseIds)
            {
                var license = GetLicense(licenseId);
                if (license != null)
                {
                    foreach (var productId in license.ProductIds)
                    {
                        unlockedProducts.Add(productId);
                    }
                }
            }
            
            return unlockedProducts.ToList();
        }
        
        public float GetLicensePrice(string licenseId)
        {
            var license = GetLicense(licenseId);
            return license?.Price ?? 0f;
        }
        
        public List<string> GetPurchasedLicenseIds()
        {
            return _purchasedLicenseIds.ToList();
        }
        
        public void RestorePurchasedLicenses(List<string> licenseIds)
        {
            _purchasedLicenseIds.Clear();
            
            if (licenseIds != null)
            {
                foreach (var licenseId in licenseIds)
                {
                    if (_allLicenses.Any(l => l.LicenseId == licenseId))
                    {
                        _purchasedLicenseIds.Add(licenseId);
                        Debug.Log($"LicenseService: Restored license '{licenseId}'");
                    }
                    else
                    {
                        Debug.LogWarning($"LicenseService: Unknown license ID '{licenseId}' in save data");
                    }
                }
            }
            
            // Всегда восстанавливаем стартовые лицензии
            InitializeStarterLicenses();
            
            Debug.Log($"LicenseService: Restored {_purchasedLicenseIds.Count} licenses");
        }
    }
} 