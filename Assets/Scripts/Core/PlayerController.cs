using UnityEngine;
using UnityEngine.InputSystem; // Важно для New Input System
using BehaviourInject; // Для [Inject]

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController _characterController;
    private PlayerInput _playerInput; // Для доступа к actions, если Behavior = Invoke Unity Events
    private IInputModeService _inputModeService;

    private Vector2 _moveInput;
    private Vector2 _lookInput;

    public float moveSpeed = 5f;
    public float lookSensitivity = 0.1f; // Чувствительность мыши
    public float gravity = -9.81f; // Сила гравитации
    public float groundCheckDistance = 0.2f; // Небольшое расстояние для проверки земли под ногами
    public LayerMask groundLayer; // Слой (или слои) для определения земли

    private float _verticalVelocity; // Текущая вертикальная скорость
    private bool _isGrounded;

    private float _cameraPitch = 0f; // Вертикальный угол камеры
    private Transform _cameraTransform; // Трансформ дочерней камеры

    [Inject]
    public void Construct(IInputModeService inputModeService)
    {
        _inputModeService = inputModeService;
    }

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>(); // Получаем PlayerInput, если он есть

        // Предполагаем, что камера - первый дочерний объект. 
        // В идеале, лучше иметь прямое назначение или более надежный поиск.
        if (transform.childCount > 0)
        {
            _cameraTransform = transform.GetChild(0); // Получаем трансформ камеры
             if (_cameraTransform.GetComponent<Camera>() == null)
             {
                Debug.LogWarning("PlayerController: First child is not a Camera. Mouse look might not work correctly.");
                _cameraTransform = null; // Сбрасываем, если это не камера
             }
        }
        if(_cameraTransform == null && Camera.main != null && Camera.main.transform.IsChildOf(transform))
        {
            // Если первая не подошла, но главная камера дочерняя - используем ее.
             _cameraTransform = Camera.main.transform;
        }
        
        if (_cameraTransform == null)
        {
            Debug.LogError("PlayerController: Camera transform not found. Make sure a camera is a child of the Player object.");
        }
    }

    void Start()
    {
        // Устанавливаем начальный игровой режим
        if (_inputModeService != null)
        {
            _inputModeService.SetInputMode(InputMode.Game);
            Debug.Log($"PlayerController Start: SetInputMode(InputMode.Game) called. Cursor.lockState = {UnityEngine.Cursor.lockState}, Cursor.visible = {UnityEngine.Cursor.visible}");
        }
        else
        {
            Debug.LogError("PlayerController: IInputModeService is null in Start! Cannot set initial input mode.");
        }
    }

    // Этот метод будет вызываться компонентом PlayerInput, если Behavior = Invoke Unity Events
    // и есть Action "Move" с таким именем.
    public void OnMove(InputAction.CallbackContext context)
    {
        if (_inputModeService != null && _inputModeService.CurrentMode == InputMode.Game)
        {
            _moveInput = context.ReadValue<Vector2>();
        }
        else
        {
            // For any other mode (UI, CashDeskOperation, MovingToCashDesk) or if service is null, disable movement.
            _moveInput = Vector2.zero;
        }
    }

    // Этот метод будет вызываться компонентом PlayerInput, если Behavior = Invoke Unity Events
    // и есть Action "Look" с таким именем.
    public void OnLook(InputAction.CallbackContext context)
    {
        if (_inputModeService != null && (_inputModeService.CurrentMode == InputMode.Game || _inputModeService.CurrentMode == InputMode.CashDeskOperation))
        {
            _lookInput = context.ReadValue<Vector2>();
        }
        else
        {
            // For UI mode, MovingToCashDesk, or if service is null, disable look.
            _lookInput = Vector2.zero;
        }
    }

    void Update()
    {
        if (_inputModeService == null) 
        {
            // If service is not available, ensure no input is processed
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            return;
        }
        
        HandleGravityAndGroundedCheck(); // Gravity should always apply

        if (_inputModeService.CurrentMode == InputMode.Game)
        {
            HandleMovement(); // Process movement input
        }
        // No else for HandleMovement as _moveInput is already zeroed if not in Game mode by OnMove
        
        if (_inputModeService.CurrentMode == InputMode.Game || _inputModeService.CurrentMode == InputMode.CashDeskOperation)
        {
            HandleLook(); // Process look input
        }
        // No else for HandleLook as _lookInput is already zeroed if not in Game or CashDeskOperation mode by OnLook
    }

    private void HandleGravityAndGroundedCheck()
    {
        // Проверка нахождения на земле
        // Используем _characterController.isGrounded, он обычно достаточно хорош
        _isGrounded = _characterController.isGrounded; 
        // Дополнительно можно использовать Physics.CheckSphere, если isGrounded не устраивает:
        // Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _characterController.height / 2 + _characterController.radius - groundCheckDistance, transform.position.z);
        // _isGrounded = Physics.CheckSphere(spherePosition, _characterController.radius, groundLayer, QueryTriggerInteraction.Ignore);

        if (_isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f; // Немного прижимаем к земле, чтобы избежать "подпрыгивания"
        }

        // Применяем гравитацию
        _verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        if (_characterController == null) return;

        Vector3 horizontalMove = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        Vector3 verticalMove = Vector3.up * _verticalVelocity; // Движение по вертикали (гравитация)

        // Объединяем горизонтальное движение и вертикальное (гравитацию)
        _characterController.Move((horizontalMove * moveSpeed + verticalMove) * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (_cameraTransform == null) return;

        // Горизонтальное вращение (вокруг оси Y) применяется ко всему объекту Player
        transform.Rotate(Vector3.up, _lookInput.x * lookSensitivity);

        // Вертикальное вращение (вокруг оси X) применяется только к камере
        _cameraPitch -= _lookInput.y * lookSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -90f, 90f); // Ограничиваем угол, чтобы не было "переворота"

        _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0, 0);
    }
} 