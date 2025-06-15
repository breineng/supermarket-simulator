using UnityEngine;
using System.Collections.Generic;
using System;

namespace Supermarket.Services.Game
{
    public interface IPlayerHandVisualsService
    {
        /// <summary>
        /// Инициализирует систему визуализации коробок в руках
        /// </summary>
        /// <param name="handTransform">Точка привязки для коробки в руках</param>
        void Initialize(Transform handTransform);
        
        /// <summary>
        /// Показывает визуализацию коробки в руках
        /// </summary>
        /// <param name="boxData">Данные коробки</param>
        void ShowBoxInHands(BoxData boxData);
        
        /// <summary>
        /// Показывает визуализацию коробки в руках с плавной анимацией
        /// </summary>
        /// <param name="boxData">Данные коробки</param>
        /// <param name="onAnimationComplete">Callback при завершении анимации</param>
        void ShowBoxInHandsAnimated(BoxData boxData, System.Action onAnimationComplete = null);
        
        /// <summary>
        /// Скрывает визуализацию коробки в руках
        /// </summary>
        void HideBoxInHands();
        
        /// <summary>
        /// Скрывает визуализацию коробки в руках с плавной анимацией
        /// </summary>
        /// <param name="onAnimationComplete">Callback при завершении анимации</param>
        void HideBoxInHandsAnimated(System.Action onAnimationComplete = null);
        
        /// <summary>
        /// Открывает визуальную коробку (анимация крышки)
        /// </summary>
        void OpenBox();
        
        /// <summary>
        /// Закрывает визуальную коробку (анимация крышки)
        /// </summary>
        void CloseBox();
        
        /// <summary>
        /// Обновляет визуализацию содержимого коробки
        /// </summary>
        /// <param name="product">Тип товара в коробке</param>
        /// <param name="quantity">Количество товаров</param>
        void UpdateBoxContents(ProductConfig product, int quantity);
        
        /// <summary>
        /// Анимирует изъятие товара из коробки
        /// Товар плавно перелетает из точной позиции в коробке к точной позиции на полке
        /// </summary>
        /// <param name="product">Изъятый товар</param>
        /// <param name="targetPosition">Точная позиция слота на полке, куда "летит" товар</param>
        void AnimateItemRemoval(ProductConfig product, Vector3 targetPosition);
        
        /// <summary>
        /// Анимирует изъятие товара из коробки с callback'ом по завершении
        /// Товар плавно перелетает из точной позиции в коробке к точной позиции на полке
        /// </summary>
        /// <param name="product">Изъятый товар</param>
        /// <param name="targetPosition">Точная позиция слота на полке, куда "летит" товар</param>
        /// <param name="onAnimationComplete">Действие, выполняемое по завершении анимации</param>
        void AnimateItemRemoval(ProductConfig product, Vector3 targetPosition, System.Action onAnimationComplete);
        
        /// <summary>
        /// Анимирует изъятие товара из коробки с целевым поворотом и callback'ом по завершении
        /// Товар плавно перелетает из точной позиции в коробке к точной позиции на полке с нужным поворотом
        /// </summary>
        /// <param name="product">Изъятый товар</param>
        /// <param name="targetPosition">Точная позиция слота на полке, куда "летит" товар</param>
        /// <param name="targetRotation">Поворот товара в целевой позиции</param>
        /// <param name="onAnimationComplete">Действие, выполняемое по завершении анимации</param>
        void AnimateItemRemoval(ProductConfig product, Vector3 targetPosition, Quaternion targetRotation, System.Action onAnimationComplete);
        
        /// <summary>
        /// Анимирует добавление товара в коробку
        /// Товар плавно перелетает из точной позиции на полке к точной позиции в коробке
        /// </summary>
        /// <param name="product">Добавляемый товар</param>
        /// <param name="sourcePosition">Точная позиция слота на полке, откуда "летит" товар</param>
        void AnimateItemAddition(ProductConfig product, Vector3 sourcePosition);
        
        /// <summary>
        /// Анимирует добавление товара в коробку с callback'ом по завершении
        /// Товар плавно перелетает из точной позиции на полке к точной позиции в коробке
        /// </summary>
        /// <param name="product">Добавляемый товар</param>
        /// <param name="sourcePosition">Точная позиция слота на полке, откуда "летит" товар</param>
        /// <param name="onAnimationComplete">Действие, выполняемое по завершении анимации</param>
        void AnimateItemAddition(ProductConfig product, Vector3 sourcePosition, System.Action onAnimationComplete);
        
        /// <summary>
        /// Анимирует добавление товара в коробку с поворотом и callback'ом по завершении
        /// Товар плавно перелетает из точной позиции на полке к точной позиции в коробке
        /// </summary>
        /// <param name="product">Добавляемый товар</param>
        /// <param name="sourcePosition">Точная позиция слота на полке, откуда "летит" товар</param>
        /// <param name="sourceRotation">Поворот товара в исходной позиции</param>
        /// <param name="onAnimationComplete">Действие, выполняемое по завершении анимации</param>
        void AnimateItemAddition(ProductConfig product, Vector3 sourcePosition, Quaternion sourceRotation, System.Action onAnimationComplete);
        
        /// <summary>
        /// Проверяет, отображается ли сейчас коробка в руках
        /// </summary>
        bool IsBoxVisible { get; }
        
        // События для отмены анимаций
        event System.Action<ProductConfig> OnTakeAnimationCancelledMidFlight;
        event System.Action<ProductConfig> OnPlaceAnimationCancelledMidFlight;
    }
} 