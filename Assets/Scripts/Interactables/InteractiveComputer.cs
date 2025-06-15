using UnityEngine;
using BehaviourInject;
using Core.Interfaces; // Make sure this namespace is correct for IInteractable
using Core.Models;   // <--- Добавляем using
using Supermarket.Services.UI; // Для UI Navigation

public class InteractiveComputer : MonoBehaviour, IInteractable
{
    [Tooltip("Ссылка на GameObject, содержащий UIDocument и ComputerUIHandler для UI компьютера. Должен быть частью префаба этого компьютера.")]
    public GameObject computerUIHolder;

    [Inject] public IInputModeService _inputModeService { get; set; } // Public for BInject
    [Inject] public IUINavigationService _uiNavigationService { get; set; } // Добавляем UI Navigation

    private bool _isComputerUIVisible = false;

    void Start()
    {
        // Ensure the computer UI is initially hidden if it's not already.
        if (computerUIHolder != null)
        {
            _isComputerUIVisible = false;
        }
        else
        {
            Debug.LogError("InteractiveComputer: ComputerUIHolder не назначен!", this);
        }
        
        // Подписываемся на изменения экрана чтобы отслеживать закрытие интерфейса компьютера
        if (_uiNavigationService != null)
        {
            _uiNavigationService.OnScreenChanged += OnScreenChanged;
        }
    }
    
    void OnDestroy()
    {
        // Отписываемся от событий
        if (_uiNavigationService != null)
        {
            _uiNavigationService.OnScreenChanged -= OnScreenChanged;
        }
    }
    
    private void OnScreenChanged(UIScreenType newScreen)
    {
        // Если интерфейс компьютера больше не активен, обновляем локальное состояние
        if (_isComputerUIVisible && newScreen != UIScreenType.ComputerUI)
        {
            _isComputerUIVisible = false;
            Debug.Log("InteractiveComputer: Компьютер закрыт через UI Navigation.");
        }
    }

    public void Interact(GameObject interactor) // interactor is the player or whoever initiated
    {
        if (computerUIHolder == null)
        {
            Debug.LogError("InteractiveComputer: ComputerUIHolder не назначен в инспекторе!", this);
            return;
        }

        if (_uiNavigationService == null)
        {
            Debug.LogError("InteractiveComputer: IUINavigationService не внедрен! Убедитесь, что Injector есть на этом объекте или его родителе.", this);
            return;
        }

        // Открываем интерфейс компьютера только если он не открыт
        // Закрытие теперь происходит через ESC (обрабатывается ComputerScreen)
        if (!_isComputerUIVisible)
        {
            // Открываем интерфейс компьютера через UI Navigation систему
            _uiNavigationService.PushScreen(UIScreenType.ComputerUI);
            _isComputerUIVisible = true;
            Debug.Log("Компьютер активирован через UI Navigation.");
        }
        else
        {
            Debug.Log("Компьютер уже открыт. Используйте ESC для закрытия.");
        }
    }

    public InteractionPromptData GetInteractionPrompt() // <--- Меняем тип
    {
        // Теперь всегда показываем "использовать компьютер", так как закрытие происходит через ESC
        string text = "использовать компьютер";
        return new InteractionPromptData(text, PromptType.RawAction); // <--- Всегда RawAction для компьютера
    }

    public void OnFocus()
    {
        // Optional: Add any visual feedback when the player looks at the computer
        // e.g., highlight, outline. For now, just a log.
        Debug.Log("InteractiveComputer focused.", this);
    }

    public void OnBlur()
    {
        // Optional: Remove visual feedback
        Debug.Log("InteractiveComputer blurred.", this);
    }

    // The Update, OnTriggerEnter, OnTriggerExit methods for direct input handling and range detection are removed.
    // PlayerInteractionController now handles detection and invokes Interact().
} 