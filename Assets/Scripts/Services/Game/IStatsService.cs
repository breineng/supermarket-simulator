using System;

namespace Supermarket.Services.Game
{
    public interface IStatsService
    {
        // Статистика по деньгам
        float GetTotalRevenue(); // Общий доход
        float GetTotalExpenses(); // Общие расходы
        float GetProfit(); // Прибыль (доход - расходы)
        
        // Статистика по покупателям
        int GetTotalCustomersServed(); // Всего обслужено покупателей
        int GetCustomersToday(); // Покупателей сегодня
        float GetAverageTransactionValue(); // Средний чек
        
        // Статистика по товарам
        int GetTotalItemsSold(); // Всего продано товаров
        string GetBestSellingProduct(); // Самый продаваемый товар
        
        // Методы для записи событий
        void RecordSale(string productName, int quantity, float totalPrice);
        void RecordPurchase(float amount); // Закупка товаров
        void RecordCustomerServed();
        void RecordTransaction(float totalAmount, System.Collections.Generic.List<ProductConfig> products);
        
        // События
        event Action OnStatsUpdated;
        
        // Сброс дневной статистики (можно вызывать в начале нового игрового дня)
        void ResetDailyStats();
    }
} 