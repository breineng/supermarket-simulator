using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using Supermarket.Services.PlayerData;
using Supermarket.Services.UI;
using Supermarket.Services.Menu;

namespace Supermarket.UI
{
    public class GameMenuHandler : BaseUIScreen
    {
        [Inject] public ISaveGameService _saveGameService;
        [Inject] public ISceneManagementService _sceneManagementService;
        [Inject] public INotificationService _notificationService;
        [Inject] public ISaveGameMenuHandler _saveGameMenuHandler;
        
        // Кнопки меню
        private Button _resumeButton;
        private Button _quickSaveButton;
        private Button _saveGameButton;
        private Button _loadGameButton;
        private Button _settingsButton;
        private Button _exitToMenuButton;
        
        // Главный overlay элемент из UXML
        private VisualElement _gameMenuOverlay;
        
        public override bool CanGoBack => true;
        public override bool BlocksGameInput => true;
        public override bool PausesGame => true;
        
        protected override void Awake()
        {
            // Настраиваем тип экрана
            _screenType = UIScreenType.GameMenu;
            _canGoBack = true;
            _blocksGameInput = true;
            _pausesGame = true;
            
            base.Awake();
            
            // Убеждаемся, что у UIDocument есть высокий sort order для отображения поверх других UI
            if (_uiDocument != null && _uiDocument.sortingOrder < 100)
            {
                _uiDocument.sortingOrder = 100;
            }
        }
        
        protected override void InitializeUI()
        {
            if (_rootElement == null) return;
            
            // Находим главный overlay элемент
            _gameMenuOverlay = FindUIElement<VisualElement>("GameMenuOverlay");
            if (_gameMenuOverlay == null)
            {
                Debug.LogError("GameMenuHandler: GameMenuOverlay element not found in UXML!");
                return;
            }
            
            // Находим элементы UI
            _resumeButton = FindUIElement<Button>("ResumeButton");
            _quickSaveButton = FindUIElement<Button>("QuickSaveButton");
            _saveGameButton = FindUIElement<Button>("SaveGameButton");
            _loadGameButton = FindUIElement<Button>("LoadGameButton");
            _settingsButton = FindUIElement<Button>("SettingsButton");
            _exitToMenuButton = FindUIElement<Button>("ExitToMenuButton");
            
            // Подписываемся на события кнопок
            _resumeButton?.RegisterCallback<ClickEvent>(OnResumeClicked);
            _quickSaveButton?.RegisterCallback<ClickEvent>(OnQuickSaveClicked);
            _saveGameButton?.RegisterCallback<ClickEvent>(OnSaveGameClicked);
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadGameClicked);
            _settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            _exitToMenuButton?.RegisterCallback<ClickEvent>(OnExitToMenuClicked);
            
            // Включаем автосохранение при инициализации UI
            if (_saveGameService != null)
            {
                _saveGameService.EnableAutoSave(300f); // Автосохранение каждые 5 минут
            }
        }
        
        public override void Show()
        {
            Debug.Log($"{GetType().Name}: Show() called. GameObject active: {gameObject.activeInHierarchy}");
            
            gameObject.SetActive(true);
            
            if (_rootElement != null)
            {
                Debug.Log($"{GetType().Name}: Setting rootElement display to Flex and visible to true");
                _rootElement.style.display = DisplayStyle.Flex;
                _rootElement.visible = true;
                
                // Важно! Показываем специфичный GameMenuOverlay элемент из UXML
                if (_gameMenuOverlay != null)
                {
                    Debug.Log($"{GetType().Name}: Setting GameMenuOverlay display to Flex");
                    _gameMenuOverlay.style.display = DisplayStyle.Flex;
                    _gameMenuOverlay.visible = true;
                    _gameMenuOverlay.Focus();
                    Debug.Log($"{GetType().Name}: GameMenuOverlay is now visible");
                }
                else
                {
                    Debug.LogError($"{GetType().Name}: _gameMenuOverlay is null! Cannot show game menu.");
                }
                
                // Set focus to enable input handling
                _rootElement.Focus();
                Debug.Log($"{GetType().Name}: Focus set on rootElement");
            }
            else
            {
                Debug.LogError($"{GetType().Name}: _rootElement is null! Cannot show UI.");
            }
            
            OnScreenShown();
            Debug.Log($"{GetType().Name}: Show() completed");
        }
        
        public override void Hide()
        {
            Debug.Log($"{GetType().Name}: Hiding screen");
            
            if (_gameMenuOverlay != null)
            {
                Debug.Log($"{GetType().Name}: Setting GameMenuOverlay display to None");
                _gameMenuOverlay.style.display = DisplayStyle.None;
                _gameMenuOverlay.visible = false;
            }
            
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
                _rootElement.visible = false;
            }
            
            OnScreenHidden();
            
            // Optionally deactivate GameObject to save performance
            // gameObject.SetActive(false);
        }
        
        protected override void CleanupUI()
        {
            // Отписываемся от событий
            _resumeButton?.UnregisterCallback<ClickEvent>(OnResumeClicked);
            _quickSaveButton?.UnregisterCallback<ClickEvent>(OnQuickSaveClicked);
            _saveGameButton?.UnregisterCallback<ClickEvent>(OnSaveGameClicked);
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadGameClicked);
            _settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
            _exitToMenuButton?.UnregisterCallback<ClickEvent>(OnExitToMenuClicked);
        }
        
        public override bool HandleBackAction()
        {
            // При нажатии ESC в игровом меню - закрываем его (resume game)
            Debug.Log("GameMenuHandler: Handling back action - resuming game");
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PopScreen(); // Вернемся к GameHUD
            }
            return true; // Обработано
        }
        
        private void OnResumeClicked(ClickEvent evt)
        {
            Debug.Log("GameMenuHandler: Resume clicked");
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PopScreen(); // Закрываем меню, возвращаемся к игре
            }
        }
        
        private void OnQuickSaveClicked(ClickEvent evt)
        {
            PerformQuickSave();
            // Закрываем меню после быстрого сохранения
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PopScreen();
            }
        }
        
        private void PerformQuickSave()
        {
            if (_saveGameService != null)
            {
                string saveName = $"QuickSave_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                if (_saveGameService.SaveGame(saveName))
                {
                    _notificationService?.ShowNotification($"Игра сохранена: {saveName}", NotificationType.Success);
                }
                else
                {
                    _notificationService?.ShowNotification("Ошибка при сохранении игры", NotificationType.Error);
                }
            }
        }
        
        private void OnSaveGameClicked(ClickEvent evt)
            {
            // Открываем меню сохранений в режиме сохранения
            Debug.Log("GameMenuHandler: Save Game clicked");
                
            if (_uiNavigationService != null)
                {
                // Настраиваем режим сохранения
                if (_saveGameMenuHandler != null)
                {
                    _saveGameMenuHandler.SetMenuMode(SaveGameMenuMode.SaveMode, true);
                }
                else
                {
                    Debug.LogWarning("GameMenuHandler: ISaveGameMenuHandler not injected!");
                }
                
                // Открываем экран
                _uiNavigationService.PushScreen(UIScreenType.SaveGameMenu);
            }
        }
        
        private void OnLoadGameClicked(ClickEvent evt)
        {
            // Открываем меню сохранений в режиме загрузки
            Debug.Log("GameMenuHandler: Load Game clicked");
            
            if (_uiNavigationService != null)
            {
                // Настраиваем режим загрузки
                if (_saveGameMenuHandler != null)
                {
                    _saveGameMenuHandler.SetMenuMode(SaveGameMenuMode.LoadMode, true);
                }
                else
                {
                    Debug.LogWarning("GameMenuHandler: ISaveGameMenuHandler not injected!");
                }
                
                // Открываем экран
                _uiNavigationService.PushScreen(UIScreenType.SaveGameMenu);
            }
        }
        
        private void OnSettingsClicked(ClickEvent evt)
        {
            // TODO: Открыть меню настроек
            Debug.Log("GameMenuHandler: Settings clicked - not implemented yet");
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PushScreen(UIScreenType.SettingsMenu);
            }
        }
        

        private void OnExitToMenuClicked(ClickEvent evt)
        {
            Debug.Log("GameMenuHandler: Exit to menu clicked");
            
            // Сохраняем перед выходом
            if (_saveGameService != null)
            {
                _saveGameService.SaveGame("AutoSave");
            }
            
            // Переходим в главное меню
            _sceneManagementService?.LoadScene("MenuScene");
        }
    }
} 