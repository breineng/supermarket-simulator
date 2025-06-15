using System.Collections.Generic;
using UnityEngine; // Для Debug

public class InventoryService : IInventoryService
{
    private Dictionary<string, int> _stockedItems;
    // Можно добавить IProductCatalogService для проверки существования ProductID, если нужно

    public InventoryService()
    {
        _stockedItems = new Dictionary<string, int>();
    }

    public int GetStockCount(string productID)
    {
        if (string.IsNullOrEmpty(productID)) return 0;
        _stockedItems.TryGetValue(productID, out int count);
        return count;
    }

    public bool AddToStock(string productID, int amount)
    {
        if (string.IsNullOrEmpty(productID) || amount <= 0)
        {
            Debug.LogWarning($"InventoryService: Invalid attempt to add to stock. ProductID: {productID}, Amount: {amount}");
            return false;
        }
        // TODO: Позже можно добавить проверку, существует ли такой ProductID через IProductCatalogService

        if (_stockedItems.ContainsKey(productID))
        {
            _stockedItems[productID] += amount;
        }
        else
        {
            _stockedItems[productID] = amount;
        }
        Debug.Log($"InventoryService: Added {amount} of {productID}. New count: {_stockedItems[productID]}");
        return true;
    }

    public bool RemoveFromStock(string productID, int amount)
    {
        if (string.IsNullOrEmpty(productID) || amount <= 0)
        {
            Debug.LogWarning($"InventoryService: Invalid attempt to remove from stock. ProductID: {productID}, Amount: {amount}");
            return false;
        }

        if (!_stockedItems.ContainsKey(productID) || _stockedItems[productID] < amount)
        {
            Debug.LogWarning($"InventoryService: Not enough {productID} in stock to remove {amount}. Current: {GetStockCount(productID)}");
            return false;
        }

        _stockedItems[productID] -= amount;
        Debug.Log($"InventoryService: Removed {amount} of {productID}. New count: {_stockedItems[productID]}");
        if (_stockedItems[productID] == 0)
        {
            // Опционально: удалить ключ, если количество 0, для чистоты словаря.
            // _stockedItems.Remove(productID); 
        }
        return true;
    }

    public Dictionary<string, int> GetAllStockedItems()
    {
        // Возвращаем копию, чтобы предотвратить внешнее изменение
        return new Dictionary<string, int>(_stockedItems);
    }
} 