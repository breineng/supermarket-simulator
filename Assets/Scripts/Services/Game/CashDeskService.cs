using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Supermarket.Interactables;

namespace Supermarket.Services.Game
{
    public class CashDeskService : ICashDeskService
    {
        private List<GameObject> _cashDesks = new List<GameObject>();
        private readonly ICustomerManagerService _customerManagerService;
        
        // Конструктор для внедрения зависимостей (POCO паттерн)
        public CashDeskService(ICustomerManagerService customerManagerService)
        {
            _customerManagerService = customerManagerService;
            Debug.Log("CashDeskService: Created as POCO with ICustomerManagerService dependency");
        }
        
        public void RegisterCashDesk(GameObject cashDesk)
        {
            if (!_cashDesks.Contains(cashDesk))
            {
                bool wasFirstCashDesk = _cashDesks.Count == 0;
                
                _cashDesks.Add(cashDesk);
                var controller = GetCashDeskController(cashDesk);
                string cashDeskId = controller?.GetCashDeskID() ?? "Unknown";
                Debug.Log($"[CustomersDebug] CashDeskService: Registered cash desk '{cashDesk.name}' with ID '{cashDeskId}'. Total: {_cashDesks.Count}");
                
                // Если это первая касса на сцене, ищем потерянных клиентов
                if (wasFirstCashDesk)
                {
                    Debug.Log($"[CashDeskAssignment] CashDeskService: First cash desk registered, checking for lost customers");
                    ReassignLostCustomersToNewCashDesk(cashDesk);
                }
            }
        }
        
        public void UnregisterCashDesk(GameObject cashDesk)
        {
            if (_cashDesks.Remove(cashDesk))
            {
                Debug.Log($"CashDeskService: Unregistered cash desk {cashDesk.name}");
            }
        }
        
        public List<GameObject> GetAllCashDesks()
        {
            // Очищаем список от уничтоженных объектов
            _cashDesks.RemoveAll(cd => cd == null);
            return new List<GameObject>(_cashDesks);
        }
        
        public GameObject FindCashDeskWithShortestQueue()
        {
            _cashDesks.RemoveAll(cd => cd == null);
            
            GameObject bestCashDesk = null;
            int shortestQueue = int.MaxValue;
            
            foreach (var cashDeskObj in _cashDesks)
            {
                var controller = GetCashDeskController(cashDeskObj);
                if (controller != null && controller.IsOpen)
                {
                    int queueLength = controller.QueueLength;
                    if (queueLength < shortestQueue)
                    {
                        shortestQueue = queueLength;
                        bestCashDesk = cashDeskObj;
                    }
                }
            }
            
            return bestCashDesk;
        }

        public GameObject FindCashDeskById(string cashDeskId)
        {
            if (string.IsNullOrEmpty(cashDeskId))
                return null;

            _cashDesks.RemoveAll(cd => cd == null);
            
            Debug.Log($"[CustomersDebug] CashDeskService.FindCashDeskById: Looking for cash desk with ID '{cashDeskId}'. Total registered: {_cashDesks.Count}");

            foreach (var cashDeskObj in _cashDesks)
            {
                var controller = GetCashDeskController(cashDeskObj);
                if (controller != null)
                {
                    string currentId = controller.GetCashDeskID();
                    Debug.Log($"[CustomersDebug] CashDeskService.FindCashDeskById: Found cash desk '{cashDeskObj.name}' with ID '{currentId}'");
                    if (currentId == cashDeskId)
                    {
                        Debug.Log($"[CustomersDebug] CashDeskService.FindCashDeskById: Match found! Returning cash desk '{cashDeskObj.name}'");
                        return cashDeskObj;
                    }
                }
                else
                {
                    Debug.LogWarning($"[CustomersDebug] CashDeskService.FindCashDeskById: Cash desk '{cashDeskObj.name}' has no CashDeskController");
                }
            }

            Debug.LogWarning($"[CustomersDebug] CashDeskService: Cash desk with ID '{cashDeskId}' not found among {_cashDesks.Count} registered cash desks");
            return null;
        }
        
        private CashDeskController GetCashDeskController(GameObject cashDeskObj)
        {
            if (cashDeskObj == null) return null;
            
            var controller = cashDeskObj.GetComponent<CashDeskController>();
            if (controller == null)
                controller = cashDeskObj.GetComponentInParent<CashDeskController>();
            if (controller == null)
                controller = cashDeskObj.GetComponentInChildren<CashDeskController>();
                
            return controller;
        }
        
        /// <summary>
        /// Ищет клиентов без кассы и направляет их к новой кассе (если это первая касса на сцене)
        /// </summary>
        public void ReassignLostCustomersToNewCashDesk(GameObject newCashDesk)
        {
            if (newCashDesk == null)
            {
                Debug.LogWarning("CashDeskService.ReassignLostCustomersToNewCashDesk: New cash desk is null");
                return;
            }
            
            // Проверяем, что это единственная касса на сцене
            _cashDesks.RemoveAll(cd => cd == null);
            if (_cashDesks.Count > 1)
            {
                Debug.Log($"CashDeskService.ReassignLostCustomersToNewCashDesk: Multiple cash desks exist ({_cashDesks.Count}), no need to reassign customers");
                return;
            }
            
            Debug.Log("CashDeskService.ReassignLostCustomersToNewCashDesk: This is the only cash desk, searching for lost customers");
            
            if (_customerManagerService == null)
            {
                Debug.LogError("CashDeskService.ReassignLostCustomersToNewCashDesk: ICustomerManagerService is null, cannot reassign customers");
                return;
            }
            
            // ИСПРАВЛЕНО: Используем ICustomerManagerService вместо FindObjectsOfType
            var allCustomers = GetAllActiveCustomers();
            int reassignedCount = 0;
            
            foreach (var customer in allCustomers)
            {
                if (customer == null) continue;
                
                var customerData = customer.GetCustomerData();
                if (customerData == null) continue;
                
                // Проверяем клиентов в состоянии Shopping с товарами, но без кассы
                bool needsReassignment = false;
                string reassignReason = "";
                
                if (customerData.CurrentState == CustomerState.Shopping && customer.HasItemsInCart() && customer.GetCashDeskId() == null)
                {
                    needsReassignment = true;
                    reassignReason = "shopping with items but no target cash desk";
                }
                else if ((customerData.CurrentState == CustomerState.GoingToCashier || 
                         customerData.CurrentState == CustomerState.JoiningQueue || 
                         customerData.CurrentState == CustomerState.WaitingInQueue) && 
                         customer.GetCashDeskId() == null)
                {
                    needsReassignment = true;
                    reassignReason = "going to/waiting at non-existent cash desk";
                }
                
                if (needsReassignment)
                {
                    Debug.Log($"CashDeskService.ReassignLostCustomersToNewCashDesk: Reassigning customer {customerData.CustomerName} (reason: {reassignReason})");
                    
                    // Уведомляем клиента о новой кассе
                    customer.OnNewCashDeskAvailable();
                    reassignedCount++;
                }
            }
            
            Debug.Log($"CashDeskService.ReassignLostCustomersToNewCashDesk: Reassigned {reassignedCount} customers to new cash desk");
        }
        
        /// <summary>
        /// Получает всех активных клиентов через ICustomerManagerService
        /// </summary>
        private List<CustomerController> GetAllActiveCustomers()
        {
            var allCustomers = new List<CustomerController>();
            
            // Получаем данные всех клиентов для сохранения (это даст нам доступ к активным клиентам)
            var customerSaveData = _customerManagerService.GetCustomersSaveData();
            
            // Для каждого сохраненного клиента находим соответствующий CustomerController
            foreach (var saveData in customerSaveData)
            {
                var customer = _customerManagerService.FindCustomerByName(saveData.CustomerName);
                if (customer != null)
                {
                    allCustomers.Add(customer);
                }
            }
            
            return allCustomers;
        }
    }
} 