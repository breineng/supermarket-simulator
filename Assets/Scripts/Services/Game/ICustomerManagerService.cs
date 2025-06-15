using System.Collections.Generic;
using Supermarket.Data;
using Supermarket.Interactables;
using UnityEngine;

namespace Supermarket.Services.Game
{
    public interface ICustomerManagerService
    {
        /// <summary>
        /// Регистрирует покупателя в менеджере
        /// </summary>
        void RegisterCustomer(CustomerController customer);
        
        /// <summary>
        /// Отменяет регистрацию покупателя
        /// </summary>
        void UnregisterCustomer(CustomerController customer);
        
        /// <summary>
        /// Получает данные всех активных покупателей для сохранения
        /// </summary>
        List<CustomerSaveData> GetCustomersSaveData();
        
        /// <summary>
        /// Восстанавливает покупателей из сохраненных данных
        /// </summary>
        void RestoreCustomers(List<CustomerSaveData> customersData);
        
        /// <summary>
        /// Очищает всех активных покупателей (для новой игры)
        /// </summary>
        void ClearAllCustomers();
        
        /// <summary>
        /// Получает количество активных покупателей
        /// </summary>
        int GetActiveCustomerCount();
        
        /// <summary>
        /// Находит покупателя по имени (для отладки)
        /// </summary>
        CustomerController FindCustomerByName(string name);
    }
} 