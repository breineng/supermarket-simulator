using UnityEngine;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Интерфейс для сервиса принятия покупательских решений
    /// Определяет вероятность покупки товара на основе его цены
    /// </summary>
    public interface IPurchaseDecisionService
    {
        /// <summary>
        /// Вычисляет вероятность покупки товара покупателем на основе цены и других факторов
        /// </summary>
        /// <param name="product">Товар для оценки</param>
        /// <param name="customerMoney">Деньги у покупателя</param>
        /// <param name="retailPriceService">Сервис розничных цен</param>
        /// <returns>Вероятность покупки от 0.0 до 1.0</returns>
        float CalculatePurchaseProbability(ProductConfig product, float customerMoney, IRetailPriceService retailPriceService);
        
        /// <summary>
        /// Проверяет, должен ли покупатель взять товар с полки
        /// </summary>
        /// <param name="product">Товар для проверки</param>
        /// <param name="customerMoney">Деньги у покупателя</param>
        /// <param name="retailPriceService">Сервис розничных цен</param>
        /// <returns>true если покупатель решает взять товар, false если нет</returns>
        bool ShouldCustomerTakeItem(ProductConfig product, float customerMoney, IRetailPriceService retailPriceService);
    }
} 