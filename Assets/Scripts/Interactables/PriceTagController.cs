using UnityEngine;
using TMPro;
using BehaviourInject;
using Supermarket.Services.Game;

namespace Supermarket.Interactables
{
    /// <summary>
    /// Компонент для управления отображением ценника на полке
    /// </summary>
    public class PriceTagController : MonoBehaviour
    {
        [Inject] 
        public IRetailPriceService retailPriceService;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI productNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private GameObject visualContainer;
        
        private ProductConfig currentProduct;
        
        private void Awake()
        {
            if (visualContainer != null)
                visualContainer.SetActive(false);
        }
        
        /// <summary>
        /// Обновляет информацию на ценнике
        /// </summary>
        public void UpdatePriceTag(ProductConfig product)
        {
            if (product == null)
            {
                HidePriceTag();
                return;
            }
            
            currentProduct = product;
            
            // Обновляем название товара
            if (productNameText != null)
            {
                productNameText.text = product.ProductName;
            }
            
            // Обновляем цену
            UpdatePrice();
            
            // Показываем ценник
            if (visualContainer != null)
                visualContainer.SetActive(true);
        }
        
        /// <summary>
        /// Обновляет только цену (вызывается при изменении цены в системе)
        /// </summary>
        public void UpdatePrice()
        {
            if (currentProduct == null || retailPriceService == null)
                return;
            
            float retailPrice = retailPriceService.GetRetailPrice(currentProduct.ProductID);
            
            if (priceText != null)
            {
                priceText.text = $"${retailPrice:F2}";
            }
        }
        
        /// <summary>
        /// Скрывает ценник
        /// </summary>
        public void HidePriceTag()
        {
            currentProduct = null;
            
            if (visualContainer != null)
                visualContainer.SetActive(false);
        }
        
        private void OnEnable()
        {
            // Подписываемся на изменения цен
            if (retailPriceService != null)
            {
                retailPriceService.OnPriceChanged += OnRetailPriceChanged;
            }
        }
        
        private void OnDisable()
        {
            // Отписываемся от изменений цен
            if (retailPriceService != null)
            {
                retailPriceService.OnPriceChanged -= OnRetailPriceChanged;
            }
        }
        
        private void OnRetailPriceChanged(string productId, float newPrice)
        {
            if (currentProduct != null && currentProduct.ProductID == productId)
            {
                UpdatePrice();
            }
        }
    }
} 