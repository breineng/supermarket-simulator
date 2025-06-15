using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class SceneManagementService : ISceneManagementService
{
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneManagementService: Scene name cannot be null or empty.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public async Task LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneManagementService: Scene name cannot be null or empty for async load.");
            return;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Можно добавить логику ожидания или проверки состояния, если нужно
        while (!asyncLoad.isDone)
        {
            // Здесь можно обновлять прогресс загрузки, если есть UI для этого
            // Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");
            await Task.Yield(); // Даем возможность другим процессам выполняться
        }
        Debug.Log($"SceneManagementService: Scene '{sceneName}' loaded asynchronously.");
    }
} 