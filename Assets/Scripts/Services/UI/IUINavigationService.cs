using System;
using UnityEngine;

namespace Supermarket.Services.UI
{
    public enum UIScreenType
    {
        None,
        // Main Menu screens
        MainMenu,
        SaveGameMenu,
        SettingsMenu,
        
        // Game screens  
        GameHUD,
        GameMenu,
        PauseMenu,
        ComputerUI,
        
        // Modals/Overlays
        ConfirmationDialog,
        NotificationOverlay
    }

    public enum UIContext
    {
        MainMenu,
        Game
    }

    public interface IUIScreen
    {
        UIScreenType ScreenType { get; }
        bool CanGoBack { get; }
        bool BlocksGameInput { get; }
        bool PausesGame { get; }
        void Show();
        void Hide();
        bool HandleBackAction(); // Returns true if handled, false to continue with default back action
    }

    public interface IUINavigationService
    {
        // Current state
        UIScreenType CurrentScreen { get; }
        UIContext CurrentContext { get; }
        bool IsGamePaused { get; }
        bool HasScreensInStack { get; }
        
        // Screen management
        void PushScreen(UIScreenType screenType, bool hideCurrentScreen = true);
        void PopScreen();
        void PopToScreen(UIScreenType screenType);
        void PopAllScreens();
        void ReplaceScreen(UIScreenType screenType);
        
        // Context management
        void SetContext(UIContext context);
        
        // Pause management
        void PauseGame();
        void ResumeGame();
        void TogglePause();
        
        // Navigation actions
        void HandleBackAction(); // Called when ESC is pressed
        void HandleMenuAction(); // Called when Menu button is pressed
        
        // Screen registration
        void RegisterScreen(IUIScreen screen);
        void UnregisterScreen(IUIScreen screen);
        
        // Events
        event Action<UIScreenType> OnScreenChanged;
        event Action<bool> OnPauseStateChanged;
        event Action<UIContext> OnContextChanged;
    }
} 