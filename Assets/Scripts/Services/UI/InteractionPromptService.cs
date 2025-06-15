using UnityEngine;
using UnityEngine.UIElements; // Используем UI Toolkit
using BehaviourInject;
// using Unity.VisualScripting; // Закомментировал, так как не используется и может вызывать ошибки если пакет не установлен

namespace Services.UI
{
    // ВРЕМЕННО УБИРАЕМ IInitializable для теста конструктора
    public class InteractionPromptService : IInteractionPromptService
    {
        private Label _promptLabel; // Компонент Label из UI Toolkit
        private UIDocument _gameHudDocument; // Ссылка на UIDocument для отложенной инициализации
        private bool _isInitialized = false; // Флаг инициализации
        private string _pendingMessage = null; // Сообщение, ожидающее отображения
        private bool _shouldShowPending = false; // Нужно ли показать ожидающее сообщение
        
        // Имя Label элемента в UXML файле
        private const string PromptLabelName = "InteractionPromptLabel"; 

        public InteractionPromptService(UIDocument gameHudDocument)
        {
            _gameHudDocument = gameHudDocument;
            TryInitialize();
        }

        private void TryInitialize()
        {
            Debug.Log("InteractionPromptService: TryInitialize() called.");
            
            if (_gameHudDocument == null)
            {
                Debug.LogError("InteractionPromptService: GameHudDocument is null!");
                return;
            }
            
            // Проверяем, готов ли UIDocument
            if (_gameHudDocument.rootVisualElement != null)
            {
                Debug.Log("InteractionPromptService: UIDocument and rootVisualElement found.");
                _promptLabel = _gameHudDocument.rootVisualElement.Q<Label>(PromptLabelName);
                if (_promptLabel != null)
                {
                    Debug.Log($"InteractionPromptService: Successfully found Label '{PromptLabelName}'.");
                    _promptLabel.style.display = DisplayStyle.None; // Скрыть по умолчанию
                    _isInitialized = true;
                    
                    // Если есть ожидающее сообщение, показываем его
                    if (_shouldShowPending && !string.IsNullOrEmpty(_pendingMessage))
                    {
                        Debug.Log($"InteractionPromptService: Showing pending message: '{_pendingMessage}'");
                        ShowPromptInternal(_pendingMessage);
                        _pendingMessage = null;
                        _shouldShowPending = false;
                    }
                }
                else
                {
                    Debug.LogError($"InteractionPromptService: Label '{PromptLabelName}' not found in UIDocument.");
                }
            }
            else
            {
                Debug.Log("InteractionPromptService: UIDocument rootVisualElement is null, will try again later.");
            }
        }

        private void ShowPromptInternal(string message)
        {
            if (_promptLabel != null)
            {
                _promptLabel.text = message;
                _promptLabel.style.display = DisplayStyle.Flex;
                Debug.Log($"InteractionPromptService: Label '{PromptLabelName}' text set to '{message}', display style set to Flex.");
            }
        }

        public void ShowPrompt(string message)
        {
            Debug.Log($"InteractionPromptService: ShowPrompt('{message}') called.");
            
            // Если не инициализированы, пытаемся инициализироваться
            if (!_isInitialized)
            {
                TryInitialize();
            }
            
            if (_isInitialized && _promptLabel != null)
            {
                ShowPromptInternal(message);
            }
            else
            {
                // Сохраняем сообщение для показа после инициализации
                Debug.Log($"InteractionPromptService: Not initialized yet, saving message '{message}' for later.");
                _pendingMessage = message;
                _shouldShowPending = true;
            }
        }

        public void HidePrompt()
        {
            Debug.Log("InteractionPromptService: HidePrompt() called.");
            
            // Если не инициализированы, пытаемся инициализироваться
            if (!_isInitialized)
            {
                TryInitialize();
            }
            
            if (_isInitialized && _promptLabel != null)
            {
                _promptLabel.style.display = DisplayStyle.None;
                Debug.Log($"InteractionPromptService: Label '{PromptLabelName}' display style set to None.");
            }
            else
            {
                // Очищаем ожидающее сообщение
                Debug.Log("InteractionPromptService: Not initialized yet, clearing pending message.");
                _pendingMessage = null;
                _shouldShowPending = false;
            }
        }

        public bool IsPromptVisible()
        {
            // Если не инициализированы, пытаемся инициализироваться
            if (!_isInitialized)
            {
                TryInitialize();
            }
            
            if (_isInitialized && _promptLabel != null)
            {
                bool visible = _promptLabel.style.display == DisplayStyle.Flex;
                return visible;
            }
            
            // Если не инициализированы, но есть ожидающее сообщение, считаем что будет видимо
            return _shouldShowPending;
        }
    }
} 