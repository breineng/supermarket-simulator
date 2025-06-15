using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Models
{
    /// <summary>
    /// Модель лицензии на группу товаров
    /// </summary>
    [CreateAssetMenu(fileName = "ProductLicense", menuName = "Supermarket/License System/Product License")]
    public class ProductLicense : ScriptableObject
    {
        [Header("License Information")]
        public string LicenseId;
        public string LicenseName;
        [TextArea(3, 5)]
        public string Description;
        public float Price;
        public bool IsStarterLicense = false;

        [Header("Products")]
        public List<string> ProductIds = new List<string>();

        // Конструктор для совместимости с кодом
        public ProductLicense()
        {
        }

        // Конструктор для создания в коде (оставляем для совместимости)
        public ProductLicense(string licenseId, string licenseName, string description, float price, List<string> productIds, bool isStarterLicense = false)
        {
            LicenseId = licenseId;
            LicenseName = licenseName;
            Description = description;
            Price = price;
            ProductIds = new List<string>(productIds);
            IsStarterLicense = isStarterLicense;
        }

        /// <summary>
        /// Проверяет, включен ли товар в эту лицензию
        /// </summary>
        public bool ContainsProduct(string productId)
        {
            return ProductIds.Contains(productId);
        }
        
        /// <summary>
        /// Возвращает количество товаров в лицензии
        /// </summary>
        public int GetProductCount()
        {
            return ProductIds?.Count ?? 0;
        }
    }
} 