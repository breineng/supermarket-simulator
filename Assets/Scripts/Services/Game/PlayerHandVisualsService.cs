using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BehaviourInject;

namespace Supermarket.Services.Game
{
    /// <summary>
    /// Сервис для визуализации коробки в руках игрока и анимации товаров.
    /// Поддерживает отключение отображения моделей для слишком больших объектов через параметр ShowModelInBox в ProductConfig.
    /// </summary>
    public class PlayerHandVisualsService : MonoBehaviour, IPlayerHandVisualsService
    {
        private class ActiveAnimation
        {
            public Coroutine Coroutine;
            public GameObject AnimatedItem;
            public bool IsRemoval; // True if item is going TO SHELF (removed from box)
            public System.Action OriginalOnComplete;
            public ProductConfig Product; 
        }
        private List<ActiveAnimation> _activeAnimations = new List<ActiveAnimation>();

        public event System.Action<ProductConfig> OnTakeAnimationCancelledMidFlight; // Item from Shelf to Box was cancelled
        public event System.Action<ProductConfig> OnPlaceAnimationCancelledMidFlight; // Item from Box to Shelf was cancelled
        
        [Header("Box Visuals Configuration")]
        [SerializeField] private GameObject _boxVisualsPreafb; // Префаб для визуализации коробки в руках
        [SerializeField] private Vector3 _handBoxPosition = new Vector3(0.6f, -0.3f, 1.0f); // Позиция коробки относительно камеры
        [SerializeField] private Vector3 _handBoxRotation = new Vector3(10f, -15f, 5f); // Поворот коробки в руках
        [SerializeField] private float _boxOpenAnimationDuration = 0.5f;
        [SerializeField] private float _itemAnimationDuration = 0.8f;
        [SerializeField] private int _maxVisualItemsPerBox = 12; // Максимальное количество визуальных товаров в коробке
        
        [Header("Item Animation Settings")]
        [SerializeField] private AnimationCurve _itemFlightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _itemArcHeight = 0.5f; // Высота дуги при полете товара

        [Header("Box Animation Settings")]
        [SerializeField] private float _boxAppearAnimationDuration = 0.4f; // Длительность анимации появления коробки
        [SerializeField] private float _boxDisappearAnimationDuration = 0.3f; // Длительность анимации исчезновения коробки
        [SerializeField] private AnimationCurve _boxAppearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Кривая появления
        [SerializeField] private AnimationCurve _boxDisappearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Кривая исчезновения
        [SerializeField] private Vector3 _boxAppearStartOffset = new Vector3(0.2f, -0.5f, 0.5f); // Начальная позиция для появления
        
        [Header("Box Sway Settings")]
        [SerializeField] private bool _enableBoxSway = true; // Включить покачивание коробки при ходьбе
        [SerializeField] private float _swayIntensity = 0.015f; // Интенсивность покачивания
        [SerializeField] private bool _useHeadBobSync = true; // Синхронизировать с головой игрока
        [SerializeField] private float _fallbackSwayFrequency = 2.0f; // Частота покачивания (если нет синхронизации с головой)
        [SerializeField] private Vector3 _swayDirection = new Vector3(0.8f, 1f, 0.5f); // Направления покачивания по осям (вертикальное доминирует)
        [SerializeField] private float _movementThreshold = 0.1f; // Минимальная скорость для активации покачивания

        [Inject] public IProductCatalogService _productCatalogService;

        private Transform _handTransform;
        private GameObject _currentBoxVisual;
        private Animator _boxAnimator;
        private Transform _boxContentsParent; // Контейнер для визуальных товаров внутри коробки
        private List<GameObject> _visualItems = new List<GameObject>(); // Список визуальных товаров в коробке
        
        // Анимация коробки
        private Coroutine _boxAnimationCoroutine;
        private Vector3 _baseBoxPosition; // Базовая позиция коробки (без покачивания)
        private Vector3 _lastPlayerPosition; // Последняя позиция игрока для вычисления скорости
        private float _swayTime; // Время для синуса покачивания (fallback режим)
        private PlayerHeadBob _playerHeadBob; // Ссылка на компонент head bob игрока
        
        public bool IsBoxVisible => _currentBoxVisual != null && _currentBoxVisual.activeInHierarchy;

        public void Initialize(Transform handTransform)
        {
            _handTransform = handTransform;
            
            if (_boxVisualsPreafb == null)
            {
                Debug.LogError("PlayerHandVisualsService: Box visuals prefab is not assigned!");
                return;
            }
            
            // Инициализируем позицию игрока для отслеживания движения
            if (_handTransform != null)
            {
                _lastPlayerPosition = _handTransform.position;
                
                // Ищем компонент PlayerHeadBob на том же объекте или родительском
                if (_useHeadBobSync)
                {
                    _playerHeadBob = _handTransform.GetComponentInParent<PlayerHeadBob>();
                    if (_playerHeadBob == null)
                    {
                        Debug.LogWarning("PlayerHandVisualsService: PlayerHeadBob component not found for synchronization. Falling back to independent sway.");
                    }
                    else
                    {
                        Debug.Log("PlayerHandVisualsService: PlayerHeadBob found - box sway will be synchronized with head bob.");
                    }
                }
            }
            
            Debug.Log($"PlayerHandVisualsService: Initialized with hand transform: {_handTransform?.name ?? "null"}");
        }

        void Update()
        {
            // Обновляем покачивание коробки при движении
            UpdateBoxSway();
        }

        public void ShowBoxInHands(BoxData boxData)
        {
            if (_handTransform == null)
            {
                Debug.LogError("PlayerHandVisualsService: Hand transform not set. Call Initialize() first.");
                return;
            }

            if (_boxVisualsPreafb == null)
            {
                Debug.LogError("PlayerHandVisualsService: Box visuals prefab is not assigned!");
                return;
            }

            // Удаляем предыдущую коробку, если есть
            HideBoxInHands();

            // Создаем новую визуальную коробку
            _currentBoxVisual = Instantiate(_boxVisualsPreafb, _handTransform);
            _currentBoxVisual.transform.localPosition = _handBoxPosition;
            _currentBoxVisual.transform.localRotation = Quaternion.Euler(_handBoxRotation);
            
            // Сохраняем базовую позицию для покачивания
            _baseBoxPosition = _handBoxPosition;
            
            // Убираем физику и коллизию для визуальной коробки
            var rigidbody = _currentBoxVisual.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            var colliders = _currentBoxVisual.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            // Получаем аниматор
            _boxAnimator = _currentBoxVisual.GetComponentInChildren<Animator>();
            if (_boxAnimator == null)
            {
                Debug.LogWarning("PlayerHandVisualsService: No Animator found on box visual prefab!");
            }

            // Ищем или создаем контейнер для содержимого коробки
            _boxContentsParent = _currentBoxVisual.transform.Find("Contents");
            if (_boxContentsParent == null)
            {
                var contentsObj = new GameObject("Contents");
                contentsObj.transform.SetParent(_currentBoxVisual.transform);
                contentsObj.transform.localPosition = Vector3.zero;
                _boxContentsParent = contentsObj.transform;
            }

            // Обновляем содержимое коробки
            UpdateBoxContents(boxData.ProductInBox, boxData.Quantity);
            
            // Убеждаемся, что коробка полностью видима
            SetBoxAlpha(1f);

            Debug.Log($"PlayerHandVisualsService: Showing box in hands - {boxData.ProductInBox?.ProductName ?? "Empty"} x{boxData.Quantity}");
        }

        public void HideBoxInHands()
        {
            if (_currentBoxVisual != null)
            {
                // --- START: Cancel active animations ---
                // Iterate over a copy for safe removal
                List<ActiveAnimation> animationsToCancel = new List<ActiveAnimation>(_activeAnimations);
                foreach (var activeAnim in animationsToCancel)
                {
                    if (activeAnim.Coroutine != null)
                    {
                        StopCoroutine(activeAnim.Coroutine);
                    }
                    if (activeAnim.AnimatedItem != null)
                    {
                        Destroy(activeAnim.AnimatedItem);
                    }

                    if (activeAnim.IsRemoval) // Item was going FROM BOX to SHELF
                    {
                        OnPlaceAnimationCancelledMidFlight?.Invoke(activeAnim.Product);
                        Debug.Log($"PlayerHandVisualsService: Placement animation for {activeAnim.Product?.ProductName} cancelled mid-flight.");
                    }
                    else // Item was going FROM SHELF to BOX
                    {
                        OnTakeAnimationCancelledMidFlight?.Invoke(activeAnim.Product);
                        Debug.Log($"PlayerHandVisualsService: Take animation for {activeAnim.Product?.ProductName} cancelled mid-flight.");
                    }
                }
                _activeAnimations.Clear();
                // --- END: Cancel active animations ---

                Destroy(_currentBoxVisual);
                _currentBoxVisual = null;
                _boxAnimator = null;
                _boxContentsParent = null;
                ClearVisualItems();
                Debug.Log("PlayerHandVisualsService: Box visuals hidden");
            }
        }

        public void OpenBox()
        {
            // Используем старую систему с Animator
            if (_boxAnimator != null)
            {
                _boxAnimator.SetBool("Opened", true);
                Debug.Log("PlayerHandVisualsService: Playing box open animation via Animator");
            }
            else
            {
                Debug.LogWarning("PlayerHandVisualsService: Cannot play open animation - no animation controller available");
            }
        }

        public void CloseBox()
        {
            // Используем старую систему с Animator
            if (_boxAnimator != null)
            {
                _boxAnimator.SetBool("Opened", false);
                Debug.Log("PlayerHandVisualsService: Playing box close animation via Animator");
            }
            else
            {
                Debug.LogWarning("PlayerHandVisualsService: Cannot play close animation - no animation controller available");
            }
        }

        public void UpdateBoxContents(ProductConfig product, int quantity)
        {
            if (_boxContentsParent == null) return;

            // Очищаем предыдущие визуальные товары
            ClearVisualItems();

            // Если товара нет или количество 0, не создаем визуальные элементы
            if (product == null || quantity <= 0) return;

            // Проверяем, нужно ли отображать модель в коробке
            if (!product.ShowModelInBox)
            {
                Debug.Log($"PlayerHandVisualsService: Product {product.ProductName} has ShowModelInBox disabled - skipping visual items creation");
                return;
            }

            // Ограничиваем количество визуальных элементов
            int visualCount = Mathf.Min(quantity, _maxVisualItemsPerBox);
            
            for (int i = 0; i < visualCount; i++)
            {
                CreateVisualItem(product, i, visualCount);
            }

            Debug.Log($"PlayerHandVisualsService: Updated box contents - {visualCount} visual items for {product.ProductName}");
        }

        public void AnimateItemRemoval(ProductConfig product, Vector3 targetPosition)
        {
            AnimateItemRemoval(product, targetPosition, null);
        }
        
        public void AnimateItemRemoval(ProductConfig product, Vector3 targetPosition, System.Action onAnimationComplete)
        {
            AnimateItemRemoval(product, targetPosition, Quaternion.identity, onAnimationComplete);
        }
        
        public void AnimateItemRemoval(ProductConfig product, Vector3 targetPosition, Quaternion targetRotation, System.Action onAnimationComplete)
        {
            if (product == null) 
            {
                onAnimationComplete?.Invoke();
                return;
            }

            // Проверяем, нужно ли отображать модель в коробке
            if (!product.ShowModelInBox)
            {
                Debug.Log($"PlayerHandVisualsService.AnimateItemRemoval: Product {product.ProductName} has ShowModelInBox disabled - skipping animation");
                onAnimationComplete?.Invoke();
                return;
            }

            if (_visualItems.Count == 0) 
            {
                Debug.LogWarning($"PlayerHandVisualsService.AnimateItemRemoval: No visual items to remove for product {product.ProductName}");
                onAnimationComplete?.Invoke();
                return;
            }

            var itemToRemove = _visualItems[_visualItems.Count - 1];
            _visualItems.RemoveAt(_visualItems.Count - 1);

            if (itemToRemove != null)
            {
                Vector3 sourcePosition = itemToRemove.transform.position;
                
                ActiveAnimation newAnimation = new ActiveAnimation
                {
                    AnimatedItem = itemToRemove,
                    IsRemoval = true,
                    OriginalOnComplete = onAnimationComplete,
                    Product = product
                };
                
                System.Action wrappedOnComplete = () => {
                    onAnimationComplete?.Invoke();
                    _activeAnimations.Remove(newAnimation);
                };
                
                newAnimation.Coroutine = StartCoroutine(AnimateItemToTarget(itemToRemove, sourcePosition, targetPosition, targetRotation, true, wrappedOnComplete));
                _activeAnimations.Add(newAnimation);
            }
        }

        public void AnimateItemAddition(ProductConfig product, Vector3 sourcePosition)
        {
            AnimateItemAddition(product, sourcePosition, null);
        }

        public void AnimateItemAddition(ProductConfig product, Vector3 sourcePosition, System.Action onAnimationComplete)
        {
            AnimateItemAddition(product, sourcePosition, Quaternion.identity, onAnimationComplete);
        }

        public void AnimateItemAddition(ProductConfig product, Vector3 sourcePosition, Quaternion sourceRotation, System.Action onAnimationComplete)
        {
            if (product == null || _boxContentsParent == null) 
            {
                Debug.LogError($"PlayerHandVisualsService.AnimateItemAddition: Product is {(product == null ? "null" : product.ProductName)} or _boxContentsParent is null. Cannot animate.");
                onAnimationComplete?.Invoke();
                return;
            }

            // Проверяем, нужно ли отображать модель в коробке
            if (!product.ShowModelInBox)
            {
                Debug.Log($"PlayerHandVisualsService.AnimateItemAddition: Product {product.ProductName} has ShowModelInBox disabled - skipping animation");
                onAnimationComplete?.Invoke();
                return;
            }

            Debug.Log($"PlayerHandVisualsService.AnimateItemAddition: Called for {product.ProductName}. Prefab is {(product.Prefab == null ? "null" : "assigned")}. SourcePos: {sourcePosition}");

            var tempItem = CreateTempVisualItem(product, sourcePosition, sourceRotation);
            if (tempItem != null)
            {
                tempItem.transform.localScale = Vector3.one;
                Vector3 targetPosition = CalculateNextItemWorldPosition();

                ActiveAnimation newAnimation = new ActiveAnimation
                {
                    AnimatedItem = tempItem,
                    IsRemoval = false,
                    OriginalOnComplete = onAnimationComplete,
                    Product = product
                };

                System.Action wrappedOnComplete = () => {
                    onAnimationComplete?.Invoke();
                    // Important: remove AFTER OriginalOnComplete, because OriginalOnComplete might trigger new logic
                    // that could be affected if the animation is already marked as "not active".
                    // However, for AddToBox scenario, OriginalOnComplete invokes ShelfLevel updates.
                    // If this animation entry is needed for some cleanup in OriginalOnComplete, this order is fine.
                    _activeAnimations.Remove(newAnimation);
                };
                
                newAnimation.Coroutine = StartCoroutine(AnimateItemToTarget(tempItem, sourcePosition, targetPosition, Quaternion.identity, false, wrappedOnComplete));
                _activeAnimations.Add(newAnimation);
            }
        }

        private void CreateVisualItem(ProductConfig product, int index, int totalCount)
        {
            if (product.Prefab == null)
            {
                Debug.LogWarning($"PlayerHandVisualsService: No prefab for product {product.ProductName}");
                return;
            }

            // Дополнительная проверка ShowModelInBox (хотя основная проверка уже была в UpdateBoxContents)
            if (!product.ShowModelInBox)
            {
                Debug.LogWarning($"PlayerHandVisualsService: CreateVisualItem called for product {product.ProductName} with ShowModelInBox=false");
                return;
            }

            var visualItem = Instantiate(product.Prefab, _boxContentsParent);
            
            // Убираем физику и скрипты с визуальных товаров
            var rigidbody = visualItem.GetComponent<Rigidbody>();
            if (rigidbody != null) Destroy(rigidbody);
            
            var colliders = visualItem.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                Destroy(collider);
            }

            var scripts = visualItem.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in scripts)
            {
                Destroy(script);
            }

            // Позиционируем товары в сетке внутри коробки
            var position = CalculateItemPosition(index, totalCount);
            visualItem.transform.localPosition = position;
            visualItem.transform.localScale = Vector3.one * 0.8f; // Уменьшенный размер для коробки
            visualItem.transform.localRotation = Quaternion.identity;
            
            _visualItems.Add(visualItem);
        }

        private Vector3 CalculateItemPosition(int index, int totalCount)
        {
            // Размещаем товары в сетке 3x4 внутри коробки
            int itemsPerRow = 3;
            int row = index / itemsPerRow;
            int col = index % itemsPerRow;
            
            float spacing = 0.08f; // Уменьшил расстояние для более плотного размещения
            float offsetX = (col - 1) * spacing; // Центрируем по X
            float offsetZ = (row - 1.5f) * spacing; // Центрируем по Z
            float offsetY = 0.03f; // Небольшой подъем над дном коробки
            
            return new Vector3(offsetX, offsetY, offsetZ);
        }

        /// <summary>
        /// Рассчитывает мировую позицию для следующего товара в коробке
        /// </summary>
        private Vector3 CalculateNextItemWorldPosition()
        {
            if (_boxContentsParent == null) return Vector3.zero;
            
            Vector3 localPosition = CalculateItemPosition(_visualItems.Count, _visualItems.Count + 1);
            return _boxContentsParent.TransformPoint(localPosition);
        }

        /// <summary>
        /// Получает мировую позицию товара в коробке по индексу
        /// </summary>
        private Vector3 GetItemWorldPosition(int index)
        {
            if (_boxContentsParent == null) return Vector3.zero;
            
            Vector3 localPosition = CalculateItemPosition(index, _visualItems.Count);
            return _boxContentsParent.TransformPoint(localPosition);
        }

        private GameObject CreateTempVisualItem(ProductConfig product, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (product.Prefab == null) return null;

            // Проверяем, нужно ли отображать модель в коробке
            if (!product.ShowModelInBox) return null;

            var tempItem = Instantiate(product.Prefab);
            tempItem.transform.position = worldPosition;
            tempItem.transform.rotation = worldRotation; // Используем исходный поворот
            tempItem.transform.localScale = Vector3.one; // Полный размер как на полке
            
            // Убираем физику
            var rigidbody = tempItem.GetComponent<Rigidbody>();
            if (rigidbody != null) Destroy(rigidbody);
            
            var colliders = tempItem.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                Destroy(collider);
            }

            return tempItem;
        }

        private IEnumerator AnimateItemToTarget(GameObject item, Vector3 startPosition, Vector3 targetPosition, bool isRemoval, System.Action onAnimationComplete = null)
        {
            // Используем стандартный поворот для обратной совместимости
            Quaternion targetRotation = isRemoval ? Quaternion.identity : Quaternion.identity;
            return AnimateItemToTarget(item, startPosition, targetPosition, targetRotation, isRemoval, onAnimationComplete);
        }

        private IEnumerator AnimateItemToTarget(GameObject item, Vector3 startPosition, Vector3 targetPosition, Quaternion targetRotation, bool isRemoval, System.Action onAnimationComplete = null)
        {
            if (item == null) 
            {
                Debug.LogError("PlayerHandVisualsService.AnimateItemToTarget: Item is null at the beginning of coroutine.");
                onAnimationComplete?.Invoke(); // Still call complete to remove from active list and potentially fix counters
                yield break;
            }
            // Ensure this is removed from active list even if item becomes null mid-animation (e.g. external destruction)
            // However, the current design is that HideBoxInHands stops and cleans up.
            // If item is destroyed externally mid-animation, this coroutine will throw MissingReferenceException.

            Debug.Log($"PlayerHandVisualsService.AnimateItemToTarget: Coroutine started for item: {item.name}. TargetPos: {targetPosition}, IsRemoval: {isRemoval}");

            var startTime = Time.time;
            
            // Сохраняем исходный поворот товара
            Quaternion startRotation = item.transform.rotation;
            
            // Рассчитываем высоту дуги на основе расстояния
            float distance = Vector3.Distance(startPosition, targetPosition);
            float dynamicArcHeight = Mathf.Clamp(distance * 0.3f, 0.2f, _itemArcHeight);
            
            while (Time.time - startTime < _itemAnimationDuration)
            {
                float progress = (Time.time - startTime) / _itemAnimationDuration;
                float easedProgress = _itemFlightCurve.Evaluate(progress);
                
                // Расчет позиции с дугой
                var currentPos = Vector3.Lerp(startPosition, targetPosition, easedProgress);
                currentPos.y += Mathf.Sin(easedProgress * Mathf.PI) * dynamicArcHeight;
                
                item.transform.position = currentPos;
                
                // Плавное вращение во время полета
                if (isRemoval)
                {
                    // При размещении товара: интерполируем от исходного поворота к целевому
                    item.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedProgress);
                }
                else
                {
                    // При взятии товара: комбинируем исходный поворот с дополнительным вращением
                    float rotationSpeed = 180f;
                    float additionalRotation = rotationSpeed * progress;
                    Quaternion flightRotation = startRotation * Quaternion.Euler(0, additionalRotation, 0);
                    item.transform.rotation = flightRotation;
                }
                
                // Масштабирование для лучшего визуального эффекта
                float scale = isRemoval ? 
                    Mathf.Lerp(0.8f, 1f, easedProgress) : // При изъятии увеличиваем
                    Mathf.Lerp(1f, 0.8f, easedProgress); // При добавлении уменьшаем
                item.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }

            // Финальная позиция и поворот
            item.transform.position = targetPosition;
            
            if (isRemoval)
            {
                // При размещении устанавливаем точный целевой поворот
                item.transform.rotation = targetRotation;
                // При изъятии уничтожаем объект
                Destroy(item);
            }
            else
            {
                // При добавлении перемещаем в контейнер коробки
                item.transform.SetParent(_boxContentsParent);
                item.transform.localPosition = CalculateItemPosition(_visualItems.Count, _visualItems.Count + 1);
                item.transform.localRotation = Quaternion.identity;
                item.transform.localScale = Vector3.one * 0.8f; // Финальный размер в коробке
                
                _visualItems.Add(item);
            }
            
            // Вызываем callback если он задан
            onAnimationComplete?.Invoke();
        }

        private void ClearVisualItems()
        {
            foreach (var item in _visualItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _visualItems.Clear();
        }

        void OnDestroy()
        {
            ClearVisualItems();
        }

        /// <summary>
        /// Обновляет покачивание коробки при движении игрока
        /// Синхронизируется с головой игрока (в противофазе) или работает независимо
        /// </summary>
        private void UpdateBoxSway()
        {
            if (!_enableBoxSway || _currentBoxVisual == null || _handTransform == null)
                return;

            bool isMoving = false;
            float speedMultiplier = 1f;
            float bobTimer = 0f;

            // Определяем движение и таймер в зависимости от настроек синхронизации
            if (_useHeadBobSync && _playerHeadBob != null && _playerHeadBob.IsHeadBobActive)
            {
                // Используем данные от PlayerHeadBob
                isMoving = _playerHeadBob.IsPlayerMoving;
                speedMultiplier = Mathf.Clamp01(_playerHeadBob.MovementSpeed / 5f);
                bobTimer = _playerHeadBob.HeadBobTimer;
            }
            else
            {
                // Fallback: собственное определение движения
                Vector3 currentPosition = _handTransform.position;
                Vector3 velocity = (currentPosition - _lastPlayerPosition) / Time.deltaTime;
                _lastPlayerPosition = currentPosition;

                float speed = velocity.magnitude;
                isMoving = speed > _movementThreshold;
                speedMultiplier = Mathf.Clamp01(speed / 5f);
                
                if (isMoving)
                {
                    _swayTime += Time.deltaTime * _fallbackSwayFrequency;
                    bobTimer = _swayTime;
                }
            }

            // Применяем покачивание коробки в противофазе к голове
            if (isMoving)
            {
                // Коробка качается в противофазе (+ Mathf.PI) и с акцентом на вертикальное движение
                Vector3 sway = new Vector3(
                    Mathf.Sin(bobTimer * 0.5f + Mathf.PI) * _swayDirection.x,        // Горизонтальное (медленнее, противофаза)
                    -Mathf.Sin(bobTimer + Mathf.PI) * _swayDirection.y,              // Вертикальное (основное, противофаза, инвертированное)
                    Mathf.Sin(bobTimer * 0.7f + Mathf.PI) * _swayDirection.z        // Глубина (средняя частота, противофаза)
                ) * _swayIntensity * speedMultiplier;

                // Применяем покачивание к текущей позиции
                _currentBoxVisual.transform.localPosition = _baseBoxPosition + sway;
            }
            else
            {
                // Плавно возвращаем коробку в базовую позицию при остановке
                _currentBoxVisual.transform.localPosition = Vector3.Lerp(
                    _currentBoxVisual.transform.localPosition,
                    _baseBoxPosition,
                    Time.deltaTime * 8f
                );
            }
        }

        /// <summary>
        /// Анимация появления коробки в руках
        /// </summary>
        private IEnumerator AnimateBoxAppear(BoxData boxData, System.Action onComplete)
        {
            // Сначала создаем коробку без анимации, но невидимую
            ShowBoxInHands(boxData);
            
            if (_currentBoxVisual == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Запоминаем целевую позицию
            Vector3 targetPosition = _handBoxPosition;
            _baseBoxPosition = targetPosition;

            // Устанавливаем начальную позицию для анимации
            Vector3 startPosition = targetPosition + _boxAppearStartOffset;
            _currentBoxVisual.transform.localPosition = startPosition;

            // Начинаем с прозрачности
            SetBoxAlpha(0f);

            float elapsed = 0f;
            while (elapsed < _boxAppearAnimationDuration)
            {
                float t = elapsed / _boxAppearAnimationDuration;
                float curveValue = _boxAppearCurve.Evaluate(t);

                // Интерполируем позицию
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
                _currentBoxVisual.transform.localPosition = currentPosition;

                // Интерполируем прозрачность
                SetBoxAlpha(curveValue);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Устанавливаем финальные значения
            _currentBoxVisual.transform.localPosition = targetPosition;
            SetBoxAlpha(1f);

            _boxAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Анимация исчезновения коробки из рук
        /// </summary>
        private IEnumerator AnimateBoxDisappear(System.Action onComplete)
        {
            if (_currentBoxVisual == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startPosition = _currentBoxVisual.transform.localPosition;
            Vector3 targetPosition = startPosition + _boxAppearStartOffset;

            float elapsed = 0f;
            while (elapsed < _boxDisappearAnimationDuration)
            {
                float t = elapsed / _boxDisappearAnimationDuration;
                float curveValue = _boxDisappearCurve.Evaluate(t);

                // Интерполируем позицию (в обратную сторону)
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
                _currentBoxVisual.transform.localPosition = currentPosition;

                // Интерполируем прозрачность (исчезаем)
                SetBoxAlpha(1f - curveValue);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Окончательно скрываем коробку
            HideBoxInHands();

            _boxAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Устанавливает прозрачность коробки и всех её дочерних объектов
        /// </summary>
        private void SetBoxAlpha(float alpha)
        {
            if (_currentBoxVisual == null) return;

            // Находим все Renderer'ы в коробке и её дочерних объектах
            Renderer[] renderers = _currentBoxVisual.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_Color"))
                    {
                        Color color = material.color;
                        color.a = alpha;
                        material.color = color;
                    }
                }
            }
        }

        public void ShowBoxInHandsAnimated(BoxData boxData, System.Action onAnimationComplete = null)
        {
            if (_boxAnimationCoroutine != null)
            {
                StopCoroutine(_boxAnimationCoroutine);
                _boxAnimationCoroutine = null;
            }

            _boxAnimationCoroutine = StartCoroutine(AnimateBoxAppear(boxData, onAnimationComplete));
        }

        public void HideBoxInHandsAnimated(System.Action onAnimationComplete = null)
        {
            if (_currentBoxVisual == null)
            {
                onAnimationComplete?.Invoke();
                return;
            }

            if (_boxAnimationCoroutine != null)
            {
                StopCoroutine(_boxAnimationCoroutine);
                _boxAnimationCoroutine = null;
            }

            _boxAnimationCoroutine = StartCoroutine(AnimateBoxDisappear(onAnimationComplete));
        }
    }
} 