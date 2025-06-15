using UnityEngine;
using BehaviourInject;
using Supermarket.Services.PlayerData;
using Supermarket.Services.UI;
using Supermarket.UI;

public class MenuContextInitiator : MonoBehaviour
{
    private const string CONTEXT_NAME = "Menu";
    private Context _menuContext;
    
    [Header("Services")]
    [SerializeField] private NotificationService _notificationService; // опционально
    
    [Header("UI Handlers")]
    [SerializeField] private MainMenuScreen _mainMenuScreen;

    void Awake()
    {
        // Создаем Menu контекст, наследуемый от Common (который наследуется от Application)
        _menuContext = Context.Create(CONTEXT_NAME)
                              .SetParentContext("Common");
        
        // НЕ создаем новые экземпляры сервисов - они уже есть в родительских контекстах
        // Только добавляем специфичные для меню сервисы
        
        // Input Mode Service - создаем только для меню
        var inputModeService = new InputModeService();
        inputModeService.SetInputMode(InputMode.UI); // В меню всегда UI режим
        _menuContext.RegisterDependency<IInputModeService>(inputModeService);
        
        // UI Service - для уведомлений в меню (опционально)
        if (_notificationService != null)
        {
            _menuContext.RegisterDependency<INotificationService>(_notificationService);
        }
        
        Debug.Log($"MenuContextInitiator: {CONTEXT_NAME} context created and services registered.");
    }
    
    void Start()
    {
        // Получаем UINavigationService из родительского контекста и устанавливаем контекст меню
        var uiNavigationService = _menuContext.Resolve<IUINavigationService>();
        if (uiNavigationService != null)
        {
            // Устанавливаем контекст главного меню
            uiNavigationService.SetContext(UIContext.MainMenu);
            
            // MainMenuScreen автоматически зарегистрируется через OnEnable
            if (_mainMenuScreen != null)
            {
                Debug.Log("MenuContextInitiator: MainMenuScreen will be auto-registered in UINavigationService");
                
                // Показываем главное меню при старте (через UINavigationService)
                uiNavigationService.PushScreen(UIScreenType.MainMenu);
            }
            else
            {
                Debug.LogError("MenuContextInitiator: MainMenuScreen not assigned in inspector!");
            }
        }
        else
        {
            Debug.LogError("MenuContextInitiator: Could not resolve UINavigationService from parent context!");
        }
        
        // Очищаем сервисы Game контекста в SaveGameService, так как в меню они недоступны
        try
        {
            var saveGameService = _menuContext.Resolve<ISaveGameService>() as SaveGameService;
            if (saveGameService != null)
            {
                saveGameService.ClearPlacementService();
                saveGameService.ClearBoxManagerService();
                saveGameService.ClearPlayerDataProvider();
                saveGameService.ClearShelfManagerService();
                Debug.Log("MenuContextInitiator: Game context services cleared in SaveGameService for menu scene.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MenuContextInitiator: Failed to clear Game context services in SaveGameService: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (_menuContext != null && !_menuContext.IsDestroyed)
        {
            _menuContext.Destroy();
            Debug.Log($"MenuContextInitiator: {CONTEXT_NAME} context destroyed.");
        }
    }
} 