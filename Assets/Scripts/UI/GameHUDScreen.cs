using UnityEngine;
using Supermarket.Services.UI;

namespace Supermarket.UI
{
    /// <summary>
    /// Game HUD screen - always visible during gameplay, doesn't pause game or block input
    /// </summary>
    public class GameHUDScreen : BaseUIScreen
    {
        public override bool CanGoBack => false; // HUD cannot be closed with back action
        public override bool BlocksGameInput => false; // HUD doesn't block game input
        public override bool PausesGame => false; // HUD doesn't pause the game
        
        protected override void Awake()
        {
            // Настраиваем тип экрана
            _screenType = UIScreenType.GameHUD;
            _canGoBack = false;
            _blocksGameInput = false;
            _pausesGame = false;
            
            base.Awake();
        }
        
        protected override void InitializeUI()
        {
            // GameHUD doesn't need to initialize specific UI elements here
            // The GameUIHandler component handles the actual HUD logic
            Debug.Log("GameHUDScreen: Initialized");
        }
        
        protected override void CleanupUI()
        {
            // Nothing to cleanup for basic HUD
        }
        
        public override bool HandleBackAction()
        {
            // HUD handles back action by opening the game menu
            Debug.Log("GameHUDScreen: Handling back action - opening game menu");
            
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PushScreen(UIScreenType.GameMenu);
                return true; // Handled
            }
            
            return false;
        }
        
        public override void Show()
        {
            base.Show();
            Debug.Log("GameHUDScreen: Game HUD is now visible");
        }
        
        public override void Hide()
        {
            base.Hide();
            Debug.Log("GameHUDScreen: Game HUD is now hidden");
        }
    }
} 