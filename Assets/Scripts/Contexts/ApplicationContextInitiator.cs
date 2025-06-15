using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game; // For IStatsService
using Supermarket.Services.PlayerData; // For ISaveGameService
using Supermarket.Services.UI; // For IUINavigationService

public class ApplicationContextInitiator : MonoBehaviour
{
    void Awake()
    {
        // Создаем глобальный Application контекст
        Context applicationContext = Context.Create("Application");

        // Регистрируем сервисы
        applicationContext.RegisterDependencyAs<SceneManagementService, ISceneManagementService>(new SceneManagementService());
        applicationContext.RegisterDependencyAs<GameConfigService, IGameConfigService>(new GameConfigService());
        applicationContext.RegisterDependencyAs<PlayerDataService, IPlayerDataService>(new PlayerDataService());
        applicationContext.RegisterDependencyAs<StatsService, IStatsService>(new StatsService());
        
        // Регистрируем новую UI Navigation систему
        applicationContext.RegisterTypeAs<UINavigationService, IUINavigationService>();
        
        // Для сервисов, которые BInject должен создать сам
        applicationContext.RegisterTypeAs<InputModeService, IInputModeService>();
        
        // Этот объект не должен уничтожаться при загрузке новых сцен
        DontDestroyOnLoad(gameObject);

        Debug.Log("ApplicationContextInitiator: Application context created and services registered.");
    }
} 