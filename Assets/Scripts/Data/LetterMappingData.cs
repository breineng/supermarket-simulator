using UnityEngine;
using System.Collections.Generic;

namespace Supermarket.Data
{
    /// <summary>
    /// ScriptableObject для хранения маппинга символов на префабы букв
    /// Позволяет создавать переиспользуемые наборы букв для разных языков и стилей
    /// </summary>
    [CreateAssetMenu(fileName = "LetterMappingData", menuName = "Supermarket/Letter Mapping Data")]
    public class LetterMappingData : ScriptableObject
    {
        [Header("Letter Mappings")]
        [SerializeField] private LetterPrefabMapping[] _letterMappings;
        
        [Header("Settings")]
        [SerializeField] private string _description = "Letter mapping for alphabet";
        [SerializeField] private bool _caseSensitive = false;
        
        private Dictionary<char, GameObject> _cachedMappings;
        
        [System.Serializable]
        public class LetterPrefabMapping
        {
            [Tooltip("Символ для маппинга")]
            public char character;
            
            [Tooltip("Префаб для этого символа")]
            public GameObject prefab;
        }
        
        /// <summary>
        /// Получить префаб для указанного символа
        /// </summary>
        /// <param name="character">Символ</param>
        /// <returns>Префаб или null если не найден</returns>
        public GameObject GetPrefabForCharacter(char character)
        {
            if (_cachedMappings == null)
                BuildCache();
                
            // Сначала пробуем точное совпадение
            if (_cachedMappings.ContainsKey(character))
                return _cachedMappings[character];
            
            // Если не чувствительны к регистру, пробуем разные варианты
            if (!_caseSensitive)
            {
                char upperChar = char.ToUpper(character);
                if (_cachedMappings.ContainsKey(upperChar))
                    return _cachedMappings[upperChar];
                    
                char lowerChar = char.ToLower(character);
                if (_cachedMappings.ContainsKey(lowerChar))
                    return _cachedMappings[lowerChar];
            }
            
            return null;
        }
        
        /// <summary>
        /// Проверить, есть ли маппинг для символа
        /// </summary>
        /// <param name="character">Символ</param>
        /// <returns>True если маппинг существует</returns>
        public bool HasMappingForCharacter(char character)
        {
            return GetPrefabForCharacter(character) != null;
        }
        
        /// <summary>
        /// Получить все доступные символы
        /// </summary>
        /// <returns>Массив символов</returns>
        public char[] GetAvailableCharacters()
        {
            if (_cachedMappings == null)
                BuildCache();
                
            char[] characters = new char[_cachedMappings.Count];
            _cachedMappings.Keys.CopyTo(characters, 0);
            return characters;
        }
        
        /// <summary>
        /// Получить количество маппингов
        /// </summary>
        /// <returns>Количество маппингов</returns>
        public int GetMappingCount()
        {
            return _letterMappings?.Length ?? 0;
        }
        
        /// <summary>
        /// Описание набора букв
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// Чувствительность к регистру
        /// </summary>
        public bool CaseSensitive => _caseSensitive;
        
        /// <summary>
        /// Построить кэш маппингов для быстрого доступа
        /// </summary>
        private void BuildCache()
        {
            _cachedMappings = new Dictionary<char, GameObject>();
            
            if (_letterMappings == null)
                return;
                
            foreach (var mapping in _letterMappings)
            {
                if (mapping.prefab != null && !_cachedMappings.ContainsKey(mapping.character))
                {
                    _cachedMappings[mapping.character] = mapping.prefab;
                }
            }
            
            Debug.Log($"LetterMappingData ({name}): Built cache with {_cachedMappings.Count} mappings");
        }
        
        /// <summary>
        /// Очистить кэш (вызывается при изменении данных)
        /// </summary>
        private void OnValidate()
        {
            _cachedMappings = null;
        }
        
        /// <summary>
        /// Валидация маппингов
        /// </summary>
        [ContextMenu("Validate Mappings")]
        public void ValidateMappings()
        {
            if (_letterMappings == null)
            {
                Debug.LogWarning($"LetterMappingData ({name}): No mappings defined");
                return;
            }
            
            int validMappings = 0;
            int duplicates = 0;
            HashSet<char> seenCharacters = new HashSet<char>();
            
            foreach (var mapping in _letterMappings)
            {
                if (mapping.prefab == null)
                {
                    Debug.LogWarning($"LetterMappingData ({name}): Character '{mapping.character}' has no prefab assigned");
                    continue;
                }
                
                if (seenCharacters.Contains(mapping.character))
                {
                    Debug.LogWarning($"LetterMappingData ({name}): Duplicate character '{mapping.character}'");
                    duplicates++;
                    continue;
                }
                
                seenCharacters.Add(mapping.character);
                validMappings++;
            }
            
            Debug.Log($"LetterMappingData ({name}): {validMappings} valid mappings, {duplicates} duplicates found");
        }
    }
} 