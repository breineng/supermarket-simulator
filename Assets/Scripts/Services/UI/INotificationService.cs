namespace Supermarket.Services.UI
{
    public interface INotificationService
    {
        void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f);
    }
    
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
} 