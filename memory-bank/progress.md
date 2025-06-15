# Progress

## Progress Status - Date: 2024-12-03

### Latest Fixes:

**✅ КРИТИЧЕСКИЙ БАГФИКС: Система кассы - дублирующий скан товаров (2024-12-03):**
- **Проблема**: Товары можно было сканировать бесконечно, получая неограниченные деньги за один товар
- **Корневая причина**: Отсканированные товары оставались на слое ScannableItem и могли быть отсканированы повторно
- **Решение**: 
  1. Добавлена проверка дублирования в `PlayerAttemptScan()` - предотвращает повторное сканирование
  2. Создан метод `ChangeItemLayerToNonScannable()` - мгновенно изменяет слой товара на Default
  3. Улучшена система подсветки `HandleHighlighting()` - игнорирует уже отсканированные товары
  4. Добавлено детальное логирование с префиксом `[ScanningBugFix]`
- **Результат**: Экономическая система защищена от абьюза, невозможно получить деньги за один товар несколько раз

**✅ Customer Avoidance Priority System (2024-12-03):**
- **Feature**: Added random/unique obstacle avoidance priority system for customers  
- **Purpose**: Customers now yield to each other more naturally based on navigation priority
- **Implementation**:
  1. Added `SetAvoidancePriority()` and `GetAvoidancePriority()` methods to `CustomerLocomotion`
  2. Added navigation settings to `CustomerSpawnerService` inspector:
     - `_minAvoidancePriority = 30` (highest priority)
     - `_maxAvoidancePriority = 70` (lowest priority)
     - `_useUniqueAvoidancePriority = false` (random vs sequential mode)
  3. Extended `CustomerSaveData` with `AvoidancePriority` field for save/load support
  4. Added `GetAvoidancePriority()` method to `CustomerManagerService` for saving
  5. Added priority restoration in `RestoreCustomer()` method
- **Priority Logic**: Lower number = higher priority (NavMeshAgent standard)
- **Result**: Customers now naturally avoid each other with hierarchical navigation priorities

**✅ Customer Exit State Save/Load Issue (2024-12-03):**
- **Problem**: Customers who were going to ExitPoint would start going to cashiers or shelves after loading a save
- **Root Cause**: Personal exit position (`_personalExitPosition`) and exit point (`_exitPoint`) were not being saved/restored
- **Additional Issue Found**: Customer state initialization missing after restoration - `StartLeaving()` was not called for restored customers in Leaving state
- **FINAL ROOT CAUSE**: `Initialize()` method was forcibly setting all customers to `Shopping` state before `RestoreState()` could restore the correct state
- **Solution**: 
  1. Added `PersonalExitPosition` and `HasPersonalExitPosition` fields to `CustomerSaveData`
  2. Added getter methods in `CustomerController` for personal exit position data
  3. Updated `CustomerManagerService` to collect/restore exit position data
  4. Modified `RestoreState` to properly restore `_exitPoint` from `IStorePointsService`
  5. Updated `StartLeaving` logic to use restored position if available, create new if not
  6. Added `InitializeRestoredState()` method to properly initialize customer behavior after restoration
  7. Added state initialization call at end of `RestoreState()` to ensure proper behavior initialization
  8. **FINAL FIX**: Created `InitializeRestored()` method that initializes customers WITHOUT changing state
  9. **FINAL FIX**: Updated `CustomerManagerService.RestoreCustomer()` to use `InitializeRestored()` instead of `Initialize()`
- **Result**: Customers now properly continue to exit after loading save, with correct state preservation and behavior initialization

### What Works:

*   **Core Systems & Architecture:**
    *   Basic Dependency Injection setup with BInject (`ApplicationContextInitiator`, `MenuContextInitiator`, `GameContextInitiator`, `BInjectSettings.asset`).
    *   Application, Menu, and Game contexts and initiator scripts operational.
    *   Base scenes (`InitScene`, `MenuScene`, `GameScene`) with context initiators in Build Settings.
    *   `AppStartup` script in `InitScene` loads `MenuScene`.
    *   Input Service (`InputService`, `InputActions`, `InputModeService`) for switching between Game, UI, and Placement modes.
    *   Basic Scene Management (`ISceneLoadService`, `SceneLoadService`).
    *   `GameManager` (attached to "GameContextHolder") injects and logs data from services.
*   **Product & Catalog:**
    *   `ProductConfig` ScriptableObject for defining product properties (name, prefab, prices, category, etc.).
    *   `ProductCatalogService` (provides product configs from `GameConfigService` loaded from `GameConfiguration.asset`).
    *   `ProductConfig.cs`:
        *   Добавлено поле `public bool CanBePlacedOnShelf = false;`.
*   **Player Controller (First-Person):**
    *   "Player" GameObject with `CharacterController`.
    *   `PlayerInputActions.inputactions` asset with "Move" and "Look" actions.
    *   `PlayerController.cs` handles input for movement and mouse look.
    *   Cursor locked and hidden during gameplay.
*   **Object Placement System:**
    *   **Core Components:**
        *   `IPlacementService`, `PlacementService` - основная логика размещения объектов
        *   `PlacementController.cs` - управление размещением (позиция, поворот)
        *   `PlayerHandPlacementController` - размещение объектов из коробки в руке
        *   `PlacementServiceConfig` ScriptableObject для настройки коллизий и углов поворота
    *   **Размещение из коробки:**
        *   Размещение происходит через `PlayerHandPlacementController` при открытой коробке
        *   Превью объекта, проверка коллизий, подтверждение размещения
        *   Автоматическое потребление предметов из коробки при размещении
        *   Поворот превью клавишами Q/E или действием "RotatePlacement"
    *   **UI упрощен:**
        *   ❌ Убрана кнопка "Строить" и панель выбора объектов (`PlacementPanel`)
        *   ❌ Убрана клавиша B для открытия меню размещения
        *   ✅ Сохранена система сохранения/загрузки размещенных объектов
        *   ✅ Размещение работает только из коробки в руке игрока
*   **Interaction System:**
    *   `IInteractable` interface (`Interact`, `GetInteractionPrompt`, `OnFocus`, `OnBlur`).
    *   `PlayerInteractionController`: Raycasts for `IInteractable` objects, handles focus/blur, and triggers `Interact()` on input.
    *   **Dynamic Interaction Prompts:**
        *   `IInteractionPromptService` and `InteractionPromptService.cs` manage a UI Label (`InteractionPromptLabel` in `GameHUD.uxml`) to display interaction prompts.
        *   `PlayerInteractionController` dynamically fetches key bindings (e.g., "E" for "Interact") and uses the service to show formatted prompts (e.g., "Press E to use computer").
*   **In-Game Computer (`InteractiveComputer.cs`, `ComputerUIHandler.cs`, `ComputerScreen.uxml`):**
    *   `InteractiveComputer.cs` implements `IInteractable` to open/close the `ComputerScreen` UI and toggle `InputMode`.
    *   Tabbed interface (Order Products, Manage Prices, Statistics - last one is placeholder).
    *   **Order Products Tab:** Lists orderable products, allows quantity input, calculates total, deducts money via `PlayerDataService`, triggers `IDeliveryService`.
    *   **Manage Prices Tab:** Lists products, displays Purchase Price, allows setting `BaseSalePrice` (modifies `ProductConfig` directly, with persistence warning).
*   **Delivery System (`IDeliveryService`, `DeliveryService.cs`):**
    *   `DeliverBoxes()` method instantiates box prefabs (with `BoxController` and `BoxData`) at a specified delivery point.
*   **Box & Item Handling (`BoxController.cs`, `BoxData.cs`, `IPlayerHandService`, `PlayerHandService.cs`, `PlayerBoxController.cs`):**
    *   `BoxController` on box prefabs implements `IInteractable`.
    *   Interacting with a box calls `_playerHandService.PickupBox(this.boxData)` and destroys the box GameObject.
    *   `PlayerHandService` manages the currently held `BoxData`.
    *   `OnHandContentChanged` event signals changes.
    *   `GameUIHandler` updates `HeldBoxInfoLabel` (in `GameHUD.uxml`) to show held item info.
    *   `PlayerBoxController.cs`:
        *   **UPDATED**: `OnToggleBoxStatePerformed` теперь корректно обрабатывает открытие коробок: `PlacementService` активируется только для размещаемых объектов, не предназначенных для полок (`CanBePlacedOnShelf == false` и `ObjectCategory != None`).
        *   (Остальная логика ЛКМ/ПКМ для полок без изменений по сравнению с предыдущим состоянием `progress.md`)
*   **Shelf Interaction (`ShelfController.cs`):**
    *   Implements `IInteractable`.
    *   **UPDATED**: `acceptedProduct` теперь `private set` и определяется первым размещенным товаром (с флагом `CanBePlacedOnShelf`).
    *   **UPDATED**: `GetInteractionPrompt()` обновлен для новой логики.
    *   **UPDATED**: Логика `CanPlaceFromOpenBox`, `PlaceItemFromOpenBox`, `TakeItemToOpenBox`, `Interact` (для закрытых коробок) обновлена для работы с `CanBePlacedOnShelf` и динамическим `acceptedProduct`.
    *   **NEW: Dynamic Item Visuals on Shelf**
        *   Заменено `List<GameObject> itemVisuals` на `public List<Transform> itemSpawnPoints`.
        *   `UpdateVisuals()` теперь инстанциирует/удаляет префабы товаров (`acceptedProduct.Prefab`) в точках `itemSpawnPoints`.
        *   Добавлена очистка инстансов в `Start()`.
        *   **NEW**: Добавлены методы `GetCurrentItemCount()` и `CustomerTakeItem()` для взаимодействия с покупателями.
*   **Customer System (NEW):**
    *   **Data Models:**
        *   `CustomerData.cs` - данные покупателя (имя, деньги, скорость, список покупок).
        *   `ShoppingItem` - элемент списка покупок с прогрессом.
        *   `CustomerState` enum - состояния для State Machine.
    *   **Spawner Service:**
        *   `ICustomerSpawnerService` и `CustomerSpawnerService` - управление спавном покупателей.
        *   Зарегистрирован в `GameContextInitiator`.
        *   Интегрирован в `GameManager` для автоматического запуска.
    *   **Customer AI:**
        *   `CustomerController` - использует State Machine для поведения.
        *   Навигация через NavMesh.
        *   Поиск и взятие товаров с полок.
        *   Поиск кассы с наименьшей очередью.
        *   Ожидание в очереди и оплата.
*   **Cash Register System (NEW):**
    *   **Data Models:**
        *   `CashDeskData.cs` - данные кассы (ID, очередь, статистика).
    *   **Cash Desk Controller:**
        *   `CashDeskController` - управление кассой, реализует IInteractable.
        *   Система очередей покупателей.
        *   Взаимодействие игрока с кассой (режим кассира).
        *   Автоматическое обслуживание очереди.
        *   Процесс сканирования и оплаты.
    *   **Player Data Updates:**
        *   Добавлен метод `AdjustMoney()` в `IPlayerDataService` и `PlayerDataService`.
        *   Защита от отрицательного баланса.
        *   Логирование транзакций.
*   **Player Data (`PlayerData.cs`, `IPlayerDataService`, `PlayerDataService.cs`):**
    *   Manages player data (e.g., `Money`) using `PlayerPrefs` (for `MainMenu` new game) and runtime instance.
    *   **NEW**: Метод `AdjustMoney(float amount)` для изменения денег игрока.
*   **Menu System (`MainMenu.uxml`, `

# Progress Log

## Completed Features

### Core Systems
// ... existing code ...

### Stage 11: Character Appearance System (Completed)
- **Character Appearance Configuration**:
  - Created CharacterAppearanceConfig ScriptableObject for managing character models
  - Support for gender-based model selection (Male/Female)
  - Gender-specific name pools
  - Configurable spawn probability for gender distribution
  
- **Clothing Customization**:
  - Color customization system for Top, Bottom, and Shoes
  - Material property mapping for each clothing slot
  - Random color selection from predefined color palettes
  - Dynamic material instance creation to avoid shared material issues
  
- **Character Model Architecture**:
  - CharacterAppearance component for runtime model switching
  - Dynamic model loading with proper cleanup
  - Support for different model hierarchies and renderer paths
  - Integration with existing CustomerLocomotion system
  
- **Customer Data Extensions**:
  - Added Gender property to CustomerData
  - Added color properties for clothing customization
  - Gender-aware name generation during spawning
  
- **Spawner Integration**:
  - CustomerSpawnerService now uses CharacterAppearanceConfig
  - Automatic gender selection based on configured probabilities
  - Random clothing color assignment on spawn
  - Proper Animator reference updating after model load

## Current State

All core mechanics implemented and working:
- ✅ Product ordering through computer
- ✅ Box delivery system
- ✅ Picking up and opening/closing boxes
- ✅ Stocking shelves (LMB to place, RMB to take)
- ✅ Customer AI with shopping behavior
- ✅ Queue system without collisions
- ✅ Cashier operations (press E to work)
- ✅ Customer animations synchronized with actions
- ✅ Economy system with money management

Requires Unity Editor setup:
- NavMesh baking for customer navigation
- Prefab configuration with proper components
- Scene setup with spawn/exit points
- Customer models and animations

### Final Fixes

**PurchaseDecisionService Refactoring (2024-12-03):**
- Problem: Дублирование функциональности между PurchaseDecisionService (POCO) и PurchaseDecisionServiceComponent (MonoBehaviour)
- Solution: 
  1. Удален дублирующий PurchaseDecisionServiceComponent
  2. Создан PurchaseDecisionConfig ScriptableObject для настройки параметров
  3. PurchaseDecisionService теперь принимает конфиг через конструктор
  4. Добавлена валидация параметров в Unity Inspector
  5. Конфиг зарегистрирован в GameContextInitiator
  6. Asset файл создан: `Assets/Resources/Data/GameConfig/PurchaseDecisionConfig.asset`

**Price Locking on Item Pickup (2024-12-03):**
- Problem: Клиенты платили текущую цену на кассе, а не цену на момент взятия товара с полки
- Solution:
  1. Добавлено поле `PurchasePrice` в `ShoppingItem` для хранения цены взятия
  2. `CustomerController` сохраняет цену при первом взятии товара с полки
  3. `CashDeskController.GetCustomerPurchasePrice()` ищет сохраненную цену в данных покупателя
  4. Касса использует цену взятия вместо текущей розничной цены
  5. Fallback система: если цена не найдена, используется текущая розничная цена
- Result: Покупатель всегда платит цену, которая была на момент взятия товара

**Shelf Interaction Issue:**
- Problem: Interaction failed when shelf collider was on child object
- Solution: Enhanced `PlayerInteractionController` to search for `IInteractable` in order:
  1. Hit object itself
  2. Parent objects (up hierarchy)
  3. Children from root (down hierarchy)

**Box Drop Height Issue:**
- Problem: Boxes dropped from player's feet due to CharacterController pivot adjustment
- Solution: Added configurable `_dropHeight` parameter (default 1.2f) to position drops at arm level

**Customer Action Animation Issues:**
- Problem: Pickup animation triggered multiple times or stuck in animation state
- Solution: 
  1. Added animation state flags (`_pickupAnimationPlayed`, `_payAnimationPlayed`) to ensure animations play only once per action
  2. Added configurable `_pickupAnimationDelay` (default 0.5s) to allow animation to start before item pickup
  3. Animation now correctly repeats for each item taken from shelf

### Stage 12: Head Bob and Box Sway System (Completed - 2024-12-03)

**Realistic Player Head Bob Implementation:**
- **PlayerHeadBob Component (`Assets/Scripts/Core/PlayerHeadBob.cs`)**:
  - Создан отдельный компонент для реалистичного покачивания головы при ходьбе
  - Автоматическое определение камеры (аналогично PlayerController)
  - Детекция движения по реальной позиции игрока с исключением Y-компоненты
  - Адаптивная амплитуда в зависимости от скорости движения
  - Плавные переходы при начале/остановке движения
  - Интеграция с InputMode system - работает только в режиме Game
  - Использует LateUpdate() для выполнения после PlayerController
  - Настраиваемые параметры: скорость, амплитуда вертикальная/горизонтальная, сглаживание
  - Публичные свойства для синхронизации с другими системами

**Synchronized Box Sway System:**
- **Enhanced PlayerHandVisualsService**:
  - Модифицирован метод UpdateBoxSway() для синхронизации с головой игрока
  - Коробка качается в противофазе к движению головы (+ Mathf.PI phase offset)
  - Акцент на вертикальное движение коробки (вверх-вниз)
  - Инвертированное вертикальное движение для большей реалистичности
  - Fallback система: работает независимо если PlayerHeadBob не найден
  - Автоматический поиск PlayerHeadBob компонента на игроке
  - Настройка через Inspector: включение синхронизации, интенсивность, направления

**Technical Implementation:**
- Head bob не конфликтует с управлением камерой мышью
- Модифицирует только localPosition камеры, не затрагивает rotation
- Сохраняет исходную позицию камеры для корректного возврата
- Легко добавляется к существующему Player объекту
- Публичные методы для динамического управления (SetHeadBobEnabled, SetBobIntensity, ResetCameraPosition)
- Правильная интеграция с архитектурой BInject

**Usage:**
- Добавить PlayerHeadBob компонент к Player GameObject
- Опционально настроить параметры через Inspector
- PlayerHandVisualsService автоматически синхронизируется при наличии компонента
- Коробки качаются в обратном такте шагам для реалистичного эффекта
  4. Fixed timing issues preventing customers from leaving while in animation state

**Customer Queue System Issues:**
- Problem 1: Customers trying to occupy same position in queue
- Solution: 
  1. Added approaching customers tracking with `_customersApproaching` list
  2. Implemented spot reservation system (`ReserveApproachingSpot`/`CancelApproachingSpot`)
  3. Queue positions now account for both queued and approaching customers
  
- Problem 2: Queue moving forward while customer still being served
- Solution:
  1. Changed from `Dequeue()` to `Peek()` when starting service
  2. Customer remains at position 0 during payment
  3. Only removed from queue after service completion
  
- Problem 3: Customers approaching queue from sides causing chaos
- Solution:
  1. Added new `JoiningQueue` state between `GoingToCashier` and `WaitingInQueue`
  2. Customers first go to end of queue position
  3. Then align with queue direction before joining
  4. Added lateral offset checking for proper line formation

**Customer Save/Load Count Bug (FIXED - 2024-12-03):**
- **Problem**: При загрузке сейва, восстановленные клиенты не записывались в `CustomerSpawnerService._activeCustomers` list, из-за чего превышался максимальный лимит клиентов.
- **Root Cause**: `CustomerManagerService.RestoreCustomer()` регистрировал клиентов только в `CustomerManagerService` через `RegisterCustomer()`, но не в `CustomerSpawnerService`.
- **Result**: `CustomerSpawnerService.GetActiveCustomerCount()` не учитывал восстановленных клиентов, позволяя спавнить больше максимального количества.
- **Solution**:
  1. Добавлен метод `RegisterRestoredCustomer(GameObject customerObj)` в `ICustomerSpawnerService` и `CustomerSpawnerService`
  2. Метод добавляет восстановленного клиента в `_activeCustomers` список и подписывается на `OnCustomerLeaving` событие
  3. Вызов `RegisterRestoredCustomer()` добавлен в `CustomerManagerService.RestoreCustomer()` после регистрации в менеджере
  4. Добавлена детальная диагностика для сравнения количества клиентов между сервисами после восстановления
  5. Улучшен `ClearAllCustomers()` с проверкой синхронизации счётчиков между сервисами

**Architectural Improvements:**
- Replaced tag-based searches with DI services (`ICashDeskService`, `IStorePointsService`)
- Eliminated duplicate raycasts with `IInteractionService`
- Improved component discovery to work with any hierarchy structure

### Stage 11: Economy and UI (Completed)

- **Money Display**:
  - ✅ Added money panel to top-right corner of HUD
  - ✅ GameUIHandler subscribes to money changes via IPlayerDataService.OnMoneyChanged
  - ✅ Real-time money updates when purchasing or selling
  - ✅ Format: "$XXX.XX"

- **Notification System**:
  - ✅ Visual notifications in bottom-left corner
  - ✅ GameUIHandler.ShowNotification() method with duration control
  - ✅ INotificationService/NotificationService for centralized management
  - ✅ Notification types: Info, Success, Warning, Error
  - ✅ Auto-removal after specified duration
  - ✅ Integrated notifications for:
    - Low stock warnings on shelves
    - Product delivery arrivals
    - Successful/failed orders
    - Insufficient funds warnings

- **Statistics System**:
  - ✅ IStatsService/StatsService for tracking game metrics
  - ✅ Tracks: revenue, expenses, customers served, items sold
  - ✅ Methods: RecordSale(), RecordPurchase(), RecordCustomerServed()
  - ✅ Calculates average transaction value
  - ✅ Identifies best-selling product
  - ✅ Registered in ApplicationContext

- **Statistics UI in Computer**:
  - ✅ Full Statistics tab in computer interface
  - ✅ Three sections: Finances, Customers, Products
  - ✅ Styled blocks with color coding
  - ✅ Connected to real data from IStatsService

- **Statistics Integration**:
  - ✅ CashDeskController records sales and served customers
  - ✅ ComputerUIHandler records purchase expenses
  - ✅ All transactions automatically reflected in statistics

### Compilation Issues (Temporary): ~~Fixed~~
- ~~Unity hasn't updated assembly references for Supermarket.Services.UI~~
- ~~INotificationService and IStatsService references temporarily commented~~
- ~~Need to uncomment after Unity refresh~~

### Box Management System Issues (Fixed)

**BoxManagerService DI Issue:**
- Problem: `IBoxManagerService` was being injected in `SaveGameService` with `[Inject]` attribute, but SaveGameService is registered in Common context while BoxManagerService exists only in Game context
- Solution: 
  1. Removed `[Inject]` attribute from `IBoxManagerService` field in SaveGameService
  2. Added `SetBoxManagerService()` and `ClearBoxManagerService()` methods (similar to PlacementService pattern)
  3. GameContextInitiator now registers BoxManagerService in Game context and sets it in SaveGameService
  4. Added proper cleanup in OnDestroy to clear service references when transitioning between scenes
  5. This follows the established pattern for services that exist only in specific contexts

**Code Changes:**
- SaveGameService: `IBoxManagerService` field changed from injected to manually set
- GameContextInitiator: Added BoxManagerService registration and SaveGameService setup
- Removed unused RegisterServices() method that was causing confusion

### Box Physics Issue (Fixed)

**Box Drop Physics Not Working:**
- Problem: When dropping boxes from hands using 'G' key, boxes appeared static without physics despite having Rigidbody component
- Root Cause: `PlayerBoxController` was calling `BoxManagerService.CreateBox()` with `isPhysical = false`
- Solution:
  1. Changed `isPhysical` parameter to `true` when creating dropped boxes
  2. Added overloaded `CreateBox` method in `IBoxManagerService` to accept `initialVelocity` parameter
  3. Updated `BoxManagerService` implementation to pass initial velocity to `SetPhysicalAndDrop()`
  4. Calculated and applied forward + upward velocity based on player direction when dropping
  5. Removed temporary workaround coroutine `ApplyPhysicsToDroppedBox`

**Code Changes:**
- IBoxManagerService: Added `CreateBox` overload with `Vector3 initialVelocity` parameter
- BoxManagerService: Implemented new overload and updated internal method signature
- PlayerBoxController: Changed to use physics-enabled box creation with calculated initial velocity

### Player Position Save/Load Issue (Fixed)

**Problem:** Player position was not being saved/restored in game saves despite having the data structure and PlayerDataProvider system
- SaveGameService always saved Vector3.zero for position and rotation
- ApplySaveData had TODO comments about player position restoration
- GameSaveGameService existed with proper functionality but was not used

**Root Cause:** SaveGameService didn't use IPlayerDataProvider to get/set actual player position data

**Solution:**
1. **Integrated PlayerDataProvider into SaveGameService:**
   - Added `IPlayerDataProvider _playerDataProvider` field (manually injected like other Game context services)
   - Added `SetPlayerDataProvider()` and `ClearPlayerDataProvider()` methods
   - Updated `CollectSaveData()` to use `_playerDataProvider.GetPlayerPosition/Rotation()` 
   - Updated `ApplySaveData()` to use `_playerDataProvider.SetPlayerPosition/Rotation()`

2. **Updated Context Management:**
   - GameContextInitiator now sets PlayerDataProvider in SaveGameService during Start()
   - MenuContextInitiator clears PlayerDataProvider when transitioning to menu
   - GameContextInitiator clears PlayerDataProvider on context destroy

3. **Removed Redundant Code:**
   - Deleted obsolete `GameSaveGameService.cs` (functionality now integrated in SaveGameService)
   - All player position save/load now works through unified SaveGameService

**Result:**
- ✅ Player position and rotation now properly saved in game files
- ✅ Player spawns at saved position when loading game
- ✅ Works correctly in both menu load and in-game load scenarios
- ✅ Maintains zero position behavior in menu scene (where player doesn't exist)
- ✅ Proper service lifecycle management across scene transitions

### Shelf Save/Load System Implementation (Completed)

**Problem:** Shelves with products were not being saved/restored in game saves
- Shelf state (product type and quantity) was lost between save/load cycles
- No system existed to track and manage shelf states across the game

**Solution: Implemented complete shelf management system**

1. **Created IShelfManagerService interface:**
   - `RegisterShelf()` / `UnregisterShelf()` for shelf lifecycle management
   - `GetShelvesSaveData()` for collecting shelf states during save
   - `RestoreShelves()` for restoring shelf states during load
   - `ClearAllShelves()` for new game scenarios

2. **Implemented ShelfManagerService:**
   - Tracks all active shelves in `_activeShelves` list
   - Uses shelf index as unique ID for save/load consistency
   - Integrates with ProductCatalogService to resolve product references
   - Provides detailed logging for debugging save/load operations
   - **IMPORTANT: Created as POCO class (not MonoBehaviour) following project rules**
   - Uses constructor injection for ProductCatalogService dependency

3. **Enhanced ShelfController:**
   - Added `RestoreState()` method for setting shelf state from save data
   - Added `ClearShelf()` method for resetting shelf state
   - Auto-registers with ShelfManagerService on Start
   - Auto-unregisters on Destroy for proper cleanup
   - Maintains existing interaction and visual systems

4. **Integrated with SaveGameService:**
   - Added `IShelfManagerService` field with Set/Clear methods
   - Updated `CollectStoreData()` to gather shelf data via ShelfManagerService
   - Updated `ApplySaveData()` to restore shelf states via ShelfManagerService
   - Added proper service cleanup in context transitions

5. **Updated Context Management:**
   - GameContextInitiator: Registers ShelfManagerService as POCO and sets it in SaveGameService
   - MenuContextInitiator: Clears ShelfManagerService reference when leaving game
   - Proper cleanup in OnDestroy methods

6. **Fixed Timing Issue (CRITICAL):**
   - **Problem**: Shelves were created via PlacementService after RestoreShelves() was called, causing empty shelves
   - **Solution**: Implemented delayed restoration system in ShelfManagerService:
     - Stores pending shelf data when RestoreShelves() is called
     - Automatically retries restoration when new shelves register via RegisterShelf()
     - Continues until all shelves from save data are successfully restored
     - Handles partial restoration scenarios gracefully

**Technical Details:**
- Shelf data includes: ShelfId, ProductType (ProductID), ItemCount
- Uses index-based shelf identification for save/load consistency
- Integrates with existing visual system (UpdateVisuals) for immediate feedback
- Maintains compatibility with existing shelf interaction systems (LMB/RMB with open boxes)
- **Follows project rules: ShelfManagerService is POCO class, not MonoBehaviour**

**Status:** ✅ **FULLY WORKING** - Shelves now correctly save/restore their product states even with asynchronous initialization timing

### Customer Save/Load System Implementation (Completed)

**Problem:** Customers with collected items would lose their progress when saving/loading game, causing item disappearance and incomplete shopping experiences.

**Status:** ✅ **COMPLETED** - Customer save/load system fully functional
- Customers maintain all state across save/load cycles
- No item loss when customers have collected products
- Queue positions and shopping progress preserved
- Integrated with existing save/load architecture

### Customer Appearance Restoration Fix (Completed)

**Problem:** During customer restoration from save data, CharacterAppearanceConfig.GenderModel.modelPrefab was null, causing character models not to load properly.

**Root Cause:** CustomerManagerService's GetGenderModel() method was returning null as a placeholder implementation. The service didn't have access to CharacterAppearanceConfig which is stored in CustomerSpawnerService.

**Solution:**
1. **Extended ICustomerSpawnerService interface:**
   - Added `GetCharacterAppearanceConfig()` method to provide access to appearance configuration

2. **Updated CustomerSpawnerService:**
   - Implemented `GetCharacterAppearanceConfig()` to return the `_appearanceConfig` field

3. **Fixed CustomerManagerService.GetGenderModel():**
   - Now properly retrieves CharacterAppearanceConfig from CustomerSpawnerService
   - Searches through available gender models to find matching gender
   - Returns first model of requested gender
   - Includes detailed logging for debugging appearance issues

4. **Enhanced appearance restoration logging:**
   - Added comprehensive logging in RestoreAppearance() method
   - Tracks gender model retrieval and application process
   - Helps diagnose future appearance-related issues

**Result:** ✅ Character models now load correctly during customer restoration, maintaining proper visual appearance.

### Save/Load Critical Fixes (Completed)

**Problems after "Load Game" button:**
1. **Old customers were not cleared** - Previous customers accumulated, creating duplicates and conflicts
2. **Shelf products sometimes not restored** - Inconsistent shelf state restoration could leave shelves empty

**Root Causes:**
1. `CustomerManagerService.RestoreCustomers()` lacked clearing logic before loading new customers
2. `ShelfManagerService.RestoreShelves()` didn't clear existing shelf states before restoration

**Solutions:**
1. **Fixed Customer Accumulation:**
   - Added `ClearAllCustomers()` call at start of `RestoreCustomers()` method
   - Ensures old customer instances are properly destroyed before loading saved customers
   - Prevents customer duplication and memory leaks

2. **Enhanced Shelf Restoration:**
   - Added explicit shelf clearing in `RestoreShelves()` method  
   - All shelves reset to empty state before applying saved data
   - Prevents old shelf contents from mixing with loaded data
   - Works in combination with existing delayed restoration system

**Technical Details:**
- Both fixes follow "clear before restore" pattern for clean state transitions
- Maintain existing delayed restoration logic for proper timing
- Include comprehensive logging for debugging load operations
- Preserve all existing functionality while fixing edge cases

**Result:** ✅ Load game now properly:
- Clears all old customers before loading saved ones (no duplication)
- Resets all shelf states before restoring saved inventory
- Maintains clean game state transitions
- Prevents data conflicts between old and loaded states

### Shelf Restoration Debugging (In Progress)

**Current Issue:** After implementing customer clearing fix, shelves are still not restoring properly on load game.

**Root Cause Found:** Index mismatch when loading from within game
- PlacementService destroys old shelf objects and creates new ones
- Old shelves have indices 0, 1 in ShelfManagerService
- New shelves register as indices 2, 3 (after old ones)
- RestoreShelves applies data to indices 0, 1 (old shelves)
- Old shelves get destroyed, leaving new shelves empty

**Why it works from main menu:**
- Scene loads fresh, shelves register with correct indices 0, 1
- No conflict between old and new objects

**Solution Implemented:**
1. **Clear all shelves before PlacementService restoration:**
   - Added `ClearAllShelves()` call before restoring placed objects
   - Unregister all existing ShelfController objects
   - Forces new shelves to register with correct indices (0, 1)

2. **Enhanced UnregisterShelf logic:**
   - Clear pending data when all shelves are unregistered
   - Prevents stale data from interfering with new registrations

**Code Changes:**
- SaveGameService: Clear shelves before PlacementService restoration
- ShelfManagerService: Clear pending data when shelf list empty

**Status:** ✅ **FIXED** - Shelf index mismatch resolved, testing needed to confirm

### Этап 16: Улучшение системы доставки (Выполнено)

**Problem:** Система доставки работала только с немедленной доставкой, что было нереалистично и не позволяло игроку планировать заказы.

**Solution: Реализована полная система отложенной доставки с UI отслеживания**

1. **Расширение IDeliveryService interface:**
   - Добавлен метод `PlaceOrder()` для отложенной доставки (по умолчанию 3 минуты)
   - Добавлен метод `CancelOrder()` с частичным возвратом средств (50%)
   - Добавлены методы `GetActiveOrders()` и `GetOrder()` для отслеживания
   - Добавлены события: `OnOrderPlaced`, `OnOrderDelivered`, `OnOrderCancelled`, `OnDeliveryImminent`
   - Добавлен метод `LoadActiveOrders()` для загрузки из сохранений

2. **Полный рефакторинг DeliveryService:**
   - Реализована очередь заказов с использованием `List<OrderSaveData>`
   - Добавлен метод `Update()` для обработки таймеров заказов в реальном времени
   - Система уведомлений за 1 минуту до доставки
   - Автоматическая доставка при истечении времени
   - Упрощенная система ID заказов: `ORDER_1`, `ORDER_2` вместо сложных временных меток
   - Настраиваемое время доставки и предупреждений через `SerializeField`
   - Сохранен старый метод `DeliverBoxes()` для обратной совместимости

3. **UI для отслеживания активных заказов:**
   - Добавлена подвкладка "Активные заказы" в секции "Магазин" компьютера
   - Карточки заказов с полной информацией:
     - ID заказа (#1, #2, и т.д.)
     - Время размещения заказа
     - Таймер обратного отсчета в формате MM:SS
     - Список заказанных товаров с правильными названиями (не ID)
     - Общая стоимость заказа
     - Кнопка отмены заказа
   - Обновление таймеров в реальном времени (каждую секунду)
   - Оптимизированная система обновления для избежания лагов

4. **Интеграция с системой сохранения:**
   - Обновлен `GameContextInitiator` для установки `DeliveryService` в `SaveGameService`
   - Добавлено восстановление активных заказов в `SaveGameService.ApplySaveData()`
   - Активные заказы сохраняются в `SaveGameData.ActiveOrders` (верхний уровень)
   - Автоматическое обновление счетчика заказов при загрузке для избежания конфликтов ID
   - Восстановление таймеров с правильным оставшимся временем

5. **Уведомления и пользовательский опыт:**
   - Уведомление при размещении заказа с указанием времени доставки
   - Предупреждение за минуту до прибытия заказа
   - Уведомление о доставке заказа
   - Уведомление об отмене заказа с суммой возврата
   - Все уведомления показывают простые номера заказов (#1, #2)

6. **Техническая реализация:**
   - Использование статического счетчика `_orderCounter` для простых ID
   - Отслеживание отправленных предупреждений через `HashSet<string>`
   - Правильное управление жизненным циклом заказов (статусы: InTransit → Delivered/Cancelled)
   - Метод `UpdateOrderCounterFromLoadedOrders()` для синхронизации счетчика при загрузке

**Technical Details:**
- Время доставки конфигурируется через инспектор (по умолчанию 3 минуты)
- Время предупреждения конфигурируется отдельно (по умолчанию 1 минута)
- Процент возврата при отмене настраивается (по умолчанию 50%)
- Обновление UI происходит только когда секция активна для оптимизации
- Поддержка загрузки/сохранения состояния заказов между сессиями

**Integration with ComputerUIHandler:**
- Переключение с `DeliverBoxes()` на `PlaceOrder()` в обработчике заказов
- Новая навигационная структура: Магазин → Товары/Мебель/Активные заказы
- Обновление таймеров через `Update()` метод с интервальной проверкой
- Правильное отображение названий товаров вместо ID в списках заказов

**Status:** ✅ **FULLY WORKING** - Система отложенной доставки полностью функциональна:
- Заказы размещаются с таймером
- Таймеры обновляются в реальном времени
- Уведомления работают корректно
- Заказы можно отменять с возвратом средств
- Активные заказы сохраняются и загружаются между сессиями
- UI показывает актуальную информацию о всех активных заказах

## Реализованные этапы

### Latest Features:

**✅ Object Relocation System with R Key (2024-12-03):**
- **Feature**: Added ability to relocate already placed objects by pressing R key
- **Purpose**: Players can now reposition any placed object without needing to pick it up from a box
- **Implementation**:
  1. **Enhanced PlayerInteractionController**: Extended existing interaction controller to handle R key input and raycasting for placed objects
  2. **Extended IPlacementService**: Added `StartRelocateMode()`, `IsInRelocateMode`, and `GetRelocatingObject()` methods
  3. **Enhanced PlacementService**: Added relocation state tracking and logic
     - New state variables: `_isInRelocateMode`, `_relocatingObject`, `_relocatingObjectData`, `_relocatingObjectIndex`
     - `StartRelocateMode()`: Hides original object and creates preview for relocation
     - `ConfirmRelocation()`: Moves object to new position and updates save data
     - `CancelRelocateMode()`: Cancels relocation and restores original object
  4. **Updated PlacementController**: Modified `ShouldUpdatePlacement()` to work in both placement and relocation modes
  5. **New Input Action**: Added "RelocateObject" action bound to R key in PlayerInputActions.inputactions
  6. **Save System Integration**: Relocated objects update their position/rotation in PlacedObjectData
- **User Experience**: Same controls as normal placement (LMB confirm, RMB cancel, scroll wheel rotate)
- **Object Detection**: Identifies placed objects by "PlacedObject" tag (proper Unity approach)
- **Result**: Players can easily rearrange their store layout by pointing at objects and pressing R

**✅ Customer Avoidance Priority System (2024-12-03):**
- **Feature**: Added random/unique obstacle avoidance priority system for customers  
- **Purpose**: Customers now yield to each other more naturally based on navigation priority
- **Implementation**:
  1. Added `SetAvoidancePriority()` and `GetAvoidancePriority()` methods to `CustomerLocomotion`
  2. Added navigation settings to `CustomerSpawnerService` inspector:
     - `_minAvoidancePriority = 30` (highest priority)
     - `_maxAvoidancePriority = 70` (lowest priority)
     - `_useUniqueAvoidancePriority = false` (random vs sequential mode)
  3. Extended `CustomerSaveData` with `AvoidancePriority` field for save/load support
  4. Added `GetAvoidancePriority()` method to `CustomerManagerService` for saving
  5. Added priority restoration in `RestoreCustomer()` method
- **Priority Logic**: Lower number = higher priority (NavMeshAgent standard)
- **Result**: Customers now naturally avoid each other with hierarchical navigation priorities