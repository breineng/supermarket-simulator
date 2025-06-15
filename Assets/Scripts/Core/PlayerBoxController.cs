using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;
using Supermarket.Services.Game; // Включает IPlayerHandService, IPlacementService, IInputModeService, IInteractionService, IBoxManagerService
using Services.UI; // Для INotificationService
using Supermarket.Interactables; // Added for BoxController

public class PlayerBoxController : MonoBehaviour
{
    [Inject]
    public IPlayerHandService _playerHandService;
    [Inject]
    public IPlacementService _placementService; // Will be needed for placing items
    [Inject]
    public IInputModeService _inputModeService; // May be needed to manage modes
    [Inject]
    public IInteractionService _interactionService; // Для получения текущего объекта в фокусе
    [Inject]
    public IBoxManagerService _boxManagerService; // Для создания коробок при выбрасывании

    // Ссылки на действия ввода
    private PlayerInput _playerInput;
    private InputAction _toggleBoxStateAction; // New unified action
    private InputAction _dropBoxAction;
    private InputAction _placeItemAction; // For LKM
    private InputAction _takeItemAction;  // For RKM

    [Header("Box Dropping")]
    [SerializeField] 
    private float _dropDistance = 1.0f; // Как далеко перед игроком появится коробка
    [SerializeField]
    private float _dropHeight = 1.2f; // Высота дропа относительно позиции игрока (примерно на уровне рук)
    [SerializeField] 
    private float _dropUpwardForce = 0.5f; // Небольшая сила вверх при выбрасывании
    [SerializeField] 
    private float _dropForwardForce = 2.0f; // Сила вперед при выбрасывании

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerBoxController: PlayerInput component not found on this GameObject.", this);
            enabled = false;
            return;
        }

        _toggleBoxStateAction = _playerInput.actions["ToggleBoxState"]; // Initialize new action
        _dropBoxAction = _playerInput.actions["DropBox"];
        _placeItemAction = _playerInput.actions["ConfirmPlacement"]; // Use ConfirmPlacement for placing items on shelf
        _takeItemAction = _playerInput.actions["CancelPlacement"]; // Use CancelPlacement for taking items from shelf

        if (_toggleBoxStateAction == null) Debug.LogError("Action 'ToggleBoxState' not found!", this);
        if (_dropBoxAction == null) Debug.LogError("Action 'DropBox' not found!", this);
        if (_placeItemAction == null) Debug.LogError("Action 'ConfirmPlacement' (for PlaceItem on shelf) not found!", this);
        if (_takeItemAction == null) Debug.LogError("Action 'CancelPlacement' (for TakeItem from shelf) not found!", this);
    }

    void OnEnable()
    {
        if (_toggleBoxStateAction != null) _toggleBoxStateAction.performed += OnToggleBoxStatePerformed;
        if (_dropBoxAction != null) _dropBoxAction.performed += OnDropBoxPerformed;
        if (_placeItemAction != null) _placeItemAction.performed += OnPlaceItemPerformed;
        if (_takeItemAction != null) _takeItemAction.performed += OnTakeItemPerformed;
    }

    void OnDisable()
    {
        if (_toggleBoxStateAction != null) _toggleBoxStateAction.performed -= OnToggleBoxStatePerformed;
        if (_dropBoxAction != null) _dropBoxAction.performed -= OnDropBoxPerformed;
        if (_placeItemAction != null) _placeItemAction.performed -= OnPlaceItemPerformed;
        if (_takeItemAction != null) _takeItemAction.performed -= OnTakeItemPerformed;
    }

    private void OnToggleBoxStatePerformed(InputAction.CallbackContext context)
    {
        if (_playerHandService == null || _placementService == null || _inputModeService == null) return;

        if (_playerHandService.IsHoldingBox())
        {
            if (_playerHandService.IsBoxOpen())
            {
                // Коробка была открыта, теперь закрываем
                _playerHandService.CloseBox();
                // Если был активен режим размещения, отменяем его
                if (_placementService.IsInPlacementMode) 
                {
                    // Проверяем, что отменяемый предмет - это тот, что в руке, чтобы не отменить что-то другое случайно
                    // Это может быть излишним, если CancelPlacementMode всегда безопасен, но для надежности:
                    ProductConfig productInHand = _playerHandService.GetProductInHand(); // Может быть null, если коробка опустела до закрытия
                    if (productInHand != null && _placementService.GetCurrentObjectType() == productInHand.ObjectCategory)
                    {
                        _placementService.CancelPlacementMode();
                    }
                    else if (productInHand == null && _placementService.IsInPlacementMode)
                    {
                        // Если рука уже пуста (последний предмет размещен, но коробка не закрылась автоматически)
                        // и режим размещения все еще активен (хотя PlacementService должен был его сбросить), отменяем.
                        _placementService.CancelPlacementMode();
                    }
                }
                Debug.Log("PlayerBoxController: ToggleBoxState - Box Closed.");
            }
            else // Коробка была закрыта, теперь открываем
            {
                _playerHandService.OpenBox();
                Debug.Log("PlayerBoxController: ToggleBoxState - Box Opened.");

                // Пытаемся активировать режим размещения для предмета в руке,
                // НО только если это не товар для полки.
                if (_inputModeService.CurrentMode == InputMode.Game) // Убедимся, что мы в игровом режиме
                {
                    ProductConfig productToPlace = _playerHandService.GetProductInHand();
                    if (productToPlace != null && _playerHandService.GetQuantityInHand() > 0)
                    {
                        // Если товар предназначен для полки, НЕ запускаем общий режим размещения.
                        // Игрок будет использовать ЛКМ/ПКМ для взаимодействия с полками.
                        if (productToPlace.CanBePlacedOnShelf)
                        {
                            Debug.Log($"PlayerBoxController: Box with '{productToPlace.ProductName}' opened. This item is for shelves. General placement mode NOT started.");
                        }
                        // Если это не товар для полки, но имеет категорию для размещения (не None), тогда запускаем режим.
                        else if (productToPlace.ObjectCategory != PlaceableObjectType.None) 
                        {
                            if (_placementService.IsInPlacementMode)
                            {
                                _placementService.CancelPlacementMode();
                            }
                            Debug.Log($"PlayerBoxController: Box with '{productToPlace.ProductName}' opened. Attempting to start general placement mode.");
                            _placementService.StartPlacementMode(productToPlace);
                        }
                        else
                        {
                            Debug.Log($"PlayerBoxController: Box with '{productToPlace.ProductName}' opened. Item is not for shelves and has no placement category. General placement mode NOT started.");
                        }
                    }
                    else
                    {
                        Debug.Log("PlayerBoxController: Box opened, but no items or product is null.");
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerBoxController: Box opened, but not in Game input mode. Placement not started.");
                }
            }
        }
    }

    private void OnDropBoxPerformed(InputAction.CallbackContext context)
    {
        if (_playerHandService == null || !_playerHandService.IsHoldingBox() || _boxManagerService == null)
        {
            if (_boxManagerService == null) Debug.LogError("PlayerBoxController: BoxManagerService is not available!");
            else Debug.Log("PlayerBoxController: Cannot drop box - not holding one or service unavailable.");
            return;
        }

        if (_playerHandService.IsBoxOpen() && _placementService.IsInPlacementMode)
        {
            _placementService.CancelPlacementMode();
        }
        if (_playerHandService.IsBoxOpen())
        {
            _playerHandService.CloseBox(); 
        }

        BoxData heldBoxData = _playerHandService.GetHeldBoxData();
        if (heldBoxData != null)
        {
            // Обновляем Debug.Log для корректной работы с ProductInBox == null
            string productName = heldBoxData.ProductInBox != null ? heldBoxData.ProductInBox.ProductName : "<Empty Generic Box>";
            Debug.Log($"PlayerBoxController: DropBox action performed for {productName} x {heldBoxData.Quantity}.");
            
            // Player's position and orientation for spawning
            Transform playerTransform = this.transform; // Assuming PlayerBoxController is on the Player GameObject
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * _dropDistance + playerTransform.up * _dropHeight; 
            
            // Вычисляем начальную скорость для выбрасывания коробки
            Vector3 initialVelocity = (playerTransform.forward * _dropForwardForce) + (playerTransform.up * _dropUpwardForce);
            
            // Используем BoxManagerService для создания коробки с физикой и начальной скоростью
            _boxManagerService.CreateBox(heldBoxData, spawnPos, true, initialVelocity);

            _playerHandService.ClearHand(); 
        }
    }

    private void OnPlaceItemPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("PlayerBoxController.OnPlaceItemPerformed: Method called");
        
        if (_playerHandService == null || !_playerHandService.IsHoldingBox() || !_playerHandService.IsBoxOpen())
        {
            Debug.Log($"PlayerBoxController.OnPlaceItemPerformed: Early return - HandService: {_playerHandService != null}, HoldingBox: {_playerHandService?.IsHoldingBox()}, BoxOpen: {_playerHandService?.IsBoxOpen()}");
            return;
        }
        
        // If general placement mode is active, let it handle the click.
        // This new logic is for specific shelf interaction.
        if (_placementService != null && _placementService.IsInPlacementMode) 
        {
             // Let PlacementService handle its own input confirmation if it's active.
             // This click might be for confirming a placement started by opening the box.
             // PlacementService should handle its own 'ConfirmPlacement' input.
             // For now, we assume if IsInPlacementMode is true, this LKM is for that.
            return; 
        }

        // Используем текущий объект в фокусе вместо нового raycast
        if (_interactionService == null || _interactionService.CurrentFocusedInteractable == null)
        {
            Debug.Log("PlayerBoxController.OnPlaceItemPerformed: No focused interactable");
            return;
        }
        
        // Проверяем, является ли объект в фокусе полкой
        ShelfController shelf = _interactionService.CurrentFocusedInteractable as ShelfController;
            if (shelf != null)
            {
                ProductConfig productInHand = _playerHandService.GetProductInHand(); // Продукт до вычитания
            Debug.Log($"PlayerBoxController: Found shelf in focus, product in hand: {productInHand?.ProductName ?? "null"}");
            
                if ((productInHand != null && shelf.CanPlaceFromOpenBox(productInHand)))
                {
                Debug.Log($"PlayerBoxController: Can place {productInHand.ProductName} on shelf, proceeding...");
                    // Только убираем товар из коробки - размещение на полке произойдет после анимации
                    _playerHandService.ConsumeItemFromHand(1);
                    
                    // Убираем автоматическое закрытие коробки, когда она опустеет.
                    // Коробка останется в руках открытой и пустой.
                    // if (_playerHandService.GetQuantityInHand() == 0)
                    // {
                    //    _playerHandService.CloseBox(); 
                    //    if (_placementService != null && _placementService.IsInPlacementMode) 
                    //    {
                    //        _placementService.CancelPlacementMode();
                    //    }
                    // }
            }
        }
        else
        {
            // Проверяем многоуровневую полку
            MultiLevelShelfController multiShelf = _interactionService.CurrentFocusedInteractable as MultiLevelShelfController;
            if (multiShelf != null)
            {
                Debug.Log($"PlayerBoxController: Found multi-level shelf in focus");
                
                ProductConfig productInHand = _playerHandService.GetProductInHand();
                if (productInHand != null && _playerHandService.GetQuantityInHand() > 0)
                {
                    // Проверяем, может ли полка принять товар с учетом летящих товаров
                    if (multiShelf.CanAcceptItemFromBox(productInHand))
                    {
                        // Убираем товар из коробки - размещение на полке произойдет после анимации
                        _playerHandService.ConsumeItemFromHand(1);
                        Debug.Log($"PlayerBoxController: Consumed {productInHand.ProductName} from box for multi-shelf placement");
                    }
                    else
                    {
                        Debug.Log($"PlayerBoxController: Cannot place {productInHand.ProductName} on multi-shelf - level is full or incompatible (including flying items)");
                    }
                }
            }
        }
    }

    private void OnTakeItemPerformed(InputAction.CallbackContext context)
    {
        if (_playerHandService == null || !_playerHandService.IsHoldingBox() || !_playerHandService.IsBoxOpen())
            return;

        // This action should not interfere with general placement mode cancellation (usually Escape or RKM).
        // So, if placement mode is active, we probably don't want RKM to also try to take from a shelf.
        // However, our current logic for opening a box immediately tries to start placement mode.
        // We need to be careful here.
        // For now, let's assume if placement mode is active for a DIFFERENT item, this RKM should still work for shelves.
        // But if placement mode is active for the item we are holding (e.g. from box opening), then it should be cancelled.

        // If general placement mode is active (e.g. placing a cash register) and it uses RKM for cancel, we should let it.
        // This specific RKM is for taking from shelf to an OPEN box.
        // Let's bypass if PlacementService is active AND intends to use RKM for cancel.
        // For now, to avoid conflict, if IsInPlacementMode, we return. This means RKM won't take from shelf if some other placement is active.
        // This might need more nuanced handling if PlacementService's Cancel is also RKM.
        if (_placementService != null && _placementService.IsInPlacementMode)
        {
             // Consider if this specific RKM should cancel placement mode if product matches?
             // For now, just return to prevent interference.
            return; 
        }

        // Используем текущий объект в фокусе вместо нового raycast
        if (_interactionService == null || _interactionService.CurrentFocusedInteractable == null)
        {
            return;
        }
        
        // Проверяем, является ли объект в фокусе полкой
        ShelfController shelf = _interactionService.CurrentFocusedInteractable as ShelfController;
            if (shelf != null)
            {
                ProductConfig productInOpenBox = _playerHandService.GetProductInHand(); // Тип продукта в открытой коробке
                ProductConfig productOnShelf = shelf.acceptedProduct; // Запоминаем продукт на полке ПЕРЕД тем, как он может стать null

                if (productOnShelf == null) // Если на полке ничего нет (acceptedProduct is null), то и брать нечего
                {
                    return;
                }

                if (shelf.CanTakeToOpenBox(productInOpenBox)) // Проверяем, можем ли мы взять товар с полки В ЭТУ КОРОБКУ
                {
                    // Сразу удаляем товар с полки для бесшовной анимации
                    if (shelf.TakeItemToOpenBox()) // Удаляем товар с полки (внутри уже уменьшается счетчик анимаций)
                    {
                        _playerHandService.AddItemToOpenBox(productOnShelf, 1); // Добавляем в коробку
                        Debug.Log($"PlayerBoxController: Removed {productOnShelf.ProductName} from shelf and added to box");
                    }
                    else
                    {
                        Debug.LogError($"PlayerBoxController: Failed to take item from shelf despite CanTakeToOpenBox returning true");
                    }
                }
            }
        else
        {
            // Проверяем многоуровневую полку
            MultiLevelShelfController multiShelf = _interactionService.CurrentFocusedInteractable as MultiLevelShelfController;
            if (multiShelf != null)
            {
                Debug.Log($"PlayerBoxController: Found multi-level shelf in focus for taking");
                
                // Используем TryTakeToOpenBox который сразу удаляет товар и добавляет в коробку
                if (!multiShelf.TryTakeToOpenBox())
                {
                    Debug.Log($"PlayerBoxController: Could not take item from multi-level shelf");
                }
            }
        }
    }
} 