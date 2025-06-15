using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourInject;
using Supermarket.Services.Game;
using Supermarket.Components;
using Supermarket.Data;
using System.Collections;

namespace Supermarket.Interactables
{
    [RequireComponent(typeof(CustomerLocomotion))]
    public class CustomerController : MonoBehaviour
    {
        private CustomerData _customerData;
        private CustomerLocomotion _locomotion;
        
        [Header("Navigation")]
        [SerializeField] private float _reachDistance = 1.5f;
        [SerializeField] private float _interactionDistance = 0.8f; // Уменьшено для точной работы с approach points
        [SerializeField] private float _queueStopDistance = 0.5f; // Минимальная дистанция в очереди
        
        [Header("Shopping Behavior")]
        [SerializeField] private float _pickupAnimationDelay = 0.5f; // Задержка перед анимацией взятия
        [SerializeField] private float _searchRadius = 50f;
        [SerializeField] private float _animationTimeout = 5f; // Таймаут для защиты от зависания
        [SerializeField] private float _cashDeskWaitTimeout = 30f; // Максимальное время ожидания кассы (в секундах)
        
        [Header("Exit Behavior")]
        [SerializeField] private float _exitRadius = 3f; // Радиус вокруг точки выхода для избежания толпления
        [SerializeField] private float _exitReachDistance = 2f; // Дистанция считается достижением выхода
        [SerializeField] private float _exitTimeout = 10f; // Таймаут выхода - если не дошли за это время, принудительно уничтожаем
        
        // Текущие цели
        private ShelfController _targetShelf;
        private MultiLevelShelfController _targetMultiShelf; // Добавляем для многоуровневых полок
        private ShoppingItem _currentShoppingItem;
        private Transform _exitPoint;
        private Vector3 _personalExitPosition; // Персональная позиция выхода с offset'ом
        
        // Касса, к которой идет покупатель
        private MonoBehaviour _targetCashDesk;
        private Vector3 _queuePosition;
        private bool _isWaitingInQueue = false;
        
        // События
        public event Action<GameObject> OnCustomerLeaving;
        
        // Таймеры
        private float _stateTimer = 0f;
        private bool _pickupAnimationPlayed = false; // Флаг для контроля анимации
        private bool _payAnimationPlayed = false; // Флаг для контроля анимации оплаты
        private bool _paymentProcessed = false; // Флаг для контроля процесса оплаты
        
        [Inject]
        public ICashDeskService _cashDeskService;
        
            [Inject]
    public IStorePointsService _storePointsService;
    
    [Inject]
    public IPurchaseDecisionService _purchaseDecisionService;
    
    [Inject]
    public IRetailPriceService _retailPriceService;
    
    [Inject]
    public IStreetWaypointService _streetWaypointService;
    
    [Inject]
    public IShoppingListGeneratorService _shoppingListGeneratorService;
    
    // Поля для уличных прогулок
    [Header("Street Walking Behavior")]
    [SerializeField] private float _waypointReachDistance = 2f;
    [SerializeField] private float _waitTimeAtWaypoint = 3f;
    [SerializeField] private float _storeEnterChance = 0.3f; // 30% шанс зайти в магазин
    [SerializeField] private float _waypointWaitTimeMin = 1f;
    [SerializeField] private float _waypointWaitTimeMax = 5f;
    
    private Transform _currentWaypoint;
    private float _waypointWaitTimer = 0f;
    private bool _isWaitingAtWaypoint = false;
    
    // Delayed cash desk restoration fields
    private string _savedCashDeskId;
    private int _savedQueuePosition;
    private bool _savedIsInQueue;
    private bool _cashDeskRestored = false;
        
        void Awake()
        {
            _locomotion = GetComponent<CustomerLocomotion>();
            if (_locomotion == null)
            {
                Debug.LogError("CustomerController: CustomerLocomotion component required!", this);
                enabled = false;
            }
        }
        
        public void Initialize(CustomerData customerData)
        {
            _customerData = customerData;
            
            // Используем точки из сервиса
            if (_storePointsService != null)
            {
                _exitPoint = _storePointsService.ExitPoint;
                if (_exitPoint == null)
                {
                    Debug.LogWarning("CustomerController: Exit point not set in StorePointsService!");
                }
            }
            else
            {
                Debug.LogError("CustomerController: StorePointsService not injected!");
            }
            
            // Начинаем с состояния уличных прогулок
            ChangeState(CustomerState.StreetWalking);
        }
        
        /// <summary>
        /// Инициализирует восстановленного покупателя БЕЗ смены состояния
        /// Используется только при загрузке сохраненной игры
        /// </summary>
        public void InitializeRestored(CustomerData customerData)
        {
            _customerData = customerData;
            
            // Используем точки из сервиса
            if (_storePointsService != null)
            {
                _exitPoint = _storePointsService.ExitPoint;
                if (_exitPoint == null)
                {
                    Debug.LogWarning("CustomerController: Exit point not set in StorePointsService!");
                }
            }
            else
            {
                Debug.LogError("CustomerController: StorePointsService not injected!");
            }
            
            // НЕ меняем состояние - оно будет восстановлено в RestoreState
            Debug.Log($"CustomerController: Initialized restored customer '{customerData.CustomerName}' without state change");
        }
        
        void Update()
        {
            if (_customerData == null) return;
            
            _stateTimer += Time.deltaTime;
            
            // Обработка текущего состояния
            switch (_customerData.CurrentState)
            {
                case CustomerState.StreetWalking:
                    UpdateStreetWalking();
                    break;
                case CustomerState.ConsideringStore:
                    UpdateConsideringStore();
                    break;
                case CustomerState.Entering:
                    UpdateEntering();
                    break;
                case CustomerState.Shopping:
                    UpdateShopping();
                    break;
                case CustomerState.GoingToShelf:
                    UpdateGoingToShelf();
                    break;
                case CustomerState.TakingItem:
                    UpdateTakingItem();
                    break;
                case CustomerState.GoingToCashier:
                    UpdateGoingToCashier();
                    break;
                case CustomerState.JoiningQueue:
                    UpdateJoiningQueue();
                    break;
                case CustomerState.WaitingInQueue:
                    UpdateWaitingInQueue();
                    break;
                case CustomerState.PlacingItemsOnBelt:
                    UpdatePlacingItemsOnBelt();
                    break;
                case CustomerState.Paying:
                    UpdatePaying();
                    break;
                case CustomerState.Leaving:
                    UpdateLeaving();
                    break;
            }
        }
        
        public void ChangeState(CustomerState newState)
        {
            Debug.Log($"[CustomersDebug] Customer {_customerData.CustomerName}: {_customerData.CurrentState} -> {newState}");
            
            // Если уходим из состояния движения к кассе, отменяем резервирование
            if ((_customerData.CurrentState == CustomerState.GoingToCashier || 
                 _customerData.CurrentState == CustomerState.JoiningQueue) &&
                newState != CustomerState.GoingToCashier && 
                newState != CustomerState.JoiningQueue && 
                newState != CustomerState.WaitingInQueue &&
                newState != CustomerState.Paying)
            {
                if (_targetCashDesk != null)
                {
                    var cashDeskController = _targetCashDesk as CashDeskController;
                    cashDeskController?.CancelApproachingSpot(gameObject);
                }
            }
            
            // Сбрасываем все триггеры анимаций перед сменой состояния
            _locomotion.ResetAllActionTriggers();
            
            _customerData.CurrentState = newState;
            _stateTimer = 0f;
            _pickupAnimationPlayed = false; // Сбрасываем флаг анимации
            _payAnimationPlayed = false; // Сбрасываем флаг анимации оплаты
            _paymentProcessed = false; // Сбрасываем флаг процесса оплаты
            
            // Инициализация нового состояния
            switch (newState)
            {
                case CustomerState.StreetWalking:
                    StartStreetWalking();
                    break;
                case CustomerState.ConsideringStore:
                    StartConsideringStore();
                    break;
                case CustomerState.Entering:
                    StartEntering();
                    break;
                case CustomerState.Shopping:
                    StartShopping();
                    break;
                case CustomerState.GoingToShelf:
                    StartGoingToShelf();
                    break;
                case CustomerState.GoingToCashier:
                    StartGoingToCashier();
                    break;
                case CustomerState.Leaving:
                    StartLeaving();
                    break;
            }
        }
        
        // Методы для уличных прогулок
        private void StartStreetWalking()
        {
            Debug.Log($"Customer {_customerData.CustomerName}: Starting street walking");
            
            if (_streetWaypointService == null || !_streetWaypointService.HasWaypoints())
            {
                Debug.LogWarning($"Customer {_customerData.CustomerName}: No waypoint service or waypoints available, skipping to store");
                ChangeState(CustomerState.ConsideringStore);
                return;
            }
            
            // Начинаем с случайного waypoint
            _currentWaypoint = _streetWaypointService.GetRandomWaypoint();
            if (_currentWaypoint != null)
            {
                _locomotion.Resume();
                _locomotion.SetDestination(_currentWaypoint.position);
                _isWaitingAtWaypoint = false;
                Debug.Log($"Customer {_customerData.CustomerName}: Moving to waypoint {_currentWaypoint.name}");
            }
            else
            {
                Debug.LogWarning($"Customer {_customerData.CustomerName}: Failed to get initial waypoint");
                ChangeState(CustomerState.ConsideringStore);
            }
        }
        
        private void UpdateStreetWalking()
        {
            // Если ждем на waypoint
            if (_isWaitingAtWaypoint)
            {
                _waypointWaitTimer += Time.deltaTime;
                
                if (_waypointWaitTimer >= _waitTimeAtWaypoint)
                {
                                         // Время ожидания прошло, решаем, что делать дальше
                     float random = UnityEngine.Random.Range(0f, 1f);
                    
                    if (random < _storeEnterChance)
                    {
                        // Решили зайти в магазин
                        Debug.Log($"Customer {_customerData.CustomerName}: Decided to enter store (chance: {random:F2})");
                        ChangeState(CustomerState.ConsideringStore);
                    }
                    else
                    {
                        // Продолжаем гулять, идем к следующему waypoint
                        MoveToNextWaypoint();
                    }
                }
                return;
            }
            
            // Если движемся к waypoint
            if (_currentWaypoint != null)
            {
                float distance = Vector3.Distance(transform.position, _currentWaypoint.position);
                
                if (distance <= _waypointReachDistance || _locomotion.HasReachedDestination())
                {
                    // Достигли waypoint
                    Debug.Log($"Customer {_customerData.CustomerName}: Reached waypoint {_currentWaypoint.name}");
                    _locomotion.Stop();
                    
                                         // Начинаем ждать на waypoint
                     _isWaitingAtWaypoint = true;
                     _waypointWaitTimer = 0f;
                     _waitTimeAtWaypoint = UnityEngine.Random.Range(_waypointWaitTimeMin, _waypointWaitTimeMax);
                     
                     // Иногда проигрываем анимацию
                     if (UnityEngine.Random.Range(0f, 1f) < 0.3f) // 30% шанс
                     {
                         int randomAnimation = UnityEngine.Random.Range(0, 2);
                        if (randomAnimation == 0)
                        {
                            _locomotion.PlayWaveAnimation();
                        }
                        // Можно добавить больше анимаций
                    }
                }
            }
            else
            {
                // Нет текущего waypoint, получаем новый
                MoveToNextWaypoint();
            }
        }
        
        private void MoveToNextWaypoint()
        {
            if (_streetWaypointService == null)
            {
                Debug.LogWarning($"Customer {_customerData.CustomerName}: No waypoint service, going to store");
                ChangeState(CustomerState.ConsideringStore);
                return;
            }
            
            Transform nextWaypoint = _streetWaypointService.GetNextWaypoint(transform.position, _currentWaypoint);
            
            if (nextWaypoint != null)
            {
                _currentWaypoint = nextWaypoint;
                _locomotion.Resume();
                _locomotion.SetDestination(_currentWaypoint.position);
                _isWaitingAtWaypoint = false;
                Debug.Log($"Customer {_customerData.CustomerName}: Moving to next waypoint {_currentWaypoint.name}");
            }
            else
            {
                Debug.LogWarning($"Customer {_customerData.CustomerName}: No next waypoint available, going to store");
                ChangeState(CustomerState.ConsideringStore);
            }
        }
        
        private void StartConsideringStore()
        {
            Debug.Log($"Customer {_customerData.CustomerName}: Considering entering store");
            _locomotion.Stop();
            
            // Генерируем список покупок если его нет
            if (_customerData.ShoppingList == null || _customerData.ShoppingList.Count == 0)
            {
                Debug.Log($"Customer {_customerData.CustomerName}: Generating shopping list");
                GenerateShoppingList();
            }
            
            // Если есть точка входа в магазин, идем к ней
            if (_streetWaypointService != null)
            {
                Transform storeEntrance = _streetWaypointService.GetStoreEntrancePoint();
                if (storeEntrance != null)
                {
                    _locomotion.Resume();
                    _locomotion.SetDestination(storeEntrance.position);
                    Debug.Log($"Customer {_customerData.CustomerName}: Moving to store entrance");
                }
                else
                {
                    Debug.LogWarning($"Customer {_customerData.CustomerName}: No store entrance point, entering directly");
                    ChangeState(CustomerState.Entering);
                }
            }
            else
            {
                ChangeState(CustomerState.Entering);
            }
        }
        
        private void UpdateConsideringStore()
        {
            // Если идем к точке входа в магазин
            if (_streetWaypointService != null)
            {
                Transform storeEntrance = _streetWaypointService.GetStoreEntrancePoint();
                if (storeEntrance != null)
                {
                    float distance = Vector3.Distance(transform.position, storeEntrance.position);
                    
                    if (distance <= _waypointReachDistance || _locomotion.HasReachedDestination())
                    {
                        Debug.Log($"Customer {_customerData.CustomerName}: Reached store entrance, entering");
                        ChangeState(CustomerState.Entering);
                    }
                    return;
                }
            }
            
            // Если нет точки входа или произошла ошибка, просто входим
            ChangeState(CustomerState.Entering);
        }
        
        private void StartEntering()
        {
            Debug.Log($"Customer {_customerData.CustomerName}: Entering store");
            // Можно добавить логику входа в магазин (анимация, проверки и т.д.)
        }
        
        private void UpdateEntering()
        {
            // Просто переходим к покупкам
            ChangeState(CustomerState.Shopping);
        }
        
        private void UpdateShopping()
        {
            // Находим следующий товар для покупки
            _currentShoppingItem = GetNextItemToBuy();
            
            if (_currentShoppingItem == null)
            {
                // Логируем состояние корзины для отладки
                Debug.Log($"Customer {_customerData.CustomerName}: No more items to buy. Shopping list status:");
                foreach (var item in _customerData.ShoppingList)
                {
                    Debug.Log($"  - {item.Product.ProductName}: Desired={item.DesiredQuantity}, Collected={item.CollectedQuantity}, IsComplete={item.IsComplete}");
                }
                
                bool hasItems = HasItemsInCart();
                Debug.Log($"Customer {_customerData.CustomerName}: HasItemsInCart = {hasItems}");
                
                // Список покупок выполнен или пуст
                if (hasItems)
                {
                    // ИСПРАВЛЕНО: Проверяем, есть ли доступные кассы перед переходом к кассе
                    FindBestCashDesk();
                    
                    if (_targetCashDesk != null)
                    {
                        Debug.Log($"Customer {_customerData.CustomerName}: Going to cashier with items");
                        ChangeState(CustomerState.GoingToCashier);
                    }
                    else
                    {
                        // Есть товары, но нет касс - ждем в состоянии покупок
                        Debug.Log($"Customer {_customerData.CustomerName}: Has items but no cash desk available, waiting for cash desk");
                        
                        // Проверяем таймаут терпения
                        if (_stateTimer >= _cashDeskWaitTimeout)
                        {
                            Debug.Log($"Customer {_customerData.CustomerName}: Patience timeout reached ({_cashDeskWaitTimeout}s), leaving with items!");
                            ChangeState(CustomerState.Leaving);
                            return;
                        }
                        
                        // Останавливаемся и ждем появления кассы
                        _locomotion.Stop();
                        
                        // Можем проиграть анимацию ожидания
                        if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% шанс каждый кадр
                        {
                            _locomotion.PlayWaveAnimation(); // Анимация недовольства/ожидания
                        }
                        
                        // Остаемся в состоянии Shopping - не переходим к Leaving!
                        return;
                    }
                }
                else
                {
                    Debug.Log($"Customer {_customerData.CustomerName}: Leaving without items (possible bug!)");
                    ChangeState(CustomerState.Leaving);
                }
                return;
            }
            
            // Ищем полку с нужным товаром
            FindShelfWithProduct(_currentShoppingItem.Product);
            
            if (_targetShelf != null || _targetMultiShelf != null)
            {
                ChangeState(CustomerState.GoingToShelf);
            }
            else
            {
                // Не нашли товар на полке
                Debug.Log($"Customer {_customerData.CustomerName}: Can't find {_currentShoppingItem.Product.ProductName}");
                
                // ВАЖНО: НЕ удаляем товар из списка, если уже что-то собрали!
                // Иначе потеряем информацию о том, что клиент взял
                if (_currentShoppingItem.CollectedQuantity == 0)
                {
                    // Товар вообще не был найден - можем удалить
                    Debug.Log($"Customer {_customerData.CustomerName}: Removing {_currentShoppingItem.Product.ProductName} from shopping list (never found)");
                    _customerData.ShoppingList.Remove(_currentShoppingItem);
                }
                else
                {
                    // Клиент уже взял часть товара - оставляем в списке, но помечаем как недоступный
                    Debug.Log($"Customer {_customerData.CustomerName}: Marking {_currentShoppingItem.Product.ProductName} as unavailable (collected {_currentShoppingItem.CollectedQuantity} of {_currentShoppingItem.DesiredQuantity})");
                    _currentShoppingItem.UnavailableInStore = true;
                }
                
                _currentShoppingItem = null;
            }
        }
        
        private void StartShopping()
        {
            // Остановить движение при начале поиска
            _locomotion.Stop();
        }
        
        private void StartGoingToShelf()
        {
            if (_targetShelf != null)
            {
                _locomotion.Resume();
                _locomotion.SetDestination(_targetShelf.GetCustomerApproachPosition());
            }
            else if (_targetMultiShelf != null)
            {
                _locomotion.Resume();
                _locomotion.SetDestination(_targetMultiShelf.GetCustomerApproachPosition());
            }
        }
        
        private void UpdateGoingToShelf()
        {
            if (_targetShelf == null && _targetMultiShelf == null)
            {
                ChangeState(CustomerState.Shopping);
                return;
            }
            
            // Определяем позицию цели (используем точку подхода)
            Vector3 targetPosition = _targetShelf != null ? 
                _targetShelf.GetCustomerApproachPosition() : 
                _targetMultiShelf.GetCustomerApproachPosition();
            
            // Проверяем, достигли ли полки
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= _interactionDistance)
            {
                _locomotion.Stop();
                // Поворачиваемся к полке БЕЗ анимации для быстрого перехода
                Transform targetTransform = _targetShelf != null ? 
                    _targetShelf.transform : 
                    _targetMultiShelf.transform;
                _locomotion.FaceTarget(targetTransform, false);
                ChangeState(CustomerState.TakingItem);
            }
        }
        
        private void UpdateTakingItem()
        {
            // Продолжаем поворачиваться к полке С анимацией (уже стоим на месте)
            Transform targetTransform = _targetShelf != null ? 
                _targetShelf.transform : 
                (_targetMultiShelf != null ? _targetMultiShelf.transform : null);
            
            if (targetTransform != null)
            {
                _locomotion.FaceTarget(targetTransform, true);
            }
            
            // Проигрываем анимацию взятия только один раз в начале каждого цикла взятия
            if (!_pickupAnimationPlayed && _stateTimer >= _pickupAnimationDelay)
            {
                // Дополнительная проверка: убеждаемся, что анимация pickup не проигрывается сейчас
                if (!_locomotion.IsPlayingActionAnimation("Pickup"))
                {
                    _locomotion.PlayPickupAnimation();
                    _pickupAnimationPlayed = true;
                }
            }
            
            // Ждем завершения анимации взятия
            if (_pickupAnimationPlayed)
            {
                // Проверяем таймаут для защиты от зависания
                if (_stateTimer >= _animationTimeout)
                {
                    Debug.LogWarning($"Customer {_customerData.CustomerName}: Pickup animation timeout!");
                    ProcessItemPickup();
                }
                // Проверяем, завершилась ли анимация
                else if (_locomotion.IsPickupAnimationComplete())
                {
                    ProcessItemPickup();
                }
            }
        }
        
        private void ProcessItemPickup()
        {
            if ((_targetShelf == null && _targetMultiShelf == null) || _currentShoppingItem == null)
            {
                // Если полка или товар пропали, возвращаемся к покупкам
                _locomotion.ResetPickupTrigger();
                ChangeState(CustomerState.Shopping);
                return;
            }
            
            // Проверяем, хочет ли покупатель взять товар с учетом цены
            bool wantsToBuy = true;
            if (_purchaseDecisionService != null && _retailPriceService != null)
            {
                wantsToBuy = _purchaseDecisionService.ShouldCustomerTakeItem(
                    _currentShoppingItem.Product, 
                    _customerData.Money, 
                    _retailPriceService
                );
                
                if (!wantsToBuy)
                {
                    float retailPrice = _retailPriceService.GetRetailPrice(_currentShoppingItem.Product.ProductID);
                    Debug.Log($"Customer {_customerData.CustomerName} refused to buy {_currentShoppingItem.Product.ProductName} due to high price (${retailPrice:F2})");
                    
                    // Помечаем товар как недоступный из-за цены
                    _currentShoppingItem.UnavailableInStore = true;
                    _locomotion.ResetPickupTrigger();
                    ChangeState(CustomerState.Shopping);
                    return;
                }
            }
            
            // Пытаемся взять товар с полки (только если покупатель хочет его купить)
            bool success = TryTakeItemFromShelf();
            
            if (success)
            {
                _currentShoppingItem.CollectedQuantity++;
                
                // Сохраняем цену, по которой клиент взял товар (первое взятие определяет цену для всего количества)
                if (_currentShoppingItem.CollectedQuantity == 1 && _retailPriceService != null)
                {
                    _currentShoppingItem.PurchasePrice = _retailPriceService.GetRetailPrice(_currentShoppingItem.Product.ProductID);
                    Debug.Log($"Customer {_customerData.CustomerName} locked price for {_currentShoppingItem.Product.ProductName} at ${_currentShoppingItem.PurchasePrice:F2}");
                }
                
                Debug.Log($"Customer {_customerData.CustomerName} took {_currentShoppingItem.Product.ProductName}");
                
                if (_currentShoppingItem.IsComplete)
                {
                    // Товар собран полностью - сбрасываем триггер перед сменой состояния
                    _locomotion.ResetPickupTrigger();
                    ChangeState(CustomerState.Shopping);
                }
                else
                {
                    // Нужно еще, берем снова
                    _stateTimer = 0f;
                    _pickupAnimationPlayed = false; // Сбрасываем флаг для следующей анимации
                }
            }
            else
            {
                // Не удалось взять товар (полка пуста?)
                Debug.Log($"Customer {_customerData.CustomerName}: Shelf is empty");
                
                // Если клиент уже что-то взял, но не может взять больше - помечаем товар как недоступный
                if (_currentShoppingItem != null && _currentShoppingItem.CollectedQuantity > 0)
                {
                    Debug.Log($"Customer {_customerData.CustomerName}: Marking {_currentShoppingItem.Product.ProductName} as unavailable (shelf empty)");
                    _currentShoppingItem.UnavailableInStore = true;
                }
                
                _locomotion.ResetPickupTrigger();
                ChangeState(CustomerState.Shopping);
            }
        }
        
        private void StartGoingToCashier()
        {
            // Сразу пытаемся найти лучшую кассу
            FindBestCashDesk();
            if (_targetCashDesk == null)
            {
                Debug.LogWarning("Customer: No available cash desk found! Going to exit instead.");
                ChangeState(CustomerState.Leaving);
            }
            else
            {
                _locomotion.Resume();
                // Резервируем место в очереди
                var cashDeskController = _targetCashDesk as CashDeskController;
                if (cashDeskController != null)
                {
                    cashDeskController.ReserveApproachingSpot(gameObject);
                    Vector3 endOfQueue = cashDeskController.GetEndOfQueuePosition();
                    _locomotion.SetDestination(endOfQueue);
                }
            }
        }
        
        private void UpdateGoingToCashier()
        {
            if (_targetCashDesk == null)
            {
                // Пытаемся найти кассу, если еще не нашли
                FindBestCashDesk();
                if (_targetCashDesk == null)
                {
                    ChangeState(CustomerState.Leaving);
                    return;
                }
            }
            
            // Идем к концу очереди
            if (_locomotion.HasReachedDestination())
            {
                // Достигли конца очереди, переходим к выстраиванию в линию
                ChangeState(CustomerState.JoiningQueue);
            }
        }
        
        private void UpdateJoiningQueue()
        {
            if (_targetCashDesk == null)
            {
                ChangeState(CustomerState.Shopping);
                return;
            }
            
            var cashDeskController = _targetCashDesk as CashDeskController;
            if (cashDeskController == null)
            {
                _targetCashDesk = null;
                ChangeState(CustomerState.Shopping);
                return;
            }
            
            // Выравниваемся по направлению очереди
            Vector3 queueDirection = cashDeskController.GetQueueDirection();
            _locomotion.FaceDirection(-queueDirection, false); // Смотрим в сторону кассы
            
            // Ждем небольшую задержку для синхронизации
            if (_stateTimer >= 0.3f)
            {
                // Пытаемся встать в очередь
                if (cashDeskController.TryJoinQueue(gameObject))
                {
                    // Переходим в WaitingInQueue только если мы все еще в состоянии JoiningQueue
                    // (TryJoinQueue могло уже изменить состояние через StartPaymentProcess)
                    if (_customerData.CurrentState == CustomerState.JoiningQueue)
                    {
                        ChangeState(CustomerState.WaitingInQueue);
                        
                        // Дополнительная проверка для запуска обслуживания
                        // Нужно добавить небольшую задержку для корректной синхронизации
                        StartCoroutine(CheckServiceAfterJoiningQueue(cashDeskController));
                    }
                }
                else
                {
                    // Не удалось встать в очередь, пробуем еще раз через некоторое время
                    _stateTimer = 0f;
                }
            }
        }
        
        private void UpdateWaitingInQueue()
        {
            // Покупатель ждет, пока его не вызовет касса
            // Face the cash desk while waiting
            if (_targetCashDesk != null)
            {
                _locomotion.FaceTarget(_targetCashDesk.transform, false);
            }
            
            // Если мы уже в очереди и не двигаемся, проверяем, достигли ли мы конечной точки
            if (_isWaitingInQueue && !_locomotion.IsMoving)
            {
                if (!_locomotion.HasReachedDestination(_queueStopDistance))
                {
                    // Если мы далеко от цели, возобновляем движение
                    Debug.Log($"Customer {_customerData.CustomerName} is too far from queue position, resuming movement.");
                    _locomotion.Resume();
                }
            }

            // Если наша цель (позиция в очереди) изменилась, NavMeshAgent сам поведет нас
            if (_locomotion.IsMoving)
            {
                // Если мы двигаемся, но почти достигли цели, останавливаемся
                if (_locomotion.HasReachedDestination(_queueStopDistance))
                {
                    _locomotion.Stop();
                    // Поворачиваемся к кассе
                    if (_targetCashDesk != null)
                    {
                        _locomotion.FaceTarget(_targetCashDesk.transform, false);
                    }
                }
            }
        }
        
        private void UpdatePlacingItemsOnBelt()
        {
            // Логика этого состояния управляется корутиной
            // Мы просто ждем, пока она не завершится и не сменит состояние
            if (_targetCashDesk != null)
            {
                _locomotion.FaceTarget(_targetCashDesk.transform, true);
            }
        }
        
        private void UpdatePaying()
        {
            // Процесс оплаты контролируется кассой
            // Мы просто ждем вызова CompletePayment
            if (_targetCashDesk != null)
            {
                _locomotion.FaceTarget(_targetCashDesk.transform, true);
            }
            
            // Проигрываем анимацию оплаты только один раз
            if (!_payAnimationPlayed)
            {
                _locomotion.PlayPayAnimation();
                _payAnimationPlayed = true;
            }
            
            // Ждем завершения анимации оплаты перед тем, как позволить завершить процесс
            if (_payAnimationPlayed && !_paymentProcessed)
            {
                // Проверяем таймаут для защиты от зависания
                if (_stateTimer >= _animationTimeout)
                {
                    Debug.LogWarning($"Customer {_customerData.CustomerName}: Pay animation timeout!");
                    _paymentProcessed = true;
                }
                // Проверяем, завершилась ли анимация
                else if (_locomotion.IsPayAnimationComplete())
                {
                    _paymentProcessed = true;
                }
            }
        }
        
        private void StartLeaving()
        {
            // Убираемся из очереди кассы, если мы там были
            if (_targetCashDesk != null && _isWaitingInQueue)
            {
                var cashDeskController = _targetCashDesk as CashDeskController;
                if (cashDeskController != null)
                {
                    cashDeskController.LeaveQueue(gameObject);
                }
                _isWaitingInQueue = false;
            }
            
            if (_exitPoint != null)
            {
                // Если у нас нет сохраненной персональной позиции выхода, создаем новую
                if (_personalExitPosition == Vector3.zero)
                {
                    // Создаем персональную позицию выхода с случайным offset'ом в радиусе
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * _exitRadius;
                    _personalExitPosition = _exitPoint.position + new Vector3(randomOffset.x, 0, randomOffset.y);
                    
                    Debug.Log($"Customer {_customerData.CustomerName}: Created new personal exit position {_personalExitPosition} (offset from {_exitPoint.position})");
                }
                else
                {
                    Debug.Log($"Customer {_customerData.CustomerName}: Using restored personal exit position {_personalExitPosition}");
                }
                
                _locomotion.Resume();
                _locomotion.SetDestination(_personalExitPosition);
            }
            else
            {
                // Если нет точки выхода, просто уничтожаем
                LeaveStore();
            }
        }
        
        private void UpdateLeaving()
        {
            if (_exitPoint != null)
            {
                // Проверяем достижение персональной позиции выхода с увеличенным радиусом
                float distanceToExit = Vector3.Distance(transform.position, _personalExitPosition);
                
                if (distanceToExit <= _exitReachDistance || _locomotion.HasReachedDestination())
                {
                    Debug.Log($"Customer {_customerData.CustomerName}: Reached exit position (distance: {distanceToExit:F2})");
                    LeaveStore();
                }
                else if (_stateTimer > _exitTimeout)
                {
                    // Таймаут - принудительно уничтожаем покупателя
                    Debug.LogWarning($"Customer {_customerData.CustomerName}: Exit timeout reached ({_exitTimeout}s), forcing leave");
                    LeaveStore();
                }
            }
        }
        
        private void LeaveStore()
        {
            Debug.Log($"Customer {_customerData.CustomerName} left the store");
            OnCustomerLeaving?.Invoke(gameObject);
            Destroy(gameObject);
        }
        
        // Вспомогательные методы
        private ShoppingItem GetNextItemToBuy()
        {
            // Ищем товар, который еще не завершен И доступен в магазине
            return _customerData.ShoppingList.FirstOrDefault(item => item.CanContinueShopping);
        }
        
        public bool HasItemsInCart()
        {
            bool hasItems = _customerData.ShoppingList.Any(item => item.CollectedQuantity > 0);
            
            // Добавляем детальное логирование для отладки проблемы с удалением полок
            Debug.Log($"[ShelfRemovalDebug] Customer {_customerData.CustomerName}: HasItemsInCart check:");
            Debug.Log($"[ShelfRemovalDebug] - Shopping list count: {_customerData.ShoppingList.Count}");
            Debug.Log($"[ShelfRemovalDebug] - Has items result: {hasItems}");
            
            foreach (var item in _customerData.ShoppingList)
            {
                Debug.Log($"[ShelfRemovalDebug] - {item.Product.ProductName}: Desired={item.DesiredQuantity}, Collected={item.CollectedQuantity}, Complete={item.IsComplete}, Price={item.PurchasePrice:F2}");
            }
            
            return hasItems;
        }
        
        private void FindShelfWithProduct(ProductConfig product)
        {
            _targetShelf = null;
            _targetMultiShelf = null;
            
            // Находим все полки в радиусе
            Collider[] colliders = Physics.OverlapSphere(transform.position, _searchRadius);
            List<ShelfController> shelves = new List<ShelfController>();
            List<MultiLevelShelfController> multiLevelShelves = new List<MultiLevelShelfController>();
            
            foreach (var collider in colliders)
            {
                // Сначала проверяем многоуровневые полки
                MultiLevelShelfController multiShelf = collider.GetComponent<MultiLevelShelfController>();
                if (multiShelf == null)
                    multiShelf = collider.GetComponentInParent<MultiLevelShelfController>();
                if (multiShelf == null)
                {
                    Transform root = collider.transform.root;
                    multiShelf = root.GetComponentInChildren<MultiLevelShelfController>();
                }
                
                if (multiShelf != null)
                {
                    int totalCount;
                    if (multiShelf.HasProduct(product, out totalCount) && totalCount > 0)
                    {
                        multiLevelShelves.Add(multiShelf);
                        continue; // Переходим к следующему коллайдеру
                    }
                }
                
                // Проверяем обычные полки для обратной совместимости
                ShelfController shelf = collider.GetComponent<ShelfController>();
                
                // Если не нашли на самом объекте, ищем на родителях
                if (shelf == null)
                {
                    shelf = collider.GetComponentInParent<ShelfController>();
                }
                
                // Если все еще не нашли, ищем в дочерних от корня
                if (shelf == null)
                {
                    Transform root = collider.transform.root;
                    shelf = root.GetComponentInChildren<ShelfController>();
                }
                
                if (shelf != null && shelf.acceptedProduct == product && shelf.GetCurrentItemCount() > 0)
                {
                    shelves.Add(shelf);
                }
            }
            
            // Приоритет отдаем многоуровневым полкам (обычно они новее и лучше организованы)
            if (multiLevelShelves.Count > 0)
            {
                _targetMultiShelf = multiLevelShelves.OrderBy(s => Vector3.Distance(transform.position, s.transform.position)).First();
                Debug.Log($"CustomerController: Found multi-level shelf with {product.ProductName}");
                return;
            }
            
            // Используем обычные полки если многоуровневых нет
            if (shelves.Count > 0)
            {
                _targetShelf = shelves.OrderBy(s => Vector3.Distance(transform.position, s.transform.position)).First();
                Debug.Log($"CustomerController: Found regular shelf with {product.ProductName}");
                return;
            }
            
            Debug.Log($"CustomerController: No shelf found with {product.ProductName}");
        }
        
        private bool TryTakeItemFromShelf()
        {
            if (_targetShelf != null)
            {
                // Проверяем, есть ли товар на обычной полке
                if (_targetShelf.GetCurrentItemCount() > 0)
                {
                    // Используем публичный метод для уменьшения количества
                    _targetShelf.CustomerTakeItem();
                    return true;
                }
            }
            else if (_targetMultiShelf != null && _currentShoppingItem != null)
            {
                // Для многоуровневой полки проверяем результат взятия
                return _targetMultiShelf.CustomerTakeItem(_currentShoppingItem.Product);
            }
            
            return false;
        }
        
        // Новые методы для работы с кассой
        private void FindBestCashDesk()
        {
            if (_cashDeskService == null)
            {
                Debug.LogError("CustomerController: CashDeskService is not injected!");
                return;
            }
            
            // Отменяем предыдущее резервирование, если было
            if (_targetCashDesk != null)
            {
                var oldCashDesk = _targetCashDesk as CashDeskController;
                oldCashDesk?.CancelApproachingSpot(gameObject);
            }
            
            // Используем сервис для поиска кассы с наименьшей очередью
            GameObject bestCashDeskObj = _cashDeskService.FindCashDeskWithShortestQueue();
            
            if (bestCashDeskObj != null)
            {
                var cashDeskController = GetCashDeskController(bestCashDeskObj);
                if (cashDeskController != null)
                {
                    _targetCashDesk = cashDeskController;
                    // НЕ резервируем здесь, резервирование происходит в StartGoingToCashier
                    // Просто устанавливаем destination к концу очереди
                    Vector3 endOfQueue = cashDeskController.GetEndOfQueuePosition();
                    _locomotion.SetDestination(endOfQueue);
                }
            }
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
        
        public CustomerData GetCustomerData()
        {
            return _customerData;
        }
        
        public void StartPaymentProcess(MonoBehaviour cashDesk)
        {
            // Вызывается кассой когда подошла наша очередь
            Debug.Log($"[CustomersDebug] CustomerController.StartPaymentProcess: Customer {_customerData.CustomerName} called to start payment process. Current state: {_customerData.CurrentState}");
            _targetCashDesk = cashDesk;
            ChangeState(CustomerState.PlacingItemsOnBelt);
            _isWaitingInQueue = false;
            StartCoroutine(PlaceItemsOnBeltCoroutine());
        }
        
        public void CompletePayment()
        {
            // Вызывается кассой после завершения оплаты
            // Но мы переходим к следующему состоянию только если анимация завершена
            if (!_paymentProcessed)
            {
                // Если анимация еще не завершена, откладываем переход
                StartCoroutine(WaitForPaymentAnimationAndLeave());
            }
            else
            {
                Debug.Log($"Customer {_customerData.CustomerName} completed payment");
                
                // Можем проиграть анимацию благодарности/прощания
                _locomotion.PlayWaveAnimation();
                
                ChangeState(CustomerState.Leaving);
            }
        }
        
        private System.Collections.IEnumerator PlaceItemsOnBeltCoroutine()
        {
            Debug.Log($"[ShelfRemovalDebug] CustomerController.PlaceItemsOnBeltCoroutine: Starting belt placement for customer {_customerData.CustomerName}");
            
            var cashDeskController = _targetCashDesk as CashDeskController;
            if (cashDeskController == null)
            {
                Debug.LogError("[ShelfRemovalDebug] Customer is at a cash desk but CashDeskController is not found!", this);
                ChangeState(CustomerState.Paying); // Fallback
                yield break;
            }

            // Даем небольшую паузу перед началом выкладки
            yield return new WaitForSeconds(0.5f);

            Debug.Log($"[ShelfRemovalDebug] CustomerController.PlaceItemsOnBeltCoroutine: Starting to place {_customerData.ShoppingList.Count} shopping items for customer {_customerData.CustomerName}");
            
            // Сначала проверим, что у нас есть товары для выкладки
            bool hasItemsToPlace = false;
            foreach (var item in _customerData.ShoppingList)
            {
                if (item.CollectedQuantity > 0)
                {
                    hasItemsToPlace = true;
                    Debug.Log($"[ShelfRemovalDebug] Customer {_customerData.CustomerName} has {item.CollectedQuantity}x {item.Product.ProductName} to place (Price: ${item.PurchasePrice:F2})");
                }
            }
            
            if (!hasItemsToPlace)
            {
                Debug.LogError($"[ShelfRemovalDebug] Customer {_customerData.CustomerName} has NO items to place on belt! This should not happen if customer reached PlacingItemsOnBelt state.");
                ChangeState(CustomerState.Paying); // Fallback to prevent getting stuck
                yield break;
            }

            foreach (var item in _customerData.ShoppingList)
            {
                if (item.CollectedQuantity > 0)
                {
                    Debug.Log($"[ShelfRemovalDebug] CustomerController.PlaceItemsOnBeltCoroutine: Placing {item.CollectedQuantity}x {item.Product.ProductName} for customer {_customerData.CustomerName}");
                    
                    for (int i = 0; i < item.CollectedQuantity; i++)
                    {
                        // Проверяем, не сменилось ли состояние во время долгой выкладки
                        if (_customerData.CurrentState != CustomerState.PlacingItemsOnBelt)
                        {
                            Debug.LogWarning($"[ShelfRemovalDebug] CustomerController.PlaceItemsOnBeltCoroutine: State changed while placing items for {_customerData.CustomerName}. Aborting.", this);
                            yield break;
                        }
                        
                        // Команда кассе выложить товар
                        Debug.Log($"[ShelfRemovalDebug] Calling PlaceItemOnBelt for {item.Product.ProductName} (item {i+1}/{item.CollectedQuantity})");
                        cashDeskController.PlaceItemOnBelt(item.Product);
                        
                        // Проигрываем анимацию, похожую на взятие товара
                        _locomotion.PlayPickupAnimation();
                        
                        // Ждем завершения анимации
                        float animationStartTime = Time.time;
                        yield return new WaitUntil(() => _locomotion.IsPickupAnimationComplete() || (Time.time - animationStartTime) > _animationTimeout);
                        
                        if ((Time.time - animationStartTime) > _animationTimeout)
                        {
                             Debug.LogWarning($"[ShelfRemovalDebug] Customer {_customerData.CustomerName}: Item placement animation timed out.", this);
                        }
                        
                        _locomotion.ResetPickupTrigger();
                        // Небольшая пауза между товарами
                        yield return new WaitForSeconds(0.2f);
                    }
                }
            }
            
            // Завершили выкладку, переходим к оплате
            Debug.Log($"[ShelfRemovalDebug] Customer {_customerData.CustomerName} finished placing items, proceeding to payment.");
            ChangeState(CustomerState.Paying);
        }
        
        private System.Collections.IEnumerator WaitForPaymentAnimationAndLeave()
        {
            // Ждем пока анимация завершится или наступит таймаут
            while (!_paymentProcessed && _stateTimer < _animationTimeout)
            {
                yield return null;
            }
            
            Debug.Log($"Customer {_customerData.CustomerName} completed payment (after animation)");
            
            // Можем проиграть анимацию благодарности/прощания
            _locomotion.PlayWaveAnimation();
            
            ChangeState(CustomerState.Leaving);
        }
        
        /// <summary>
        /// Дополнительная проверка для запуска обслуживания после присоединения к очереди
        /// Решает race condition проблемы
        /// </summary>
        private System.Collections.IEnumerator CheckServiceAfterJoiningQueue(CashDeskController cashDesk)
        {
            // Ждем небольшую задержку для синхронизации
            yield return new WaitForSeconds(0.2f);
            
            // Принудительно проверяем и запускаем обслуживание если нужно
            // но только если покупатель все еще ждет в очереди
            if (cashDesk != null && _customerData.CurrentState == CustomerState.WaitingInQueue)
            {
                cashDesk.CheckAndStartProcessing();
            }
        }
        
        public void UpdateQueuePosition(Vector3 newPosition, int positionInQueue)
        {
            // Вызывается кассой для обновления позиции в очереди
            Vector3 oldPosition = _queuePosition;
            _queuePosition = newPosition;
            _isWaitingInQueue = true;
            
            if (_customerData.CurrentState == CustomerState.WaitingInQueue)
            {
                // Если позиция изменилась существенно, возобновляем движение
                float distanceChange = Vector3.Distance(oldPosition, newPosition);
                if (distanceChange > 0.1f)
                {
                    // Устанавливаем новую цель движения
                    _locomotion.SetDestination(newPosition);
                    // Возобновляем движение к новой позиции
                    _locomotion.Resume();
                    Debug.Log($"Customer {_customerData.CustomerName} moving to new queue position {positionInQueue}");
                }
            }
        }
        
        // Методы для сохранения/восстановления состояния
        public float GetStateTimer()
        {
            return _stateTimer;
        }
        
        public bool GetIsInQueue()
        {
            return _isWaitingInQueue;
        }
        
        public Vector3 GetQueuePosition()
        {
            return _queuePosition;
        }
        
        public int GetQueuePositionIndex()
        {
            // Если есть ссылка на кассу и клиент в очереди, получаем позицию от кассы
            if (_targetCashDesk != null && _isWaitingInQueue)
            {
                var cashDesk = _targetCashDesk as CashDeskController;
                if (cashDesk != null)
                {
                    // Нужно запросить позицию в очереди у кассы
                    return cashDesk.GetCustomerQueuePosition(gameObject);
                }
            }
            return -1; // Не в очереди
        }
        
        public string GetCashDeskId()
        {
            if (_targetCashDesk != null)
            {
                var cashDeskController = _targetCashDesk as CashDeskController;
                if (cashDeskController != null)
                {
                    // Получаем реальный ID кассы, а не имя GameObject
                    return cashDeskController.GetCashDeskID();
                }
            }
            return null;
        }

        public Vector3 GetCashDeskPosition()
        {
            if (_targetCashDesk != null && _targetCashDesk.gameObject != null)
            {
                return _targetCashDesk.gameObject.transform.position;
            }
            return Vector3.zero;
        }
        
        public bool GetPickupAnimationPlayed()
        {
            return _pickupAnimationPlayed;
        }
        
        public bool GetPayAnimationPlayed()
        {
            return _payAnimationPlayed;
        }
        
        public bool GetPaymentProcessed()
        {
            return _paymentProcessed;
        }
        
        public string GetTargetShelfId()
        {
            if (_targetShelf != null && _targetShelf.gameObject != null)
            {
                return _targetShelf.gameObject.name;
            }
            return null;
        }
        
        public int GetCurrentShoppingItemIndex()
        {
            if (_currentShoppingItem == null) return -1;
            
            for (int i = 0; i < _customerData.ShoppingList.Count; i++)
            {
                if (_customerData.ShoppingList[i] == _currentShoppingItem)
                {
                    return i;
                }
            }
            return -1;
        }
        
        public Vector3 GetTargetPosition()
        {
            if (_locomotion != null && _locomotion.GetComponent<NavMeshAgent>() != null)
            {
                return _locomotion.GetComponent<NavMeshAgent>().destination;
            }
            return transform.position;
        }
        
        public bool HasTarget()
        {
            if (_locomotion != null && _locomotion.GetComponent<NavMeshAgent>() != null)
            {
                return _locomotion.GetComponent<NavMeshAgent>().hasPath;
            }
            return false;
        }
        
        public Vector3 GetPersonalExitPosition()
        {
            return _personalExitPosition;
        }
        
        public bool HasPersonalExitPosition()
        {
            // Проверяем, что _personalExitPosition не является нулевым или дефолтным вектором
            return _personalExitPosition != Vector3.zero;
        }
        
        // Методы для сохранения данных уличных прогулок
        public string GetCurrentWaypointId()
        {
            return _currentWaypoint != null ? _currentWaypoint.name : null;
        }
        
        public float GetWaypointWaitTimer()
        {
            return _waypointWaitTimer;
        }
        
        public bool GetIsWaitingAtWaypoint()
        {
            return _isWaitingAtWaypoint;
        }
        
        /// <summary>
        /// Восстанавливает внутреннее состояние контроллера из сохраненных данных
        /// </summary>
        public void RestoreState(CustomerSaveData saveData)
        {
            try
            {
                // Восстанавливаем внутренние переменные
                _stateTimer = saveData.StateTimer;
                _isWaitingInQueue = saveData.IsInQueue;
                _queuePosition = saveData.QueueWorldPosition;
                _pickupAnimationPlayed = saveData.PickupAnimationPlayed;
                _payAnimationPlayed = saveData.PayAnimationPlayed;
                _paymentProcessed = saveData.PaymentProcessed;
                
                // Восстанавливаем цель движения
                if (saveData.HasTarget)
                {
                    _locomotion?.SetDestination(saveData.TargetPosition);
                }
                
                // Восстанавливаем ссылку на полку (если есть)
                if (!string.IsNullOrEmpty(saveData.TargetShelfId))
                {
                    GameObject shelfObj = GameObject.Find(saveData.TargetShelfId);
                    if (shelfObj != null)
                    {
                        _targetShelf = shelfObj.GetComponent<ShelfController>();
                        if (_targetShelf == null)
                        {
                            _targetShelf = shelfObj.GetComponentInParent<ShelfController>();
                        }
                        if (_targetShelf == null)
                        {
                            _targetShelf = shelfObj.GetComponentInChildren<ShelfController>();
                        }
                    }
                }
                
                // Восстанавливаем ссылку на кассу (если есть)
                if (!string.IsNullOrEmpty(saveData.CashDeskId))
                {
                    Debug.Log($"[CustomersDebug] CustomerController.RestoreState: Restoring cash desk connection for customer {_customerData.CustomerName}, CashDeskId: {saveData.CashDeskId}, IsInQueue: {saveData.IsInQueue}, QueuePosition: {saveData.QueuePosition}");
                    
                    Debug.Log($"[CustomersDebug] CustomerController.RestoreState: Looking for cash desk with ID '{saveData.CashDeskId}' for customer {_customerData.CustomerName}");
                    
                    // Store the cash desk ID for delayed restoration if needed
                    _savedCashDeskId = saveData.CashDeskId;
                    _savedQueuePosition = saveData.QueuePosition;
                    _savedIsInQueue = saveData.IsInQueue;
                    
                    GameObject cashDeskObj = _cashDeskService?.FindCashDeskById(saveData.CashDeskId);
                    if (cashDeskObj != null)
                    {
                        Debug.Log($"[CustomersDebug] CustomerController.RestoreState: Found cash desk '{cashDeskObj.name}' with ID '{saveData.CashDeskId}' for customer {_customerData.CustomerName}");
                        var cashDeskController = GetCashDeskController(cashDeskObj);
                        if (cashDeskController != null)
                        {
                            Debug.Log($"[CustomersDebug] CustomerController.RestoreState: Found CashDeskController for customer {_customerData.CustomerName}");
                            _targetCashDesk = cashDeskController;
                            
                            // Если покупатель был в очереди, восстанавливаем позицию
                            if (saveData.IsInQueue)
                            {
                                // Если позиция некорректная (-1), восстанавливаем в конец очереди
                                int positionToRestore = saveData.QueuePosition >= 0 ? saveData.QueuePosition : 0;
                                Debug.Log($"[CustomersDebug] CustomerController.RestoreState: About to call RestoreCustomerInQueue for customer {_customerData.CustomerName}, position {positionToRestore} (original: {saveData.QueuePosition})");
                                try
                                {
                                    cashDeskController.RestoreCustomerInQueue(gameObject, positionToRestore);
                                    Debug.Log($"[CustomersDebug] CustomerController: Successfully called RestoreCustomerInQueue for customer {_customerData.CustomerName}");
                                    _cashDeskRestored = true;
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[CustomersDebug] CustomerController: Exception calling RestoreCustomerInQueue for customer {_customerData.CustomerName}: {ex.Message}");
                                }
                            }
                            else
                            {
                                Debug.Log($"[CustomersDebug] CustomerController.RestoreState: Customer {_customerData.CustomerName} has cash desk but not in queue");
                                _cashDeskRestored = true;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[CustomersDebug] CustomerController.RestoreState: CashDeskController not found on cash desk '{cashDeskObj.name}' for customer {_customerData.CustomerName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomersDebug] CustomerController.RestoreState: Cash desk with ID '{saveData.CashDeskId}' not found for customer {_customerData.CustomerName}. Will retry during initialization.");
                    }
                }
                
                // Восстанавливаем текущий элемент списка покупок
                if (saveData.CurrentShoppingItemIndex >= 0 && saveData.CurrentShoppingItemIndex < _customerData.ShoppingList.Count)
                {
                    _currentShoppingItem = _customerData.ShoppingList[saveData.CurrentShoppingItemIndex];
                }
                
                // Восстанавливаем точку выхода из сервиса
                if (_storePointsService != null)
                {
                    _exitPoint = _storePointsService.ExitPoint;
                    if (_exitPoint == null)
                    {
                        Debug.LogWarning($"CustomerController: Exit point not set in StorePointsService during restore for '{_customerData.CustomerName}'!");
                    }
                }
                else
                {
                    Debug.LogError($"CustomerController: StorePointsService not injected during restore for '{_customerData.CustomerName}'!");
                }
                
                // Восстанавливаем персональную позицию выхода (если была сохранена)
                if (saveData.HasPersonalExitPosition)
                {
                    _personalExitPosition = saveData.PersonalExitPosition;
                    Debug.Log($"CustomerController: Restored personal exit position {_personalExitPosition} for '{_customerData.CustomerName}'");
                }
                
                // Восстанавливаем данные уличных прогулок
                if (!string.IsNullOrEmpty(saveData.CurrentWaypointId) && _streetWaypointService != null)
                {
                    // Ищем waypoint по имени
                    var waypoints = _streetWaypointService.GetAllWaypoints();
                    _currentWaypoint = waypoints.Find(wp => wp.name == saveData.CurrentWaypointId);
                    if (_currentWaypoint != null)
                    {
                        Debug.Log($"CustomerController: Restored current waypoint '{_currentWaypoint.name}' for '{_customerData.CustomerName}'");
                    }
                    else
                    {
                        Debug.LogWarning($"CustomerController: Could not find waypoint '{saveData.CurrentWaypointId}' for '{_customerData.CustomerName}'");
                    }
                }
                
                _waypointWaitTimer = saveData.WaypointWaitTimer;
                _isWaitingAtWaypoint = saveData.IsWaitingAtWaypoint;
                
                Debug.Log($"CustomerController: Restored state for '{_customerData.CustomerName}' - State: {_customerData.CurrentState}, Timer: {_stateTimer}, InQueue: {_isWaitingInQueue}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CustomerController: Failed to restore state for '{_customerData.CustomerName}': {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Инициализирует состояние покупателя после восстановления из сохранения
        /// Вызывает необходимые методы инициализации для текущего состояния
        /// </summary>
        public void InitializeRestoredState()
        {
            Debug.Log($"[CustomersDebug] CustomerController.InitializeRestoredState: Initializing state {_customerData.CurrentState} for customer {_customerData.CustomerName}");
            
            // Try to restore cash desk connection if it wasn't successful during RestoreState
            if (!_cashDeskRestored && !string.IsNullOrEmpty(_savedCashDeskId))
            {
                Debug.Log($"[CustomersDebug] CustomerController.InitializeRestoredState: Attempting delayed cash desk restoration for customer {_customerData.CustomerName}, CashDeskId: {_savedCashDeskId}");
                GameObject cashDeskObj = _cashDeskService?.FindCashDeskById(_savedCashDeskId);
                if (cashDeskObj != null)
                {
                    Debug.Log($"[CustomersDebug] CustomerController.InitializeRestoredState: Found cash desk '{cashDeskObj.name}' on retry for customer {_customerData.CustomerName}");
                    var cashDeskController = GetCashDeskController(cashDeskObj);
                    if (cashDeskController != null)
                    {
                        _targetCashDesk = cashDeskController;
                        
                        // Если покупатель был в очереди, восстанавливаем позицию
                        if (_savedIsInQueue)
                        {
                            int positionToRestore = _savedQueuePosition >= 0 ? _savedQueuePosition : 0;
                            Debug.Log($"[CustomersDebug] CustomerController.InitializeRestoredState: Calling RestoreCustomerInQueue for customer {_customerData.CustomerName}, position {positionToRestore} (original: {_savedQueuePosition})");
                            try
                            {
                                cashDeskController.RestoreCustomerInQueue(gameObject, positionToRestore);
                                Debug.Log($"[CustomersDebug] CustomerController.InitializeRestoredState: Successfully restored queue for customer {_customerData.CustomerName}");
                                _cashDeskRestored = true;
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[CustomersDebug] CustomerController.InitializeRestoredState: Exception restoring queue for customer {_customerData.CustomerName}: {ex.Message}");
                            }
                        }
                        else
                        {
                            _cashDeskRestored = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomersDebug] CustomerController.InitializeRestoredState: CashDeskController not found on retry for customer {_customerData.CustomerName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CustomersDebug] CustomerController.InitializeRestoredState: Still cannot find cash desk with ID '{_savedCashDeskId}' for customer {_customerData.CustomerName}");
                }
            }
            
            switch (_customerData.CurrentState)
            {
                case CustomerState.StreetWalking:
                    StartStreetWalking();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized StreetWalking state for restored customer '{_customerData.CustomerName}'");
                    break;
                case CustomerState.ConsideringStore:
                    StartConsideringStore();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized ConsideringStore state for restored customer '{_customerData.CustomerName}'");
                    break;
                case CustomerState.Entering:
                    StartEntering();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized Entering state for restored customer '{_customerData.CustomerName}'");
                    break;
                case CustomerState.Shopping:
                    StartShopping();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized Shopping state for restored customer '{_customerData.CustomerName}'");
                    break;
                case CustomerState.GoingToShelf:
                    StartGoingToShelf();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized GoingToShelf state for restored customer '{_customerData.CustomerName}'");
                    break;
                case CustomerState.GoingToCashier:
                    StartGoingToCashier();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized GoingToCashier state for restored customer '{_customerData.CustomerName}'");
                    break;
                case CustomerState.Leaving:
                    // Для состояния Leaving особенно важно правильно инициализировать движение к выходу
                    StartLeaving();
                    Debug.Log($"[CustomersDebug] CustomerController: Initialized Leaving state for restored customer '{_customerData.CustomerName}'");
                    break;
                // Для других состояний инициализация может не требоваться или быть специфичной
                case CustomerState.TakingItem:
                    Debug.Log($"[CustomersDebug] CustomerController: TakingItem state for restored customer '{_customerData.CustomerName}' - no special initialization");
                    break;
                case CustomerState.JoiningQueue:
                    Debug.Log($"[CustomersDebug] CustomerController: JoiningQueue state for restored customer '{_customerData.CustomerName}' - no special initialization");
                    break;
                case CustomerState.WaitingInQueue:
                    // Для состояния WaitingInQueue важно инициализировать состояние ожидания
                    if (_targetCashDesk != null)
                    {
                        _isWaitingInQueue = true;
                        _locomotion?.Stop();
                        // Устанавливаем правильное направление взгляда на кассу
                        if (_targetCashDesk.transform != null)
                        {
                            _locomotion?.FaceTarget(_targetCashDesk.transform, false);
                        }
                        Debug.Log($"[CustomersDebug] CustomerController: Initialized WaitingInQueue state for restored customer '{_customerData.CustomerName}'");
                        
                        // Проверяем и запускаем обслуживание, если игрок уже за кассой
                        var cashDesk = _targetCashDesk as CashDeskController;
                        if (cashDesk != null)
                        {
                            Debug.Log($"[CustomersDebug] CustomerController: Calling CheckAndStartProcessing for restored customer '{_customerData.CustomerName}'");
                            cashDesk.CheckAndStartProcessing();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomersDebug] CustomerController: Customer '{_customerData.CustomerName}' restored in WaitingInQueue state but has no target cash desk!");
                        
                        // Если касса не найдена, но у нас есть сохраненные данные, запускаем отложенное восстановление
                        if (!string.IsNullOrEmpty(_savedCashDeskId))
                        {
                            Debug.Log($"[CustomersDebug] CustomerController: Starting delayed queue restoration coroutine for customer '{_customerData.CustomerName}'");
                            StartCoroutine(DelayedQueueRestoration());
                        }
                    }
                    break;
                case CustomerState.PlacingItemsOnBelt:
                    // Для PlacingItemsOnBelt нужно возобновить процесс выкладки товаров
                    if (_targetCashDesk != null)
                    {
                        _isWaitingInQueue = false;
                        Debug.Log($"[CustomersDebug] CustomerController: Starting PlaceItemsOnBeltCoroutine for restored customer '{_customerData.CustomerName}'");
                        StartCoroutine(PlaceItemsOnBeltCoroutine());
                        Debug.Log($"[CustomersDebug] CustomerController: Initialized PlacingItemsOnBelt state for restored customer '{_customerData.CustomerName}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomersDebug] CustomerController: Customer '{_customerData.CustomerName}' restored in PlacingItemsOnBelt state but has no target cash desk!");
                        ChangeState(CustomerState.Shopping);
                    }
                    break;
                case CustomerState.Paying:
                    // Эти состояния требуют дополнительного контекста и могут быть проблематичными для восстановления
                    Debug.LogWarning($"[CustomersDebug] CustomerController: Customer '{_customerData.CustomerName}' restored in complex state {_customerData.CurrentState}, may need manual handling");
                    break;
            }
        }
        
        /// <summary>
        /// Генерирует список покупок для клиента, который решил зайти в магазин
        /// </summary>
        private void GenerateShoppingList()
        {
            if (_shoppingListGeneratorService != null && _customerData != null)
            {
                _customerData.ShoppingList = _shoppingListGeneratorService.GenerateShoppingListWithPreferences(
                    _customerData.Money,
                    _customerData.Gender
                );
                
                Debug.Log($"Customer {_customerData.CustomerName}: Generated shopping list with {_customerData.ShoppingList.Count} items:");
                foreach (var item in _customerData.ShoppingList)
                {
                    Debug.Log($"  - {item.Product.ProductName} x{item.DesiredQuantity}");
                }
            }
            else
            {
                Debug.LogWarning($"Customer {_customerData?.CustomerName}: Cannot generate shopping list - service or data missing");
                
                // Создаем пустой список как fallback
                if (_customerData != null)
                {
                    _customerData.ShoppingList = new List<ShoppingItem>();
                }
            }
        }
        
        /// <summary>
        /// Корутина для отложенного восстановления очереди касс
        /// Запускается когда касса не найдена во время инициализации, но есть сохраненные данные
        /// </summary>
        private System.Collections.IEnumerator DelayedQueueRestoration()
        {
            Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Starting delayed restoration for customer '{_customerData.CustomerName}' with CashDeskId '{_savedCashDeskId}'");
            
            // Ждем некоторое время, чтобы дать кассам время зарегистрироваться
            int maxAttempts = 10;
            int attempt = 0;
            
            while (attempt < maxAttempts && !_cashDeskRestored)
            {
                yield return new WaitForSeconds(0.5f); // Ждем 0.5 секунды между попытками
                attempt++;
                
                Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Attempt {attempt}/{maxAttempts} for customer '{_customerData.CustomerName}'");
                
                GameObject cashDeskObj = _cashDeskService?.FindCashDeskById(_savedCashDeskId);
                if (cashDeskObj != null)
                {
                    Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Found cash desk '{cashDeskObj.name}' on attempt {attempt} for customer '{_customerData.CustomerName}'");
                    var cashDeskController = GetCashDeskController(cashDeskObj);
                    if (cashDeskController != null)
                    {
                        _targetCashDesk = cashDeskController;
                        
                        // Если покупатель был в очереди, восстанавливаем позицию
                        if (_savedIsInQueue)
                        {
                            int positionToRestore = _savedQueuePosition >= 0 ? _savedQueuePosition : 0;
                            Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Calling RestoreCustomerInQueue for customer '{_customerData.CustomerName}', position {positionToRestore}");
                            try
                            {
                                cashDeskController.RestoreCustomerInQueue(gameObject, positionToRestore);
                                Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Successfully restored queue for customer '{_customerData.CustomerName}'");
                                _cashDeskRestored = true;
                                
                                // Инициализируем состояние ожидания в очереди
                                _isWaitingInQueue = true;
                                _locomotion?.Stop();
                                if (_targetCashDesk.transform != null)
                                {
                                    _locomotion?.FaceTarget(_targetCashDesk.transform, false);
                                }
                                
                                // Проверяем необходимость запуска обслуживания
                                var cashDesk = _targetCashDesk as CashDeskController;
                                if (cashDesk != null)
                                {
                                    Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Calling CheckAndStartProcessing for restored customer '{_customerData.CustomerName}'");
                                    cashDesk.CheckAndStartProcessing();
                                }
                                
                                yield break; // Успешно восстановили, выходим
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Exception restoring queue for customer '{_customerData.CustomerName}': {ex.Message}");
                            }
                        }
                        else
                        {
                            _cashDeskRestored = true;
                            yield break; // Касса найдена, но покупатель не в очереди
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CustomersDebug] CustomerController.DelayedQueueRestoration: CashDeskController not found on attempt {attempt} for customer '{_customerData.CustomerName}'");
                    }
                }
                else
                {
                    Debug.Log($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Cash desk '{_savedCashDeskId}' still not found on attempt {attempt} for customer '{_customerData.CustomerName}'");
                }
            }
            
            if (!_cashDeskRestored)
            {
                Debug.LogError($"[CustomersDebug] CustomerController.DelayedQueueRestoration: Failed to restore cash desk after {maxAttempts} attempts for customer '{_customerData.CustomerName}'. Changing state to Shopping.");
                // Если не удалось восстановить кассу, переводим покупателя обратно в состояние покупок
                ChangeState(CustomerState.Shopping);
            }
        }

        void OnDestroy()
        {
            // Отменяем резервирование при уничтожении
            if (_targetCashDesk != null)
            {
                var cashDeskController = _targetCashDesk as CashDeskController;
                cashDeskController?.CancelApproachingSpot(gameObject);
            }
        }
        
        /// <summary>
        /// Вызывается когда касса, к которой идет/стоит клиент, была продана/уничтожена
        /// </summary>
        public void OnCashDeskDestroyed()
        {
            Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Cash desk destroyed! Current state: {_customerData.CurrentState}");
            
            // Сбрасываем связь с кассой
            _targetCashDesk = null;
            _isWaitingInQueue = false;
            
            // Обрабатываем в зависимости от текущего состояния
            switch (_customerData.CurrentState)
            {
                case CustomerState.GoingToCashier:
                case CustomerState.JoiningQueue:
                case CustomerState.WaitingInQueue:
                    // Если клиент был в процессе похода к кассе или в очереди
                    Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Searching for alternative cash desk");
                    
                    // Пытаемся найти другую кассу
                    FindBestCashDesk();
                    
                    if (_targetCashDesk != null)
                    {
                        // Нашли другую кассу - идем к ней
                        Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Found alternative cash desk, going there");
                        ChangeState(CustomerState.GoingToCashier);
                    }
                    else
                    {
                        // Других касс нет - продолжаем покупки или уходим
                        Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: No alternative cash desk found");
                        
                        bool hasItems = HasItemsInCart();
                        if (hasItems)
                        {
                            // ИСПРАВЛЕНО: Есть товары, но нет касс - переходим в специальное состояние ожидания кассы
                            Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Has items but no cash desk, entering waiting state");
                            ChangeState(CustomerState.Shopping); // Используем Shopping как состояние ожидания кассы
                        }
                        else
                        {
                            // Нет товаров - просто уходим
                            Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: No items, leaving store");
                            ChangeState(CustomerState.Leaving);
                        }
                    }
                    break;
                    
                case CustomerState.PlacingItemsOnBelt:
                case CustomerState.Paying:
                    // Если клиент уже обслуживается - это критическая ошибка!
                    Debug.LogError($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Cash desk destroyed while customer is being served! This should not happen!");
                    
                    // ИСПРАВЛЕНО: Не переводим в Shopping, а создаем специальное состояние ожидания
                    Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Emergency transition to waiting state");
                    ChangeState(CustomerState.Shopping); // Будет ждать новую кассу в UpdateShopping
                    break;
                    
                default:
                    // В других состояниях ничего не делаем
                    Debug.Log($"[CashDeskDestruction] Customer {_customerData.CustomerName}: Cash desk destroyed but customer is in {_customerData.CurrentState} state - no action needed");
                    break;
            }
        }
        
        /// <summary>
        /// Вызывается когда появляется новая касса (для переназначения потерянных клиентов)
        /// </summary>
        public void OnNewCashDeskAvailable()
        {
            Debug.Log($"[CashDeskAssignment] Customer {_customerData.CustomerName}: New cash desk available! Current state: {_customerData.CurrentState}");
            
            // Проверяем, нужно ли клиенту переназначение
            bool needsReassignment = false;
            string reason = "";
            
            if (_customerData.CurrentState == CustomerState.Shopping && HasItemsInCart() && _targetCashDesk == null)
            {
                needsReassignment = true;
                reason = "shopping with items but no target cash desk";
            }
            else if ((_customerData.CurrentState == CustomerState.GoingToCashier || 
                     _customerData.CurrentState == CustomerState.JoiningQueue || 
                     _customerData.CurrentState == CustomerState.WaitingInQueue) && 
                     _targetCashDesk == null)
            {
                needsReassignment = true;
                reason = "going to/waiting at non-existent cash desk";
            }
            
            if (needsReassignment)
            {
                Debug.Log($"[CashDeskAssignment] Customer {_customerData.CustomerName}: Needs reassignment (reason: {reason})");
                
                // Ищем кассу
                FindBestCashDesk();
                
                if (_targetCashDesk != null)
                {
                    Debug.Log($"[CashDeskAssignment] Customer {_customerData.CustomerName}: Found cash desk, going there");
                    ChangeState(CustomerState.GoingToCashier);
                }
                else
                {
                    Debug.LogWarning($"[CashDeskAssignment] Customer {_customerData.CustomerName}: Still no cash desk found after reassignment attempt");
                }
            }
            else
            {
                Debug.Log($"[CashDeskAssignment] Customer {_customerData.CustomerName}: No reassignment needed (state: {_customerData.CurrentState}, has items: {HasItemsInCart()}, has target: {_targetCashDesk != null})");
            }
        }
    }
} 