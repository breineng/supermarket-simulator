using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;

[RequireComponent(typeof(PlayerInput))] // Гарантируем наличие PlayerInput
public class InputModeSwitcher : MonoBehaviour
{
    private IInputModeService _inputModeService;
    private IPlacementService _placementService;
    private PlayerInput _playerInput;
    private InputAction _toggleUIModeAction;

    [Inject]
    public void Construct(IInputModeService inputModeService, IPlacementService placementService)
    {
        _inputModeService = inputModeService;
        _placementService = placementService;
    }

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("InputModeSwitcher: PlayerInput component not found!");
            enabled = false; // Выключаем компонент, если нет PlayerInput
            return;
        }
        // Имя действия должно совпадать с тем, что вы указали в PlayerInputActions
        _toggleUIModeAction = _playerInput.actions.FindAction("ToggleUIMode"); 
        if (_toggleUIModeAction == null)
        {
            Debug.LogError("InputModeSwitcher: 'ToggleUIMode' action not found in PlayerInputActions! Check the action name.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (_toggleUIModeAction != null)
        {
            _toggleUIModeAction.performed += OnToggleUIModePerformed;
        }
    }

    void OnDisable()
    {
        if (_toggleUIModeAction != null)
        {
            _toggleUIModeAction.performed -= OnToggleUIModePerformed;
        }
    }

    private void OnToggleUIModePerformed(InputAction.CallbackContext context)
    {
        if (_inputModeService == null) return;

        if (_inputModeService.CurrentMode == InputMode.Game)
        {
            _inputModeService.SetInputMode(InputMode.UI);
            Debug.Log("InputModeSwitcher: Switched to UI mode by ToggleUIMode action.");
        }
        else // CurrentMode == InputMode.UI
        {
            if (_placementService == null || !_placementService.IsInPlacementMode) 
            {
                _inputModeService.SetInputMode(InputMode.Game);
                Debug.Log("InputModeSwitcher: Switched to Game mode by ToggleUIMode action (not in placement).");
            }
            else
            {
                Debug.Log("InputModeSwitcher: In UI mode and Placement Mode is active. ToggleUIMode action does not switch to Game mode here.");
            }
        }
    }

    // Убираем метод Update, так как он больше не нужен для Input.GetKeyDown
} 