using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;
using Supermarket.Interactables;
using Supermarket.Services.Game;

/// <summary>
/// Handles input for cash desk operations. Should be attached to the Player object.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class CashDeskInputController : MonoBehaviour
{
    [Inject]
    public IInputModeService _inputModeService;
    
    [Inject]
    public ICashDeskService _cashDeskService;

    private PlayerInput _playerInput;
    private InputAction _exitCashDeskAction;
    private InputAction _scanItemAction;
    private CashDeskController _currentCashDesk;
    private float _lastExitTime = 0f; // Для предотвращения спама
    private const float EXIT_COOLDOWN = 0.2f; // Минимальная задержка между выходами

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("CashDeskInputController: PlayerInput component not found!", this);
            enabled = false;
            return;
        }

        // Find the ExitCashDesk action
        _exitCashDeskAction = _playerInput.actions.FindAction("ExitCashDesk");
        if (_exitCashDeskAction == null)
        {
            Debug.LogError("CashDeskInputController: 'ExitCashDesk' action not found in PlayerInputActions! Please check the action name.", this);
            enabled = false;
        }

        // Find the ScanItem action
        _scanItemAction = _playerInput.actions.FindAction("ScanItem");
        if (_scanItemAction == null)
        {
            Debug.LogError("CashDeskInputController: 'ScanItem' action not found in PlayerInputActions! Please check the action name.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (_exitCashDeskAction != null)
        {
            _exitCashDeskAction.performed += OnExitCashDeskPerformed;
        }
        if (_scanItemAction != null)
        {
            _scanItemAction.performed += OnScanItemPerformed;
        }
    }

    void OnDisable()
    {
        if (_exitCashDeskAction != null)
        {
            _exitCashDeskAction.performed -= OnExitCashDeskPerformed;
        }
        if (_scanItemAction != null)
        {
            _scanItemAction.performed -= OnScanItemPerformed;
        }
    }

    private void OnScanItemPerformed(InputAction.CallbackContext context)
    {
        // Only handle input if we're in CashDeskOperation mode
        if (_inputModeService != null && _inputModeService.CurrentMode == InputMode.CashDeskOperation)
        {
            if (_currentCashDesk != null && _currentCashDesk.IsPlayerOperating)
            {
                _currentCashDesk.PlayerAttemptScan();
            }
        }
    }

    private void OnExitCashDeskPerformed(InputAction.CallbackContext context)
    {
        // Check cooldown to prevent spam
        if (Time.time - _lastExitTime < EXIT_COOLDOWN)
        {
            return;
        }
        
        // Only handle input if we're in CashDeskOperation mode
        if (_inputModeService != null && _inputModeService.CurrentMode == InputMode.CashDeskOperation)
        {
            _lastExitTime = Time.time;
            
            if (_currentCashDesk != null && _currentCashDesk.IsPlayerOperating)
            {
                _currentCashDesk.StopPlayerOperation();
            }
            else
            {
                // Fallback: find any operating cash desk using the service
                if (_cashDeskService != null)
                {
                    foreach (var cashDeskGo in _cashDeskService.GetAllCashDesks())
                    {
                        var cashDesk = cashDeskGo.GetComponent<CashDeskController>();
                        if (cashDesk != null && cashDesk.IsPlayerOperating)
                        {
                            cashDesk.StopPlayerOperation();
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Set the current cash desk that the player is operating (called by CashDeskController)
    /// </summary>
    public void SetCurrentCashDesk(CashDeskController cashDesk)
    {
        _currentCashDesk = cashDesk;
    }

    /// <summary>
    /// Clear the current cash desk reference (called by CashDeskController)
    /// </summary>
    public void ClearCurrentCashDesk()
    {
        _currentCashDesk = null;
    }
} 