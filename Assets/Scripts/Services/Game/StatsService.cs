using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Supermarket.Services.Game
{
    public class StatsService : IStatsService
    {
        // Общая статистика
        private float _totalRevenue = 0f;
        private float _totalExpenses = 0f;
        private int _totalCustomersServed = 0;
        private int _totalItemsSold = 0;
        
        // Дневная статистика
        private float _dailyRevenue = 0f;
        private float _dailyExpenses = 0f;
        private int _dailyCustomersServed = 0;
        
        // Статистика по товарам
        private Dictionary<string, int> _productSalesCount = new Dictionary<string, int>();
        private Dictionary<string, float> _productSalesRevenue = new Dictionary<string, float>();
        
        public event Action OnStatsUpdated;
        
        public float GetTotalRevenue() => _totalRevenue;
        public float GetTotalExpenses() => _totalExpenses;
        public float GetProfit() => _totalRevenue - _totalExpenses;
        
        public int GetTotalCustomersServed() => _totalCustomersServed;
        public int GetCustomersToday() => _dailyCustomersServed;
        
        public float GetAverageTransactionValue()
        {
            if (_totalCustomersServed == 0) return 0f;
            return _totalRevenue / _totalCustomersServed;
        }
        
        public int GetTotalItemsSold() => _totalItemsSold;
        
        public string GetBestSellingProduct()
        {
            if (_productSalesCount.Count == 0) return "Нет данных";
            
            var bestProduct = _productSalesCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
            return bestProduct.Key ?? "Нет данных";
        }
        
        public void RecordSale(string productName, int quantity, float totalPrice)
        {
            _totalRevenue += totalPrice;
            _dailyRevenue += totalPrice;
            _totalItemsSold += quantity;
            
            // Обновляем статистику по товарам
            if (!_productSalesCount.ContainsKey(productName))
            {
                _productSalesCount[productName] = 0;
                _productSalesRevenue[productName] = 0f;
            }
            _productSalesCount[productName] += quantity;
            _productSalesRevenue[productName] += totalPrice;
            
            Debug.Log($"StatsService: Recorded sale of {quantity}x {productName} for ${totalPrice:F2}");
            
            OnStatsUpdated?.Invoke();
        }
        
        public void RecordPurchase(float amount)
        {
            _totalExpenses += amount;
            _dailyExpenses += amount;
            
            Debug.Log($"StatsService: Recorded purchase expense of ${amount:F2}");
            
            OnStatsUpdated?.Invoke();
        }
        
        public void RecordCustomerServed()
        {
            _totalCustomersServed++;
            _dailyCustomersServed++;
            
            Debug.Log($"StatsService: Customer served. Total: {_totalCustomersServed}, Today: {_dailyCustomersServed}");
            
            OnStatsUpdated?.Invoke();
        }
        
        public void RecordTransaction(float totalAmount, List<ProductConfig> products)
        {
            if (products == null || products.Count == 0) return;

            var productQuantities = products.GroupBy(p => p)
                                            .ToDictionary(g => g.Key, g => g.Count());

            foreach (var entry in productQuantities)
            {
                ProductConfig product = entry.Key;
                int quantity = entry.Value;
                float priceForGroup = product.BaseSalePrice * quantity;
                RecordSale(product.ProductName, quantity, priceForGroup);
            }

            RecordCustomerServed();
            // Note: We are not directly using totalAmount argument to avoid double-counting revenue.
            // RecordSale already sums it up. We could use it for verification.
            Debug.Log($"StatsService: Full transaction recorded. Verified total: ${totalAmount:F2}");
        }
        
        public void ResetDailyStats()
        {
            _dailyRevenue = 0f;
            _dailyExpenses = 0f;
            _dailyCustomersServed = 0;
            
            Debug.Log("StatsService: Daily stats reset");
            
            OnStatsUpdated?.Invoke();
        }
    }
} 