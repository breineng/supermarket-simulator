using UnityEngine;
using System; // Added for Action
using Supermarket.Data; // Для PlayerHeldBoxData

namespace Supermarket.Services.Game // Помещаем в тот же неймспейс, что и DeliveryService
{
    public class PlayerHandService : IPlayerHandService
    { 
        private readonly IProductCatalogService _productCatalogService;
        private BoxData _heldBoxData;
        private bool _isBoxOpen = false; // New: State for open/closed box
        private Transform _boxVisualsParent; // Точка привязки для визуальной коробки

        public event Action OnHandContentChanged; // Changed to simple Action
        public event Action OnBoxStateChanged; // New: Event for box state
        
        // Новые события для визуализации
        public event Action<BoxData> OnBoxPickedUp;
        public event Action OnBoxDropped;
        public event Action<ProductConfig, int> OnItemConsumedFromBox;
        public event Action<ProductConfig, int> OnItemAddedToBox;

        // Конструктор для внедрения зависимостей
        public PlayerHandService(IProductCatalogService productCatalogService)
        {
            _productCatalogService = productCatalogService;
            Debug.Log("PlayerHandService: Created with ProductCatalogService dependency");
        }

        public bool IsHoldingBox()
        {
            return _heldBoxData != null;
        }

        public BoxData GetHeldBoxData()
        {
            return _heldBoxData;
        }

        public ProductConfig GetProductInHand()
        {
            return _heldBoxData != null ? _heldBoxData.ProductInBox : null;
        }

        public int GetQuantityInHand() // Implemented GetQuantityInHand
        {
            return _heldBoxData != null ? _heldBoxData.Quantity : 0;
        }

        public Transform GetBoxVisualsParent()
        {
            return _boxVisualsParent;
        }

        public void SetBoxVisualsParent(Transform visualsParent)
        {
            _boxVisualsParent = visualsParent;
            Debug.Log($"PlayerHandService: Box visuals parent set to {_boxVisualsParent?.name ?? "null"}");
        }

        public void PickupBox(BoxData boxData)
        {
            if (boxData == null) // ProductInBox может быть null для пустой коробки на земле
            {
                Debug.LogError("PlayerHandService: Tried to pick up null BoxData.");
                return;
            }
            
            if (boxData.Quantity <= 0) // Если коробка на земле пуста (по количеству)
            {
                // В руках она становится "полностью пустой" коробкой, независимо от ее предыдущего ProductInBox на земле
                _heldBoxData = new BoxData(null, 0);
                Debug.Log("Player picked up an empty (by quantity) box from ground. It is now a generic empty box in hand.");
            }
            else if (boxData.ProductInBox == null) // Если у коробки на земле нет типа (маловероятно, но для полноты)
            {
                 _heldBoxData = new BoxData(null, 0);
                 Debug.LogWarning("PlayerHandService: Picked up a box from ground with no ProductInBox type. It is now a generic empty box in hand.");
            }
            else // Стандартный подбор коробки с товаром
            {
                _heldBoxData = new BoxData(boxData.ProductInBox, boxData.Quantity);
                Debug.Log($"Player picked up box: {_heldBoxData.ProductInBox.ProductName} x {_heldBoxData.Quantity}. Box is closed.");
            }
            
            _isBoxOpen = false; 
            OnHandContentChanged?.Invoke(); 
            OnBoxStateChanged?.Invoke();
            OnBoxPickedUp?.Invoke(_heldBoxData); // Новое событие
        }

        public void ConsumeItemFromHand(int amount = 1)
        {
            if (_heldBoxData == null || amount <= 0)
            {
                return;
            }
            
            // Если ProductInBox УЖЕ null (т.е. это "полностью пустая" коробка), из нее нельзя расходовать.
            if (_heldBoxData.ProductInBox == null) 
            {
                Debug.LogWarning("PlayerHandService: Tried to consume from a generic empty box. No items to consume.");
                return;
            }
           
            if (_heldBoxData.Quantity < amount)
            {
                Debug.LogWarning($"PlayerHandService: Tried to consume {amount} of {_heldBoxData.ProductInBox.ProductName}, but only have {_heldBoxData.Quantity}.");
                return;
            }

            ProductConfig consumedProduct = _heldBoxData.ProductInBox; // Запомним продукт до возможного обнуления
            string consumedProductName = consumedProduct.ProductName; // Запомним имя до возможного обнуления ProductInBox
            _heldBoxData.Quantity -= amount;
            Debug.Log($"Consumed {amount} of {consumedProductName} from hand. Remaining: {_heldBoxData.Quantity}");

            // Вызываем событие об изъятии товара
            OnItemConsumedFromBox?.Invoke(consumedProduct, amount);

            if (_heldBoxData.Quantity <= 0)
            {
                Debug.Log($"PlayerHandService: Box of {consumedProductName} is now empty. Converting to generic empty box in hand.");
                _heldBoxData = new BoxData(null, 0); // Становится "полностью пустой" коробкой (с ProductInBox = null)
            }

            OnHandContentChanged?.Invoke();
        }

        public void ClearHand()
        {
            if (_heldBoxData != null)
            {
                Debug.Log($"Player hand cleared. Was holding: {_heldBoxData.ProductInBox?.ProductName}");
                _heldBoxData = null;
                _isBoxOpen = false;
                OnHandContentChanged?.Invoke(); // Invoked without arguments
                OnBoxStateChanged?.Invoke(); // Invoked without arguments
                OnBoxDropped?.Invoke(); // Новое событие
            }
        }

        public bool IsBoxOpen()
        {
            return _heldBoxData != null && _isBoxOpen;
        }

        public void OpenBox()
        {
            if (_heldBoxData != null && !_isBoxOpen)
            {
                _isBoxOpen = true;
                if (_heldBoxData.ProductInBox != null)
                {
                    Debug.Log($"PlayerHandService: Box of {_heldBoxData.ProductInBox.ProductName} opened.");
                }
                else
                {
                    Debug.Log("PlayerHandService: Generic empty box opened.");
                }
                OnBoxStateChanged?.Invoke();
            }
            else if (_heldBoxData == null)
            {
                Debug.LogWarning("PlayerHandService: Cannot open box, not holding one.");
            }
        }

        public void CloseBox()
        {
            if (_heldBoxData != null && _isBoxOpen)
            {
                _isBoxOpen = false;
                if (_heldBoxData.ProductInBox != null)
                {
                    Debug.Log($"PlayerHandService: Box of {_heldBoxData.ProductInBox.ProductName} closed.");
                }
                else
                {
                    Debug.Log("PlayerHandService: Generic empty box closed.");
                }
                OnBoxStateChanged?.Invoke();
            }
            else if (_heldBoxData == null)
            {
                Debug.LogWarning("PlayerHandService: Cannot close box, not holding one.");
            }
        }

        public void AddItemToOpenBox(ProductConfig product, int amount)
        {
            if (!IsBoxOpen())
            {
                Debug.LogWarning("PlayerHandService: Cannot add item, box is not open or not holding one.");
                return;
            }

            if (product == null)
            {
                Debug.LogError("PlayerHandService: Cannot add item to box. The provided product is null.");
                return;
            }
            
            if (amount <= 0)
            {
                Debug.LogWarning("PlayerHandService: Amount to add must be positive.");
                return;
            }

            // Если коробка в руках "полностью пустая" (ProductInBox == null)
            if (_heldBoxData.ProductInBox == null)
            {
                // Проверяем, не превышает ли добавляемое количество максимальную вместимость
                if (amount > product.ItemsPerBox)
                {
                    Debug.LogWarning($"PlayerHandService: Cannot add {amount} items of {product.ProductName}. Box capacity is {product.ItemsPerBox}. Adding only {product.ItemsPerBox} items.");
                    amount = product.ItemsPerBox;
                }
                
                _heldBoxData = new BoxData(product, amount);
                Debug.Log($"PlayerHandService: Added {amount} of {product.ProductName} to generic empty box. It is now a '{product.ProductName}' box. New quantity: {_heldBoxData.Quantity}/{product.ItemsPerBox}");
            }
            // Если коробка не пуста и тип совпадает.
            else if (_heldBoxData.ProductInBox == product)
            {
                // Проверяем, не превысит ли новое количество максимальную вместимость
                int newTotalQuantity = _heldBoxData.Quantity + amount;
                if (newTotalQuantity > product.ItemsPerBox)
                {
                    int actualAmountToAdd = product.ItemsPerBox - _heldBoxData.Quantity;
                    if (actualAmountToAdd <= 0)
                    {
                        Debug.LogWarning($"PlayerHandService: Cannot add {product.ProductName} to box. Box is already full ({_heldBoxData.Quantity}/{product.ItemsPerBox}).");
                        return;
                    }
                    
                    Debug.LogWarning($"PlayerHandService: Cannot add {amount} items of {product.ProductName}. Box would exceed capacity ({product.ItemsPerBox}). Adding only {actualAmountToAdd} items.");
                    amount = actualAmountToAdd;
                }
                
                _heldBoxData.Quantity += amount;
                Debug.Log($"PlayerHandService: Added {amount} of {product.ProductName} to open box '{_heldBoxData.ProductInBox.ProductName}'. New quantity: {_heldBoxData.Quantity}/{product.ItemsPerBox}");
            }
            // Если коробка не пуста, но тип НЕ совпадает.
            else
            {
                Debug.LogError($"PlayerHandService: Cannot add item to box. Product mismatch. Box holds '{_heldBoxData.ProductInBox.ProductName}', tried to add '{product.ProductName}'.");
                return;
            }
            
            // Вызываем событие о добавлении товара
            OnItemAddedToBox?.Invoke(product, amount);
            OnHandContentChanged?.Invoke();
        }
        
        public bool CanAddItemToOpenBox(ProductConfig product, int amount = 1)
        {
            if (!IsBoxOpen())
                return false;

            if (product == null || amount <= 0)
                return false;

            // Если коробка в руках "полностью пустая" (ProductInBox == null)
            if (_heldBoxData.ProductInBox == null)
            {
                return amount <= product.ItemsPerBox;
            }
            // Если коробка не пуста и тип совпадает
            else if (_heldBoxData.ProductInBox == product)
            {
                int newTotalQuantity = _heldBoxData.Quantity + amount;
                return newTotalQuantity <= product.ItemsPerBox;
            }
            // Если коробка не пуста, но тип НЕ совпадает
            else
            {
                return false;
            }
        }

        // Методы для сохранения/восстановления
        public PlayerHeldBoxData GetSaveData()
        {
            if (_heldBoxData == null)
            {
                return null; // Нет коробки в руках
            }

            return new PlayerHeldBoxData
            {
                ProductType = _heldBoxData.ProductInBox?.ProductID ?? "", // ProductID для сохранения
                ItemCount = _heldBoxData.Quantity,
                IsOpen = _isBoxOpen
            };
        }

        public void RestoreFromSaveData(PlayerHeldBoxData saveData)
        {
            if (saveData == null)
            {
                // Нет коробки для восстановления
                ClearHand();
                Debug.Log("PlayerHandService: No held box data to restore - hands are empty.");
                return;
            }

            // Нужно найти ProductConfig по ID
            ProductConfig product = null;
            if (!string.IsNullOrEmpty(saveData.ProductType))
            {
                // Ищем ProductConfig по ProductID через внедренный сервис
                if (_productCatalogService != null)
                {
                    product = _productCatalogService.GetProductConfigByID(saveData.ProductType);
                    if (product == null)
                    {
                        Debug.LogWarning($"PlayerHandService: Product ID '{saveData.ProductType}' not found in catalog during restore");
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerHandService: IProductCatalogService is null during restore");
                }
            }

            // Создаем BoxData для восстановления
            _heldBoxData = new BoxData(product, saveData.ItemCount);
            _isBoxOpen = saveData.IsOpen;

            // Уведомляем о восстановлении
            OnHandContentChanged?.Invoke();
            OnBoxStateChanged?.Invoke();
            OnBoxPickedUp?.Invoke(_heldBoxData);

            string productName = product?.ProductName ?? "Empty Generic Box";
            string openState = _isBoxOpen ? "open" : "closed";
            Debug.Log($"PlayerHandService: Restored held box - {productName} x{saveData.ItemCount} ({openState})");
        }
    }
} 