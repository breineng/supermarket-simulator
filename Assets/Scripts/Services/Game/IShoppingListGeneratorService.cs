using System.Collections.Generic;
using Supermarket.Data;

namespace Supermarket.Services.Game
{
    public interface IShoppingListGeneratorService
    {
        /// <summary>
        /// Генерирует случайный список покупок для клиента
        /// </summary>
        List<ShoppingItem> GenerateShoppingList(float customerMoney);
        
        /// <summary>
        /// Генерирует список покупок с указанным количеством товаров
        /// </summary>
        List<ShoppingItem> GenerateShoppingList(float customerMoney, int itemCount);
        
        /// <summary>
        /// Генерирует список покупок с учетом предпочтений
        /// </summary>
        List<ShoppingItem> GenerateShoppingListWithPreferences(
            float customerMoney, 
            CharacterAppearanceConfig.Gender gender,
            int minItems = 1,
            int maxItems = 5
        );
    }
}