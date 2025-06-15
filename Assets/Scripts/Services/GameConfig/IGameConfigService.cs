using System.Collections.Generic;
using Core.Models;

public interface IGameConfigService
{
    GameConfiguration GetGameConfiguration();
    ProductConfig GetProductByID(string productID);
    List<ProductConfig> GetAllProducts();
    // Добавить методы для доступа к другим типам конфигураций по мере их добавления
    
    // Методы для работы с лицензиями
    List<ProductLicense> GetAllLicenses();
} 