using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;

public enum StorePointType
{
    Entry,
    Exit
}

public class StorePointsRegistrar : MonoBehaviour
{
    [SerializeField] private StorePointType _pointType = StorePointType.Exit;
    
    [Inject]
    public IStorePointsService _storePointsService;
    
    void Start()
    {
        if (_storePointsService == null)
        {
            Debug.LogError($"StorePointsRegistrar: IStorePointsService not injected on {gameObject.name}!");
            return;
        }
        
        switch (_pointType)
        {
            case StorePointType.Entry:
                _storePointsService.SetEntryPoint(transform);
                break;
            case StorePointType.Exit:
                _storePointsService.SetExitPoint(transform);
                break;
        }
    }
} 