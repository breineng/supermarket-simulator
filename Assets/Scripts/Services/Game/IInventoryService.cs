using System.Collections.Generic;

public interface IInventoryService
{
    int GetStockCount(string productID);
    bool AddToStock(string productID, int amount);
    bool RemoveFromStock(string productID, int amount);
    Dictionary<string, int> GetAllStockedItems(); // Для отладки или общего обзора
} 