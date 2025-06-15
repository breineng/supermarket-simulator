using UnityEngine;
using Supermarket.Data;

namespace Supermarket.Services.Game
{
    public interface ICustomerSpawnerService
    {
        // Запуск/остановка спавна покупателей
        void StartSpawning();
        void StopSpawning();
        bool IsSpawning { get; }
        
        // Настройки спавна
        void SetSpawnInterval(float intervalInSeconds);
        void SetMaxCustomers(int maxCustomers);
        
        // Управление точками спавна
        void AddSpawnPoint(Transform spawnPoint);
        void RemoveSpawnPoint(Transform spawnPoint);
        void SetSpawnPoints(Transform[] spawnPoints);
        int GetSpawnPointCount();
        
        // Текущее состояние
        int GetActiveCustomerCount();
        
        // Конфигурация внешности
        CharacterAppearanceConfig GetCharacterAppearanceConfig();
        
        // Доступ к префабу клиента
        GameObject GetCustomerPrefab();
        
        // Регистрация восстановленных клиентов (для загрузки сейва)
        void RegisterRestoredCustomer(GameObject customerObj);
        
        // События
        event System.Action<GameObject> OnCustomerSpawned;
        event System.Action<GameObject> OnCustomerLeft;
    }
} 