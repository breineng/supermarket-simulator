using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviourInject;
using Core.Interfaces;
using Core.Models;
using Supermarket.Services.Game;
using Supermarket.Components;
using UnityEngine.UIElements;

namespace Supermarket.Interactables
{
    public class CashDeskController : MonoBehaviour, IInteractable
    {
        [Inject]
        public IPlayerDataService _playerDataService;
        
        [Inject]
        public IInputModeService _inputModeService;
        
        [Inject]
        public ICashDeskService _cashDeskService;
        
        [Inject]
        public IStatsService _statsService;
        
        [Inject]
        public IRetailPriceService _retailPriceService;
        
        [Header("Cash Desk Configuration")]
        [SerializeField] private string _cashDeskID = "CashDesk_01";
        [SerializeField] private Transform _queueStartPoint;
        [SerializeField] private float _queueSpacing = 2.0f;
        [SerializeField] private float _processingTimePerItem = 0.5f;
        [SerializeField] private float _paymentProcessingTime = 2f;
        [SerializeField] private Transform _playerPosition; // Точка, где должен стоять игрок при работе на кассе
        [SerializeField] private float _moveSpeed = 3f; // Скорость перемещения к кассе
        [SerializeField] private float _arrivalThreshold = 0.1f; // Расстояние считающееся "прибытием"
        
        [Header("Item Placement")]
        [SerializeField] private Transform _itemPlacementPoint; // Точка начала выкладки товаров
        [SerializeField] private Vector2 _itemSpacing = new Vector2(0.3f, 0.4f); // Расстояние между товарами по X и Z
        [SerializeField] private Vector3 _itemRotation = new Vector3(0, 90, 0); // Поворот для каждого товара
        [SerializeField] private int _gridColumns = 7; // Количество колонок в сетке
        [SerializeField] private int _gridRows = 3; // Количество рядов в сетке
        
        [Header("Manual Scanning")]
        [SerializeField] private Transform _scannerPoint; // Точка, над которой пролетает товар
        [SerializeField] private Transform _collectionPoint; // Точка, куда летят отсканированные товары
        [SerializeField] private float _itemFlySpeed = 5f;
        [SerializeField] private LayerMask _scannableLayer;

        [Header("UI")]
        [SerializeField] private CashDeskUIHandler _uiHandler;
        
        private List<ProductConfig> _scannedProductConfigs = new List<ProductConfig>();
        private List<GameObject> _placedItems = new List<GameObject>(); // Список выложенных товаров
        private List<GameObject> _scannedItems = new List<GameObject>(); // Список отсканированных товаров
        private float _runningTotal = 0f; // Текущая сумма для покупателя
        
        private CashDeskData _cashDeskData;
        private bool _isProcessingCustomer = false;
        private bool _isPlayerOperating = false;
        private bool _isMovingToPosition = false; // Флаг перемещения к позиции
        private Transform _playerTransform; // Ссылка на трансформ игрока
        private bool _isOperationInProgress = false; // Флаг для предотвращения race conditions
        
        private OutlineController _hoveredOutlineController;
        private Camera _mainCamera;
        private CashDeskInputController _activeInputController;
        
        // Словарь для отслеживания позиций покупателей в очереди
        private Dictionary<GameObject, int> _queuePositions = new Dictionary<GameObject, int>();
        
        // Список покупателей, идущих к кассе (но еще не в очереди)
        private List<GameObject> _customersApproaching = new List<GameObject>();
        
        // Объект для синхронизации доступа к очереди
        private readonly object _queueLock = new object();
        
        // События
        public event System.Action<GameObject> OnCustomerJoinedQueue;
        public event System.Action<GameObject> OnCustomerLeftQueue;
        public event System.Action<float> OnTransactionCompleted;

        // UI Events
        public event System.Action<ProductConfig> OnItemScanned;
        public event System.Action<float> OnTotalUpdated;
        public event System.Action OnTransactionFinalized;
        public event System.Action OnOperationStarted;
        
        public bool IsPlayerOperating => _isPlayerOperating;
        
        void Awake()
        {
            _cashDeskData = new CashDeskData(_cashDeskID);
            _mainCamera = Camera.main;
        }
        
        void Start()
        {
            if (_queueStartPoint == null)
            {
                // Если не указана точка очереди, используем позицию перед кассой
                GameObject queuePoint = new GameObject("QueueStartPoint");
                queuePoint.transform.parent = transform;
                queuePoint.transform.localPosition = new Vector3(0, 0, -2);
                _queueStartPoint = queuePoint.transform;
            }
            
            // Регистрируем кассу в сервисе
            _cashDeskService?.RegisterCashDesk(gameObject);
            
            // Открываем кассу по умолчанию
            OpenCashDesk();
        }
        
        void OnDestroy()
        {
            // Отменяем регистрацию при уничтожении
            _cashDeskService?.UnregisterCashDesk(gameObject);
            
            // Очищаем словарь позиций
            _queuePositions.Clear();
            _customersApproaching.Clear();
        }
        
        // IInteractable implementation
        public InteractionPromptData GetInteractionPrompt()
        {
            if (_isPlayerOperating) 
            {
                // If player is hovering over an item, don't show cash desk prompt (item prompt has priority)
                if (_hoveredOutlineController != null)
                {
                    Debug.Log("CashDeskController.GetInteractionPrompt: Player is operating AND hovering over item, returning Empty");
                    return InteractionPromptData.Empty;
                }
                Debug.Log("CashDeskController.GetInteractionPrompt: Player is operating, returning Space prompt");
                return new InteractionPromptData("Нажмите [Space] чтобы завершить обслуживание", PromptType.Complete);
            }
            if (_cashDeskData.IsOpen)
            {
                string status = _cashDeskData.CurrentCustomer != null ? "обслуживается клиент" : "свободна";
                return new InteractionPromptData($"управлять кассой ({status})", PromptType.RawAction);
            }
            else
            {
                return new InteractionPromptData("касса закрыта", PromptType.Complete);
            }
        }
        
        public void Interact(GameObject interactor)
        {
            if (!_cashDeskData.IsOpen) return;
            if (_isPlayerOperating || _isOperationInProgress) return;
            StartPlayerOperation(interactor);
        }
        
        public void OnFocus()
        {
            // Визуальная подсветка при фокусе
        }
        
        public void OnBlur()
        {
            // Убрать подсветку
        }
        
        // Управление кассой игроком
        private void StartPlayerOperation(GameObject interactor)
        {
            if (_isOperationInProgress || _isPlayerOperating) return;
            _isOperationInProgress = true;
            
            Debug.Log($"[CustomersDebug] CashDeskController.StartPlayerOperation: Player starting operation. Queue count: {_cashDeskData.CustomerQueue.Count}, Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}");
            
            _playerTransform = interactor.transform;
            _isPlayerOperating = true;
            
            if (_playerPosition != null)
            {
                _inputModeService?.SetInputMode(InputMode.MovingToCashDesk);
                _isMovingToPosition = true;
                StartCoroutine(MovePlayerToPosition());
            }
            else
            {
                _inputModeService?.SetInputMode(InputMode.CashDeskOperation);
            }
            
            _activeInputController = interactor.GetComponent<CashDeskInputController>();
            if (_activeInputController != null)
            {
                _activeInputController.SetCurrentCashDesk(this);
            }
            else
            {
                Debug.LogWarning("CashDeskController: CashDeskInputController not found on the interactor!");
            }
            
            _uiHandler?.Show(this);
            OnOperationStarted?.Invoke();
            
            if (!_isProcessingCustomer && _cashDeskData.CurrentCustomer == null && _cashDeskData.CustomerQueue.Count > 0)
            {
                Debug.Log($"[CustomersDebug] CashDeskController.StartPlayerOperation: Starting to process next customer from queue");
                StartCoroutine(ProcessNextCustomer());
            }
            else
            {
                Debug.Log($"[CustomersDebug] CashDeskController.StartPlayerOperation: Not processing customer. Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}, Queue count: {_cashDeskData.CustomerQueue.Count}");
            }
            _isOperationInProgress = false;
        }
        
        private IEnumerator MovePlayerToPosition()
        {
            if (_playerTransform == null || _playerPosition == null)
            {
                _isMovingToPosition = false;
                yield break;
            }

            Vector3 startPosition = _playerTransform.position;
            Vector3 targetPosition = _playerPosition.position;
            
            // Keep the player's Y position to avoid floating/sinking
            targetPosition.y = startPosition.y;
            
            float distance = Vector3.Distance(startPosition, targetPosition);
            
            // If already close enough, no need to move
            if (distance <= _arrivalThreshold)
            {
                _isMovingToPosition = false;
                // Only switch to CashDeskOperation if we're still operating
                if (_isPlayerOperating)
                {
                    _inputModeService?.SetInputMode(InputMode.CashDeskOperation);
                }
                yield break;
            }
            
            float journeyTime = distance / _moveSpeed;
            float elapsedTime = 0;
            
            // Also rotate player to face the cash desk direction if needed
            Vector3 targetDirection = (_playerPosition.forward).normalized;
            Quaternion startRotation = _playerTransform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            
            while (elapsedTime < journeyTime)
            {
                // Check if operation was cancelled or movement stopped
                if (!_isMovingToPosition || !_isPlayerOperating)
                {
                    _isMovingToPosition = false;
                    yield break;
                }
                    
                elapsedTime += Time.deltaTime;
                float fractionOfJourney = elapsedTime / journeyTime;
                
                // Smooth movement
                if (_playerTransform != null) // Additional null check
                {
                    _playerTransform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
                    _playerTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, fractionOfJourney);
                }
                
                yield return null;
            }
            
            // Ensure final position and rotation only if still operating
            if (_isPlayerOperating && _playerTransform != null)
            {
                _playerTransform.position = targetPosition;
                _playerTransform.rotation = targetRotation;
            }
            
            _isMovingToPosition = false;
            
            // Switch to cash desk operation mode after movement is complete, only if still operating
            if (_isPlayerOperating)
            {
                _inputModeService?.SetInputMode(InputMode.CashDeskOperation);
            }
        }
        
        public void StopPlayerOperation()
        {
            if (_isOperationInProgress || !_isPlayerOperating) return;
            
            _isOperationInProgress = true;
            _isPlayerOperating = false;
            _isMovingToPosition = false;
            
            // Clear hovered item outline
            if (_hoveredOutlineController != null)
            {
                _hoveredOutlineController.IsOutlineEnabled = false;
                _hoveredOutlineController = null;
            }
            
            if(_activeInputController != null)
            {
                _activeInputController.SetCurrentCashDesk(null);
                _activeInputController = null;
            }

            _inputModeService?.SetInputMode(InputMode.Game);
            _uiHandler?.Hide();
            
            _isOperationInProgress = false;
        }
        
        // Управление очередью
        public bool TryJoinQueue(GameObject customer)
        {
            if (!_cashDeskData.IsOpen) return false;
            
            lock (_queueLock)
            {
                if (!_cashDeskData.CustomerQueue.Contains(customer))
                {
                    _cashDeskData.CustomerQueue.Enqueue(customer);
                    OnCustomerJoinedQueue?.Invoke(customer);
                    
                    // Удаляем из списка подходящих
                    _customersApproaching.Remove(customer);
                    
                    // Обновляем позицию в очереди
                    UpdateQueuePositions();
                    
                    Debug.Log($"[CustomersDebug] CashDeskController.TryJoinQueue: Customer {customer.name} joined queue. Player operating: {_isPlayerOperating}, Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}, Queue count: {_cashDeskData.CustomerQueue.Count}");
                    
                    // Если касса свободна и игрок работает, начинаем обслуживание
                    if (_isPlayerOperating && !_isProcessingCustomer && _cashDeskData.CurrentCustomer == null)
                    {
                        Debug.Log($"[CustomersDebug] CashDeskController.TryJoinQueue: Starting service for new customer {customer.name} (queue was empty)");
                        StartCoroutine(ProcessNextCustomer());
                    }
                    else
                    {
                        Debug.Log($"[CustomersDebug] CashDeskController.TryJoinQueue: Customer {customer.name} joined queue but service not started. Player operating: {_isPlayerOperating}, Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}");
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
        
        public void LeaveQueue(GameObject customer)
        {
            lock (_queueLock)
            {
                if (_cashDeskData.CustomerQueue.Contains(customer))
                {
                    // Создаем новую очередь без этого покупателя
                    var newQueue = new Queue<GameObject>(_cashDeskData.CustomerQueue.Where(c => c != customer));
                    _cashDeskData.CustomerQueue = newQueue;
                    
                    // Удаляем из словаря позиций
                    _queuePositions.Remove(customer);
                    
                    OnCustomerLeftQueue?.Invoke(customer);
                    UpdateQueuePositions();
                }
            }
        }
        
        public Vector3 GetQueuePosition(GameObject customer)
        {
            lock (_queueLock)
            {
                // Проверяем, есть ли покупатель в словаре позиций
                if (_queuePositions.ContainsKey(customer))
                {
                    int position = _queuePositions[customer];
                    return _queueStartPoint.position + _queueStartPoint.forward * (position * _queueSpacing);
                }
                
                // Если покупателя нет в очереди, возвращаем позицию за последним в очереди
                int lastPosition = _cashDeskData.CustomerQueue.Count;
                return _queueStartPoint.position + _queueStartPoint.forward * (lastPosition * _queueSpacing);
            }
        }
        
        // Получить позицию конца очереди (куда должны идти новые покупатели)
        public Vector3 GetEndOfQueuePosition()
        {
            lock (_queueLock)
            {
                // Учитываем и тех, кто в очереди, и тех, кто идет к ней
                int totalPositions = _cashDeskData.CustomerQueue.Count + _customersApproaching.Count;
                // Добавляем дополнительное место в конце для подхода
                return _queueStartPoint.position + _queueStartPoint.forward * ((totalPositions + 0.5f) * _queueSpacing);
            }
        }
        
        // Получить направление очереди (для выравнивания покупателей)
        public Vector3 GetQueueDirection()
        {
            return _queueStartPoint.forward;
        }
        
        public int GetQueuePosition(int customerPosition)
        {
            return customerPosition;
        }
        
        // Обработка покупателей
        private IEnumerator ProcessNextCustomer()
        {
            Debug.Log($"[CustomersDebug] CashDeskController.ProcessNextCustomer: Starting ProcessNextCustomer. Player operating: {_isPlayerOperating}, Queue count: {_cashDeskData.CustomerQueue.Count}");
            
            if (!_isPlayerOperating || _cashDeskData.CustomerQueue.Count == 0)
            {
                Debug.Log($"[CustomersDebug] CashDeskController.ProcessNextCustomer: Aborting - Player operating: {_isPlayerOperating}, Queue count: {_cashDeskData.CustomerQueue.Count}");
                _isProcessingCustomer = false;
                yield break;
            }
            
            _isProcessingCustomer = true;
            
            lock (_queueLock)
            {
                if (_cashDeskData.CustomerQueue.Count > 0)
                {
                    _cashDeskData.CurrentCustomer = _cashDeskData.CustomerQueue.Peek();
                }
            }
            
            if (_cashDeskData.CurrentCustomer == null)
            {
                Debug.Log($"[CustomersDebug] CashDeskController.ProcessNextCustomer: No current customer available");
                _isProcessingCustomer = false;
                yield break;
            }
            
            CustomerController customerController = _cashDeskData.CurrentCustomer.GetComponent<CustomerController>();
            if (customerController == null)
            {
                Debug.LogError("[CustomersDebug] Customer without CustomerController!");
                _cashDeskData.CurrentCustomer = null;
                _isProcessingCustomer = false;
                yield break;
            }
            
            Debug.Log($"[CustomersDebug] CashDeskController.ProcessNextCustomer: Starting payment process for customer {customerController.GetCustomerData().CustomerName}");
            customerController.StartPaymentProcess(this);
            
            yield return new WaitUntil(() => customerController.GetCustomerData().CurrentState == CustomerState.Paying);
            
            Debug.Log($"[CustomersDebug] CashDeskController.ProcessNextCustomer: Customer {customerController.GetCustomerData().CustomerName} reached Paying state");
            
            // Ждем, пока игрок отсканирует все товары
            yield return new WaitUntil(() => _placedItems.Count == 0 && _isPlayerOperating);

            // Если игрок вышел, не завершив, прерываем
            if (!_isPlayerOperating)
            {
                Debug.Log("[CustomersDebug] Player left before finishing scanning.");
                _isProcessingCustomer = false;
                yield break;
            }

            yield return new WaitForSeconds(_paymentProcessingTime);
            
            if (_playerDataService != null)
            {
                _playerDataService.AdjustMoney(_runningTotal);
                Debug.Log($"[CustomersDebug] Transaction completed: {customerController.GetCustomerData().CustomerName} paid ${_runningTotal:F2}");
            }
            
            if (_statsService != null)
            {
                // Записываем каждый проданный товар
                _statsService.RecordSale("Manual Scan", _scannedItems.Count, _runningTotal);
                _statsService.RecordCustomerServed();
            }
            
            _cashDeskData.CustomersServed++;
            _cashDeskData.TotalRevenue += _runningTotal;
            
            customerController.CompletePayment();
            
            OnTransactionCompleted?.Invoke(_runningTotal);
            
            lock (_queueLock)
            {
                if (_cashDeskData.CustomerQueue.Count > 0 && _cashDeskData.CustomerQueue.Peek() == _cashDeskData.CurrentCustomer)
                {
                    _cashDeskData.CustomerQueue.Dequeue();
                    UpdateQueuePositions();
                }
            }

            ClearBelt();
            
            _cashDeskData.CurrentCustomer = null;
            _isProcessingCustomer = false;

            FinishTransaction();
            
            // Проверяем очередь с небольшой задержкой, чтобы дать время новым покупателям присоединиться
            yield return new WaitForSeconds(0.1f);
            
            // Если есть еще клиенты в очереди, начинаем обслуживание следующего
            if (_isPlayerOperating && _cashDeskData.CustomerQueue.Count > 0)
            {
                Debug.Log($"[CustomersDebug] CashDeskController: Starting service for next customer in queue (count: {_cashDeskData.CustomerQueue.Count})");
                StartCoroutine(ProcessNextCustomer());
            }
            else
            {
                Debug.Log($"[CustomersDebug] CashDeskController: No more customers in queue. Player operating: {_isPlayerOperating}, Queue count: {_cashDeskData.CustomerQueue.Count}");
            }
        }
        
        private void UpdateQueuePositions()
        {
            // Очищаем старые позиции
            _queuePositions.Clear();
            
            int position = 0;
            foreach (var customer in _cashDeskData.CustomerQueue)
            {
                if (customer != null)
                {
                    // Сохраняем позицию в словаре
                    _queuePositions[customer] = position;
                    
                    CustomerController controller = customer.GetComponent<CustomerController>();
                    if (controller != null)
                    {
                        Vector3 targetPosition = _queueStartPoint.position + _queueStartPoint.forward * (position * _queueSpacing);
                        controller.UpdateQueuePosition(targetPosition, position);
                    }
                }
                position++;
            }
        }
        
        // Управление кассой
        public void OpenCashDesk()
        {
            _cashDeskData.IsOpen = true;
        }
        
        public void CloseCashDesk()
        {
            _cashDeskData.IsOpen = false;
            StopPlayerOperation();
        }
        
        public bool IsOpen => _cashDeskData.IsOpen;
        public bool HasQueue => _cashDeskData.CustomerQueue.Count > 0;
        public int QueueLength => _cashDeskData.GetQueueLength();
        
        /// <summary>
        /// Получает ID кассы
        /// </summary>
        public string GetCashDeskID()
        {
            return _cashDeskID;
        }
        
        // Резервировать место для покупателя, идущего к кассе
        public void ReserveApproachingSpot(GameObject customer)
        {
            lock (_queueLock)
            {
                if (!_customersApproaching.Contains(customer) && !_cashDeskData.CustomerQueue.Contains(customer))
                {
                    _customersApproaching.Add(customer);
                }
            }
        }
        
        // Отменить резервирование места (если покупатель передумал)
        public void CancelApproachingSpot(GameObject customer)
        {
            lock (_queueLock) { _customersApproaching.Remove(customer); }
        }
        
        /// <summary>
        /// Восстанавливает покупателя в очереди после загрузки сохранения
        /// </summary>
        public void RestoreCustomerInQueue(GameObject customer, int queuePosition)
        {
            if (!_cashDeskData.IsOpen || customer == null) return;
            
            Debug.Log($"[CustomersDebug] CashDeskController.RestoreCustomerInQueue: Restoring customer {customer.name} at position {queuePosition}");
            
            lock (_queueLock)
            {
                // Создаем временный список для восстановления очереди в правильном порядке
                var customersInQueue = _cashDeskData.CustomerQueue.ToList();
                
                // Убеждаемся, что клиент не дублируется
                customersInQueue.RemoveAll(c => c == customer);
                
                // Вставляем клиента в нужную позицию
                if (queuePosition >= 0 && queuePosition <= customersInQueue.Count)
                {
                    customersInQueue.Insert(queuePosition, customer);
                }
                else
                {
                    // Если позиция некорректная, добавляем в конец
                    customersInQueue.Add(customer);
                }
                
                // Пересоздаем очередь
                _cashDeskData.CustomerQueue.Clear();
                foreach (var c in customersInQueue)
                {
                    _cashDeskData.CustomerQueue.Enqueue(c);
                }
                
                // Обновляем позиции в очереди
                UpdateQueuePositions();
                
                Debug.Log($"[CustomersDebug] CashDeskController.RestoreCustomerInQueue: Restored customer {customer.name} at queue position {queuePosition}. Total queue: {_cashDeskData.CustomerQueue.Count}. Current state - Player operating: {_isPlayerOperating}, Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}");
                
                // Если касса свободна и игрок работает, проверяем необходимость начать обслуживание
                if (_isPlayerOperating && !_isProcessingCustomer && _cashDeskData.CurrentCustomer == null && _cashDeskData.CustomerQueue.Count > 0)
                {
                    Debug.Log($"[CustomersDebug] CashDeskController.RestoreCustomerInQueue: Starting service after restoring customer in queue");
                    StartCoroutine(ProcessNextCustomer());
                }
                else
                {
                    Debug.Log($"[CustomersDebug] CashDeskController.RestoreCustomerInQueue: Not starting service. Player operating: {_isPlayerOperating}, Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}, Queue count: {_cashDeskData.CustomerQueue.Count}");
                }
            }
        }
        
        /// <summary>
        /// Получает позицию покупателя в очереди
        /// </summary>
        /// <param name="customer">Покупатель для поиска</param>
        /// <returns>Позиция в очереди (0-based) или -1 если не найден</returns>
        public int GetCustomerQueuePosition(GameObject customer)
        {
            if (customer == null) return -1;
            
            lock (_queueLock)
            {
                if (_queuePositions.ContainsKey(customer))
                {
                    return _queuePositions[customer];
                }
            }
            
            return -1; // Клиент не в очереди
        }
        
        /// <summary>
        /// Получает список всех клиентов в очереди
        /// </summary>
        public List<GameObject> GetCustomersInQueue()
        {
            lock (_queueLock)
            {
                return new List<GameObject>(_cashDeskData.CustomerQueue);
            }
        }
        
        /// <summary>
        /// Получает список всех приближающихся клиентов
        /// </summary>
        public List<GameObject> GetApproachingCustomers()
        {
            return new List<GameObject>(_customersApproaching);
        }
        
        /// <summary>
        /// Принудительно проверяет очередь и запускает обслуживание, если нужно
        /// Используется для решения race conditions
        /// </summary>
        public void CheckAndStartProcessing()
        {
            Debug.Log($"[CustomersDebug] CashDeskController.CheckAndStartProcessing: Check called. Player operating: {_isPlayerOperating}, Processing: {_isProcessingCustomer}, Current customer: {_cashDeskData.CurrentCustomer != null}, Queue count: {_cashDeskData.CustomerQueue.Count}");
            
            if (_isPlayerOperating && !_isProcessingCustomer && _cashDeskData.CurrentCustomer == null && _cashDeskData.CustomerQueue.Count > 0)
            {
                Debug.Log($"[CustomersDebug] CashDeskController.CheckAndStartProcessing: Force starting service for customer in queue (count: {_cashDeskData.CustomerQueue.Count})");
                StartCoroutine(ProcessNextCustomer());
            }
            else
            {
                Debug.Log($"[CustomersDebug] CashDeskController.CheckAndStartProcessing: Conditions not met for starting service");
            }
        }
        
        /// <summary>
        /// Размещает префаб товара на ленте кассы
        /// </summary>
        public void PlaceItemOnBelt(ProductConfig product)
        {
            Debug.Log($"[ShelfRemovalDebug] CashDeskController.PlaceItemOnBelt: Attempting to place {product.ProductName}");
            
            if (product.Prefab == null || _itemPlacementPoint == null)
            {
                Debug.LogWarning($"[ShelfRemovalDebug] Product '{product.ProductName}' has no prefab or placement point is not set.", this);
                return;
            }

            if (_placedItems.Count >= _gridColumns * _gridRows)
            {
                Debug.LogWarning($"[ShelfRemovalDebug] Item grid is full.", this);
                return;
            }

            int currentItemIndex = _placedItems.Count;
            int row = currentItemIndex / _gridColumns;
            int column = currentItemIndex % _gridColumns;

            Vector3 offset = (_itemPlacementPoint.right * column * _itemSpacing.x) + 
                             (_itemPlacementPoint.forward * row * _itemSpacing.y);
            
            Vector3 placementPosition = _itemPlacementPoint.position + offset;
            Quaternion placementRotation = _itemPlacementPoint.rotation * Quaternion.Euler(_itemRotation);
            
            Debug.Log($"[ShelfRemovalDebug] Placing {product.ProductName} at position {placementPosition}");
            
            GameObject itemObject = Instantiate(product.Prefab, placementPosition, placementRotation, _itemPlacementPoint);
            
            itemObject.AddComponent<OutlineController>();

            var productHolder = itemObject.AddComponent<ProductHolder>();
            productHolder.Product = product;

            itemObject.layer = LayerMask.NameToLayer("ScannableItem");
            foreach (Transform child in itemObject.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("ScannableItem");
            }

            _placedItems.Add(itemObject);
            
            Debug.Log($"[ShelfRemovalDebug] Successfully placed {product.ProductName}. Total items on belt: {_placedItems.Count}");
        }

        /// <summary>
        /// Очищает все товары с ленты
        /// </summary>
        public void ClearBelt()
        {
            foreach (var item in _placedItems)
            {
                if (item != null) 
                {
                    Debug.Log($"[ScanningBugFix] Destroying unscanned item: {item.name}");
                    Destroy(item);
                }
            }
            _placedItems.Clear();

            foreach (var item in _scannedItems)
            {
                if (item != null) 
                {
                    Debug.Log($"[ScanningBugFix] Destroying scanned item: {item.name}");
                    Destroy(item);
                }
            }
            _scannedItems.Clear();
            
            _runningTotal = 0f;
            Debug.Log("[ScanningBugFix] Cash desk belt cleared completely");
        }

        private void HandleHighlighting()
        {
            // If we were hovering over an item last frame, turn its outline off.
            if (_hoveredOutlineController != null)
            {
                _hoveredOutlineController.IsOutlineEnabled = false;
                _hoveredOutlineController = null;
            }

            Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, _scannableLayer))
            {
                var outlineController = hit.collider.GetComponentInParent<OutlineController>();
                if (outlineController != null)
                {
                    // КРИТИЧЕСКИЙ БАГФИКС: Проверяем, что товар еще не был отсканирован
                    GameObject hitItem = outlineController.gameObject;
                    if (_scannedItems.Contains(hitItem))
                    {
                        // Этот товар уже отсканирован, не подсвечиваем его
                        return;
                    }
                    
                    // We're looking at a new item, so highlight it.
                    _hoveredOutlineController = outlineController;
                    _hoveredOutlineController.IsOutlineEnabled = true;
                }
            }
        }
        
        public void PlayerAttemptScan()
        {
            if (_hoveredOutlineController != null && _isPlayerOperating)
            {
                GameObject itemToScan = _hoveredOutlineController.gameObject;
                
                // КРИТИЧЕСКИЙ БАГФИКС: Проверяем, что товар еще не был отсканирован
                if (_scannedItems.Contains(itemToScan))
                {
                    Debug.LogWarning($"Item {itemToScan.name} is already scanned! Ignoring duplicate scan attempt.");
                    return;
                }
                
                // Turn off outline as we are now processing this item
                _hoveredOutlineController.IsOutlineEnabled = false;
                _hoveredOutlineController = null;
                
                // КРИТИЧЕСКИЙ БАГФИКС: Немедленно изменяем слой, чтобы товар не мог быть отсканирован повторно
                ChangeItemLayerToNonScannable(itemToScan);
                
                _placedItems.Remove(itemToScan);
                _scannedItems.Add(itemToScan);
                
                if(itemToScan.TryGetComponent<ProductHolder>(out var productHolder))
                {
                    // Пытаемся получить цену покупки из данных покупателя (цена на момент взятия с полки)
                    float purchasePrice = GetCustomerPurchasePrice(productHolder.Product);
                    
                    // Если не удалось найти цену покупки, используем текущую розничную цену
                    if (purchasePrice <= 0f)
                    {
                        purchasePrice = _retailPriceService?.GetRetailPrice(productHolder.Product.ProductID) ?? productHolder.Product.BaseSalePrice;
                        Debug.LogWarning($"Could not find purchase price for {productHolder.Product.ProductName}, using current retail price: ${purchasePrice:F2}");
                    }
                    
                    _runningTotal += purchasePrice;
                    Debug.Log($"Scanned {productHolder.Product.ProductName}. Purchase Price: ${purchasePrice:F2}. Total: ${_runningTotal:F2}");
                }

                StartCoroutine(AnimateItemScan(itemToScan));
            }
        }

        private IEnumerator AnimateItemScan(GameObject item)
        {
            // The outline will be disabled automatically by the OutlineController's OnDisable if the object is destroyed
            // or we can handle it manually if needed. For now, let's assume the object remains active but unscannable.
            var outlineController = item.GetComponent<OutlineController>();
            if (outlineController != null)
            {
                outlineController.IsOutlineEnabled = false;
            }

            // Disable physics while animating
            if (item.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
            
            var productHolder = item.GetComponent<ProductHolder>();
            if (productHolder == null)
            {
                Debug.LogWarning("Scanned item does not have a ProductHolder component.");
                yield break;
            }

            Vector3 startPos = item.transform.position;
            Quaternion startRot = item.transform.rotation;

            Vector3 scannerPos = _scannerPoint != null ? _scannerPoint.position : startPos + Vector3.up;
            
            // Вычисляем позицию в сетке для отсканированного товара
            Vector3 collectionPos = startPos;
            if (_collectionPoint != null)
            {
                int scannedItemIndex = _scannedItems.Count - 1; // Текущий товар уже добавлен в _scannedItems
                int row = scannedItemIndex / _gridColumns;
                int column = scannedItemIndex % _gridColumns;
                
                Vector3 offset = (_collectionPoint.right * column * _itemSpacing.x) + 
                                 (_collectionPoint.forward * row * _itemSpacing.y);
                collectionPos = _collectionPoint.position + offset;
            }

            // First part of animation: move to scanner
            float journeyLength = Vector3.Distance(startPos, scannerPos);
            float duration = journeyLength / _itemFlySpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                item.transform.position = Vector3.Lerp(startPos, scannerPos, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            item.transform.position = scannerPos;
            
            // Notify UI
            OnItemScanned?.Invoke(productHolder.Product);
            OnTotalUpdated?.Invoke(_runningTotal);

            // Second part: move from scanner to collection point
            journeyLength = Vector3.Distance(scannerPos, collectionPos);
            duration = journeyLength / _itemFlySpeed;
            elapsedTime = 0f;

            // Вычисляем целевую ротацию с учетом _itemRotation
            Quaternion targetRotation = _collectionPoint != null ? 
                _collectionPoint.rotation * Quaternion.Euler(_itemRotation) : 
                startRot;

            while (elapsedTime < duration)
            {
                item.transform.position = Vector3.Lerp(scannerPos, collectionPos, elapsedTime / duration);
                item.transform.rotation = Quaternion.Lerp(startRot, targetRotation, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            item.transform.position = collectionPos;
            item.transform.rotation = targetRotation;
            if (_collectionPoint != null)
            {
                item.transform.SetParent(_collectionPoint);
            }

            // Re-enable physics after animation
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        void Update()
        {
            if (_isPlayerOperating && !_isMovingToPosition)
            {
                HandleHighlighting();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_itemPlacementPoint != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Vector3 cubeSize = new Vector3(_itemSpacing.x * 0.8f, 0.05f, _itemSpacing.y * 0.8f);
                for (int row = 0; row < _gridRows; row++)
                {
                    for (int col = 0; col < _gridColumns; col++)
                    {
                        Vector3 offset = (_itemPlacementPoint.right * col * _itemSpacing.x) + 
                                         (_itemPlacementPoint.forward * row * _itemSpacing.y);
                        Vector3 cellPosition = _itemPlacementPoint.position + offset;
                        Matrix4x4 originalMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(cellPosition, _itemPlacementPoint.rotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, cubeSize);
                        Gizmos.matrix = originalMatrix;
                    }
                }
            }

            if (_scannerPoint != null)
            {
                Gizmos.color = new Color(1, 0, 0, 0.7f);
                Gizmos.DrawSphere(_scannerPoint.position, 0.1f);
            }
            
            if (_collectionPoint != null)
            {
                // Рисуем сетку для точки сбора отсканированных товаров
                Gizmos.color = new Color(0, 0, 1, 0.5f);
                Vector3 cubeSize = new Vector3(_itemSpacing.x * 0.8f, 0.05f, _itemSpacing.y * 0.8f);
                for (int row = 0; row < _gridRows; row++)
                {
                    for (int col = 0; col < _gridColumns; col++)
                    {
                        Vector3 offset = (_collectionPoint.right * col * _itemSpacing.x) + 
                                         (_collectionPoint.forward * row * _itemSpacing.y);
                        Vector3 cellPosition = _collectionPoint.position + offset;
                        Matrix4x4 originalMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(cellPosition, _collectionPoint.rotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, cubeSize);
                        Gizmos.matrix = originalMatrix;
                    }
                }
            }
        }

        /// <summary>
        /// КРИТИЧЕСКИЙ БАГФИКС: Изменяет слой товара на несканируемый
        /// </summary>
        private void ChangeItemLayerToNonScannable(GameObject item)
        {
            // Изменяем слой на Default, чтобы товар больше не мог быть отсканирован
            item.layer = LayerMask.NameToLayer("Default");
            
            // Также изменяем слой всех дочерних объектов
            foreach (Transform child in item.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            
            Debug.Log($"[ScanningBugFix] Changed layer of {item.name} to Default to prevent re-scanning");
        }

        /// <summary>
        /// Получает цену покупки товара из данных текущего покупателя
        /// </summary>
        private float GetCustomerPurchasePrice(ProductConfig product)
        {
            if (_cashDeskData.CurrentCustomer == null)
                return 0f;
                
            var customerController = _cashDeskData.CurrentCustomer.GetComponent<CustomerController>();
            if (customerController == null)
                return 0f;
                
            var customerData = customerController.GetCustomerData();
            if (customerData == null || customerData.ShoppingList == null)
                return 0f;
            
            // Ищем товар в списке покупок покупателя
            foreach (var shoppingItem in customerData.ShoppingList)
            {
                if (shoppingItem.Product != null && 
                    shoppingItem.Product.ProductID == product.ProductID && 
                    shoppingItem.CollectedQuantity > 0 &&
                    shoppingItem.PurchasePrice > 0f)
                {
                    return shoppingItem.PurchasePrice;
                }
            }
            
            return 0f;
        }

        public void FinishTransaction()
        {
            if (!_isPlayerOperating || _isProcessingCustomer) return;
            
            Debug.Log($"[ScanningBugFix] Finishing transaction. Scanned items count: {_scannedItems.Count}, Total: ${_runningTotal:F2}");
            
            OnTransactionFinalized?.Invoke();

            // Clear state for next customer
            _runningTotal = 0;
            _scannedProductConfigs.Clear();
            
            // Уничтожаем все отсканированные товары с логированием
            foreach (var item in _scannedItems)
            {
                if (item != null)
                {
                    Debug.Log($"[ScanningBugFix] Destroying scanned item on transaction finish: {item.name}");
                    Destroy(item);
                }
            }
            _scannedItems.Clear();
            
            // Обновляем UI для отображения нулевой суммы
            OnTotalUpdated?.Invoke(0);
            
            // Очищаем UI после завершения транзакции
            if (_uiHandler != null)
            {
                _uiHandler.Clear();
            }
            
            Debug.Log("[ScanningBugFix] Transaction finished and state cleared");
        }
    }
} 