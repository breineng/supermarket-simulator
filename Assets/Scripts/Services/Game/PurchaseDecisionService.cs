using UnityEngine;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Сервис принятия покупательских решений
    /// Реализует логику влияния цены на покупательское поведение
    /// </summary>
    public class PurchaseDecisionService : IPurchaseDecisionService
    {
        private readonly PurchaseDecisionConfig _config;
        
        public PurchaseDecisionService(PurchaseDecisionConfig config)
        {
            _config = config;
            
            if (_config == null)
            {
                Debug.LogWarning("PurchaseDecisionService: Config is null, using default values");
            }
        }
        
        public float CalculatePurchaseProbability(ProductConfig product, float customerMoney, IRetailPriceService retailPriceService)
        {
            if (product == null || retailPriceService == null)
                return 0f;
                
            float retailPrice = retailPriceService.GetRetailPrice(product.ProductID);
            float basePrice = product.BaseSalePrice;
            
            // Фактор доступности - может ли покупатель позволить себе товар
            float affordabilityFactor = CalculateAffordabilityFactor(retailPrice, customerMoney);
            
            // Фактор справедливости цены - насколько цена отличается от базовой
            float priceFairnessFactor = CalculatePriceFairnessFactor(retailPrice, basePrice);
            
            // Итоговая вероятность = базовая вероятность * факторы
            float baseProbability = _config?.basePurchaseProbability ?? 0.8f;
            float probability = baseProbability * affordabilityFactor * priceFairnessFactor;
            
            // Ограничиваем минимальным значением
            float minProbability = _config?.minPurchaseProbability ?? 0.1f;
            return Mathf.Max(probability, minProbability);
        }
        
        public bool ShouldCustomerTakeItem(ProductConfig product, float customerMoney, IRetailPriceService retailPriceService)
        {
            float probability = CalculatePurchaseProbability(product, customerMoney, retailPriceService);
            float randomValue = Random.Range(0f, 1f);
            
            bool decision = randomValue <= probability;
            
            bool enableLogging = _config?.enableDetailedLogging ?? true;
            if (enableLogging && retailPriceService != null)
            {
                float retailPrice = retailPriceService.GetRetailPrice(product.ProductID);
                Debug.Log($"Customer decision for {product.ProductName}: Price=${retailPrice:F2}, Probability={probability:F2}, Random={randomValue:F2}, Decision={decision}");
            }
            
            return decision;
        }
        
        /// <summary>
        /// Вычисляет фактор доступности товара на основе денег покупателя
        /// </summary>
        private float CalculateAffordabilityFactor(float price, float customerMoney)
        {
            if (customerMoney <= 0)
                return 0f;
                
            float threshold = _config?.affordabilityThreshold ?? 0.3f;
            float affordableAmount = customerMoney * threshold;
            
            if (price <= affordableAmount)
            {
                // Товар полностью доступен
                return 1f;
            }
            else if (price <= customerMoney)
            {
                // Товар доступен, но дорогой - снижаем вероятность покупки
                float excessFactor = (price - affordableAmount) / (customerMoney - affordableAmount);
                return Mathf.Lerp(1f, 0.2f, excessFactor);
            }
            else
            {
                // Товар слишком дорогой
                return 0f;
            }
        }
        
        /// <summary>
        /// Вычисляет фактор справедливости цены относительно базовой стоимости
        /// </summary>
        private float CalculatePriceFairnessFactor(float retailPrice, float basePrice)
        {
            if (basePrice <= 0)
                return 1f;
                
            float priceRatio = retailPrice / basePrice;
            
            if (priceRatio <= 1f)
            {
                // Цена равна или ниже базовой - максимальная привлекательность
                return 1f;
            }
            else 
            {
                float maxMultiplier = _config?.maxPriceMultiplier ?? 3.0f;
                if (priceRatio <= maxMultiplier)
                {
                    // Цена выше базовой, но в разумных пределах - плавно снижаем привлекательность
                    float excessFactor = (priceRatio - 1f) / (maxMultiplier - 1f);
                    return Mathf.Lerp(1f, 0.1f, excessFactor);
                }
                else
                {
                    // Цена чрезмерно высокая
                    return 0.05f;
                }
            }
        }
    }
} 