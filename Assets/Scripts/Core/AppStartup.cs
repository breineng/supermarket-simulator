using UnityEngine;
using BehaviourInject;
using System.Collections; // Для IEnumerator

public class AppStartup : MonoBehaviour
{
    private ISceneManagementService _sceneManagementService;

    [Inject]
    public void Construct(ISceneManagementService sceneManagementService)
    {
        _sceneManagementService = sceneManagementService;
    }

    IEnumerator Start()
    {
        // Небольшая задержка, чтобы убедиться, что все контексты, 
        // особенно ApplicationContext, полностью инициализированы.
        // В более сложных сценариях здесь может быть система проверки готовности сервисов.
        yield return null; // Ожидание одного кадра достаточно для Awake/Start циклов

        if (_sceneManagementService == null)
        {
            Debug.LogError("AppStartup: ISceneManagementService not injected!");
            yield break;
        }
        
        Debug.Log("AppStartup: All contexts should be initialized. Loading MenuScene...");
        // Для простоты пока используем синхронную загрузку.
        // Позже можно переключить на LoadSceneAsync, если потребуется экран загрузки.
        _sceneManagementService.LoadScene("MenuScene");
    }
} 