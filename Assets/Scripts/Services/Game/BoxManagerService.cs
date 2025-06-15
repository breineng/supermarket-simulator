using System.Collections.Generic;
using UnityEngine;
using BehaviourInject;
using Supermarket.Data;
using Supermarket.Interactables;

namespace Supermarket.Services.Game
{
    public class BoxManagerService : MonoBehaviour, IBoxManagerService
    {
        [Header("Box Configuration")]
        [SerializeField] private GameObject _boxPrefab; // Префаб коробки для восстановления
        
        [Inject] public IProductCatalogService _productCatalogService;
        
        // Список всех активных коробок на сцене
        private readonly List<BoxController> _activeBoxes = new List<BoxController>();
        
        void Awake()
        {
            if (_boxPrefab == null)
            {
                Debug.LogError("BoxManagerService: Box Prefab is not assigned in the Inspector!", this);
            }
        }
        
        public void RegisterBox(BoxController box)
        {
            if (box == null)
            {
                Debug.LogWarning("BoxManagerService: Attempted to register null box");
                return;
            }
            
            if (!_activeBoxes.Contains(box))
            {
                _activeBoxes.Add(box);
                Debug.Log($"BoxManagerService: Registered box '{box.gameObject.name}'. Total active boxes: {_activeBoxes.Count}");
            }
        }
        
        public void UnregisterBox(BoxController box)
        {
            if (box == null) return;
            
            if (_activeBoxes.Remove(box))
            {
                Debug.Log($"BoxManagerService: Unregistered box '{box.gameObject.name}'. Total active boxes: {_activeBoxes.Count}");
            }
        }
        
        public List<BoxSaveData> GetBoxesSaveData()
        {
            List<BoxSaveData> boxesData = new List<BoxSaveData>();
            
            foreach (var box in _activeBoxes)
            {
                if (box == null || box.CurrentBoxData == null) continue;
                
                // Собираем данные коробки
                BoxSaveData saveData = new BoxSaveData
                {
                    Position = box.transform.position,
                    ItemCount = box.CurrentBoxData.Quantity,
                    ProductType = box.CurrentBoxData.ProductInBox?.ProductID ?? "",
                    IsOpen = false
                };
                
                boxesData.Add(saveData);
                string productName = box.CurrentBoxData.ProductInBox?.ProductName ?? "Empty";
                Debug.Log($"BoxManagerService: Collected save data for box at {saveData.Position} with {saveData.ItemCount}x {productName}");
            }
            
            Debug.Log($"BoxManagerService: Collected {boxesData.Count} boxes for saving");
            return boxesData;
        }
        
        public void RestoreBoxes(List<BoxSaveData> boxesData)
        {
            if (boxesData == null || boxesData.Count == 0)
            {
                Debug.Log("BoxManagerService: No boxes data to restore");
                return;
            }
            
            // Очищаем существующие коробки перед восстановлением
            ClearAllBoxes();
            
            foreach (var saveData in boxesData)
            {
                // Находим продукт по ID
                ProductConfig product = null;
                if (!string.IsNullOrEmpty(saveData.ProductType) && _productCatalogService != null)
                {
                    product = _productCatalogService.GetProductConfigByID(saveData.ProductType);
                    if (product == null)
                    {
                        Debug.LogWarning($"BoxManagerService: Product ID '{saveData.ProductType}' not found in catalog");
                    }
                }
                
                // Создаем BoxData
                BoxData boxData = new BoxData(product, saveData.ItemCount);
                
                // Создаем коробку в нужной позиции (без поворота, так как его нет в BoxSaveData)
                CreateBoxInternal(boxData, saveData.Position, Quaternion.identity, true, Vector3.zero);
                
                string productName = product?.ProductName ?? "Empty";
                Debug.Log($"BoxManagerService: Restored box at {saveData.Position} with {saveData.ItemCount}x {productName}");
            }
            
            Debug.Log($"BoxManagerService: Restored {boxesData.Count} boxes");
        }
        
        public void CreateBox(BoxData boxData, Vector3 position, bool isPhysical)
        {
            CreateBoxInternal(boxData, position, Quaternion.identity, isPhysical, Vector3.zero);
        }
        
        public void CreateBox(BoxData boxData, Vector3 position, bool isPhysical, Vector3 initialVelocity)
        {
            CreateBoxInternal(boxData, position, Quaternion.identity, isPhysical, initialVelocity);
        }
        
        private void CreateBoxInternal(BoxData boxData, Vector3 position, Quaternion rotation, bool isPhysical, Vector3 initialVelocity = default)
        {
            if (_boxPrefab == null)
            {
                Debug.LogError("BoxManagerService: Cannot create box - prefab not assigned!");
                return;
            }
            
            if (boxData == null)
            {
                Debug.LogError("BoxManagerService: Cannot create box - boxData is null!");
                return;
            }
            
            // Создаем объект коробки
            GameObject boxInstance = Instantiate(_boxPrefab, position, rotation);
            
            // Получаем компонент BoxController
            BoxController boxController = boxInstance.GetComponent<BoxController>();
            if (boxController == null)
            {
                Debug.LogError($"BoxManagerService: Box prefab '{_boxPrefab.name}' is missing BoxController component!", boxInstance);
                Destroy(boxInstance);
                return;
            }
            
            // Инициализируем коробку
            boxController.Initialize(boxData);
            
            // Настраиваем физику, если нужно
            if (isPhysical)
            {
                boxController.SetPhysicalAndDrop(initialVelocity);
            }
            
            // Коробка автоматически зарегистрируется через BoxController.Start()
            string productName = boxData.ProductInBox != null ? boxData.ProductInBox.ProductName : "Empty";
            Debug.Log($"BoxManagerService: Created box '{productName}' x{boxData.Quantity} at {position} (Physical: {isPhysical})");
        }
        
        public void ClearAllBoxes()
        {
            Debug.Log($"BoxManagerService: Clearing {_activeBoxes.Count} active boxes");
            
            // Создаем копию списка, так как коробки будут удаляться из списка при Destroy
            var boxesToDestroy = new List<BoxController>(_activeBoxes);
            
            foreach (var box in boxesToDestroy)
            {
                if (box != null)
                {
                    Destroy(box.gameObject);
                }
            }
            
            _activeBoxes.Clear();
            Debug.Log("BoxManagerService: All boxes cleared");
        }
    }
} 