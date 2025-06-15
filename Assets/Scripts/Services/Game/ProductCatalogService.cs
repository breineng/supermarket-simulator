using System.Collections.Generic;
using System.Linq;
// Для [Inject] больше не нужно BehaviourInject, если только для других целей
using UnityEngine; // Для Debug
using Supermarket.Services.Game; // Для ILicenseService

public class ProductCatalogService : IProductCatalogService
{
    private readonly IGameConfigService _gameConfigService;
    private readonly ILicenseService _licenseService;

    // Зависимости внедряются через конструктор
    public ProductCatalogService(IGameConfigService gameConfigService, ILicenseService licenseService)
    {
        _gameConfigService = gameConfigService;
        _licenseService = licenseService;
        
        if (_gameConfigService == null)
        {
            // Это предупреждение полезно, если BInject не сможет разрешить зависимость
            Debug.LogError("ProductCatalogService: IGameConfigService is null in constructor! Ensure it's registered in a parent or current context.");
        }
        
        if (_licenseService == null)
        {
            Debug.LogError("ProductCatalogService: ILicenseService is null in constructor! Ensure it's registered in a parent or current context.");
        }
    }

    public List<ProductConfig> GetAllProductConfigs()
    {
        if (_gameConfigService == null) 
        {
            Debug.LogError("ProductCatalogService: _gameConfigService is null in GetAllProductConfigs. Was it injected correctly?");
            return new List<ProductConfig>();
        }
        // Возвращаем копию списка, чтобы предотвратить внешние изменения оригинального списка конфигураций
        return new List<ProductConfig>(_gameConfigService.GetAllProducts());
    }

    public ProductConfig GetProductConfigByID(string productID)
    {
        if (_gameConfigService == null) 
        {
             Debug.LogError("ProductCatalogService: _gameConfigService is null in GetProductConfigByID. Was it injected correctly?");
            return null;
        }
        return _gameConfigService.GetProductByID(productID);
    }
    
    public List<ProductConfig> GetUnlockedProductConfigs()
    {
        if (_gameConfigService == null || _licenseService == null)
        {
            Debug.LogError("ProductCatalogService: Missing dependencies in GetUnlockedProductConfigs");
            return new List<ProductConfig>();
        }
        
        var allProducts = _gameConfigService.GetAllProducts();
        var unlockedProducts = new List<ProductConfig>();
        
        foreach (var product in allProducts)
        {
            // Мебель всегда доступна без лицензий (нужна для обустройства магазина)
            // Товары проверяются через систему лицензий
            if (IsFurniture(product) || _licenseService.IsProductUnlocked(product.ProductID))
            {
                unlockedProducts.Add(product);
            }
        }
        
        return unlockedProducts;
    }
    
    public List<ProductConfig> GetOrderableProductConfigs()
    {
        // Получаем разблокированные товары и фильтруем только те, которые можно заказать
        return GetUnlockedProductConfigs()
            .Where(p => p.CanBeOrdered)
            .ToList();
    }
    
    public List<ProductConfig> GetOrderableProductConfigsByCategory(ProductCategory category)
    {
        var orderableProducts = GetOrderableProductConfigs();
        
        if (category == ProductCategory.All)
            return orderableProducts;
            
        return FilterByCategory(orderableProducts, category);
    }
    
    public List<ProductConfig> GetProductConfigsByCategory(ProductCategory category)
    {
        var allProducts = GetAllProductConfigs();
        
        if (category == ProductCategory.All)
            return allProducts;
            
        return FilterByCategory(allProducts, category);
    }
    
    private List<ProductConfig> FilterByCategory(List<ProductConfig> products, ProductCategory category)
    {
        switch (category)
        {
            case ProductCategory.Goods:
                return products.Where(p => p.ObjectCategory == PlaceableObjectType.Goods).ToList();
                
            case ProductCategory.Furniture:
                return products.Where(p => IsFurniture(p)).ToList();
                
            default:
                return products;
        }
    }
    
    private bool IsFurniture(ProductConfig product)
    {
        return product.ObjectCategory == PlaceableObjectType.Shelf || 
               product.ObjectCategory == PlaceableObjectType.CashDesk;
    }
    
    public bool IsProductUnlocked(string productID)
    {
        if (_licenseService == null)
        {
            Debug.LogError("ProductCatalogService: _licenseService is null in IsProductUnlocked");
            return false;
        }
        
        var product = GetProductConfigByID(productID);
        if (product == null)
            return false;
            
        // Мебель всегда доступна без лицензий (для обустройства магазина)
        if (IsFurniture(product))
            return true;
            
        return _licenseService.IsProductUnlocked(productID);
    }

    public List<ProductConfig> GetProductConfigsBySubcategory(ProductSubcategory subcategory)
    {
        var goodsProducts = GetProductConfigsByCategory(ProductCategory.Goods);
        
        if (subcategory == ProductSubcategory.All)
            return goodsProducts;
            
        return goodsProducts.Where(p => GetProductSubcategory(p) == subcategory).ToList();
    }
    
    public List<ProductConfig> GetOrderableProductConfigsBySubcategory(ProductSubcategory subcategory)
    {
        var orderableGoods = GetOrderableProductConfigsByCategory(ProductCategory.Goods);
        
        if (subcategory == ProductSubcategory.All)
            return orderableGoods;
            
        return orderableGoods.Where(p => GetProductSubcategory(p) == subcategory).ToList();
    }
    
    public ProductSubcategory GetProductSubcategory(ProductConfig product)
    {
        // Мебель не имеет подкатегорий товаров
        if (IsFurniture(product))
            return ProductSubcategory.All;
            
        // Определяем подкатегорию по названию товара
        string productName = product.ProductName.ToLower();
        
        if (productName.Contains("сок") || productName.Contains("чай") || productName.Contains("вода") || productName.Contains("энергетик"))
            return ProductSubcategory.Drinks;
            
        if (productName.Contains("чипсы") || productName.Contains("крекеры"))
            return ProductSubcategory.Snacks;
            
        if (productName.Contains("молоко") || productName.Contains("йогурт") || productName.Contains("творог"))
            return ProductSubcategory.Dairy;
            
        if (productName.Contains("шоколад") || productName.Contains("конфеты") || productName.Contains("жвачка"))
            return ProductSubcategory.Sweets;
            
        if (productName.Contains("орехи") || productName.Contains("арахис") || productName.Contains("фисташки"))
            return ProductSubcategory.Nuts;
            
        // По умолчанию все остальные товары попадают в снеки
        return ProductSubcategory.Snacks;
    }
    
    public List<ProductConfig> GetFurnitureConfigsBySubcategory(FurnitureSubcategory subcategory)
    {
        var furnitureProducts = GetProductConfigsByCategory(ProductCategory.Furniture);
        
        if (subcategory == FurnitureSubcategory.All)
            return furnitureProducts;
            
        return furnitureProducts.Where(p => GetFurnitureSubcategory(p) == subcategory).ToList();
    }
    
    public List<ProductConfig> GetOrderableFurnitureConfigsBySubcategory(FurnitureSubcategory subcategory)
    {
        var orderableFurniture = GetOrderableProductConfigsByCategory(ProductCategory.Furniture);
        
        if (subcategory == FurnitureSubcategory.All)
            return orderableFurniture;
            
        return orderableFurniture.Where(p => GetFurnitureSubcategory(p) == subcategory).ToList();
    }
    
    public FurnitureSubcategory GetFurnitureSubcategory(ProductConfig product)
    {
        // Только мебель имеет подкатегории мебели
        if (!IsFurniture(product))
            return FurnitureSubcategory.All;
            
        // Определяем подкатегорию по типу объекта
        switch (product.ObjectCategory)
        {
            case PlaceableObjectType.Shelf:
                return FurnitureSubcategory.Shelves;
                
            case PlaceableObjectType.CashDesk:
                return FurnitureSubcategory.CashDesks;
                
            default:
                return FurnitureSubcategory.All;
        }
    }
} 