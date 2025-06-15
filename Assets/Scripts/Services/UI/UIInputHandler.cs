using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;

namespace Supermarket.Services.UI
{
    /// <summary>
    /// Central input handler for UI navigation actions like ESC (Cancel) and Menu button
    /// </summary>
    [RequireComponent(typeof(PlayerInput))] // Гарантируем наличие PlayerInput
    public class UIInputHandler : MonoBehaviour
    {
        [Inject] public IUINavigationService _uiNavigationService;
        
        private PlayerInput _playerInput;
        private InputAction _cancelAction;
        
        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("UIInputHandler: PlayerInput component not found!");
                enabled = false;
                return;
            }
            
            // Находим действие Cancel (ESC key)
            _cancelAction = _playerInput.actions.FindAction("Cancel");
            if (_cancelAction == null)
            {
                Debug.LogError("UIInputHandler: 'Cancel' action not found in PlayerInputActions! Check the action name.");
                enabled = false;
            }
        }
        
        void OnEnable()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed += OnCancelPerformed;
            }
        }
        
        void OnDisable()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancelPerformed;
            }
        }
        
        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("UIInputHandler: Cancel action (ESC) performed");
            
            if (_uiNavigationService != null)
            {
                _uiNavigationService.HandleBackAction();
            }
            else
            {
                Debug.LogError("UIInputHandler: UINavigationService is not injected!");
            }
        }
        
        // Method to handle menu toggle (can be called from other scripts or bound to different input)
        public void HandleMenuToggle()
        {
            Debug.Log("UIInputHandler: Menu toggle requested");
            
            if (_uiNavigationService != null)
            {
                _uiNavigationService.HandleMenuAction();
            }
            else
            {
                Debug.LogError("UIInputHandler: UINavigationService is not injected!");
            }
        }
    }
} 