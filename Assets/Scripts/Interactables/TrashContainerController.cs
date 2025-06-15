using UnityEngine;
using Supermarket.Interactables;

namespace Supermarket.Interactables
{
    /// <summary>
    /// Контроллер мусорного контейнера. Уничтожает коробки, которые в него попадают.
    /// Использует зону обнаружения (Trigger Collider) для автоматического уничтожения коробок.
    /// </summary>
    public class TrashContainerController : MonoBehaviour
    {
        [Header("Trash Container Settings")]
        [SerializeField] private float _destructionDelay = 0.5f; // Задержка перед уничтожением для плавности
        [SerializeField] private AudioClip _trashSound; // Звук попадания в мусор (опционально)
        [SerializeField] private ParticleSystem _trashEffect; // Эффект попадания в мусор (опционально)
        
        [Header("Detection Zone")]
        [SerializeField] private Collider _detectionCollider; // Триггер-зона для обнаружения коробок

        private void Awake()
        {
            // Проверяем наличие триггер-коллайдера
            if (_detectionCollider == null)
            {
                // Ищем триггер-коллайдер в дочерних объектах или на самом объекте
                _detectionCollider = GetComponentInChildren<Collider>();
                if (_detectionCollider != null && !_detectionCollider.isTrigger)
                {
                    Debug.LogWarning($"TrashContainerController: Found collider but it's not a trigger. Setting isTrigger = true on {_detectionCollider.name}", this);
                    _detectionCollider.isTrigger = true;
                }
            }

            if (_detectionCollider == null)
            {
                Debug.LogError("TrashContainerController: No detection collider found! Trash container will not work properly.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Проверяем, является ли объект коробкой
            BoxController boxController = other.GetComponentInParent<BoxController>();
            if (boxController != null)
            {
                Debug.Log($"TrashContainerController: Box '{other.name}' entered trash container. Starting destruction process.", this);
                StartCoroutine(DestroyBoxWithDelay(boxController));
            }
        }

        private System.Collections.IEnumerator DestroyBoxWithDelay(BoxController boxController)
        {
            // Ждем перед уничтожением для плавности
            yield return new WaitForSeconds(_destructionDelay);

            if (boxController != null) // Проверяем, что коробка еще существует
            {
                // Воспроизводим звук (если есть)
                if (_trashSound != null)
                {
                    AudioSource.PlayClipAtPoint(_trashSound, boxController.transform.position);
                }

                // Воспроизводим эффект (если есть)
                if (_trashEffect != null)
                {
                    var effect = Instantiate(_trashEffect, boxController.transform.position, boxController.transform.rotation);
                    effect.Play();
                    
                    // Уничтожаем эффект через время его проигрывания
                    Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
                }

                string boxInfo = "Unknown Box";
                if (boxController.CurrentBoxData != null)
                {
                    if (boxController.CurrentBoxData.ProductInBox != null)
                    {
                        boxInfo = $"{boxController.CurrentBoxData.ProductInBox.ProductName} x{boxController.CurrentBoxData.Quantity}";
                    }
                    else
                    {
                        boxInfo = "Empty Box";
                    }
                }

                Debug.Log($"TrashContainerController: Destroying box: {boxInfo}", this);
                
                // Уничтожаем коробку
                Destroy(boxController.gameObject);
            }
        }

        #region Editor Gizmos
        
        private void OnDrawGizmosSelected()
        {
            // Отображаем зону обнаружения в редакторе
            if (_detectionCollider != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Красный полупрозрачный
                Gizmos.matrix = Matrix4x4.TRS(_detectionCollider.transform.position, 
                                             _detectionCollider.transform.rotation, 
                                             _detectionCollider.transform.lossyScale);
                
                if (_detectionCollider is BoxCollider boxCol)
                {
                    Gizmos.DrawCube(boxCol.center, boxCol.size);
                }
                else if (_detectionCollider is SphereCollider sphereCol)
                {
                    Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
                }
                else if (_detectionCollider is CapsuleCollider capsuleCol)
                {
                    Gizmos.DrawCube(capsuleCol.center, new Vector3(capsuleCol.radius * 2, capsuleCol.height, capsuleCol.radius * 2));
                }
            }
        }
        
        #endregion
    }
} 