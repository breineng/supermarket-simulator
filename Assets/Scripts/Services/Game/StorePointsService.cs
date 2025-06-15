using UnityEngine;

namespace Supermarket.Services.Game
{
    public class StorePointsService : IStorePointsService
    {
        private Transform _entryPoint;
        private Transform _exitPoint;
        
        public Transform EntryPoint => _entryPoint;
        public Transform ExitPoint => _exitPoint;
        
        public void SetEntryPoint(Transform point)
        {
            _entryPoint = point;
            Debug.Log($"StorePointsService: Entry point set to {point?.name ?? "null"}");
        }
        
        public void SetExitPoint(Transform point)
        {
            _exitPoint = point;
            Debug.Log($"StorePointsService: Exit point set to {point?.name ?? "null"}");
        }
    }
} 