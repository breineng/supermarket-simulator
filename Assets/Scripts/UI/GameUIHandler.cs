using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using System.Collections;
using Supermarket.Services.Game;

public class GameUIHandler : MonoBehaviour
{
    [Inject] public IPlayerHandService _playerHandService;
    [Inject] public IPlayerDataService _playerDataService;

    private UIDocument _uiDocument;
    
    private VisualElement _rootVisualElement;
    private Label _heldBoxInfoLabel;
    private Label _moneyAmountLabel;
    private VisualElement _notificationContainer;

    // Имена элементов в UXML
    private const string HeldBoxInfoLabelName = "HeldBoxInfoLabel";
    private const string MoneyAmountLabelName = "MoneyAmountLabel";
    private const string NotificationContainerName = "NotificationContainer";

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("GameUIHandler: UIDocument component not found on this GameObject.");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        _rootVisualElement = _uiDocument.rootVisualElement;
        if (_rootVisualElement == null)
        {
            Debug.LogError("GameUIHandler: RootVisualElement is null. Script will be disabled.");
            enabled = false;
            return;
        }

        // Запрашиваем основные элементы UI
        _heldBoxInfoLabel = _rootVisualElement.Q<Label>(HeldBoxInfoLabelName);
        _moneyAmountLabel = _rootVisualElement.Q<Label>(MoneyAmountLabelName);
        _notificationContainer = _rootVisualElement.Q<VisualElement>(NotificationContainerName);

        // Проверки на null для UI элементов
        if (_heldBoxInfoLabel == null) Debug.LogError($"GameUIHandler: Label '{HeldBoxInfoLabelName}' not found.");
        if (_moneyAmountLabel == null) Debug.LogError($"GameUIHandler: Label '{MoneyAmountLabelName}' not found.");
        if (_notificationContainer == null) Debug.LogError($"GameUIHandler: VisualElement '{NotificationContainerName}' not found.");

        // Инициализируем отображение денег
        UpdateMoneyDisplay();
        
        // Подписываемся на изменения денег
        if (_playerDataService != null)
        {
            _playerDataService.OnMoneyChanged += UpdateMoneyDisplay;
        }
    }

    void OnDisable()
    {
        // Отписываемся от событий
        if (_playerDataService != null)
        {
            _playerDataService.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    void Update()
    {
        UpdateHeldBoxInfoLabel();
    }

    private void UpdateHeldBoxInfoLabel()
    {
        if (_heldBoxInfoLabel == null || _playerHandService == null) return;

        if (_playerHandService.IsHoldingBox())
        {
            ProductConfig productInHand = _playerHandService.GetProductInHand();
            int quantity = _playerHandService.GetQuantityInHand();
            bool isOpen = _playerHandService.IsBoxOpen();

            string statusText = isOpen ? "открыта" : "закрыта";
            
            if (productInHand != null)
            {
                _heldBoxInfoLabel.text = $"Коробка {statusText}: {productInHand.ProductName} x{quantity}";
            }
            else
            {
                // Показываем информацию о пустой коробке
                _heldBoxInfoLabel.text = $"Коробка {statusText}: Пустая";
            }
            
            _heldBoxInfoLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            _heldBoxInfoLabel.style.display = DisplayStyle.None;
        }
    }

    private void UpdateMoneyDisplay()
    {
        if (_moneyAmountLabel == null || _playerDataService == null) return;

        float money = _playerDataService.CurrentPlayerData.Money;
        _moneyAmountLabel.text = $"Деньги: ${money:F0}";
    }

    public void ShowNotification(string message, float duration = 3f)
    {
        if (_notificationContainer == null) return;

        var notification = new Label(message);
        notification.AddToClassList("notification");
        notification.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        notification.style.color = Color.white;
        notification.style.paddingLeft = 10;
        notification.style.paddingRight = 10;
        notification.style.paddingTop = 5;
        notification.style.paddingBottom = 5;
        notification.style.marginBottom = 5;
        notification.style.borderTopLeftRadius = 5;
        notification.style.borderTopRightRadius = 5;
        notification.style.borderBottomLeftRadius = 5;
        notification.style.borderBottomRightRadius = 5;

        _notificationContainer.Add(notification);

        StartCoroutine(RemoveNotificationAfterDelay(notification, duration));
    }

    private IEnumerator RemoveNotificationAfterDelay(VisualElement notification, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notification.parent != null)
        {
            notification.parent.Remove(notification);
        }
    }
} 