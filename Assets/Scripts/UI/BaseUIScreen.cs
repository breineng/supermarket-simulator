using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using Supermarket.Services.UI;

namespace Supermarket.UI
{
    /// <summary>
    /// Base class for UI screens that implements common functionality
    /// </summary>
    public abstract class BaseUIScreen : MonoBehaviour, IUIScreen
    {
        [Header("Screen Configuration")]
        [SerializeField] protected UIScreenType _screenType;
        [SerializeField] protected bool _canGoBack = true;
        [SerializeField] protected bool _blocksGameInput = true;
        [SerializeField] protected bool _pausesGame = true;
        
        [Inject] public IUINavigationService _uiNavigationService;
        
        protected UIDocument _uiDocument;
        protected VisualElement _rootElement;
        
        public UIScreenType ScreenType => _screenType;
        public virtual bool CanGoBack => _canGoBack;
        public virtual bool BlocksGameInput => _blocksGameInput;
        public virtual bool PausesGame => _pausesGame;
        
        protected virtual void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError($"{GetType().Name}: UIDocument component not found");
                enabled = false;
                return;
            }
        }
        
        protected virtual void OnEnable()
        {
            if (_uiDocument != null)
            {
                _rootElement = _uiDocument.rootVisualElement;
                InitializeUI();
            }
            
            // Register with navigation service
            if (_uiNavigationService != null)
            {
                _uiNavigationService.RegisterScreen(this);
            }
        }
        
        protected virtual void OnDisable()
        {
            CleanupUI();
            
            // Unregister from navigation service
            if (_uiNavigationService != null)
            {
                _uiNavigationService.UnregisterScreen(this);
            }
        }
        
        public virtual void Show()
        {
            Debug.Log($"{GetType().Name}: Show() called. GameObject active: {gameObject.activeInHierarchy}");
            
            gameObject.SetActive(true);
            
            if (_rootElement != null)
            {
                Debug.Log($"{GetType().Name}: Setting rootElement display to Flex and visible to true");
                _rootElement.style.display = DisplayStyle.Flex;
                _rootElement.visible = true;
                
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
        
        public virtual void Hide()
        {
            Debug.Log($"{GetType().Name}: Hiding screen");
            
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
                _rootElement.visible = false;
            }
            
            OnScreenHidden();
            
            // Optionally deactivate GameObject to save performance
            // gameObject.SetActive(false);
        }
        
        public virtual bool HandleBackAction()
        {
            Debug.Log($"{GetType().Name}: Handling back action (default behavior)");
            
            // Default behavior: go back if possible
            if (CanGoBack && _uiNavigationService != null)
            {
                _uiNavigationService.PopScreen();
                return true; // Handled
            }
            
            return false; // Not handled, let navigation service handle it
        }
        
        /// <summary>
        /// Initialize UI elements and event handlers
        /// </summary>
        protected abstract void InitializeUI();
        
        /// <summary>
        /// Cleanup UI event handlers
        /// </summary>
        protected abstract void CleanupUI();
        
        /// <summary>
        /// Called when screen is shown
        /// </summary>
        protected virtual void OnScreenShown() { }
        
        /// <summary>
        /// Called when screen is hidden
        /// </summary>
        protected virtual void OnScreenHidden() { }
        
        /// <summary>
        /// Helper method to find UI elements safely
        /// </summary>
        protected T FindUIElement<T>(string name) where T : VisualElement
        {
            if (_rootElement == null) return null;
            
            var element = _rootElement.Q<T>(name);
            if (element == null)
            {
                Debug.LogWarning($"{GetType().Name}: UI element '{name}' of type {typeof(T).Name} not found");
            }
            return element;
        }
    }
} 