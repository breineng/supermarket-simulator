using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using System.Collections.Generic;
using System.Globalization;
using Supermarket.Interactables;
using Supermarket.Data;

[RequireComponent(typeof(UIDocument))]
public class CashDeskUIHandler : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _container;

    // UI Elements
    private Label _totalAmountLabel;
    private ScrollView _scannedItemsScrollView;
    private VisualElement _scannedItemsContainer;

    private CashDeskController _activeCashDesk;
    private List<ProductConfig> _scannedItemsList = new List<ProductConfig>();

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        InitializeUIElements();
        Hide();
    }

    private void OnEnable()
    {
        // Переинициализация UI элементов после активации GameObject
        // Это исправляет баг UIDocument, когда UI перестает реагировать после SetActive(false/true)
        if (_uiDocument != null)
        {
            InitializeUIElements();
        }
    }

    private void InitializeUIElements()
    {
        if (_uiDocument == null) return;
        
        _root = _uiDocument.rootVisualElement;
        
        _container = _root.Q<VisualElement>("CashDeskScreenContainer");
        _totalAmountLabel = _root.Q<Label>("TotalAmountLabel");
        _scannedItemsScrollView = _root.Q<ScrollView>("ScannedItemsScrollView");
        
        if(_scannedItemsScrollView != null)
            _scannedItemsContainer = _scannedItemsScrollView.Q<VisualElement>("unity-content-container");
    }

    private void OnDestroy()
    {
        UnsubscribeFromCashDeskEvents();
    }

    public void Show(CashDeskController controller)
    {
        if (_container != null)
            _container.style.display = DisplayStyle.Flex;

        _activeCashDesk = controller;
        SubscribeToCashDeskEvents();
        Clear();
    }

    public void Hide()
    {
        if (_container != null)
            _container.style.display = DisplayStyle.None;
        
        UnsubscribeFromCashDeskEvents();
        _activeCashDesk = null;
    }
    
    public void Clear()
    {
        _scannedItemsList.Clear();
        if(_scannedItemsContainer != null)
            _scannedItemsContainer.Clear();
        
        if(_totalAmountLabel != null)
            _totalAmountLabel.text = "$0.00";
    }

    private void SubscribeToCashDeskEvents()
    {
        if (_activeCashDesk == null) return;

        _activeCashDesk.OnOperationStarted += HandleOperationStarted;
        _activeCashDesk.OnItemScanned += HandleItemScanned;
        _activeCashDesk.OnTotalUpdated += HandleTotalUpdated;
        _activeCashDesk.OnTransactionFinalized += HandleTransactionFinalized;
    }

    private void UnsubscribeFromCashDeskEvents()
    {
        if (_activeCashDesk == null) return;

        _activeCashDesk.OnOperationStarted -= HandleOperationStarted;
        _activeCashDesk.OnItemScanned -= HandleItemScanned;
        _activeCashDesk.OnTotalUpdated -= HandleTotalUpdated;
        _activeCashDesk.OnTransactionFinalized -= HandleTransactionFinalized;
    }

    private void HandleOperationStarted()
    {
        Clear();
    }

    private void HandleItemScanned(ProductConfig scannedItem)
    {
        _scannedItemsList.Add(scannedItem);
        
        if (_scannedItemsContainer == null) return;
        
        var itemRow = new VisualElement();
        itemRow.AddToClassList("scanned-item-row");

        var nameLabel = new Label(scannedItem.ProductName);
        nameLabel.AddToClassList("item-name");

        var priceLabel = new Label(scannedItem.BaseSalePrice.ToString("C", CultureInfo.GetCultureInfo("en-US")));
        priceLabel.AddToClassList("item-price");
        
        itemRow.Add(nameLabel);
        itemRow.Add(priceLabel);
        
        _scannedItemsContainer.Add(itemRow);
        _scannedItemsScrollView.ScrollTo(_scannedItemsContainer[_scannedItemsContainer.childCount-1]);
    }

    private void HandleTotalUpdated(float newTotal)
    {
        if (_totalAmountLabel != null)
        {
            _totalAmountLabel.text = newTotal.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        }
    }
    
    private void HandleTransactionFinalized()
    {
        // Maybe show a "Thank you" message or something similar before clearing.
        // For now, we just clear for the next customer.
        Clear();
    }
} 