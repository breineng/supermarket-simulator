using UnityEngine;

[CreateAssetMenu(fileName = "ProductConfig", menuName = "Supermarket/Game Configuration/Product Config")]
public class ProductConfig : ScriptableObject
{
    [Header("Basic Info")]
    public string ProductID; // Уникальный идентификатор товара
    public string ProductName;
    [TextArea]
    public string Description;

    [Header("Visuals")]
    public Sprite Icon; // Для UI
    public GameObject Prefab; // Префаб товара для размещения в мире (если нужен)
    public bool ShowModelInBox = true; // Отображать ли модель товара в коробке (для слишком больших объектов можно отключить)

    [Header("Economic Info")]
    public float PurchasePrice; // Цена закупки
    public float BaseSalePrice; // Базовая цена продажи (игрок сможет ее менять)
    public bool CanBeOrdered = true; // Может ли товар быть заказан через компьютер
    
    [Header("Box Info")]
    public int ItemsPerBox = 10; // Количество единиц товара в одной коробке при заказе

    [Header("Placement Info")]
    public PlaceableObjectType ObjectCategory = PlaceableObjectType.None; // Категория размещаемого объекта
    public bool CanBePlacedOnShelf = false; // Может ли этот товар быть размещен на стандартной полке

    // Можно добавить категории, требования к хранению (холодильник и т.д.)
} 