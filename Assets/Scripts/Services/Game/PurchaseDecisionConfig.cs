using UnityEngine;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Конфигурация для настройки параметров покупательских решений
    /// </summary>
    [CreateAssetMenu(fileName = "PurchaseDecisionConfig", menuName = "Supermarket/Game Configuration/Purchase Decision Config")]
    public class PurchaseDecisionConfig : ScriptableObject
    {
        [Header("Price Sensitivity Settings")]
        [Tooltip("Базовая вероятность покупки при справедливой цене (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float basePurchaseProbability = 0.8f;
        
        [Tooltip("Максимальное превышение базовой цены для расчета (например, 3.0 = до 300% от базовой цены)")]
        [Range(1f, 10f)]
        public float maxPriceMultiplier = 3.0f;
        
        [Tooltip("Минимальная вероятность покупки даже при очень высокой цене (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float minPurchaseProbability = 0.1f;
        
        [Tooltip("Порог доступности - какую часть денег покупатель готов потратить на один товар (0.0 - 1.0)")]
        [Range(0f, 1f)]
        public float affordabilityThreshold = 0.3f;
        
        [Header("Debug Settings")]
        [Tooltip("Включить детальное логирование решений покупателей")]
        public bool enableDetailedLogging = true;
        
        private void OnValidate()
        {
            // Проверяем корректность значений
            basePurchaseProbability = Mathf.Clamp01(basePurchaseProbability);
            maxPriceMultiplier = Mathf.Max(1f, maxPriceMultiplier);
            minPurchaseProbability = Mathf.Clamp01(minPurchaseProbability);
            affordabilityThreshold = Mathf.Clamp01(affordabilityThreshold);
            
            // Минимальная вероятность не должна быть больше базовой
            if (minPurchaseProbability > basePurchaseProbability)
            {
                minPurchaseProbability = basePurchaseProbability;
            }
        }
    }
} 