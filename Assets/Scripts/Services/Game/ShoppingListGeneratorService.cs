using System.Collections.Generic;
using UnityEngine;
using BehaviourInject;
using Supermarket.Data;

namespace Supermarket.Services.Game
{
    public class ShoppingListGeneratorService : MonoBehaviour, IShoppingListGeneratorService
    {
        [Header("Shopping List Configuration")]
        [SerializeField] private int _minItems = 1;
        [SerializeField] private int _maxItems = 5;
        [SerializeField] private float _budgetFactor = 0.7f; // Клиенты тратят 70% от своих денег
        
        [Inject]
        public IProductCatalogService _productCatalogService;
        
        [Inject]
        public IRetailPriceService _retailPriceService;

        public List<ShoppingItem> GenerateShoppingList(float customerMoney)
        {
            return GenerateShoppingList(customerMoney, Random.Range(_minItems, _maxItems + 1));
        }

        public List<ShoppingItem> GenerateShoppingList(float customerMoney, int itemCount)
        {
            return GenerateShoppingListWithPreferences(customerMoney, CharacterAppearanceConfig.Gender.Male, itemCount, itemCount);
        }

        public List<ShoppingItem> GenerateShoppingListWithPreferences(
            float customerMoney, 
            CharacterAppearanceConfig.Gender gender, 
            int minItems = 1, 
            int maxItems = 5)
        {
            List<ShoppingItem> shoppingList = new List<ShoppingItem>();
            
            if (_productCatalogService == null || _retailPriceService == null)
            {
                Debug.LogWarning("ShoppingListGeneratorService: Required services not available");
                return shoppingList;
            }
            
            var allProducts = _productCatalogService.GetAllProductConfigs();
            if (allProducts == null || allProducts.Count == 0)
            {
                Debug.LogWarning("ShoppingListGeneratorService: No products available in catalog");
                return shoppingList;
            }
            
            float availableBudget = customerMoney * _budgetFactor;
            int targetItemCount = Random.Range(minItems, maxItems + 1);
            
            List<ProductConfig> shuffledProducts = new List<ProductConfig>(allProducts);
            ShuffleList(shuffledProducts);
            
            float totalCost = 0f;
            
            foreach (var product in shuffledProducts)
            {
                if (shoppingList.Count >= targetItemCount)
                    break;
                    
                float retailPrice = _retailPriceService.GetRetailPrice(product.ProductID);
                
                // Проверяем, может ли клиент позволить себе хотя бы одну единицу товара
                if (retailPrice > availableBudget - totalCost)
                    continue;
                
                // Определяем максимальное количество, которое клиент может купить
                int maxAffordable = Mathf.FloorToInt((availableBudget - totalCost) / retailPrice);
                
                if (maxAffordable <= 0)
                    continue;
                
                // Генерируем случайное количество от 1 до максимально доступного (но не больше 3)
                int desiredQuantity = Random.Range(1, Mathf.Min(maxAffordable + 1, 4));
                
                float itemTotalCost = retailPrice * desiredQuantity;
                
                // Добавляем товар в список
                ShoppingItem item = new ShoppingItem(product, desiredQuantity);
                shoppingList.Add(item);
                totalCost += itemTotalCost;
                
                Debug.Log($"ShoppingListGeneratorService: Added {product.ProductName} x{desiredQuantity} (${retailPrice:F2} each, total: ${itemTotalCost:F2})");
            }
            
            Debug.Log($"ShoppingListGeneratorService: Generated shopping list with {shoppingList.Count} items, total cost: ${totalCost:F2}, budget: ${availableBudget:F2}");
            
            return shoppingList;
        }
        
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
} 