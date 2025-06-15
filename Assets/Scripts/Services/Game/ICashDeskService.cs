using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Services.Game
{
    public interface ICashDeskService
    {
        void RegisterCashDesk(GameObject cashDesk);
        void UnregisterCashDesk(GameObject cashDesk);
        List<GameObject> GetAllCashDesks();
        GameObject FindCashDeskWithShortestQueue();
        GameObject FindCashDeskById(string cashDeskId);
        
        /// <summary>
        /// Ищет клиентов без кассы и направляет их к новой кассе (если это первая касса на сцене)
        /// </summary>
        void ReassignLostCustomersToNewCashDesk(GameObject newCashDesk);
    }
} 