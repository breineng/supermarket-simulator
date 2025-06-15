using UnityEngine;
using System.Collections.Generic;

namespace Supermarket.Interactables
{
    /// <summary>
    /// Компонент для управления одним уровнем полки
    /// Каждый уровень может содержать только один тип товара
    /// </summary>
    public class ShelfLevel : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxCapacity = 10;
        [SerializeField] private List<Transform> itemSpawnPoints = new List<Transform>();
        
        [Header("Price Tag")]
        [SerializeField] private Transform priceTagPosition;
        [SerializeField] private GameObject priceTagPrefab; // Префаб ценника
        
        [Header("Visual Feedback")]
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.3f); // Желтоватая подсветка
        
        // Текущее состояние уровня
        private ProductConfig acceptedProduct; // Тип товара на этом уровне
        private int currentItemCount = 0;
        private int itemsInAnimation = 0; // Счетчик товаров в анимации
        private int itemsFlyingToShelf = 0; // Счетчик товаров, летящих К полке (из коробки)
        private int itemsFlyingFromShelf = 0; // Счетчик товаров, летящих ОТ полки (в коробку)
        private List<GameObject> spawnedItemInstances = new List<GameObject>();
        
        // Компонент ценника (создается динамически)
        private PriceTagController priceTag;
        
        // Визуальные компоненты для подсветки
        private Renderer levelRenderer;
        private Material originalMaterial;
        private bool isHighlighted = false;
        
        public ProductConfig AcceptedProduct => acceptedProduct;
        public int CurrentItemCount => currentItemCount;
        public int MaxCapacity => maxCapacity;
        public bool IsEmpty => currentItemCount == 0;
        public bool IsFull => currentItemCount >= maxCapacity;
        public List<Transform> ItemSpawnPoints => itemSpawnPoints; // Публичный доступ к точкам спавна
        
        /// <summary>
        /// Проверяет, можно ли принять товар из коробки игрока с учетом летящих товаров
        /// Этот метод используется в PlayerBoxController для предотвращения потери товаров
        /// </summary>
        public bool CanAcceptItemFromBox(ProductConfig product)
        {
            if (product == null || !product.CanBePlacedOnShelf)
                return false;
            
            // Проверяем заполненность с учетом летящих товаров
            int totalExpectedItems = currentItemCount + itemsFlyingToShelf;
            if (totalExpectedItems >= maxCapacity)
                return false;
            
            // Если уровень пуст, можно разместить любой подходящий товар
            if (IsEmpty)
                return true;
            
            // Если уровень не пуст, можно разместить только такой же товар
            return product == acceptedProduct;
        }
        
        private void Awake()
        {
            // Находим Renderer для визуальной подсветки
            levelRenderer = GetComponentInChildren<Renderer>();
            if (levelRenderer != null)
            {
                originalMaterial = levelRenderer.material;
            }
            
            // Если префаб ценника не назначен, пытаемся загрузить его
            if (priceTagPrefab == null)
            {
                // Сначала пытаемся загрузить из Resources (если префаб туда скопирован)
                priceTagPrefab = Resources.Load<GameObject>("UI/PriceTag");
                
                // Если не нашли в Resources, используем прямую загрузку через Unity Editor
                #if UNITY_EDITOR
                if (priceTagPrefab == null)
                {
                    priceTagPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/PriceTag.prefab");
                    if (priceTagPrefab != null)
                    {
                        Debug.Log("ShelfLevel: Loaded PriceTag prefab from AssetDatabase");
                    }
                }
                #endif
                
                if (priceTagPrefab == null)
                {
                    Debug.LogWarning("ShelfLevel: PriceTag prefab not found. Please assign it in the inspector or place it in Resources/UI/", this);
                }
            }
        }
        
        /// <summary>
        /// Проверяет, можно ли разместить товар на этом уровне
        /// </summary>
        public bool CanPlaceProduct(ProductConfig product)
        {
            if (product == null || !product.CanBePlacedOnShelf || IsFull)
                return false;
            
            // Если уровень пуст, можно разместить любой подходящий товар
            if (IsEmpty)
                return true;
            
            // Если уровень не пуст, можно разместить только такой же товар
            return product == acceptedProduct;
        }
        
        /// <summary>
        /// Размещает товар на уровне
        /// </summary>
        public bool PlaceProduct(ProductConfig product)
        {
            if (!CanPlaceProduct(product))
                return false;
            
            // Если уровень пуст, устанавливаем тип товара
            if (IsEmpty)
            {
                acceptedProduct = product;
                CreateOrUpdatePriceTag();
            }
            
            currentItemCount++;
            UpdateVisuals();
            return true;
        }
        
        /// <summary>
        /// Проверяет, можно ли забрать товар с этого уровня
        /// </summary>
        public bool CanTakeProduct(ProductConfig productInBox = null)
        {
            // Только проверяем, есть ли вообще товары на уровне
            if (currentItemCount <= 0)
                return false;
            
            // Если коробка пустая, можно забрать любой товар
            if (productInBox == null)
                return true;
            
            // Если в коробке есть товар, можно забрать только такой же
            return productInBox == acceptedProduct;
        }
        
        /// <summary>
        /// Забирает товар с уровня
        /// </summary>
        public ProductConfig TakeProduct()
        {
            // Проверяем, есть ли товары на уровне физически
            if (currentItemCount <= 0)
            {
                Debug.Log($"ShelfLevel '{gameObject.name}': Cannot take item - no items on level (count: {currentItemCount})");
                return null;
            }
            
            // Увеличиваем счетчик товаров, летящих ОТ полки
            itemsFlyingFromShelf++;
            itemsInAnimation++; // Оставляем для обратной совместимости
            
            ProductConfig takenProduct = acceptedProduct;
            currentItemCount--;
            
            // Если уровень опустел, сбрасываем тип товара
            if (IsEmpty)
            {
                acceptedProduct = null;
                RemovePriceTag();
            }
            
            UpdateVisuals();
            Debug.Log($"ShelfLevel '{gameObject.name}': Took product. Current count: {currentItemCount}, Items flying from shelf: {itemsFlyingFromShelf}");
            return takenProduct;
        }
        
        /// <summary>
        /// Покупатель забирает товар с уровня
        /// </summary>
        /// <returns>true если товар был успешно взят, false если товара нет</returns>
        public bool CustomerTakeProduct()
        {
            if (!IsEmpty)
            {
                currentItemCount--;
                
                if (IsEmpty)
                {
                    acceptedProduct = null;
                    RemovePriceTag();
                }
                
                UpdateVisuals();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Обновляет визуальное отображение товаров
        /// </summary>
        private void UpdateVisuals()
        {
            // Синхронизируем количество заспавненных объектов с currentItemCount
            while (spawnedItemInstances.Count < currentItemCount)
            {
                if (acceptedProduct == null || acceptedProduct.Prefab == null)
                {
                    Debug.LogError($"Cannot spawn item visual: acceptedProduct is null or has no Prefab. Level: {gameObject.name}", this);
                    break;
                }
                
                if (spawnedItemInstances.Count < itemSpawnPoints.Count)
                {
                    Transform spawnPoint = itemSpawnPoints[spawnedItemInstances.Count];
                    GameObject newItemInstance = Instantiate(acceptedProduct.Prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                    spawnedItemInstances.Add(newItemInstance);
                }
                else
                {
                    Debug.LogWarning($"Not enough spawn points on level '{gameObject.name}' to display all items.", this);
                    break;
                }
            }
            
            while (spawnedItemInstances.Count > currentItemCount)
            {
                if (spawnedItemInstances.Count > 0)
                {
                    GameObject instanceToRemove = spawnedItemInstances[spawnedItemInstances.Count - 1];
                    spawnedItemInstances.RemoveAt(spawnedItemInstances.Count - 1);
                    if (instanceToRemove != null)
                    {
                        Destroy(instanceToRemove);
                    }
                }
            }
        }
        
        /// <summary>
        /// Создает или обновляет ценник
        /// </summary>
        private void CreateOrUpdatePriceTag()
        {
            if (priceTag == null && priceTagPosition != null && priceTagPrefab != null)
            {
                // Создаем ценник из префаба
                GameObject priceTagObject = Instantiate(priceTagPrefab, priceTagPosition.position, priceTagPosition.rotation, priceTagPosition);
                priceTag = priceTagObject.GetComponent<PriceTagController>();
                
                if (priceTag == null)
                {
                    Debug.LogError($"ShelfLevel: PriceTagController component not found on price tag prefab!", this);
                    Destroy(priceTagObject);
                    return;
                }
            }
            
            if (priceTag != null && acceptedProduct != null)
            {
                priceTag.UpdatePriceTag(acceptedProduct);
            }
        }
        
        /// <summary>
        /// Удаляет ценник
        /// </summary>
        private void RemovePriceTag()
        {
            if (priceTag != null)
            {
                Destroy(priceTag.gameObject);
                priceTag = null;
            }
        }
        
        /// <summary>
        /// Восстанавливает состояние уровня из сохраненных данных
        /// </summary>
        public void RestoreState(ProductConfig product, int itemCount)
        {
            Debug.Log($"ShelfLevel '{gameObject.name}': RestoreState called - Product: '{product?.ProductName ?? "NULL"}', ItemCount: {itemCount}");
            
            // Очищаем текущее состояние
            ClearLevel();
            
            // Устанавливаем новое состояние
            acceptedProduct = product;
            currentItemCount = Mathf.Clamp(itemCount, 0, maxCapacity);
            
            // Создаем или обновляем ценник
            if (acceptedProduct != null)
            {
                CreateOrUpdatePriceTag();
            }
            
            // Обновляем визуалы
            UpdateVisuals();
            
            Debug.Log($"ShelfLevel '{gameObject.name}': State restored - {acceptedProduct?.ProductName ?? "Empty"} x{currentItemCount}");
        }
        
        /// <summary>
        /// Получает позицию следующего свободного слота на уровне
        /// Используется для анимации размещения товара
        /// </summary>
        public Vector3 GetNextAvailableSlotPosition()
        {
            if (itemSpawnPoints.Count == 0)
            {
                // Если нет назначенных точек спавна, возвращаем позицию уровня
                Debug.LogWarning($"ShelfLevel '{gameObject.name}': No itemSpawnPoints configured, using level position + offset");
                return transform.position + Vector3.up * 0.1f;
            }
            
            // Рассчитываем индекс для товара, который сейчас будет размещаться
            // itemsFlyingToShelf уже увеличен для текущего товара
            int targetIndexForItemBeingPlaced = currentItemCount + itemsFlyingToShelf - 1;
            int nextSlotIndex = Mathf.Clamp(targetIndexForItemBeingPlaced, 0, itemSpawnPoints.Count - 1);
            Vector3 slotPosition = itemSpawnPoints[nextSlotIndex].position;
            
            Debug.Log($"ShelfLevel '{gameObject.name}': GetNextAvailableSlotPosition - currentItemCount: {currentItemCount}, itemsFlyingToShelf: {itemsFlyingToShelf}, calculatedTargetIndex: {targetIndexForItemBeingPlaced}, finalSlotIndex: {nextSlotIndex}, position: {slotPosition}");
            return slotPosition;
        }
        
        /// <summary>
        /// Получает поворот следующего свободного слота на уровне
        /// Используется для анимации размещения товара
        /// </summary>
        public Quaternion GetNextAvailableSlotRotation()
        {
            if (itemSpawnPoints.Count == 0)
            {
                // Если нет назначенных точек спавна, возвращаем стандартный поворот
                Debug.LogWarning($"ShelfLevel '{gameObject.name}': No itemSpawnPoints configured, using identity rotation");
                return Quaternion.identity;
            }
            
            // Рассчитываем индекс для товара, который сейчас будет размещаться
            // itemsFlyingToShelf уже увеличен для текущего товара
            int targetIndexForItemBeingPlaced = currentItemCount + itemsFlyingToShelf - 1;
            int nextSlotIndex = Mathf.Clamp(targetIndexForItemBeingPlaced, 0, itemSpawnPoints.Count - 1);
            Quaternion slotRotation = itemSpawnPoints[nextSlotIndex].rotation;
            
            Debug.Log($"ShelfLevel '{gameObject.name}': GetNextAvailableSlotRotation - currentItemCount: {currentItemCount}, itemsFlyingToShelf: {itemsFlyingToShelf}, calculatedTargetIndex: {targetIndexForItemBeingPlaced}, finalSlotIndex: {nextSlotIndex}, rotation: {slotRotation}");
            return slotRotation;
        }
        
        /// <summary>
        /// Получает позицию последнего занятого слота на уровне
        /// Используется для анимации взятия товара
        /// </summary>
        public Vector3 GetLastOccupiedSlotPosition()
        {
            // currentItemCount has ALREADY been decremented in TakeProduct().
            // So, its current value is the 0-based index of the slot
            // from which an item was just visually and logically removed.
            int indexOfTakenItem = currentItemCount;

            if (itemSpawnPoints.Count == 0)
            {
                Debug.LogWarning($"ShelfLevel '{gameObject.name}': No itemSpawnPoints configured, using level position + offset for taken item animation.");
                return transform.position + Vector3.up * 0.1f;
            }

            // Ensure the index is valid for the itemSpawnPoints list.
            if (indexOfTakenItem >= itemSpawnPoints.Count || indexOfTakenItem < 0)
            {
                Debug.LogError($"ShelfLevel '{gameObject.name}': GetLastOccupiedSlotPosition - Invalid index for taken item. currentItemCount (post-decrement): {currentItemCount}, spawnPoints count: {itemSpawnPoints.Count}. Using level position as fallback.");
                return transform.position + Vector3.up * 0.1f; // Fallback to prevent error
            }

            Vector3 slotPosition = itemSpawnPoints[indexOfTakenItem].position;
            
            Debug.Log($"ShelfLevel '{gameObject.name}': GetLastOccupiedSlotPosition - currentItemCount (post-decrement): {currentItemCount}, effectiveIndexOfTakenItem: {indexOfTakenItem}, position: {slotPosition}");
            return slotPosition;
        }
        
        /// <summary>
        /// Получает поворот последней занятой точки спавна
        /// </summary>
        public Quaternion GetLastOccupiedSlotRotation()
        {
            // currentItemCount has ALREADY been decremented in TakeProduct().
            // So, its current value is the 0-based index of the slot
            // from which an item was just visually and logically removed.
            int indexOfTakenItem = currentItemCount;

            if (itemSpawnPoints.Count == 0)
            {
                Debug.LogWarning($"ShelfLevel '{gameObject.name}': No itemSpawnPoints configured, using identity rotation for taken item animation.");
                return Quaternion.identity;
            }

            // Ensure the index is valid.
            if (indexOfTakenItem >= itemSpawnPoints.Count || indexOfTakenItem < 0)
            {
                Debug.LogError($"ShelfLevel '{gameObject.name}': GetLastOccupiedSlotRotation - Invalid index for taken item. currentItemCount (post-decrement): {currentItemCount}, spawnPoints count: {itemSpawnPoints.Count}. Using identity rotation as fallback.");
                return Quaternion.identity; // Fallback
            }

            Quaternion slotRotation = itemSpawnPoints[indexOfTakenItem].rotation;
            
            Debug.Log($"ShelfLevel '{gameObject.name}': GetLastOccupiedSlotRotation - currentItemCount (post-decrement): {currentItemCount}, effectiveIndexOfTakenItem: {indexOfTakenItem}, rotation: {slotRotation}");
            return slotRotation;
        }
        
        /// <summary>
        /// Очищает уровень от всех товаров
        /// </summary>
        public void ClearLevel()
        {
            foreach (GameObject instance in spawnedItemInstances)
            {
                if (instance != null) Destroy(instance);
            }
            spawnedItemInstances.Clear();
            currentItemCount = 0;
            itemsInAnimation = 0; // Сбрасываем счетчик анимаций при очистке
            itemsFlyingToShelf = 0; // Сбрасываем счетчик товаров, летящих к полке
            itemsFlyingFromShelf = 0; // Сбрасываем счетчик товаров, летящих от полки
            acceptedProduct = null;
            RemovePriceTag();
        }
        
        /// <summary>
        /// Начинается анимация размещения товара НА полку (из коробки)
        /// Вызывается перед началом анимации для резервирования слота
        /// </summary>
        public void StartPlacementAnimation()
        {
            itemsFlyingToShelf++;
            itemsInAnimation++; // Для обратной совместимости
            Debug.Log($"ShelfLevel '{gameObject.name}': Started placement animation. Items flying to shelf: {itemsFlyingToShelf}");
        }
        
        /// <summary>
        /// Завершается анимация размещения товара НА полку (из коробки)
        /// </summary>
        public void CompletePlacementAnimation()
        {
            if (itemsFlyingToShelf > 0)
            {
                itemsFlyingToShelf--;
            }
            if (itemsInAnimation > 0)
            {
                itemsInAnimation--;
            }
            Debug.Log($"ShelfLevel '{gameObject.name}': Completed placement animation. Items flying to shelf: {itemsFlyingToShelf}");
        }
        
        /// <summary>
        /// Завершается анимация взятия товара С полки (в коробку)
        /// </summary>
        public void CompleteTakeFromShelfAnimation()
        {
            if (itemsFlyingFromShelf > 0)
            {
                itemsFlyingFromShelf--;
            }
            if (itemsInAnimation > 0)
            {
                itemsInAnimation--;
            }
            Debug.Log($"ShelfLevel '{gameObject.name}': Completed take from shelf animation. Items flying from shelf: {itemsFlyingFromShelf}");
        }
        
        /// <summary>
        /// Включает визуальную подсветку уровня
        /// </summary>
        public void Highlight()
        {
            if (!isHighlighted && levelRenderer != null)
            {
                isHighlighted = true;
                
                if (highlightMaterial != null)
                {
                    levelRenderer.material = highlightMaterial;
                }
                else
                {
                    // Если нет специального материала, меняем цвет существующего
                    Material mat = new Material(originalMaterial);
                    mat.color = highlightColor;
                    levelRenderer.material = mat;
                }
            }
        }
        
        /// <summary>
        /// Отключает визуальную подсветку уровня
        /// </summary>
        public void RemoveHighlight()
        {
            if (isHighlighted && levelRenderer != null)
            {
                isHighlighted = false;
                levelRenderer.material = originalMaterial;
            }
        }
        
        /// <summary>
        /// Вызывается после завершения анимации взятия товара
        /// </summary>
        public void CompleteTakeAnimation()
        {
            if (itemsInAnimation > 0)
            {
                itemsInAnimation--;
                Debug.Log($"ShelfLevel '{gameObject.name}': Completed take animation. Items in animation: {itemsInAnimation}");
            }
        }
        
        /// <summary>
        /// Вызывается, когда анимация взятия товара С полки (в коробку) была прервана
        /// </summary>
        public void CancelTakeFromShelfAnimation(ProductConfig product)
        {
            // Уменьшаем счетчики, так как товар не долетел до коробки
            // ProductConfig передается для возможной будущей логики, если понадобится знать, какой именно товар отменен
            if (itemsFlyingFromShelf > 0)
            {
                itemsFlyingFromShelf--;
            }
            if (itemsInAnimation > 0) // Старый счетчик тоже уменьшаем
            {
                itemsInAnimation--;
            }
            Debug.Log($"ShelfLevel '{gameObject.name}': Take from shelf animation CANCELLED for {product?.ProductName ?? "Unknown Product"}. Items flying from shelf: {itemsFlyingFromShelf}");
            
            // Важно: currentItemCount НЕ увеличиваем обратно, так как товар считается "потерянным"
            // Если бы он должен был вернуться на полку, логика была бы сложнее (найти свободный слот, обновить визуалы и т.д.)
        }

        /// <summary>
        /// Вызывается, когда анимация размещения товара НА полку (из коробки) была прервана
        /// </summary>
        public void CancelPlacementOnShelfAnimation(ProductConfig product)
        {
            // Уменьшаем счетчики, так как анимация была прервана
            if (itemsFlyingToShelf > 0)
            {
                itemsFlyingToShelf--;
            }
            if (itemsInAnimation > 0) // Старый счетчик тоже уменьшаем
            {
                itemsInAnimation--;
            }
            
            // ВАЖНО: Чтобы избежать потери товара, принудительно размещаем его на полке
            // Товар уже был убран из коробки игрока, поэтому он должен попасть на полку
            if (product != null)
            {
                // Если уровень пуст, устанавливаем тип товара
                if (IsEmpty)
                {
                    acceptedProduct = product;
                    CreateOrUpdatePriceTag();
                }
                
                // Размещаем товар на полке (если это тот же тип товара или полка пуста)
                if (acceptedProduct == product)
                {
                    currentItemCount++;
                    UpdateVisuals();
                    Debug.Log($"ShelfLevel '{gameObject.name}': Placement animation CANCELLED, but product {product.ProductName} was placed on shelf to prevent loss. Current count: {currentItemCount}");
                }
                else
                {
                    Debug.LogWarning($"ShelfLevel '{gameObject.name}': Cannot place {product.ProductName} on shelf during cancellation - shelf accepts {acceptedProduct?.ProductName}. Item will be lost!");
                }
            }
            
            Debug.Log($"ShelfLevel '{gameObject.name}': Placement on shelf animation CANCELLED for {product?.ProductName ?? "Unknown Product"}. Items flying to shelf: {itemsFlyingToShelf}");
        }
        
        private void OnDestroy()
        {
            ClearLevel();
            
            // Восстанавливаем оригинальный материал при уничтожении
            if (levelRenderer != null && originalMaterial != null)
            {
                levelRenderer.material = originalMaterial;
            }
        }
    }
} 