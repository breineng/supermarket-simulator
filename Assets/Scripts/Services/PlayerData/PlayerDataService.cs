using UnityEngine;
using System; // For Action

public class PlayerDataService : IPlayerDataService
{
    private const string SAVE_KEY = "Supermarket_PlayerData";
    private PlayerData _currentPlayerData;
    
    // Событие изменения денег
    public event Action OnMoneyChanged;

    public PlayerData CurrentPlayerData => _currentPlayerData;

    public PlayerDataService()
    {
        LoadData(); // Загружаем данные при создании сервиса
    }

    public void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SAVE_KEY);
            _currentPlayerData = JsonUtility.FromJson<PlayerData>(jsonData);
            if (_currentPlayerData == null) // Если десериализация не удалась
            {
                Debug.LogWarning("PlayerDataService: Failed to parse saved data. Resetting to default.");
                ResetDataInternal();
            }
            else
            {
                Debug.Log("PlayerDataService: Player data loaded successfully.");
            }
        }
        else
        {
            Debug.Log("PlayerDataService: No saved data found. Initializing with default data.");
            ResetDataInternal();
        }
    }

    public void SaveData()
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("PlayerDataService: Cannot save null player data.");
            return;
        }
        string jsonData = JsonUtility.ToJson(_currentPlayerData);
        PlayerPrefs.SetString(SAVE_KEY, jsonData);
        PlayerPrefs.Save(); // Убедимся, что данные записаны на диск
        Debug.Log("PlayerDataService: Player data saved.");
    }

    public bool HasSavedData()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }
    
    private void ResetDataInternal()
    {
        _currentPlayerData = new PlayerData();
    }

    public void ResetData()
    {
        ResetDataInternal();
        SaveData(); // Сохраняем сброшенные данные, чтобы перезаписать старые (если были)
        Debug.Log("PlayerDataService: Player data has been reset to default and saved.");
    }

    public void AdjustMoney(float amount)
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("PlayerDataService: Cannot adjust money - player data is null.");
            return;
        }
        
        float oldMoney = _currentPlayerData.Money;
        _currentPlayerData.Money += amount;
        
        // Не позволяем деньгам уйти в минус
        if (_currentPlayerData.Money < 0)
        {
            _currentPlayerData.Money = 0;
            Debug.LogWarning($"PlayerDataService: Money would go negative. Clamped to 0.");
        }
        
        Debug.Log($"PlayerDataService: Money adjusted by {amount:F2}. Old: ${oldMoney:F2}, New: ${_currentPlayerData.Money:F2}");
        
        // Вызываем событие изменения денег
        OnMoneyChanged?.Invoke();
        
        // Автосохранение после изменения денег (опционально)
        // SaveData();
    }
    
    public float GetMoney()
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("PlayerDataService: Cannot get money - player data is null.");
            return 0f;
        }
        return _currentPlayerData.Money;
    }
    
    public void SetMoney(float amount)
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("PlayerDataService: Cannot set money - player data is null.");
            return;
        }
        
        float oldMoney = _currentPlayerData.Money;
        _currentPlayerData.Money = amount;
        
        // Не позволяем деньгам уйти в минус
        if (_currentPlayerData.Money < 0)
        {
            _currentPlayerData.Money = 0;
            Debug.LogWarning($"PlayerDataService: Money set to negative value. Clamped to 0.");
        }
        
        Debug.Log($"PlayerDataService: Money set to ${_currentPlayerData.Money:F2} (was ${oldMoney:F2})");
        
        // Вызываем событие изменения денег
        OnMoneyChanged?.Invoke();
    }
    
    public void SetCharacterAppearance(string gender, int clothingIndex, Color shirtColor, Color pantsColor)
    {
        if (_currentPlayerData == null)
        {
            Debug.LogError("PlayerDataService: Cannot set character appearance - player data is null.");
            return;
        }
        
        _currentPlayerData.CharacterGender = gender;
        _currentPlayerData.ClothingIndex = clothingIndex;
        _currentPlayerData.ShirtColor = shirtColor;
        _currentPlayerData.PantsColor = pantsColor;
        
        Debug.Log($"PlayerDataService: Character appearance updated - Gender: {gender}, Clothing: {clothingIndex}");
    }
} 