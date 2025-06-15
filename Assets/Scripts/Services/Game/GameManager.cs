using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;

public class GameManager : MonoBehaviour
{
    private IProductCatalogService _productCatalogService;
    private IInventoryService _inventoryService;
    private IPlayerDataService _playerDataService;
    private IPlacementService _placementService;
    private ICustomerSpawnerService _customerSpawnerService;
    
    [Header("Game Settings")]
    [SerializeField] private bool _autoStartCustomerSpawning = true;
    [SerializeField] private float _customerSpawnDelay = 3f; // Задержка перед началом спавна

    [Inject]
    public void Construct(
        IProductCatalogService productCatalogService,
        IInventoryService inventoryService,
        IPlayerDataService playerDataService,
        IPlacementService placementService,
        ICustomerSpawnerService customerSpawnerService)
    {
        _productCatalogService = productCatalogService;
        _inventoryService = inventoryService;
        _playerDataService = playerDataService;
        _placementService = placementService;
        _customerSpawnerService = customerSpawnerService;

        Debug.Log("GameManager: Dependencies injected.");
        if (_placementService == null) Debug.LogError("GameManager: IPlacementService is NULL after injection!");
        if (_customerSpawnerService == null) Debug.LogError("GameManager: ICustomerSpawnerService is NULL after injection!");
    }

    void Start()
    {
        if (_playerDataService != null)
        {
            Debug.Log($"GameManager: Player starting money: {_playerDataService.CurrentPlayerData.Money}");
        }
        else
        {
            Debug.LogError("GameManager: IPlayerDataService is null in Start!");
        }

        if (_productCatalogService != null)
        {
            var products = _productCatalogService.GetAllProductConfigs();
            Debug.Log($"GameManager: Number of available products in catalog: {products.Count}");
            foreach (var product in products)
            {
                Debug.Log($" - Product: {product.ProductName}, Price: {product.BaseSalePrice}, Category: {product.ObjectCategory}, Prefab: {(product.Prefab != null ? product.Prefab.name : "None")}");
            }
        }
        else
        {
            Debug.LogError("GameManager: IProductCatalogService is null in Start!");
        }

        if (_inventoryService != null)
        {
            // Пример: добавляем немного товара на склад для теста
            _inventoryService.AddToStock("MILK_01", 10); // Предположим, есть товар с таким ID
            _inventoryService.AddToStock("BREAD_01", 20);

            var stock = _inventoryService.GetAllStockedItems();
            Debug.Log($"GameManager: Current stock count: {stock.Count}");
            foreach (var item in stock)
            {
                Debug.Log($" - Item: {item.Key}, Quantity: {item.Value}");
            }
        }
        else
        {
            Debug.LogError("GameManager: IInventoryService is null in Start!");
        }

        // Запуск спавна покупателей
        if (_customerSpawnerService != null && _autoStartCustomerSpawning)
        {
            Invoke(nameof(StartCustomerSpawning), _customerSpawnDelay);
            Debug.Log($"GameManager: Customer spawning will start in {_customerSpawnDelay} seconds.");
        }

        Debug.Log("GameManager: Game Started (basic initialization complete).");
    }
    
    private void StartCustomerSpawning()
    {
        if (_customerSpawnerService != null && !_customerSpawnerService.IsSpawning)
        {
            _customerSpawnerService.StartSpawning();
            Debug.Log("GameManager: Customer spawning started!");
        }
    }
    
    void OnDestroy()
    {
        // Остановка спавна при уничтожении GameManager
        if (_customerSpawnerService != null && _customerSpawnerService.IsSpawning)
        {
            _customerSpawnerService.StopSpawning();
            Debug.Log("GameManager: Customer spawning stopped.");
        }
    }
} 