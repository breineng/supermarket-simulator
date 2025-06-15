using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;
using Supermarket.Interactables;
using System.Collections.Generic;
using Core.Interfaces; // Для IInteractable

public class PlayerHandVisualsController : MonoBehaviour
{
    [Header("Hand Transform Reference")]
    [SerializeField] private Transform _handTransform; // Точка привязки коробки в руках (обычно камера игрока)
    
    [Inject] public IPlayerHandService _playerHandService;
    [Inject] public IPlayerHandVisualsService _playerHandVisualsService;
    [Inject] public IInteractionService _interactionService; // Для получения позиций для анимации

    // Структура для хранения информации об анимации
    private class AnimationContext
    {
        public ProductConfig Product;
        public IInteractable TargetInteractable;
        public Supermarket.Interactables.ShelfLevel TargetShelfLevel; // Конкретный уровень для многоуровневых полок
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public bool IsPlacement; // true = из коробки на полку, false = с полки в коробку
        public System.Action<AnimationContext> OnComplete;
    }
    
    // Список активных анимаций
    private List<AnimationContext> _activeAnimations = new List<AnimationContext>();

    void Start()
    {
        if (_handTransform == null)
        {
            // Если не назначен вручную, пытаемся найти камеру игрока
            var playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                _handTransform = playerCamera.transform;
                Debug.Log("PlayerHandVisualsController: Using player camera as hand transform");
            }
            else
            {
                Debug.LogError("PlayerHandVisualsController: Hand transform not assigned and no camera found!");
                enabled = false;
                return;
            }
        }

        if (_playerHandVisualsService != null)
        {
            _playerHandVisualsService.Initialize(_handTransform);
            // Устанавливаем точку привязки в PlayerHandService
            _playerHandService?.SetBoxVisualsParent(_handTransform);
            
            // Подписываемся на события отмены анимации
            _playerHandVisualsService.OnTakeAnimationCancelledMidFlight += OnTakeAnimationCancelled;
            _playerHandVisualsService.OnPlaceAnimationCancelledMidFlight += OnPlaceAnimationCancelled;
        }
        else
        {
            Debug.LogError("PlayerHandVisualsController: IPlayerHandVisualsService not injected!");
        }

        // Подписываемся на события PlayerHandService
        if (_playerHandService != null)
        {
            _playerHandService.OnBoxPickedUp += OnBoxPickedUp;
            _playerHandService.OnBoxDropped += OnBoxDropped;
            _playerHandService.OnBoxStateChanged += OnBoxStateChanged;
            _playerHandService.OnHandContentChanged += OnHandContentChanged;
            _playerHandService.OnItemConsumedFromBox += OnItemConsumed;
            _playerHandService.OnItemAddedToBox += OnItemAdded;
            
            Debug.Log("PlayerHandVisualsController: Subscribed to PlayerHandService events");
        }
        else
        {
            Debug.LogError("PlayerHandVisualsController: IPlayerHandService not injected!");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (_playerHandService != null)
        {
            _playerHandService.OnBoxPickedUp -= OnBoxPickedUp;
            _playerHandService.OnBoxDropped -= OnBoxDropped;
            _playerHandService.OnBoxStateChanged -= OnBoxStateChanged;
            _playerHandService.OnHandContentChanged -= OnHandContentChanged;
            _playerHandService.OnItemConsumedFromBox -= OnItemConsumed;
            _playerHandService.OnItemAddedToBox -= OnItemAdded;
            
            // Отписываемся от событий отмены анимации
            if (_playerHandVisualsService != null)
            {
                _playerHandVisualsService.OnTakeAnimationCancelledMidFlight -= OnTakeAnimationCancelled;
                _playerHandVisualsService.OnPlaceAnimationCancelledMidFlight -= OnPlaceAnimationCancelled;
            }
        }
    }

    private void OnBoxPickedUp(BoxData boxData)
    {
        if (_playerHandVisualsService != null)
        {
            // Используем анимированную версию для плавного появления
            _playerHandVisualsService.ShowBoxInHandsAnimated(boxData);
            Debug.Log($"PlayerHandVisualsController: Showing box visuals with animation for {boxData.ProductInBox?.ProductName ?? "empty box"}");
        }
    }

    private void OnBoxDropped()
    {
        if (_playerHandVisualsService != null)
        {
            // Используем анимированную версию для плавного исчезновения
            _playerHandVisualsService.HideBoxInHandsAnimated();
            Debug.Log("PlayerHandVisualsController: Hiding box visuals with animation");
        }
    }

    private void OnBoxStateChanged()
    {
        if (_playerHandService == null || _playerHandVisualsService == null) return;

        if (_playerHandService.IsHoldingBox())
        {
            if (_playerHandService.IsBoxOpen())
            {
                _playerHandVisualsService.OpenBox();
                Debug.Log("PlayerHandVisualsController: Opening box visuals");
            }
            else
            {
                _playerHandVisualsService.CloseBox();
                Debug.Log("PlayerHandVisualsController: Closing box visuals");
            }
        }
    }

    private void OnHandContentChanged()
    {
        if (_playerHandService == null || _playerHandVisualsService == null) return;

        if (_playerHandService.IsHoldingBox())
        {
            var boxData = _playerHandService.GetHeldBoxData();
            _playerHandVisualsService.UpdateBoxContents(boxData.ProductInBox, boxData.Quantity);
            Debug.Log($"PlayerHandVisualsController: Updated box contents - {boxData.ProductInBox?.ProductName ?? "empty"} x{boxData.Quantity}");
        }
    }

    private void OnItemConsumed(ProductConfig product, int amount)
    {
        if (_playerHandVisualsService == null || product == null) return;

        // Сохраняем текущую полку в фокусе для всех анимаций этой партии
        IInteractable targetInteractable = _interactionService?.CurrentFocusedInteractable;
        
        for (int i = 0; i < amount; i++)
        {
            // Создаем контекст анимации
            AnimationContext context = new AnimationContext
            {
                Product = product,
                TargetInteractable = targetInteractable,
                IsPlacement = true
            };
            
            // Резервируем слот для этого товара на целевой полке
            if (targetInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                // Сохраняем текущий фокусный уровень
                context.TargetShelfLevel = multiShelf.GetCurrentFocusedLevel();
                
                // Резервируем слот на конкретном уровне
                if (context.TargetShelfLevel != null)
                {
                    context.TargetShelfLevel.StartPlacementAnimation();
                    // Получаем позицию и поворот с конкретного уровня
                    context.TargetPosition = context.TargetShelfLevel.GetNextAvailableSlotPosition();
                    context.TargetRotation = context.TargetShelfLevel.GetNextAvailableSlotRotation();
                }
                else
                {
                    Debug.LogWarning($"PlayerHandVisualsController: No focused level on multi-shelf");
                    context.TargetPosition = multiShelf.transform.position + Vector3.up * 0.5f;
                    context.TargetRotation = Quaternion.identity;
                }
            }
            
            _activeAnimations.Add(context);
            
            // Запускаем анимацию с callback'ом для завершения размещения на полке
            _playerHandVisualsService.AnimateItemRemoval(product, context.TargetPosition, context.TargetRotation, () => {
                OnItemAnimationCompleted(context);
            });
            
            Debug.Log($"PlayerHandVisualsController: Starting animation for item {i+1}/{amount} of {product.ProductName} to position {context.TargetPosition} with rotation {context.TargetRotation}");
        }
    }

    private void OnItemAdded(ProductConfig product, int amount)
    {
        if (_playerHandVisualsService == null || product == null)
        {
            Debug.LogError("PlayerHandVisualsController.OnItemAdded: VisualsService or Product is null. Cannot animate.");
            return;
        }

        // Сохраняем текущую полку в фокусе для всех анимаций этой партии
        IInteractable sourceInteractable = _interactionService?.CurrentFocusedInteractable;
        
        // Получаем позицию и поворот источника для анимации один раз для всей партии
        Vector3 sourcePosition = Vector3.zero;
        Quaternion sourceRotation = Quaternion.identity;
        Supermarket.Interactables.ShelfLevel sourceShelfLevel = null;
        
        if (sourceInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
        {
            sourceShelfLevel = multiShelf.GetCurrentFocusedLevel();
            if (sourceShelfLevel != null)
            {
                sourcePosition = sourceShelfLevel.GetLastOccupiedSlotPosition();
                sourceRotation = sourceShelfLevel.GetLastOccupiedSlotRotation();
            }
            else
            {
                Debug.LogWarning($"PlayerHandVisualsController: No focused level on multi-shelf for take animation");
                sourcePosition = multiShelf.transform.position + Vector3.up * 0.5f;
                sourceRotation = Quaternion.identity;
            }
        }
        else
        {
            // Фоллбэк позиция
            sourcePosition = GetInteractionTargetPosition();
            sourceRotation = Quaternion.identity;
        }
        
        Debug.Log($"PlayerHandVisualsController.OnItemAdded: Preparing to animate addition of {amount}x {product.ProductName}. SourcePos: {sourcePosition}, SourceRot: {sourceRotation.eulerAngles}");
        
        for (int i = 0; i < amount; i++)
        {
            // Создаем контекст анимации
            AnimationContext context = new AnimationContext
            {
                Product = product,
                TargetInteractable = sourceInteractable,
                TargetShelfLevel = sourceShelfLevel,
                IsPlacement = false
            };
            
            _activeAnimations.Add(context);
            
            Debug.Log($"PlayerHandVisualsController.OnItemAdded: Animating item {i+1}/{amount} of {product.ProductName}");
            // Запускаем анимацию с callback'ом для завершения добавления в коробку
            _playerHandVisualsService.AnimateItemAddition(product, sourcePosition, sourceRotation, () => {
                OnItemAnimationCompleted(context);
            });
        }
    }
    
    /// <summary>
    /// Вызывается по завершении анимации товара
    /// </summary>
    private void OnItemAnimationCompleted(AnimationContext context)
    {
        if (!_activeAnimations.Contains(context))
        {
            Debug.LogWarning($"PlayerHandVisualsController: Animation context not found");
            return;
        }
        
        if (context.IsPlacement)
        {
            // Завершаем размещение товара на полке
            if (context.TargetShelfLevel != null)
            {
                // Работаем с конкретным уровнем
                bool placed = context.TargetShelfLevel.PlaceProduct(context.Product);
                context.TargetShelfLevel.CompletePlacementAnimation();
                
                if (placed)
                {
                    Debug.Log($"PlayerHandVisualsController: Completed placement of {context.Product.ProductName} on ShelfLevel");
                }
                else
                {
                    Debug.LogWarning($"PlayerHandVisualsController: Failed to place {context.Product.ProductName} on ShelfLevel");
                }
            }
            else if (context.TargetInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                // Фоллбэк на старый метод, если по какой-то причине уровень не сохранен
                CompleteMultiLevelShelfPlacement(multiShelf, context.Product);
                multiShelf.CompletePlacementAnimationOnFocusedLevel();
                Debug.Log($"PlayerHandVisualsController: Completed placement of {context.Product.ProductName} on MultiLevelShelfController (fallback)");
            }
        }
        else
        {
            // При взятии товара с полки нужно уменьшить счетчик анимаций
            if (context.TargetShelfLevel != null)
            {
                context.TargetShelfLevel.CompleteTakeFromShelfAnimation();
                Debug.Log($"PlayerHandVisualsController: Completed take animation for {context.Product.ProductName} from ShelfLevel");
            }
            else if (context.TargetInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                multiShelf.CompleteTakeAnimationOnFocusedLevel();
                Debug.Log($"PlayerHandVisualsController: Completed take animation for {context.Product.ProductName} from MultiLevelShelfController (fallback)");
            }
        }
        
        // Удаляем контекст после использования
        _activeAnimations.Remove(context);
    }
    
    /// <summary>
    /// Завершает размещение товара на многоуровневой полке
    /// </summary>
    private void CompleteMultiLevelShelfPlacement(Supermarket.Interactables.MultiLevelShelfController multiShelf, ProductConfig product)
    {
        // Используем новый метод для завершения размещения на фокусном уровне
        bool placed = multiShelf.CompletePlacementOnFocusedLevel(product);
        if (placed)
        {
            Debug.Log($"PlayerHandVisualsController: Successfully completed placement of {product.ProductName} on MultiLevelShelfController");
        }
        else
        {
            Debug.LogWarning($"PlayerHandVisualsController: Failed to complete placement of {product.ProductName} on MultiLevelShelfController");
        }
    }

    private Vector3 GetInteractionTargetPosition()
    {
        // Пытаемся получить позицию текущего объекта взаимодействия
        if (_interactionService != null && _interactionService.CurrentFocusedInteractable != null)
        {
            var interactable = _interactionService.CurrentFocusedInteractable;
            if (interactable is MonoBehaviour mb)
            {
                return mb.transform.position;
            }
        }

        // Если не удалось получить позицию взаимодействия, используем позицию перед игроком
        if (_handTransform != null)
        {
            return _handTransform.position + _handTransform.forward * 2f;
        }

        return Vector3.zero;
    }
    
    /// <summary>
    /// Получает точную позицию слота для размещения товара
    /// </summary>
    private Vector3 GetTargetSlotPosition(bool isPlacement)
    {
        // Пытаемся получить точную позицию слота на полке
        if (_interactionService != null && _interactionService.CurrentFocusedInteractable != null)
        {
            // Многоуровневая полка (основная используемая система)
            if (_interactionService.CurrentFocusedInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                Vector3 position = isPlacement ? multiShelf.GetNextAvailableSlotPosition() : multiShelf.GetLastOccupiedSlotPosition();
                Debug.Log($"PlayerHandVisualsController: Using MultiLevelShelfController position: {position}");
                return position;
            }
        }

        // Фоллбэк - общая позиция взаимодействия
        Vector3 fallbackPosition = GetInteractionTargetPosition();
        Debug.Log($"PlayerHandVisualsController: Using fallback position: {fallbackPosition}");
        return fallbackPosition;
    }
    
    /// <summary>
    /// Получает точную позицию слота-источника на полке
    /// </summary>
    private Vector3 GetSourceSlotPosition()
    {
        return GetTargetSlotPosition(isPlacement: false);
    }

    /// <summary>
    /// Получает поворот точки спавна на полке
    /// </summary>
    private Quaternion GetSourceSlotRotation()
    {
        // Пытаемся получить поворот точки спавна с полки
        if (_interactionService != null && _interactionService.CurrentFocusedInteractable != null)
        {
            // Многоуровневая полка (основная используемая система)
            if (_interactionService.CurrentFocusedInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                return GetMultiLevelShelfSlotRotation(multiShelf);
            }
        }

        // Фоллбэк - стандартный поворот
        return Quaternion.identity;
    }

    /// <summary>
    /// Получает поворот точки спавна с многоуровневой полки
    /// </summary>
    private Quaternion GetMultiLevelShelfSlotRotation(Supermarket.Interactables.MultiLevelShelfController multiShelf)
    {
        // Используем новый метод для получения поворота с фокусного уровня
        return multiShelf.GetLastOccupiedSlotRotation();
    }

    /// <summary>
    /// Получает точный поворот слота для размещения товара
    /// </summary>
    private Quaternion GetTargetSlotRotation()
    {
        // Пытаемся получить точный поворот слота на полке
        if (_interactionService != null && _interactionService.CurrentFocusedInteractable != null)
        {
            // Многоуровневая полка (основная используемая система)
            if (_interactionService.CurrentFocusedInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                return multiShelf.GetNextAvailableSlotRotation();
            }
        }

        // Фоллбэк - стандартный поворот
        return Quaternion.identity;
    }

    private void OnTakeAnimationCancelled(ProductConfig product)
    {
        Debug.Log($"PlayerHandVisualsController: Received OnTakeAnimationCancelled for {product?.ProductName}");
        
        // Находим контекст анимации для этого товара (товар летел С полки В коробку)
        var context = _activeAnimations.Find(c => c.Product == product && !c.IsPlacement);
        if (context != null)
        {
            // Уведомляем конкретный уровень об отмене анимации
            if (context.TargetShelfLevel != null)
            {
                context.TargetShelfLevel.CancelTakeFromShelfAnimation(product);
            }
            else if (context.TargetInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                multiShelf.HandleTakeAnimationCancelled(product);
            }
            
            // Удаляем контекст
            _activeAnimations.Remove(context);
        }
        else
        {
            Debug.LogWarning($"PlayerHandVisualsController: No animation context found for cancelled take animation of {product?.ProductName}");
        }
    }
    
    private void OnPlaceAnimationCancelled(ProductConfig product)
    {
        Debug.Log($"PlayerHandVisualsController: Received OnPlaceAnimationCancelled for {product?.ProductName}");
        
        // Находим контекст анимации для этого товара (товар летел ИЗ коробки НА полку)
        var context = _activeAnimations.Find(c => c.Product == product && c.IsPlacement);
        if (context != null)
        {
            // Уведомляем конкретный уровень об отмене анимации
            if (context.TargetShelfLevel != null)
            {
                context.TargetShelfLevel.CancelPlacementOnShelfAnimation(product);
            }
            else if (context.TargetInteractable is Supermarket.Interactables.MultiLevelShelfController multiShelf)
            {
                multiShelf.HandlePlacementAnimationCancelled(product);
            }
            
            // Удаляем контекст
            _activeAnimations.Remove(context);
        }
        else
        {
            Debug.LogWarning($"PlayerHandVisualsController: No animation context found for cancelled placement animation of {product?.ProductName}");
        }
    }
} 