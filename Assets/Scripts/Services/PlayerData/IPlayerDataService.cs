using System; // For Action
using UnityEngine; // For Color

public interface IPlayerDataService
{
    PlayerData CurrentPlayerData { get; }
    void SaveData();
    void LoadData();
    bool HasSavedData();
    void ResetData(); // Для новой игры или сброса прогресса
    void AdjustMoney(float amount); // Добавить/убавить деньги (положительное - добавить, отрицательное - убавить)
    float GetMoney(); // Получить текущее количество денег
    void SetMoney(float amount); // Установить точное количество денег
    void SetCharacterAppearance(string gender, int clothingIndex, Color shirtColor, Color pantsColor); // Установить внешний вид персонажа
    
    // События
    event Action OnMoneyChanged; // Событие при изменении денег
} 