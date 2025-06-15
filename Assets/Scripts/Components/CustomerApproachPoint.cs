using UnityEngine;

namespace Supermarket.Components
{
    /// <summary>
    /// Компонент для обозначения точки подхода покупателей к полкам
    /// Отображает гизмо в редакторе для удобства настройки
    /// </summary>
    public class CustomerApproachPoint : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private Color gizmoColor = Color.green;
        [SerializeField] private float gizmoSize = 0.5f;
        [SerializeField] private bool showDirection = true;
        [SerializeField] private float directionLength = 1.0f;

        private void OnDrawGizmos()
        {
            // Рисуем сферу в позиции точки подхода
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);
            
            // Рисуем направление (стрелку), показывающую в какую сторону должен смотреть покупатель
            if (showDirection)
            {
                Vector3 direction = transform.forward * directionLength;
                Gizmos.DrawRay(transform.position, direction);
                
                // Рисуем маленький треугольник на конце стрелки
                Vector3 arrowHead = transform.position + direction;
                Vector3 arrowSide1 = arrowHead - transform.forward * 0.2f + transform.right * 0.1f;
                Vector3 arrowSide2 = arrowHead - transform.forward * 0.2f - transform.right * 0.1f;
                
                Gizmos.DrawLine(arrowHead, arrowSide1);
                Gizmos.DrawLine(arrowHead, arrowSide2);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Когда объект выбран, рисуем более яркую версию
            Color selectedColor = gizmoColor;
            selectedColor.a = 1.0f;
            Gizmos.color = selectedColor;
            
            Gizmos.DrawSphere(transform.position, gizmoSize * 0.8f);
            
            if (showDirection)
            {
                Vector3 direction = transform.forward * directionLength;
                Gizmos.DrawRay(transform.position, direction);
            }
        }
    }
} 