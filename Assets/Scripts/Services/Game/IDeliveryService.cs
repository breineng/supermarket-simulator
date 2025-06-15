using System.Collections.Generic;
using UnityEngine;
using System;
using Supermarket.Data;

namespace Supermarket.Services.Game
{
    public interface IDeliveryService
    {
        /// <summary>
        /// Старый метод немедленной доставки (для обратной совместимости)
        /// </summary>
        void DeliverBoxes(Dictionary<ProductConfig, int> orderedProducts);
        
        /// <summary>
        /// Новый метод заказа с отложенной доставкой
        /// </summary>
        /// <param name="orderedProducts">Заказанные товары</param>
        /// <param name="deliveryTimeMinutes">Время доставки в минутах (игрового времени)</param>
        /// <returns>ID заказа</returns>
        string PlaceOrder(Dictionary<ProductConfig, int> orderedProducts);
        
        /// <summary>
        /// Отменить заказ (возможно с частичным возвратом средств)
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="refundPercentage">Процент возврата (0-1)</param>
        /// <returns>Сумма возврата</returns>
        float CancelOrder(string orderId, float refundPercentage = 0.5f);
        
        /// <summary>
        /// Получить все активные заказы
        /// </summary>
        List<OrderSaveData> GetActiveOrders();
        
        /// <summary>
        /// Получить заказ по ID
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <returns>Данные заказа или null если не найден</returns>
        OrderSaveData GetOrder(string orderId);
        
        /// <summary>
        /// Загружает активные заказы из сохранения
        /// </summary>
        /// <param name="orders">Список заказов для восстановления</param>
        void LoadActiveOrders(List<OrderSaveData> orders);
        
        /// <summary>
        /// События доставки
        /// </summary>
        event Action<OrderSaveData> OnOrderPlaced;
        event Action<OrderSaveData> OnOrderDelivered;
        event Action<OrderSaveData> OnOrderCancelled;
        event Action<OrderSaveData> OnDeliveryImminent; // За минуту до доставки
    }
} 