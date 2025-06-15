using System.Collections.Generic;

public enum ProductSubcategory
{
    All = 0,
    Drinks = 1,      // Напитки
    Snacks = 2,      // Снеки
    Dairy = 3,       // Молочные продукты
    Sweets = 4,      // Сладости
    Nuts = 5         // Орехи
}

public enum FurnitureSubcategory
{
    All = 0,
    Shelves = 1,     // Полки
    CashDesks = 2    // Кассы
}

public interface IProductCatalogService
{
    List<ProductConfig> GetAllProductConfigs();
    ProductConfig GetProductConfigByID(string productID);
    
    /// <summary>
    /// Получить только разблокированные товары
    /// </summary>
    List<ProductConfig> GetUnlockedProductConfigs();
    
    /// <summary>
    /// Получить товары, доступные для заказа (разблокированные и с флагом CanBeOrdered)
    /// </summary>
    List<ProductConfig> GetOrderableProductConfigs();
    
    /// <summary>
    /// Проверить, разблокирован ли товар
    /// </summary>
    bool IsProductUnlocked(string productID);
    
    // Методы для категорий
    List<ProductConfig> GetOrderableProductConfigsByCategory(ProductCategory category);
    List<ProductConfig> GetProductConfigsByCategory(ProductCategory category);
    List<ProductConfig> GetProductConfigsBySubcategory(ProductSubcategory subcategory);
    List<ProductConfig> GetOrderableProductConfigsBySubcategory(ProductSubcategory subcategory);
    ProductSubcategory GetProductSubcategory(ProductConfig product);
    
    // Методы для подкатегорий мебели
    List<ProductConfig> GetFurnitureConfigsBySubcategory(FurnitureSubcategory subcategory);
    List<ProductConfig> GetOrderableFurnitureConfigsBySubcategory(FurnitureSubcategory subcategory);
    FurnitureSubcategory GetFurnitureSubcategory(ProductConfig product);
} 