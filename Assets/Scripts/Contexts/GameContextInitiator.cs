using UnityEngine;
using BehaviourInject;
using Services.UI; // Добавляем using для IInteractionPromptService
using Supermarket.Services.Game; // Using the new namespace for IDeliveryService
using Supermarket.Services.UI; // Using for INotificationService
using Supermarket.Services.PlayerData; // Using for IPlayerDataProvider
using Supermarket.UI; // Using for GameMenuHandler
using UnityEngine.UIElements;

public class GameContextInitiator : MonoBehaviour
{
    private const string CONTEXT_NAME = "Game";
    private Context _gameContext;

    [Header("Service Configurations")]
    [SerializeField]
    private PlacementServiceConfig _placementServiceConfig; // Ссылка на ScriptableObject с конфигурацией
    [SerializeField]
    private PurchaseDecisionConfig _purchaseDecisionConfig; // Ссылка на ScriptableObject с конфигурацией покупательских решений

    [Header("MonoBehaviour Services (Assign in Inspector)")]
    [SerializeField]
    private DeliveryService _deliveryServiceInstance; // Ссылка на экземпляр DeliveryService на сцене
    [SerializeField]
    private CustomerSpawnerService _customerSpawnerServiceInstance; // Ссылка на экземпляр CustomerSpawnerService на сцене
    [SerializeField]
    private NotificationService _notificationServiceInstance; // Ссылка на экземпляр NotificationService на сцене
    [SerializeField]
    private PlayerDataProvider _playerDataProviderInstance; // Ссылка на экземпляр PlayerDataProvider на игроке
    [SerializeField]
    private BoxManagerService _boxManagerServiceInstance; // Ссылка на экземпляр BoxManagerService на сцене
    [SerializeField]
    private PlayerHandVisualsService _playerHandVisualsServiceInstance; // Ссылка на экземпляр PlayerHandVisualsService на сцене
    [SerializeField]
    private StreetWaypointService _streetWaypointServiceInstance; // Ссылка на экземпляр StreetWaypointService на сцене
    [SerializeField]
    private ShoppingListGeneratorService _shoppingListGeneratorServiceInstance; // Ссылка на экземпляр ShoppingListGeneratorService на сцене

    
    [Header("UI Handlers")]
    [SerializeField]
    private GameMenuHandler _gameMenuHandler; // Ссылка на GameMenuHandler в игровой сцене
    [SerializeField]
    private UIDocument _gameHudDocument; // UIDocument с игровым HUD для InteractionPromptService
    [SerializeField]
    private GameHUDScreen _gameHUDScreen; // Ссылка на GameHUDScreen компонент
    [SerializeField]
    private UIInputHandler _uiInputHandler; // Ссылка на UIInputHandler для обработки ESC и навигации

    void Awake()
    {
        // Создаем Game контекст, наследуемый от Common (который наследуется от Application)
        _gameContext = Context.Create(CONTEXT_NAME)
                              .SetParentContext("Common");

        // Регистрируем сервисы GameContext
        // Для классов с зависимостями в конструкторе используем RegisterTypeAs,
        // чтобы BInject сам создал экземпляр и внедрил зависимости.
        // ProductCatalogService теперь регистрируется позже, после LicenseService
        // _gameContext.RegisterTypeAs<ProductCatalogService, IProductCatalogService>(); // УДАЛЕНО - регистрируется ниже
        _gameContext.RegisterDependencyAs<InventoryService, IInventoryService>(new InventoryService()); // InventoryService пока без зависимостей конструктора
        _gameContext.RegisterTypeAs<PlayerHandService, IPlayerHandService>(); // Регистрируем PlayerHandService
        
        // Регистрируем новые сервисы для улучшенной архитектуры
        _gameContext.RegisterTypeAs<InteractionService, IInteractionService>();
        _gameContext.RegisterTypeAs<CashDeskService, ICashDeskService>();
        _gameContext.RegisterTypeAs<StorePointsService, IStorePointsService>();
        
        // Сначала регистрируем PlacementServiceConfig как зависимость в контексте
        if (_placementServiceConfig == null)
        {
            Debug.LogError("GameContextInitiator: PlacementServiceConfig is not assigned in the Inspector! PlacementService will use default or fail if config is critical.", this);
            // Если конфиг критичен, можно здесь создать "пустой" или дефолтный экземпляр:
            // _placementServiceConfig = ScriptableObject.CreateInstance<PlacementServiceConfig>();
            // Или просто позволить PlacementService обработать null (он это делает и использует Physics.DefaultRaycastLayers)
        }
        else
        {
            _gameContext.RegisterDependency(_placementServiceConfig); // Регистрируем экземпляр конфига
            Debug.Log("GameContextInitiator: PlacementServiceConfig registered in context.");
        }
        
        // Регистрируем BoxManagerService (MonoBehaviour)
        if (_boxManagerServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: BoxManagerService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<BoxManagerService, IBoxManagerService>(_boxManagerServiceInstance);
            Debug.Log("GameContextInitiator: BoxManagerService registered in context.");
        }
        
        // Регистрируем сервис подсказок взаимодействия
        if (_gameHudDocument != null)
        {
            var interactionPromptService = new InteractionPromptService(_gameHudDocument);
            _gameContext.RegisterDependency<IInteractionPromptService>(interactionPromptService);
            Debug.Log("GameContextInitiator: InteractionPromptService registered in context.");
        }
        else
        {
            Debug.LogError("GameContextInitiator: GameHudDocument is not assigned in the Inspector! InteractionPromptService will not be available.", this);
        }
        
        _gameContext.RegisterTypeAs<PlacementService, IPlacementService>();
        Debug.Log("GameContextInitiator: PlacementService registered in context.");
        
        // Регистрируем ShelfManagerService как POCO (не MonoBehaviour)
        _gameContext.RegisterTypeAs<ShelfManagerService, IShelfManagerService>();
        Debug.Log("GameContextInitiator: ShelfManagerService registered as POCO service in context.");

        // Регистрируем CustomerManagerService как POCO (не MonoBehaviour)
        _gameContext.RegisterTypeAs<CustomerManagerService, ICustomerManagerService>();
        Debug.Log("GameContextInitiator: CustomerManagerService registered as POCO service in context.");

        // Регистрируем DeliveryService (MonoBehaviour)
        if (_deliveryServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: DeliveryService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<DeliveryService, IDeliveryService>(_deliveryServiceInstance);
            Debug.Log("GameContextInitiator: DeliveryService registered in context.");
        }
        
        // Регистрируем CustomerSpawnerService (MonoBehaviour)
        if (_customerSpawnerServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: CustomerSpawnerService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<CustomerSpawnerService, ICustomerSpawnerService>(_customerSpawnerServiceInstance);
            Debug.Log("GameContextInitiator: CustomerSpawnerService registered in context.");
        }
        
        // Регистрируем NotificationService (MonoBehaviour)
        if (_notificationServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: NotificationService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<NotificationService, INotificationService>(_notificationServiceInstance);
            Debug.Log("GameContextInitiator: NotificationService registered in context.");
        }
        
        // Регистрируем PlayerDataProvider (MonoBehaviour)
        if (_playerDataProviderInstance == null)
        {
            Debug.LogError("GameContextInitiator: PlayerDataProvider instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<PlayerDataProvider, IPlayerDataProvider>(_playerDataProviderInstance);
            Debug.Log("GameContextInitiator: PlayerDataProvider registered in context.");
        }
        
        // Регистрируем сервис названия супермаркета
        _gameContext.RegisterTypeAs<SupermarketNameService, ISupermarketNameService>();
        Debug.Log("GameContextInitiator: SupermarketNameService registered in context.");
        
        // Регистрируем сервисы цен и лицензий
        // RetailPriceService должен регистрироваться до ProductCatalogService, так как может потребоваться
        _gameContext.RegisterTypeAs<RetailPriceService, IRetailPriceService>();
        Debug.Log("GameContextInitiator: RetailPriceService registered in context.");
        
        // Регистрируем LicenseService (POCO)
        _gameContext.RegisterTypeAs<LicenseService, ILicenseService>();
        Debug.Log("GameContextInitiator: LicenseService registered in context.");
        
        // Регистрируем PurchaseDecisionConfig
        if (_purchaseDecisionConfig == null)
        {
            Debug.LogError("GameContextInitiator: PurchaseDecisionConfig is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependency(_purchaseDecisionConfig);
            Debug.Log("GameContextInitiator: PurchaseDecisionConfig registered in context.");
        }
        
        // Регистрируем PurchaseDecisionService (POCO с конфигом)
        _gameContext.RegisterTypeAs<PurchaseDecisionService, IPurchaseDecisionService>();
        Debug.Log("GameContextInitiator: PurchaseDecisionService registered in context.");
        
        // Теперь регистрируем ProductCatalogService, который зависит от GameConfigService и LicenseService
        _gameContext.RegisterTypeAs<ProductCatalogService, IProductCatalogService>();
        Debug.Log("GameContextInitiator: ProductCatalogService registered in context after GameConfigService and LicenseService.");
        
        // Регистрируем PlayerHandVisualsService (MonoBehaviour)
        if (_playerHandVisualsServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: PlayerHandVisualsService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<PlayerHandVisualsService, IPlayerHandVisualsService>(_playerHandVisualsServiceInstance);
            Debug.Log("GameContextInitiator: PlayerHandVisualsService registered in context.");
        }
        
        // Регистрируем StreetWaypointService (MonoBehaviour)
        if (_streetWaypointServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: StreetWaypointService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<StreetWaypointService, IStreetWaypointService>(_streetWaypointServiceInstance);
            Debug.Log("GameContextInitiator: StreetWaypointService registered in context.");
        }
        
        // Регистрируем ShoppingListGeneratorService (MonoBehaviour)
        if (_shoppingListGeneratorServiceInstance == null)
        {
            Debug.LogError("GameContextInitiator: ShoppingListGeneratorService instance is not assigned in the Inspector!", this);
        }
        else
        {
            _gameContext.RegisterDependencyAs<ShoppingListGeneratorService, IShoppingListGeneratorService>(_shoppingListGeneratorServiceInstance);
            Debug.Log("GameContextInitiator: ShoppingListGeneratorService registered in context.");
        }
        
        Debug.Log($"GameContextInitiator: {CONTEXT_NAME} context created, parented to Common, and services registered.");
    }
    
    void Start()
    {
        // Инициализируем UI Navigation систему
        InitializeUINavigation();
        
        // Устанавливаем IPlacementService в SaveGameService из CommonContext
        try
        {
            Debug.Log("GameContextInitiator: Attempting to resolve SaveGameService and PlacementService...");
            
            var saveGameService = _gameContext.Resolve<ISaveGameService>() as SaveGameService;
            var placementService = _gameContext.Resolve<IPlacementService>();
            
            Debug.Log($"GameContextInitiator: SaveGameService resolved: {saveGameService != null}, type: {saveGameService?.GetType().Name}");
            Debug.Log($"GameContextInitiator: PlacementService resolved: {placementService != null}, type: {placementService?.GetType().Name}");
            
            if (saveGameService != null && placementService != null)
            {
                saveGameService.SetPlacementService(placementService);
                Debug.Log("GameContextInitiator: IPlacementService successfully set in SaveGameService for placed objects support.");
                
                // Проверяем, есть ли выбранный файл для загрузки
                string selectedSaveFile = saveGameService.GetSelectedSaveFile();
                if (!string.IsNullOrEmpty(selectedSaveFile))
                {
                    Debug.Log($"GameContextInitiator: Found selected save file '{selectedSaveFile}', loading in game scene...");
                    
                    // Загружаем в следующем кадре, чтобы все сервисы успели инициализироваться
                    StartCoroutine(LoadSaveFileDelayed(saveGameService));
                }
            }
            else
            {
                Debug.LogWarning($"GameContextInitiator: Could not set IPlacementService in SaveGameService - SaveGameService: {saveGameService != null}, PlacementService: {placementService != null}");
            }
            
            // Устанавливаем BoxManagerService в SaveGameService
            if (saveGameService != null)
            {
                var boxManagerService = _gameContext.Resolve<IBoxManagerService>();
                if (boxManagerService != null)
                {
                    saveGameService.SetBoxManagerService(boxManagerService);
                    Debug.Log("GameContextInitiator: IBoxManagerService successfully set in SaveGameService for box save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve IBoxManagerService for SaveGameService.");
                }
                
                // Устанавливаем PlayerDataProvider в SaveGameService для сохранения позиции игрока
                var playerDataProvider = _gameContext.Resolve<IPlayerDataProvider>();
                if (playerDataProvider != null)
                {
                    saveGameService.SetPlayerDataProvider(playerDataProvider);
                    Debug.Log("GameContextInitiator: IPlayerDataProvider successfully set in SaveGameService for player position save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve IPlayerDataProvider for SaveGameService.");
                }
                
                // Устанавливаем ShelfManagerService в SaveGameService для сохранения состояния полок
                var shelfManagerService = _gameContext.Resolve<IShelfManagerService>();
                if (shelfManagerService != null)
                {
                    saveGameService.SetShelfManagerService(shelfManagerService);
                    Debug.Log("GameContextInitiator: IShelfManagerService successfully set in SaveGameService for shelf save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve IShelfManagerService for SaveGameService.");
                }
                
                // Устанавливаем CustomerManagerService в SaveGameService для сохранения состояния покупателей
                var customerManagerService = _gameContext.Resolve<ICustomerManagerService>();
                if (customerManagerService != null)
                {
                    saveGameService.SetCustomerManagerService(customerManagerService);
                    Debug.Log("GameContextInitiator: ICustomerManagerService successfully set in SaveGameService for customer save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve ICustomerManagerService for SaveGameService.");
                }
                
                // Устанавливаем DeliveryService в SaveGameService для сохранения активных заказов
                var deliveryService = _gameContext.Resolve<IDeliveryService>();
                if (deliveryService != null)
                {
                    saveGameService.SetDeliveryService(deliveryService);
                    Debug.Log("GameContextInitiator: IDeliveryService successfully set in SaveGameService for delivery save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve IDeliveryService for SaveGameService.");
                }
                
                // Устанавливаем LicenseService в SaveGameService для сохранения лицензий
                var licenseService = _gameContext.Resolve<ILicenseService>();
                if (licenseService != null)
                {
                    saveGameService.SetLicenseService(licenseService);
                    Debug.Log("GameContextInitiator: ILicenseService successfully set in SaveGameService for license save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve ILicenseService for SaveGameService.");
                }
                
                // Устанавливаем SupermarketNameService в SaveGameService для сохранения названия супермаркета
                var supermarketNameService = _gameContext.Resolve<ISupermarketNameService>();
                if (supermarketNameService != null)
                {
                    saveGameService.SetSupermarketNameService(supermarketNameService);
                    Debug.Log("GameContextInitiator: ISupermarketNameService successfully set in SaveGameService for supermarket name save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve ISupermarketNameService for SaveGameService.");
                }
                
                // Устанавливаем RetailPriceService в SaveGameService для сохранения кастомных цен
                var retailPriceService = _gameContext.Resolve<IRetailPriceService>();
                if (retailPriceService != null)
                {
                    saveGameService.SetRetailPriceService(retailPriceService);
                    Debug.Log("GameContextInitiator: IRetailPriceService successfully set in SaveGameService for custom prices save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve IRetailPriceService for SaveGameService.");
                }

                // Устанавливаем PlayerHandService в SaveGameService для сохранения коробки в руках
                var playerHandService = _gameContext.Resolve<IPlayerHandService>();
                if (playerHandService != null)
                {
                    saveGameService.SetPlayerHandService(playerHandService);
                    Debug.Log("GameContextInitiator: IPlayerHandService successfully set in SaveGameService for held box save/load support.");
                }
                else
                {
                    Debug.LogWarning("GameContextInitiator: Could not resolve IPlayerHandService for SaveGameService.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameContextInitiator: Failed to set IPlacementService in SaveGameService: {e.Message}\nStackTrace: {e.StackTrace}");
        }
        
        Debug.Log("GameContextInitiator: Game context fully initialized");
    }

    /// <summary>
    /// Инициализирует UI Navigation систему для игровой сцены
    /// </summary>
    private void InitializeUINavigation()
    {
        try
        {
            // Получаем UI Navigation Service из родительского контекста
            var uiNavigationService = _gameContext.Resolve<IUINavigationService>();
            if (uiNavigationService != null)
            {
                // Устанавливаем контекст игры
                uiNavigationService.SetContext(Supermarket.Services.UI.UIContext.Game);
                
                // Регистрируем экраны
                if (_gameHUDScreen != null)
                {
                    // GameHUD регистрируется автоматически через OnEnable
                    Debug.Log("GameContextInitiator: GameHUDScreen will be auto-registered");
                }
                else
                {
                    Debug.LogError("GameContextInitiator: GameHUDScreen not assigned in inspector!");
                }
                
                if (_gameMenuHandler != null)
                {
                    // GameMenu регистрируется автоматически через OnEnable  
                    Debug.Log("GameContextInitiator: GameMenuHandler will be auto-registered");
                }
                else
                {
                    Debug.LogError("GameContextInitiator: GameMenuHandler not assigned in inspector!");
                }
                
                // Проверяем наличие UIInputHandler для обработки ESC
                if (_uiInputHandler != null)
                {
                    Debug.Log("GameContextInitiator: UIInputHandler found, ESC handling should work");
                }
                else
                {
                    Debug.LogError("GameContextInitiator: UIInputHandler not assigned in inspector! ESC key will not work.");
                }
                
                // Запускаем с HUD экрана
                uiNavigationService.PushScreen(Supermarket.Services.UI.UIScreenType.GameHUD, false);
                
                Debug.Log("GameContextInitiator: UI Navigation system initialized for Game context");
            }
            else
            {
                Debug.LogError("GameContextInitiator: Could not resolve IUINavigationService!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameContextInitiator: Failed to initialize UI Navigation: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// Загружает выбранный файл сохранения с задержкой
    /// Увеличенная задержка дает больше времени полкам, предустановленным объектам и другим объектам на регистрацию
    /// </summary>
    private System.Collections.IEnumerator LoadSaveFileDelayed(SaveGameService saveGameService)
    {
        Debug.Log("GameContextInitiator: Starting LoadSaveFileDelayed coroutine...");
        
        // Ждем 0.2 секунды для надежной инициализации всех объектов сцены, включая предустановленные объекты
        yield return new WaitForSeconds(0.2f);
        
        Debug.Log("GameContextInitiator: Delay completed, loading save file...");
        
        if (saveGameService != null)
        {
            saveGameService.LoadGameInGameScene();
        }
        else
        {
            Debug.LogError("GameContextInitiator: SaveGameService is null in LoadSaveFileDelayed!");
        }
    }

    void OnDestroy()
    {
        // Очищаем ссылки на сервисы Game контекста в SaveGameService перед уничтожением контекста
        try
        {
            var saveGameService = _gameContext?.Resolve<ISaveGameService>() as SaveGameService;
            if (saveGameService != null)
            {
                saveGameService.ClearPlacementService();
                saveGameService.ClearBoxManagerService();
                saveGameService.ClearPlayerDataProvider();
                saveGameService.ClearShelfManagerService();
                saveGameService.ClearCustomerManagerService();
                saveGameService.ClearDeliveryService();
                saveGameService.ClearLicenseService();
                saveGameService.ClearSupermarketNameService();
                saveGameService.ClearRetailPriceService();
                saveGameService.ClearPlayerHandService();
                Debug.Log("GameContextInitiator: Cleared PlacementService, BoxManagerService, PlayerDataProvider, ShelfManagerService, CustomerManagerService, DeliveryService, LicenseService, SupermarketNameService, RetailPriceService and PlayerHandService from SaveGameService on context destroy.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GameContextInitiator: Could not clear services from SaveGameService on destroy: {e.Message}");
        }
        
        if (_gameContext != null && !_gameContext.IsDestroyed)
        {
            _gameContext.Destroy();
            Debug.Log($"GameContextInitiator: {CONTEXT_NAME} context destroyed.");
        }
    }
} 