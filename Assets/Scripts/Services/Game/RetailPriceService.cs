using System;
using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Сервис управления розничными ценами товаров
    /// </summary>
    public class RetailPriceService : IRetailPriceService
    {
        private readonly IProductCatalogService _productCatalog;
        private readonly Dictionary<string, float> _customPrices = new Dictionary<string, float>();
        
        public event Action<string, float> OnPriceChanged;
        
        public RetailPriceService(IProductCatalogService productCatalog)
        {
            _productCatalog = productCatalog;
        }
        
        public float GetRetailPrice(string productId)
        {
            // Если есть кастомная цена, возвращаем её
            if (_customPrices.ContainsKey(productId))
            {
                return _customPrices[productId];
            }
            
            // Иначе возвращаем базовую цену из конфига
            var product = _productCatalog.GetProductConfigByID(productId);
            return product != null ? product.BaseSalePrice : 0f;
        }
        
        public void SetRetailPrice(string productId, float price)
        {
            if (price <= 0)
            {
                Debug.LogWarning($"RetailPriceService: Cannot set price to {price} for product {productId}");
                return;
            }
            
            var product = _productCatalog.GetProductConfigByID(productId);
            if (product == null)
            {
                Debug.LogError($"RetailPriceService: Product {productId} not found");
                return;
            }
            
            // Если цена равна базовой, удаляем из кастомных
            if (Mathf.Approximately(price, product.BaseSalePrice))
            {
                _customPrices.Remove(productId);
            }
            else
            {
                _customPrices[productId] = price;
            }
            
            OnPriceChanged?.Invoke(productId, price);
            Debug.Log($"RetailPriceService: Set price for {productId} to ${price:F2}");
        }
        
        public void ResetToBasePrice(string productId)
        {
            var product = _productCatalog.GetProductConfigByID(productId);
            if (product == null)
            {
                Debug.LogError($"RetailPriceService: Product {productId} not found");
                return;
            }
            
            _customPrices.Remove(productId);
            OnPriceChanged?.Invoke(productId, product.BaseSalePrice);
            Debug.Log($"RetailPriceService: Reset {productId} to base price ${product.BaseSalePrice:F2}");
        }
        
        public Dictionary<string, float> GetCustomPrices()
        {
            return new Dictionary<string, float>(_customPrices);
        }
        
        public void RestorePrices(Dictionary<string, float> prices)
        {
            _customPrices.Clear();
            
            if (prices != null)
            {
                foreach (var kvp in prices)
                {
                    _customPrices[kvp.Key] = kvp.Value;
                    Debug.Log($"RetailPriceService: Restored price for {kvp.Key}: ${kvp.Value:F2}");
                }
            }
            
            Debug.Log($"RetailPriceService: Restored {_customPrices.Count} custom prices");
        }
    }
} 