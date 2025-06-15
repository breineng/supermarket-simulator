using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using Supermarket.Services.UI;

namespace Supermarket.UI
{
    /// <summary>
    /// Computer screen that integrates with UI Navigation system
    /// </summary>
    public class ComputerScreen : BaseUIScreen
    {
        [Header("Computer UI")]
        [SerializeField] private ComputerUIHandler _computerUIHandler;
        
        [Inject] public IInputModeService _inputModeService;
        
        public override bool CanGoBack => true;
        public override bool BlocksGameInput => true; 
        public override bool PausesGame => false; // Computer doesn't pause game
        
        protected override void Awake()
        {
            // Настраиваем тип экрана
            _screenType = UIScreenType.ComputerUI;
            _canGoBack = true;
            _blocksGameInput = true;
            _pausesGame = false;
            
            base.Awake();
            
            // Если ComputerUIHandler не назначен, ищем его на этом же объекте
            if (_computerUIHandler == null)
            {
                _computerUIHandler = GetComponent<ComputerUIHandler>();
            }
        }
        
        protected override void InitializeUI()
        {
            // ComputerUIHandler уже инициализирует свой UI
            // Здесь мы просто убеждаемся, что экран скрыт изначально
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
            }
        }
        
        public override void Show()
        {
            Debug.Log("ComputerScreen: Showing computer UI");
            
            // Активируем GameObject (этот объект должен содержать и ComputerScreen и ComputerUIHandler)
            gameObject.SetActive(true);
            
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.Flex;
                _rootElement.visible = true;
            }
            
            // Устанавливаем UI режим для курсора
            if (_inputModeService != null)
            {
                _inputModeService.SetInputMode(InputMode.UI);
            }
            
            OnScreenShown();
        }
        
        public override void Hide()
        {
            Debug.Log("ComputerScreen: Hiding computer UI");
            
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
                _rootElement.visible = false;
            }
            
            // Возвращаем игровой режим для курсора
            if (_inputModeService != null)
            {
                _inputModeService.SetInputMode(InputMode.Game);
            }
            
            OnScreenHidden();
            
            // Деактивируем GameObject после обработки (этот объект содержит и ComputerScreen и ComputerUIHandler)
            // gameObject.SetActive(false);
        }

        public override bool HandleBackAction()
        {
            // При нажатии ESC в интерфейсе компьютера - закрываем его
            Debug.Log("ComputerScreen: Handling back action - closing computer");
            if (_uiNavigationService != null)
            {
                _uiNavigationService.PopScreen(); // Вернемся к GameHUD
            }
            return true; // Обработано
        }
        
        protected override void CleanupUI()
        {
            // ComputerUIHandler очищает свой UI сам
        }
    }
} 