using UnityEngine;
using BehaviourInject;

[RequireComponent(typeof(CharacterController))]
public class PlayerHeadBob : MonoBehaviour
{
    [Header("Head Bob Settings")]
    [SerializeField] private float bobSpeed = 14f;          // Скорость покачивания головы
    [SerializeField] private float bobAmount = 0.05f;       // Амплитуда вертикального покачивания
    [SerializeField] private float bobHorizontalAmount = 0.02f; // Амплитуда горизонтального покачивания
    [SerializeField] private float bobSmoothTransition = 12f;   // Скорость сглаживания при остановке
    [SerializeField] private bool enableHeadBob = true;     // Включение/выключение эффекта
    
    [Header("Optional: Manual Camera Override")]
    [SerializeField] private Transform cameraOverride;     // Переопределение камеры (если нужно)
    
    private CharacterController _characterController;
    private Transform _cameraTransform;
    private IInputModeService _inputModeService;
    
    // Head bob state
    private float _timer = 0f;
    private Vector3 _originalCameraPosition;
    private bool _isInitialized = false;
    private bool _wasMovingLastFrame = false;
    
    // Movement detection
    private Vector3 _lastPosition;
    private float _currentMovementSpeed = 0f;
    private const float MOVEMENT_THRESHOLD = 0.01f;

    // Public properties for external access to head bob state (used by PlayerHandVisualsService for box sway sync)
    public float HeadBobTimer => _timer;
    public bool IsPlayerMoving => _currentMovementSpeed > MOVEMENT_THRESHOLD && _characterController != null && _characterController.isGrounded;
    public float MovementSpeed => _currentMovementSpeed;
    public bool IsHeadBobActive => enableHeadBob && _isInitialized;

    [Inject]
    public void Construct(IInputModeService inputModeService)
    {
        _inputModeService = inputModeService;
    }

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        
        // Находим камеру - сначала пробуем переопределение, потом ищем автоматически
        if (cameraOverride != null)
        {
            _cameraTransform = cameraOverride;
        }
        else
        {
            FindCameraTransform();
        }
        
        if (_cameraTransform == null)
        {
            Debug.LogError("PlayerHeadBob: Camera transform not found. Make sure a camera is a child of the Player object or assign it manually.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        InitializeHeadBob();
    }

    void LateUpdate()
    {
        if (!_isInitialized || !enableHeadBob || _cameraTransform == null) 
            return;

        UpdateMovementDetection();
        
        // Применяем head bob только в игровом режиме
        if (_inputModeService != null && _inputModeService.CurrentMode == InputMode.Game)
        {
            ApplyHeadBob();
        }
        else
        {
            // В других режимах плавно возвращаем камеру в исходное положение
            SmoothReturnToOrigin();
        }
    }

    private void FindCameraTransform()
    {
        // Ищем камеру аналогично PlayerController
        if (transform.childCount > 0)
        {
            Transform firstChild = transform.GetChild(0);
            if (firstChild.GetComponent<Camera>() != null)
            {
                _cameraTransform = firstChild;
                return;
            }
        }
        
        // Если первый дочерний объект не камера, ищем главную камеру среди дочерних
        if (Camera.main != null && Camera.main.transform.IsChildOf(transform))
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void InitializeHeadBob()
    {
        if (_cameraTransform == null) return;
        
        _originalCameraPosition = _cameraTransform.localPosition;
        _lastPosition = transform.position;
        _isInitialized = true;
        
        Debug.Log($"PlayerHeadBob: Initialized with camera at local position {_originalCameraPosition}");
    }

    private void UpdateMovementDetection()
    {
        // Вычисляем текущую скорость движения
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - _lastPosition;
        
        // Исключаем Y-компоненту для детекции только горизонтального движения
        deltaPosition.y = 0f;
        
        _currentMovementSpeed = deltaPosition.magnitude / Time.deltaTime;
        _lastPosition = currentPosition;
    }

    private void ApplyHeadBob()
    {
        bool isMoving = _currentMovementSpeed > MOVEMENT_THRESHOLD && _characterController.isGrounded;
        
        if (isMoving)
        {
            // Игрок движется - применяем head bob
            _timer += Time.deltaTime * bobSpeed;
            
            // Используем скорость движения для модуляции амплитуды
            float speedMultiplier = Mathf.Clamp01(_currentMovementSpeed / 5f); // Нормализуем скорость
            
            // Вычисляем смещения
            float yOffset = Mathf.Sin(_timer) * bobAmount * speedMultiplier;
            float xOffset = Mathf.Cos(_timer * 0.5f) * bobHorizontalAmount * speedMultiplier;
            
            // Применяем смещение относительно исходной позиции
            Vector3 targetPosition = _originalCameraPosition + new Vector3(xOffset, yOffset, 0f);
            _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, targetPosition, Time.deltaTime * bobSmoothTransition);
            
            _wasMovingLastFrame = true;
        }
        else
        {
            // Игрок остановился - плавно возвращаем камеру в исходное положение
            SmoothReturnToOrigin();
            
            // Сбрасываем таймер при остановке для более плавного старта
            if (_wasMovingLastFrame)
            {
                _timer = 0f;
                _wasMovingLastFrame = false;
            }
        }
    }

    private void SmoothReturnToOrigin()
    {
        _cameraTransform.localPosition = Vector3.Lerp(
            _cameraTransform.localPosition, 
            _originalCameraPosition, 
            Time.deltaTime * bobSmoothTransition
        );
    }

    // Публичные методы для управления head bob
    public void SetHeadBobEnabled(bool enabled)
    {
        enableHeadBob = enabled;
        
        if (!enabled)
        {
            SmoothReturnToOrigin();
        }
    }

    public void SetBobIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        bobAmount = 0.05f * intensity;
        bobHorizontalAmount = 0.02f * intensity;
    }

    // Метод для сброса позиции камеры (полезно при телепортации игрока)
    public void ResetCameraPosition()
    {
        if (_cameraTransform != null && _isInitialized)
        {
            _cameraTransform.localPosition = _originalCameraPosition;
            _timer = 0f;
        }
    }

    void OnValidate()
    {
        // Ограничиваем значения в инспекторе
        bobSpeed = Mathf.Clamp(bobSpeed, 1f, 30f);
        bobAmount = Mathf.Clamp(bobAmount, 0f, 0.2f);
        bobHorizontalAmount = Mathf.Clamp(bobHorizontalAmount, 0f, 0.1f);
        bobSmoothTransition = Mathf.Clamp(bobSmoothTransition, 1f, 30f);
    }
} 