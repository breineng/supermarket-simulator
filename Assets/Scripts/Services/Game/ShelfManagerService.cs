using System.Collections.Generic;
using UnityEngine;
using Supermarket.Data;
using Supermarket.Interactables;
using System.Collections;

namespace Supermarket.Services.Game
{
    public class ShelfManagerService : IShelfManagerService
    {
        private readonly IProductCatalogService _productCatalogService;
        
        // Список всех активных полок на сцене
        private readonly List<ShelfController> _activeShelves = new List<ShelfController>();
        
        // Список всех активных многоуровневых полок на сцене
        private readonly List<MultiLevelShelfController> _activeMultiLevelShelves = new List<MultiLevelShelfController>();
        
        // Данные для отложенного восстановления полок
        private List<ShelfSaveData> _pendingShelfData = null;
        
        // Конструктор для внедрения зависимостей (POCO паттерн)
        public ShelfManagerService(IProductCatalogService productCatalogService)
        {
            _productCatalogService = productCatalogService;
            Debug.Log("ShelfManagerService: Created as POCO with ProductCatalogService dependency");
        }
        
        public void RegisterShelf(ShelfController shelf)
        {
            if (shelf == null)
            {
                Debug.LogWarning("ShelfManagerService: Attempted to register null shelf");
                return;
            }
            
            if (!_activeShelves.Contains(shelf))
            {
                _activeShelves.Add(shelf);
                Debug.Log($"ShelfManagerService: Registered shelf '{shelf.gameObject.name}' (Index: {_activeShelves.Count - 1}). Total active shelves: {_activeShelves.Count}");
                
                // Если у нас есть отложенные данные для восстановления, пробуем их применить
                if (_pendingShelfData != null)
                {
                    Debug.Log($"ShelfManagerService: Has pending shelf data ({_pendingShelfData.Count} shelves), attempting delayed restoration after registration");
                    TryRestorePendingShelves();
                }
                else
                {
                    Debug.Log("ShelfManagerService: No pending shelf data, registration complete");
                }
            }
            else
            {
                Debug.LogWarning($"ShelfManagerService: Shelf '{shelf.gameObject.name}' already registered");
            }
        }
        
        public void UnregisterShelf(ShelfController shelf)
        {
            if (shelf == null) return;
            
            if (_activeShelves.Remove(shelf))
            {
                Debug.Log($"ShelfManagerService: Unregistered shelf '{shelf.gameObject.name}'. Total active shelves: {_activeShelves.Count}");
                
                // Если список полок пуст, очищаем также отложенные данные
                if (_activeShelves.Count == 0)
                {
                    Debug.Log("ShelfManagerService: All shelves unregistered, clearing pending data");
                    _pendingShelfData = null;
                }
            }
        }
        
        public void RegisterMultiLevelShelf(MultiLevelShelfController multiShelf)
        {
            if (multiShelf == null)
            {
                Debug.LogWarning("ShelfManagerService: Attempted to register null multi-level shelf");
                return;
            }
            
            if (!_activeMultiLevelShelves.Contains(multiShelf))
            {
                _activeMultiLevelShelves.Add(multiShelf);
                Debug.Log($"ShelfManagerService: Registered multi-level shelf '{multiShelf.gameObject.name}' (Index: {_activeMultiLevelShelves.Count - 1}). Total active multi-level shelves: {_activeMultiLevelShelves.Count}");
                
                // Если у нас есть отложенные данные для восстановления, пробуем их применить
                if (_pendingShelfData != null)
                {
                    Debug.Log($"ShelfManagerService: Has pending shelf data ({_pendingShelfData.Count} shelves), attempting delayed restoration after multi-level registration");
                    TryRestorePendingShelves();
                }
            }
            else
            {
                Debug.LogWarning($"ShelfManagerService: Multi-level shelf '{multiShelf.gameObject.name}' already registered");
            }
        }
        
        public void UnregisterMultiLevelShelf(MultiLevelShelfController multiShelf)
        {
            if (multiShelf == null) return;
            
            if (_activeMultiLevelShelves.Remove(multiShelf))
            {
                Debug.Log($"ShelfManagerService: Unregistered multi-level shelf '{multiShelf.gameObject.name}'. Total active multi-level shelves: {_activeMultiLevelShelves.Count}");
                
                // Если все списки пусты, очищаем отложенные данные
                if (_activeShelves.Count == 0 && _activeMultiLevelShelves.Count == 0)
                {
                    Debug.Log("ShelfManagerService: All shelves unregistered, clearing pending data");
                    _pendingShelfData = null;
                }
            }
        }
        
        public List<ShelfSaveData> GetShelvesSaveData()
        {
            List<ShelfSaveData> shelvesData = new List<ShelfSaveData>();
            
            // Сохраняем обычные полки
            for (int i = 0; i < _activeShelves.Count; i++)
            {
                var shelf = _activeShelves[i];
                if (shelf == null) continue;
                
                // Создаем данные полки
                ShelfSaveData saveData = new ShelfSaveData
                {
                    ShelfId = i, // Используем индекс как ID полки
                    ProductType = shelf.acceptedProduct?.ProductID ?? "",
                    ItemCount = shelf.GetCurrentItemCount(),
                    Levels = new List<ShelfLevelData>() // Пустой для обычных полок
                };
                
                shelvesData.Add(saveData);
                string productName = shelf.acceptedProduct?.ProductName ?? "Empty";
                Debug.Log($"ShelfManagerService: Collected save data for shelf {i} with {saveData.ItemCount}x {productName}");
            }
            
            // Сохраняем многоуровневые полки
            int multiShelfStartIndex = _activeShelves.Count; // Начинаем индексацию после обычных полок
            for (int i = 0; i < _activeMultiLevelShelves.Count; i++)
            {
                var multiShelf = _activeMultiLevelShelves[i];
                if (multiShelf == null) continue;
                
                // Получаем уровни полки
                var shelfLevels = multiShelf.ShelfLevels;
                if (shelfLevels == null)
                {
                    Debug.LogError($"ShelfManagerService: ShelfLevels is null for multi-level shelf {multiShelf.gameObject.name}");
                    continue;
                }
                
                // Создаем данные для многоуровневой полки
                ShelfSaveData saveData = new ShelfSaveData
                {
                    ShelfId = multiShelfStartIndex + i, // ID с учетом обычных полок
                    ProductType = "", // Не используется для многоуровневых
                    ItemCount = 0, // Не используется для многоуровневых
                    Levels = new List<ShelfLevelData>()
                };
                
                // Сохраняем данные каждого уровня
                foreach (var level in shelfLevels)
                {
                    if (level == null) continue;
                    
                    var levelData = new ShelfLevelData
                    {
                        Level = shelfLevels.IndexOf(level),
                        ProductType = level.AcceptedProduct?.ProductID ?? "",
                        ItemCount = level.CurrentItemCount
                    };
                    
                    saveData.Levels.Add(levelData);
                }
                
                shelvesData.Add(saveData);
                Debug.Log($"ShelfManagerService: Collected save data for multi-level shelf {multiShelf.gameObject.name} with {saveData.Levels.Count} levels");
            }
            
            Debug.Log($"ShelfManagerService: Collected {shelvesData.Count} shelves for saving ({_activeShelves.Count} regular, {_activeMultiLevelShelves.Count} multi-level)");
            return shelvesData;
        }
        
        public void RestoreShelves(List<ShelfSaveData> shelvesData)
        {
            if (shelvesData == null || shelvesData.Count == 0)
            {
                Debug.Log("ShelfManagerService: No shelves data to restore");
                return;
            }
            
            // КРИТИЧЕСКИ ВАЖНО: Очищаем состояние всех полок перед загрузкой
            Debug.Log($"ShelfManagerService: Clearing existing shelf states before restoration");
            foreach (var shelf in _activeShelves)
            {
                if (shelf != null)
                {
                    shelf.RestoreState(null, 0); // Очищаем полку
                }
            }
            
            Debug.Log($"ShelfManagerService: Restoring {shelvesData.Count} shelves, have {_activeShelves.Count} registered shelves");
            
            // Сохраняем данные для отложенного восстановления
            _pendingShelfData = shelvesData;
            
            // Если нет зарегистрированных полок, попробуем подождать немного
            if (_activeShelves.Count == 0)
            {
                Debug.LogWarning("ShelfManagerService: No shelves registered yet! Starting delayed restoration coroutine.");
                // Запускаем корутину для ожидания (но нам нужен MonoBehaviour для этого)
                // Вместо этого просто сохраняем данные и полагаемся на RegisterShelf
            }
            
            // Пробуем восстановить сейчас
            TryRestorePendingShelves();
        }
        
        private void TryRestorePendingShelves()
        {
            if (_pendingShelfData == null)
            {
                Debug.Log("ShelfManagerService: TryRestorePendingShelves called but no pending data");
                return;
            }
            
            if (_activeShelves.Count == 0 && _activeMultiLevelShelves.Count == 0)
            {
                Debug.Log("ShelfManagerService: No shelves registered yet, will retry when shelves are registered");
                return;
            }
            
            Debug.Log($"ShelfManagerService: Attempting to restore {_pendingShelfData.Count} shelves with {_activeShelves.Count} regular shelves and {_activeMultiLevelShelves.Count} multi-level shelves registered");
            
            // Логируем какие полки у нас зарегистрированы
            for (int i = 0; i < _activeShelves.Count; i++)
            {
                var shelf = _activeShelves[i];
                Debug.Log($"ShelfManagerService: Registered shelf {i}: '{shelf?.gameObject?.name ?? "NULL"}'");
            }
            
            int multiShelfStartIndex = _activeShelves.Count;
            for (int i = 0; i < _activeMultiLevelShelves.Count; i++)
            {
                var multiShelf = _activeMultiLevelShelves[i];
                Debug.Log($"ShelfManagerService: Registered multi-level shelf {multiShelfStartIndex + i}: '{multiShelf?.gameObject?.name ?? "NULL"}'");
            }
            
            // Логируем какие полки нужно восстановить
            foreach (var saveData in _pendingShelfData)
            {
                if (saveData.Levels != null && saveData.Levels.Count > 0)
                {
                    Debug.Log($"ShelfManagerService: Need to restore multi-level shelf {saveData.ShelfId} with {saveData.Levels.Count} levels");
                }
                else
                {
                    Debug.Log($"ShelfManagerService: Need to restore regular shelf {saveData.ShelfId}: ProductType='{saveData.ProductType}', ItemCount={saveData.ItemCount}");
                }
            }
            
            // Подсчитываем, сколько полок удалось восстановить
            int restoredCount = 0;
            int maxShelfId = -1;
            
            foreach (var saveData in _pendingShelfData)
            {
                // Проверяем, это многоуровневая полка или обычная
                if (saveData.Levels != null && saveData.Levels.Count > 0)
                {
                    // Восстанавливаем многоуровневую полку
                    int multiShelfIndex = saveData.ShelfId - _activeShelves.Count;
                    if (multiShelfIndex >= 0 && multiShelfIndex < _activeMultiLevelShelves.Count)
                    {
                        var multiShelf = _activeMultiLevelShelves[multiShelfIndex];
                        if (multiShelf != null)
                        {
                            RestoreMultiLevelShelfState(multiShelf, saveData);
                            Debug.Log($"ShelfManagerService: Successfully restored multi-level shelf {saveData.ShelfId}");
                            restoredCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"ShelfManagerService: Multi-level shelf at index {multiShelfIndex} is null");
                        }
                    }
                    else
                    {
                        if (saveData.ShelfId > maxShelfId)
                            maxShelfId = saveData.ShelfId;
                        Debug.LogWarning($"ShelfManagerService: Cannot restore multi-level shelf {saveData.ShelfId} yet, index {multiShelfIndex} out of range");
                    }
                }
                else
                {
                    // Восстанавливаем обычную полку
                if (saveData.ShelfId >= 0 && saveData.ShelfId < _activeShelves.Count)
                {
                    var shelf = _activeShelves[saveData.ShelfId];
                    if (shelf != null)
                    {
                        // Восстанавливаем состояние полки
                        ProductConfig product = null;
                        if (!string.IsNullOrEmpty(saveData.ProductType) && _productCatalogService != null)
                        {
                            product = _productCatalogService.GetProductConfigByID(saveData.ProductType);
                            if (product == null)
                            {
                                Debug.LogWarning($"ShelfManagerService: Product ID '{saveData.ProductType}' not found in catalog for shelf {saveData.ShelfId}");
                            }
                            else
                            {
                                Debug.Log($"ShelfManagerService: Found product '{product.ProductName}' for shelf {saveData.ShelfId}");
                            }
                        }
                        else if (string.IsNullOrEmpty(saveData.ProductType))
                        {
                            Debug.Log($"ShelfManagerService: Shelf {saveData.ShelfId} has empty ProductType, will be cleared");
                        }
                        
                        // Устанавливаем состояние полки напрямую
                        Debug.Log($"ShelfManagerService: Calling RestoreShelfState for shelf {saveData.ShelfId} with {saveData.ItemCount} items");
                        RestoreShelfState(shelf, product, saveData.ItemCount);
                        
                        string productName = product?.ProductName ?? "Empty";
                        Debug.Log($"ShelfManagerService: Successfully restored shelf {saveData.ShelfId} with {saveData.ItemCount}x {productName}");
                        restoredCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"ShelfManagerService: Shelf at index {saveData.ShelfId} is null");
                    }
                }
                else
                {
                    // Запоминаем максимальный индекс для проверки
                    if (saveData.ShelfId > maxShelfId)
                        maxShelfId = saveData.ShelfId;
                        
                    Debug.LogWarning($"ShelfManagerService: Cannot restore shelf {saveData.ShelfId} yet, have {_activeShelves.Count} shelves registered (out of range)");
                }
            }
            }
            
            // Общее количество зарегистрированных полок
            int totalRegisteredShelves = _activeShelves.Count + _activeMultiLevelShelves.Count;
            
            // Если восстановили все полки из данных или максимальный индекс меньше количества полок,
            // считаем операцию завершенной
            if (restoredCount == _pendingShelfData.Count || maxShelfId < totalRegisteredShelves)
            {
                Debug.Log($"ShelfManagerService: Completed shelf restoration - restored {restoredCount}/{_pendingShelfData.Count} shelves");
                _pendingShelfData = null; // Очищаем отложенные данные
            }
            else
            {
                Debug.Log($"ShelfManagerService: Partial restoration - restored {restoredCount}/{_pendingShelfData.Count} shelves, waiting for more shelves to register (maxShelfId: {maxShelfId}, totalRegistered: {totalRegisteredShelves})");
            }
        }
        
        public void ClearAllShelves()
        {
            Debug.Log($"ShelfManagerService: Clearing {_activeShelves.Count} regular shelves and {_activeMultiLevelShelves.Count} multi-level shelves");
            
            // Очищаем отложенные данные
            _pendingShelfData = null;
            
            // Очищаем обычные полки
            foreach (var shelf in _activeShelves)
            {
                if (shelf != null)
                {
                    // Очищаем полку
                    RestoreShelfState(shelf, null, 0);
                }
            }
            
            // Очищаем многоуровневые полки
            foreach (var multiShelf in _activeMultiLevelShelves)
            {
                if (multiShelf != null)
                {
                    // Создаем пустой список для очистки всех уровней
                    var emptyLevels = new List<(ProductConfig product, int count)>();
                    
                    // Получаем количество уровней
                    var shelfLevels = multiShelf.ShelfLevels;
                    if (shelfLevels != null)
                    {
                        // Добавляем пустое состояние для каждого уровня
                        for (int i = 0; i < shelfLevels.Count; i++)
                        {
                            emptyLevels.Add((null, 0));
                        }
                    }
                    
                    multiShelf.RestoreState(emptyLevels);
                }
            }
            
            Debug.Log("ShelfManagerService: All shelves cleared");
        }
        
        /// <summary>
        /// Восстанавливает состояние конкретной полки
        /// </summary>
        private void RestoreShelfState(ShelfController shelf, ProductConfig product, int itemCount)
        {
            // Используем новый публичный метод вместо рефлексии
            shelf.RestoreState(product, itemCount);
        }
        
        /// <summary>
        /// Восстанавливает состояние многоуровневой полки
        /// </summary>
        private void RestoreMultiLevelShelfState(MultiLevelShelfController multiShelf, ShelfSaveData saveData)
        {
            if (saveData.Levels == null) return;
            
            // Создаем список состояний уровней
            var levelStates = new List<(ProductConfig product, int count)>();
            
            foreach (var levelData in saveData.Levels)
            {
                ProductConfig product = null;
                
                if (!string.IsNullOrEmpty(levelData.ProductType) && _productCatalogService != null)
                {
                    product = _productCatalogService.GetProductConfigByID(levelData.ProductType);
                    if (product == null)
                    {
                        Debug.LogWarning($"ShelfManagerService: Product ID '{levelData.ProductType}' not found for level {levelData.Level}");
                    }
                }
                
                levelStates.Add((product, levelData.ItemCount));
            }
            
            // Восстанавливаем состояние полки
            multiShelf.RestoreState(levelStates);
        }
    }
} 