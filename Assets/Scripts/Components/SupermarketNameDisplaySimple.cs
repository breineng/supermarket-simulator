using System.Collections.Generic;
using UnityEngine;
using BehaviourInject;
using Supermarket.Services.Game;
using Supermarket.Data;

namespace Supermarket.Components
{
    /// <summary>
    /// Упрощенный компонент для отображения названия супермаркета 3D буквами
    /// Использует LetterMappingData ScriptableObject для маппинга символов
    /// </summary>
    public class SupermarketNameDisplaySimple : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _letterSpacing = 1.5f;
        [SerializeField] private float _letterScale = 1.0f;
        [SerializeField] private bool _centerAlignment = true;
        [SerializeField] private bool _rotateLetters180 = true;
        
        [Header("Position")]
        [SerializeField] private Transform _lettersParent;
        [SerializeField] private Vector3 _startPosition = Vector3.zero;
        
        [Header("Letter Mapping")]
        [SerializeField] private Supermarket.Data.LetterMappingData _letterMappingData;
        
        [Inject] public ISupermarketNameService SupermarketNameService { get; set; }
        
        private readonly List<GameObject> _currentLetters = new List<GameObject>();
        
        private void Start()
        {
            // Если не указан родительский объект, используем себя
            if (_lettersParent == null)
                _lettersParent = transform;
                
            // Проверяем наличие данных маппинга
            if (_letterMappingData == null)
            {
                Debug.LogError("SupermarketNameDisplaySimple: LetterMappingData is not assigned!");
                return;
            }
                
            // Подписываемся на изменения названия
            if (SupermarketNameService != null)
            {
                SupermarketNameService.OnNameChanged += OnNameChanged;
                
                // Отображаем текущее название
                DisplayName(SupermarketNameService.CurrentName);
            }
            else
            {
                Debug.LogWarning("SupermarketNameDisplaySimple: SupermarketNameService is null, displaying default name");
                DisplayName("СУПЕРМАРКЕТ");
            }
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
            
            // Вычисляем общую ширину названия для центрирования
            float totalWidth = CalculateNameWidth(name);
            float startX = _centerAlignment ? _startPosition.x - (totalWidth / 2f) : _startPosition.x;
            float currentX = startX;
            
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
                    
                    // Устанавливаем поворот (180 градусов по Z, если включено)
                    if (_rotateLetters180)
                    {
                        letterInstance.transform.localRotation = Quaternion.Euler(0, 0, 180);
                    }
                    
                    // Устанавливаем масштаб
                    letterInstance.transform.localScale = Vector3.one * _letterScale;
                    
                    // Добавляем в список для последующего удаления
                    _currentLetters.Add(letterInstance);
                    
                    // Сдвигаем позицию для следующей буквы
                    currentX += _letterSpacing;
                }
                else
                {
                    Debug.LogWarning($"SupermarketNameDisplaySimple: Could not find prefab for character '{character}'");
                    // Все равно сдвигаем позицию, чтобы не накладывались буквы
                    currentX += _letterSpacing;
                }
            }
            
            Debug.Log($"SupermarketNameDisplaySimple: Displayed name '{name}' with {_currentLetters.Count} letters" + 
                     (_centerAlignment ? $", centered (width: {totalWidth:F1})" : ", left-aligned"));
        }
        
        private GameObject GetLetterPrefab(char character)
        {
            if (_letterMappingData == null)
                return null;
                
            return _letterMappingData.GetPrefabForCharacter(character);
        }
        
        private float CalculateNameWidth(string name)
        {
            if (string.IsNullOrEmpty(name))
                return 0f;
            
            // Подсчитываем количество видимых символов (не пробелов)
            int visibleCharacters = 0;
            int totalCharacters = name.Length;
            
            foreach (char character in name)
            {
                if (character != ' ')
                {
                    visibleCharacters++;
                }
            }
            
            // Общая ширина = (количество всех символов - 1) * расстояние между буквами
            // Минус 1, потому что после последней буквы нет расстояния
            return (totalCharacters - 1) * _letterSpacing;
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
        public void SetSettings(float letterSpacing, float letterScale, Vector3 startPosition, 
                                bool centerAlignment = true, bool rotateLetters180 = true)
        {
            _letterSpacing = letterSpacing;
            _letterScale = letterScale;
            _startPosition = startPosition;
            _centerAlignment = centerAlignment;
            _rotateLetters180 = rotateLetters180;
            
            // Обновляем отображение с новыми настройками
            if (SupermarketNameService != null)
                DisplayName(SupermarketNameService.CurrentName);
        }
        
        // Метод для принудительного обновления отображения
        [ContextMenu("Refresh Display")]
        public void RefreshDisplay()
        {
            if (SupermarketNameService != null)
                DisplayName(SupermarketNameService.CurrentName);
            else
                DisplayName("СУПЕРМАРКЕТ");
        }
    }
} 