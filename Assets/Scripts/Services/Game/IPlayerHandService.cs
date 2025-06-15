// Если BoxData будет в своем неймспейсе, его нужно будет подключить
using Supermarket.Data; // Для PlayerHeldBoxData
using System; // Added for Action
using UnityEngine;

public interface IPlayerHandService
{
    bool IsHoldingBox();
    BoxData GetHeldBoxData();
    ProductConfig GetProductInHand(); // Возвращает ProductConfig из коробки в руках
    int GetQuantityInHand(); // Added to get the quantity of items in the held box
    void PickupBox(BoxData boxData);
    void ConsumeItemFromHand(int amount = 1); // Уменьшает количество товара в коробке в руках
    void ClearHand(); // Очищает руки (например, если коробка опустела)
    
    bool IsBoxOpen(); // New: Check if the held box is open
    void OpenBox();   // New: Opens the held box
    void CloseBox();  // New: Closes the held box
    void AddItemToOpenBox(ProductConfig product, int amount); // New: Adds item to an open box
    bool CanAddItemToOpenBox(ProductConfig product, int amount = 1); // New: Check if item can be added to open box

    // Новые методы для визуализации
    Transform GetBoxVisualsParent(); // Получить точку привязки визуальной коробки
    void SetBoxVisualsParent(Transform visualsParent); // Установить точку привязки

    // Событие для UI, чтобы обновить отображение предмета в руках
    event Action OnHandContentChanged; // Changed to simple Action
    event Action OnBoxStateChanged; // New: Event for box open/close state changes
    
    // Новые события для визуализации
    event Action<BoxData> OnBoxPickedUp; // Когда коробка взята в руки
    event Action OnBoxDropped; // Когда коробка выброшена из рук
    
    // Методы для сохранения/восстановления
    PlayerHeldBoxData GetSaveData(); // Получить данные для сохранения
    void RestoreFromSaveData(PlayerHeldBoxData saveData); // Восстановить из сохраненных данных
    event Action<ProductConfig, int> OnItemConsumedFromBox; // Когда товар изъят из коробки
    event Action<ProductConfig, int> OnItemAddedToBox; // Когда товар добавлен в коробку
} 