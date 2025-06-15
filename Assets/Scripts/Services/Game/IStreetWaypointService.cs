using UnityEngine;
using System.Collections.Generic;

namespace Supermarket.Services.Game
{
    public interface IStreetWaypointService
    {
        /// <summary>
        /// Получить случайную точку waypoint для начала движения
        /// </summary>
        Transform GetRandomWaypoint();
        
        /// <summary>
        /// Получить следующую точку waypoint относительно текущей позиции
        /// </summary>
        Transform GetNextWaypoint(Vector3 currentPosition, Transform currentWaypoint = null);
        
        /// <summary>
        /// Получить все доступные waypoints
        /// </summary>
        List<Transform> GetAllWaypoints();
        
        /// <summary>
        /// Проверить, есть ли доступные waypoints
        /// </summary>
        bool HasWaypoints();
        
        /// <summary>
        /// Получить точку входа в магазин (переход от street walking к магазину)
        /// </summary>
        Transform GetStoreEntrancePoint();
    }
} 