using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Supermarket.Data;
using Supermarket.Interactables;
using Supermarket.Components;

namespace Supermarket.Services.Game
{
    public class CustomerManagerService : ICustomerManagerService
    {
        private readonly IProductCatalogService _productCatalogService;
        private readonly ICustomerSpawnerService _customerSpawnerService;
        
        // Список всех активных покупателей
        private readonly List<CustomerController> _activeCustomers = new List<CustomerController>();
        
        // Конструктор для внедрения зависимостей (POCO паттерн)
        public CustomerManagerService(IProductCatalogService productCatalogService, ICustomerSpawnerService customerSpawnerService)
        {
            _productCatalogService = productCatalogService;
            _customerSpawnerService = customerSpawnerService;
            Debug.Log("CustomerManagerService: Created as POCO with dependencies");
        }
        
        public void RegisterCustomer(CustomerController customer)
        {
            if (customer == null)
            {
                Debug.LogWarning("CustomerManagerService: Attempted to register null customer");
                return;
            }
            
            if (!_activeCustomers.Contains(customer))
            {
                _activeCustomers.Add(customer);
                Debug.Log($"CustomerManagerService: Registered customer '{customer.GetCustomerData()?.CustomerName}'. Total active customers: {_activeCustomers.Count}");
            }
        }
        
        public void UnregisterCustomer(CustomerController customer)
        {
            if (customer == null) return;
            
            if (_activeCustomers.Remove(customer))
            {
                string customerName = customer.GetCustomerData()?.CustomerName ?? "Unknown";
                Debug.Log($"CustomerManagerService: Unregistered customer '{customerName}'. Total active customers: {_activeCustomers.Count}");
            }
        }
        
        public List<CustomerSaveData> GetCustomersSaveData()
        {
            List<CustomerSaveData> customersData = new List<CustomerSaveData>();
            
            // Убираем null ссылки
            _activeCustomers.RemoveAll(c => c == null);
            
            for (int i = 0; i < _activeCustomers.Count; i++)
            {
                var customer = _activeCustomers[i];
                if (customer == null) continue;
                
                var customerData = customer.GetCustomerData();
                if (customerData == null) continue;
                
                // Создаем данные для сохранения
                CustomerSaveData saveData = ConvertToSaveData(customer, customerData);
                customersData.Add(saveData);
                
                Debug.Log($"CustomerManagerService: Collected save data for customer '{customerData.CustomerName}' in state {customerData.CurrentState}");
            }
            
            Debug.Log($"CustomerManagerService: Collected {customersData.Count} customers for saving");
            return customersData;
        }
        
        public void RestoreCustomers(List<CustomerSaveData> customersData)
        {
            if (customersData == null || customersData.Count == 0)
            {
                Debug.Log("CustomerManagerService: No customers data to restore");
                return;
            }
            
            // КРИТИЧЕСКИ ВАЖНО: Очищаем старых клиентов перед загрузкой новых
            Debug.Log($"CustomerManagerService: Clearing {_activeCustomers.Count} existing customers before restoration");
            ClearAllCustomers();
            
            Debug.Log($"CustomerManagerService: Restoring {customersData.Count} customers");
            
            foreach (var saveData in customersData)
            {
                RestoreCustomer(saveData);
            }
            
            // Проверяем синхронизацию количества клиентов
            int managerCount = GetActiveCustomerCount();
            int spawnerCount = _customerSpawnerService?.GetActiveCustomerCount() ?? 0;
            
            Debug.Log($"CustomerManagerService: Completed customers restoration. Manager count: {managerCount}, Spawner count: {spawnerCount}");
            
            if (managerCount != spawnerCount)
            {
                Debug.LogWarning($"CustomerManagerService: Customer count mismatch! Manager: {managerCount}, Spawner: {spawnerCount}");
            }
        }
        
        public void ClearAllCustomers()
        {
            Debug.Log($"CustomerManagerService: Clearing {_activeCustomers.Count} active customers");
            
            int spawnerCountBefore = _customerSpawnerService?.GetActiveCustomerCount() ?? 0;
            Debug.Log($"CustomerManagerService: CustomerSpawnerService count before clearing: {spawnerCountBefore}");
            
            // Уничтожаем всех покупателей
            foreach (var customer in _activeCustomers.ToList())
            {
                if (customer != null && customer.gameObject != null)
                {
                    Object.Destroy(customer.gameObject);
                }
            }
            
            _activeCustomers.Clear();
            
            // Проверяем, что CustomerSpawnerService тоже очистился
            int spawnerCountAfter = _customerSpawnerService?.GetActiveCustomerCount() ?? 0;
            Debug.Log($"CustomerManagerService: All customers cleared. Manager count: {_activeCustomers.Count}, Spawner count: {spawnerCountAfter}");
            
            if (spawnerCountAfter > 0)
            {
                Debug.LogWarning($"CustomerManagerService: CustomerSpawnerService still has {spawnerCountAfter} customers after clearing!");
            }
        }
        
        public int GetActiveCustomerCount()
        {
            _activeCustomers.RemoveAll(c => c == null);
            return _activeCustomers.Count;
        }
        
        public CustomerController FindCustomerByName(string name)
        {
            return _activeCustomers.FirstOrDefault(c => c != null && c.GetCustomerData()?.CustomerName == name);
        }
        
        /// <summary>
        /// Конвертирует CustomerController и CustomerData в CustomerSaveData
        /// </summary>
        private CustomerSaveData ConvertToSaveData(CustomerController controller, CustomerData customerData)
        {
            var isInQueue = GetIsInQueue(controller);
            var queuePosition = GetQueuePosition(controller);
            var cashDeskId = GetCashDeskId(controller);
            
            Debug.Log($"[CustomersDebug] CustomerManagerService.ConvertToSaveData: Customer {customerData.CustomerName} - IsInQueue: {isInQueue}, QueuePosition: {queuePosition}, CashDeskId: {cashDeskId}");
            
            var saveData = new CustomerSaveData
            {
                // Базовая информация
                CustomerName = customerData.CustomerName,
                Position = controller.transform.position,
                Rotation = controller.transform.eulerAngles,
                Money = customerData.Money,
                
                // Внешность
                Gender = (int)customerData.Gender,
                TopClothingColor = ColorToArray(customerData.TopClothingColor),
                BottomClothingColor = ColorToArray(customerData.BottomClothingColor),
                ShoesColor = ColorToArray(customerData.ShoesColor),
                
                // Состояние
                CurrentState = (int)customerData.CurrentState,
                StateTimer = GetStateTimer(controller),
                
                // Список покупок
                ShoppingList = ConvertShoppingList(customerData.ShoppingList),
                
                // Данные очереди и целей (попробуем получить через рефлексию или публичные методы)
                IsInQueue = isInQueue,
                QueuePosition = queuePosition,
                CashDeskId = cashDeskId,
                QueueWorldPosition = GetQueueWorldPosition(controller),
                
                // Флаги (если доступны)
                PickupAnimationPlayed = GetPickupAnimationPlayed(controller),
                PayAnimationPlayed = GetPayAnimationPlayed(controller),
                PaymentProcessed = GetPaymentProcessed(controller),
                
                // Текущие цели
                TargetShelfId = GetTargetShelfId(controller),
                CurrentShoppingItemIndex = GetCurrentShoppingItemIndex(controller),
                
                // Данные для состояния выхода
                PersonalExitPosition = GetPersonalExitPosition(controller),
                HasPersonalExitPosition = GetHasPersonalExitPosition(controller),
                
                // Приоритет избегания
                AvoidancePriority = GetAvoidancePriority(controller),
                
                // Данные для уличных прогулок
                CurrentWaypointId = GetCurrentWaypointId(controller),
                WaypointWaitTimer = GetWaypointWaitTimer(controller),
                IsWaitingAtWaypoint = GetIsWaitingAtWaypoint(controller)
            };
            
            return saveData;
        }
        
        /// <summary>
        /// Восстанавливает одного покупателя из сохраненных данных
        /// </summary>
        private void RestoreCustomer(CustomerSaveData saveData)
        {
            try
            {
                // Получаем prefab покупателя от CustomerSpawnerService
                GameObject customerPrefab = GetCustomerPrefab();
                if (customerPrefab == null)
                {
                    Debug.LogError($"CustomerManagerService: Cannot restore customer '{saveData.CustomerName}' - no customer prefab available");
                    return;
                }
                
                // Создаем объект покупателя
                GameObject customerObj = Object.Instantiate(customerPrefab, saveData.Position, Quaternion.Euler(saveData.Rotation));
                
                // Восстанавливаем CustomerData
                CustomerData customerData = RestoreCustomerData(saveData);
                
                // Применяем внешность
                RestoreAppearance(customerObj, saveData, customerData);
                
                // Инициализируем контроллер
                CustomerController customerController = customerObj.GetComponent<CustomerController>();
                if (customerController != null)
                {
                    // Регистрируем в менеджере до инициализации
                    RegisterCustomer(customerController);
                    
                    // КРИТИЧЕСКИ ВАЖНО: Регистрируем в CustomerSpawnerService для подсчета активных клиентов
                    _customerSpawnerService?.RegisterRestoredCustomer(customerObj);
                    
                    // Инициализируем с восстановленными данными БЕЗ смены состояния
                    customerController.InitializeRestored(customerData);
                    
                    // Восстанавливаем внутреннее состояние контроллера
                    RestoreControllerState(customerController, saveData);
                    
                    // КРИТИЧЕСКИ ВАЖНО: Инициализируем состояние ПОСЛЕ восстановления всех данных
                    customerController.InitializeRestoredState();
                    
                    // Восстанавливаем приоритет avoidance
                    CustomerLocomotion locomotion = customerObj.GetComponent<CustomerLocomotion>();
                    if (locomotion != null)
                    {
                        locomotion.SetAvoidancePriority(saveData.AvoidancePriority);
                    }
                    
                    // Подписываемся на событие ухода
                    customerController.OnCustomerLeaving += HandleCustomerLeaving;
                    
                    Debug.Log($"CustomerManagerService: Restored customer '{saveData.CustomerName}' at {saveData.Position} in state {(CustomerState)saveData.CurrentState}");
                }
                else
                {
                    Debug.LogError($"CustomerManagerService: CustomerController not found on restored customer '{saveData.CustomerName}'");
                    Object.Destroy(customerObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CustomerManagerService: Failed to restore customer '{saveData.CustomerName}': {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Получает prefab покупателя от CustomerSpawnerService
        /// </summary>
        private GameObject GetCustomerPrefab()
        {
            // Используем публичный метод интерфейса вместо рефлексии
            if (_customerSpawnerService != null)
            {
                return _customerSpawnerService.GetCustomerPrefab();
            }
            
            Debug.LogWarning("CustomerManagerService: Could not get customer prefab - spawner service is null");
            return null;
        }
        
        /// <summary>
        /// Восстанавливает CustomerData из сохраненных данных
        /// </summary>
        private CustomerData RestoreCustomerData(CustomerSaveData saveData)
        {
            CustomerData customerData = new CustomerData(saveData.CustomerName, saveData.Money);
            customerData.Gender = (CharacterAppearanceConfig.Gender)saveData.Gender;
            customerData.TopClothingColor = ArrayToColor(saveData.TopClothingColor);
            customerData.BottomClothingColor = ArrayToColor(saveData.BottomClothingColor);
            customerData.ShoesColor = ArrayToColor(saveData.ShoesColor);
            customerData.CurrentState = (CustomerState)saveData.CurrentState;
            
            // Восстанавливаем список покупок
            customerData.ShoppingList = RestoreShoppingList(saveData.ShoppingList);
            
            return customerData;
        }
        
        /// <summary>
        /// Восстанавливает внешность покупателя
        /// </summary>
        private void RestoreAppearance(GameObject customerObj, CustomerSaveData saveData, CustomerData customerData)
        {
            Debug.Log($"CustomerManagerService: Restoring appearance for '{saveData.CustomerName}' with gender {customerData.Gender}");
            
            CharacterAppearance appearance = customerObj.GetComponent<CharacterAppearance>();
            if (appearance == null)
            {
                appearance = customerObj.AddComponent<CharacterAppearance>();
                Debug.Log($"CustomerManagerService: Added CharacterAppearance component to '{saveData.CustomerName}'");
            }
            
            // Получаем модель по полу (упрощенно, без точного восстановления модели)
            CharacterAppearanceConfig.GenderModel genderModel = GetGenderModel(customerData.Gender);
            
            if (appearance != null && genderModel != null)
            {
                Debug.Log($"CustomerManagerService: Applying appearance for '{saveData.CustomerName}' with model prefab: {genderModel.modelPrefab?.name ?? "NULL"}");
                appearance.ApplyAppearance(genderModel, customerData);
                
                // Обновляем ссылку на Animator
                CustomerLocomotion locomotion = customerObj.GetComponent<CustomerLocomotion>();
                if (locomotion != null)
                {
                    locomotion.UpdateAnimatorReference();
                    Debug.Log($"CustomerManagerService: Updated Animator reference for '{saveData.CustomerName}'");
                }
                else
                {
                    Debug.LogWarning($"CustomerManagerService: CustomerLocomotion not found on '{saveData.CustomerName}'");
                }
            }
            else
            {
                Debug.LogError($"CustomerManagerService: Failed to restore appearance for '{saveData.CustomerName}' - Appearance: {appearance != null}, GenderModel: {genderModel != null}");
                if (genderModel != null && genderModel.modelPrefab == null)
                {
                    Debug.LogError($"CustomerManagerService: GenderModel found but modelPrefab is null for gender {customerData.Gender}");
                }
            }
        }
        
        /// <summary>
        /// Получает модель персонажа по полу (упрощенная версия)
        /// </summary>
        private CharacterAppearanceConfig.GenderModel GetGenderModel(CharacterAppearanceConfig.Gender gender)
        {
            // Получаем CharacterAppearanceConfig от CustomerSpawnerService
            var appearanceConfig = _customerSpawnerService?.GetCharacterAppearanceConfig();
            if (appearanceConfig == null)
            {
                Debug.LogWarning("CustomerManagerService: CharacterAppearanceConfig is null, cannot restore customer appearance");
                return null;
            }
            
            Debug.Log($"CustomerManagerService: Looking for gender model {gender} in config with {appearanceConfig.genderModels?.Length ?? 0} models");
            
            // Ищем модель с подходящим полом
            var genderModels = appearanceConfig.genderModels;
            if (genderModels == null || genderModels.Length == 0)
            {
                Debug.LogWarning("CustomerManagerService: No gender models found in CharacterAppearanceConfig");
                return null;
            }
            
            foreach (var model in genderModels)
            {
                Debug.Log($"CustomerManagerService: Checking model with gender {model.gender}, prefab: {model.modelPrefab?.name ?? "NULL"}");
                if (model.gender == gender)
                {
                    Debug.Log($"CustomerManagerService: Found matching gender model for {gender}, prefab: {model.modelPrefab?.name ?? "NULL"}");
                    return model;
                }
            }
            
            // Если не нашли точное соответствие, возвращаем первую доступную модель
            Debug.LogWarning($"CustomerManagerService: Gender model for {gender} not found, using first available model: {genderModels[0].modelPrefab?.name ?? "NULL"}");
            return genderModels[0];
        }
        
        /// <summary>
        /// Восстанавливает внутреннее состояние CustomerController
        /// </summary>
        private void RestoreControllerState(CustomerController controller, CustomerSaveData saveData)
        {
            // Используем новый метод CustomerController для восстановления состояния
            controller.RestoreState(saveData);
        }
        
        /// <summary>
        /// Обработчик ухода покупателя
        /// </summary>
        private void HandleCustomerLeaving(GameObject customerObj)
        {
            var customer = customerObj.GetComponent<CustomerController>();
            if (customer != null)
            {
                UnregisterCustomer(customer);
            }
        }
        
        // Utility методы для конвертации данных
        private float[] ColorToArray(Color color)
        {
            return new float[] { color.r, color.g, color.b, color.a };
        }
        
        private Color ArrayToColor(float[] array)
        {
            if (array == null || array.Length < 4) return Color.white;
            return new Color(array[0], array[1], array[2], array[3]);
        }
        
        private List<ShoppingItemSaveData> ConvertShoppingList(List<ShoppingItem> shoppingList)
        {
            var result = new List<ShoppingItemSaveData>();
            foreach (var item in shoppingList)
            {
                result.Add(new ShoppingItemSaveData
                {
                    ProductId = item.Product?.ProductID ?? "",
                    DesiredQuantity = item.DesiredQuantity,
                    CollectedQuantity = item.CollectedQuantity
                });
            }
            return result;
        }
        
        private List<ShoppingItem> RestoreShoppingList(List<ShoppingItemSaveData> saveList)
        {
            var result = new List<ShoppingItem>();
            if (saveList == null) return result;
            
            foreach (var saveItem in saveList)
            {
                var product = _productCatalogService?.GetProductConfigByID(saveItem.ProductId);
                if (product != null)
                {
                    var shoppingItem = new ShoppingItem(product, saveItem.DesiredQuantity);
                    shoppingItem.CollectedQuantity = saveItem.CollectedQuantity;
                    result.Add(shoppingItem);
                }
                else
                {
                    Debug.LogWarning($"CustomerManagerService: Product '{saveItem.ProductId}' not found when restoring shopping list");
                }
            }
            return result;
        }
        
        // Методы для получения приватных данных контроллера (через рефлексию или расширение API)
        // Теперь используем публичные методы CustomerController
        
        private float GetStateTimer(CustomerController controller) => controller.GetStateTimer();
        private bool GetIsInQueue(CustomerController controller) => controller.GetIsInQueue();
        private int GetQueuePosition(CustomerController controller) => controller.GetQueuePositionIndex();
        private string GetCashDeskId(CustomerController controller) => controller.GetCashDeskId();
        private Vector3 GetQueueWorldPosition(CustomerController controller) => controller.GetQueuePosition();
        private bool GetPickupAnimationPlayed(CustomerController controller) => controller.GetPickupAnimationPlayed();
        private bool GetPayAnimationPlayed(CustomerController controller) => controller.GetPayAnimationPlayed();
        private bool GetPaymentProcessed(CustomerController controller) => controller.GetPaymentProcessed();
        private string GetTargetShelfId(CustomerController controller) => controller.GetTargetShelfId();
        private int GetCurrentShoppingItemIndex(CustomerController controller) => controller.GetCurrentShoppingItemIndex();
        private Vector3 GetPersonalExitPosition(CustomerController controller) => controller.GetPersonalExitPosition();
        private bool GetHasPersonalExitPosition(CustomerController controller) => controller.HasPersonalExitPosition();
        
        private int GetAvoidancePriority(CustomerController controller)
        {
            var locomotion = controller.GetComponent<CustomerLocomotion>();
            return locomotion != null ? locomotion.GetAvoidancePriority() : 50;
        }
        
        // Новые методы для данных уличных прогулок
        private string GetCurrentWaypointId(CustomerController controller) => controller.GetCurrentWaypointId();
        private float GetWaypointWaitTimer(CustomerController controller) => controller.GetWaypointWaitTimer();
        private bool GetIsWaitingAtWaypoint(CustomerController controller) => controller.GetIsWaitingAtWaypoint();
    }
} 