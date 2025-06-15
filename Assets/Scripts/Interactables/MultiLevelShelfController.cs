using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Supermarket.Services.Game;
using Core.Interfaces;
using BehaviourInject;
using Core.Models;
using Supermarket.Services.UI;

namespace Supermarket.Interactables
{
    /// <summary>
    /// Контроллер многоуровневой полки
    /// Каждый уровень может содержать только один тип товара
    /// </summary>
    public class MultiLevelShelfController : MonoBehaviour, IInteractable
    {
        [Inject] public IPlayerHandService _playerHandService;
        [Inject] public IInputModeService _inputModeService;
        [Inject] public INotificationService _notificationService;
        [Inject] public IShelfManagerService _shelfManagerService;
        
        [Header("Configuration")]
        [SerializeField] private List<ShelfLevel> shelfLevels = new List<ShelfLevel>();
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayerMask = -1; // По умолчанию все слои
        
        [Header("Customer Interaction")]
        [SerializeField] private Transform customerApproachPoint; // Точка, куда должны подходить покупатели
        
        private ShelfLevel currentFocusedLevel;
        private ShelfLevel previousFocusedLevel;
        private bool isInFocus = false;
        
        // Публичное свойство для доступа к уровням (для ShelfManagerService)
        public List<ShelfLevel> ShelfLevels => shelfLevels;
        
        private void Start()
        {
            // Регистрируем полку в ShelfManagerService для сохранения
            if (_shelfManagerService != null)
            {
                _shelfManagerService.RegisterMultiLevelShelf(this);
                Debug.Log($"MultiLevelShelfController: Registered in ShelfManagerService");
            }
            else
            {
                Debug.LogWarning("MultiLevelShelfController: ShelfManagerService not injected!");
            }
            
            // Находим все уровни, если они не назначены в инспекторе
            if (shelfLevels.Count == 0)
            {
                shelfLevels = GetComponentsInChildren<ShelfLevel>().ToList();
            }
        }
        
        private void Update()
        {
            // Постоянно обновляем фокусный уровень, только если полка в фокусе
            // Это нужно для обновления подсветки и корректной работы взаимодействия
            if (isInFocus)
            {
                UpdateFocusedLevel();
            }
        }
        
        public InteractionPromptData GetInteractionPrompt()
        {
            if (_playerHandService == null)
                return InteractionPromptData.Empty;
            
            // UpdateFocusedLevel() теперь вызывается в Update(), поэтому убираем отсюда
            
            if (_playerHandService.IsHoldingBox() && _playerHandService.IsBoxOpen())
            {
                return GetOpenBoxPrompt();
            }
            else if (_playerHandService.IsHoldingBox() && !_playerHandService.IsBoxOpen())
            {
                return InteractionPromptData.Empty; // Закрытая коробка - нет взаимодействия
            }
            else if (!_playerHandService.IsHoldingBox())
            {
                return GetGeneralPrompt();
            }
            
            return InteractionPromptData.Empty;
        }
        
        private InteractionPromptData GetOpenBoxPrompt()
        {
            if (currentFocusedLevel == null)
                return new InteractionPromptData("Смотрите на уровень полки для взаимодействия", PromptType.Complete);
            
            string promptText = "";
            ProductConfig productInBox = _playerHandService.GetProductInHand();
            int quantityInBox = _playerHandService.GetQuantityInHand();
            
            // Проверка выкладки товара (ЛКМ)
            if (productInBox != null)
            {
                if (currentFocusedLevel.CanPlaceProduct(productInBox))
                {
                    if (quantityInBox > 0)
                    {
                        promptText += $"[ЛКМ] Выложить {productInBox.ProductName} (уровень {GetLevelIndex(currentFocusedLevel) + 1})";
                    }
                    else
                    {
                        promptText += "В коробке закончились товары";
                    }
                }
                else if (currentFocusedLevel.IsFull)
                {
                    promptText += $"Уровень {GetLevelIndex(currentFocusedLevel) + 1} полон";
                }
                else if (!currentFocusedLevel.IsEmpty && currentFocusedLevel.AcceptedProduct != productInBox)
                {
                    promptText += $"На уровне {GetLevelIndex(currentFocusedLevel) + 1} уже {currentFocusedLevel.AcceptedProduct.ProductName}";
                }
            }
            
            // Проверка забора товара (ПКМ)
            if (!currentFocusedLevel.IsEmpty)
            {
                if (currentFocusedLevel.CanTakeProduct(productInBox))
                {
                    if (!string.IsNullOrEmpty(promptText)) promptText += "\n";
                    promptText += $"[ПКМ] Забрать {currentFocusedLevel.AcceptedProduct.ProductName} в коробку";
                }
                else if (productInBox != null && productInBox != currentFocusedLevel.AcceptedProduct)
                {
                    if (!string.IsNullOrEmpty(promptText)) promptText += "\n";
                    promptText += $"Нельзя смешивать {currentFocusedLevel.AcceptedProduct.ProductName} с {productInBox.ProductName}";
                }
            }
            
            return new InteractionPromptData(promptText, PromptType.Complete);
        }
        
        private InteractionPromptData GetGeneralPrompt()
        {
            string info = $"Полка ({shelfLevels.Count} уровней):\n";
            
            for (int i = 0; i < shelfLevels.Count; i++)
            {
                var level = shelfLevels[i];
                if (level.IsEmpty)
                {
                    info += $"Уровень {i + 1}: Пусто\n";
                }
                else
                {
                    info += $"Уровень {i + 1}: {level.AcceptedProduct.ProductName} ({level.CurrentItemCount}/{level.MaxCapacity})\n";
                }
            }
            
            return new InteractionPromptData(info.TrimEnd('\n'), PromptType.Complete);
        }
        
        public void Interact(GameObject interactor)
        {
            // Старый метод взаимодействия не используется
            Debug.Log("MultiLevelShelfController.Interact(E) called, but action is disabled.");
        }
        
        /// <summary>
        /// Обрабатывает выкладку товара из открытой коробки (ЛКМ)
        /// </summary>
        public bool TryPlaceFromOpenBox()
        {
            if (currentFocusedLevel == null || _playerHandService == null)
                return false;
            
            ProductConfig productInBox = _playerHandService.GetProductInHand();
            if (productInBox == null || !currentFocusedLevel.CanPlaceProduct(productInBox))
                return false;
            
            // Проверяем, что есть товары в коробке
            if (_playerHandService.GetQuantityInHand() <= 0)
                return false;
            
            // Забираем товар из коробки
            _playerHandService.ConsumeItemFromHand(1);
            
            // Размещаем на полке
            bool placed = currentFocusedLevel.PlaceProduct(productInBox);
            
            return placed;
        }
        
        /// <summary>
        /// Проверяет, может ли фокусный уровень принять товар из коробки игрока с учетом летящих товаров
        /// Используется для предотвращения потери товаров в PlayerBoxController
        /// </summary>
        public bool CanAcceptItemFromBox(ProductConfig product)
        {
            if (currentFocusedLevel == null)
                return false;
            
            return currentFocusedLevel.CanAcceptItemFromBox(product);
        }
        
        /// <summary>
        /// Завершает размещение товара на фокусном уровне (используется после анимации)
        /// </summary>
        public bool CompletePlacementOnFocusedLevel(ProductConfig product)
        {
            if (currentFocusedLevel == null || product == null)
                return false;
            
            // Размещаем товар на фокусном уровне без проверки коробки
            // (товар уже был вычтен из коробки перед анимацией)
            bool placed = currentFocusedLevel.PlaceProduct(product);
            
            return placed;
        }
        
        /// <summary>
        /// Обрабатывает забор товара в открытую коробку (ПКМ)
        /// </summary>
        public bool TryTakeToOpenBox()
        {
            if (currentFocusedLevel == null || _playerHandService == null || currentFocusedLevel.IsEmpty)
                return false;
            
            ProductConfig productInBox = _playerHandService.GetProductInHand();
            if (!currentFocusedLevel.CanTakeProduct(productInBox))
                return false;
            
            // Проверяем, может ли коробка принять еще один товар
            ProductConfig productToTake = currentFocusedLevel.AcceptedProduct;
            if (!_playerHandService.CanAddItemToOpenBox(productToTake, 1))
            {
                Debug.Log($"MultiLevelShelfController: Cannot add {productToTake?.ProductName} to box - box is full or incompatible");
                return false;
            }
            
            ProductConfig takenProduct = currentFocusedLevel.TakeProduct();
            if (takenProduct != null)
            {
                // Добавляем товар в коробку
                _playerHandService.AddItemToOpenBox(takenProduct, 1);
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Определяет, на какой уровень полки смотрит игрок
        /// </summary>
        private void UpdateFocusedLevel()
        {
            previousFocusedLevel = currentFocusedLevel;
            currentFocusedLevel = null;
            
            if (shelfLevels.Count == 0)
                return;
            
            // Получаем позицию камеры игрока
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
                return;
            
            // Делаем raycast для точного определения уровня
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            
            // Проверяем попадание в коллайдер с учетом слоев
            if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayerMask))
            {
                // Проверяем каждый уровень
                foreach (var level in shelfLevels)
                {
                    if (level == null) continue;
                    
                    // Проверяем, попал ли raycast в этот уровень или его дочерние объекты
                    Transform hitTransform = hit.transform;
                    while (hitTransform != null)
                    {
                        if (hitTransform == level.transform)
                        {
                            currentFocusedLevel = level;
                            // Debug.Log($"MultiLevelShelfController: Focused on level {GetLevelIndex(level) + 1}");
                            break;
                        }
                        hitTransform = hitTransform.parent;
                    }
                    
                    if (currentFocusedLevel != null)
                        break;
                }
                
                // Если не попали точно в уровень, выбираем ближайший по высоте
                if (currentFocusedLevel == null)
                {
                    float hitHeight = hit.point.y;
                    float closestDistance = float.MaxValue;
                    
                    foreach (var level in shelfLevels)
                    {
                        if (level == null) continue;
                        
                        float levelHeight = level.transform.position.y;
                        float distance = Mathf.Abs(hitHeight - levelHeight);
                        
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            currentFocusedLevel = level;
                        }
                    }
                    
                    if (currentFocusedLevel != null)
                    {
                        // Debug.Log($"MultiLevelShelfController: Focused on closest level {GetLevelIndex(currentFocusedLevel) + 1} by height");
                    }
                }
            }
            
            // Обновляем подсветку
            if (previousFocusedLevel != currentFocusedLevel)
            {
                // Убираем подсветку с предыдущего уровня
                if (previousFocusedLevel != null)
                {
                    previousFocusedLevel.RemoveHighlight();
                }
                
                // Добавляем подсветку на новый уровень
                if (currentFocusedLevel != null)
                {
                    currentFocusedLevel.Highlight();
                }
                
                // Логируем изменение для отладки
                if (isInFocus)
                {
                    Debug.Log($"MultiLevelShelfController: Level focus changed from {(previousFocusedLevel != null ? GetLevelIndex(previousFocusedLevel) + 1 : 0)} to {(currentFocusedLevel != null ? GetLevelIndex(currentFocusedLevel) + 1 : 0)}");
                }
            }
        }
        
        /// <summary>
        /// Получает индекс уровня в списке
        /// </summary>
        private int GetLevelIndex(ShelfLevel level)
        {
            return shelfLevels.IndexOf(level);
        }
        
        /// <summary>
        /// Покупатель забирает товар с полки
        /// </summary>
        /// <returns>true если товар был успешно взят, false если товара нет</returns>
        public bool CustomerTakeItem(ProductConfig product)
        {
            // Находим первый уровень с нужным товаром
            var level = shelfLevels.FirstOrDefault(l => l.AcceptedProduct == product && !l.IsEmpty);
            if (level != null)
            {
                // Пытаемся взять товар
                bool taken = level.CustomerTakeProduct();
                
                if (taken)
                {
                    // Проверяем на низкий запас
                    if (level.IsEmpty)
                    {
                        _notificationService?.ShowNotification(
                            $"На уровне {GetLevelIndex(level) + 1} закончился {product.ProductName}!", 
                            NotificationType.Warning, 
                            5f
                        );
                    }
                    else if (level.CurrentItemCount <= 2)
                    {
                        _notificationService?.ShowNotification(
                            $"Мало товара на уровне {GetLevelIndex(level) + 1}: {product.ProductName} (осталось {level.CurrentItemCount})", 
                            NotificationType.Warning
                        );
                    }
                }
                
                return taken;
            }
            
            return false; // Товар не найден или полка пуста
        }
        
        /// <summary>
        /// Проверяет, есть ли товар на полке
        /// </summary>
        public bool HasProduct(ProductConfig product, out int totalCount)
        {
            totalCount = 0;
            foreach (var level in shelfLevels)
            {
                if (level.AcceptedProduct == product)
                {
                    totalCount += level.CurrentItemCount;
                }
            }
            return totalCount > 0;
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
            Debug.LogWarning($"MultiLevelShelfController '{gameObject.name}': customerApproachPoint not set, using shelf position");
            return transform.position + transform.forward * -1.5f; // Отступ на 1.5 метра перед полкой
        }
        
        /// <summary>
        /// Получает товар с фокусного уровня без его удаления
        /// Используется для подготовки анимации
        /// </summary>
        public ProductConfig GetProductFromFocusedLevel()
        {
            if (currentFocusedLevel == null || currentFocusedLevel.IsEmpty)
                return null;
            
            return currentFocusedLevel.AcceptedProduct;
        }
        
        /// <summary>
        /// Проверяет, можно ли взять товар с фокусного уровня в коробку
        /// </summary>
        public bool CanTakeFromFocusedLevel(ProductConfig productInBox)
        {
            if (currentFocusedLevel == null || currentFocusedLevel.IsEmpty)
                return false;
            
            return currentFocusedLevel.CanTakeProduct(productInBox);
        }
        
        /// <summary>
        /// Восстанавливает состояние полки из сохраненных данных
        /// </summary>
        public void RestoreState(List<(ProductConfig product, int count)> levelStates)
        {
            for (int i = 0; i < levelStates.Count && i < shelfLevels.Count; i++)
            {
                var (product, count) = levelStates[i];
                shelfLevels[i].RestoreState(product, count);
            }
        }
        
        /// <summary>
        /// Получает позицию следующего свободного слота на текущем фокусном уровне
        /// Используется для анимации размещения товара
        /// </summary>
        public Vector3 GetNextAvailableSlotPosition()
        {
            if (currentFocusedLevel != null)
            {
                return currentFocusedLevel.GetNextAvailableSlotPosition();
            }
            
            // Фоллбэк - позиция полки
            Debug.LogWarning($"MultiLevelShelfController '{gameObject.name}': No focused level, using shelf position + offset");
            return transform.position + Vector3.up * 0.5f;
        }
        
        /// <summary>
        /// Получает поворот следующего свободного слота на текущем фокусном уровне
        /// Используется для анимации размещения товара
        /// </summary>
        public Quaternion GetNextAvailableSlotRotation()
        {
            if (currentFocusedLevel != null)
            {
                return currentFocusedLevel.GetNextAvailableSlotRotation();
            }
            
            // Фоллбэк - стандартный поворот
            Debug.LogWarning($"MultiLevelShelfController '{gameObject.name}': No focused level, using identity rotation");
            return Quaternion.identity;
        }
        
        /// <summary>
        /// Получает позицию последнего занятого слота на фокусном уровне
        /// Используется для анимации взятия товара
        /// </summary>
        public Vector3 GetLastOccupiedSlotPosition()
        {
            if (currentFocusedLevel != null) // Проверяем только, есть ли фокусный уровень
            {
                // ShelfLevel.GetLastOccupiedSlotPosition сам обработает случай, когда currentItemCount только что стал 0
                return currentFocusedLevel.GetLastOccupiedSlotPosition();
            }
            
            // Фоллбэк, если по какой-то причине нет фокусного уровня
            Debug.LogWarning($"MultiLevelShelfController '{gameObject.name}': No focused level for GetLastOccupiedSlotPosition. Returning self's transform.position as fallback.");
            return transform.position; 
        }

        /// <summary>
        /// Получает поворот последней занятой точки спавна на фокусном уровне
        /// Используется для анимации взятия товара
        /// </summary>
        public Quaternion GetLastOccupiedSlotRotation()
        {
            if (currentFocusedLevel != null) // Проверяем только, есть ли фокусный уровень
            {
                // ShelfLevel.GetLastOccupiedSlotRotation сам обработает случай, когда currentItemCount только что стал 0
                return currentFocusedLevel.GetLastOccupiedSlotRotation();
            }
            
            // Фоллбэк, если по какой-то причине нет фокусного уровня
            Debug.LogWarning($"MultiLevelShelfController '{gameObject.name}': No focused level for GetLastOccupiedSlotRotation. Returning Quaternion.identity as fallback.");
            return Quaternion.identity;
        }

        /// <summary>
        /// Вызывается после завершения анимации взятия товара с фокусного уровня
        /// </summary>
        public void CompleteTakeAnimationOnFocusedLevel()
        {
            if (currentFocusedLevel != null)
            {
                currentFocusedLevel.CompleteTakeFromShelfAnimation();
            }
        }

        /// <summary>
        /// Получает текущий фокусный уровень
        /// </summary>
        public ShelfLevel GetCurrentFocusedLevel()
        {
            return currentFocusedLevel;
        }
        
        /// <summary>
        /// Начинает анимацию размещения товара на фокусный уровень
        /// Вызывается перед началом анимации для резервирования слота
        /// </summary>
        public void StartPlacementAnimationOnFocusedLevel()
        {
            if (currentFocusedLevel != null)
            {
                currentFocusedLevel.StartPlacementAnimation();
            }
        }
        
        /// <summary>
        /// Завершает анимацию размещения товара на фокусный уровень
        /// </summary>
        public void CompletePlacementAnimationOnFocusedLevel()
        {
            if (currentFocusedLevel != null)
            {
                currentFocusedLevel.CompletePlacementAnimation();
            }
        }

        /// <summary>
        /// Обрабатывает отмену анимации взятия товара С полки, когда товар летел в коробку
        /// </summary>
        public void HandleTakeAnimationCancelled(ProductConfig product)
        {
            if (currentFocusedLevel != null)
            {
                currentFocusedLevel.CancelTakeFromShelfAnimation(product);
                Debug.Log($"MultiLevelShelfController: Relayed take animation cancellation for {product?.ProductName} to {currentFocusedLevel.name}");
            }
            else
            {
                Debug.LogWarning($"MultiLevelShelfController: Cannot handle take animation cancellation, no focused level. Product: {product?.ProductName}");
            }
        }

        /// <summary>
        /// Обрабатывает отмену анимации размещения товара НА полку, когда товар летел из коробки
        /// </summary>
        public void HandlePlacementAnimationCancelled(ProductConfig product)
        {
            if (currentFocusedLevel != null)
            {
                currentFocusedLevel.CancelPlacementOnShelfAnimation(product);
                Debug.Log($"MultiLevelShelfController: Relayed placement animation cancellation for {product?.ProductName} to {currentFocusedLevel.name}");
            }
            else
            {
                Debug.LogWarning($"MultiLevelShelfController: Cannot handle placement animation cancellation, no focused level. Product: {product?.ProductName}");
            }
        }

        public void OnFocus()
        {
            isInFocus = true;
            Debug.Log($"MultiLevelShelfController: OnFocus - {gameObject.name}");
        }

        public void OnBlur()
        {
            isInFocus = false;
            
            // Убираем подсветку с предыдущего уровня
            if (previousFocusedLevel != null)
            {
                previousFocusedLevel.RemoveHighlight();
                previousFocusedLevel = null;
            }
            
            currentFocusedLevel = null;
            Debug.Log($"MultiLevelShelfController: OnBlur - {gameObject.name}");
        }
        
        private void OnDestroy()
        {
            // Отменяем регистрацию полки в ShelfManagerService
            if (_shelfManagerService != null)
            {
                _shelfManagerService.UnregisterMultiLevelShelf(this);
            }
        }
    }
} 