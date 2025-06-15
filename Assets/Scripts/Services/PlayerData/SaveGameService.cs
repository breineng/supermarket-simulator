using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Supermarket.Data;
using BehaviourInject;
using Supermarket.Services.Game; // Для IStatsService
using Supermarket.Interactables; // Для ShelfController и BoxController

namespace Supermarket.Services.PlayerData
{
    // Интерфейс для получения данных игрока из игровой сцены
    public interface IPlayerDataProvider
    {
        Vector3 GetPlayerPosition();
        Vector3 GetPlayerRotation();
        void SetPlayerPosition(Vector3 position);
        void SetPlayerRotation(Vector3 rotation);
    }
    
    // Интерфейс для выбора сохранений (используется в меню)
    public interface ISaveGameSelectionService
    {
        List<SaveGameInfo> GetSavesList();
        bool DeleteSave(string saveName);
        bool SaveExists(string saveName);
        DateTime? GetSaveDate(string saveName);
        string GetCurrentSaveVersion();
        bool IsSaveCompatible(string saveName);
        
        // Новые методы для выбора файла загрузки
        void SetSelectedSaveFile(string saveName);
        string GetSelectedSaveFile();
        void ClearSelectedSaveFile();
    }
    
    public class SaveGameService : MonoBehaviour, ISaveGameService, ISaveGameSelectionService
    {
        [Header("Save Settings")]
        [SerializeField] private string _saveDirectory = "SavedGames";
        [SerializeField] private string _fileExtension = ".sav";
        [SerializeField] private bool _prettyPrintJson = true;
        [SerializeField] private string _autoSaveName = "AutoSave";

        [Inject] public IPlayerDataService _playerDataService;
        [Inject] public IStatsService _statsService;
        [Inject] public ISceneManagementService _sceneManagementService;
        [Inject] public IInputModeService _inputModeService;

        // Опциональные игровые сервисы (доступны только в GameContext)
        private IPlacementService _placementService;
        
        // Сервис управления коробками (доступен только в GameContext)
        private IBoxManagerService _boxManagerService;
        
        // Сервис данных о позиции игрока (доступен только в игре)
        private IPlayerDataProvider _playerDataProvider;
        
        // Сервис управления полками (доступен только в GameContext)
        private IShelfManagerService _shelfManagerService;
        
        // Сервис управления покупателями (доступен только в GameContext)
        private ICustomerManagerService _customerManagerService;
        
        // Сервис доставки (доступен только в GameContext)
        private IDeliveryService _deliveryService;
        
        // Сервис лицензий (доступен только в GameContext)
        private ILicenseService _licenseService;
        
        // Сервис названия супермаркета (доступен только в GameContext)
        private ISupermarketNameService _supermarketNameService;
        
        // Сервис розничных цен (доступен только в GameContext)
        private IRetailPriceService _retailPriceService;
        
        // Сервис управления коробкой в руках игрока (доступен только в GameContext)
        private IPlayerHandService _playerHandService;
        
        // Поле для хранения выбранного в меню файла сохранения
        private string _selectedSaveFile;
        
        private string _savePath;
        private Coroutine _autoSaveCoroutine;
        private float _sessionStartTime;
        
        /// <summary>
        /// Устанавливает IPlacementService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetPlacementService(IPlacementService placementService)
        {
            _placementService = placementService;
            Debug.Log("SaveGameService: IPlacementService set, placed objects save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IPlacementService
        /// Вызывается при переходе в меню где PlacementService недоступен
        /// </summary>
        public void ClearPlacementService()
        {
            _placementService = null;
            Debug.Log("SaveGameService: IPlacementService cleared, placed objects save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает IBoxManagerService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetBoxManagerService(IBoxManagerService boxManagerService)
        {
            _boxManagerService = boxManagerService;
            Debug.Log("SaveGameService: IBoxManagerService set, box save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IBoxManagerService
        /// Вызывается при переходе в меню где BoxManagerService недоступен
        /// </summary>
        public void ClearBoxManagerService()
        {
            _boxManagerService = null;
            Debug.Log("SaveGameService: IBoxManagerService cleared, box save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает IPlayerDataProvider для этого SaveGameService
        /// Вызывается из GameContextInitiator когда игрок доступен
        /// </summary>
        public void SetPlayerDataProvider(IPlayerDataProvider playerDataProvider)
        {
            _playerDataProvider = playerDataProvider;
            Debug.Log("SaveGameService: IPlayerDataProvider set, player position save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IPlayerDataProvider
        /// Вызывается при переходе в меню где игрок недоступен
        /// </summary>
        public void ClearPlayerDataProvider()
        {
            _playerDataProvider = null;
            Debug.Log("SaveGameService: IPlayerDataProvider cleared, player position save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает IShelfManagerService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetShelfManagerService(IShelfManagerService shelfManagerService)
        {
            _shelfManagerService = shelfManagerService;
            Debug.Log("SaveGameService: IShelfManagerService set, shelf save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IShelfManagerService
        /// Вызывается при переходе в меню где полки недоступны
        /// </summary>
        public void ClearShelfManagerService()
        {
            _shelfManagerService = null;
            Debug.Log("SaveGameService: IShelfManagerService cleared, shelf save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает ICustomerManagerService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetCustomerManagerService(ICustomerManagerService customerManagerService)
        {
            _customerManagerService = customerManagerService;
            Debug.Log("SaveGameService: ICustomerManagerService set, customer save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на ICustomerManagerService
        /// Вызывается при переходе в меню где покупатели недоступны
        /// </summary>
        public void ClearCustomerManagerService()
        {
            _customerManagerService = null;
            Debug.Log("SaveGameService: ICustomerManagerService cleared, customer save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает IDeliveryService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetDeliveryService(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
            Debug.Log("SaveGameService: IDeliveryService set, delivery save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IDeliveryService
        /// Вызывается при переходе в меню где доставка недоступна
        /// </summary>
        public void ClearDeliveryService()
        {
            _deliveryService = null;
            Debug.Log("SaveGameService: IDeliveryService cleared, delivery save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает ILicenseService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetLicenseService(ILicenseService licenseService)
        {
            _licenseService = licenseService;
            Debug.Log("SaveGameService: ILicenseService set, license save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на ILicenseService
        /// Вызывается при переходе в меню где лицензии недоступны
        /// </summary>
        public void ClearLicenseService()
        {
            _licenseService = null;
            Debug.Log("SaveGameService: ILicenseService cleared, license save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает ISupermarketNameService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetSupermarketNameService(ISupermarketNameService supermarketNameService)
        {
            _supermarketNameService = supermarketNameService;
            Debug.Log("SaveGameService: ISupermarketNameService set, supermarket name save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на ISupermarketNameService
        /// Вызывается при переходе в меню где название супермаркета недоступно
        /// </summary>
        public void ClearSupermarketNameService()
        {
            _supermarketNameService = null;
            Debug.Log("SaveGameService: ISupermarketNameService cleared, supermarket name save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает IRetailPriceService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetRetailPriceService(IRetailPriceService retailPriceService)
        {
            _retailPriceService = retailPriceService;
            Debug.Log("SaveGameService: IRetailPriceService set, custom prices save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IRetailPriceService
        /// Вызывается при переходе в меню где розничные цены недоступны
        /// </summary>
        public void ClearRetailPriceService()
        {
            _retailPriceService = null;
            Debug.Log("SaveGameService: IRetailPriceService cleared, custom prices save/load disabled.");
        }
        
        /// <summary>
        /// Устанавливает IPlayerHandService для этого SaveGameService
        /// Вызывается из GameContextInitiator когда GameContext становится активным
        /// </summary>
        public void SetPlayerHandService(IPlayerHandService playerHandService)
        {
            _playerHandService = playerHandService;
            Debug.Log("SaveGameService: IPlayerHandService set, held box save/load enabled.");
        }
        
        /// <summary>
        /// Очищает ссылку на IPlayerHandService
        /// Вызывается при переходе в меню где управление коробкой в руках недоступно
        /// </summary>
        public void ClearPlayerHandService()
        {
            _playerHandService = null;
            Debug.Log("SaveGameService: IPlayerHandService cleared, held box save/load disabled.");
        }
        
        // Ленивая инициализация пути сохранения
        public string SavePath
        {
            get
            {
                if (string.IsNullOrEmpty(_savePath))
                {
                    _savePath = Path.Combine(Application.persistentDataPath, _saveDirectory);
                    
                    // Создаем папку, если не существует
                    if (!Directory.Exists(_savePath))
                    {
                        Directory.CreateDirectory(_savePath);
                        Debug.Log($"SaveGameService: Created save directory at {_savePath}");
                    }
                }
                return _savePath;
            }
        }
        
        // События
        public event Action<string> OnSaveCompleted;
        public event Action<string> OnSaveError;
        public event Action<string> OnLoadCompleted;
        public event Action<string> OnLoadError;
        
        void Awake()
        {
            _sessionStartTime = Time.time;
        }
        
        public bool SaveGame(string saveName)
        {
            try
            {
                // Собираем данные для сохранения
                SaveGameData saveData = CollectSaveData();
                
                // Создаем и сохраняем скриншот
                string screenshotPath = CaptureAndSaveScreenshot(saveName);
                if (!string.IsNullOrEmpty(screenshotPath))
                {
                    saveData.ScreenshotPath = screenshotPath;
                    Debug.Log($"Screenshot saved: {screenshotPath}");
                }
                
                // Сериализуем в JSON
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });
                
                // Сохраняем в файл
                string filePath = GetSaveFilePath(saveName);
                File.WriteAllText(filePath, json);
                
                Debug.Log($"Game saved successfully: {saveName}");
                OnSaveCompleted?.Invoke(saveName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                OnSaveError?.Invoke(e.Message);
                return false;
            }
        }
        
        public bool LoadGame(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Save file not found: {saveName}");
                }
                
                // Читаем JSON из файла
                string json = File.ReadAllText(filePath);
                
                // Проверяем совместимость версии
                var tempData = JsonConvert.DeserializeObject<SaveGameData>(json);
                if (!SaveGameMigration.IsVersionCompatible(tempData.Version))
                {
                    throw new Exception($"Save version {tempData.Version} is not compatible");
                }
                
                // Мигрируем данные если необходимо
                SaveGameData saveData = SaveGameMigration.MigrateSaveData(json);
                
                if (saveData == null)
                {
                    throw new Exception("Failed to migrate save data");
                }
                
                // Применяем загруженные данные
                ApplySaveData(saveData);
                
                Debug.Log($"Game loaded successfully: {saveName}");
                OnLoadCompleted?.Invoke(saveName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                OnLoadError?.Invoke(e.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Загружает игру в игровой сцене используя предварительно выбранный файл
        /// Этот метод должен вызываться только в игровой сцене где доступны все сервисы
        /// </summary>
        public bool LoadGameInGameScene()
        {
            if (string.IsNullOrEmpty(_selectedSaveFile))
            {
                Debug.LogWarning("SaveGameService: No save file selected for loading in game scene.");
                return false;
            }
            
            Debug.Log($"SaveGameService: Loading selected save file '{_selectedSaveFile}' in game scene...");
            bool result = LoadGame(_selectedSaveFile);
            
            // Очищаем выбранный файл после загрузки
            if (result)
            {
                ClearSelectedSaveFile();
            }
            
            return result;
        }
        
        public bool DeleteSave(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                bool saveFileDeleted = false;
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    saveFileDeleted = true;
                    Debug.Log($"Save deleted: {saveName}");
                }
                
                // Также удаляем связанный скриншот
                string screenshotPath = Path.Combine(SavePath, $"{saveName}_screenshot.png");
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                    Debug.Log($"Screenshot deleted: {screenshotPath}");
                }
                
                return saveFileDeleted;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
                return false;
            }
        }
        
        public List<SaveGameInfo> GetSavesList()
        {
            List<SaveGameInfo> saves = new List<SaveGameInfo>();
            
            try
            {
                string[] files = Directory.GetFiles(SavePath, "*" + _fileExtension);
                
                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        SaveGameData data = JsonConvert.DeserializeObject<SaveGameData>(json);
                        
                        FileInfo fileInfo = new FileInfo(file);
                        
                        saves.Add(new SaveGameInfo
                        {
                            SaveName = Path.GetFileNameWithoutExtension(file),
                            SaveDate = data.SaveDate,
                            Version = data.Version,
                            FileSize = fileInfo.Length,
                            PlayTime = data.PlayTime,
                            Money = data.PlayerData?.Money ?? 0,
                            Day = data.Statistics?.CurrentDay ?? 1,
                            IsCompatible = SaveGameMigration.IsVersionCompatible(data.Version),
                            ScreenshotPath = data.ScreenshotPath
                        });
                    }
                    catch
                    {
                        // Пропускаем поврежденные файлы
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get saves list: {e.Message}");
            }
            
            return saves.OrderByDescending(s => s.SaveDate).ToList();
        }
        
        public void EnableAutoSave(float intervalInSeconds)
        {
            DisableAutoSave();
            _autoSaveCoroutine = StartCoroutine(AutoSaveRoutine(intervalInSeconds));
        }
        
        public void DisableAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
            }
        }
        
        public bool SaveExists(string saveName)
        {
            return File.Exists(GetSaveFilePath(saveName));
        }
        
        public DateTime? GetSaveDate(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    SaveGameData data = JsonConvert.DeserializeObject<SaveGameData>(json);
                    return data.SaveDate;
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
            
            return null;
        }
        
        public string GetCurrentSaveVersion()
        {
            return SaveGameMigration.CURRENT_VERSION;
        }
        
        public bool IsSaveCompatible(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    SaveGameData data = JsonConvert.DeserializeObject<SaveGameData>(json);
                    return SaveGameMigration.IsVersionCompatible(data.Version);
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
            
            return false;
        }
        
        private string GetSaveFilePath(string saveName)
        {
            return Path.Combine(SavePath, saveName + _fileExtension);
        }
        
        private IEnumerator AutoSaveRoutine(float interval)
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                SaveGame(_autoSaveName);
            }
        }
        
        protected virtual SaveGameData CollectSaveData()
        {
            SaveGameData saveData = new SaveGameData
            {
                Version = SaveGameMigration.CURRENT_VERSION,
                SaveDate = DateTime.Now,
                PlayTime = Time.time - _sessionStartTime
            };
            
            // Собираем данные игрока
            if (_playerDataService != null)
            {
                var playerData = _playerDataService.CurrentPlayerData;
                
                // Получаем позицию игрока через провайдер, если он доступен
                Vector3 playerPosition = Vector3.zero;
                Vector3 playerRotation = Vector3.zero;
                
                if (_playerDataProvider != null)
                {
                    playerPosition = _playerDataProvider.GetPlayerPosition();
                    playerRotation = _playerDataProvider.GetPlayerRotation();
                    Debug.Log($"SaveGameService: Player position saved: {playerPosition}, rotation: {playerRotation}");
                }
                else
                {
                    Debug.Log("SaveGameService: IPlayerDataProvider is null, saving zero position/rotation (menu scene behavior)");
                }
                
                // Собираем данные коробки в руках
                PlayerHeldBoxData heldBoxData = null;
                if (_playerHandService != null)
                {
                    heldBoxData = _playerHandService.GetSaveData();
                    if (heldBoxData != null)
                    {
                        string productName = !string.IsNullOrEmpty(heldBoxData.ProductType) ? heldBoxData.ProductType : "Empty";
                        string openState = heldBoxData.IsOpen ? "open" : "closed";
                        Debug.Log($"SaveGameService: Collected held box data - {productName} x{heldBoxData.ItemCount} ({openState})");
                    }
                    else
                    {
                        Debug.Log("SaveGameService: No box in player hands to save");
                    }
                }
                else
                {
                    Debug.LogWarning("SaveGameService: IPlayerHandService is null, cannot collect held box data");
                }

                saveData.PlayerData = new PlayerSaveData
                {
                    Money = playerData.Money,
                    Position = playerPosition,
                    Rotation = playerRotation,
                    CustomPrices = _retailPriceService?.GetCustomPrices() ?? new Dictionary<string, float>(),
                    HeldBox = heldBoxData, // Добавляем данные коробки в руках
                    Appearance = new CharacterAppearanceData
                    {
                        Gender = playerData.CharacterGender == "Female" ? 1 : 0, // Преобразуем строку в int
                        ClothingIndex = playerData.ClothingIndex,
                        ShirtColor = playerData.ShirtColor,
                        PantsColor = playerData.PantsColor
                    }
                };
            }
            
            // Собираем данные магазина
            saveData.StoreData = CollectStoreData();
            
            // Собираем активные заказы
            saveData.ActiveOrders = CollectActiveOrders();
            
            // Собираем статистику
            if (_statsService != null)
            {
                saveData.Statistics = new StatsSaveData
                {
                    TotalRevenue = _statsService.GetTotalRevenue(),
                    TotalExpenses = _statsService.GetTotalExpenses(),
                    TotalCustomersServed = _statsService.GetTotalCustomersServed(),
                    TotalItemsSold = _statsService.GetTotalItemsSold(),
                    ProductSales = new Dictionary<string, int>(), // TODO: Получить из StatsService
                    CurrentDay = 1, // TODO: Реализовать систему дней
                    CurrentDayRevenue = 0, // TODO: Получить из StatsService
                    CurrentDayCustomers = _statsService.GetCustomersToday()
                };
            }
            
            // Настройки игры
            saveData.Settings = new GameSettingsSaveData
            {
                MasterVolume = 1.0f, // TODO: Получить из системы настроек
                MusicVolume = 0.7f,
                SFXVolume = 1.0f,
                MouseSensitivity = 1.0f,
                GraphicsQuality = QualitySettings.GetQualityLevel(),
                AutoSaveEnabled = _autoSaveCoroutine != null,
                AutoSaveInterval = 300f // TODO: Сохранить актуальный интервал
            };
            
            // Лицензии
            if (_licenseService != null)
            {
                saveData.UnlockedLicenses = _licenseService.GetPurchasedLicenseIds();
                Debug.Log($"SaveGameService: Collected {saveData.UnlockedLicenses.Count} purchased licenses for saving.");
            }
            else
            {
                Debug.LogWarning("SaveGameService: ILicenseService is null, cannot collect license data.");
                saveData.UnlockedLicenses = new List<string>();
            }
            
            return saveData;
        }
        
        private StoreSaveData CollectStoreData()
        {
            StoreSaveData storeData = new StoreSaveData
            {
                PlacedObjects = new List<PlacedObjectData>(),
                Shelves = new List<ShelfSaveData>(),
                Boxes = new List<BoxSaveData>(),
                Customers = new List<CustomerSaveData>()
            };
            
            // Собираем название супермаркета
            if (_supermarketNameService != null)
            {
                storeData.SupermarketName = _supermarketNameService.CurrentName;
                Debug.Log($"SaveGameService: Collected supermarket name for saving: '{storeData.SupermarketName}'");
            }
            else
            {
                Debug.LogWarning("SaveGameService: ISupermarketNameService is null, using default supermarket name.");
            }

            // Собираем данные размещенных объектов через PlacementService
            if (_placementService != null)
            {
                storeData.PlacedObjects = _placementService.GetPlacedObjectsData();
                Debug.Log($"SaveGameService: Collected {storeData.PlacedObjects.Count} placed objects for saving.");
            }
            else
            {
                Debug.LogWarning("SaveGameService: IPlacementService is null, cannot collect placed objects data.");
            }

            // Собираем данные полок
            // TODO: Получить из сервиса управления магазином
            
            // Собираем данные полок через ShelfManagerService
            if (_shelfManagerService != null)
            {
                storeData.Shelves = _shelfManagerService.GetShelvesSaveData();
                Debug.Log($"SaveGameService: Collected {storeData.Shelves.Count} shelves for saving.");
            }
            else
            {
                Debug.LogWarning("SaveGameService: IShelfManagerService is null, cannot collect shelves data.");
            }

            // Собираем данные коробок через BoxManagerService
            if (_boxManagerService != null)
            {
                storeData.Boxes = _boxManagerService.GetBoxesSaveData();
                Debug.Log($"SaveGameService: Collected {storeData.Boxes.Count} boxes for saving.");
            }
            else
            {
                Debug.LogWarning("SaveGameService: IBoxManagerService is null, cannot collect boxes data.");
            }

            // Собираем данные покупателей через CustomerManagerService
            if (_customerManagerService != null)
            {
                storeData.Customers = _customerManagerService.GetCustomersSaveData();
                Debug.Log($"SaveGameService: Collected {storeData.Customers.Count} customers for saving.");
            }
            else
            {
                Debug.LogWarning("SaveGameService: ICustomerManagerService is null, cannot collect customers data.");
            }

            return storeData;
        }
        
        private List<OrderSaveData> CollectActiveOrders()
        {
            // Получаем активные заказы из DeliveryService
            if (_deliveryService != null)
            {
                return _deliveryService.GetActiveOrders();
            }
            
            Debug.LogWarning("SaveGameService: DeliveryService not found, cannot collect active orders.");
            return new List<OrderSaveData>();
        }
        
        protected virtual void ApplySaveData(SaveGameData saveData)
        {
            // Восстанавливаем данные игрока
            if (_playerDataService != null && saveData.PlayerData != null)
            {
                _playerDataService.SetMoney(saveData.PlayerData.Money);
                
                // Восстанавливаем позицию игрока, если провайдер доступен
                if (_playerDataProvider != null)
                {
                    _playerDataProvider.SetPlayerPosition(saveData.PlayerData.Position);
                    _playerDataProvider.SetPlayerRotation(saveData.PlayerData.Rotation);
                    Debug.Log($"SaveGameService: Player position restored: {saveData.PlayerData.Position}, rotation: {saveData.PlayerData.Rotation}");
                }
                else
                {
                    Debug.Log("SaveGameService: IPlayerDataProvider is null, cannot restore player position (menu scene behavior)");
                }
                
                // Восстанавливаем внешний вид
                if (saveData.PlayerData.Appearance != null)
                {
                    var appearance = saveData.PlayerData.Appearance;
                    _playerDataService.SetCharacterAppearance(
                        appearance.Gender == 1 ? "Female" : "Male",
                        appearance.ClothingIndex,
                        appearance.ShirtColor,
                        appearance.PantsColor
                    );
                }
                
                // Восстанавливаем кастомные цены
                if (_retailPriceService != null && saveData.PlayerData.CustomPrices != null)
                {
                    _retailPriceService.RestorePrices(saveData.PlayerData.CustomPrices);
                    Debug.Log($"SaveGameService: Restored {saveData.PlayerData.CustomPrices.Count} custom prices");
                }

                // Восстанавливаем коробку в руках игрока
                if (_playerHandService != null)
                {
                    _playerHandService.RestoreFromSaveData(saveData.PlayerData.HeldBox);
                    if (saveData.PlayerData.HeldBox != null)
                    {
                        string productName = !string.IsNullOrEmpty(saveData.PlayerData.HeldBox.ProductType) ? saveData.PlayerData.HeldBox.ProductType : "Empty";
                        string openState = saveData.PlayerData.HeldBox.IsOpen ? "open" : "closed";
                        Debug.Log($"SaveGameService: Restored held box - {productName} x{saveData.PlayerData.HeldBox.ItemCount} ({openState})");
                    }
                    else
                    {
                        Debug.Log("SaveGameService: No held box data to restore - hands are empty");
                    }
                }
                else
                {
                    Debug.LogWarning("SaveGameService: IPlayerHandService is null, cannot restore held box");
                }
            }

            // Очищаем ВСЕ полки перед восстановлением размещенных объектов
            // Это критично для загрузки из игры, чтобы индексы полок не сбивались
            if (_shelfManagerService != null)
            {
                Debug.Log("SaveGameService: Clearing all shelves before restoring placed objects");
                _shelfManagerService.ClearAllShelves();
                
                // Также очищаем все зарегистрированные полки
                var allShelves = GameObject.FindObjectsOfType<ShelfController>();
                foreach (var shelf in allShelves)
                {
                    _shelfManagerService.UnregisterShelf(shelf);
                }
                Debug.Log($"SaveGameService: Unregistered {allShelves.Length} existing regular shelves");
                
                // Очищаем все зарегистрированные многоуровневые полки
                var allMultiShelves = GameObject.FindObjectsOfType<MultiLevelShelfController>();
                foreach (var multiShelf in allMultiShelves)
                {
                    _shelfManagerService.UnregisterMultiLevelShelf(multiShelf);
                }
                Debug.Log($"SaveGameService: Unregistered {allMultiShelves.Length} existing multi-level shelves");
            }

            // Восстанавливаем размещенные объекты
            if (_placementService != null && saveData.StoreData?.PlacedObjects != null)
            {
                Debug.Log($"SaveGameService: Starting restoration of {saveData.StoreData.PlacedObjects.Count} placed objects. PlacementService: {_placementService.GetType().Name}");
                foreach (var obj in saveData.StoreData.PlacedObjects)
                {
                    Debug.Log($"SaveGameService: Will restore object - PrefabName: '{obj.PrefabName}', Position: {obj.Position}, ObjectType: '{obj.ObjectType}'");
                }
                
                _placementService.RestorePlacedObjects(saveData.StoreData.PlacedObjects);
                Debug.Log($"SaveGameService: Completed restoration call for {saveData.StoreData.PlacedObjects.Count} placed objects.");
            }
            else
            {
                string reason = "";
                if (_placementService == null) reason += "PlacementService is null; ";
                if (saveData.StoreData?.PlacedObjects == null) reason += "PlacedObjects data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore placed objects - {reason}");
            }

            // Восстанавливаем коробки через BoxManagerService
            if (_boxManagerService != null && saveData.StoreData?.Boxes != null)
            {
                Debug.Log($"SaveGameService: Starting restoration of {saveData.StoreData.Boxes.Count} boxes. BoxManagerService: {_boxManagerService.GetType().Name}");
                foreach (var box in saveData.StoreData.Boxes)
                {
                    Debug.Log($"SaveGameService: Will restore box - ProductType: '{box.ProductType}', Position: {box.Position}, ItemCount: {box.ItemCount}");
                }
                
                _boxManagerService.RestoreBoxes(saveData.StoreData.Boxes);
                Debug.Log($"SaveGameService: Completed restoration call for {saveData.StoreData.Boxes.Count} boxes.");
            }
            else
            {
                string reason = "";
                if (_boxManagerService == null) reason += "BoxManagerService is null; ";
                if (saveData.StoreData?.Boxes == null) reason += "Boxes data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore boxes - {reason}");
            }

            // ВАЖНО: Восстанавливаем полки ПОСЛЕ размещенных объектов, 
            // чтобы полки успели зарегистрироваться в ShelfManagerService
            if (_shelfManagerService != null && saveData.StoreData?.Shelves != null)
            {
                Debug.Log($"SaveGameService: Starting restoration of {saveData.StoreData.Shelves.Count} shelves. ShelfManagerService: {_shelfManagerService.GetType().Name}");
                foreach (var shelf in saveData.StoreData.Shelves)
                {
                    if (shelf.Levels != null && shelf.Levels.Count > 0)
                    {
                        Debug.Log($"SaveGameService: Will restore multi-level shelf {shelf.ShelfId} with {shelf.Levels.Count} levels");
                    }
                    else
                {
                    Debug.Log($"SaveGameService: Will restore shelf {shelf.ShelfId} - ProductType: '{shelf.ProductType}', ItemCount: {shelf.ItemCount}");
                    }
                }
                
                // ShelfManagerService сам обработает отложенное восстановление если полки еще не зарегистрированы
                _shelfManagerService.RestoreShelves(saveData.StoreData.Shelves);
                Debug.Log($"SaveGameService: Completed restoration call for {saveData.StoreData.Shelves.Count} shelves.");
            }
            else
            {
                string reason = "";
                if (_shelfManagerService == null) reason += "ShelfManagerService is null; ";
                if (saveData.StoreData?.Shelves == null) reason += "Shelves data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore shelves - {reason}");
            }

            // Восстанавливаем покупателей через CustomerManagerService
            if (_customerManagerService != null && saveData.StoreData?.Customers != null)
            {
                Debug.Log($"SaveGameService: Starting restoration of {saveData.StoreData.Customers.Count} customers. CustomerManagerService: {_customerManagerService.GetType().Name}");
                foreach (var customer in saveData.StoreData.Customers)
                {
                    Debug.Log($"SaveGameService: Will restore customer '{customer.CustomerName}' - State: {(CustomerState)customer.CurrentState}, Position: {customer.Position}");
                }
                
                _customerManagerService.RestoreCustomers(saveData.StoreData.Customers);
                Debug.Log($"SaveGameService: Completed restoration call for {saveData.StoreData.Customers.Count} customers.");
            }
            else
            {
                string reason = "";
                if (_customerManagerService == null) reason += "CustomerManagerService is null; ";
                if (saveData.StoreData?.Customers == null) reason += "Customers data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore customers - {reason}");
            }

            // Восстанавливаем лицензии
            if (_licenseService != null && saveData.UnlockedLicenses != null)
            {
                Debug.Log($"SaveGameService: Starting restoration of {saveData.UnlockedLicenses.Count} purchased licenses.");
                _licenseService.RestorePurchasedLicenses(saveData.UnlockedLicenses);
                Debug.Log($"SaveGameService: Completed restoration of licenses.");
            }
            else
            {
                string reason = "";
                if (_licenseService == null) reason += "LicenseService is null; ";
                if (saveData.UnlockedLicenses == null) reason += "UnlockedLicenses data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore licenses - {reason}");
            }
            
            // Восстанавливаем название супермаркета
            if (_supermarketNameService != null && saveData.StoreData?.SupermarketName != null)
            {
                Debug.Log($"SaveGameService: Restoring supermarket name: '{saveData.StoreData.SupermarketName}'");
                _supermarketNameService.LoadName(saveData.StoreData.SupermarketName);
            }
            else
            {
                string reason = "";
                if (_supermarketNameService == null) reason += "SupermarketNameService is null; ";
                if (saveData.StoreData?.SupermarketName == null) reason += "SupermarketName data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore supermarket name - {reason}");
            }

            // Восстанавливаем активные заказы через DeliveryService
            if (_deliveryService != null && saveData.ActiveOrders != null)
            {
                Debug.Log($"SaveGameService: Starting restoration of {saveData.ActiveOrders.Count} active orders. DeliveryService: {_deliveryService.GetType().Name}");
                foreach (var order in saveData.ActiveOrders)
                {
                    Debug.Log($"SaveGameService: Will restore order '{order.OrderId}' - Status: {order.Status}, Remaining time: {order.DeliveryTime:F1}s");
                }
                
                _deliveryService.LoadActiveOrders(saveData.ActiveOrders);
                Debug.Log($"SaveGameService: Completed restoration call for {saveData.ActiveOrders.Count} active orders.");
            }
            else
            {
                string reason = "";
                if (_deliveryService == null) reason += "DeliveryService is null; ";
                if (saveData.ActiveOrders == null) reason += "ActiveOrders data is null; ";
                Debug.LogWarning($"SaveGameService: Cannot restore active orders - {reason}");
            }

            // Переходим в игру после успешной загрузки только если мы НЕ в игровой сцене
            if (_sceneManagementService != null)
            {
                // Проверяем текущую сцену
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (currentScene.name != "GameScene")
                {
                    Debug.Log("SaveGameService: Loading game scene after save data applied.");
                    _sceneManagementService.LoadScene("GameScene");
                }
                else
                {
                    Debug.Log("SaveGameService: Already in game scene, save data applied successfully.");
                    
                    // Если мы уже в игровой сцене (загрузка из игрового меню), 
                    // НЕ устанавливаем input mode принудительно - пусть UINavigationService управляет этим
                    // после PopAllScreens() он сам установит правильный режим
                    Debug.Log("SaveGameService: Input mode will be managed by UINavigationService after screen navigation.");
                }
            }
            else
            {
                Debug.LogError("SaveGameService: Cannot load GameScene - SceneManagementService is null");
            }
        }

        public void SetSelectedSaveFile(string saveName)
        {
            _selectedSaveFile = saveName;
        }

        public string GetSelectedSaveFile()
        {
            return _selectedSaveFile;
        }

        public void ClearSelectedSaveFile()
        {
            _selectedSaveFile = null;
        }

        public void AutoSave()
        {
            if (_playerDataService == null)
            {
                Debug.LogError("SaveGameService: Cannot auto-save - PlayerDataService is null");
                return;
            }

            SaveGame(_autoSaveName);
        }
        
        /// <summary>
        /// Захватывает скриншот игры и сохраняет его для сохранения
        /// </summary>
        /// <param name="saveName">Имя сохранения</param>
        /// <returns>Относительный путь к файлу скриншота или null в случае ошибки</returns>
        private string CaptureAndSaveScreenshot(string saveName)
        {
            try
            {
                // Создаем имя файла скриншота
                string screenshotFileName = $"{saveName}_screenshot.png";
                string screenshotPath = Path.Combine(SavePath, screenshotFileName);
                
                // Получаем основную камеру
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("SaveGameService: No main camera found for screenshot");
                    return null;
                }
                
                // Создаем RenderTexture для захвата
                int width = 256;  // Размер превью скриншота
                int height = 144; // 16:9 соотношение
                
                RenderTexture renderTexture = new RenderTexture(width, height, 24);
                RenderTexture currentRT = RenderTexture.active;
                
                // Настраиваем камеру для рендера в RenderTexture
                RenderTexture previousTarget = mainCamera.targetTexture;
                mainCamera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                
                // Рендерим кадр
                mainCamera.Render();
                
                // Создаем Texture2D и читаем пиксели
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();
                
                // Восстанавливаем исходные настройки камеры
                mainCamera.targetTexture = previousTarget;
                RenderTexture.active = currentRT;
                
                // Сохраняем в файл
                byte[] data = screenshot.EncodeToPNG();
                File.WriteAllBytes(screenshotPath, data);
                
                // Очищаем ресурсы
                UnityEngine.Object.DestroyImmediate(screenshot);
                renderTexture.Release();
                
                Debug.Log($"Screenshot saved: {screenshotPath}");
                return screenshotFileName; // Возвращаем только имя файла (относительный путь)
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to capture screenshot: {e.Message}");
                return null;
            }
        }
    }
} 