using System;
using UnityEngine;

namespace Supermarket.Services.Game
{
    public class SupermarketNameService : ISupermarketNameService
    {
        private string _currentName = "СУПЕРМАРКЕТ";
        
        public string CurrentName => _currentName;
        
        public event Action<string> OnNameChanged;
        
        public void SetName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                Debug.LogWarning("SupermarketNameService: Attempted to set empty or null name");
                return;
            }
            
            if (_currentName != newName)
            {
                _currentName = newName;
                Debug.Log($"SupermarketNameService: Name changed to '{_currentName}'");
                OnNameChanged?.Invoke(_currentName);
            }
        }
        
        public void LoadName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _currentName = name.ToUpper();
                Debug.Log($"SupermarketNameService: Name loaded from save: '{_currentName}'");
                // Не вызываем OnNameChanged при загрузке, так как это не изменение пользователем
            }
            else
            {
                Debug.LogWarning("SupermarketNameService: Loaded name is empty, using default");
            }
        }
    }
} 