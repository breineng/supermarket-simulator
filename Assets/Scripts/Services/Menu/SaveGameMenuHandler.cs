using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using Supermarket.Services.PlayerData;
using Supermarket.Services.UI;
using Supermarket.UI;

namespace Supermarket.Services.Menu
{
    public enum SaveGameMenuMode
    {
        LoadMode,  // Только загрузка
        SaveMode,  // Только сохранение  
        FullMode   // И сохранение, и загрузка (по умолчанию)
    }

    public interface ISaveGameMenuHandler
    {
        void SetMenuMode(SaveGameMenuMode mode, bool isInGame);
    }

    public class SaveGameMenuHandler : BaseUIScreen, ISaveGameMenuHandler
    {
        [Inject] public ISaveGameService _saveGameService;
        [Inject] public ISaveGameSelectionService _saveGameSelectionService;
        [Inject] public ISceneManagementService _sceneManagementService;

        // UI элементы
        private ScrollView _savesList;
        private Label _saveInfoTitle;
        private VisualElement _saveScreenshot;
        private Label _saveInfoDate;
        private Label _saveInfoPlayTime;
        private Label _saveInfoMoney;
        private Label _saveInfoDay;
        private Button _loadButton;
        private Button _saveButton;
        private Button _deleteButton;
        private Button _backButton;
        private VisualElement _newSavePanel;
        private TextField _saveNameInput;
        private Button _confirmSaveButton;
        private Button _cancelSaveButton;
        private Button _closeModalButton;
        
        // Состояние
        private SaveGameInfo _selectedSave;
        private bool _isInGame = false;
        private SaveGameMenuMode _menuMode = SaveGameMenuMode.FullMode;
        
        public override bool CanGoBack => true;
        public override bool BlocksGameInput => true;
        public override bool PausesGame => true;
        
        protected override void Awake()
        {
            // Это один экран SaveGameMenu, который работает в разных режимах
            _screenType = UIScreenType.SaveGameMenu;
            _canGoBack = true;
            _blocksGameInput = true;
            _pausesGame = true;
            
            base.Awake();
            
            // Убеждаемся, что у UIDocument есть высокий sort order
            if (_uiDocument != null && _uiDocument.sortingOrder < 150)
            {
                _uiDocument.sortingOrder = 150;
            }
        }
        
        protected override void InitializeUI()
        {
            if (_rootElement == null) return;
            
            // Находим UI элементы
            _savesList = FindUIElement<ScrollView>("SavesList");
            _saveInfoTitle = FindUIElement<Label>("SaveInfoTitle");
            _saveScreenshot = FindUIElement<VisualElement>("SaveScreenshot");
            _saveInfoDate = FindUIElement<Label>("SaveInfoDate");
            _saveInfoPlayTime = FindUIElement<Label>("SaveInfoPlayTime");
            _saveInfoMoney = FindUIElement<Label>("SaveInfoMoney");
            _saveInfoDay = FindUIElement<Label>("SaveInfoDay");
            _loadButton = FindUIElement<Button>("LoadButton");
            _saveButton = FindUIElement<Button>("SaveButton");
            _deleteButton = FindUIElement<Button>("DeleteButton");
            _backButton = FindUIElement<Button>("BackButton");
            _newSavePanel = FindUIElement<VisualElement>("NewSavePanel");
            _saveNameInput = FindUIElement<TextField>("SaveNameInput");
            _confirmSaveButton = FindUIElement<Button>("ConfirmSaveButton");
            _cancelSaveButton = FindUIElement<Button>("CancelSaveButton");
            _closeModalButton = FindUIElement<Button>("CloseModalButton");
            
            // Подписываемся на события UI
            _loadButton?.RegisterCallback<ClickEvent>(OnLoadClicked);
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveClicked);
            _deleteButton?.RegisterCallback<ClickEvent>(OnDeleteClicked);
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            _confirmSaveButton?.RegisterCallback<ClickEvent>(OnConfirmSaveClicked);
            _cancelSaveButton?.RegisterCallback<ClickEvent>(OnCancelSaveClicked);
            _closeModalButton?.RegisterCallback<ClickEvent>(OnCancelSaveClicked);
            
            // Подписываемся на события сохранения/загрузки
            if (_saveGameService != null)
            {
                _saveGameService.OnSaveCompleted += OnSaveCompleted;
                _saveGameService.OnSaveError += OnSaveError;
                _saveGameService.OnLoadCompleted += OnLoadCompleted;
                _saveGameService.OnLoadError += OnLoadError;
            }
        }
        
        protected override void CleanupUI()
        {
            // Отписываемся от событий UI
            _loadButton?.UnregisterCallback<ClickEvent>(OnLoadClicked);
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveClicked);
            _deleteButton?.UnregisterCallback<ClickEvent>(OnDeleteClicked);
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _confirmSaveButton?.UnregisterCallback<ClickEvent>(OnConfirmSaveClicked);
            _cancelSaveButton?.UnregisterCallback<ClickEvent>(OnCancelSaveClicked);
            _closeModalButton?.UnregisterCallback<ClickEvent>(OnCancelSaveClicked);
            
            // Отписываемся от событий сохранения/загрузки
            if (_saveGameService != null)
            {
                _saveGameService.OnSaveCompleted -= OnSaveCompleted;
                _saveGameService.OnSaveError -= OnSaveError;
                _saveGameService.OnLoadCompleted -= OnLoadCompleted;
                _saveGameService.OnLoadError -= OnLoadError;
            }
        }
        
        protected override void OnScreenShown()
        {
            RefreshSavesList();
            UpdateUIForContext();
            UpdateSortOrder();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Убеждаемся что экран начально скрыт (должен показываться только через PushScreen)
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
                _rootElement.visible = false;
                Debug.Log("SaveGameMenuHandler: Screen initially hidden on enable.");
            }
        }
        
        /// <summary>
        /// Устанавливает режим меню и контекст
        /// </summary>
        public void SetMenuMode(SaveGameMenuMode mode, bool isInGame = true)
        {
            Debug.Log($"SaveGameMenuHandler: SetMenuMode called. Mode: {mode}, IsInGame: {isInGame}");
            
            _menuMode = mode;
            _isInGame = isInGame;
            
            // Не меняем ScreenType - это всегда SaveGameMenu, просто в разных режимах
            // _screenType всегда остается UIScreenType.SaveGameMenu
            
            UpdateUIForContext();
        }
        
        public void SetInGameContext(bool isInGame)
        {
            SetInGameContext(isInGame, SaveGameMenuMode.FullMode);
        }
        
        public void SetInGameContext(bool isInGame, SaveGameMenuMode mode)
        {
            SetMenuMode(mode, isInGame);
        }
        
        private void UpdateUIForContext()
        {
            // В зависимости от режима показываем/скрываем кнопки
            switch (_menuMode)
            {
                case SaveGameMenuMode.LoadMode:
                    SetVisible(_loadButton, true);
                    SetVisible(_saveButton, false);
                    break;
                    
                case SaveGameMenuMode.SaveMode:
                    SetVisible(_loadButton, false);
                    SetVisible(_saveButton, true);
                    break;
                    
                case SaveGameMenuMode.FullMode:
                default:
                    SetVisible(_loadButton, true);
                    SetVisible(_saveButton, _isInGame); // Сохранение только в игре
                    break;
            }
        }
        
        private void UpdateSortOrder()
        {
            // Убеждаемся, что окно сохранения отображается поверх других элементов
            if (_uiDocument != null)
            {
                if (_isInGame)
                {
                    // В игре должно быть выше игрового меню
                    _uiDocument.sortingOrder = 200;
                }
                else
                {
                    // В главном меню стандартный порядок
                    _uiDocument.sortingOrder = 10;
                }
            }
        }
        
        private void RefreshSavesList()
        {
            if (_savesList == null || _saveGameSelectionService == null) return;
            
            _savesList.Clear();
            
            var saves = _saveGameSelectionService.GetSavesList();
            if (saves != null)
            {
                foreach (var save in saves)
                {
                    var saveItem = CreateSaveItem(save);
                    _savesList.Add(saveItem);
                }
            }
            
            // Обновляем информацию о выбранном сохранении
            UpdateSaveInfo();
        }
        
        private VisualElement CreateSaveItem(SaveGameInfo save)
        {
            var saveItem = new VisualElement();
            saveItem.AddToClassList("save-item");
            
            var nameLabel = new Label(save.SaveName);
            nameLabel.AddToClassList("save-name");
            
            var dateLabel = new Label(save.SaveDate.ToString("dd.MM.yyyy HH:mm"));
            dateLabel.AddToClassList("save-date");
            
            saveItem.Add(nameLabel);
            saveItem.Add(dateLabel);
            
            saveItem.RegisterCallback<ClickEvent>(evt => OnSaveItemClicked(save, saveItem));
            
            return saveItem;
        }
        
        private void OnSaveItemClicked(SaveGameInfo save, VisualElement item)
        {
            // Убираем выделение с предыдущего элемента
            if (_selectedSave != null)
            {
                var oldItems = _savesList.Query<VisualElement>(className: "save-item").ToList();
                foreach (var oldItem in oldItems)
                {
                    oldItem.RemoveFromClassList("selected");
                }
            }
            
            _selectedSave = save;
            item.AddToClassList("selected");
            UpdateSaveInfo();
        }
        
        private void UpdateSaveInfo()
        {
            if (_selectedSave != null)
            {
                SetText(_saveInfoTitle, _selectedSave.SaveName);
                SetText(_saveInfoDate, _selectedSave.SaveDate.ToString("dd.MM.yyyy HH:mm"));
                SetText(_saveInfoPlayTime, FormatPlayTime(_selectedSave.PlayTime));
                SetText(_saveInfoMoney, $"${_selectedSave.Money:F2}");
                SetText(_saveInfoDay, $"День {_selectedSave.Day}");
                
                // Загружаем и отображаем скриншот
                LoadAndDisplayScreenshot(_selectedSave.ScreenshotPath);
                
                SetEnabled(_loadButton, true);
                SetEnabled(_deleteButton, true);
            }
            else
            {
                SetText(_saveInfoTitle, "Выберите сохранение");
                SetText(_saveInfoDate, "");
                SetText(_saveInfoPlayTime, "");
                SetText(_saveInfoMoney, "");
                SetText(_saveInfoDay, "");
                
                // Очищаем скриншот
                ClearScreenshot();
                
                SetEnabled(_loadButton, false);
                SetEnabled(_deleteButton, false);
            }
        }
        
        private string FormatPlayTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        
        private void OnLoadClicked(ClickEvent evt)
        {
            if (_selectedSave == null || _saveGameService == null) return;
            
            Debug.Log($"SaveGameMenuHandler: Loading save {_selectedSave.SaveName}. IsInGame: {_isInGame}");
            
            if (!_isInGame)
            {
                // Загрузка из главного меню - используем механизм отложенной загрузки
                Debug.Log("SaveGameMenuHandler: Loading from main menu - setting selected save file and switching to game scene");
                
                if (_saveGameSelectionService != null)
                {
                    _saveGameSelectionService.SetSelectedSaveFile(_selectedSave.SaveName);
                    Debug.Log($"SaveGameMenuHandler: Selected save file '{_selectedSave.SaveName}' for loading in game scene");
                }
                
                // Переходим в игровую сцену, там загрузится автоматически
                if (_sceneManagementService != null)
                {
                    _sceneManagementService.LoadScene("GameScene");
                }
                
                // Закрываем экран сохранений
                if (_uiNavigationService != null)
                {
                    _uiNavigationService.PopScreen();
                }
            }
            else
            {
                // Загрузка в игре - перезагружаем сцену для чистого состояния
                Debug.Log("SaveGameMenuHandler: Loading in game - reloading scene for clean state");
                
                if (_saveGameSelectionService != null)
                {
                    _saveGameSelectionService.SetSelectedSaveFile(_selectedSave.SaveName);
                    Debug.Log($"SaveGameMenuHandler: Selected save file '{_selectedSave.SaveName}' for loading after scene reload");
                }
                
                // Закрываем экран сохранений перед перезагрузкой сцены
                if (_uiNavigationService != null)
                {
                    Debug.Log("SaveGameMenuHandler: Closing save screen before scene reload");
                    _uiNavigationService.PopScreen();
                }
                
                // Перезагружаем игровую сцену
                if (_sceneManagementService != null)
                {
                    Debug.Log("SaveGameMenuHandler: Reloading GameScene to ensure clean state");
                    _sceneManagementService.LoadScene("GameScene");
                }
                else
                {
                    Debug.LogError("SaveGameMenuHandler: SceneManagementService is null!");
                }
            }
        }
        

        
        private void OnSaveClicked(ClickEvent evt)
        {
            Debug.Log("SaveGameMenuHandler: Save clicked - showing save panel");
            
            // Показываем панель для ввода имени сохранения
            SetVisible(_newSavePanel, true);
            _saveNameInput?.Focus();
        }
        
        private void OnDeleteClicked(ClickEvent evt)
        {
            if (_selectedSave == null || _saveGameSelectionService == null) return;
            
            Debug.Log($"SaveGameMenuHandler: Deleting save {_selectedSave.SaveName}");
            
            _saveGameSelectionService.DeleteSave(_selectedSave.SaveName);
            _selectedSave = null;
            RefreshSavesList();
        }
        
        private void OnBackClicked(ClickEvent evt)
        {
            Debug.Log("SaveGameMenuHandler: Back clicked");
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PopScreen();
            }
        }
        
        private void OnConfirmSaveClicked(ClickEvent evt)
        {
            var saveName = _saveNameInput?.value;
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogWarning("SaveGameMenuHandler: Save name is empty");
                return;
            }
            
            Debug.Log($"SaveGameMenuHandler: Saving game as {saveName}");
            _saveGameService?.SaveGame(saveName);
            
            SetVisible(_newSavePanel, false);
        }
        
        private void OnCancelSaveClicked(ClickEvent evt)
        {
            Debug.Log("SaveGameMenuHandler: Save cancelled");
            SetVisible(_newSavePanel, false);
        }
        
        private void OnSaveCompleted(string saveName)
        {
            Debug.Log($"SaveGameMenuHandler: Save completed: {saveName}");
            RefreshSavesList();
            
            // Закрываем меню после сохранения ТОЛЬКО если текущий экран - это именно SaveGameMenu
            // Это предотвращает двойной PopScreen() когда сохранение происходит из GameMenuHandler (быстрое сохранение)
            if (_uiNavigationService != null && _uiNavigationService.CurrentScreen == UIScreenType.SaveGameMenu)
            {
                Debug.Log("SaveGameMenuHandler: SaveGameMenu is current screen, closing after save completion");
                _uiNavigationService.PopScreen();
            }
            else
            {
                Debug.Log($"SaveGameMenuHandler: Current screen is {_uiNavigationService?.CurrentScreen}, not closing SaveGameMenu");
            }
        }
        
        private void OnSaveError(string error)
        {
            Debug.LogError($"SaveGameMenuHandler: Save error: {error}");
            // TODO: Показать уведомление об ошибке
        }
        
        private void OnLoadCompleted(string saveName)
        {
            Debug.Log($"SaveGameMenuHandler: Load completed: {saveName}");
            
            // Больше не нужно переключать сцену здесь - это делается в OnLoadClicked
            // Просто убеждаемся, что экран закрыт для случая загрузки в игре
            if (_isInGame && _uiNavigationService != null)
            {
                Debug.Log("SaveGameMenuHandler: Ensuring UI is closed after load completion in game");
                _uiNavigationService.PopAllScreens();
            }
        }
        
        private void OnLoadError(string error)
        {
            Debug.LogError($"SaveGameMenuHandler: Load error: {error}");
            // TODO: Показать уведомление об ошибке
        }
        
        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text;
        }
        
        private static void SetEnabled(Button button, bool enabled)
        {
            if (button != null) button.SetEnabled(enabled);
        }
        
        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null)
            {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Загружает и отображает скриншот сохранения
        /// </summary>
        /// <param name="screenshotPath">Относительный путь к файлу скриншота</param>
        private void LoadAndDisplayScreenshot(string screenshotPath)
        {
            if (_saveScreenshot == null)
            {
                Debug.LogWarning("SaveGameMenuHandler: Screenshot element not found");
                return;
            }
            
            // Очищаем предыдущий скриншот
            ClearScreenshot();
            
            if (string.IsNullOrEmpty(screenshotPath))
            {
                Debug.Log("SaveGameMenuHandler: No screenshot path provided");
                return;
            }
            
            try
            {
                // Получаем путь к файлу скриншота
                string savePath = Path.Combine(Application.persistentDataPath, "SavedGames");
                string fullScreenshotPath = Path.Combine(savePath, screenshotPath);
                
                if (!File.Exists(fullScreenshotPath))
                {
                    Debug.LogWarning($"SaveGameMenuHandler: Screenshot file not found: {fullScreenshotPath}");
                    return;
                }
                
                // Загружаем файл как байты
                byte[] fileData = File.ReadAllBytes(fullScreenshotPath);
                
                // Создаем текстуру из байтов
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData))
                {
                    // Создаем стиль фона с текстурой
                    var backgroundImage = new StyleBackground(texture);
                    _saveScreenshot.style.backgroundImage = backgroundImage;
                    _saveScreenshot.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    
                    Debug.Log($"SaveGameMenuHandler: Screenshot loaded successfully: {screenshotPath}");
                }
                else
                {
                    Debug.LogError($"SaveGameMenuHandler: Failed to load image data from: {fullScreenshotPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveGameMenuHandler: Error loading screenshot: {e.Message}");
            }
        }
        
        /// <summary>
        /// Очищает отображаемый скриншот
        /// </summary>
        private void ClearScreenshot()
        {
            if (_saveScreenshot != null)
            {
                _saveScreenshot.style.backgroundImage = StyleKeyword.Null;
            }
        }
    }
} 