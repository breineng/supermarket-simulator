using UnityEngine;
using UnityEditor;
using Supermarket.Data;

namespace Supermarket.Editor
{
    /// <summary>
    /// Кастомный редактор для LetterMappingData с дополнительными возможностями
    /// </summary>
    [CustomEditor(typeof(LetterMappingData))]
    public class LetterMappingDataEditor : UnityEditor.Editor
    {
        private LetterMappingData _target;
        private string _searchFilter = "";
        private bool _showOnlyEmpty = false;
        
        private void OnEnable()
        {
            _target = (LetterMappingData)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Основные настройки
            EditorGUILayout.LabelField("Letter Mapping Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_caseSensitive"));
            
            EditorGUILayout.Space(10);
            
            // Заголовок для маппингов с колонками
            EditorGUILayout.LabelField("Letter Mappings", EditorStyles.boldLabel);
            
            // Показываем заголовки колонок
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15); // Отступ для кнопки складывания
            EditorGUILayout.LabelField("Char", GUILayout.Width(25));
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            // Рисуем разделительную линию
            Rect lineRect = GUILayoutUtility.GetRect(0, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(5);
            
            // Фильтры
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filters:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField("Search:", _searchFilter);
            _showOnlyEmpty = EditorGUILayout.ToggleLeft("Only Empty", _showOnlyEmpty, GUILayout.Width(80));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _searchFilter = "";
                _showOnlyEmpty = false;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Массив маппингов с фильтрацией
            SerializedProperty mappingsProperty = serializedObject.FindProperty("_letterMappings");
            
            if (string.IsNullOrEmpty(_searchFilter) && !_showOnlyEmpty)
            {
                // Показываем все элементы без фильтрации
                EditorGUILayout.PropertyField(mappingsProperty, true);
            }
            else
            {
                // Показываем отфильтрованные элементы
                DrawFilteredMappings(mappingsProperty);
            }
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space(10);
            
            // Дополнительные кнопки
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Validate Mappings"))
            {
                _target.ValidateMappings();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Cyrillic Template"))
            {
                GenerateCyrillicTemplate();
            }
            if (GUILayout.Button("Cyrillic (Upper Only)"))
            {
                GenerateCyrillicTemplate(false);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Latin Template"))
            {
                GenerateLatinTemplate();
            }
            if (GUILayout.Button("Latin (Upper Only)"))
            {
                GenerateLatinTemplate(false);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Numbers & Symbols"))
            {
                GenerateNumbersAndSymbolsTemplate();
            }
            if (GUILayout.Button("Full Mixed Template"))
            {
                GenerateFullMixedTemplate();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Информация
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Mappings Count: {_target.GetMappingCount()}");
            EditorGUILayout.LabelField($"Case Sensitive: {_target.CaseSensitive}");
            EditorGUILayout.LabelField($"Description: {_target.Description}");
            
            if (_target.GetMappingCount() > 0)
            {
                EditorGUILayout.Space(5);
                var availableChars = _target.GetAvailableCharacters();
                string charsList = string.Join(", ", availableChars);
                if (charsList.Length > 100)
                    charsList = charsList.Substring(0, 100) + "...";
                EditorGUILayout.LabelField($"Available: {charsList}");
            }
        }
        
        private void GenerateCyrillicTemplate(bool includeBothCases = true)
        {
            string dialogTitle = includeBothCases ? "Generate Cyrillic Template (Both Cases)" : "Generate Cyrillic Template (Upper Only)";
            string dialogMessage = includeBothCases ? 
                "This will clear existing mappings and create a template for Cyrillic alphabet (both upper and lower case). Continue?" :
                "This will clear existing mappings and create a template for Cyrillic alphabet (upper case only). Continue?";
                
            if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
            {
                Undo.RecordObject(_target, "Generate Cyrillic Template");
                
                string cyrillicUpper = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
                string cyrillicLower = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
                
                string allChars = includeBothCases ? cyrillicUpper + cyrillicLower : cyrillicUpper;
                
                SerializedObject so = new SerializedObject(_target);
                
                // Очищаем существующие маппинги
                SerializedProperty mappingsProperty = so.FindProperty("_letterMappings");
                mappingsProperty.ClearArray();
                
                // Добавляем шаблон кириллицы
                for (int i = 0; i < allChars.Length; i++)
                {
                    mappingsProperty.InsertArrayElementAtIndex(i);
                    SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                    
                    SerializedProperty charProperty = element.FindPropertyRelative("character");
                    SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
                    
                    charProperty.intValue = allChars[i];
                    prefabProperty.objectReferenceValue = null; // Пользователь должен назначить вручную
                }
                
                // Обновляем описание
                SerializedProperty descProperty = so.FindProperty("_description");
                string description = includeBothCases ? "Cyrillic alphabet mappings (both cases)" : "Cyrillic alphabet mappings (upper case)";
                descProperty.stringValue = description;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
                
                Debug.Log($"Generated Cyrillic template with {allChars.Length} characters");
            }
        }
        
        private void GenerateLatinTemplate(bool includeBothCases = true)
        {
            string dialogTitle = includeBothCases ? "Generate Latin Template (Both Cases)" : "Generate Latin Template (Upper Only)";
            string dialogMessage = includeBothCases ? 
                "This will clear existing mappings and create a template for Latin alphabet (both upper and lower case). Continue?" :
                "This will clear existing mappings and create a template for Latin alphabet (upper case only). Continue?";
                
            if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
            {
                Undo.RecordObject(_target, "Generate Latin Template");
                
                string latinUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                string latinLower = "abcdefghijklmnopqrstuvwxyz";
                
                string allChars = includeBothCases ? latinUpper + latinLower : latinUpper;
                
                SerializedObject so = new SerializedObject(_target);
                
                // Очищаем существующие маппинги
                SerializedProperty mappingsProperty = so.FindProperty("_letterMappings");
                mappingsProperty.ClearArray();
                
                // Добавляем шаблон латиницы
                for (int i = 0; i < allChars.Length; i++)
                {
                    mappingsProperty.InsertArrayElementAtIndex(i);
                    SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                    
                    SerializedProperty charProperty = element.FindPropertyRelative("character");
                    SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
                    
                    charProperty.intValue = allChars[i];
                    prefabProperty.objectReferenceValue = null; // Пользователь должен назначить вручную
                }
                
                // Обновляем описание
                SerializedProperty descProperty = so.FindProperty("_description");
                string description = includeBothCases ? "Latin alphabet mappings (both cases)" : "Latin alphabet mappings (upper case)";
                descProperty.stringValue = description;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
                
                Debug.Log($"Generated Latin template with {allChars.Length} characters");
            }
        }
        
        private void GenerateNumbersAndSymbolsTemplate()
        {
            if (EditorUtility.DisplayDialog(
                "Generate Numbers & Symbols Template", 
                "This will clear existing mappings and create a template for numbers and common symbols. Continue?",
                "Yes", "Cancel"))
            {
                Undo.RecordObject(_target, "Generate Numbers & Symbols Template");
                
                string numbers = "0123456789";
                string symbols = "!@#$%^&*()-_=+[]{}|;':\",./<>?~ ";
                
                string allChars = numbers + symbols;
                
                SerializedObject so = new SerializedObject(_target);
                
                // Очищаем существующие маппинги
                SerializedProperty mappingsProperty = so.FindProperty("_letterMappings");
                mappingsProperty.ClearArray();
                
                // Добавляем числа и символы
                for (int i = 0; i < allChars.Length; i++)
                {
                    mappingsProperty.InsertArrayElementAtIndex(i);
                    SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                    
                    SerializedProperty charProperty = element.FindPropertyRelative("character");
                    SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
                    
                    charProperty.intValue = allChars[i];
                    prefabProperty.objectReferenceValue = null;
                }
                
                // Обновляем описание
                SerializedProperty descProperty = so.FindProperty("_description");
                descProperty.stringValue = "Numbers and symbols mappings";
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
                
                Debug.Log($"Generated Numbers & Symbols template with {allChars.Length} characters");
            }
        }
        
        private void GenerateFullMixedTemplate()
        {
            if (EditorUtility.DisplayDialog(
                "Generate Full Mixed Template", 
                "This will clear existing mappings and create a comprehensive template with Cyrillic, Latin, numbers and symbols. Continue?",
                "Yes", "Cancel"))
            {
                Undo.RecordObject(_target, "Generate Full Mixed Template");
                
                string cyrillicUpper = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
                string cyrillicLower = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
                string latinUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                string latinLower = "abcdefghijklmnopqrstuvwxyz";
                string numbers = "0123456789";
                string commonSymbols = "!@#$%^&*()-_=+.,<>?:; ";
                
                string allChars = cyrillicUpper + cyrillicLower + latinUpper + latinLower + numbers + commonSymbols;
                
                SerializedObject so = new SerializedObject(_target);
                
                // Очищаем существующие маппинги
                SerializedProperty mappingsProperty = so.FindProperty("_letterMappings");
                mappingsProperty.ClearArray();
                
                // Добавляем все символы
                for (int i = 0; i < allChars.Length; i++)
                {
                    mappingsProperty.InsertArrayElementAtIndex(i);
                    SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                    
                    SerializedProperty charProperty = element.FindPropertyRelative("character");
                    SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
                    
                    charProperty.intValue = allChars[i];
                    prefabProperty.objectReferenceValue = null;
                }
                
                // Обновляем описание
                SerializedProperty descProperty = so.FindProperty("_description");
                descProperty.stringValue = "Full mixed template (Cyrillic, Latin, numbers, symbols)";
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
                
                Debug.Log($"Generated Full Mixed template with {allChars.Length} characters");
            }
        }
        
        private void DrawFilteredMappings(SerializedProperty mappingsProperty)
        {
            EditorGUILayout.LabelField($"Filtered Results:", EditorStyles.miniBoldLabel);
            
            bool foundAny = false;
            for (int i = 0; i < mappingsProperty.arraySize; i++)
            {
                SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                SerializedProperty charProperty = element.FindPropertyRelative("character");
                SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
                
                char character = (char)charProperty.intValue;
                bool isEmpty = prefabProperty.objectReferenceValue == null;
                
                // Применяем фильтры
                bool matchesSearch = string.IsNullOrEmpty(_searchFilter) || 
                    character.ToString().ToLower().Contains(_searchFilter.ToLower());
                bool matchesEmpty = !_showOnlyEmpty || isEmpty;
                
                if (matchesSearch && matchesEmpty)
                {
                    foundAny = true;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                    EditorGUILayout.PropertyField(element, GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            if (!foundAny)
            {
                EditorGUILayout.HelpBox("No items match the current filter.", MessageType.Info);
            }
        }
    }
} 