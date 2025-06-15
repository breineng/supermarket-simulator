using UnityEngine;

[System.Serializable]
public class BoxData
{
    public ProductConfig ProductInBox;
    public int Quantity;
    // Можно добавить ID коробки, статус (открыта/закрыта) и т.д. в будущем

    public BoxData(ProductConfig product, int quantity)
    {
        ProductInBox = product;
        Quantity = quantity;
    }
} 