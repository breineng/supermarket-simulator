namespace Services.UI
{
    public interface IInteractionPromptService
    {
        void ShowPrompt(string message);
        void HidePrompt();
        bool IsPromptVisible();
    }
} 