using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;
using Supermarket.Services.Game; // For IPlayerHandService

public class PlayerHandPlacementController : MonoBehaviour
{
    [Inject]
    public IPlayerHandService _playerHandService;
    [Inject]
    public IPlacementService _placementService;
    [Inject]
    public IInputModeService _inputModeService; 

    private PlayerInput _playerInput;
    private InputAction _placeFromHandAction;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerHandPlacementController: PlayerInput component not found.", this);
            enabled = false;
            return;
        }
        _placeFromHandAction = _playerInput.actions["PlaceFromHand"]; // Убедитесь, что действие так называется
        if (_placeFromHandAction == null)
        {
            Debug.LogError("PlayerHandPlacementController: 'PlaceFromHand' action not found. Please define it.", this);
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        if (_placeFromHandAction != null)
        {
            _placeFromHandAction.performed += OnPlaceFromHandPerformed;
        }
    }

    void OnDisable()
    {
        if (_placeFromHandAction != null)
        {
            _placeFromHandAction.performed -= OnPlaceFromHandPerformed;
        }
    }

    private void OnPlaceFromHandPerformed(InputAction.CallbackContext context)
    {
        if (_playerHandService == null || _placementService == null || _inputModeService == null)
        {
            Debug.LogError("PlayerHandPlacementController: One or more services are null.");
            return;
        }

        // Не начинать новый режим размещения, если уже в нем или если не в режиме игры
        if (_placementService.IsInPlacementMode || _inputModeService.CurrentMode != InputMode.Game)
        {
            // Можно добавить Debug.Log, если нужно знать, почему не сработало
            // Debug.Log($"PlayerHandPlacementController: Cannot start placement. InPlacementMode: {_placementService.IsInPlacementMode}, CurrentInputMode: {_inputModeService.CurrentMode}");
            return;
        }

        if (_playerHandService.IsHoldingBox())
        {
            ProductConfig productToPlace = _playerHandService.GetProductInHand();
            if (productToPlace != null)
            {
                Debug.Log($"PlayerHandPlacementController: Attempting to place {productToPlace.ProductName} from hand.");
                _placementService.StartPlacementMode(productToPlace);
                // PlacementService сам должен переключить InputMode в Game (для управления размещением)
                // или в специальный Placement режим, если такой будет. Сейчас он ставит Game.
            }
            else
            {
                Debug.LogWarning("PlayerHandPlacementController: Player is holding a box, but ProductConfig is null.");
            }
        }
        else
        {
            Debug.Log("PlayerHandPlacementController: Player is not holding a box. Cannot place from hand.");
            // Здесь можно решить, должна ли эта кнопка также открывать общее меню строительства, если руки пусты
            // Например, вызвать метод из GameUIHandler: _gameUIHandler.TogglePlacementPanel();
            // Но для этого нужна ссылка на GameUIHandler.
        }
    }
} 