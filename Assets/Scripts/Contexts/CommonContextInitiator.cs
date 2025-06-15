using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Menu;
using Supermarket.Services.UI;
using Supermarket.Services.PlayerData;

namespace Supermarket.Contexts
{
    public class CommonContextInitiator : MonoBehaviour
    {
        private const string CONTEXT_NAME = "Common";
        private Context _commonContext;
        
        [Header("Services")]
        [SerializeField] private SaveGameService _saveGameService;
        
        [Header("UI Handlers")]
        [SerializeField] private SaveGameMenuHandler _saveGameMenuHandler;
        [SerializeField] private NotificationService _notificationService; // Опционально, может быть уже в других контекстах
        
        void Awake()
        {
            // Проверяем, не существует ли уже контекст
            if (Context.Exists(CONTEXT_NAME))
            {
                Debug.Log("CommonContextInitiator: Common context already exists, destroying duplicate GameObject.");
                Destroy(gameObject);
                return;
            }
            
            // Создаем Common контекст, наследуемый от Application
            _commonContext = Context.Create(CONTEXT_NAME)
                                     .SetParentContext("Application");
            
            // Регистрируем SaveGameService
            if (_saveGameService == null)
            {
                Debug.LogError("CommonContextInitiator: SaveGameService not assigned in inspector!");
            }
            else
            {
                // В CommonContext регистрируем как ISaveGameSelectionService для работы с файлами в меню
                _commonContext.RegisterDependency<ISaveGameSelectionService>(_saveGameService);
                // Также регистрируем как ISaveGameService для совместимости
                _commonContext.RegisterDependency<ISaveGameService>(_saveGameService);
                Debug.Log("CommonContextInitiator: SaveGameService registered in context as ISaveGameSelectionService and ISaveGameService.");
            }
            
            // Регистрируем SaveGameMenuHandler
            if (_saveGameMenuHandler == null)
            {
                Debug.LogError("CommonContextInitiator: SaveGameMenuHandler not assigned in inspector!");
            }
            else
            {
                // Убеждаемся что GameObject активен для новой архитектуры BaseUIScreen
                if (!_saveGameMenuHandler.gameObject.activeInHierarchy)
                {
                    _saveGameMenuHandler.gameObject.SetActive(true);
                    Debug.Log("CommonContextInitiator: SaveGameMenuHandler GameObject activated for BaseUIScreen registration.");
                }
                
                _commonContext.RegisterDependency<SaveGameMenuHandler>(_saveGameMenuHandler);
                _commonContext.RegisterDependency<ISaveGameMenuHandler>(_saveGameMenuHandler);
                Debug.Log("CommonContextInitiator: SaveGameMenuHandler registered in context as SaveGameMenuHandler and ISaveGameMenuHandler.");
            }
            
            // Регистрируем NotificationService если он назначен (может быть общий для всех сцен)
            if (_notificationService != null)
            {
                _commonContext.RegisterDependency<INotificationService>(_notificationService);
                Debug.Log("CommonContextInitiator: NotificationService registered in context.");
            }
            
            // Этот объект не должен уничтожаться при загрузке новых сцен
            DontDestroyOnLoad(gameObject);
            
            Debug.Log($"CommonContextInitiator: {CONTEXT_NAME} context created and services registered.");
        }
        
        void OnDestroy()
        {
            if (_commonContext != null && !_commonContext.IsDestroyed)
            {
                _commonContext.Destroy();
                Debug.Log($"CommonContextInitiator: {CONTEXT_NAME} context destroyed.");
            }
        }
        
        // Метод для получения SaveGameMenuHandler из других контекстов
        public SaveGameMenuHandler GetSaveGameMenuHandler()
        {
            return _saveGameMenuHandler;
        }
    }
} 