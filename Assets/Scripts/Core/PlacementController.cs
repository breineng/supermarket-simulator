using UnityEngine;
using UnityEngine.InputSystem;
using BehaviourInject;

public class PlacementController : MonoBehaviour
{
    private IPlacementService _placementService;
    private IInputModeService _inputModeService;
    private PlayerInput _playerInput;
    
    [SerializeField] private Camera _mainCamera;

    private InputAction _confirmAction;
    private InputAction _cancelAction;
    private InputAction _rotateAction;

    public float maxPlacementDistance = 10f;
    public LayerMask placementLayermask = -1; // По умолчанию все слои

    [Inject]
    public void Construct(IPlacementService placementService, IInputModeService inputModeService)
    {
        _placementService = placementService;
        _inputModeService = inputModeService;
    }

    void Awake()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlacementController: PlayerInput component not found on this GameObject or its parents!");
            enabled = false;
            return;
        }

        _confirmAction = _playerInput.actions.FindAction("ConfirmPlacement");
        _cancelAction = _playerInput.actions.FindAction("CancelPlacement");
        _rotateAction = _playerInput.actions.FindAction("RotatePlacement");

        if (_confirmAction == null) Debug.LogError("PlacementController Awake: 'ConfirmPlacement' action NOT FOUND!");
        if (_cancelAction == null) Debug.LogError("PlacementController Awake: 'CancelPlacement' action NOT FOUND!");
        if (_rotateAction == null) 
        {
            Debug.LogError("PlacementController Awake: 'RotatePlacement' action NOT FOUND!"); 
        }
        else
        {
            Debug.Log("PlacementController Awake: 'RotatePlacement' action FOUND successfully.");
        }
        
        // Проверяем, назначена ли камера в инспекторе
        if (_mainCamera == null)
        {
            // Если не назначена, пробуем Camera.main как запасной вариант
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                Debug.LogWarning("PlacementController: Main camera not assigned in inspector, using Camera.main");
            }
            else
            {
                Debug.LogError("PlacementController: Main camera not assigned and Camera.main is null!");
                enabled = false;
            }
        }
    }

    void OnEnable()
    {
        if (_confirmAction != null) _confirmAction.performed += OnConfirmPlacement;
        if (_cancelAction != null) _cancelAction.performed += OnCancelPlacement;
        
        if (_rotateAction != null) 
        {
            _rotateAction.performed += OnRotatePlacement;
            Debug.Log("PlacementController OnEnable: Subscribed to _rotateAction.performed.");
        }
        else
        {
            Debug.LogError("PlacementController OnEnable: _rotateAction is NULL, cannot subscribe.");
        }
    }

    void OnDisable()
    {
        if (_confirmAction != null) _confirmAction.performed -= OnConfirmPlacement;
        if (_cancelAction != null) _cancelAction.performed -= OnCancelPlacement;
        if (_rotateAction != null) _rotateAction.performed -= OnRotatePlacement;
    }

    void Update()
    {
        if (!ShouldUpdatePlacement()) return;

        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, placementLayermask, QueryTriggerInteraction.Ignore))
        {
            _placementService.UpdatePlacementPosition(hit.point, true); 
        }
        else
        {
            _placementService.UpdatePlacementPosition(ray.origin + ray.direction * maxPlacementDistance, false); 
        }
    }

    private bool ShouldUpdatePlacement()
    {
        return _placementService != null && (_placementService.IsInPlacementMode || _placementService.IsInRelocateMode) && 
               _inputModeService != null && _inputModeService.CurrentMode == InputMode.Game &&
               _mainCamera != null;
    }

    private void OnConfirmPlacement(InputAction.CallbackContext context)
    {
        if (!ShouldUpdatePlacement()) return;
        _placementService.ConfirmPlacement();
    }

    private void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (!ShouldUpdatePlacement()) return;
        _placementService.CancelPlacementMode();
    }

    public void OnRotatePlacement(InputAction.CallbackContext context)
    {
        Debug.Log("OnRotatePlacement called."); 
        if (!ShouldUpdatePlacement()) 
        {
            Debug.Log("OnRotatePlacement: ShouldUpdatePlacement() is false."); 
            return;
        }

        float rotationValue = context.ReadValue<float>();
        Debug.Log($"OnRotatePlacement: rotationValue = {rotationValue}"); 

        if (Mathf.Abs(rotationValue) > 0.001f) 
        {
            Debug.Log("OnRotatePlacement: Calling _placementService.RotatePreview"); 
            _placementService.RotatePreview(rotationValue);
        }
        else
        {
            Debug.Log("OnRotatePlacement: rotationValue too small, not rotating."); 
        }
    }
} 