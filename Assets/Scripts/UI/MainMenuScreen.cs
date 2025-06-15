using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using Supermarket.Services.UI;
using Supermarket.Services.PlayerData;
using Supermarket.Services.Menu;

namespace Supermarket.UI
{
    /// <summary>
    /// Main menu screen
    /// </summary>
    public class MainMenuScreen : BaseUIScreen
    {
        [Inject] public ISceneManagementService _sceneManagementService;
        [Inject] public IPlayerDataService _playerDataService;
        [Inject] public ISaveGameSelectionService _saveGameSelectionService;
        [Inject] public ISaveGameMenuHandler _saveGameMenuHandler;

        private Button _continueButton;
        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _exitButton;

        public override bool CanGoBack => false; // Main menu is root, can't go back
        public override bool BlocksGameInput => true; // Menu blocks game input
        public override bool PausesGame => false; // Menu doesn't pause game (no game running)
        
        protected override void Awake()
        {
            // Настраиваем тип экрана
            _screenType = UIScreenType.MainMenu;
            _canGoBack = false;
            _blocksGameInput = true;
            _pausesGame = false;
            
            base.Awake();
        }
        
        protected override void InitializeUI()
        {
            if (_rootElement == null) return;
            
            // Находим кнопки
            _continueButton = FindUIElement<Button>("ContinueButton");
            _newGameButton = FindUIElement<Button>("NewGameButton");
            _loadGameButton = FindUIElement<Button>("LoadGameButton");
            _exitButton = FindUIElement<Button>("ExitButton");

            // Подписываемся на события кнопок
            _continueButton?.RegisterCallback<ClickEvent>(OnContinueClicked);
            _newGameButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadGameClicked);
            _exitButton?.RegisterCallback<ClickEvent>(OnExitClicked);
            
            // Обновляем видимость кнопки "Продолжить"
            UpdateContinueButtonVisibility();
        }
        
        protected override void CleanupUI()
        {
            // Отписываемся от событий
            _continueButton?.UnregisterCallback<ClickEvent>(OnContinueClicked);
            _newGameButton?.UnregisterCallback<ClickEvent>(OnNewGameClicked);
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadGameClicked);
            _exitButton?.UnregisterCallback<ClickEvent>(OnExitClicked);
        }
        
        public override bool HandleBackAction()
        {
            // Main menu doesn't handle back action - maybe show quit confirmation
            Debug.Log("MainMenuScreen: Back action on main menu - ignoring");
            return false; // Let navigation service handle it (which will ignore it)
        }

        protected override void OnScreenShown()
        {
            base.OnScreenShown();
            
            // Обновляем состояние кнопок при показе экрана
            UpdateContinueButtonVisibility();
            
            // Устанавливаем фокус на корневой элемент
            if (_rootElement != null)
            {
                _rootElement.Focus();
            }
        }
        
        private void UpdateContinueButtonVisibility()
        {
            if (_continueButton != null && _saveGameSelectionService != null)
            {
                bool hasAutoSave = _saveGameSelectionService.SaveExists("AutoSave");
                _continueButton.style.display = hasAutoSave ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        private void OnContinueClicked(ClickEvent evt)
        {
            Debug.Log("MainMenuScreen: Continue clicked");
            
            if (_saveGameSelectionService != null && _saveGameSelectionService.SaveExists("AutoSave"))
            {
                // Выбираем автосохранение для загрузки в игровой сцене
                _saveGameSelectionService.SetSelectedSaveFile("AutoSave");
                Debug.Log("MainMenuScreen: Selected AutoSave for loading in game scene.");
                
                // Переходим в игровую сцену
                _sceneManagementService?.LoadScene("GameScene");
            }
            else
            {
                Debug.LogWarning("MainMenuScreen: AutoSave file not found!");
            }
        }

        private void OnNewGameClicked(ClickEvent evt)
        {
            Debug.Log("MainMenuScreen: New Game clicked");
            
            if (_playerDataService == null)
            {
                Debug.LogError("MainMenuScreen: IPlayerDataService is null!");
                return;
            }
            
            _playerDataService.ResetData(); // Сбрасываем данные игрока

            if (_sceneManagementService == null)
            {
                Debug.LogError("MainMenuScreen: ISceneManagementService is null!");
                return;
            }
            
            _sceneManagementService.LoadScene("GameScene");
        }
        
        private void OnLoadGameClicked(ClickEvent evt)
        {
            Debug.Log("MainMenuScreen: Load Game clicked");
            if (_uiNavigationService != null)
            {
                // Настраиваем режим загрузки
                if (_saveGameMenuHandler != null)
                {
                    _saveGameMenuHandler.SetMenuMode(SaveGameMenuMode.LoadMode, false); // false = не в игре
                }
                else
                {
                    Debug.LogWarning("MainMenuScreen: ISaveGameMenuHandler not injected!");
                }
                
                // Открываем меню сохранений в режиме загрузки
                _uiNavigationService.PushScreen(UIScreenType.SaveGameMenu);
            }
        }

        private void OnExitClicked(ClickEvent evt)
        {
            Debug.Log("MainMenuScreen: Exit clicked");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
} 