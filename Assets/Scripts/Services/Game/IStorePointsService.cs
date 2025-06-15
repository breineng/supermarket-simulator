using UnityEngine;

namespace Supermarket.Services.Game
{
    public interface IStorePointsService
    {
        Transform EntryPoint { get; }
        Transform ExitPoint { get; }
        
        void SetEntryPoint(Transform point);
        void SetExitPoint(Transform point);
    }
} 