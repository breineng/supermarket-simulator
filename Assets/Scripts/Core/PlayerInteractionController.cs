using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;
using Core.Interfaces; // Assuming IInteractable is in this namespace
using Services.UI; // Добавляем using для нашего сервиса
using Core.Models; // <--- Добавляем using
using Supermarket.Services.Game; // Для IInteractionService

public class PlayerInteractionController : MonoBehaviour
{
    public float interactionDistance = 3f;
    public LayerMask interactionLayer; // Set this in the inspector to specify which layers to interact with
    public Camera playerCamera; // Assign your player's main camera

    private IInteractable _currentInteractable;
    private IInteractable _focusedInteractable;
    private string _lastPromptText = "";

    // Assuming you have an InputActionAsset and an "Interact" action in a "Player" action map
    private PlayerInput _playerInput;
    private InputAction _interactAction;
    private InputAction _relocateAction; // Добавляем действие для перемещения

    [Inject] // Теперь это будет использоваться
    public IInteractionPromptService _promptService { get; set; } 
    
    [Inject] // Добавляем зависимость IPlayerHandService
    public IPlayerHandService _playerHandService { get; set; }
    
    [Inject] // Добавляем зависимость IInteractionService
    public IInteractionService _interactionService { get; set; }
    
    [Inject] // Добавляем зависимость IInputModeService  
    public IInputModeService _inputModeService { get; set; }

    [Inject] // Добавляем зависимость IPlacementService для перемещения  
    public IPlacementService _placementService { get; set; }

    // Ссылка на компонент продажи объектов
    private Supermarket.Components.PlaceableObjectSeller _placeableObjectSeller;

    void Awake()
    {
        Debug.Log($"PlayerInteractionController: Awake - _promptService is {(_promptService == null ? "NULL" : "NOT NULL")}");

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteractionController: Player camera not found. Please assign it or ensure a MainCamera exists.", this);
                enabled = false;
                return;
            }
        }

        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
             Debug.LogWarning("PlayerInteractionController: PlayerInput component not found. Attempting to find 'Interact' action manually. Consider adding PlayerInput component for better input management.");
            // Try to find the action manually if PlayerInput is not used directly here.
            // This part might need adjustment based on how your InputActions are set up.
            var inputActionAsset = Resources.Load<InputActionAsset>("InputActions"); // Assuming your asset is named InputActions.inputactions and is in a Resources folder
            if (inputActionAsset != null) {
                _interactAction = inputActionAsset.FindActionMap("Player", throwIfNotFound: true).FindAction("Interact", throwIfNotFound: true);
            } else {
                 Debug.LogError("PlayerInteractionController: InputActionAsset 'InputActions' not found in Resources. Interaction will not work.", this);
                 enabled = false;
                 return;
            }
        }
        else
        {
            _interactAction = _playerInput.actions["Interact"];
        }

        _relocateAction = _playerInput.actions["RelocateObject"];
        if (_relocateAction == null)
        {
            Debug.LogError("PlayerInteractionController: 'RelocateObject' action not found. Relocate functionality will not work.", this);
        }

        if (_interactAction == null)
        {
            Debug.LogError("PlayerInteractionController: 'Interact' action not found. Please ensure it is defined in your InputActions.", this);
            enabled = false;
            return;
        }
    }

    void Start() // Добавим проверку и в Start, так как зависимости могут внедряться между Awake и Start
    {
        Debug.Log($"PlayerInteractionController: Start - _promptService is {(_promptService == null ? "NULL" : "NOT NULL")}");
        
        // Получаем ссылку на компонент продажи объектов
        _placeableObjectSeller = GetComponent<Supermarket.Components.PlaceableObjectSeller>();
        if (_placeableObjectSeller == null)
        {
            Debug.LogWarning("PlayerInteractionController: PlaceableObjectSeller component not found. Sell hints will not be available.");
        }
        
        // Подписываемся на события изменения состояния руки после инициализации всех сервисов
        if (_playerHandService != null)
        {
            _playerHandService.OnHandContentChanged += OnPlayerHandChanged;
            _playerHandService.OnBoxStateChanged += OnPlayerHandChanged;
            Debug.Log("PlayerInteractionController: Subscribed to PlayerHandService events.");
        }
        else
        {
            Debug.LogError("PlayerInteractionController: _playerHandService is null in Start! Cannot subscribe to events.");
        }
    }

    void OnEnable()
    {
        if (_interactAction != null)
        {
            _interactAction.performed += OnInteractPerformed;
        }
        
        if (_relocateAction != null)
        {
            _relocateAction.performed += OnRelocatePerformed;
        }
    }

    void OnDisable()
    {
        if (_interactAction != null)
        {
            _interactAction.performed -= OnInteractPerformed;
        }
        
        if (_relocateAction != null)
        {
            _relocateAction.performed -= OnRelocatePerformed;
        }
        // Отписываемся от событий при выключении
        if (_playerHandService != null)
        {
            _playerHandService.OnHandContentChanged -= OnPlayerHandChanged;
            _playerHandService.OnBoxStateChanged -= OnPlayerHandChanged;
             Debug.Log("PlayerInteractionController: Unsubscribed from PlayerHandService events.");
        }
        ClearFocus(); // Clear focus when disabled
        if (_promptService != null && _promptService.IsPromptVisible()) // Дополнительно скрываем, если была видна
        {
            _promptService.HidePrompt();
        }
    }

    void Update()
    {
        HandleInteractionFocus();
        
        // Периодически обновляем подсказку для текущего объекта в фокусе
        // Это нужно для объектов с динамическим состоянием (например, многоуровневые полки)
        if (_focusedInteractable != null)
        {
            UpdateFocusedInteractablePrompt();
        }
    }

    private void HandleInteractionFocus()
    {
        // Only handle interaction focus in Game and CashDeskOperation modes
        // In UI mode, we don't scan for new interactables but keep showing the prompt for the currently focused one
        if (_inputModeService != null && 
            _inputModeService.CurrentMode != InputMode.Game && 
            _inputModeService.CurrentMode != InputMode.CashDeskOperation)
        {
            // In UI mode, keep the current focus but don't scan for new ones
            if (_inputModeService.CurrentMode == InputMode.UI)
            {
                // Update the prompt for the currently focused interactable (if any)
                if (_focusedInteractable != null)
                {
                    UpdateFocusedInteractablePrompt();
                }
                return;
            }
            
            // Clear focus in other modes (MovingToCashDesk, etc.)
            if (_focusedInteractable != null)
            {
                ClearFocus();
            }
            return;
        }
        
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.yellow); // Рисуем луч каждую секунду

        IInteractable newFocusedInteractable = null;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
        {
            // Debug.Log($"PlayerInteractionController: Raycast hit {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}.");
            newFocusedInteractable = hit.collider.GetComponent<IInteractable>();
            if (newFocusedInteractable == null)
            {
                // Попробуем поискать на родительских объектах, если коллайдер на дочернем
                newFocusedInteractable = hit.collider.GetComponentInParent<IInteractable>();
                // if (newFocusedInteractable != null) Debug.Log("PlayerInteractionController: Found IInteractable on parent of a hit collider.");
            }
            
            // Если все еще не нашли, попробуем поискать в дочерних объектах от корня родителя
            if (newFocusedInteractable == null)
            {
                // Получаем корневой объект (самый верхний родитель)
                Transform root = hit.collider.transform.root;
                newFocusedInteractable = root.GetComponentInChildren<IInteractable>();
                if (newFocusedInteractable != null) 
                {
                    Debug.Log($"PlayerInteractionController: Found IInteractable in children of root object {root.name}.");
                }
            }
        }

        if (newFocusedInteractable != _focusedInteractable)
        {
            ClearFocus(); // Вызываем ClearFocus перед SetFocus, если фокус изменился
            if (newFocusedInteractable != null)
            {
                SetFocus(newFocusedInteractable);
            }
        }
    }

    private void SetFocus(IInteractable interactable)
    {
        Debug.Log($"PlayerInteractionController: SetFocus on {((MonoBehaviour)interactable).gameObject.name}");
        _focusedInteractable = interactable;
        _focusedInteractable.OnFocus(); // Вызов метода OnFocus у объекта
        
        // Сохраняем в сервис взаимодействий
        _interactionService?.SetFocusedInteractable(interactable);
        
        UpdateFocusedInteractablePrompt(); // Вызываем обновление подсказки при установке нового фокуса
    }

    private void UpdateFocusedInteractablePrompt()
    {
        if (_focusedInteractable == null) return;

        InteractionPromptData promptData = InteractionPromptData.Empty;
        try
        {
            promptData = _focusedInteractable.GetInteractionPrompt();
            // Debug.Log($"PlayerInteractionController.UpdateFocusedInteractablePrompt: Получена подсказка от {((MonoBehaviour)_focusedInteractable).gameObject.name}: Текст='{promptData.Text}', Тип='{promptData.Type}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PlayerInteractionController.UpdateFocusedInteractablePrompt: Ошибка при вызове GetInteractionPrompt(): {ex.Message}\n{ex.StackTrace}");
        }
        
        // Проверяем, можно ли добавить подсказку для перемещения
        string relocateHint = "";
        bool hasRelocateHint = false;
        
        if (CanShowRelocateHint())
        {
            GameObject focusedObject = ((MonoBehaviour)_focusedInteractable).gameObject;
            if (focusedObject.CompareTag("PlacedObject"))
            {
                relocateHint = GetRelocateHintText();
                hasRelocateHint = true;
            }
        }

        // Проверяем, можно ли добавить подсказку для продажи
        string sellHint = "";
        bool hasSellHint = false;
        
        if (_placeableObjectSeller != null)
        {
            GameObject focusedObject = ((MonoBehaviour)_focusedInteractable).gameObject;
            sellHint = _placeableObjectSeller.GetSellHint(focusedObject);
            hasSellHint = !string.IsNullOrEmpty(sellHint);
        }
        
        // Формируем финальную подсказку
        InteractionPromptData finalPromptData;
        
        if (hasRelocateHint || hasSellHint)
        {
            // Если есть дополнительные подсказки, комбинируем
            string combinedText;
            if (!string.IsNullOrEmpty(promptData.Text))
            {
                // Если оригинальная подсказка RawAction, сначала преобразуем её в Complete формат
                if (promptData.Type == PromptType.RawAction)
                {
                    string keyBinding = GetInteractKeyBinding();
                    string originalComplete = $"Нажмите [{keyBinding.ToUpper()}] чтобы {promptData.Text.ToLower()}";
                    combinedText = originalComplete;
                }
                else
                {
                    combinedText = promptData.Text;
                }
            }
            else
            {
                combinedText = "";
            }

            // Добавляем подсказку для перемещения
            if (hasRelocateHint)
            {
                if (!string.IsNullOrEmpty(combinedText))
                    combinedText += "\n";
                combinedText += relocateHint;
            }

            // Добавляем подсказку для продажи
            if (hasSellHint)
            {
                if (!string.IsNullOrEmpty(combinedText))
                    combinedText += "\n";
                combinedText += sellHint;
            }
            
            // Комбинированная подсказка всегда Complete
            finalPromptData = new InteractionPromptData(combinedText, PromptType.Complete);
        }
        else
        {
            // Если нет дополнительных подсказок, используем оригинальную как есть
            finalPromptData = promptData;
        }
        
        // Обновляем подсказку только если текст изменился
        if (finalPromptData.Text != _lastPromptText)
        {
            _lastPromptText = finalPromptData.Text;
            ShowPrompt(finalPromptData);
        }
    }

    /// <summary>
    /// Получает клавишу для основного взаимодействия
    /// </summary>
    private string GetInteractKeyBinding()
    {
        if (_interactAction != null)
        {
            string keyBinding = _interactAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(keyBinding))
            {
                return keyBinding;
            }
        }
        return "E"; // Fallback
    }

    /// <summary>
    /// Проверяет, можно ли показать подсказку для перемещения
    /// </summary>
    private bool CanShowRelocateHint()
    {
        return _placementService != null && 
               _inputModeService != null && 
               _inputModeService.CurrentMode == InputMode.Game &&
               !_placementService.IsInPlacementMode && 
               !_placementService.IsInRelocateMode;
    }

    /// <summary>
    /// Получает текст подсказки для перемещения
    /// </summary>
    private string GetRelocateHintText()
    {
        string relocateKeyBinding = "R"; // Fallback
        
        if (_relocateAction != null)
        {
            string keyBinding = _relocateAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(keyBinding))
            {
                relocateKeyBinding = keyBinding;
            }
        }
        
        return $"Нажмите [{relocateKeyBinding.ToUpper()}] чтобы переместить";
    }

    // Новый метод для централизованного отображения подсказки
    private void ShowPrompt(InteractionPromptData promptData)
    {
        Debug.Log($"PlayerInteractionController.ShowPrompt: promptData.Text='{promptData.Text}', promptData.Type='{promptData.Type}', _promptService exists={_promptService != null}");

        if (_promptService == null) return; // Если сервис недоступен, ничего не делаем

        if (!promptData.IsEmpty)
        {
            if (promptData.Type == PromptType.Complete)
            {
                Debug.Log($"PlayerInteractionController.ShowPrompt: Showing Complete prompt: \"{promptData.Text}\"");
                _promptService.ShowPrompt(promptData.Text);
            }
            else // Type == RawAction
            {
                if (_interactAction != null)
                {
                    string keyBinding = _interactAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
                    if (string.IsNullOrEmpty(keyBinding))
                    {
                        keyBinding = "Клавиша не назначена"; // Fallback
                    }
                    string finalPrompt = $"Нажмите [{keyBinding.ToUpper()}] чтобы {promptData.Text.ToLower()}";
                    Debug.Log($"PlayerInteractionController.ShowPrompt: Showing RawAction prompt: \"{finalPrompt}\"");
                    _promptService.ShowPrompt(finalPrompt);
                }
                else
                {
                    // Fallback если _interactAction почему-то null, но мы уже проверяли это в Awake
                    Debug.LogWarning("PlayerInteractionController.ShowPrompt: _interactAction is null, showing RawAction text as is.");
                    _promptService.ShowPrompt(promptData.Text);
                }
            }
        }
        else if (_promptService != null) 
        {
             Debug.Log("PlayerInteractionController.ShowPrompt: promptData is empty, hiding prompt.");
            _promptService.HidePrompt(); 
        }
        // Если _promptService == null, то ничего не делаем, ошибки уже залогированы в Awake/Start
    }

    private void ClearFocus()
    {
        if (_focusedInteractable != null) // Check if the C# interface reference is not null
        {
            MonoBehaviour interactableMonoBehaviour = _focusedInteractable as MonoBehaviour;

            // Check if the underlying UnityEngine.Object still exists
            if (interactableMonoBehaviour != null) 
            {
                Debug.Log($"PlayerInteractionController: ClearFocus from {interactableMonoBehaviour.gameObject.name}");
                _focusedInteractable.OnBlur(); // Call OnBlur on the valid object
            }
            else
            {
                Debug.Log("PlayerInteractionController: ClearFocus - _focusedInteractable was non-null but is now destroyed or not a MonoBehaviour.");
            }

            // Hide prompt if it's visible
            if (_promptService != null && _promptService.IsPromptVisible())
            {
                // Debug.Log("PlayerInteractionController.ClearFocus: Calling _promptService.HidePrompt()"); // Optional debug log
                _promptService.HidePrompt();
            }
        }
        
        // Очищаем в сервисе взаимодействий
        _interactionService?.ClearFocusedInteractable();
        
        // Always null out the C# interface reference after handling it
        _focusedInteractable = null;
        _lastPromptText = ""; // Очищаем кэш подсказки
    }

    private void OnPlayerHandChanged()
    {
        // Если какой-либо объект в фокусе, обновить его подсказку, так как состояние руки изменилось
        if (_focusedInteractable != null)
        {
            Debug.Log("PlayerInteractionController: PlayerHandChanged triggered, updating focused prompt.");
            UpdateFocusedInteractablePrompt();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Only process interactions in Game and CashDeskOperation modes
        if (_inputModeService != null && 
            _inputModeService.CurrentMode != InputMode.Game && 
            _inputModeService.CurrentMode != InputMode.CashDeskOperation)
        {
            Debug.Log($"PlayerInteractionController: Interaction blocked in {_inputModeService.CurrentMode} mode");
            return;
        }
        
        if (_focusedInteractable != null)
        {
            Debug.Log($"Interacting with {_focusedInteractable}");
            IInteractable interactableToInteractWith = _focusedInteractable; // Копируем ссылку на случай, если ClearFocus изменит _focusedInteractable
            
            interactableToInteractWith.Interact(this.gameObject); // Pass the player GameObject as the interactor

            // После взаимодействия состояние могло измениться, поэтому обновим подсказку
            // Это особенно важно, если Interact() не приводит к ClearFocus() немедленно
            if (_focusedInteractable == interactableToInteractWith) // Если фокус все еще на том же объекте
            {
                 UpdateFocusedInteractablePrompt(); 
            }
            // Если Interact() привел к уничтожению объекта и ClearFocus(), то UpdateFocusedInteractablePrompt() не вызовется, что корректно.
        }
    }

    private void OnRelocatePerformed(InputAction.CallbackContext context)
    {
        if (_placementService == null || _inputModeService == null)
        {
            Debug.LogError("PlayerInteractionController: Required services for relocation are null.");
            return;
        }

        // Проверяем, что мы в игровом режиме
        if (_inputModeService.CurrentMode != InputMode.Game)
        {
            Debug.Log("PlayerInteractionController: Not in Game mode. Relocate action ignored.");
            return;
        }

        // Если уже в режиме размещения или перемещения, ничего не делаем
        if (_placementService.IsInPlacementMode || _placementService.IsInRelocateMode)
        {
            Debug.Log("PlayerInteractionController: Already in placement or relocate mode. Relocate action ignored.");
            return;
        }

        // Используем уже имеющийся _focusedInteractable вместо нового raycast
        if (_focusedInteractable != null)
        {
            GameObject hitObject = ((MonoBehaviour)_focusedInteractable).gameObject;
            
            // Проверяем, является ли объект размещенным объектом по тегу
            if (hitObject.CompareTag("PlacedObject"))
            {
                Debug.Log($"PlayerInteractionController: Attempting to relocate {hitObject.name}");
                bool success = _placementService.StartRelocateMode(hitObject);
                
                if (success)
                {
                    Debug.Log($"PlayerInteractionController: Successfully started relocate mode for {hitObject.name}");
                }
                else
                {
                    Debug.LogWarning($"PlayerInteractionController: Failed to start relocate mode for {hitObject.name}");
                }
            }
            else
            {
                Debug.Log($"PlayerInteractionController: Object {hitObject.name} is not a placed object that can be relocated.");
            }
        }
        else
        {
            Debug.Log("PlayerInteractionController: No object in focus. Nothing to relocate.");
        }
    }
} 