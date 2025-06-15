using System;
using System.Collections.Generic;
using Core.Models;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Интерфейс сервиса управления лицензиями на товары
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>
        /// Событие при покупке новой лицензии
        /// </summary>
        event Action<ProductLicense> OnLicensePurchased;
        
        /// <summary>
        /// Событие при изменении доступности товара
        /// </summary>
        event Action<string> OnProductUnlocked;
        
        /// <summary>
        /// Получить все доступные лицензии
        /// </summary>
        List<ProductLicense> GetAllLicenses();
        
        /// <summary>
        /// Получить доступные для покупки лицензии
        /// </summary>
        List<ProductLicense> GetAvailableLicenses();
        
        /// <summary>
        /// Получить купленные лицензии
        /// </summary>
        List<ProductLicense> GetPurchasedLicenses();
        
        /// <summary>
        /// Проверить, куплена ли лицензия
        /// </summary>
        bool IsLicensePurchased(string licenseId);
        
        /// <summary>
        /// Проверить, разблокирован ли товар
        /// </summary>
        bool IsProductUnlocked(string productId);
        
        /// <summary>
        /// Купить лицензию
        /// </summary>
        bool PurchaseLicense(string licenseId);
        
        /// <summary>
        /// Получить лицензию по ID
        /// </summary>
        ProductLicense GetLicense(string licenseId);
        
        /// <summary>
        /// Получить все разблокированные товары
        /// </summary>
        List<string> GetUnlockedProductIds();
        
        /// <summary>
        /// Получить стоимость лицензии
        /// </summary>
        float GetLicensePrice(string licenseId);
        
        /// <summary>
        /// Инициализировать начальные лицензии
        /// </summary>
        void InitializeStarterLicenses();
        
        /// <summary>
        /// Сохранить состояние лицензий
        /// </summary>
        List<string> GetPurchasedLicenseIds();
        
        /// <summary>
        /// Восстановить состояние лицензий
        /// </summary>
        void RestorePurchasedLicenses(List<string> licenseIds);
    }
} 