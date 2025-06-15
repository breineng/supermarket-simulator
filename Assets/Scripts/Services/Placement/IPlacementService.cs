using UnityEngine;
using System.Collections.Generic;
using Supermarket.Data; // Для PlacedObjectData

public enum PlaceableObjectType
{
    None, // Not a placeable object or default
    Shelf,
    CashDesk,
    Goods
    // Add other types later e.g. CashDesk
}

public interface IPlacementService
{
    void StartPlacementMode(ProductConfig productToPlace);
    void UpdatePlacementPosition(Vector3 worldPosition, bool raycastHitSurface);
    bool ConfirmPlacement();
    void CancelPlacementMode();
    bool IsInPlacementMode { get; }
    PlaceableObjectType GetCurrentObjectType();
    void RotatePreview(float rotationAmount);
    
    // Методы для сохранения/загрузки
    List<PlacedObjectData> GetPlacedObjectsData();
    void RestorePlacedObjects(List<PlacedObjectData> placedObjectsData);
    void ClearAllPlacedObjects();
    
    // Методы для перемещения размещенных объектов
    bool StartRelocateMode(GameObject objectToRelocate);
    bool IsInRelocateMode { get; }
    GameObject GetRelocatingObject();
    
    // Методы для предустановленных объектов
    void RegisterPreplacedObject(GameObject preplacedObject, string objectId, PlaceableObjectType objectType);
    
    // Метод для удаления одного конкретного объекта
    bool RemovePlacedObject(GameObject objectToRemove);
} 