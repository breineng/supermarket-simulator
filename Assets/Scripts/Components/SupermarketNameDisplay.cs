using System.Collections.Generic;
using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;

namespace Supermarket.Components
{
    /// <summary>
    /// Компонент для отображения названия супермаркета 3D буквами
    /// </summary>
    public class SupermarketNameDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _letterSpacing = 1.5f;
        [SerializeField] private float _letterScale = 1.0f;
        
        [Header("Position")]
        [SerializeField] private Transform _lettersParent;
        [SerializeField] private Vector3 _startPosition = Vector3.zero;
        
        [Header("Letter Prefabs")]
        [SerializeField] private GameObject[] _letterPrefabs;
        
        [Inject] public ISupermarketNameService SupermarketNameService { get; set; }
        
        private readonly List<GameObject> _currentLetters = new List<GameObject>();
        
        private void Start()
        {
            // Если не указан родительский объект, используем себя
            if (_lettersParent == null)
                _lettersParent = transform;
                
            // Подписываемся на изменения названия
            SupermarketNameService.OnNameChanged += OnNameChanged;
            
            // Отображаем текущее название
            DisplayName(SupermarketNameService.CurrentName);
        }
        
        private void OnDestroy()
        {
            if (SupermarketNameService != null)
                SupermarketNameService.OnNameChanged -= OnNameChanged;
        }
        
        private void OnNameChanged(string newName)
        {
            DisplayName(newName);
        }
        
        private void DisplayName(string name)
        {
            // Удаляем существующие буквы
            ClearCurrentLetters();
            
            if (string.IsNullOrEmpty(name))
                return;
                
            float currentX = _startPosition.x;
            
            foreach (char character in name)
            {
                if (character == ' ')
                {
                    // Для пробела просто сдвигаем позицию
                    currentX += _letterSpacing;
                    continue;
                }
                
                GameObject letterPrefab = LoadLetterPrefab(character);
                if (letterPrefab != null)
                {
                    // Создаем экземпляр буквы
                    GameObject letterInstance = Instantiate(letterPrefab, _lettersParent);
                    
                    // Устанавливаем позицию
                    Vector3 letterPosition = new Vector3(currentX, _startPosition.y, _startPosition.z);
                    letterInstance.transform.localPosition = letterPosition;
                    
                    // Устанавливаем масштаб
                    letterInstance.transform.localScale = Vector3.one * _letterScale;
                    
                    // Добавляем в список для последующего удаления
                    _currentLetters.Add(letterInstance);
                    
                    // Сдвигаем позицию для следующей буквы
                    currentX += _letterSpacing;
                }
                else
                {
                    Debug.LogWarning($"SupermarketNameDisplay: Could not find prefab for character '{character}'");
                    // Все равно сдвигаем позицию, чтобы не накладывались буквы
                    currentX += _letterSpacing;
                }
            }
            
            Debug.Log($"SupermarketNameDisplay: Displayed name '{name}' with {_currentLetters.Count} letters");
        }
        
        private GameObject LoadLetterPrefab(char character)
        {
            if (_letterPrefabs == null || _letterPrefabs.Length == 0)
            {
                Debug.LogError("SupermarketNameDisplay: Letter prefabs array is not assigned!");
                return null;
            }
            
            // Ищем префаб по имени символа
            string targetCharacterName = GetCharacterName(character);
            
            foreach (GameObject prefab in _letterPrefabs)
            {
                if (prefab != null && DoesNameMatch(prefab.name, targetCharacterName))
                {
                    return prefab;
                }
            }
            
            return null;
        }
        
        private string GetCharacterName(char character)
        {
            return character.ToString();
        }
        
        private bool DoesNameMatch(string prefabName, string targetCharacter)
        {
            // Поддерживаем различные форматы имен префабов
            char upperChar = char.ToUpper(targetCharacter[0]);
            char lowerChar = char.ToLower(targetCharacter[0]);
            
            // Список возможных паттернов для сравнения
            string[] patterns = {
                $"Letter_{upperChar} 1",  // Letter_А 1
                $"Letter_{lowerChar} 1",  // Letter_а 1
                $"Letter_{upperChar}",    // Letter_А
                $"Letter_{lowerChar}",    // Letter_а
                upperChar.ToString(),     // А
                lowerChar.ToString()      // а
            };
            
            foreach (string pattern in patterns)
            {
                if (prefabName.Equals(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void ClearCurrentLetters()
        {
            foreach (GameObject letter in _currentLetters)
            {
                if (letter != null)
                    DestroyImmediate(letter);
            }
            _currentLetters.Clear();
        }
        
        // Метод для установки настроек из инспектора или кода
        public void SetSettings(float letterSpacing, float letterScale, Vector3 startPosition)
        {
            _letterSpacing = letterSpacing;
            _letterScale = letterScale;
            _startPosition = startPosition;
            
            // Обновляем отображение с новыми настройками
            if (SupermarketNameService != null)
                DisplayName(SupermarketNameService.CurrentName);
        }
    }
} 