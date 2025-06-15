using UnityEngine;
using UnityEditor;
using Supermarket.Data;

namespace Supermarket.Editor
{
    /// <summary>
    /// Кастомный PropertyDrawer для LetterPrefabMapping
    /// Отображает символ и префаб в одной строке для удобства
    /// </summary>
    [CustomPropertyDrawer(typeof(LetterMappingData.LetterPrefabMapping))]
    public class LetterPrefabMappingDrawer : PropertyDrawer
    {
        private const float CHARACTER_WIDTH = 30f;
        private const float SPACING = 5f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Получаем свойства
            SerializedProperty characterProperty = property.FindPropertyRelative("character");
            SerializedProperty prefabProperty = property.FindPropertyRelative("prefab");
            
            // Вычисляем позиции
            Rect charRect = new Rect(position.x, position.y, CHARACTER_WIDTH, EditorGUIUtility.singleLineHeight);
            Rect prefabRect = new Rect(position.x + CHARACTER_WIDTH + SPACING, position.y, 
                                      position.width - CHARACTER_WIDTH - SPACING, EditorGUIUtility.singleLineHeight);
            
            // Отображаем символ
            char currentChar = (char)characterProperty.intValue;
            string charDisplay = currentChar == ' ' ? "Space" : currentChar.ToString();
            
            // Если символ не отображается корректно, показываем его код
            if (char.IsControl(currentChar) || currentChar < 32)
            {
                charDisplay = $"\\{(int)currentChar}";
            }
            
            // Создаем стиль для отображения символа
            GUIStyle charStyle = new GUIStyle(EditorStyles.textField);
            charStyle.alignment = TextAnchor.MiddleCenter;
            charStyle.fontStyle = FontStyle.Bold;
            
            // Цветовое кодирование для разных типов символов
            Color originalColor = GUI.backgroundColor;
            if (char.IsUpper(currentChar))
            {
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f); // Светло-зеленый для заглавных
            }
            else if (char.IsLower(currentChar))
            {
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1f); // Светло-синий для строчных
            }
            else if (char.IsDigit(currentChar))
            {
                GUI.backgroundColor = new Color(1f, 1f, 0.8f); // Светло-желтый для цифр
            }
            else if (currentChar == ' ')
            {
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f); // Серый для пробела
            }
            
            // Поле для редактирования символа
            EditorGUI.BeginChangeCheck();
            string newCharString = EditorGUI.TextField(charRect, charDisplay, charStyle);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(newCharString))
                {
                    if (newCharString.ToLower() == "space")
                    {
                        characterProperty.intValue = ' ';
                    }
                    else if (newCharString.StartsWith("\\") && newCharString.Length > 1)
                    {
                        // Поддержка ввода кода символа
                        if (int.TryParse(newCharString.Substring(1), out int charCode))
                        {
                            characterProperty.intValue = charCode;
                        }
                    }
                    else
                    {
                        characterProperty.intValue = newCharString[0];
                    }
                }
            }
            
            GUI.backgroundColor = originalColor;
            
            // Поле для префаба с визуальным индикатором
            Color prefabBgColor = GUI.backgroundColor;
            if (prefabProperty.objectReferenceValue == null)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // Светло-красный для пустых префабов
            }
            
            EditorGUI.PropertyField(prefabRect, prefabProperty, GUIContent.none);
            GUI.backgroundColor = prefabBgColor;
            
            // Добавляем tooltip с информацией
            string tooltip = $"Character: '{currentChar}' (Code: {(int)currentChar})";
            if (prefabProperty.objectReferenceValue != null)
            {
                tooltip += $"\nPrefab: {prefabProperty.objectReferenceValue.name}";
            }
            else
            {
                tooltip += "\nPrefab: Not assigned";
            }
            
            GUI.tooltip = tooltip;
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
} 