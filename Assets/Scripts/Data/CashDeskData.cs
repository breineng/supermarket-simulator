using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CashDeskData
{
    public string CashDeskID;
    public bool IsOpen = true;
    public bool IsOccupied = false;
    
    // Очередь покупателей
    public Queue<GameObject> CustomerQueue;
    
    // Текущий обслуживаемый покупатель
    public GameObject CurrentCustomer;
    
    // Статистика
    public int CustomersServed = 0;
    public float TotalRevenue = 0f;
    
    public CashDeskData(string id)
    {
        CashDeskID = id;
        CustomerQueue = new Queue<GameObject>();
    }
    
    public int GetQueueLength()
    {
        return CustomerQueue != null ? CustomerQueue.Count : 0;
    }
} 