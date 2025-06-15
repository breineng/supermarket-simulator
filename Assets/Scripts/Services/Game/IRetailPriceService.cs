using System;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Интерфейс для управления розничными ценами товаров
    /// </summary>
    public interface IRetailPriceService
    {
        /// <summary>
        /// Событие при изменении цены товара
        /// </summary>
        event Action<string, float> OnPriceChanged;
        
        /// <summary>
        /// Получить розничную цену товара
        /// </summary>
        float GetRetailPrice(string productId);
        
        /// <summary>
        /// Установить розничную цену товара
        /// </summary>
        void SetRetailPrice(string productId, float price);
        
        /// <summary>
        /// Сбросить цену товара к базовой
        /// </summary>
        void ResetToBasePrice(string productId);
        
        /// <summary>
        /// Получить все измененные цены для сохранения
        /// </summary>
        System.Collections.Generic.Dictionary<string, float> GetCustomPrices();
        
        /// <summary>
        /// Восстановить цены из сохранения
        /// </summary>
        void RestorePrices(System.Collections.Generic.Dictionary<string, float> prices);
    }
} 