using UnityEngine;
using BehaviourInject;
// using Supermarket.Services.Game; // BoxData в глобальном неймспейсе или в Assets/Scripts/Data
using Services.UI; // Для IInteractionPromptService
using Core.Interfaces; // Для IInteractable
using Core.Models; // <--- Добавляем using
using Supermarket.Services.Game; // Для IBoxManagerService
// Если BoxData в своем неймспейсе, нужно будет добавить using.

namespace Supermarket.Interactables // Добавляем неймспейс
{
    [RequireComponent(typeof(Rigidbody))] // Added Rigidbody requirement
    public class BoxController : MonoBehaviour, IInteractable
    {
        public BoxData CurrentBoxData { get; private set; }

        private IPlayerHandService _playerHandService;
        private IInteractionPromptService _interactionPromptService;
        private IBoxManagerService _boxManagerService;
        private Rigidbody _rigidbody; // Added Rigidbody reference

        private bool _isFocused = false;
        private bool _isPhysical = false; // To track if the box is in a physical state (dropped)

        [Inject]
        public void Construct(IPlayerHandService playerHandService, IInteractionPromptService interactionPromptService, IBoxManagerService boxManagerService)
        {
            _playerHandService = playerHandService;
            _interactionPromptService = interactionPromptService;
            _boxManagerService = boxManagerService;
        }

        void Awake() // Added Awake
        {
            _rigidbody = GetComponent<Rigidbody>();
            // Make the box non-physical initially (e.g., when spawned by delivery)
            // It will become physical when dropped by the player.
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true; 
            }
        }

        void Start()
        {
            // Регистрируем коробку в BoxManagerService после инициализации зависимостей
            if (_boxManagerService != null)
            {
                _boxManagerService.RegisterBox(this);
            }
            else
            {
                Debug.LogWarning("BoxController: BoxManagerService not available for registration");
            }
        }

        void OnDestroy()
        {
            // Убираем коробку из BoxManagerService при уничтожении
            if (_boxManagerService != null)
            {
                _boxManagerService.UnregisterBox(this);
            }
        }

        public void Initialize(BoxData boxData)
        {
            CurrentBoxData = boxData;
            if (CurrentBoxData == null) // Добавим проверку на null самого BoxData
            {
                Debug.LogError("BoxController initialized with null BoxData! Deactivating.", this);
                gameObject.SetActive(false); 
                return;
            }

            if (CurrentBoxData.ProductInBox != null)
            {
                gameObject.name = $"Box_{CurrentBoxData.ProductInBox.ProductName}";
            }
            else
            {
                gameObject.name = "EmptyBox_Instance"; // Для "полностью пустых" коробок на земле
            }
            _isPhysical = false; 
            if (_rigidbody != null) _rigidbody.isKinematic = true; 
        }

        public InteractionPromptData GetInteractionPrompt() // <--- Меняем тип
        {
            if (CurrentBoxData == null) 
                return new InteractionPromptData("(Ошибка: нет данных о коробке)", PromptType.Complete);

            string text;
            if (CurrentBoxData.ProductInBox == null) 
            {
                text = "взять пустую коробку";
            }
            else if (CurrentBoxData.Quantity <= 0) 
            {
                text = $"взять коробку (пустая)";
            }
            else 
            {
                text = $"взять коробку ({CurrentBoxData.ProductInBox.ProductName} x{CurrentBoxData.Quantity})";
            }
            // Все эти варианты - это RawAction, т.к. PlayerInteractionController добавит "Нажмите Е чтобы..."
            return new InteractionPromptData(text, PromptType.RawAction); 
        }

        public void Interact(GameObject interactor)
        {
            // Разрешаем взаимодействие, если есть CurrentBoxData.
            // ProductInBox МОЖЕТ быть null для "полностью пустой" коробки на земле.
            if (CurrentBoxData == null) 
            {
                Debug.LogError("BoxController: Attempted to interact with a box that has no CurrentBoxData.", this);
                return;
            }

            // Если ProductInBox null, но Quantity > 0 - это странная ситуация, залогируем.
            if (CurrentBoxData.ProductInBox == null && CurrentBoxData.Quantity > 0)
            {
                Debug.LogWarning($"BoxController: Interacting with a box that has no ProductInBox type but reports Quantity > 0 ({CurrentBoxData.Quantity}). Proceeding to pick up as empty.", this);
            }

            if (_playerHandService.IsHoldingBox())
            {
                // TODO: Показать игроку сообщение, что руки уже заняты
                // For now, let's provide a more specific prompt if focused
                if (_interactionPromptService != null && _isFocused) 
                {
                     _interactionPromptService.ShowPrompt("Руки заняты!"); 
                }
                else
                {
                    Debug.Log("Player Hand is already full.");
                }
                return;
            }

            // Обновляем Debug.Log для корректной работы с ProductInBox == null
            string productName = CurrentBoxData.ProductInBox != null ? CurrentBoxData.ProductInBox.ProductName : "Empty Generic Box";
            Debug.Log($"Interacting with box: {productName} x {CurrentBoxData.Quantity}");
            
            _playerHandService.PickupBox(CurrentBoxData); 
            
            // BoxManagerService автоматически уберет коробку из списка через OnDestroy
            Destroy(gameObject); 
        }
        
        // New method to make the box physical and apply force
        public void SetPhysicalAndDrop(Vector3 initialVelocity)
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = initialVelocity; // Apply initial velocity directly for more control than AddForce for a single impulse
                // Or use AddForce if you prefer:
                // _rigidbody.AddForce(forceDirection, ForceMode.Impulse);
                _isPhysical = true;
                Debug.Log($"BoxController: {gameObject.name} is now physical and dropped.");
            }
            else
            {
                Debug.LogError($"BoxController: Rigidbody not found on {gameObject.name}. Cannot make physical.");
            }
        }

        public void OnFocus()
        {
            _isFocused = true;
            // TODO: Добавить визуальное выделение, если нужно
            // if (_interactionPromptService != null) // Уже есть в PlayerInteractionController
            // { 
            // _interactionPromptService.ShowPrompt(GetInteractionPrompt());
            // }
        }

        public void OnBlur()
        {
            _isFocused = false;
            // TODO: Убрать визуальное выделение
            // if (_interactionPromptService != null) // Уже есть в PlayerInteractionController
            // {
            // _interactionPromptService.HidePrompt();
            // }
        }
    }
} 