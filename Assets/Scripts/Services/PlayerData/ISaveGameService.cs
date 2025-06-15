using System;
using System.Collections.Generic;

namespace Supermarket.Services.PlayerData
{
    public interface ISaveGameService
    {
        // Основные методы сохранения/загрузки
        bool SaveGame(string saveName);
        bool LoadGame(string saveName);
        bool LoadGameInGameScene();
        bool DeleteSave(string saveName);
        List<SaveGameInfo> GetSavesList();
        
        // Автосохранение
        void EnableAutoSave(float intervalInSeconds);
        void DisableAutoSave();
        
        // События
        event Action<string> OnSaveCompleted;
        event Action<string> OnSaveError;
        event Action<string> OnLoadCompleted;
        event Action<string> OnLoadError;
        
        // Проверки
        bool SaveExists(string saveName);
        DateTime? GetSaveDate(string saveName);
        string GetCurrentSaveVersion();
        bool IsSaveCompatible(string saveName);
    }
    
    // Информация о сохранении
    public class SaveGameInfo
    {
        public string SaveName { get; set; }
        public DateTime SaveDate { get; set; }
        public string Version { get; set; }
        public long FileSize { get; set; }
        public float PlayTime { get; set; } // Время игры в секундах
        public float Money { get; set; } // Для быстрого превью
        public int Day { get; set; } // Игровой день
        public bool IsCompatible { get; set; }
        public string ScreenshotPath { get; set; } // Путь к скриншоту сохранения
    }
} 