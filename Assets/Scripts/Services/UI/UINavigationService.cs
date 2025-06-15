using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BehaviourInject;

namespace Supermarket.Services.UI
{
    public class UINavigationService : IUINavigationService
    {
        private readonly Dictionary<UIScreenType, IUIScreen> _registeredScreens = new();
        private readonly Stack<UIScreenType> _screenStack = new();
        private IInputModeService _inputModeService;
        
        // Store the input mode before showing UI screens so we can restore it
        private InputMode _inputModeBeforeUI = InputMode.Game;
        
        public UIScreenType CurrentScreen { get; private set; } = UIScreenType.None;
        public UIContext CurrentContext { get; private set; } = UIContext.MainMenu;
        public bool IsGamePaused { get; private set; } = false;
        public bool HasScreensInStack => _screenStack.Count > 0;
        
        public event Action<UIScreenType> OnScreenChanged;
        public event Action<bool> OnPauseStateChanged;
        public event Action<UIContext> OnContextChanged;
        
        [Inject]
        public void Construct(IInputModeService inputModeService)
        {
            _inputModeService = inputModeService;
        }
        
        public void RegisterScreen(IUIScreen screen)
        {
            if (screen == null)
            {
                Debug.LogError("UINavigationService: Cannot register null screen");
                return;
            }
            
            Debug.Log($"UINavigationService: Registering screen {screen.ScreenType} ({screen.GetType().Name})");
            
            if (_registeredScreens.ContainsKey(screen.ScreenType))
            {
                Debug.LogWarning($"UINavigationService: Screen {screen.ScreenType} is already registered. Replacing.");
            }
            
            _registeredScreens[screen.ScreenType] = screen;
            Debug.Log($"UINavigationService: Screen {screen.ScreenType} registered successfully. Total screens: {_registeredScreens.Count}");
            Debug.Log($"UINavigationService: All registered screens: {string.Join(", ", _registeredScreens.Keys)}");
        }
        
        public void UnregisterScreen(IUIScreen screen)
        {
            if (screen == null) return;
            
            if (_registeredScreens.ContainsKey(screen.ScreenType))
            {
                _registeredScreens.Remove(screen.ScreenType);
                Debug.Log($"UINavigationService: Unregistered screen {screen.ScreenType}");
            }
        }
        
        public void SetContext(UIContext context)
        {
            if (CurrentContext != context)
            {
                Debug.Log($"UINavigationService: Changing context from {CurrentContext} to {context}");
                CurrentContext = context;
                OnContextChanged?.Invoke(context);
                
                // When switching to game context, ensure proper initial state
                if (context == UIContext.Game)
                {
                    // Start with GameHUD if no screens are active
                    if (CurrentScreen == UIScreenType.None)
                    {
                        PushScreen(UIScreenType.GameHUD, false);
                    }
                }
            }
        }
        
        public void PushScreen(UIScreenType screenType, bool hideCurrentScreen = true)
        {
            Debug.Log($"UINavigationService: PushScreen called. ScreenType: {screenType}, Current: {CurrentScreen}");
            Debug.Log($"UINavigationService: Registered screens: {string.Join(", ", _registeredScreens.Keys)}");
            
            if (!_registeredScreens.TryGetValue(screenType, out var screen))
            {
                Debug.LogError($"UINavigationService: Screen {screenType} is not registered! Available screens: {string.Join(", ", _registeredScreens.Keys)}");
                return;
            }
            
            Debug.Log($"UINavigationService: Found screen {screenType}: {screen?.GetType().Name}");
            
            // Hide current screen if requested
            if (hideCurrentScreen && CurrentScreen != UIScreenType.None)
            {
                if (_registeredScreens.TryGetValue(CurrentScreen, out var currentScreen))
                {
                    Debug.Log($"UINavigationService: Hiding current screen {CurrentScreen}");
                    currentScreen.Hide();
                }
            }
            
            // Add current screen to stack if it exists
            if (CurrentScreen != UIScreenType.None)
            {
                _screenStack.Push(CurrentScreen);
                Debug.Log($"UINavigationService: Pushed {CurrentScreen} to stack. Stack size: {_screenStack.Count}");
            }
            
            // Show new screen
            CurrentScreen = screenType;
            Debug.Log($"UINavigationService: Showing screen {screenType}");
            screen.Show();
            
            // Update input mode and pause state
            UpdateInputModeAndPause(screen);
            
            OnScreenChanged?.Invoke(screenType);
            Debug.Log($"UINavigationService: Screen change complete. Current: {CurrentScreen}");
        }
        
        public void PopScreen()
        {
            if (_screenStack.Count == 0)
            {
                Debug.LogWarning("UINavigationService: No screens to pop");
                return;
            }
            
            Debug.Log($"UINavigationService: Popping screen {CurrentScreen}");
            
            // Hide current screen
            if (CurrentScreen != UIScreenType.None && _registeredScreens.TryGetValue(CurrentScreen, out var currentScreen))
            {
                currentScreen.Hide();
            }
            
            // Get previous screen from stack
            var previousScreenType = _screenStack.Pop();
            CurrentScreen = previousScreenType;
            
            // Show previous screen
            if (_registeredScreens.TryGetValue(previousScreenType, out var previousScreen))
            {
                previousScreen.Show();
                UpdateInputModeAndPause(previousScreen);
            }
            
            OnScreenChanged?.Invoke(CurrentScreen);
        }
        
        public void PopToScreen(UIScreenType screenType)
        {
            Debug.Log($"UINavigationService: Popping to screen {screenType}");
            
            // Hide current screen
            if (CurrentScreen != UIScreenType.None && _registeredScreens.TryGetValue(CurrentScreen, out var currentScreen))
            {
                currentScreen.Hide();
            }
            
            // Pop until we find the target screen or stack is empty
            while (_screenStack.Count > 0)
            {
                var poppedScreen = _screenStack.Pop();
                if (poppedScreen == screenType)
                {
                    CurrentScreen = screenType;
                    if (_registeredScreens.TryGetValue(screenType, out var targetScreen))
                    {
                        targetScreen.Show();
                        UpdateInputModeAndPause(targetScreen);
                    }
                    OnScreenChanged?.Invoke(CurrentScreen);
                    return;
                }
            }
            
            Debug.LogWarning($"UINavigationService: Screen {screenType} not found in stack");
        }
        
        public void PopAllScreens()
        {
            Debug.Log("UINavigationService: Popping all screens");
            
            // Hide current screen
            if (CurrentScreen != UIScreenType.None && _registeredScreens.TryGetValue(CurrentScreen, out var currentScreen))
            {
                currentScreen.Hide();
            }
            
            _screenStack.Clear();
            
            // Resume game and set appropriate input mode
            ResumeGame();
            
            // В игровом контексте возвращаемся к GameHUD, в остальных контекстах очищаем все экраны
            if (CurrentContext == UIContext.Game)
            {
                // Показываем GameHUD
                CurrentScreen = UIScreenType.GameHUD;
                if (_registeredScreens.TryGetValue(UIScreenType.GameHUD, out var gameHudScreen))
                {
                    gameHudScreen.Show();
                    Debug.Log("UINavigationService: Returned to GameHUD after popping all screens");
                }
                else
                {
                    Debug.LogWarning("UINavigationService: GameHUD not registered, cannot return to it");
                    CurrentScreen = UIScreenType.None;
                }
                
                // Restore the input mode that was active before opening UI screens
                _inputModeService?.SetInputMode(_inputModeBeforeUI);
            }
            else
            {
                CurrentScreen = UIScreenType.None;
            }
            
            OnScreenChanged?.Invoke(CurrentScreen);
        }
        
        public void ReplaceScreen(UIScreenType screenType)
        {
            Debug.Log($"UINavigationService: Replacing screen {CurrentScreen} with {screenType}");
            
            if (!_registeredScreens.TryGetValue(screenType, out var screen))
            {
                Debug.LogError($"UINavigationService: Screen {screenType} is not registered");
                return;
            }
            
            // Hide current screen
            if (CurrentScreen != UIScreenType.None && _registeredScreens.TryGetValue(CurrentScreen, out var currentScreen))
            {
                currentScreen.Hide();
            }
            
            // Replace current screen (don't add to stack)
            CurrentScreen = screenType;
            screen.Show();
            
            UpdateInputModeAndPause(screen);
            OnScreenChanged?.Invoke(screenType);
        }
        
        public void PauseGame()
        {
            if (!IsGamePaused)
            {
                IsGamePaused = true;
                Time.timeScale = 0f;
                OnPauseStateChanged?.Invoke(true);
                Debug.Log("UINavigationService: Game paused");
            }
        }
        
        public void ResumeGame()
        {
            if (IsGamePaused)
            {
                IsGamePaused = false;
                Time.timeScale = 1f;
                OnPauseStateChanged?.Invoke(false);
                Debug.Log("UINavigationService: Game resumed");
            }
        }
        
        public void TogglePause()
        {
            if (IsGamePaused)
                ResumeGame();
            else
                PauseGame();
        }
        
        public void HandleBackAction()
        {
            Debug.Log($"UINavigationService: Handling back action. Current screen: {CurrentScreen}");
            
            // First, let the current screen handle the back action
            if (CurrentScreen != UIScreenType.None && _registeredScreens.TryGetValue(CurrentScreen, out var currentScreen))
            {
                if (currentScreen.HandleBackAction())
                {
                    Debug.Log("UINavigationService: Screen handled back action");
                    return; // Screen handled it
                }
            }
            
            // Default back behavior based on context and current screen
            switch (CurrentContext)
            {
                case UIContext.MainMenu:
                    HandleMainMenuBackAction();
                    break;
                    
                case UIContext.Game:
                    HandleGameBackAction();
                    break;
            }
        }
        
        public void HandleMenuAction()
        {
            Debug.Log($"UINavigationService: Handling menu action. Context: {CurrentContext}");
            
            switch (CurrentContext)
            {
                case UIContext.Game:
                    // Toggle game menu
                    if (CurrentScreen == UIScreenType.GameMenu)
                    {
                        PopScreen(); // Close menu
                    }
                    else
                    {
                        PushScreen(UIScreenType.GameMenu);
                    }
                    break;
            }
        }
        
        private void HandleMainMenuBackAction()
        {
            switch (CurrentScreen)
            {
                case UIScreenType.SaveGameMenu:
                case UIScreenType.SettingsMenu:
                    PopScreen(); // Go back to previous screen
                    break;
                    
                case UIScreenType.MainMenu:
                default:
                    // Main menu - maybe show quit confirmation or ignore
                    Debug.Log("UINavigationService: Back action on main menu - ignoring");
                    break;
            }
        }
        
        private void HandleGameBackAction()
        {
            switch (CurrentScreen)
            {
                case UIScreenType.GameHUD:
                    // Open game menu
                    PushScreen(UIScreenType.GameMenu);
                    break;
                    
                case UIScreenType.GameMenu:
                    // Close game menu
                    PopScreen();
                    break;
                    
                case UIScreenType.ComputerUI:
                    // Close computer UI
                    PopScreen();
                    break;
                    
                case UIScreenType.SaveGameMenu:
                case UIScreenType.SettingsMenu:
                    // Go back to previous screen
                    PopScreen();
                    break;
                    
                default:
                    // For other screens, try to go back
                    if (HasScreensInStack)
                    {
                        PopScreen();
                    }
                    break;
            }
        }
        
        private void UpdateInputModeAndPause(IUIScreen screen)
        {
            if (_inputModeService == null) return;
            
            // Set input mode based on screen
            if (screen.BlocksGameInput)
            {
                // Store current input mode before switching to UI
                if (_inputModeService.CurrentMode != InputMode.UI)
                {
                    _inputModeBeforeUI = _inputModeService.CurrentMode;
                }
                
                _inputModeService.SetInputMode(InputMode.UI);
            }
            else if (CurrentContext == UIContext.Game)
            {
                // Restore the previous input mode (could be Game or CashDeskOperation)
                _inputModeService.SetInputMode(_inputModeBeforeUI);
            }
            
            // Set pause state based on screen
            if (CurrentContext == UIContext.Game)
            {
                if (screen.PausesGame)
                {
                    PauseGame();
                }
                else
                {
                    ResumeGame();
                }
            }
        }
    }
} 