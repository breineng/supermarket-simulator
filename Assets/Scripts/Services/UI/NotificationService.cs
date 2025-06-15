using UnityEngine;
using BehaviourInject;

namespace Supermarket.Services.UI
{
    public class NotificationService : MonoBehaviour, INotificationService
    {
        [SerializeField] private GameUIHandler _gameUIHandler;
        
        void Awake()
        {
            if (_gameUIHandler == null)
            {
                Debug.LogError("NotificationService: GameUIHandler not assigned in inspector!");
            }
        }
        
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            if (_gameUIHandler == null)
            {
                Debug.LogWarning($"NotificationService: Cannot show notification - GameUIHandler is null. Message: {message}");
                return;
            }
            
            // Добавляем префикс в зависимости от типа
            string formattedMessage = type switch
            {
                NotificationType.Success => $"✓ {message}",
                NotificationType.Warning => $"⚠ {message}",
                NotificationType.Error => $"✕ {message}",
                _ => message
            };
            
            _gameUIHandler.ShowNotification(formattedMessage, duration);
            
            // Логируем важные уведомления
            switch (type)
            {
                case NotificationType.Warning:
                    Debug.LogWarning($"Notification: {message}");
                    break;
                case NotificationType.Error:
                    Debug.LogError($"Notification: {message}");
                    break;
                default:
                    Debug.Log($"Notification: {message}");
                    break;
            }
        }
    }
} 