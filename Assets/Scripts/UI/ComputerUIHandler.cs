using UnityEngine;
using UnityEngine.UIElements;
using BehaviourInject;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Для Sum()
using Supermarket.Services.Game; // Added for IDeliveryService, ISupermarketNameService and IRetailPriceService
using Supermarket.Services.UI; // Added for INotificationService
using Supermarket.Data; // Added for OrderSaveData

public class ComputerUIHandler : MonoBehaviour
{
    [Inject] public IProductCatalogService _productCatalogService; // Должен быть public для BInject
    [Inject] public IPlayerDataService _playerDataService;     // Должен быть public для BInject
    [Inject] public IInputModeService _inputModeService;       // Должен быть public для BInject
    [Inject] public IDeliveryService _deliveryService; // Added IDeliveryService
    [Inject] public IStatsService _statsService; // Added IStatsService
    [Inject] public INotificationService _notificationService; // Added INotificationService
    [Inject] public ILicenseService _licenseService; // Added ILicenseService
    [Inject] public ISupermarketNameService _supermarketNameService; // Added ISupermarketNameService
    [Inject] public IRetailPriceService _retailPriceService; // Added IRetailPriceService

    private UIDocument _uiDocument;
    private VisualElement _root;

    // Основные вкладки
    private Button _tabShop;
    private Button _tabManagePrices;
    private Button _tabLicenses;
    private Button _tabStatistics;
    private Button _tabStoreSettings;

    // Подвкладки магазина
    private Button _subTabGoods;
    private Button _subTabFurniture;
    private Button _subTabActiveOrders;

    // Секции
    private VisualElement _shopSection;
    private VisualElement _goodsSection;
    private VisualElement _furnitureSection;
    private VisualElement _activeOrdersSection;
    private VisualElement _managePricesSection;
    private VisualElement _licensesSection;
    private VisualElement _statisticsSection;
    private VisualElement _storeSettingsSection;

    // Элементы секции товаров
    private VisualElement _subcategoriesContainer;
    private VisualElement _productCardsContainer;
    private Label _cartItemCount;
    private Label _cartTotalAmount;
    private Button _submitOrderButton;

    // Элементы секции мебели
    private VisualElement _furnitureSubcategoriesContainer;
    private VisualElement _furnitureCardsContainer;
    private Label _furnitureCartItemCount;
    private Label _furnitureCartTotalAmount;
    private Button _submitFurnitureOrderButton;

    // Элементы секции управления ценами
    private ScrollView _productPriceListScrollView;
    private Dictionary<ProductConfig, FloatField> _salePriceFields = new Dictionary<ProductConfig, FloatField>();

    // Словари для хранения количества товаров в корзинах
    private Dictionary<ProductConfig, int> _goodsCart = new Dictionary<ProductConfig, int>();
    private Dictionary<ProductConfig, int> _furnitureCart = new Dictionary<ProductConfig, int>();
    
    // Текущие состояния
    private ProductSubcategory _currentSubcategory = ProductSubcategory.All;
    private FurnitureSubcategory _currentFurnitureSubcategory = FurnitureSubcategory.All;
    private List<Button> _subcategoryButtons = new List<Button>();
    private List<Button> _furnitureSubcategoryButtons = new List<Button>();
    private bool _isActiveOrdersSectionVisible = false;
    private float _lastTimerUpdate = 0f;
    private const float TIMER_UPDATE_INTERVAL = 1f; // Обновляем таймеры раз в секунду
    private int _lastOrdersCount = -1; // Кэш для оптимизации обновления

    // Элементы секции статистики
    private Label _totalRevenueLabel;
    private Label _totalExpensesLabel;
    private Label _profitLabel;
    private Label _totalCustomersLabel;
    private Label _customersTodayLabel;
    private Label _averageTransactionLabel;
    private Label _totalItemsSoldLabel;
    private Label _bestSellingProductLabel;

    // Элементы секции лицензий
    private ScrollView _licensesScrollView;
    private VisualElement _licensesContainer;
    private VisualElement _licenseCardTemplate;

    // Элементы секции активных заказов
    private ScrollView _activeOrdersScrollView;
    private VisualElement _activeOrdersContainer;
    private VisualElement _orderCardTemplate;
    
    // Элементы секции настроек магазина
    private TextField _storeNameField;
    private Button _changeStoreNameButton;
    
    // Контейнер для уведомлений
    private VisualElement _computerNotificationContainer;
    
    // Элемент отображения денег игрока
    private Label _playerMoneyLabel;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("ComputerUIHandler: UIDocument component not found on this GameObject.", this);
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("ComputerUIHandler: RootVisualElement is null.", this);
            enabled = false;
            return;
        }

        // --- Поиск элементов UI ---
        // Основные вкладки
        _tabShop = _root.Q<Button>("TabShop");
        _tabManagePrices = _root.Q<Button>("TabManagePrices");
        _tabLicenses = _root.Q<Button>("TabLicenses");
        _tabStatistics = _root.Q<Button>("TabStatistics");
        _tabStoreSettings = _root.Q<Button>("TabStoreSettings");

        // Подвкладки магазина
        _subTabGoods = _root.Q<Button>("SubTabGoods");
        _subTabFurniture = _root.Q<Button>("SubTabFurniture");
        _subTabActiveOrders = _root.Q<Button>("SubTabActiveOrders");

        // Секции
        _shopSection = _root.Q<VisualElement>("ShopSection");
        _goodsSection = _root.Q<VisualElement>("GoodsSection");
        _furnitureSection = _root.Q<VisualElement>("FurnitureSection");
        _managePricesSection = _root.Q<VisualElement>("ManagePricesSection");
        _licensesSection = _root.Q<VisualElement>("LicensesSection");
        _activeOrdersSection = _root.Q<VisualElement>("ActiveOrdersSection");
        _statisticsSection = _root.Q<VisualElement>("StatisticsSection");
        _storeSettingsSection = _root.Q<VisualElement>("StoreSettingsSection");

        // Элементы секции товаров
        _subcategoriesContainer = _root.Q<VisualElement>("SubcategoriesContainer");
        _productCardsContainer = _root.Q<VisualElement>("ProductCardsContainer");
        _cartItemCount = _root.Q<Label>("CartItemCount");
        _cartTotalAmount = _root.Q<Label>("CartTotalAmount");
        _submitOrderButton = _root.Q<Button>("SubmitOrderButton");

        // Элементы секции мебели
        _furnitureSubcategoriesContainer = _root.Q<VisualElement>("FurnitureSubcategoriesContainer");
        _furnitureCardsContainer = _root.Q<VisualElement>("FurnitureCardsContainer");
        _furnitureCartItemCount = _root.Q<Label>("FurnitureCartItemCount");
        _furnitureCartTotalAmount = _root.Q<Label>("FurnitureCartTotalAmount");
        _submitFurnitureOrderButton = _root.Q<Button>("SubmitFurnitureOrderButton");

        // Элементы управления ценами
        _productPriceListScrollView = _root.Q<ScrollView>("ProductPriceList");
        
        // Элементы статистики
        _totalRevenueLabel = _root.Q<Label>("TotalRevenueLabel");
        _totalExpensesLabel = _root.Q<Label>("TotalExpensesLabel");
        _profitLabel = _root.Q<Label>("ProfitLabel");
        _totalCustomersLabel = _root.Q<Label>("TotalCustomersLabel");
        _customersTodayLabel = _root.Q<Label>("CustomersTodayLabel");
        _averageTransactionLabel = _root.Q<Label>("AverageTransactionLabel");
        _totalItemsSoldLabel = _root.Q<Label>("TotalItemsSoldLabel");
        _bestSellingProductLabel = _root.Q<Label>("BestSellingProductLabel");

        // Элементы лицензий
        _licensesScrollView = _root.Q<ScrollView>("LicensesScrollView");
        _licensesContainer = _root.Q<VisualElement>("LicensesContainer");
        _licenseCardTemplate = _root.Q<VisualElement>("LicenseCardTemplate");

        // Элементы секции активных заказов
        _activeOrdersScrollView = _root.Q<ScrollView>("ActiveOrdersScrollView");
        _activeOrdersContainer = _root.Q<VisualElement>("ActiveOrdersContainer");
        _orderCardTemplate = _root.Q<VisualElement>("OrderCardTemplate");
        
        // Элементы секции настроек магазина
        _storeNameField = _root.Q<TextField>("StoreNameField");
        _changeStoreNameButton = _root.Q<Button>("ChangeStoreNameButton");
        
        // Инициализируем контейнер уведомлений
        _computerNotificationContainer = _root.Q<VisualElement>("ComputerNotificationContainer");
        
        // Инициализируем элемент отображения денег игрока
        _playerMoneyLabel = _root.Q<Label>("PlayerMoneyLabel");

        // --- Подписка на события ---
        _tabShop?.RegisterCallback<ClickEvent>(evt => ShowShopSection());
        _tabManagePrices?.RegisterCallback<ClickEvent>(evt => ShowManagePricesSection());
        _tabLicenses?.RegisterCallback<ClickEvent>(evt => ShowLicensesSection());
        _tabStatistics?.RegisterCallback<ClickEvent>(evt => ShowStatisticsSection());
        _tabStoreSettings?.RegisterCallback<ClickEvent>(evt => ShowStoreSettingsSection());
        
        _subTabGoods?.RegisterCallback<ClickEvent>(evt => ShowGoodsSubSection());
        _subTabFurniture?.RegisterCallback<ClickEvent>(evt => ShowFurnitureSubSection());
        _subTabActiveOrders?.RegisterCallback<ClickEvent>(evt => ShowActiveOrdersSection());
        
        _submitOrderButton?.RegisterCallback<ClickEvent>(OnSubmitGoodsOrderClicked);
        _submitFurnitureOrderButton?.RegisterCallback<ClickEvent>(OnSubmitFurnitureOrderClicked);
        _changeStoreNameButton?.RegisterCallback<ClickEvent>(OnChangeStoreNameClicked);

        // --- Инициализация UI ---
        if (_productCatalogService == null)
        {
            Debug.LogError("ComputerUIHandler: IProductCatalogService is not injected!", this);
        }
        else
        {
            InitializeSubcategories();
            InitializeFurnitureSubcategories();
            PopulateProductPriceList();
        }
        
        // Подписываемся на изменения денег игрока
        if (_playerDataService != null)
        {
            _playerDataService.OnMoneyChanged += UpdatePlayerMoneyDisplay;
            UpdatePlayerMoneyDisplay(); // Начальное значение
        }
        else
        {
            Debug.LogError("ComputerUIHandler: IPlayerDataService is not injected!");
        }
        
        ShowShopSection(); // По умолчанию показываем секцию магазина
        ShowGoodsSubSection(); // И подсекцию товаров
    }

    void OnDisable()
    {
        _tabShop?.UnregisterCallback<ClickEvent>(evt => ShowShopSection());
        _tabManagePrices?.UnregisterCallback<ClickEvent>(evt => ShowManagePricesSection());
        _tabLicenses?.UnregisterCallback<ClickEvent>(evt => ShowLicensesSection());
        _tabStatistics?.UnregisterCallback<ClickEvent>(evt => ShowStatisticsSection());
        _tabStoreSettings?.UnregisterCallback<ClickEvent>(evt => ShowStoreSettingsSection());
        
        _subTabGoods?.UnregisterCallback<ClickEvent>(evt => ShowGoodsSubSection());
        _subTabFurniture?.UnregisterCallback<ClickEvent>(evt => ShowFurnitureSubSection());
        _subTabActiveOrders?.UnregisterCallback<ClickEvent>(evt => ShowActiveOrdersSection());
        
        _submitOrderButton?.UnregisterCallback<ClickEvent>(OnSubmitGoodsOrderClicked);
        _submitFurnitureOrderButton?.UnregisterCallback<ClickEvent>(OnSubmitFurnitureOrderClicked);
        _changeStoreNameButton?.UnregisterCallback<ClickEvent>(OnChangeStoreNameClicked);

        // Отписываемся от событий изменения денег
        if (_playerDataService != null)
        {
            _playerDataService.OnMoneyChanged -= UpdatePlayerMoneyDisplay;
        }
        
        _salePriceFields.Clear();
        _subcategoryButtons.Clear();
        _furnitureSubcategoryButtons.Clear();
    }

    private void ShowShopSection()
    {
        ShowMainSection(_shopSection);
        UpdateActiveMainTab(_tabShop);
        ShowGoodsSubSection(); // По умолчанию показываем товары
    }

    private void ShowGoodsSubSection()
    {
        ShowShopSubSection(_goodsSection);
        UpdateActiveSubTab(_subTabGoods);
        PopulateGoodsCards();
        UpdateGoodsCartDisplay();
        _isActiveOrdersSectionVisible = false;
    }

    private void ShowFurnitureSubSection()
    {
        ShowShopSubSection(_furnitureSection);
        UpdateActiveSubTab(_subTabFurniture);
        InitializeFurnitureSubcategories();
        PopulateFurnitureCards();
        UpdateFurnitureCartDisplay();
        _isActiveOrdersSectionVisible = false;
    }

    private void ShowManagePricesSection()
    {
        ShowMainSection(_managePricesSection);
        UpdateActiveMainTab(_tabManagePrices);
        PopulateProductPriceList();
        _isActiveOrdersSectionVisible = false;
    }

    private void ShowLicensesSection()
    {
        ShowMainSection(_licensesSection);
        UpdateActiveMainTab(_tabLicenses);
        PopulateLicensesList();
        _isActiveOrdersSectionVisible = false;
    }

    private void ShowActiveOrdersSection()
    {
        ShowShopSubSection(_activeOrdersSection);
        UpdateActiveSubTab(_subTabActiveOrders);
        PopulateActiveOrdersList();
        _isActiveOrdersSectionVisible = true;
        _lastOrdersCount = -1; // Сбрасываем кэш при открытии секции
    }

    private void ShowStatisticsSection()
    {
        ShowMainSection(_statisticsSection);
        UpdateActiveMainTab(_tabStatistics);
        UpdateStatistics();
        _isActiveOrdersSectionVisible = false;
    }
    
    private void ShowStoreSettingsSection()
    {
        ShowMainSection(_storeSettingsSection);
        UpdateActiveMainTab(_tabStoreSettings);
        InitializeStoreSettings();
        _isActiveOrdersSectionVisible = false;
    }

    private void ShowMainSection(VisualElement sectionToShow)
    {
        // Скрываем все основные секции
        if (_shopSection != null) _shopSection.style.display = DisplayStyle.None;
        if (_managePricesSection != null) _managePricesSection.style.display = DisplayStyle.None;
        if (_licensesSection != null) _licensesSection.style.display = DisplayStyle.None;
        if (_activeOrdersSection != null) _activeOrdersSection.style.display = DisplayStyle.None;
        if (_statisticsSection != null) _statisticsSection.style.display = DisplayStyle.None;
        if (_storeSettingsSection != null) _storeSettingsSection.style.display = DisplayStyle.None;

        // Показываем нужную секцию
        if (sectionToShow != null) sectionToShow.style.display = DisplayStyle.Flex;
    }

    private void ShowShopSubSection(VisualElement subSectionToShow)
    {
        // Скрываем все подсекции магазина
        if (_goodsSection != null) _goodsSection.style.display = DisplayStyle.None;
        if (_furnitureSection != null) _furnitureSection.style.display = DisplayStyle.None;
        if (_activeOrdersSection != null) _activeOrdersSection.style.display = DisplayStyle.None;

        // Показываем нужную подсекцию
        if (subSectionToShow != null) subSectionToShow.style.display = DisplayStyle.Flex;
    }

    private void UpdateActiveMainTab(Button activeTab)
    {
        // Убираем активность со всех основных вкладок
        _tabShop?.RemoveFromClassList("active-tab");
        _tabManagePrices?.RemoveFromClassList("active-tab");
        _tabLicenses?.RemoveFromClassList("active-tab");
        _tabStatistics?.RemoveFromClassList("active-tab");
        _tabStoreSettings?.RemoveFromClassList("active-tab");

        // Добавляем активность к нужной вкладке
        activeTab?.AddToClassList("active-tab");
    }

    private void UpdateActiveSubTab(Button activeSubTab)
    {
        // Убираем активность со всех подвкладок
        _subTabGoods?.RemoveFromClassList("active-sub-tab");
        _subTabFurniture?.RemoveFromClassList("active-sub-tab");
        _subTabActiveOrders?.RemoveFromClassList("active-sub-tab");

        // Добавляем активность к нужной подвкладке
        activeSubTab?.AddToClassList("active-sub-tab");
    }

    private void InitializeSubcategories()
    {
        if (_subcategoriesContainer == null) return;

        _subcategoriesContainer.Clear();
        _subcategoryButtons.Clear();

        var subcategories = new Dictionary<ProductSubcategory, string>
        {
            { ProductSubcategory.All, "Все товары" },
            { ProductSubcategory.Drinks, "Напитки" },
            { ProductSubcategory.Snacks, "Снеки" },
            { ProductSubcategory.Dairy, "Молочка" },
            { ProductSubcategory.Sweets, "Сладости" },
            { ProductSubcategory.Nuts, "Орехи" }
        };

        foreach (var subcategory in subcategories)
        {
            var button = new Button(() => OnSubcategoryClicked(subcategory.Key));
            button.text = subcategory.Value;
            button.style.marginBottom = 5;
            button.style.height = 35;
            button.style.backgroundColor = new StyleColor(new Color(35f/255f, 50f/255f, 65f/255f));
            button.style.color = Color.white;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 3;
            button.style.borderLeftWidth = button.style.borderRightWidth = button.style.borderTopWidth = button.style.borderBottomWidth = 0;
            
            if (subcategory.Key == _currentSubcategory)
            {
                button.style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
            }

            _subcategoriesContainer.Add(button);
            _subcategoryButtons.Add(button);
        }
    }

    private void OnSubcategoryClicked(ProductSubcategory subcategory)
    {
        _currentSubcategory = subcategory;
        UpdateSubcategoryButtons();
        PopulateGoodsCards();
    }

    private void UpdateSubcategoryButtons()
    {
        var subcategories = new List<ProductSubcategory> 
        { 
            ProductSubcategory.All, ProductSubcategory.Drinks, ProductSubcategory.Snacks,
            ProductSubcategory.Dairy, ProductSubcategory.Sweets, ProductSubcategory.Nuts
        };

        for (int i = 0; i < _subcategoryButtons.Count && i < subcategories.Count; i++)
        {
            if (subcategories[i] == _currentSubcategory)
            {
                _subcategoryButtons[i].style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
            }
            else
            {
                _subcategoryButtons[i].style.backgroundColor = new StyleColor(new Color(35f/255f, 50f/255f, 65f/255f));
            }
        }
    }

    private void PopulateGoodsCards()
    {
        if (_productCardsContainer == null || _productCatalogService == null) return;

        _productCardsContainer.Clear();
        
        var availableProducts = _productCatalogService.GetOrderableProductConfigsBySubcategory(_currentSubcategory);

        foreach (var product in availableProducts)
        {
            var productCard = CreateGoodsCard(product);
            _productCardsContainer.Add(productCard);
        }
        UpdateGoodsCartDisplay();
    }

    private void PopulateFurnitureCards()
    {
        if (_furnitureCardsContainer == null || _productCatalogService == null) return;

        _furnitureCardsContainer.Clear();
        
        var availableFurniture = _productCatalogService.GetOrderableFurnitureConfigsBySubcategory(_currentFurnitureSubcategory);

        foreach (var furniture in availableFurniture)
        {
            var furnitureCard = CreateFurnitureCard(furniture);
            _furnitureCardsContainer.Add(furnitureCard);
        }
        UpdateFurnitureCartDisplay();
    }

    private VisualElement CreateGoodsCard(ProductConfig product)
    {
        return CreateProductCardBase(product, _goodsCart, UpdateGoodsCartDisplay);
    }

    private VisualElement CreateFurnitureCard(ProductConfig product)
    {
        return CreateProductCardBase(product, _furnitureCart, UpdateFurnitureCartDisplay);
    }

    private VisualElement CreateProductCardBase(ProductConfig product, Dictionary<ProductConfig, int> cart, System.Action updateCartAction)
    {
        var card = new VisualElement();
        card.style.width = 280;
        card.style.height = 480; // Увеличил высоту для квадратного контейнера изображения
        card.style.marginLeft = card.style.marginRight = card.style.marginTop = card.style.marginBottom = 10;
        card.style.backgroundColor = new StyleColor(new Color(25f/255f, 40f/255f, 55f/255f));
        card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
        card.style.borderLeftWidth = card.style.borderRightWidth = card.style.borderTopWidth = card.style.borderBottomWidth = 1;
        card.style.borderLeftColor = card.style.borderRightColor = card.style.borderTopColor = card.style.borderBottomColor = new StyleColor(new Color(40f/255f, 55f/255f, 70f/255f));
        card.style.overflow = Overflow.Hidden;

        // Изображение товара
        var imageContainer = new VisualElement();
        imageContainer.style.height = 280; // Квадратный контейнер (равен ширине карточки)
        imageContainer.style.backgroundColor = new StyleColor(Color.white); // Белый фон
        imageContainer.style.alignItems = Align.Center;
        imageContainer.style.justifyContent = Justify.Center;
        
        // Пытаемся загрузить спрайт товара
        if (product.Icon != null)
        {
            var productImage = new Image();
            productImage.sprite = product.Icon;
            productImage.style.width = Length.Percent(90);
            productImage.style.height = Length.Percent(90);
            productImage.style.maxWidth = Length.Percent(100);
            productImage.style.maxHeight = Length.Percent(100);
            productImage.scaleMode = ScaleMode.ScaleToFit;
            imageContainer.Add(productImage);
        }
        else
        {
            // Фоллбек на эмодзи если спрайт не найден
            var imagePlaceholder = new Label("📦");
            imagePlaceholder.style.fontSize = 48;
            imagePlaceholder.style.color = new StyleColor(new Color(100f/255f, 115f/255f, 130f/255f));
            imageContainer.Add(imagePlaceholder);
        }

        // Информация о товаре
        var infoContainer = new VisualElement();
        infoContainer.style.paddingLeft = infoContainer.style.paddingRight = infoContainer.style.paddingTop = infoContainer.style.paddingBottom = 15;
        infoContainer.style.flexGrow = 1;
        infoContainer.style.justifyContent = Justify.SpaceBetween;

        // Заголовок
        var titleContainer = new VisualElement();
        var nameLabel = new Label(product.ProductName);
        nameLabel.style.fontSize = 16;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginBottom = 5;
        nameLabel.style.whiteSpace = WhiteSpace.Normal;

        var categoryLabel = new Label(GetCategoryDisplayName(product));
        categoryLabel.style.fontSize = 11;
        categoryLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
        categoryLabel.style.marginBottom = 10;

        titleContainer.Add(nameLabel);
        titleContainer.Add(categoryLabel);

        // Информация о коробке (только для товаров с ItemsPerBox > 1)
        if (product.ItemsPerBox > 1)
        {
            var boxInfoLabel = new Label($"Коробка: {product.ItemsPerBox} шт.");
            boxInfoLabel.style.fontSize = 11;
            boxInfoLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            boxInfoLabel.style.marginBottom = 5;
            titleContainer.Add(boxInfoLabel);
        }

        // Цена с учетом количества в коробке
        float totalPricePerBox = product.PurchasePrice * product.ItemsPerBox;
        var priceLabel = new Label(product.ItemsPerBox > 1 ? 
            $"${totalPricePerBox:F2} (${product.PurchasePrice:F2}/шт.)" : 
            $"${product.PurchasePrice:F2}");
        priceLabel.style.fontSize = 20;
        priceLabel.style.color = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
        priceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        priceLabel.style.marginBottom = 15;

        // Контролы количества - показываем только если товар уже в корзине
        var quantityContainer = new VisualElement();
        quantityContainer.style.flexDirection = FlexDirection.Row;
        quantityContainer.style.alignItems = Align.Center;
        quantityContainer.style.justifyContent = Justify.Center;

        // Проверяем, есть ли товар в корзине
        int currentQuantity = cart.ContainsKey(product) ? cart[product] : 0;

        if (currentQuantity == 0)
        {
            // Показываем только кнопку "Добавить"
            var addButton = new Button(() => {
                cart[product] = 1;
                updateCartAction();
                // Перерисовываем карточку
                ReplaceCard(product, cart, updateCartAction);
            });
            addButton.text = product.ItemsPerBox > 1 ? "Добавить коробку" : "Добавить";
            addButton.style.height = 35;
            addButton.style.paddingLeft = addButton.style.paddingRight = 20;
            addButton.style.backgroundColor = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
            addButton.style.color = Color.white;
            addButton.style.borderTopLeftRadius = addButton.style.borderTopRightRadius = addButton.style.borderBottomLeftRadius = addButton.style.borderBottomRightRadius = 5;
            addButton.style.borderLeftWidth = addButton.style.borderRightWidth = addButton.style.borderTopWidth = addButton.style.borderBottomWidth = 0;
            
            quantityContainer.Add(addButton);
        }
        else
        {
            // Показываем полные контролы
            var minusButton = new Button(() => {
                int newQuantity = Mathf.Max(0, cart[product] - 1);
                cart[product] = newQuantity;
                if (newQuantity == 0) cart.Remove(product);
                updateCartAction();
                ReplaceCard(product, cart, updateCartAction);
            });
            minusButton.text = "−";
            minusButton.style.width = 30;
            minusButton.style.height = 30;
            minusButton.style.fontSize = 18;
            minusButton.style.backgroundColor = new StyleColor(new Color(233f/255f, 69f/255f, 96f/255f));
            minusButton.style.color = Color.white;
            minusButton.style.borderTopLeftRadius = minusButton.style.borderTopRightRadius = minusButton.style.borderBottomLeftRadius = minusButton.style.borderBottomRightRadius = 15;
            minusButton.style.borderLeftWidth = minusButton.style.borderRightWidth = minusButton.style.borderTopWidth = minusButton.style.borderBottomWidth = 0;

            var quantityLabel = new Label(currentQuantity.ToString());
            quantityLabel.style.width = 60;
            quantityLabel.style.fontSize = 16;
            quantityLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            quantityLabel.style.color = Color.white;
            quantityLabel.style.backgroundColor = new StyleColor(new Color(35f/255f, 50f/255f, 65f/255f));
            quantityLabel.style.borderTopLeftRadius = quantityLabel.style.borderTopRightRadius = quantityLabel.style.borderBottomLeftRadius = quantityLabel.style.borderBottomRightRadius = 3;
            quantityLabel.style.marginLeft = quantityLabel.style.marginRight = 10;

            var plusButton = new Button(() => {
                cart[product] = cart[product] + 1;
                updateCartAction();
                ReplaceCard(product, cart, updateCartAction);
            });
            plusButton.text = "+";
            plusButton.style.width = 30;
            plusButton.style.height = 30;
            plusButton.style.fontSize = 18;
            plusButton.style.backgroundColor = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
            plusButton.style.color = Color.white;
            plusButton.style.borderTopLeftRadius = plusButton.style.borderTopRightRadius = plusButton.style.borderBottomLeftRadius = plusButton.style.borderBottomRightRadius = 15;
            plusButton.style.borderLeftWidth = plusButton.style.borderRightWidth = plusButton.style.borderTopWidth = plusButton.style.borderBottomWidth = 0;

            quantityContainer.Add(minusButton);
            quantityContainer.Add(quantityLabel);
            quantityContainer.Add(plusButton);
        }

        infoContainer.Add(titleContainer);
        infoContainer.Add(priceLabel);
        infoContainer.Add(quantityContainer);

        card.Add(imageContainer);
        card.Add(infoContainer);

        return card;
    }

    private void ReplaceCard(ProductConfig product, Dictionary<ProductConfig, int> cart, System.Action updateCartAction)
    {
        // Находим и заменяем карточку товара
        if (cart == _goodsCart)
        {
            PopulateGoodsCards();
        }
        else if (cart == _furnitureCart)
        {
            PopulateFurnitureCards();
        }
    }

    private void UpdateGoodsCartDisplay()
    {
        UpdateCartDisplay(_goodsCart, _cartItemCount, _cartTotalAmount, _submitOrderButton);
    }

    private void UpdateFurnitureCartDisplay()
    {
        UpdateCartDisplay(_furnitureCart, _furnitureCartItemCount, _furnitureCartTotalAmount, _submitFurnitureOrderButton);
    }

    private void UpdateCartDisplay(Dictionary<ProductConfig, int> cart, Label itemCountLabel, Label totalAmountLabel, Button submitButton)
    {
        if (itemCountLabel == null || totalAmountLabel == null) return;

        int totalBoxes = 0;
        int totalItems = 0;
        float totalAmount = 0;
        
        foreach (var entry in cart)
        {
            int boxQuantity = entry.Value;
            if (boxQuantity > 0)
            {
                totalBoxes += boxQuantity;
                totalItems += boxQuantity * entry.Key.ItemsPerBox; // Учитываем количество товаров в коробке
                totalAmount += boxQuantity * entry.Key.PurchasePrice * entry.Key.ItemsPerBox; // Стоимость коробки = цена за единицу * количество в коробке
            }
        }

        // Показываем количество коробок и общее количество товаров
        if (totalBoxes == 1)
        {
            itemCountLabel.text = totalItems == 1 ? "1 коробка (1 товар)" : $"1 коробка ({totalItems} товаров)";
        }
        else
        {
            itemCountLabel.text = $"{totalBoxes} коробок ({totalItems} товаров)";
        }
        
        totalAmountLabel.text = $"${totalAmount:F0}";
        
        // Активация/деактивация кнопки заказа
        if (submitButton != null)
        {
            bool hasItems = totalItems > 0;
            submitButton.SetEnabled(hasItems);
            
            // Изменяем стиль для визуального отображения состояния
            if (hasItems)
            {
                submitButton.style.backgroundColor = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
                submitButton.style.opacity = 1f;
            }
            else
            {
                submitButton.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                submitButton.style.opacity = 0.5f;
            }
        }
    }

    private void OnSubmitGoodsOrderClicked(ClickEvent evt)
    {
        SubmitOrder(_goodsCart, "товаров");
    }

    private void OnSubmitFurnitureOrderClicked(ClickEvent evt)
    {
        SubmitOrder(_furnitureCart, "мебели");
    }

    private void SubmitOrder(Dictionary<ProductConfig, int> cart, string itemType)
    {
        if (_playerDataService == null)
        {
            Debug.LogError("ComputerUIHandler: PlayerDataService is not injected. Cannot process order.");
            return;
        }

        float currentOrderTotal = 0;
        Dictionary<ProductConfig, int> productsToOrder = new Dictionary<ProductConfig, int>();

        foreach (var entry in cart)
        {
            ProductConfig config = entry.Key;
            int boxQuantity = entry.Value;

            if (boxQuantity > 0)
            {
                // Рассчитываем стоимость коробок (цена за единицу * количество в коробке * количество коробок)
                currentOrderTotal += config.PurchasePrice * config.ItemsPerBox * boxQuantity;
                // В заказ добавляем общее количество товаров (коробки * товары в коробке)
                productsToOrder.Add(config, boxQuantity * config.ItemsPerBox);
            }
        }

        if (productsToOrder.Count == 0)
        {
            Debug.Log($"ComputerUIHandler: No {itemType} selected for order.");
            string noItemsMessage = $"Не выбрано {itemType} для заказа";
            
            // Показываем уведомление в общей системе уведомлений
            _notificationService?.ShowNotification(noItemsMessage, NotificationType.Warning);
            
            // Показываем локальное уведомление в интерфейсе компьютера
            ShowComputerNotification($"⚠ {noItemsMessage}", NotificationType.Warning, 3f);
            return;
        }

        if (_playerDataService.CurrentPlayerData.Money >= currentOrderTotal)
        {
            _playerDataService.AdjustMoney(-currentOrderTotal);
            _playerDataService.SaveData();

            Debug.Log($"ComputerUIHandler: Order submitted! Total: {currentOrderTotal:F2}. Remaining money: {_playerDataService.GetMoney():F2}");
            
            if (_statsService != null)
            {
                _statsService.RecordPurchase(currentOrderTotal);
            }
            
            if (_deliveryService != null)
            {
                // Используем новый метод с отложенной доставкой
                string orderId = _deliveryService.PlaceOrder(productsToOrder);
                
                if (!string.IsNullOrEmpty(orderId))
                {
                    Debug.Log($"ComputerUIHandler: Order {orderId} placed successfully");
                }
                else
                {
                    Debug.LogError("ComputerUIHandler: Failed to place order");
                }
            }
            else
            {
                Debug.LogError("ComputerUIHandler: DeliveryService is not injected. Cannot deliver boxes.");
            }

            foreach(var item in productsToOrder)
            {
                Debug.Log($" - Ordered: {item.Key.ProductName} x {item.Value}");
            }

            // Очистка корзины и обновление UI
            cart.Clear();
            if (cart == _goodsCart)
            {
                PopulateGoodsCards();
                UpdateGoodsCartDisplay();
            }
            else if (cart == _furnitureCart)
            {
                PopulateFurnitureCards();
                UpdateFurnitureCartDisplay();
            }
            
            string successMessage = $"Заказ {itemType} оформлен на сумму ${currentOrderTotal:F0}";
            
            // Показываем уведомление в общей системе уведомлений
            _notificationService?.ShowNotification(successMessage, NotificationType.Success);
            
            // Показываем локальное уведомление в интерфейсе компьютера
            ShowComputerNotification($"✓ {successMessage}", NotificationType.Success, 4f);
        }
        else
        {
            Debug.LogWarning($"ComputerUIHandler: Not enough money to place order. Required: {currentOrderTotal:F2}, Available: {_playerDataService.CurrentPlayerData.Money:F2}");
            
            float shortage = currentOrderTotal - _playerDataService.CurrentPlayerData.Money;
            string insufficientFundsMessage = $"Недостаточно денег! Не хватает: ${shortage:F0}";
            
            // Показываем уведомление в общей системе уведомлений
            _notificationService?.ShowNotification(insufficientFundsMessage, NotificationType.Error);
            
            // Показываем локальное уведомление в интерфейсе компьютера
            ShowComputerNotification($"✕ {insufficientFundsMessage}", NotificationType.Error, 5f);
        }
    }

    private string GetCategoryDisplayName(ProductConfig product)
    {
        if (product.ObjectCategory == PlaceableObjectType.Goods)
            return "Товар";
        else if (product.ObjectCategory == PlaceableObjectType.Shelf)
            return "Полка";
        else if (product.ObjectCategory == PlaceableObjectType.CashDesk)
            return "Касса";
        else
            return "Другое";
    }

    private void PopulateProductPriceList()
    {
        if (_productPriceListScrollView == null || _productCatalogService == null) return;

        _productPriceListScrollView.Clear();
        _salePriceFields.Clear();
        
        // Получаем только товары, которые могут быть размещены на полках
        var allProducts = _productCatalogService.GetAllProductConfigs()
            .Where(product => product.CanBePlacedOnShelf)
            .ToList();

        // Если нет товаров для отображения, показываем сообщение
        if (allProducts.Count == 0)
        {
            var noProductsLabel = new Label("Нет товаров, которые можно разместить на полках");
            noProductsLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            noProductsLabel.style.fontSize = 14;
            noProductsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noProductsLabel.style.marginTop = 50;
            _productPriceListScrollView.Add(noProductsLabel);
            return;
        }

        foreach (var productConfig in allProducts)
        {
            var itemRow = new VisualElement();
            itemRow.AddToClassList("product-price-item");
            itemRow.style.flexDirection = FlexDirection.Row;
            itemRow.style.justifyContent = Justify.SpaceBetween;
            itemRow.style.alignItems = Align.Center;
            itemRow.style.paddingLeft = itemRow.style.paddingRight = 15;
            itemRow.style.paddingTop = itemRow.style.paddingBottom = 12;
            itemRow.style.marginBottom = 8;
            itemRow.style.backgroundColor = new StyleColor(new Color(25f/255f, 40f/255f, 55f/255f));
            itemRow.style.borderTopLeftRadius = itemRow.style.borderTopRightRadius = itemRow.style.borderBottomLeftRadius = itemRow.style.borderBottomRightRadius = 5;
            itemRow.style.borderLeftWidth = itemRow.style.borderRightWidth = itemRow.style.borderTopWidth = itemRow.style.borderBottomWidth = 1;
            itemRow.style.borderLeftColor = itemRow.style.borderRightColor = itemRow.style.borderTopColor = itemRow.style.borderBottomColor = new StyleColor(new Color(40f/255f, 55f/255f, 70f/255f));

            // Название товара
            var nameLabel = new Label(productConfig.ProductName);
            nameLabel.style.width = new Length(25, LengthUnit.Percent);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 14;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.whiteSpace = WhiteSpace.Normal;
            
            // Цена закупки
            var purchasePriceLabel = new Label($"Закупка: ${productConfig.PurchasePrice:F0}");
            purchasePriceLabel.style.width = new Length(18, LengthUnit.Percent);
            purchasePriceLabel.style.fontSize = 12;
            purchasePriceLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));

            // Контейнер для цены продажи
            var salePriceContainer = new VisualElement();
            salePriceContainer.style.flexDirection = FlexDirection.Row;
            salePriceContainer.style.alignItems = Align.Center;
            salePriceContainer.style.width = new Length(35, LengthUnit.Percent);
            salePriceContainer.style.marginRight = 10;

            var salePriceLabel = new Label("Продажа: $");
            salePriceLabel.style.marginRight = 5;
            salePriceLabel.style.fontSize = 12;
            salePriceLabel.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            salePriceLabel.style.flexShrink = 0; // Не сжимать лейбл
            
            var salePriceField = new FloatField();
            // Получаем актуальную розничную цену (может быть кастомной)
            float currentPrice = _retailPriceService?.GetRetailPrice(productConfig.ProductID) ?? productConfig.BaseSalePrice;
            salePriceField.value = currentPrice;
            salePriceField.style.width = 80;
            salePriceField.style.maxWidth = 80;
            salePriceField.style.backgroundColor = new StyleColor(new Color(35f/255f, 50f/255f, 65f/255f));
            salePriceField.style.borderLeftColor = salePriceField.style.borderRightColor = salePriceField.style.borderTopColor = salePriceField.style.borderBottomColor = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
            salePriceField.style.borderLeftWidth = salePriceField.style.borderRightWidth = salePriceField.style.borderTopWidth = salePriceField.style.borderBottomWidth = 1;
            salePriceField.style.borderTopLeftRadius = salePriceField.style.borderTopRightRadius = salePriceField.style.borderBottomLeftRadius = salePriceField.style.borderBottomRightRadius = 3;
            _salePriceFields[productConfig] = salePriceField;

            salePriceContainer.Add(salePriceLabel);
            salePriceContainer.Add(salePriceField);

            // Кнопка применить
            var applyButton = new Button(() => OnApplyPriceClicked(productConfig, salePriceField));
            applyButton.text = "Применить";
            applyButton.style.width = new Length(20, LengthUnit.Percent);
            applyButton.style.height = 30;
            applyButton.style.fontSize = 12;
            applyButton.style.backgroundColor = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
            applyButton.style.color = Color.white;
            applyButton.style.borderTopLeftRadius = applyButton.style.borderTopRightRadius = applyButton.style.borderBottomLeftRadius = applyButton.style.borderBottomRightRadius = 4;
            applyButton.style.borderLeftWidth = applyButton.style.borderRightWidth = applyButton.style.borderTopWidth = applyButton.style.borderBottomWidth = 0;
            applyButton.style.flexShrink = 0; // Не сжимать кнопку
            applyButton.AddToClassList("apply-price-button");

            itemRow.Add(nameLabel);
            itemRow.Add(purchasePriceLabel);
            itemRow.Add(salePriceContainer);
            itemRow.Add(applyButton);
            _productPriceListScrollView.Add(itemRow);
        }
    }

    private void OnApplyPriceClicked(ProductConfig productConfig, FloatField priceField)
    {
        if (productConfig == null || priceField == null || _retailPriceService == null) return;

        float newPrice = Mathf.Max(0, priceField.value); // Не позволяем цене быть отрицательной
        priceField.SetValueWithoutNotify(newPrice); // Обновляем поле, если значение было исправлено

        // Получаем текущую розничную цену
        float currentPrice = _retailPriceService.GetRetailPrice(productConfig.ProductID);
        
        if (Mathf.Approximately(currentPrice, newPrice)) 
        {
            _notificationService?.ShowNotification("Цена не изменилась", NotificationType.Warning);
            ShowComputerNotification("⚠ Цена не изменилась", NotificationType.Warning);
            return; // Цена не изменилась
        }

        // Устанавливаем новую цену через RetailPriceService
        _retailPriceService.SetRetailPrice(productConfig.ProductID, newPrice);
        
        // Показываем уведомление об успешном изменении цены
        _notificationService?.ShowNotification(
            $"Цена '{productConfig.ProductName}' изменена с ${currentPrice:F2} на ${newPrice:F2}", 
            NotificationType.Success
        );
        
        Debug.Log($"ComputerUIHandler: Price for '{productConfig.ProductName}' changed from ${currentPrice:F2} to ${newPrice:F2}");
        
        // Показываем локальное уведомление в интерфейсе компьютера
        ShowComputerNotification(
            $"✓ Цена '{productConfig.ProductName}' изменена с ${currentPrice:F2} на ${newPrice:F2}",
            NotificationType.Success
        );
    }

    private void UpdateStatistics()
    {
        // Используем реальные данные из IStatsService
        if (_statsService != null)
        {
            // Финансовая статистика
            if (_totalRevenueLabel != null) _totalRevenueLabel.text = $"${_statsService.GetTotalRevenue():F2}";
            if (_totalExpensesLabel != null) _totalExpensesLabel.text = $"${_statsService.GetTotalExpenses():F2}";
            if (_profitLabel != null) _profitLabel.text = $"${_statsService.GetProfit():F2}";
            
            // Статистика покупателей  
            if (_totalCustomersLabel != null) _totalCustomersLabel.text = _statsService.GetTotalCustomersServed().ToString();
            if (_customersTodayLabel != null) _customersTodayLabel.text = _statsService.GetCustomersToday().ToString();
            if (_averageTransactionLabel != null) _averageTransactionLabel.text = $"${_statsService.GetAverageTransactionValue():F2}";
            
            // Статистика товаров
            if (_totalItemsSoldLabel != null) _totalItemsSoldLabel.text = _statsService.GetTotalItemsSold().ToString();
            if (_bestSellingProductLabel != null) _bestSellingProductLabel.text = _statsService.GetBestSellingProduct();
        }
        else
        {
            Debug.LogError("ComputerUIHandler: StatsService is not injected. Cannot display statistics.");
        }
    }

    public void CloseComputerUI()
    {
        gameObject.SetActive(false);
        if (_inputModeService != null)
        {
            _inputModeService.SetInputMode(InputMode.Game);
        }
        else
        {
            Debug.LogError("ComputerUIHandler: InputModeService is not injected. Cannot restore game input mode.");
        }
    }

    private void PopulateLicensesList()
    {
        if (_licensesContainer == null || _licenseCardTemplate == null || _licenseService == null)
        {
            Debug.LogError("ComputerUIHandler: Missing dependencies for licenses UI");
            return;
        }

        // Очищаем контейнер
        _licensesContainer.Clear();

        // Получаем все лицензии
        var allLicenses = _licenseService.GetAllLicenses();

        foreach (var license in allLicenses)
        {
            // Создаем копию шаблона карточки
            var card = new VisualElement();
            card.style.display = DisplayStyle.Flex;
            card.style.width = 300;
            card.style.marginLeft = card.style.marginRight = card.style.marginTop = card.style.marginBottom = 10;
            card.style.paddingLeft = card.style.paddingRight = card.style.paddingTop = card.style.paddingBottom = 15;
            card.style.backgroundColor = new StyleColor(new Color(22f/255f, 33f/255f, 62f/255f));
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            card.style.borderLeftWidth = card.style.borderRightWidth = card.style.borderTopWidth = card.style.borderBottomWidth = 1;
            card.style.borderLeftColor = card.style.borderRightColor = card.style.borderTopColor = card.style.borderBottomColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));

            // Создаем заголовок карточки
            var cardHeader = new VisualElement();
            cardHeader.style.flexDirection = FlexDirection.Row;
            cardHeader.style.justifyContent = Justify.SpaceBetween;
            cardHeader.style.alignItems = Align.Center;
            cardHeader.style.marginBottom = 10;

            var licenseName = new Label(license.LicenseName);
            licenseName.style.fontSize = 18;
            licenseName.style.color = Color.white;
            licenseName.style.unityFontStyleAndWeight = FontStyle.Bold;

            var productCount = new Label($"{license.GetProductCount()} товаров");
            productCount.style.fontSize = 12;
            productCount.style.color = new StyleColor(new Color(0.67f, 0.67f, 0.67f));
            productCount.style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
            productCount.style.paddingLeft = productCount.style.paddingRight = 8;
            productCount.style.paddingTop = productCount.style.paddingBottom = 2;
            productCount.style.borderTopLeftRadius = productCount.style.borderTopRightRadius = productCount.style.borderBottomLeftRadius = productCount.style.borderBottomRightRadius = 12;

            cardHeader.Add(licenseName);
            cardHeader.Add(productCount);

            // Описание
            var description = new Label(license.Description);
            description.style.fontSize = 14;
            description.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            description.style.marginBottom = 10;
            description.style.whiteSpace = WhiteSpace.Normal;

            // Список товаров
            var productsList = new VisualElement();
            productsList.style.marginBottom = 15;
            productsList.style.paddingLeft = productsList.style.paddingRight = productsList.style.paddingTop = productsList.style.paddingBottom = 10;
            productsList.style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
            productsList.style.borderTopLeftRadius = productsList.style.borderTopRightRadius = productsList.style.borderBottomLeftRadius = productsList.style.borderBottomRightRadius = 4;

            foreach (var productId in license.ProductIds)
            {
                var productConfig = _productCatalogService.GetProductConfigByID(productId);
                if (productConfig != null)
                {
                    var productItem = new Label(productConfig.ProductName);
                    productItem.style.fontSize = 12;
                    productItem.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                    productItem.style.marginBottom = 2;
                    productsList.Add(productItem);
                }
            }

            // Футер карточки
            var cardFooter = new VisualElement();
            cardFooter.style.flexDirection = FlexDirection.Row;
            cardFooter.style.justifyContent = Justify.SpaceBetween;
            cardFooter.style.alignItems = Align.Center;

            var price = new Label(license.Price > 0 ? $"${license.Price:F0}" : "Бесплатно");
            price.style.fontSize = 20;
            price.style.color = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
            price.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Проверяем, куплена ли лицензия
            bool isPurchased = _licenseService.IsLicensePurchased(license.LicenseId);
            
            if (isPurchased)
            {
                // Лицензия уже куплена
                var purchasedLabel = new Label("Куплено");
                purchasedLabel.style.fontSize = 14;
                purchasedLabel.style.color = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
                purchasedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                cardFooter.Add(price);
                cardFooter.Add(purchasedLabel);
            }
            else
            {
                // Лицензия доступна для покупки
                var purchaseButton = new Button(() => OnPurchaseLicenseClicked(license.LicenseId));
                purchaseButton.text = "Купить";
                purchaseButton.style.paddingLeft = purchaseButton.style.paddingRight = 16;
                purchaseButton.style.paddingTop = purchaseButton.style.paddingBottom = 8;
                purchaseButton.style.backgroundColor = new StyleColor(new Color(233f/255f, 69f/255f, 96f/255f));
                purchaseButton.style.color = Color.white;
                purchaseButton.style.borderTopLeftRadius = purchaseButton.style.borderTopRightRadius = purchaseButton.style.borderBottomLeftRadius = purchaseButton.style.borderBottomRightRadius = 4;
                purchaseButton.style.borderLeftWidth = purchaseButton.style.borderRightWidth = purchaseButton.style.borderTopWidth = purchaseButton.style.borderBottomWidth = 0;
                
                // Проверяем, хватает ли денег
                bool canAfford = _playerDataService != null && _playerDataService.GetMoney() >= license.Price;
                purchaseButton.SetEnabled(canAfford);
                
                cardFooter.Add(price);
                cardFooter.Add(purchaseButton);
            }

            // Собираем карточку
            card.Add(cardHeader);
            card.Add(description);
            card.Add(productsList);
            card.Add(cardFooter);

            // Добавляем карточку в контейнер
            _licensesContainer.Add(card);
        }

        Debug.Log($"ComputerUIHandler: Populated {allLicenses.Count} license cards");
    }

    private void OnPurchaseLicenseClicked(string licenseId)
    {
        if (_licenseService == null || _playerDataService == null)
        {
            Debug.LogError("ComputerUIHandler: Missing services for license purchase");
            return;
        }

        bool success = _licenseService.PurchaseLicense(licenseId);
        
        if (success)
        {
            var license = _licenseService.GetLicense(licenseId);
            if (license != null)
            {
                // Показываем уведомление об успешной покупке
                if (_notificationService != null)
                {
                    _notificationService.ShowNotification($"Лицензия '{license.LicenseName}' успешно приобретена!");
                }
                
                Debug.Log($"ComputerUIHandler: Successfully purchased license '{license.LicenseName}'");
                
                // Обновляем UI лицензий
                PopulateLicensesList();
                
                // Обновляем список товаров для заказа (могли разблокироваться новые товары)
                PopulateGoodsCards();
                
                // Обновляем список товаров для управления ценами
                PopulateProductPriceList();
            }
        }
        else
        {
            // Показываем уведомление об ошибке
            if (_notificationService != null)
            {
                var license = _licenseService.GetLicense(licenseId);
                string message = license != null 
                    ? $"Недостаточно средств для покупки '{license.LicenseName}'. Требуется: ${license.Price:F0}"
                    : "Ошибка при покупке лицензии";
                _notificationService.ShowNotification(message);
            }
            
            Debug.LogWarning($"ComputerUIHandler: Failed to purchase license '{licenseId}'");
        }
    }

    private void InitializeFurnitureSubcategories()
    {
        if (_furnitureSubcategoriesContainer == null) return;

        _furnitureSubcategoriesContainer.Clear();
        _furnitureSubcategoryButtons.Clear();

        var subcategories = new Dictionary<FurnitureSubcategory, string>
        {
            { FurnitureSubcategory.All, "Вся мебель" },
            { FurnitureSubcategory.Shelves, "Полки" },
            { FurnitureSubcategory.CashDesks, "Кассы" }
        };

        foreach (var subcategory in subcategories)
        {
            var button = new Button(() => OnFurnitureSubcategoryClicked(subcategory.Key));
            button.text = subcategory.Value;
            button.style.marginBottom = 5;
            button.style.height = 35;
            button.style.backgroundColor = new StyleColor(new Color(35f/255f, 50f/255f, 65f/255f));
            button.style.color = Color.white;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 3;
            button.style.borderLeftWidth = button.style.borderRightWidth = button.style.borderTopWidth = button.style.borderBottomWidth = 0;
            
            if (subcategory.Key == _currentFurnitureSubcategory)
            {
                button.style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
            }

            _furnitureSubcategoriesContainer.Add(button);
            _furnitureSubcategoryButtons.Add(button);
        }
    }

    private void OnFurnitureSubcategoryClicked(FurnitureSubcategory subcategory)
    {
        _currentFurnitureSubcategory = subcategory;
        UpdateFurnitureSubcategoryButtons();
        PopulateFurnitureCards();
    }

    private void UpdateFurnitureSubcategoryButtons()
    {
        var subcategories = new List<FurnitureSubcategory> 
        { 
            FurnitureSubcategory.All, FurnitureSubcategory.Shelves, FurnitureSubcategory.CashDesks
        };

        for (int i = 0; i < _furnitureSubcategoryButtons.Count && i < subcategories.Count; i++)
        {
            if (subcategories[i] == _currentFurnitureSubcategory)
            {
                _furnitureSubcategoryButtons[i].style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
            }
            else
            {
                _furnitureSubcategoryButtons[i].style.backgroundColor = new StyleColor(new Color(35f/255f, 50f/255f, 65f/255f));
            }
        }
    }

    private void PopulateActiveOrdersList()
    {
        if (_activeOrdersContainer == null || _orderCardTemplate == null || _deliveryService == null)
        {
            Debug.LogError("ComputerUIHandler: Missing dependencies for active orders UI");
            return;
        }

        // Очищаем контейнер
        _activeOrdersContainer.Clear();

        // Получаем активные заказы
        var activeOrders = _deliveryService.GetActiveOrders();

        if (activeOrders.Count == 0)
        {
            // Показываем сообщение об отсутствии заказов
            var noOrdersLabel = new Label("Нет активных заказов");
            noOrdersLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            noOrdersLabel.style.fontSize = 14;
            noOrdersLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noOrdersLabel.style.marginTop = 50;
            _activeOrdersContainer.Add(noOrdersLabel);
            return;
        }

        foreach (var order in activeOrders)
        {
            CreateOrderCard(order);
        }

        Debug.Log($"ComputerUIHandler: Populated {activeOrders.Count} active order cards");
    }

    private void CreateOrderCard(OrderSaveData order)
    {
        // Создаем копию шаблона карточки заказа
        var card = new VisualElement();
        card.style.display = DisplayStyle.Flex;
        card.style.marginBottom = 15;
        card.style.paddingLeft = card.style.paddingRight = card.style.paddingTop = card.style.paddingBottom = 15;
        card.style.backgroundColor = new StyleColor(new Color(25f/255f, 40f/255f, 55f/255f));
        card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
        card.style.borderLeftWidth = 4;
        card.style.borderLeftColor = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));

        // Заголовок заказа
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 10;

        var orderId = new Label($"Заказ #{order.OrderId.Replace("ORDER_", "")}");
        orderId.style.fontSize = 16;
        orderId.style.color = Color.white;
        orderId.style.unityFontStyleAndWeight = FontStyle.Bold;

        var status = new Label("В пути");
        status.style.fontSize = 12;
        status.style.color = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
        status.style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
        status.style.paddingLeft = status.style.paddingRight = 12;
        status.style.paddingTop = status.style.paddingBottom = 4;
        status.style.borderTopLeftRadius = status.style.borderTopRightRadius = status.style.borderBottomLeftRadius = status.style.borderBottomRightRadius = 12;

        header.Add(orderId);
        header.Add(status);

        // Информация о времени
        var timeInfo = new VisualElement();
        timeInfo.style.flexDirection = FlexDirection.Row;
        timeInfo.style.justifyContent = Justify.SpaceBetween;
        timeInfo.style.marginBottom = 10;

        var orderTimeContainer = new VisualElement();
        orderTimeContainer.style.flexDirection = FlexDirection.Column;
        var orderTimeLabel = new Label("Время заказа:");
        orderTimeLabel.style.fontSize = 11;
        orderTimeLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
        var orderTime = new Label(order.OrderTime.ToString("HH:mm"));
        orderTime.style.fontSize = 13;
        orderTime.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
        orderTimeContainer.Add(orderTimeLabel);
        orderTimeContainer.Add(orderTime);

        var remainingTimeContainer = new VisualElement();
        remainingTimeContainer.style.flexDirection = FlexDirection.Column;
        remainingTimeContainer.style.alignItems = Align.FlexEnd;
        var remainingLabel = new Label("Осталось:");
        remainingLabel.style.fontSize = 11;
        remainingLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
        var remainingTime = new Label(FormatTime(order.DeliveryTime));
        remainingTime.name = "TimeRemaining";
        remainingTime.style.fontSize = 14;
        remainingTime.style.color = new StyleColor(new Color(1f, 0.8f, 0.2f));
        remainingTime.style.unityFontStyleAndWeight = FontStyle.Bold;
        remainingTimeContainer.Add(remainingLabel);
        remainingTimeContainer.Add(remainingTime);

        timeInfo.Add(orderTimeContainer);
        timeInfo.Add(remainingTimeContainer);

        // Список товаров в заказе
        var itemsContainer = new VisualElement();
        itemsContainer.style.backgroundColor = new StyleColor(new Color(15f/255f, 52f/255f, 96f/255f));
        itemsContainer.style.paddingLeft = itemsContainer.style.paddingRight = itemsContainer.style.paddingTop = itemsContainer.style.paddingBottom = 10;
        itemsContainer.style.borderTopLeftRadius = itemsContainer.style.borderTopRightRadius = itemsContainer.style.borderBottomLeftRadius = itemsContainer.style.borderBottomRightRadius = 4;
        itemsContainer.style.marginBottom = 10;

        var itemsLabel = new Label("Товары в заказе:");
        itemsLabel.style.fontSize = 12;
        itemsLabel.style.color = new StyleColor(new Color(0.67f, 0.67f, 0.67f));
        itemsLabel.style.marginBottom = 5;
        itemsContainer.Add(itemsLabel);

        var itemsList = new VisualElement();
        itemsList.style.flexDirection = FlexDirection.Column;
        foreach (var item in order.Items)
        {
            // Получаем название товара по его ID
            string productName = item.ProductType;
            if (_productCatalogService != null)
            {
                var productConfig = _productCatalogService.GetProductConfigByID(item.ProductType);
                if (productConfig != null)
                {
                    productName = productConfig.ProductName;
                }
            }
            
            var itemRow = new Label($"• {productName} x{item.Quantity}");
            itemRow.style.fontSize = 11;
            itemRow.style.color = Color.white;
            itemRow.style.marginBottom = 2;
            itemsList.Add(itemRow);
        }
        itemsContainer.Add(itemsList);

        // Стоимость и кнопка отмены
        var footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.style.alignItems = Align.Center;

        var total = new Label($"Итого: ${order.TotalCost:F0}");
        total.style.fontSize = 16;
        total.style.color = new StyleColor(new Color(76f/255f, 175f/255f, 80f/255f));
        total.style.unityFontStyleAndWeight = FontStyle.Bold;

        var cancelButton = new Button(() => OnCancelOrderClicked(order.OrderId));
        cancelButton.text = "Отменить заказ";
        cancelButton.style.paddingLeft = cancelButton.style.paddingRight = 12;
        cancelButton.style.paddingTop = cancelButton.style.paddingBottom = 6;
        cancelButton.style.backgroundColor = new StyleColor(new Color(233f/255f, 69f/255f, 96f/255f));
        cancelButton.style.color = Color.white;
        cancelButton.style.borderTopLeftRadius = cancelButton.style.borderTopRightRadius = cancelButton.style.borderBottomLeftRadius = cancelButton.style.borderBottomRightRadius = 4;
        cancelButton.style.borderLeftWidth = cancelButton.style.borderRightWidth = cancelButton.style.borderTopWidth = cancelButton.style.borderBottomWidth = 0;

        footer.Add(total);
        footer.Add(cancelButton);

        // Собираем карточку
        card.Add(header);
        card.Add(timeInfo);
        card.Add(itemsContainer);
        card.Add(footer);

        // Добавляем карточку в контейнер
        _activeOrdersContainer.Add(card);
    }

    private string FormatTime(float seconds)
    {
        if (seconds <= 0) return "Доставляется...";
        
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:D2}:{secs:D2}";
    }

    private void OnCancelOrderClicked(string orderId)
    {
        if (_deliveryService == null)
        {
            Debug.LogError("ComputerUIHandler: DeliveryService is null, cannot cancel order");
            return;
        }

        float refund = _deliveryService.CancelOrder(orderId);
        
        if (refund > 0)
        {
            _notificationService?.ShowNotification($"Заказ отменен. Возврат: ${refund:F0}", NotificationType.Warning);
        }
        else
        {
            _notificationService?.ShowNotification("Не удалось отменить заказ", NotificationType.Error);
        }

        // Обновляем список заказов
        PopulateActiveOrdersList();
    }

    void Update()
    {
        // Обновляем таймеры активных заказов только если секция видима и прошла секунда
        if (_isActiveOrdersSectionVisible && _deliveryService != null && Time.time - _lastTimerUpdate >= TIMER_UPDATE_INTERVAL)
        {
            var activeOrders = _deliveryService.GetActiveOrders();
            
            // Проверяем, нужно ли обновлять UI (изменилось количество заказов или время)
            if (activeOrders.Count != _lastOrdersCount)
            {
                // Пересоздаем список если количество заказов изменилось
                PopulateActiveOrdersList();
                _lastOrdersCount = activeOrders.Count;
            }
            else if (activeOrders.Count > 0)
            {
                // Обновляем только таймеры если заказы есть
                UpdateActiveOrdersTimersOptimized(activeOrders);
            }
            
            _lastTimerUpdate = Time.time;
        }
    }

    private void UpdateActiveOrdersTimersOptimized(List<OrderSaveData> activeOrders)
    {
        if (_activeOrdersContainer == null) return;

        var orderCards = _activeOrdersContainer.Children().ToList();
        int updatedCount = 0;
        
        for (int i = 0; i < orderCards.Count && i < activeOrders.Count; i++)
        {
            var card = orderCards[i];
            var order = activeOrders[i];
            
            // Ищем именно элемент с таймером обратного отсчета
            var timeRemainingLabel = card.Q<Label>("TimeRemaining");
            if (timeRemainingLabel != null)
            {
                string newTime = FormatTime(order.DeliveryTime);
                if (timeRemainingLabel.text != newTime)
                {
                    timeRemainingLabel.text = newTime;
                    updatedCount++;
                }
            }
        }
        
        if (updatedCount > 0)
        {
            Debug.Log($"ComputerUIHandler: Updated {updatedCount} order timers");
        }
    }
    
    private void InitializeStoreSettings()
    {
        if (_storeNameField != null && _supermarketNameService != null)
        {
            _storeNameField.value = _supermarketNameService.CurrentName;
        }
    }
    
    private void OnChangeStoreNameClicked(ClickEvent evt)
    {
        if (_supermarketNameService == null)
        {
            ShowComputerNotification("✕ Сервис названия супермаркета недоступен", NotificationType.Error);
            return;
        }
        
        if (_storeNameField == null)
        {
            ShowComputerNotification("✕ Поле ввода названия не найдено", NotificationType.Error);
            return;
        }
        
        string newName = _storeNameField.value?.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            ShowComputerNotification("⚠ Название не может быть пустым", NotificationType.Warning);
            return;
        }
        
        // Проверяем, отличается ли новое название от текущего
        string currentName = _supermarketNameService.CurrentName;
        if (newName == currentName)
        {
            ShowComputerNotification("⚠ Название не изменилось", NotificationType.Warning);
            return;
        }
        
        _supermarketNameService.SetName(newName);
        ShowComputerNotification($"✓ Название изменено на: {newName}", NotificationType.Success);
        
        Debug.Log($"ComputerUIHandler: Store name changed to '{newName}'");
    }
    

    
    /// <summary>
    /// Показывает уведомление прямо в интерфейсе компьютера
    /// </summary>
    private void ShowComputerNotification(string message, NotificationType type, float duration = 3f)
    {
        if (_computerNotificationContainer == null) return;

        // Создаем элемент уведомления
        var notification = new Label(message);
        notification.AddToClassList("computer-notification");
        
        // Стилизация в зависимости от типа
        Color backgroundColor = type switch
        {
            NotificationType.Success => new Color(46f/255f, 125f/255f, 50f/255f, 0.9f), // Зеленый
            NotificationType.Warning => new Color(255f/255f, 152f/255f, 0f/255f, 0.9f), // Оранжевый
            NotificationType.Error => new Color(198f/255f, 40f/255f, 40f/255f, 0.9f),   // Красный
            _ => new Color(33f/255f, 150f/255f, 243f/255f, 0.9f) // Синий для Info
        };
        
        notification.style.backgroundColor = backgroundColor;
        notification.style.color = Color.white;
        notification.style.paddingLeft = 15;
        notification.style.paddingRight = 15;
        notification.style.paddingTop = 10;
        notification.style.paddingBottom = 10;
        notification.style.marginBottom = 10;
        notification.style.borderTopLeftRadius = 8;
        notification.style.borderTopRightRadius = 8;
        notification.style.borderBottomLeftRadius = 8;
        notification.style.borderBottomRightRadius = 8;
        notification.style.fontSize = 14;
        notification.style.whiteSpace = WhiteSpace.Normal;
        notification.style.textOverflow = TextOverflow.Ellipsis;
        
        // Добавляем тень
        notification.style.borderLeftWidth = notification.style.borderRightWidth = 
            notification.style.borderTopWidth = notification.style.borderBottomWidth = 1;
        notification.style.borderLeftColor = notification.style.borderRightColor = 
            notification.style.borderTopColor = notification.style.borderBottomColor = 
            new Color(0f, 0f, 0f, 0.2f);

        // Добавляем в контейнер
        _computerNotificationContainer.Add(notification);

        // Запускаем корутину для автоматического удаления
        StartCoroutine(RemoveComputerNotificationAfterDelay(notification, duration));
    }
    
    /// <summary>
    /// Удаляет уведомление через заданное время
    /// </summary>
    private System.Collections.IEnumerator RemoveComputerNotificationAfterDelay(VisualElement notification, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notification?.parent != null)
        {
            notification.parent.Remove(notification);
        }
    }
    
    /// <summary>
    /// Обновляет отображение денег игрока в интерфейсе компьютера
    /// </summary>
    private void UpdatePlayerMoneyDisplay()
    {
        if (_playerMoneyLabel != null && _playerDataService != null)
        {
            float money = _playerDataService.GetMoney();
            _playerMoneyLabel.text = $"${money:F0}";
        }
    }
} 