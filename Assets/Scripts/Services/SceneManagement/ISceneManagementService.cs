using System.Threading.Tasks;

public interface ISceneManagementService
{
    void LoadScene(string sceneName);
    Task LoadSceneAsync(string sceneName);
} 