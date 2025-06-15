using UnityEngine;
using Supermarket.Data;

namespace Supermarket.Components
{
    public class CharacterAppearance : MonoBehaviour
    {
        [Header("Model Configuration")]
        [SerializeField] private CharacterAppearanceConfig.GenderModel _currentModel;
        
        [Header("Current Colors")]
        [SerializeField] private Color _topClothingColor = Color.white;
        [SerializeField] private Color _bottomClothingColor = Color.white;
        [SerializeField] private Color _shoesColor = Color.white;
        
        private GameObject _currentModelInstance;
        
        // Применяет конфигурацию модели и цвета к персонажу
        public void ApplyAppearance(CharacterAppearanceConfig.GenderModel model, CustomerData customerData)
        {
            _currentModel = model;
            
            // Удаляем старую модель если есть
            if (_currentModelInstance != null)
            {
                DestroyImmediate(_currentModelInstance);
            }
            
            // Создаем новую модель
            if (model != null && model.modelPrefab != null)
            {
                _currentModelInstance = Instantiate(model.modelPrefab, transform);
                _currentModelInstance.transform.localPosition = Vector3.zero;
                _currentModelInstance.transform.localRotation = Quaternion.identity;
                
                // Применяем цвета
                ApplyClothingColors(customerData.TopClothingColor, customerData.BottomClothingColor, customerData.ShoesColor);
            }
        }
        
        // Применяет цвета одежды к текущей модели
        public void ApplyClothingColors(Color topColor, Color bottomColor, Color shoesColor)
        {
            if (_currentModelInstance == null || _currentModel == null)
                return;
                
            _topClothingColor = topColor;
            _bottomClothingColor = bottomColor;
            _shoesColor = shoesColor;
            
            // Применяем цвет верхней одежды
            ApplyColorToMaterial(_currentModel.topClothingRendererPath, 
                               _currentModel.topClothingMaterialIndex, 
                               _currentModel.topClothing.materialPropertyName, 
                               topColor);
            
            // Применяем цвет нижней одежды
            ApplyColorToMaterial(_currentModel.bottomClothingRendererPath, 
                               _currentModel.bottomClothingMaterialIndex, 
                               _currentModel.bottomClothing.materialPropertyName, 
                               bottomColor);
            
            // Применяем цвет обуви
            ApplyColorToMaterial(_currentModel.shoesRendererPath, 
                               _currentModel.shoesMaterialIndex, 
                               _currentModel.shoes.materialPropertyName, 
                               shoesColor);
        }
        
        private void ApplyColorToMaterial(string rendererPath, int materialIndex, string propertyName, Color color)
        {
            if (string.IsNullOrEmpty(rendererPath))
                return;
                
            Transform rendererTransform = _currentModelInstance.transform.Find(rendererPath);
            if (rendererTransform == null)
            {
                // Пробуем найти рекурсивно
                Renderer[] allRenderers = _currentModelInstance.GetComponentsInChildren<Renderer>();
                foreach (var rend in allRenderers)
                {
                    if (rend.name == rendererPath || rend.transform.name == rendererPath)
                    {
                        rendererTransform = rend.transform;
                        break;
                    }
                }
            }
            
            if (rendererTransform != null)
            {
                Renderer renderer = rendererTransform.GetComponent<Renderer>();
                if (renderer != null && renderer.materials.Length > materialIndex)
                {
                    // Создаем копию материала чтобы не менять общий материал
                    Material mat = renderer.materials[materialIndex];
                    if (mat.HasProperty(propertyName))
                    {
                        mat.SetColor(propertyName, color);
                    }
                    else
                    {
                        Debug.LogWarning($"Material doesn't have property '{propertyName}'");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Couldn't find renderer at path '{rendererPath}'");
            }
        }
        
        // Возвращает экземпляр модели для доступа к Animator и другим компонентам
        public GameObject GetModelInstance()
        {
            return _currentModelInstance;
        }
        
        void OnDestroy()
        {
            if (_currentModelInstance != null)
            {
                DestroyImmediate(_currentModelInstance);
            }
        }
    }
} 