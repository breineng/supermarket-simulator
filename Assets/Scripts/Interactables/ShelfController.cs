using UnityEngine;
using Supermarket.Services.Game; // For IPlayerHandService and IShelfManagerService
using Core.Interfaces; // For IInteractable
using BehaviourInject;
using System.Collections.Generic;
using Core.Models; // <--- Добавляем using
using Supermarket.Services.UI; // For INotificationService

namespace Supermarket.Interactables
{
    public class ShelfController : MonoBehaviour, IInteractable
    {
        [Inject]
        public IPlayerHandService _playerHandService;

        [Inject]
        public IInputModeService _inputModeService;
        
        [Inject]
        public INotificationService _notificationService;

        [Inject]
        public IShelfManagerService _shelfManagerService;

        // acceptedProduct теперь будет хранить тип товара, который УЖЕ лежит на полке.
        // Может быть null, если полка пуста.
        // Настраивать его в инспекторе больше не нужно, он будет определяться первым положенным товаром.
        public ProductConfig acceptedProduct { get; private set; } 
        public int maxCapacity = 10; 
        // public List<GameObject> itemVisuals = new List<GameObject>(); // Старая система визуализации
        public List<Transform> itemSpawnPoints = new List<Transform>(); // Новая система: точки для спавна префабов
        [Header("Customer Interaction")]
        public Transform customerApproachPoint; // Точка, куда должны подходить покупатели
        private List<GameObject> _spawnedItemInstances = new List<GameObject>(); // Ссылки на заспавненные префабы

        private int _currentItemCount = 0;
        private int _itemsInAnimation = 0; // Счетчик товаров, которые сейчас анимируются

        private void Start()
        {
            // Очистим существующие инстансы на случай перезапуска сцены или если что-то было в редакторе
            foreach (GameObject instance in _spawnedItemInstances)
            {
                if (instance != null) Destroy(instance);
            }
            _spawnedItemInstances.Clear();
            _itemsInAnimation = 0; // Сбрасываем счетчик анимаций
            // _currentItemCount должен быть 0 при старте, если не предусмотрена логика загрузки сохраненного состояния
            // acceptedProduct также должен быть null
            // UpdateVisuals(); // Обновим на случай, если _currentItemCount не 0 (хотя не должен быть)
            
            // Регистрируем полку в ShelfManagerService для сохранения
            if (_shelfManagerService != null)
            {
                _shelfManagerService.RegisterShelf(this);
            }
            else
            {
                Debug.LogWarning($"ShelfController: IShelfManagerService is null, shelf '{gameObject.name}' will not be saved", this);
            }
        }

        public InteractionPromptData GetInteractionPrompt()
        {
            if (_playerHandService == null)
            {
                return InteractionPromptData.Empty;
            }
  
            if (_playerHandService.IsHoldingBox() && _playerHandService.IsBoxOpen()) // Рука с ОТКРЫТОЙ коробкой
            {
                string promptText = "";
                ProductConfig currentHeldProduct = _playerHandService.GetProductInHand(); 
                int quantityInHand = _playerHandService.GetQuantityInHand();
  
                // --- Логика для ЛКМ (Выложить товар из коробки) ---
                if (currentHeldProduct != null) // Если в открытой коробке есть конкретный товар
                {
                    if (currentHeldProduct.CanBePlacedOnShelf) 
                    {
                        bool canPlaceThisProductType = (_currentItemCount == 0 || currentHeldProduct == acceptedProduct) && _currentItemCount < maxCapacity;
                        if (canPlaceThisProductType && quantityInHand > 0)
                        {
                            promptText += $"Нажмите [ЛКМ] чтобы выложить {currentHeldProduct.ProductName}\n";
                        }
                        else if (currentHeldProduct != acceptedProduct && _currentItemCount > 0)
                        {
                            promptText += $"Полка занята ({acceptedProduct.ProductName})\n";
                        }
                        else if (_currentItemCount >= maxCapacity && currentHeldProduct == acceptedProduct)
                        {
                            promptText += "Полка полна\n";
                        }
                        else if (quantityInHand <= 0)
                        {
                            promptText += "В коробке закончились товары\n";
                        }
                    }
                    else
                    {
                        promptText += $"{currentHeldProduct.ProductName} нельзя класть на полку\n";
                    }
                }
                // currentHeldProduct == null (в руках "полностью пустая" ОТКРЫТАЯ коробка)
                // В этом случае ЛКМ не предлагаем (нечего выкладывать).
                // Подсказка для ПКМ (взять в пустую коробку) будет ниже.

                // --- Логика для ПКМ (Забрать товар с полки в открытую коробку) ---
                // Проверяем, есть ли товары на полке физически
                if (_currentItemCount > 0 && acceptedProduct != null) // Есть что забирать с полки
                {
                    bool canTake = false;
                    string pkmActionText = "";

                    if (currentHeldProduct == null) // В руках "полностью пустая" коробка
                    {
                        canTake = _playerHandService.CanAddItemToOpenBox(acceptedProduct, 1);
                        if (canTake)
                        {
                            pkmActionText = $"Нажмите [ПКМ] чтобы забрать {acceptedProduct.ProductName} в пустую коробку";
                        }
                        else
                        {
                            if (promptText.Length > 0) promptText += "\n";
                            promptText += $"Коробка переполнена (лимит: {acceptedProduct.ItemsPerBox} шт.)";
                        }
                    }
                    else if (currentHeldProduct == acceptedProduct) // В руках коробка того же типа, что и на полке
                    {
                        canTake = _playerHandService.CanAddItemToOpenBox(acceptedProduct, 1);
                        if (canTake)
                        {
                            int currentInBox = _playerHandService.GetQuantityInHand();
                            pkmActionText = $"Нажмите [ПКМ] чтобы забрать {acceptedProduct.ProductName} в коробку ({currentInBox}/{acceptedProduct.ItemsPerBox})";
                        }
                        else
                        {
                            if (promptText.Length > 0) promptText += "\n";
                            promptText += $"Коробка полна ({quantityInHand}/{acceptedProduct.ItemsPerBox})";
                        }
                    }
                    else // В руках коробка другого типа
                    {
                        if (promptText.Length > 0) promptText += "\n";
                        promptText += $"Нельзя смешивать {acceptedProduct.ProductName} с {currentHeldProduct.ProductName} в одной коробке";
                    }

                    if (canTake)
                    {
                        if (promptText.Length > 0) promptText += "\n";
                        promptText += pkmActionText;
                    }
                }
                else if (currentHeldProduct == null) // Полка пуста, в руках пустая открытая коробка
                {
                     // Можно добавить, если нужно, но обычно это состояние не требует явной подсказки для ПКМ
                     // promptText += "Полка пуста, в руках пустая коробка"; 
                }

                if (!string.IsNullOrEmpty(promptText)) 
                {
                    return new InteractionPromptData(promptText.TrimEnd('\n'), PromptType.Complete);
                }
                
                // Фоллбэк для открытой коробки, если не сформировано других подсказок
                if (currentHeldProduct != null) 
                {
                    return new InteractionPromptData($"Открыта коробка с {currentHeldProduct.ProductName} ({quantityInHand}/{currentHeldProduct.ItemsPerBox})", PromptType.Complete);
                }
                else 
                {
                    return new InteractionPromptData("Открыта пустая коробка", PromptType.Complete); 
                }
            }
            else if (_playerHandService.IsHoldingBox() && !_playerHandService.IsBoxOpen()) // Рука с ЗАКРЫТОЙ коробкой
            {
                return InteractionPromptData.Empty; // Никаких подсказок от полки, если коробка в руках закрыта
            }
            else if (!_playerHandService.IsHoldingBox()) // В руках НЕТ коробки ВООБЩЕ
            {
                if (acceptedProduct != null && _currentItemCount > 0) 
                    return new InteractionPromptData($"Полка: {acceptedProduct.ProductName} ({_currentItemCount}/{maxCapacity})", PromptType.Complete);
                return new InteractionPromptData("Полка (пусто)", PromptType.Complete);
            }
            
            return InteractionPromptData.Empty; // Сюда не должны доходить
        }

        public void Interact(GameObject interactor) // Старый Interact (E)
        {
            // Если мы решили, что из закрытой коробки нельзя выкладывать предметы по клавише E,
            // то этот метод не должен выполнять логику выкладывания.
            // Подсказка для этого действия уже убрана в GetInteractionPrompt.

            // Текущая проверка ниже означает, что метод Interact(E) вызывается только для закрытых коробок.
            // if (_playerHandService == null || !_playerHandService.IsHoldingBox() || _playerHandService.IsBoxOpen()) return; 

            // Закомментируем логику выкладывания, чтобы она не выполнялась:
            /*
            ProductConfig heldProduct = _playerHandService.GetProductInHand();
            if (heldProduct != null && heldProduct.CanBePlacedOnShelf)
            {
                if (_currentItemCount == 0) // Полка пуста
                {
                    if (_currentItemCount < maxCapacity)
                    {
                        acceptedProduct = heldProduct; // Запоминаем тип товара для этой полки
                        _playerHandService.ConsumeItemFromHand(1);
                        _currentItemCount++;
                        UpdateVisuals();
                        Debug.Log($"Placed {heldProduct.ProductName} on shelf with E. Current count: {_currentItemCount}");
                    }
                }
                else if (heldProduct == acceptedProduct && _currentItemCount < maxCapacity) // На полке такой же товар и есть место
                {
                    _playerHandService.ConsumeItemFromHand(1);
                    _currentItemCount++;
                    UpdateVisuals();
                    Debug.Log($"Placed {heldProduct.ProductName} on shelf with E. Current count: {_currentItemCount}");
                }
            }
            */
            Debug.Log("ShelfController.Interact(E) called, but action for closed box is disabled.");
        }

        public void OnFocus()
        {
            // Optional: Add visual feedback when focused
        }

        public void OnBlur()
        {
            // Optional: Remove visual feedback when blurred
        }

        private void UpdateVisuals()
        {
            if (itemSpawnPoints.Count == 0 && maxCapacity > 0)
            {
                // Debug.LogWarning($"Shelf '{gameObject.name}' has maxCapacity > 0 but no itemSpawnPoints assigned. Visuals will not work correctly.", this);
                // Если нет точек спавна, но есть вместимость, старая логика активации тут не сработает.
                // Для новой логики это критично.
            }

            // Синхронизируем количество заспавненных объектов с _currentItemCount
            while (_spawnedItemInstances.Count < _currentItemCount)
            {
                // Нужно добавить визуальный элемент
                if (acceptedProduct == null || acceptedProduct.Prefab == null)
                {
                    Debug.LogError($"Cannot spawn item visual: acceptedProduct is null or has no Prefab. Shelf: {gameObject.name}", this);
                    break; // Прерываем цикл, чтобы избежать бесконечного цикла ошибок
                }
                if (_spawnedItemInstances.Count < itemSpawnPoints.Count)
                {
                    Transform spawnPoint = itemSpawnPoints[_spawnedItemInstances.Count];
                    GameObject newItemInstance = Instantiate(acceptedProduct.Prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint); // Спавним как дочерний элемент точки
                    _spawnedItemInstances.Add(newItemInstance);
                }
                else
                {
                    Debug.LogWarning($"Not enough spawn points on shelf '{gameObject.name}' to display all items. Have {_spawnedItemInstances.Count}, need to show {_currentItemCount}.", this);
                    break; // Больше нет точек для спавна
                }
            }

            while (_spawnedItemInstances.Count > _currentItemCount)
            {
                // Нужно удалить визуальный элемент
                if (_spawnedItemInstances.Count > 0)
                {
                    GameObject instanceToRemove = _spawnedItemInstances[_spawnedItemInstances.Count - 1];
                    _spawnedItemInstances.RemoveAt(_spawnedItemInstances.Count - 1);
                    if (instanceToRemove != null) // Дополнительная проверка перед Destroy
                    {
                        Destroy(instanceToRemove);
                    }
                }
                else
                {
                    break; // Больше нет инстансов для удаления
                }
            }
        }

        // New methods for LKM/PKM interaction with open box
        public bool CanPlaceFromOpenBox(ProductConfig productInBox)
        {
            if (productInBox == null || !productInBox.CanBePlacedOnShelf || _currentItemCount >= maxCapacity)
            {
                return false;
            }
            if (_currentItemCount == 0) // Полка пуста, можно класть любой подходящий товар
            {
                return true;
            }
            // Полка не пуста, можно класть только такой же товар
            return productInBox == acceptedProduct;
        }

        public bool PlaceItemFromOpenBox()
        {
            // Предполагается, что CanPlaceFromOpenBox уже был вызван и вернул true
            // Также предполагается, что PlayerBoxController уже вызвал _playerHandService.ConsumeItemFromHand(1)
            
            ProductConfig productToPlace = _playerHandService.GetProductInHand(); // Получаем актуальный продукт ПОСЛЕ Consume
                                                                            // Или лучше, чтобы PlayerBoxController передавал продукт?
                                                                            // Пока оставим так, но это место для возможного рефакторинга.
                                                                            // Если ConsumeItemFromHand очистил руку, GetProductInHand вернет null.
                                                                            // Это не страшно, т.к. acceptedProduct УЖЕ должен быть установлен или установится сейчас.

            if (_currentItemCount == 0) 
            {
                // Если полка была пуста, теперь ее тип определяется положенным товаром
                // Важно: продукт, который ФАКТИЧЕСКИ вычитается из руки сервисом, должен быть известен.
                // Мы должны установить acceptedProduct НА ОСНОВЕ ТОГО, ЧТО БЫЛО В РУКЕ ДО ConsumeItemFromHand.
                // Это значит, PlayerBoxController должен передать ProductConfig в PlaceItemFromOpenBox.
                // ---- РЕФАКТОРИНГ ----
                // Изменим PlaceItemFromOpenBox, чтобы он принимал ProductConfig.
                // PlayerBoxController будет вызывать ConsumeItemFromHand, а затем PlaceItemFromOpenBox(consumedProduct).
                Debug.LogError("PlaceItemFromOpenBox вызван некорректно, если _currentItemCount == 0. Требуется ProductConfig.");
                return false; // Это состояние не должно достигаться при правильном вызове с ProductConfig
            }
            
            // Если мы здесь, значит _currentItemCount > 0 и товар совпадает с acceptedProduct
            // или _currentItemCount был 0, но productPlacedByCaller был передан и acceptedProduct установлен.
            _currentItemCount++;
            UpdateVisuals();
            Debug.Log($"Placed {acceptedProduct.ProductName} on shelf from open box. Current count: {_currentItemCount}");
            return true;
        }

        // Перегруженный метод для корректной установки acceptedProduct при первом размещении
        public bool PlaceItemFromOpenBox(ProductConfig productPlacedByCaller)
        {
            if (productPlacedByCaller == null || !productPlacedByCaller.CanBePlacedOnShelf || _currentItemCount >= maxCapacity)
                return false;

            if (_currentItemCount == 0)
            {
                acceptedProduct = productPlacedByCaller;
            }
            else if (productPlacedByCaller != acceptedProduct)
            {
                 Debug.LogWarning($"Attempted to place {productPlacedByCaller.ProductName} on a shelf for {acceptedProduct.ProductName}. This should be caught by CanPlaceFromOpenBox.");
                return false; // Не должны сюда попадать, если CanPlaceFromOpenBox работает правильно
            }

            _currentItemCount++;
            UpdateVisuals();
            Debug.Log($"Placed {acceptedProduct.ProductName} on shelf from open box. Current count: {_currentItemCount}");
            return true;
        }

        public bool CanTakeToOpenBox(ProductConfig productInBox)
        {
            // Проверяем, есть ли товары на полке физически
            if (_currentItemCount <= 0) return false;
            
            // Проверяем, может ли коробка принять еще один товар
            if (_playerHandService != null && !_playerHandService.CanAddItemToOpenBox(acceptedProduct, 1))
            {
                return false;
            }
            
            // Если коробка "полностью пустая" (ProductInBox == null), в неё можно положить любой товар с полки
            if (productInBox == null) return true;
            
            // Если коробка не пустая, можно положить только такой же товар как в коробке
            return productInBox == acceptedProduct;
        }
        
        /// <summary>
        /// Забирает товар в открытую коробку
        /// </summary>
        public bool TakeItemToOpenBox()
        {
            // Проверяем, есть ли товары на полке физически
            if (_currentItemCount <= 0)
            {
                Debug.Log($"ShelfController: Cannot take item - no items on shelf (count: {_currentItemCount})");
                return false;
            }
            
            // Увеличиваем счетчик анимаций перед удалением товара
            _itemsInAnimation++;
            
            _currentItemCount--;
            
            // Счетчик анимаций будет уменьшен после завершения анимации
            
            UpdateVisuals();
            Debug.Log($"Took {acceptedProduct.ProductName} from shelf into open box. Current count: {_currentItemCount}, Items in animation: {_itemsInAnimation}");
            
            if (_currentItemCount == 0)
            {
                acceptedProduct = null; // Полка снова становится "без типа"
            }
            return true;
        }

        /// <summary>
        /// Вызывается после завершения анимации взятия товара
        /// </summary>
        public void CompleteTakeAnimation()
        {
            if (_itemsInAnimation > 0)
            {
                _itemsInAnimation--;
                Debug.Log($"ShelfController: Completed take animation. Items in animation: {_itemsInAnimation}");
            }
        }

        // Публичные методы для покупателей
        public int GetCurrentItemCount()
        {
            return _currentItemCount;
        }
        
        /// <summary>
        /// Получает позицию, куда должен подойти покупатель для взаимодействия с полкой
        /// </summary>
        public Vector3 GetCustomerApproachPosition()
        {
            if (customerApproachPoint != null)
            {
                return customerApproachPoint.position;
            }
            
            // Если точка не задана, используем позицию полки с небольшим отступом
            Debug.LogWarning($"ShelfController '{gameObject.name}': customerApproachPoint not set, using shelf position");
            return transform.position + transform.forward * -1.5f; // Отступ на 1.5 метра перед полкой
        }
        
        public void CustomerTakeItem()
        {
            if (_currentItemCount > 0)
            {
                _currentItemCount--;
                UpdateVisuals();
                Debug.Log($"Customer took {acceptedProduct?.ProductName} from shelf. Remaining: {_currentItemCount}");
                
                // Проверяем низкий запас товаров
                if (_currentItemCount == 0)
                {
                    _notificationService?.ShowNotification($"На полке закончился товар: {acceptedProduct?.ProductName}!", NotificationType.Warning, 5f);
                    acceptedProduct = null; // Полка снова становится "без типа"
                }
                else if (_currentItemCount <= 2)
                {
                    _notificationService?.ShowNotification($"Мало товара на полке: {acceptedProduct?.ProductName} (осталось {_currentItemCount} шт.)", NotificationType.Warning);
                }
            }
        }
        
        /// <summary>
        /// Восстанавливает состояние полки из сохраненных данных
        /// Используется системой сохранения для загрузки игры
        /// </summary>
        public void RestoreState(ProductConfig product, int itemCount)
        {
            Debug.Log($"ShelfController '{gameObject.name}': RestoreState called - Product: '{product?.ProductName ?? "NULL"}', ItemCount: {itemCount}");
            
            // Очищаем текущее состояние
            Debug.Log($"ShelfController '{gameObject.name}': Clearing current state (was: '{acceptedProduct?.ProductName ?? "NULL"}' x{_currentItemCount})");
            ClearShelf();
            
            // Устанавливаем новое состояние
            acceptedProduct = product;
            _currentItemCount = itemCount;
            _itemsInAnimation = 0; // Сбрасываем счетчик анимаций при восстановлении
            
            Debug.Log($"ShelfController '{gameObject.name}': State restored to '{acceptedProduct?.ProductName ?? "NULL"}' x{_currentItemCount}");
            
            // Обновляем визуалы
            UpdateVisuals();
            
            Debug.Log($"ShelfController '{gameObject.name}': Visuals updated, restoration complete");
        }
        
        /// <summary>
        /// Получает позицию следующего свободного слота на полке
        /// Используется для анимации размещения товара
        /// </summary>
        public Vector3 GetNextAvailableSlotPosition()
        {
            if (itemSpawnPoints.Count == 0)
            {
                // Если нет назначенных точек спавна, возвращаем позицию полки
                Debug.LogWarning($"ShelfController '{gameObject.name}': No itemSpawnPoints configured, using shelf position + offset");
                return transform.position + Vector3.up * 0.5f;
            }
            
            // Следующий свободный слот имеет индекс равный текущему количеству товаров
            int nextSlotIndex = Mathf.Min(_currentItemCount, itemSpawnPoints.Count - 1);
            Vector3 slotPosition = itemSpawnPoints[nextSlotIndex].position;
            
            Debug.Log($"ShelfController '{gameObject.name}': GetNextAvailableSlotPosition - currentItemCount: {_currentItemCount}, slotIndex: {nextSlotIndex}, position: {slotPosition}");
            return slotPosition;
        }
        
        /// <summary>
        /// Получает позицию последнего занятого слота на полке
        /// Используется для анимации взятия товара
        /// </summary>
        public Vector3 GetLastOccupiedSlotPosition()
        {
            // При взятии товара _currentItemCount уже уменьшен, но визуально товар еще на месте
            // Поэтому нужно использовать _currentItemCount + _itemsInAnimation для правильной позиции
            int visualItemCount = _currentItemCount + _itemsInAnimation;
            
            if (itemSpawnPoints.Count == 0 || visualItemCount <= 0)
            {
                // Если нет назначенных точек спавна или товаров, возвращаем позицию полки
                Debug.LogWarning($"ShelfController '{gameObject.name}': No itemSpawnPoints or no items, using shelf position + offset");
                return transform.position + Vector3.up * 0.5f;
            }
            
            // Позиция товара, который сейчас забирается (визуально еще на месте)
            int lastSlotIndex = Mathf.Clamp(visualItemCount - 1, 0, itemSpawnPoints.Count - 1);
            Vector3 slotPosition = itemSpawnPoints[lastSlotIndex].position;
            
            Debug.Log($"ShelfController '{gameObject.name}': GetLastOccupiedSlotPosition - currentItemCount: {_currentItemCount}, itemsInAnimation: {_itemsInAnimation}, visualCount: {visualItemCount}, slotIndex: {lastSlotIndex}, position: {slotPosition}");
            return slotPosition;
        }

        /// <summary>
        /// Очищает все товары с полки
        /// </summary>
        public void ClearShelf()
        {
            foreach (GameObject instance in _spawnedItemInstances)
            {
                if (instance != null) Destroy(instance);
            }
            _spawnedItemInstances.Clear();
            _currentItemCount = 0;
            _itemsInAnimation = 0; // Сбрасываем счетчик анимаций при очистке
            acceptedProduct = null;
        }

        private void OnDestroy()
        {
            // Отменяем регистрацию полки в ShelfManagerService
            if (_shelfManagerService != null)
            {
                _shelfManagerService.UnregisterShelf(this);
            }
        }
    }
} 