using System.Collections.Generic;
using UnityEngine;
using Supermarket.Data;
using Supermarket.Interactables; // Для BoxController

namespace Supermarket.Services.Game
{
    public interface IBoxManagerService
    {
        /// <summary>
        /// Регистрирует коробку в сцене для системы сохранения
        /// </summary>
        void RegisterBox(BoxController box);
        
        /// <summary>
        /// Убирает коробку из системы сохранения (когда игрок ее поднимает или уничтожает)
        /// </summary>
        void UnregisterBox(BoxController box);
        
        /// <summary>
        /// Собирает данные всех коробок на земле для сохранения
        /// </summary>
        List<BoxSaveData> GetBoxesSaveData();
        
        /// <summary>
        /// Восстанавливает коробки на сцене из сохраненных данных
        /// </summary>
        void RestoreBoxes(List<BoxSaveData> boxesData);
        
        /// <summary>
        /// Создает новую коробку в указанной позиции
        /// </summary>
        /// <param name="boxData">Данные коробки</param>
        /// <param name="position">Позиция создания</param>
        /// <param name="isPhysical">Если true, коробка будет физической (с gravity)</param>
        void CreateBox(BoxData boxData, Vector3 position, bool isPhysical);
        
        /// <summary>
        /// Создает новую коробку в указанной позиции с начальной скоростью
        /// </summary>
        /// <param name="boxData">Данные коробки</param>
        /// <param name="position">Позиция создания</param>
        /// <param name="isPhysical">Если true, коробка будет физической (с gravity)</param>
        /// <param name="initialVelocity">Начальная скорость для физической коробки</param>
        void CreateBox(BoxData boxData, Vector3 position, bool isPhysical, Vector3 initialVelocity);
        
        /// <summary>
        /// Очищает все коробки на сцене (например, при новой игре)
        /// </summary>
        void ClearAllBoxes();
    }
} 