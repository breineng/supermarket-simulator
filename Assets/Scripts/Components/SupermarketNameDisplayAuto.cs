using System.Collections.Generic;
using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;

namespace Supermarket.Components
{
    /// <summary>
    /// Компонент для отображения названия супермаркета 3D буквами с автоматическим поиском префабов
    /// </summary>
    public class SupermarketNameDisplayAuto : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _letterSpacing = 1.5f;
        [SerializeField] private float _letterScale = 1.0f;
        
        [Header("Position")]
        [SerializeField] private Transform _lettersParent;
        [SerializeField] private Vector3 _startPosition = Vector3.zero;
        
        [Inject] public ISupermarketNameService SupermarketNameService { get; set; }
        
        private readonly List<GameObject> _currentLetters = new List<GameObject>();
        private Dictionary<char, GameObject> _letterPrefabsCache = new Dictionary<char, GameObject>();
        
        private void Start()
        {
            // Если не указан родительский объект, используем себя
            if (_lettersParent == null)
                _lettersParent = transform;
                
            // Кэшируем префабы букв
            CacheLetterPrefabs();
                
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
        
        private void CacheLetterPrefabs()
        {
            // Загружаем все префабы из папки Assets/Prefabs/Alphabet
            GameObject[] allPrefabs = FindAllLetterPrefabs();
            
            foreach (GameObject prefab in allPrefabs)
            {
                char? character = GetCharacterFromPrefabName(prefab.name);
                if (character.HasValue && !_letterPrefabsCache.ContainsKey(character.Value))
                {
                    _letterPrefabsCache[character.Value] = prefab;
                    Debug.Log($"SupermarketNameDisplayAuto: Cached prefab '{prefab.name}' for character '{character.Value}'");
                }
            }
            
            Debug.Log($"SupermarketNameDisplayAuto: Cached {_letterPrefabsCache.Count} letter prefabs");
        }
        
        private GameObject[] FindAllLetterPrefabs()
        {
            List<GameObject> prefabs = new List<GameObject>();
            
            // Используем Resources.LoadAll для загрузки всех префабов из папки Resources
            // Но сначала переместим префабы в Resources или используем другой подход
            
            // Альтернативный подход - найти все префабы в проекте
            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Alphabet" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }
            #endif
            
            return prefabs.ToArray();
        }
        
        private char? GetCharacterFromPrefabName(string prefabName)
        {
            // Пытаемся извлечь символ из имени префаба
            // Поддерживаем форматы: "Letter_А 1", "Letter_а", "А", и т.д.
            
            if (prefabName.StartsWith("Letter_") && prefabName.Length >= 8)
            {
                // Формат "Letter_А" или "Letter_А 1"
                char character = prefabName[7]; // Символ после "Letter_"
                return character;
            }
            
            if (prefabName.Length == 1)
            {
                // Формат "А"
                return prefabName[0];
            }
            
            return null;
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
                
                GameObject letterPrefab = GetLetterPrefab(character);
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
                    Debug.LogWarning($"SupermarketNameDisplayAuto: Could not find prefab for character '{character}'");
                    // Все равно сдвигаем позицию, чтобы не накладывались буквы
                    currentX += _letterSpacing;
                }
            }
            
            Debug.Log($"SupermarketNameDisplayAuto: Displayed name '{name}' with {_currentLetters.Count} letters");
        }
        
        private GameObject GetLetterPrefab(char character)
        {
            // Сначала пробуем точное совпадение
            if (_letterPrefabsCache.ContainsKey(character))
            {
                return _letterPrefabsCache[character];
            }
            
            // Пробуем совпадение по верхнему регистру
            char upperChar = char.ToUpper(character);
            if (_letterPrefabsCache.ContainsKey(upperChar))
            {
                return _letterPrefabsCache[upperChar];
            }
            
            // Пробуем совпадение по нижнему регистру
            char lowerChar = char.ToLower(character);
            if (_letterPrefabsCache.ContainsKey(lowerChar))
            {
                return _letterPrefabsCache[lowerChar];
            }
            
            return null;
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
        
        // Метод для принудительного обновления кэша префабов
        [ContextMenu("Refresh Letter Prefabs Cache")]
        public void RefreshPrefabsCache()
        {
            _letterPrefabsCache.Clear();
            CacheLetterPrefabs();
            
            // Обновляем отображение
            if (SupermarketNameService != null)
                DisplayName(SupermarketNameService.CurrentName);
        }
    }
} 