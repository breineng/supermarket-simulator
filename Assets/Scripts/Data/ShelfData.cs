using System.Collections.Generic;

public class ShelfData
{
    public string ShelfID { get; private set; }
    public int Capacity { get; private set; } // Сколько единиц товара может вместить
    public string DisplayedProductID { get; set; } // Какой товар сейчас на полке (ID)
    public int CurrentFillCount { get; set; } // Сколько единиц товара сейчас на полке

    public ShelfData(string id, int capacity)
    {
        ShelfID = id;
        Capacity = capacity;
    }
} 