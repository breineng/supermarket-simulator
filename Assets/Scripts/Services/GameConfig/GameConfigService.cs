using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Core.Models;

public class GameConfigService : IGameConfigService
{
    private GameConfiguration _gameConfiguration;
    private LicenseConfiguration _licenseConfiguration;
    private Dictionary<string, ProductConfig> _productsByID;

    public GameConfigService()
    {
        // Путь для Resources.Load должен быть без Assets/Resources и без расширения .asset
        _gameConfiguration = Resources.Load<GameConfiguration>("Data/GameConfig/GameConfiguration");
        _licenseConfiguration = Resources.Load<LicenseConfiguration>("Data/GameConfig/LicenseConfiguration");

        if (_gameConfiguration == null)
        {
            Debug.LogError("GameConfigService: Failed to load GameConfiguration from Resources/Data/GameConfig/GameConfiguration.asset");
            _productsByID = new Dictionary<string, ProductConfig>(); // Инициализируем пустым, чтобы избежать NullRef
            return;
        }

        if (_licenseConfiguration == null)
        {
            Debug.LogWarning("GameConfigService: Failed to load LicenseConfiguration from Resources/Data/GameConfig/LicenseConfiguration.asset");
        }

        // Кэшируем продукты по ID для быстрого доступа
        _productsByID = new Dictionary<string, ProductConfig>();
        if (_gameConfiguration.AllProducts != null)
        {
            foreach (var product in _gameConfiguration.AllProducts)
            {
                if (product != null && !string.IsNullOrEmpty(product.ProductID) && !_productsByID.ContainsKey(product.ProductID))
                {
                    _productsByID.Add(product.ProductID, product);
                }
                else if (product != null && _productsByID.ContainsKey(product.ProductID))
                {
                     Debug.LogWarning($"GameConfigService: Duplicate ProductID '{product.ProductID}' found for product '{product.ProductName}'. Only the first one was added.");
                }
                else if (product == null)
                {
                    Debug.LogWarning("GameConfigService: Found a null ProductConfig in GameConfiguration.AllProducts list.");
                }
                else if (string.IsNullOrEmpty(product.ProductID))
                {
                     Debug.LogWarning($"GameConfigService: Product '{product.ProductName}' has a null or empty ProductID and was not added to the lookup dictionary.");
                }
            }
        }
        else
        {
             Debug.LogWarning("GameConfigService: GameConfiguration.AllProducts list is null.");
        }
    }

    public GameConfiguration GetGameConfiguration()
    {
        return _gameConfiguration;
    }

    public ProductConfig GetProductByID(string productID)
    {
        if (string.IsNullOrEmpty(productID) || _productsByID == null) return null;
        _productsByID.TryGetValue(productID, out ProductConfig product);
        return product;
    }

    public List<ProductConfig> GetAllProducts()
    {
        return _gameConfiguration != null && _gameConfiguration.AllProducts != null 
               ? _gameConfiguration.AllProducts 
               : new List<ProductConfig>();
    }

    public List<ProductLicense> GetAllLicenses()
    {
        if (_licenseConfiguration == null || _licenseConfiguration.AllLicenses == null)
        {
            Debug.LogWarning("GameConfigService: No license configuration available");
            return new List<ProductLicense>();
        }
        
        return new List<ProductLicense>(_licenseConfiguration.AllLicenses);
    }
} 