using UnityEngine;
using BehaviourInject;

namespace Supermarket.Components
{
    /// <summary>
    /// Компонент для автоматической регистрации предустановленных объектов в PlacementService.
    /// Используется для объектов, которые уже размещены на сцене, но должны поддерживать перемещение и сохранение.
    /// </summary>
    public class PreplacedObjectRegistrar : MonoBehaviour
    {
        [SerializeField] 
        [Tooltip("Уникальный ID объекта для системы сохранения. Должен совпадать с ProductID в каталоге или быть уникальным.")]
        private string objectId = "Computer";
        
        [SerializeField]
        [Tooltip("Тип объекта для системы размещения.")]
        private PlaceableObjectType objectType = PlaceableObjectType.None;
        
        [SerializeField]
        [Tooltip("Должен ли объект быть зарегистрирован только если не найдены сохраненные данные для него.")]
        private bool registerOnlyIfNotInSave = true;

        [Inject] 
        public IPlacementService _placementService { get; set; }

        private void Start()
        {
            // Регистрируем объект сразу в Start(), так как dependency injection уже произошел
            RegisterObject();
        }

        private void RegisterObject()
        {
            Debug.Log($"PreplacedObjectRegistrar: Attempting to register {gameObject.name} with ID '{objectId}'...");
            
            if (_placementService == null)
            {
                Debug.LogError($"PreplacedObjectRegistrar: IPlacementService не внедрен в {gameObject.name}. Убедитесь, что объект находится в правильном контексте.", this);
                return;
            }

            Debug.Log($"PreplacedObjectRegistrar: PlacementService found. RegisterOnlyIfNotInSave = {registerOnlyIfNotInSave}");

            if (registerOnlyIfNotInSave)
            {
                // Проверяем, есть ли уже этот объект в сохраненных данных
                var existingData = _placementService.GetPlacedObjectsData();
                Debug.Log($"PreplacedObjectRegistrar: Found {existingData.Count} existing placed objects in PlacementService");
                
                foreach (var data in existingData)
                {
                    Debug.Log($"PreplacedObjectRegistrar: Existing object in save data: '{data.PrefabName}'");
                    if (data.PrefabName == objectId)
                    {
                        Debug.Log($"PreplacedObjectRegistrar: Object with ID '{objectId}' already exists in save data. Skipping registration of {gameObject.name}.");
                        return;
                    }
                }
                
                Debug.Log($"PreplacedObjectRegistrar: Object with ID '{objectId}' not found in existing data. Proceeding with registration.");
            }

            // Регистрируем объект в системе размещения
            _placementService.RegisterPreplacedObject(gameObject, objectId, objectType);
            Debug.Log($"PreplacedObjectRegistrar: Successfully registered {gameObject.name} with ID '{objectId}' and type '{objectType}'.");
        }

        // Метод для настройки через код (если нужно)
        public void SetConfiguration(string id, PlaceableObjectType type, bool onlyIfNotInSave = true)
        {
            objectId = id;
            objectType = type;
            registerOnlyIfNotInSave = onlyIfNotInSave;
        }
    }
} 