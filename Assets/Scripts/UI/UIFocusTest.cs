using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace Supermarket.UI
{
    /// <summary>
    /// Временный скрипт для диагностики проблем с UI Toolkit
    /// </summary>
    public class UIFocusTest : MonoBehaviour
    {
        private UIDocument _uiDocument;
        
        void Start()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("UIFocusTest: No UIDocument found on this GameObject");
                return;
            }
            
            // Логируем все кнопки в документе
            var buttons = _uiDocument.rootVisualElement.Query<Button>().ToList();
            Debug.Log($"UIFocusTest: Found {buttons.Count} buttons in UIDocument:");
            foreach (var button in buttons)
            {
                Debug.Log($"  - Button: '{button.name}', Enabled: {button.enabledInHierarchy}, Display: {button.style.display.value}");
                
                // Добавляем обработчик для отладки
                button.RegisterCallback<ClickEvent>(evt => 
                {
                    Debug.Log($"UIFocusTest: Button '{button.name}' was clicked!");
                });
                
                // Проверяем, есть ли уже обработчики
                button.RegisterCallback<MouseDownEvent>(evt =>
                {
                    Debug.Log($"UIFocusTest: MouseDown on button '{button.name}'");
                });
            }
            
            // Проверяем панель управления событиями
            var panel = _uiDocument.rootVisualElement.panel;
            if (panel != null)
            {
                Debug.Log($"UIFocusTest: Panel exists, ContextType: {panel.contextType}");
            }
            else
            {
                Debug.LogError("UIFocusTest: Panel is null!");
            }
        }
        
        void Update()
        {
            // При нажатии F10 выводим состояние UI
            if (Input.GetKeyDown(KeyCode.F10))
            {
                if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                {
                    var root = _uiDocument.rootVisualElement;
                    Debug.Log($"UIFocusTest: Root element - Enabled: {root.enabledInHierarchy}, Display: {root.style.display.value}");
                    
                    // Проверяем все активные кнопки
                    var buttons = root.Query<Button>().Where(b => b.enabledInHierarchy).ToList();
                    Debug.Log($"UIFocusTest: {buttons.Count} enabled buttons found");
                }
            }
        }
    }
} 