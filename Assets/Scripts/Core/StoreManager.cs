using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;

public class StoreManager : MonoBehaviour
{
    [Header("Store Points")]
    [SerializeField] private Transform _entryPoint;
    [SerializeField] private Transform _exitPoint;
    
    [Inject]
    public IStorePointsService _storePointsService;
    
    void Start()
    {
        if (_storePointsService == null)
        {
            Debug.LogError("StoreManager: IStorePointsService not injected!");
            return;
        }
        
        // Регистрируем точки в сервисе
        if (_entryPoint != null)
            _storePointsService.SetEntryPoint(_entryPoint);
        
        if (_exitPoint != null)
            _storePointsService.SetExitPoint(_exitPoint);
        
        Debug.Log("StoreManager: Store points configured");
    }
} 