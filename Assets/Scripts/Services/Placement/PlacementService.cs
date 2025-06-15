using UnityEngine;
using Supermarket.Services.Game; // Added for IPlayerHandService
using System.Collections.Generic; // Для List
using Supermarket.Data; // Для PlacedObjectData
// Для [Inject] больше не нужно BehaviourInject, если только для других целей

public class PlacementService : IPlacementService
{
    public bool IsInPlacementMode
    {
        get { return _isPlacingObject; }
    }
    
    public bool IsInRelocateMode
    {
        get { return _isInRelocateMode; }
    }
    
    public GameObject GetRelocatingObject()
    {
        return _relocatingObject;
    }

    private PlaceableObjectType _currentObjectType;
    private GameObject _placementPreviewInstance;
    private Collider _placementPreviewCollider; // Кэшируем коллайдер превью
    private readonly IProductCatalogService _productCatalogService;
    private readonly IInputModeService _inputModeService; 
    private readonly PlacementServiceConfig _config; // Новое поле для конфига
    private readonly IPlayerHandService _playerHandService; // Added IPlayerHandService
    // _mainCamera здесь больше не нужен, его будет использовать PlacementController

    private bool _isCurrentPlacementValid = true; 
    private Color _validPlacementColor = new Color(0, 1, 0, 0.5f); // Зеленый полупрозрачный
    private Color _invalidPlacementColor = new Color(1, 0, 0, 0.5f); // Красный полупрозрачный

    // Новые поля для управления превью и финальным объектом
    private ProductConfig _currentProductConfig;
    private Material _previewMaterialInstance; // Инстанс материала для превью

    // Удаляем прямое поле и свойство для _collisionCheckLayers, оно теперь будет из _config
    private readonly LayerMask _collisionCheckLayers;
    private bool _isPlacingObject = false; // Добавлено поле для явного отслеживания состояния
    private bool _isPlacingFromHandSession = false; // Флаг для отслеживания сессии размещения из руки

    // Система отслеживания размещенных объектов
    private List<GameObject> _placedObjects = new List<GameObject>();
    private List<PlacedObjectData> _placedObjectsData = new List<PlacedObjectData>();

    // Переменные для режима перемещения
    private bool _isInRelocateMode = false;
    private GameObject _relocatingObject = null;
    private PlacedObjectData _relocatingObjectData = null;
    private int _relocatingObjectIndex = -1;

    // Зависимость внедряется через конструктор
    public PlacementService(IProductCatalogService productCatalogService, IInputModeService inputModeService, PlacementServiceConfig config, IPlayerHandService playerHandService)
    {
        _productCatalogService = productCatalogService;
        _inputModeService = inputModeService;
        _config = config;
        _playerHandService = playerHandService; // Assign IPlayerHandService

        if (_productCatalogService == null)
        {
            Debug.LogError("PlacementService: IProductCatalogService is null!");
        }
        if (_inputModeService == null)
        {
            Debug.LogError("PlacementService: IInputModeService is null!");
        }
        if (_playerHandService == null) // Check for playerHandService
        {
            Debug.LogError("PlacementService: IPlayerHandService is null!");
        }
        if (_config == null)
        {
            Debug.LogError("PlacementService: PlacementServiceConfig is null! Defaulting CollisionCheckLayers.");
            _collisionCheckLayers = Physics.DefaultRaycastLayers; // Запасной вариант, если конфиг не предоставлен
        }
        else
        {
            _collisionCheckLayers = _config.CollisionCheckLayers;
            // Debug.Log($"PlacementService Constructor: Loaded CollisionCheckLayers from config. Value: {_collisionCheckLayers.value} (Everything = -1)"); // Убрано
            // Можно также инициализировать _validPlacementColor и _invalidPlacementColor из _config, если они там будут
            // _validPlacementColor = _config.validPlacementColor;
            // _invalidPlacementColor = _config.invalidPlacementColor;
        }
    }

    public PlaceableObjectType GetCurrentObjectType()
    {
        if (!IsInPlacementMode || _currentProductConfig == null) // Проверяем _currentProductConfig
        {
            Debug.LogWarning("PlacementService: Not in placement mode or no current product. Cannot get current object type.");
            return PlaceableObjectType.None; // Возвращаем None, если нет активного продукта
        }
        return _currentProductConfig.ObjectCategory; // Возвращаем категорию из текущего ProductConfig
    }

    public void StartPlacementMode(ProductConfig productToPlace)
    {
        if (IsInPlacementMode)
        {
            CancelPlacementMode(); // Очищаем предыдущее состояние, если было
        }

        if (productToPlace == null || productToPlace.Prefab == null)
        {
            Debug.LogError("PlacementService: ProductConfig to place or its Prefab is null. Cannot start placement mode.");
            _currentProductConfig = null;
            _isPlacingObject = false; // Убедимся, что флаг сброшен
            _isPlacingFromHandSession = false; // Сбрасываем флаг сессии
            return;
        }

        _currentProductConfig = productToPlace; // Сохраняем переданный ProductConfig
        _currentObjectType = _currentProductConfig.ObjectCategory; // Обновляем _currentObjectType для GetCurrentObjectType()
        _isPlacingObject = false; // Убедимся, что флаг сброшен перед созданием превью
        _isPlacingFromHandSession = false; // И сессия тоже сброшена

        // Создаем экземпляр превью из _currentProductConfig.Prefab
        _placementPreviewInstance = Object.Instantiate(_currentProductConfig.Prefab);
        _placementPreviewCollider = _placementPreviewInstance.GetComponentInChildren<Collider>();
        if (_placementPreviewCollider == null) 
        {
            Debug.LogWarning($"PlacementService: Preview instance for {_currentProductConfig.ProductName} has no Collider. Collision check might not work as expected.");
        }
        
        Renderer previewRenderer = _placementPreviewInstance.GetComponentInChildren<Renderer>();
        if (previewRenderer != null)
        {
            // Создаем новый материал, чтобы не менять общий материал префаба
            _previewMaterialInstance = new Material(previewRenderer.sharedMaterial); 
            previewRenderer.material = _previewMaterialInstance;
        }
        else
        {
            Debug.LogWarning($"PlacementService: Preview instance for {_currentProductConfig.ProductName} has no Renderer. Cannot change material color.");
        }
        
        _isCurrentPlacementValid = true; 
        UpdatePreviewMaterial(); 
        
        // Устанавливаем флаг сессии размещения из руки
        _isPlacingFromHandSession = (_playerHandService.IsHoldingBox() && 
                                     _playerHandService.GetProductInHand() == productToPlace &&
                                     _playerHandService.IsBoxOpen());
        
        _isPlacingObject = true; // Устанавливаем флаг после успешного старта
        _inputModeService?.SetInputMode(InputMode.Game);
        Debug.Log($"PlacementService: Started placement mode for Product: {_currentProductConfig.ProductName}. Preview: {_placementPreviewInstance.name}. IsFromHandSession: {_isPlacingFromHandSession}. Switched to GAME input mode for placement.");
    }

    public void UpdatePlacementPosition(Vector3 worldPosition, bool raycastHitValidSurface)
    {
        if ((!IsInPlacementMode && !IsInRelocateMode) || _placementPreviewInstance == null) return;
        _placementPreviewInstance.transform.position = worldPosition;
        // Поворот превью можно будет добавить здесь, если нужно
        // _placementPreviewInstance.transform.rotation = Quaternion.LookRotation(someForwardDirection);

        // Убираем проверку высоты worldPosition.y
        if (!raycastHitValidSurface) 
        {
            _isCurrentPlacementValid = false;
        }
        else
        {
            // Если луч попал на валидную поверхность, проверяем на пересечения
            _isCurrentPlacementValid = !CheckForCollisions();
        }
        UpdatePreviewMaterial();
    }

    private bool CheckForCollisions()
    {
        if (_placementPreviewInstance == null || _placementPreviewCollider == null)
        {
            return false; 
        }

        // Получаем центр коллайдера в мировых координатах
        Vector3 colliderCenter = _placementPreviewCollider.bounds.center;
        Quaternion previewRotation = _placementPreviewInstance.transform.rotation;
        
        // Получаем локальные размеры коллайдера с учетом масштаба
        Vector3 localExtents = GetColliderLocalExtents(_placementPreviewCollider);

        // Визуальный дебаг bounds превью объекта (показываем oriented bounding box)
        DrawOrientedBounds(colliderCenter, localExtents, previewRotation, Color.cyan, 0.1f);

        Collider[] colliders = Physics.OverlapBox(colliderCenter, localExtents, previewRotation, _collisionCheckLayers, QueryTriggerInteraction.Ignore);

        if (colliders.Length > 0)
        {
            bool actualCollisionFound = false;
            foreach (var col in colliders)
            {
                if (col != _placementPreviewCollider && !IsColliderPartOfPreview(col, _placementPreviewInstance.transform))
                {
                    actualCollisionFound = true;
                    
                    // Визуальный дебаг коллизий - рисуем красные bounds для объектов, с которыми есть пересечение
                    DrawBounds(col.bounds, Color.red, 0.1f);
                    
                    // Рисуем линию от центра превью к центру коллизирующего объекта
                    Debug.DrawLine(colliderCenter, col.bounds.center, Color.yellow, 0.1f);
                }
                else
                {
                    // Визуальный дебаг для объектов, которые игнорируются (часть превью)
                    DrawBounds(col.bounds, Color.gray, 0.1f);
                }
            }
            return actualCollisionFound;
        }
        return false; 
    }

    // Вспомогательный метод для проверки принадлежности коллайдера к иерархии превью
    private bool IsColliderPartOfPreview(Collider detectedCollider, Transform previewRoot)
    {
        Transform current = detectedCollider.transform;
        while (current != null)
        {
            if (current == previewRoot) return true;
            current = current.parent;
        }
        return false;
    }

    private void UpdatePreviewMaterial()
    {
        if (_placementPreviewInstance == null || _previewMaterialInstance == null) return;
        // Renderer previewRenderer = _placementPreviewInstance.GetComponentInChildren<Renderer>(); // Уже не нужно, т.к. материал сохранен
        // if (previewRenderer != null && _previewMaterialInstance != null) // Проверка _previewMaterialInstance уже есть
        _previewMaterialInstance.color = _isCurrentPlacementValid ? _validPlacementColor : _invalidPlacementColor;
    }

    public bool ConfirmPlacement()
    {
        // Обрабатываем режим перемещения
        if (IsInRelocateMode)
        {
            return ConfirmRelocation();
        }
        
        // Обычный режим размещения
        if (!IsInPlacementMode || !_isPlacingObject || _placementPreviewInstance == null || _currentProductConfig == null)
        {
            Debug.LogWarning("PlacementService: Not in placement mode, no preview instance, or no product config. Cannot confirm.");
            return false;
        }

        if (!_isCurrentPlacementValid) // Проверка перед подтверждением
        {
            Debug.LogWarning("PlacementService: Current placement is invalid. Cannot confirm.");
            return false; 
        }

        // Создаем финальный объект из _currentProductConfig.Prefab
        GameObject finalObject = Object.Instantiate(_currentProductConfig.Prefab, _placementPreviewInstance.transform.position, _placementPreviewInstance.transform.rotation);
        finalObject.name = _currentProductConfig.ProductName + "_Placed"; // Даем осмысленное имя
        finalObject.tag = "PlacedObject"; // Устанавливаем тег для размещенных объектов
        Debug.Log($"PlacementService: Confirmed placement of {finalObject.name} at {_placementPreviewInstance.transform.position}.");

        // Регистрируем размещенный объект для сохранения
        _placedObjects.Add(finalObject);
        _placedObjectsData.Add(new PlacedObjectData
        {
            PrefabName = _currentProductConfig.ProductID, // Используем ProductID для корректного восстановления
            Position = finalObject.transform.position,
            Rotation = finalObject.transform.rotation,
            ObjectType = _currentProductConfig.ObjectCategory.ToString()
        });
        Debug.Log($"PlacementService: Registered placed object with ID '{_currentProductConfig.ProductID}' for save system.");

        bool consumedFromHand = false;
        if (_playerHandService != null && 
            _playerHandService.IsHoldingBox() && 
            _playerHandService.GetProductInHand() == _currentProductConfig)
        {
            _playerHandService.ConsumeItemFromHand(1);
            consumedFromHand = true;
            Debug.Log($"PlacementService: Consumed 1 unit of {_currentProductConfig.ProductName} from player\\'s hand.");
        }
        else if (_playerHandService != null && _playerHandService.IsHoldingBox() && _playerHandService.GetProductInHand() != _currentProductConfig)
        {
            Debug.LogWarning($"PlacementService: Player is holding {_playerHandService.GetProductInHand()?.ProductName} but placed {_currentProductConfig.ProductName}. Item not consumed from hand. This might indicate a logic issue if placement was meant to be from hand.");
        }
        
        // Логика продолжения или завершения сессии размещения
        if (consumedFromHand && _isPlacingFromHandSession && _playerHandService.IsBoxOpen() && _playerHandService.GetQuantityInHand() > 0)
        {
            Debug.Log("PlacementService: Continuing placement from hand session.");
            
            // Уничтожаем старое превью
            if (_placementPreviewInstance != null)
            {
                Object.Destroy(_placementPreviewInstance);
                // _placementPreviewInstance = null; // Не обязательно, так как сразу присваиваем новое значение
            }
            if (_previewMaterialInstance != null) 
            {
                Object.Destroy(_previewMaterialInstance);
                // _previewMaterialInstance = null; // Не обязательно
            }

            // _currentProductConfig остается тем же
            // Создаем новый экземпляр превью
            _placementPreviewInstance = Object.Instantiate(_currentProductConfig.Prefab);
            _placementPreviewCollider = _placementPreviewInstance.GetComponentInChildren<Collider>();
            Renderer previewRenderer = _placementPreviewInstance.GetComponentInChildren<Renderer>();
            if (previewRenderer != null)
            {
                _previewMaterialInstance = new Material(previewRenderer.sharedMaterial);
                previewRenderer.material = _previewMaterialInstance;
            }
            else
            {
                Debug.LogWarning($"PlacementService: New preview instance for {_currentProductConfig.ProductName} has no Renderer. Cannot change material color.");
                 _previewMaterialInstance = null; // Явно обнуляем, если рендерера нет
            }
            _isCurrentPlacementValid = true; 
            UpdatePreviewMaterial(); 
            // _isPlacingObject остается true
            // _inputModeService?.SetInputMode(InputMode.Game); // Режим уже должен быть Game
            
            Debug.Log($"PlacementService: New preview created for continuous placement of {_currentProductConfig.ProductName}.");
        }
        else
        {
            // Завершаем сессию размещения
            if (_placementPreviewInstance != null)
            {
                Object.Destroy(_placementPreviewInstance);
                _placementPreviewInstance = null;
            }
            if (_previewMaterialInstance != null)
            {
                Object.Destroy(_previewMaterialInstance);
                _previewMaterialInstance = null;
            }
            
            _currentProductConfig = null; 
            _isPlacingObject = false; 
            _isPlacingFromHandSession = false; // Сбрасываем флаг сессии
            _inputModeService?.SetInputMode(InputMode.Game); 
            Debug.Log("PlacementService: Placement session ended. Preview destroyed. Switched to GAME input mode.");
        }
        return true;
    }

    public void CancelPlacementMode()
    {
        // Если в режиме перемещения, отменяем перемещение
        if (IsInRelocateMode)
        {
            CancelRelocateMode();
            return;
        }
        
        // Более простая проверка: если нет превью, то и отменять нечего
        if (_placementPreviewInstance == null && !_isPlacingObject) 
        {
             Debug.LogWarning("PlacementService: No active placement preview or not in placement mode. Nothing to cancel.");
             // Убедимся, что все флаги сброшены на всякий случай
             _isPlacingObject = false;
             _isPlacingFromHandSession = false;
             _currentProductConfig = null;
             _inputModeService?.SetInputMode(InputMode.Game); // На всякий случай вернуть в Game режим
             return;
        }
        
        Debug.Log("PlacementService: Cancelling placement mode.");
        if (_placementPreviewInstance != null)
        {
            Object.Destroy(_placementPreviewInstance);
            _placementPreviewInstance = null;
        }
        if (_previewMaterialInstance != null)
        {
            Object.Destroy(_previewMaterialInstance);
            _previewMaterialInstance = null;
        }

        _currentProductConfig = null;
        _isPlacingObject = false; 
        _isPlacingFromHandSession = false; // Сбрасываем флаг сессии
        _inputModeService?.SetInputMode(InputMode.Game);
        Debug.Log("PlacementService: Placement mode cancelled. Preview destroyed. Switched to GAME input mode.");
    }

    public void RotatePreview(float rotationAmount) // rotationAmount может быть -1 или 1 для направления, или значением с оси
    {
        if ((!IsInPlacementMode && !IsInRelocateMode) || _placementPreviewInstance == null) return;

        float angleToRotate = _config.RotationStepAngle * rotationAmount; // Используем rotationAmount как множитель
        _placementPreviewInstance.transform.Rotate(Vector3.up, angleToRotate);

        // После поворота нужно перепроверить валидность размещения
        _isCurrentPlacementValid = !CheckForCollisions();
        UpdatePreviewMaterial();
        Debug.Log($"PlacementService: Rotated preview by {angleToRotate} degrees.");
    }

    public bool IsPlacing() // Новый метод, который уже был добавлен
    {
        return _isPlacingObject;
    }

    // Методы для сохранения/загрузки
    public List<PlacedObjectData> GetPlacedObjectsData()
    {
        return new List<PlacedObjectData>(_placedObjectsData);
    }

    public void RestorePlacedObjects(List<PlacedObjectData> placedObjectsData)
    {
        if (placedObjectsData == null || _productCatalogService == null) 
        {
            Debug.LogWarning($"PlacementService.RestorePlacedObjects: placedObjectsData null: {placedObjectsData == null}, _productCatalogService null: {_productCatalogService == null}");
            return;
        }

        Debug.Log($"PlacementService.RestorePlacedObjects: Starting restoration of {placedObjectsData.Count} objects");

        // Сначала создаем список специальных ID предустановленных объектов, которые не нужно очищать
        var preplacedObjectIds = new List<string> { "Computer" };
        
        // Проверяем синхронизацию массивов
        if (_placedObjects.Count != _placedObjectsData.Count)
        {
            Debug.LogWarning($"PlacementService: Arrays out of sync! _placedObjects.Count = {_placedObjects.Count}, _placedObjectsData.Count = {_placedObjectsData.Count}. Fixing...");
            
            // Приводим к минимальному размеру
            int minCount = Mathf.Min(_placedObjects.Count, _placedObjectsData.Count);
            if (_placedObjects.Count > minCount)
            {
                _placedObjects.RemoveRange(minCount, _placedObjects.Count - minCount);
            }
            if (_placedObjectsData.Count > minCount)
            {
                _placedObjectsData.RemoveRange(minCount, _placedObjectsData.Count - minCount);
            }
            
            Debug.Log($"PlacementService: After sync: _placedObjects.Count = {_placedObjects.Count}, _placedObjectsData.Count = {_placedObjectsData.Count}");
        }
        
        // Очищаем только объекты, которые НЕ являются предустановленными
        for (int i = _placedObjects.Count - 1; i >= 0; i--)
        {
            // Дополнительная проверка границ
            if (i < _placedObjectsData.Count)
            {
                var objectData = _placedObjectsData[i];
                if (!preplacedObjectIds.Contains(objectData.PrefabName))
                {
                    if (_placedObjects[i] != null)
                    {
                        Object.Destroy(_placedObjects[i]);
                    }
                    _placedObjects.RemoveAt(i);
                    _placedObjectsData.RemoveAt(i);
                }
            }
            else
            {
                Debug.LogWarning($"PlacementService: Index {i} out of bounds for _placedObjectsData (size: {_placedObjectsData.Count}). Removing object only.");
                if (_placedObjects[i] != null)
                {
                    Object.Destroy(_placedObjects[i]);
                }
                _placedObjects.RemoveAt(i);
            }
        }

        foreach (var objectData in placedObjectsData)
        {
            Debug.Log($"PlacementService.RestorePlacedObjects: Processing object with PrefabName: '{objectData.PrefabName}'");
            
            // Проверяем, есть ли уже этот объект в системе (для предустановленных объектов)
            bool isPreplacedObject = false;
            GameObject existingObject = null;
            int existingIndex = -1;
            
            for (int i = 0; i < _placedObjectsData.Count; i++)
            {
                if (_placedObjectsData[i].PrefabName == objectData.PrefabName)
                {
                    // Проверяем, что индекс не выходит за границы массива _placedObjects
                    if (i < _placedObjects.Count && _placedObjects[i] != null)
                    {
                        isPreplacedObject = true;
                        existingObject = _placedObjects[i];
                        existingIndex = i;
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"PlacementService: Found data for '{objectData.PrefabName}' at index {i}, but _placedObjects array has only {_placedObjects.Count} elements or object is null. Arrays may be out of sync.");
                        break;
                    }
                }
            }
            
            if (isPreplacedObject && existingObject != null)
            {
                // Обновляем позицию существующего предустановленного объекта
                existingObject.transform.position = objectData.Position;
                existingObject.transform.rotation = objectData.Rotation;
                
                // Обновляем данные сохранения
                _placedObjectsData[existingIndex] = new PlacedObjectData
                {
                    PrefabName = objectData.PrefabName,
                    Position = objectData.Position,
                    Rotation = objectData.Rotation,
                    ObjectType = objectData.ObjectType
                };
                
                Debug.Log($"PlacementService: Updated preplaced object '{objectData.PrefabName}' position to {objectData.Position}.");
                continue;
            }
            
            // Используем PrefabName как ProductID для поиска конфигурации
            ProductConfig productConfig = _productCatalogService.GetProductConfigByID(objectData.PrefabName);
            if (productConfig == null)
            {
                Debug.LogWarning($"PlacementService: Could not find ProductConfig for ID '{objectData.PrefabName}'. Available products in catalog:");
                
                // Выводим все доступные продукты для отладки
                var allProducts = _productCatalogService.GetAllProductConfigs();
                foreach (var product in allProducts)
                {
                    Debug.Log($"  - Available ProductID: '{product.ProductID}', Name: '{product.ProductName}'");
                }
                
                Debug.LogWarning($"PlacementService: Skipping restore of '{objectData.PrefabName}'.");
                continue;
            }

            Debug.Log($"PlacementService.RestorePlacedObjects: Found ProductConfig '{productConfig.ProductName}' for ID '{objectData.PrefabName}'");
            
            if (productConfig.Prefab == null)
            {
                Debug.LogError($"PlacementService: ProductConfig '{productConfig.ProductName}' has null Prefab! Skipping restore.");
                continue;
            }

            // Создаем новый объект (для не предустановленных объектов)
            GameObject restoredObject = Object.Instantiate(productConfig.Prefab, objectData.Position, objectData.Rotation);
            restoredObject.name = objectData.PrefabName + "_Restored";
            restoredObject.tag = "PlacedObject"; // Устанавливаем тег для восстановленных объектов

            // Регистрируем восстановленный объект
            _placedObjects.Add(restoredObject);
            _placedObjectsData.Add(new PlacedObjectData
            {
                PrefabName = objectData.PrefabName,
                Position = objectData.Position,
                Rotation = objectData.Rotation,
                ObjectType = objectData.ObjectType
            });

            Debug.Log($"PlacementService: Successfully restored object '{objectData.PrefabName}' at {objectData.Position}. Created GameObject: '{restoredObject.name}'");
        }

        Debug.Log($"PlacementService: Restoration completed. Successfully restored {_placedObjects.Count} placed objects.");
    }

    public void ClearAllPlacedObjects()
    {
        // Уничтожаем все размещенные объекты
        foreach (var obj in _placedObjects)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }

        _placedObjects.Clear();
        _placedObjectsData.Clear();
        Debug.Log("PlacementService: Cleared all placed objects.");
    }

    public bool StartRelocateMode(GameObject objectToRelocate)
    {
        if (objectToRelocate == null)
        {
            Debug.LogWarning("PlacementService: Cannot start relocate mode - object to relocate is null.");
            return false;
        }
        
        // Проверяем, находится ли объект в списке размещенных объектов
        int objectIndex = _placedObjects.IndexOf(objectToRelocate);
        if (objectIndex == -1)
        {
            Debug.LogWarning($"PlacementService: Object {objectToRelocate.name} is not in the placed objects list. Cannot relocate.");
            return false;
        }
        
        // Если уже в режиме размещения или перемещения, отменяем предыдущий режим
        if (IsInPlacementMode)
        {
            CancelPlacementMode();
        }
        if (IsInRelocateMode)
        {
            CancelRelocateMode();
        }
        
        // Находим ProductConfig для этого объекта
        PlacedObjectData objectData = _placedObjectsData[objectIndex];
        ProductConfig productConfig = _productCatalogService.GetProductConfigByID(objectData.PrefabName);
        
        // Для предустановленных объектов (например, Computer) может не быть ProductConfig
        // В этом случае создаем превью на основе оригинального объекта
        if (productConfig == null)
        {
            Debug.LogWarning($"PlacementService: Could not find ProductConfig for ID '{objectData.PrefabName}'. Using original object for preview.");
            
            // Сохраняем информацию о перемещаемом объекте
            _relocatingObject = objectToRelocate;
            _relocatingObjectData = objectData;
            _relocatingObjectIndex = objectIndex;
            _currentProductConfig = null; // Нет конфигурации для предустановленных объектов
            _currentObjectType = PlaceableObjectType.None; // Тип по умолчанию
            _isInRelocateMode = true;
            
            // Сначала создаем превью на основе оригинального объекта (пока он еще видим)
            _placementPreviewInstance = Object.Instantiate(objectToRelocate, objectToRelocate.transform.position, objectToRelocate.transform.rotation);
            _placementPreviewInstance.name = objectToRelocate.name + "_Preview";
            _placementPreviewCollider = _placementPreviewInstance.GetComponentInChildren<Collider>();
            
            // Затем скрываем оригинальный объект
            HideObjectVisually(_relocatingObject);

            // Рекурсивно применяем слой Preview ко всем дочерним объектам
            SetLayerRecursively(_placementPreviewInstance.transform, LayerMask.NameToLayer("Preview"));
            
            Renderer preplacedPreviewRenderer = _placementPreviewInstance.GetComponentInChildren<Renderer>();
            if (preplacedPreviewRenderer != null)
            {
                _previewMaterialInstance = new Material(preplacedPreviewRenderer.sharedMaterial);
                preplacedPreviewRenderer.material = _previewMaterialInstance;
            }
            
            _isCurrentPlacementValid = true;
            UpdatePreviewMaterial();
            
            // Переключаем input mode
            _inputModeService?.SetInputMode(InputMode.Game);
            
            Debug.Log($"PlacementService: Started relocate mode for preplaced object {objectToRelocate.name}. Object visually hidden, preview created from original.");
            return true;
        }
        
        // Сохраняем информацию о перемещаемом объекте
        _relocatingObject = objectToRelocate;
        _relocatingObjectData = objectData;
        _relocatingObjectIndex = objectIndex;
        _currentProductConfig = productConfig;
        _currentObjectType = _currentProductConfig.ObjectCategory;
        _isInRelocateMode = true;
        
        // Скрываем оригинальный объект (отключаем рендереры и коллайдеры вместо SetActive)
        HideObjectVisually(_relocatingObject);
        
        // Создаем превью для перемещения
        _placementPreviewInstance = Object.Instantiate(_currentProductConfig.Prefab, objectToRelocate.transform.position, objectToRelocate.transform.rotation);
        _placementPreviewCollider = _placementPreviewInstance.GetComponentInChildren<Collider>();

        // Рекурсивно применяем слой Preview ко всем дочерним объектам
        SetLayerRecursively(_placementPreviewInstance.transform, LayerMask.NameToLayer("Preview"));
        
        Renderer previewRenderer = _placementPreviewInstance.GetComponentInChildren<Renderer>();
        if (previewRenderer != null)
        {
            _previewMaterialInstance = new Material(previewRenderer.sharedMaterial);
            previewRenderer.material = _previewMaterialInstance;
        }
        
        _isCurrentPlacementValid = true;
        UpdatePreviewMaterial();
        
        // Переключаем input mode
        _inputModeService?.SetInputMode(InputMode.Game);
        
        Debug.Log($"PlacementService: Started relocate mode for {objectToRelocate.name}. Object visually hidden, preview created.");
        return true;
    }
    
    /// <summary>
    /// Получает локальные extents коллайдера с учетом масштаба
    /// </summary>
    private Vector3 GetColliderLocalExtents(Collider collider)
    {
        if (collider == null) return Vector3.zero;
        
        // Для BoxCollider используем локальный размер
        if (collider is BoxCollider boxCollider)
        {
            Vector3 size = boxCollider.size;
            Vector3 scale = collider.transform.lossyScale;
            // Применяем масштаб и делим на 2 для получения extents (половина размера)
            return Vector3.Scale(size, scale) * 0.5f;
        }
        
        // Для других типов коллайдеров используем bounds как fallback
        // Но это может быть неточно для повернутых объектов
        return collider.bounds.extents;
    }
    
    /// <summary>
    /// Отрисовывает ориентированный bounding box с помощью Debug.DrawLine
    /// </summary>
    private void DrawOrientedBounds(Vector3 center, Vector3 extents, Quaternion rotation, Color color, float duration = 0.0f)
    {
        // Вычисляем 8 углов куба в локальных координатах, затем поворачиваем их
        Vector3[] localCorners = new Vector3[8];
        localCorners[0] = new Vector3(-extents.x, -extents.y, -extents.z); // Левый нижний задний
        localCorners[1] = new Vector3(extents.x, -extents.y, -extents.z);  // Правый нижний задний
        localCorners[2] = new Vector3(extents.x, -extents.y, extents.z);   // Правый нижний передний
        localCorners[3] = new Vector3(-extents.x, -extents.y, extents.z);  // Левый нижний передний
        localCorners[4] = new Vector3(-extents.x, extents.y, -extents.z);  // Левый верхний задний
        localCorners[5] = new Vector3(extents.x, extents.y, -extents.z);   // Правый верхний задний
        localCorners[6] = new Vector3(extents.x, extents.y, extents.z);    // Правый верхний передний
        localCorners[7] = new Vector3(-extents.x, extents.y, extents.z);   // Левый верхний передний
        
        // Поворачиваем углы и перемещаем в мировые координаты
        Vector3[] worldCorners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            worldCorners[i] = center + rotation * localCorners[i];
        }
        
        // Рисуем нижний квадрат
        Debug.DrawLine(worldCorners[0], worldCorners[1], color, duration);
        Debug.DrawLine(worldCorners[1], worldCorners[2], color, duration);
        Debug.DrawLine(worldCorners[2], worldCorners[3], color, duration);
        Debug.DrawLine(worldCorners[3], worldCorners[0], color, duration);
        
        // Рисуем верхний квадрат
        Debug.DrawLine(worldCorners[4], worldCorners[5], color, duration);
        Debug.DrawLine(worldCorners[5], worldCorners[6], color, duration);
        Debug.DrawLine(worldCorners[6], worldCorners[7], color, duration);
        Debug.DrawLine(worldCorners[7], worldCorners[4], color, duration);
        
        // Рисуем вертикальные линии, соединяющие нижний и верхний квадраты
        Debug.DrawLine(worldCorners[0], worldCorners[4], color, duration);
        Debug.DrawLine(worldCorners[1], worldCorners[5], color, duration);
        Debug.DrawLine(worldCorners[2], worldCorners[6], color, duration);
        Debug.DrawLine(worldCorners[3], worldCorners[7], color, duration);
    }
    
    /// <summary>
    /// Отрисовывает bounds объекта с помощью Debug.DrawLine
    /// </summary>
    private void DrawBounds(Bounds bounds, Color color, float duration = 0.0f)
    {
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        
        // Вычисляем 8 углов куба
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f; // Левый нижний задний
        corners[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;  // Правый нижний задний
        corners[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;   // Правый нижний передний
        corners[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;  // Левый нижний передний
        corners[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;  // Левый верхний задний
        corners[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;   // Правый верхний задний
        corners[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;    // Правый верхний передний
        corners[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;   // Левый верхний передний
        
        // Рисуем нижний квадрат
        Debug.DrawLine(corners[0], corners[1], color, duration);
        Debug.DrawLine(corners[1], corners[2], color, duration);
        Debug.DrawLine(corners[2], corners[3], color, duration);
        Debug.DrawLine(corners[3], corners[0], color, duration);
        
        // Рисуем верхний квадрат
        Debug.DrawLine(corners[4], corners[5], color, duration);
        Debug.DrawLine(corners[5], corners[6], color, duration);
        Debug.DrawLine(corners[6], corners[7], color, duration);
        Debug.DrawLine(corners[7], corners[4], color, duration);
        
        // Рисуем вертикальные линии, соединяющие нижний и верхний квадраты
        Debug.DrawLine(corners[0], corners[4], color, duration);
        Debug.DrawLine(corners[1], corners[5], color, duration);
        Debug.DrawLine(corners[2], corners[6], color, duration);
        Debug.DrawLine(corners[3], corners[7], color, duration);
    }
    
    /// <summary>
    /// Рекурсивно применяет слой ко всем дочерним объектам
    /// </summary>
    private void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;
        
        // Применяем слой к текущему объекту
        root.gameObject.layer = layer;
        
        // Рекурсивно применяем слой ко всем дочерним объектам
        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }
    
    /// <summary>
    /// Скрывает объект визуально, отключая рендереры и коллайдеры, но сохраняя логику скриптов
    /// </summary>
    private void HideObjectVisually(GameObject obj)
    {
        if (obj == null) return;
        
        // Отключаем все рендереры в объекте и его дочерних объектах
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
        
        Debug.Log($"PlacementService: Visually hidden object {obj.name} (disabled {renderers.Length} renderers and {colliders.Length} colliders).");
    }
    
    /// <summary>
    /// Показывает объект визуально, включая рендереры и коллайдеры обратно
    /// </summary>
    private void ShowObjectVisually(GameObject obj)
    {
        if (obj == null) return;
        
        // Включаем все рендереры в объекте и его дочерних объектах
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
        
        // Включаем все коллайдеры в объекте и его дочерних объектах
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
        
        Debug.Log($"PlacementService: Visually shown object {obj.name} (enabled {renderers.Length} renderers and {colliders.Length} colliders).");
    }
    
    private void CancelRelocateMode()
    {
        if (!IsInRelocateMode)
        {
            Debug.LogWarning("PlacementService: Not in relocate mode. Nothing to cancel.");
            return;
        }
        
        Debug.Log("PlacementService: Cancelling relocate mode.");
        
        // Показываем обратно оригинальный объект (включаем рендереры и коллайдеры вместо SetActive)
        if (_relocatingObject != null)
        {
            ShowObjectVisually(_relocatingObject);
        }
        
        // Уничтожаем превью
        if (_placementPreviewInstance != null)
        {
            Object.Destroy(_placementPreviewInstance);
            _placementPreviewInstance = null;
        }
        if (_previewMaterialInstance != null)
        {
            Object.Destroy(_previewMaterialInstance);
            _previewMaterialInstance = null;
        }
        
        // Сбрасываем состояние
        _isInRelocateMode = false;
        _relocatingObject = null;
        _relocatingObjectData = null;
        _relocatingObjectIndex = -1;
        _currentProductConfig = null;
        
        _inputModeService?.SetInputMode(InputMode.Game);
        Debug.Log("PlacementService: Relocate mode cancelled. Original object visually restored.");
    }

    private bool ConfirmRelocation()
    {
        if (!IsInRelocateMode || _relocatingObject == null || _placementPreviewInstance == null)
        {
            Debug.LogWarning("PlacementService: Not in relocate mode, no relocating object, or no preview instance. Cannot confirm relocation.");
            return false;
        }

        if (!_isCurrentPlacementValid)
        {
            Debug.LogWarning("PlacementService: Current relocation position is invalid. Cannot confirm relocation.");
            return false;
        }

        // Перемещаем оригинальный объект в новую позицию
        _relocatingObject.transform.position = _placementPreviewInstance.transform.position;
        _relocatingObject.transform.rotation = _placementPreviewInstance.transform.rotation;
        
        // Показываем оригинальный объект обратно (включаем рендереры и коллайдеры вместо SetActive)
        ShowObjectVisually(_relocatingObject);
        
        // Обновляем данные для сохранения
        if (_relocatingObjectIndex >= 0 && _relocatingObjectIndex < _placedObjectsData.Count)
        {
            _placedObjectsData[_relocatingObjectIndex].Position = _relocatingObject.transform.position;
            _placedObjectsData[_relocatingObjectIndex].Rotation = _relocatingObject.transform.rotation;
        }
        
        Debug.Log($"PlacementService: Successfully relocated {_relocatingObject.name} to {_relocatingObject.transform.position}.");
        
        // Уничтожаем превью
        if (_placementPreviewInstance != null)
        {
            Object.Destroy(_placementPreviewInstance);
            _placementPreviewInstance = null;
        }
        if (_previewMaterialInstance != null)
        {
            Object.Destroy(_previewMaterialInstance);
            _previewMaterialInstance = null;
        }
        
        // Сбрасываем состояние режима перемещения
        _isInRelocateMode = false;
        _relocatingObject = null;
        _relocatingObjectData = null;
        _relocatingObjectIndex = -1;
        _currentProductConfig = null;
        
        _inputModeService?.SetInputMode(InputMode.Game);
        Debug.Log("PlacementService: Relocation confirmed and completed.");
        
        return true;
    }

    // Новый метод для регистрации предустановленных объектов
    public void RegisterPreplacedObject(GameObject preplacedObject, string objectId, PlaceableObjectType objectType)
    {
        if (preplacedObject == null)
        {
            Debug.LogWarning("PlacementService: Cannot register preplaced object - object is null.");
            return;
        }

        if (string.IsNullOrEmpty(objectId))
        {
            Debug.LogWarning("PlacementService: Cannot register preplaced object - objectId is null or empty.");
            return;
        }

        // Проверяем, не зарегистрирован ли уже этот объект
        if (_placedObjects.Contains(preplacedObject))
        {
            Debug.LogWarning($"PlacementService: Object {preplacedObject.name} is already registered.");
            return;
        }

        // Убеждаемся, что у объекта есть правильный тег
        if (!preplacedObject.CompareTag("PlacedObject"))
        {
            preplacedObject.tag = "PlacedObject";
            Debug.Log($"PlacementService: Set 'PlacedObject' tag on {preplacedObject.name}.");
        }

        // Регистрируем объект в системе
        _placedObjects.Add(preplacedObject);
        _placedObjectsData.Add(new PlacedObjectData
        {
            PrefabName = objectId,
            Position = preplacedObject.transform.position,
            Rotation = preplacedObject.transform.rotation,
            ObjectType = objectType.ToString()
        });

        Debug.Log($"PlacementService: Successfully registered preplaced object '{preplacedObject.name}' with ID '{objectId}' and type '{objectType}'.");
    }
    
    /// <summary>
    /// Удаляет конкретный размещенный объект из системы
    /// </summary>
    /// <param name="objectToRemove">Объект для удаления</param>
    /// <returns>True если объект был найден и удален, false если объект не найден</returns>
    public bool RemovePlacedObject(GameObject objectToRemove)
    {
        if (objectToRemove == null)
        {
            Debug.LogWarning("PlacementService: Cannot remove placed object - object is null.");
            return false;
        }

        // Ищем объект в списке размещенных объектов
        int objectIndex = _placedObjects.IndexOf(objectToRemove);
        if (objectIndex == -1)
        {
            Debug.LogWarning($"PlacementService: Object {objectToRemove.name} is not in the placed objects list. Cannot remove.");
            return false;
        }

        // Проверяем синхронизацию массивов
        if (objectIndex >= _placedObjectsData.Count)
        {
            Debug.LogError($"PlacementService: Array synchronization error! Object index {objectIndex} is out of bounds for _placedObjectsData (size: {_placedObjectsData.Count}).");
            return false;
        }

        // Если это объект, который сейчас перемещается, отменяем перемещение
        if (IsInRelocateMode && _relocatingObject == objectToRemove)
        {
            CancelRelocateMode();
        }

        // Удаляем из обоих списков
        _placedObjects.RemoveAt(objectIndex);
        _placedObjectsData.RemoveAt(objectIndex);

        Debug.Log($"PlacementService: Successfully removed placed object '{objectToRemove.name}' from tracking. Object was at index {objectIndex}.");
        
        return true;
    }
} 