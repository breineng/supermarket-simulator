using System;

namespace Supermarket.Services.Game
{
    public interface ISupermarketNameService
    {
        /// <summary>
        /// Текущее название супермаркета
        /// </summary>
        string CurrentName { get; }
        
        /// <summary>
        /// Событие изменения названия супермаркета
        /// </summary>
        event Action<string> OnNameChanged;
        
        /// <summary>
        /// Устанавливает новое название супермаркета
        /// </summary>
        /// <param name="newName">Новое название</param>
        void SetName(string newName);
        
        /// <summary>
        /// Загружает название из данных сохранения
        /// </summary>
        /// <param name="name">Название из сохранения</param>
        void LoadName(string name);
    }
} 