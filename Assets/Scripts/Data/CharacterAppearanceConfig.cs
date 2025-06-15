using UnityEngine;

namespace Supermarket.Data
{
    [CreateAssetMenu(fileName = "CharacterAppearance", menuName = "Supermarket/Character Appearance")]
    public class CharacterAppearanceConfig : ScriptableObject
    {
        [System.Serializable]
        public enum Gender
        {
            Male,
            Female
        }
        
        [System.Serializable]
        public class ClothingColorSlot
        {
            public string materialPropertyName = "_BaseColor"; // Название свойства в шейдере
            public Color[] possibleColors;
            
            public Color GetRandomColor()
            {
                if (possibleColors == null || possibleColors.Length == 0)
                    return Color.white;
                    
                return possibleColors[Random.Range(0, possibleColors.Length)];
            }
        }
        
        [System.Serializable]
        public class GenderModel
        {
            public Gender gender;
            public GameObject modelPrefab;
            public string[] possibleNames;
            
            [Header("Clothing Slots")]
            public ClothingColorSlot topClothing = new ClothingColorSlot();
            public ClothingColorSlot bottomClothing = new ClothingColorSlot();
            public ClothingColorSlot shoes = new ClothingColorSlot();
            
            [Header("Material References")]
            [Tooltip("Renderer that has the top clothing material")]
            public string topClothingRendererPath = "Body";
            public int topClothingMaterialIndex = 0;
            
            [Tooltip("Renderer that has the bottom clothing material")]
            public string bottomClothingRendererPath = "Body";
            public int bottomClothingMaterialIndex = 1;
            
            [Tooltip("Renderer that has the shoes material")]
            public string shoesRendererPath = "Body";
            public int shoesMaterialIndex = 2;
            
            public string GetRandomName()
            {
                if (possibleNames == null || possibleNames.Length == 0)
                    return "Customer";
                    
                return possibleNames[Random.Range(0, possibleNames.Length)];
            }
        }
        
        [Header("Character Models")]
        public GenderModel[] genderModels;
        
        [Header("Gender Distribution")]
        [Range(0f, 1f)]
        public float maleSpawnChance = 0.5f;
        
        public GenderModel GetRandomGenderModel()
        {
            if (genderModels == null || genderModels.Length == 0)
                return null;
                
            // Если есть только одна модель, возвращаем её
            if (genderModels.Length == 1)
                return genderModels[0];
                
            // Выбираем пол на основе вероятности
            float random = Random.value;
            Gender selectedGender = random < maleSpawnChance ? Gender.Male : Gender.Female;
            
            // Ищем модель с нужным полом
            foreach (var model in genderModels)
            {
                if (model.gender == selectedGender)
                    return model;
            }
            
            // Если не нашли, возвращаем первую доступную
            return genderModels[0];
        }
    }
} 