using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;
using Core.Interfaces;
using Core.Models;
using Supermarket.Services.Game;
using Supermarket.Interactables;
using System.Collections.Generic;
using System.Linq;

namespace Supermarket.Components
{
    /// <summary>
    /// Компонент для продажи размещенных объектов нажатием клавиши U
    /// Интегрируется с существующей системой IInteractable
    /// При продаже полок с товарами автоматически создает коробки с товарами
    /// </summary>
    public class PlaceableObjectSeller : MonoBehaviour
    {
        [Inject] public IPlacementService _placementService { get; set; }
        [Inject] public IPlayerDataService _playerDataService { get; set; }
        [Inject] public IProductCatalogService _productCatalogService { get; set; }
        [Inject] public IInputModeService _inputModeService { get; set; }
        [Inject] public IInteractionService _interactionService { get; set; }
        [Inject] public IBoxManagerService _boxManagerService { get; set; }

        private PlayerInput _playerInput;
        private InputAction _sellAction;

        [Header("Sell Settings")]
        [SerializeField] 
        [Tooltip("Процент от стоимости объекта, который получает игрок при продаже")]
        [Range(0.1f, 1.0f)]
        private float _sellPriceMultiplier = 0.5f; // 50% от стоимости

        [Header("Box Spawn Settings")]
        [SerializeField]
        [Tooltip("Радиус спавна коробок вокруг позиции полки")]
        private float _boxSpawnRadius = 2.0f;
        
        [SerializeField]
        [Tooltip("Высота спавна коробок над землей")]
        private float _boxSpawnHeight = 0.2f;

        private void Awake()
        {
            _playerInput = GetComponentInParent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("PlaceableObjectSeller: PlayerInput component not found on this GameObject or its parents!");
                enabled = false;
                return;
            }

            _sellAction = _playerInput.actions.FindAction("SellObject");
            if (_sellAction == null)
            {
                Debug.LogError("PlaceableObjectSeller: 'SellObject' action not found in PlayerInput actions!");
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (_sellAction != null)
            {
                _sellAction.performed += OnSellActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (_sellAction != null)
            {
                _sellAction.performed -= OnSellActionPerformed;
            }
        }

        private void OnSellActionPerformed(InputAction.CallbackContext context)
        {
            // Проверяем, что мы в игровом режиме
            if (_inputModeService != null && _inputModeService.CurrentMode != InputMode.Game)
            {
                Debug.Log("PlaceableObjectSeller: Not in Game mode. Sell action ignored.");
                return;
            }

            // Проверяем, что мы не в режиме размещения или перемещения
            if (_placementService != null && (_placementService.IsInPlacementMode || _placementService.IsInRelocateMode))
            {
                Debug.Log("PlaceableObjectSeller: In placement or relocate mode. Sell action ignored.");
                return;
            }

            // Используем текущий объект в фокусе из InteractionService
            if (_interactionService?.CurrentFocusedInteractable != null)
            {
                GameObject focusedObject = ((MonoBehaviour)_interactionService.CurrentFocusedInteractable).gameObject;
                
                // Проверяем, является ли объект размещенным объектом по тегу
                if (focusedObject.CompareTag("PlacedObject"))
                {
                    TrySellObject(focusedObject);
                }
                else
                {
                    Debug.Log($"PlaceableObjectSeller: Object {focusedObject.name} is not a placed object that can be sold.");
                }
            }
            else
            {
                Debug.Log("PlaceableObjectSeller: No object in focus. Nothing to sell.");
            }
        }

        /// <summary>
        /// Проверяет, можно ли продать размещенный объект и возвращает подсказку для продажи
        /// </summary>
        /// <param name="gameObject">Объект для проверки</param>
        /// <returns>Подсказка для продажи или пустая строка если продажа невозможна</returns>
        public string GetSellHint(GameObject gameObject)
        {
            if (!CanShowSellHint()) return "";
            
            if (!gameObject.CompareTag("PlacedObject")) return "";

            // Получаем информацию о цене объекта
            string objectPrefabName = GetObjectPrefabName(gameObject);
            if (string.IsNullOrEmpty(objectPrefabName)) return "";

            ProductConfig productConfig = _productCatalogService?.GetProductConfigByID(objectPrefabName);
            if (productConfig == null) return "";

            float sellPrice = productConfig.PurchasePrice * _sellPriceMultiplier;
            
            // Получаем клавишу из input system
            string sellKeyBinding = GetSellKeyBinding();
            
            return $"Нажмите [{sellKeyBinding}] чтобы продать за ${sellPrice:F0}";
        }

        /// <summary>
        /// Получает клавишу для действия продажи из input system
        /// </summary>
        private string GetSellKeyBinding()
        {
            if (_sellAction != null)
            {
                string keyBinding = _sellAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (!string.IsNullOrEmpty(keyBinding))
                {
                    return keyBinding.ToUpper();
                }
            }
            return "U"; // Fallback
        }

        /// <summary>
        /// Проверяет, можно ли показывать подсказку о продаже
        /// </summary>
        private bool CanShowSellHint()
        {
            if (_inputModeService != null && _inputModeService.CurrentMode != InputMode.Game)
                return false;

            if (_placementService != null && (_placementService.IsInPlacementMode || _placementService.IsInRelocateMode))
                return false;

            return true;
        }

        private void TrySellObject(GameObject objectToSell)
        {
            if (objectToSell == null)
            {
                Debug.LogWarning("PlaceableObjectSeller: Cannot sell null object.");
                return;
            }

            Debug.Log($"PlaceableObjectSeller: Attempting to sell {objectToSell.name}");

            // Получаем PrefabName объекта
            string objectPrefabName = GetObjectPrefabName(objectToSell);
            if (string.IsNullOrEmpty(objectPrefabName))
            {
                Debug.LogWarning($"PlaceableObjectSeller: Could not find data for object {objectToSell.name} in placement service.");
                return;
            }

            // Получаем ProductConfig для объекта
            ProductConfig productConfig = _productCatalogService?.GetProductConfigByID(objectPrefabName);
            if (productConfig == null)
            {
                Debug.LogWarning($"PlaceableObjectSeller: Could not find ProductConfig for '{objectPrefabName}'. Object cannot be sold.");
                return;
            }

            // Рассчитываем цену продажи (50% от закупочной цены)
            float sellPrice = productConfig.PurchasePrice * _sellPriceMultiplier;

            // Добавляем деньги игроку
            if (_playerDataService != null)
            {
                _playerDataService.AdjustMoney(sellPrice);
                Debug.Log($"PlaceableObjectSeller: Sold {productConfig.ProductName} for ${sellPrice:F0} (50% of ${productConfig.PurchasePrice:F0})");
            }

            // Уничтожаем объект со сцены
            DestroyObject(objectToSell, objectPrefabName);
        }

        /// <summary>
        /// Получает PrefabName объекта из данных PlacementService
        /// </summary>
        private string GetObjectPrefabName(GameObject gameObject)
        {
            var placedObjectsData = _placementService?.GetPlacedObjectsData();
            if (placedObjectsData == null) return null;

            Vector3 objectPosition = gameObject.transform.position;
            
            foreach (var data in placedObjectsData)
            {
                // Сравниваем позиции объектов (с некоторой погрешностью)
                if (Vector3.Distance(data.Position, objectPosition) < 0.1f)
                {
                    return data.PrefabName;
                }
            }

            return null;
        }

        /// <summary>
        /// Уничтожает объект и удаляет его из данных размещения
        /// </summary>
        private void DestroyObject(GameObject objectToDestroy, string objectPrefabName)
        {
            // Собираем товары с полок перед уничтожением
            CollectProductsFromShelves(objectToDestroy);
            
            // НОВОЕ: Проверяем, является ли объект кассой, и уведомляем клиентов
            HandleCashDeskDestruction(objectToDestroy);
            
            // Используем новый метод для удаления одного объекта вместо перестройки всего
            bool removed = _placementService?.RemovePlacedObject(objectToDestroy) ?? false;
            
            if (!removed)
            {
                Debug.LogWarning($"PlaceableObjectSeller: Failed to remove object '{objectPrefabName}' from PlacementService tracking. Object will still be destroyed.");
            }

            // Уничтожаем объект
            Destroy(objectToDestroy);
            Debug.Log($"PlaceableObjectSeller: Object '{objectPrefabName}' destroyed and removed from placement data");
        }
        
        /// <summary>
        /// Обрабатывает уничтожение кассы - уведомляет всех клиентов в очереди
        /// </summary>
        private void HandleCashDeskDestruction(GameObject objectToDestroy)
        {
            // Проверяем, является ли объект кассой
            var cashDeskController = objectToDestroy.GetComponent<CashDeskController>();
            if (cashDeskController == null)
            {
                // Возможно, компонент на дочернем объекте
                cashDeskController = objectToDestroy.GetComponentInChildren<CashDeskController>();
            }
            if (cashDeskController == null)
            {
                // Возможно, компонент на родительском объекте
                cashDeskController = objectToDestroy.GetComponentInParent<CashDeskController>();
            }
            
            if (cashDeskController != null)
            {
                Debug.Log($"PlaceableObjectSeller: Detected cash desk destruction - {objectToDestroy.name}. Notifying customers.");
                
                // Получаем всех клиентов из очереди
                var queue = cashDeskController.GetCustomersInQueue();
                var approaching = cashDeskController.GetApproachingCustomers();
                
                Debug.Log($"PlaceableObjectSeller: Found {queue.Count} customers in queue and {approaching.Count} approaching customers");
                
                // Уведомляем всех клиентов в очереди
                foreach (var customer in queue)
                {
                    if (customer != null)
                    {
                        var customerController = customer.GetComponent<CustomerController>();
                        if (customerController != null)
                        {
                            Debug.Log($"PlaceableObjectSeller: Notifying customer {customerController.GetCustomerData().CustomerName} about cash desk destruction");
                            customerController.OnCashDeskDestroyed();
                        }
                    }
                }
                
                // Уведомляем всех приближающихся клиентов
                foreach (var customer in approaching)
                {
                    if (customer != null)
                    {
                        var customerController = customer.GetComponent<CustomerController>();
                        if (customerController != null)
                        {
                            Debug.Log($"PlaceableObjectSeller: Notifying approaching customer {customerController.GetCustomerData().CustomerName} about cash desk destruction");
                            customerController.OnCashDeskDestroyed();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Собирает товары с полок (обычных и многоуровневых) и создает коробки
        /// </summary>
        private void CollectProductsFromShelves(GameObject shelfObject)
        {
            if (_boxManagerService == null)
            {
                Debug.LogWarning("PlaceableObjectSeller: BoxManagerService not available, cannot create boxes for products");
                return;
            }

            Vector3 shelfPosition = shelfObject.transform.position;
            int boxesCreated = 0;

            // Проверяем многоуровневые полки
            MultiLevelShelfController multiShelf = shelfObject.GetComponent<MultiLevelShelfController>();
            if (multiShelf != null)
            {
                boxesCreated += CollectFromMultiLevelShelf(multiShelf, shelfPosition);
            }

            // Проверяем обычные полки
            ShelfController singleShelf = shelfObject.GetComponent<ShelfController>();
            if (singleShelf != null)
            {
                boxesCreated += CollectFromSingleShelf(singleShelf, shelfPosition);
            }

            if (boxesCreated > 0)
            {
                Debug.Log($"PlaceableObjectSeller: Created {boxesCreated} boxes with products from sold shelf");
            }
        }

        /// <summary>
        /// Собирает товары с многоуровневой полки
        /// </summary>
        private int CollectFromMultiLevelShelf(MultiLevelShelfController multiShelf, Vector3 basePosition)
        {
            int boxesCreated = 0;
            var shelfLevels = multiShelf.ShelfLevels;
            
            if (shelfLevels == null) return 0;

            foreach (var level in shelfLevels)
            {
                if (level == null || level.IsEmpty) continue;

                ProductConfig product = level.AcceptedProduct;
                int itemCount = level.CurrentItemCount;

                if (product != null && itemCount > 0)
                {
                    // Создаем коробку с товарами с этого уровня
                    BoxData boxData = new BoxData(product, itemCount);
                    Vector3 spawnPosition = GetRandomBoxSpawnPosition(basePosition);
                    
                    _boxManagerService.CreateBox(boxData, spawnPosition, true);
                    boxesCreated++;

                    Debug.Log($"PlaceableObjectSeller: Created box with {itemCount}x {product.ProductName} from shelf level");
                }
            }

            return boxesCreated;
        }

        /// <summary>
        /// Собирает товары с обычной полки
        /// </summary>
        private int CollectFromSingleShelf(ShelfController singleShelf, Vector3 basePosition)
        {
            if (singleShelf.acceptedProduct == null || singleShelf.GetCurrentItemCount() <= 0)
                return 0;

            ProductConfig product = singleShelf.acceptedProduct;
            int itemCount = singleShelf.GetCurrentItemCount();

            // Создаем коробку с товарами с полки
            BoxData boxData = new BoxData(product, itemCount);
            Vector3 spawnPosition = GetRandomBoxSpawnPosition(basePosition);
            
            _boxManagerService.CreateBox(boxData, spawnPosition, true);

            Debug.Log($"PlaceableObjectSeller: Created box with {itemCount}x {product.ProductName} from single shelf");
            return 1;
        }

        /// <summary>
        /// Генерирует случайную позицию для спавна коробки около полки
        /// </summary>
        private Vector3 GetRandomBoxSpawnPosition(Vector3 basePosition)
        {
            // Генерируем случайную позицию в радиусе вокруг полки
            Vector2 randomCircle = Random.insideUnitCircle * _boxSpawnRadius;
            Vector3 spawnPosition = basePosition + new Vector3(randomCircle.x, _boxSpawnHeight, randomCircle.y);
            
            return spawnPosition;
        }
    }
} 