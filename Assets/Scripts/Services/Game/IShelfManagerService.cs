using System.Collections.Generic;
using UnityEngine;
using Supermarket.Data;
using Supermarket.Interactables;

namespace Supermarket.Services.Game
{
    public interface IShelfManagerService
    {
        /// <summary>
        /// Регистрирует полку в системе для сохранения
        /// </summary>
        void RegisterShelf(ShelfController shelf);
        
        /// <summary>
        /// Убирает полку из системы сохранения
        /// </summary>
        void UnregisterShelf(ShelfController shelf);
        
        /// <summary>
        /// Регистрирует многоуровневую полку в системе для сохранения
        /// </summary>
        void RegisterMultiLevelShelf(MultiLevelShelfController multiShelf);
        
        /// <summary>
        /// Убирает многоуровневую полку из системы сохранения
        /// </summary>
        void UnregisterMultiLevelShelf(MultiLevelShelfController multiShelf);
        
        /// <summary>
        /// Собирает данные всех полок для сохранения
        /// </summary>
        List<ShelfSaveData> GetShelvesSaveData();
        
        /// <summary>
        /// Восстанавливает состояние полок из сохраненных данных
        /// </summary>
        void RestoreShelves(List<ShelfSaveData> shelvesData);
        
        /// <summary>
        /// Очищает все полки (например, при новой игре)
        /// </summary>
        void ClearAllShelves();
    }
} 