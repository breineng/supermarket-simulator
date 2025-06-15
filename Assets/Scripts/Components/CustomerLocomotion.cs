using UnityEngine;
using UnityEngine.AI;

namespace Supermarket.Components
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerLocomotion : MonoBehaviour
    {
        [Header("Movement Configuration")]
        [SerializeField] private float _walkSpeed = 3.5f;
        [SerializeField] private float _runSpeed = 5.5f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _animationDampTime = 0.1f;
        
        [Header("Animation Parameters")]
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private string _isWalkingParameter = "IsWalking";
        [SerializeField] private string _isStrafingParameter = "IsStrafing";
        [SerializeField] private string _strafeDirectionParameter = "StrafeDirection";
        
        [Header("Turn Animation Parameters")]
        [SerializeField] private string _leftTurnTrigger = "LeftTurn90";
        [SerializeField] private string _rightTurnTrigger = "RightTurn90";
        [SerializeField] private string _isTurningParameter = "IsTurning";
        [SerializeField] private float _turnAnimationThreshold = 45f; // Угол для активации анимации поворота
        [SerializeField] private float _turnAnimationCooldown = 0.5f; // Задержка между анимациями поворота
        
        [Header("Action Animation Triggers")]
        [SerializeField] private string _pickupTrigger = "Pickup";
        [SerializeField] private string _payTrigger = "Pay";
        [SerializeField] private string _waveTrigger = "Wave";
        
        [Header("Action Animation States")]
        [SerializeField] private string _pickupStateName = "Pickup"; // Имя состояния анимации в Animator
        [SerializeField] private string _payStateName = "Pay"; // Имя состояния анимации в Animator
        
        [Header("Movement Detection")]
        [SerializeField] private float _movementThreshold = 0.1f;
        [SerializeField] private float _angleThreshold = 30f; // Угол для определения strafing
        
        private NavMeshAgent _navAgent;
        private Animator _animator;
        private Vector3 _previousPosition;
        private bool _isMoving = false;
        private float _currentSpeed = 0f;
        
        // Для анимаций поворота
        private bool _isTurning = false;
        private float _lastTurnTime = 0f;
        private Quaternion _targetRotation;
        private bool _waitingForTurnComplete = false;
        
        // Публичные свойства
        public bool IsMoving => _isMoving;
        public float CurrentSpeed => _currentSpeed;
        public bool UseRunSpeed { get; set; } = false;
        public bool IsTurning => _isTurning;
        
        void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            
            // Не ищем Animator в Awake, так как модель может быть загружена позже
            FindAnimator();
            
            if (_navAgent == null)
            {
                Debug.LogError("CustomerLocomotion: NavMeshAgent component missing!", this);
                enabled = false;
                return;
            }
            
            // Настройка NavMeshAgent
            _navAgent.speed = _walkSpeed;
            _navAgent.angularSpeed = _rotationSpeed * 10f; // NavMeshAgent использует градусы/сек
            
            _previousPosition = transform.position;
        }
        
        void Update()
        {
            UpdateMovementState();
            UpdateAnimation();
            UpdateRotation();
        }
        
        private void UpdateMovementState()
        {
            // Определяем, движется ли персонаж
            Vector3 velocity = _navAgent.velocity;
            float speed = velocity.magnitude;
            
            _isMoving = speed > _movementThreshold && !_isTurning;
            _currentSpeed = speed;
            
            // Обновляем скорость NavMeshAgent в зависимости от состояния
            float targetSpeed = UseRunSpeed ? _runSpeed : _walkSpeed;
            if (Mathf.Abs(_navAgent.speed - targetSpeed) > 0.01f)
            {
                _navAgent.speed = targetSpeed;
            }
        }
        
        private void UpdateAnimation()
        {
            if (_animator == null) return;
            
            // Обновляем параметр скорости для blend tree (если используется)
            if (HasParameter(_speedParameter))
            {
                float normalizedSpeed = _currentSpeed / _walkSpeed;
                _animator.SetFloat(_speedParameter, normalizedSpeed, _animationDampTime, Time.deltaTime);
            }
            
            // Обновляем булевый параметр ходьбы
            if (HasParameter(_isWalkingParameter))
            {
                _animator.SetBool(_isWalkingParameter, _isMoving && !_isTurning);
            }
            
            // Обновляем параметр поворота
            if (HasParameter(_isTurningParameter))
            {
                _animator.SetBool(_isTurningParameter, _isTurning);
            }
            
            // Определяем strafing (боковое движение)
            if (_isMoving && !_isTurning && HasParameter(_isStrafingParameter))
            {
                Vector3 moveDirection = _navAgent.velocity.normalized;
                float angle = Vector3.Angle(transform.forward, moveDirection);
                
                bool isStrafing = angle > _angleThreshold;
                _animator.SetBool(_isStrafingParameter, isStrafing);
                
                if (isStrafing && HasParameter(_strafeDirectionParameter))
                {
                    // Определяем направление strafe (влево = -1, вправо = 1)
                    Vector3 cross = Vector3.Cross(transform.forward, moveDirection);
                    float strafeDirection = Mathf.Sign(cross.y);
                    _animator.SetFloat(_strafeDirectionParameter, strafeDirection);
                }
            }
            else if (HasParameter(_isStrafingParameter))
            {
                _animator.SetBool(_isStrafingParameter, false);
            }
        }
        
        private void UpdateRotation()
        {
            // Если выполняется анимация поворота, ждем ее завершения
            if (_waitingForTurnComplete)
            {
                // Проверяем таймаут анимации поворота (защита от застревания)
                if (Time.time - _lastTurnTime > 2f)
                {
                    Debug.LogWarning("CustomerLocomotion: Turn animation timeout, forcing completion");
                    _waitingForTurnComplete = false;
                    _isTurning = false;
                    Resume();
                    return;
                }
                
                // Проверяем, завершилась ли анимация поворота
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsTag("Turn") && stateInfo.normalizedTime >= 0.9f)
                {
                    _waitingForTurnComplete = false;
                    _isTurning = false;
                    Resume(); // Автоматически возобновляем движение после поворота
                }
                else
                {
                    // Во время анимации поворота плавно поворачиваем к цели
                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, 
                        _rotationSpeed * Time.deltaTime * 0.5f);
                }
                return;
            }
            
            // Плавный поворот в направлении движения
            if (_isMoving && _navAgent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 direction = _navAgent.velocity.normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                        _rotationSpeed * Time.deltaTime);
                }
            }
        }
        
        // Публичные методы для управления движением
        public void SetDestination(Vector3 destination)
        {
            if (_navAgent != null && _navAgent.isActiveAndEnabled)
            {
                _navAgent.SetDestination(destination);
            }
        }
        
        public void Stop()
        {
            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
                _navAgent.velocity = Vector3.zero;
            }
        }
        
        public void Resume()
        {
            if (_navAgent != null)
            {
                _navAgent.isStopped = false;
                // Сбрасываем состояние поворота если застряли
                if (_isTurning && Time.time - _lastTurnTime > 2f)
                {
                    Debug.LogWarning("CustomerLocomotion: Resetting stuck turn animation");
                    _isTurning = false;
                    _waitingForTurnComplete = false;
                }
            }
        }
        
        public float GetRemainingDistance()
        {
            if (_navAgent != null && _navAgent.hasPath)
            {
                return _navAgent.remainingDistance;
            }
            return 0f;
        }
        
        public bool HasReachedDestination()
        {
            if (_navAgent == null) return true;
            
            return !_navAgent.pathPending && 
                   _navAgent.remainingDistance <= _navAgent.stoppingDistance &&
                   (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude == 0f);
        }
        
        public bool HasReachedDestination(float distance)
        {
            if (_navAgent == null) return true;
            
            return !_navAgent.pathPending && 
                   _navAgent.remainingDistance <= distance &&
                   (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude == 0f);
        }
        
        // Методы для особых состояний движения
        public void FaceDirection(Vector3 direction, bool useAnimation = true)
        {
            if (direction == Vector3.zero) return;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angle = Quaternion.Angle(transform.rotation, targetRotation);
            
            // Если угол достаточно большой и прошло достаточно времени с последнего поворота
            if (useAnimation && angle > _turnAnimationThreshold && 
                Time.time - _lastTurnTime > _turnAnimationCooldown && 
                !_isMoving && !_isTurning)
            {
                // Запускаем анимацию поворота
                StartTurnAnimation(direction);
            }
            else
            {
                // Обычный плавный поворот
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    _rotationSpeed * Time.deltaTime);
            }
        }
        
        public void FaceTarget(Transform target, bool useAnimation = true)
        {
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0; // Игнорируем вертикальную составляющую
                FaceDirection(direction, useAnimation);
            }
        }
        
        private void StartTurnAnimation(Vector3 direction)
        {
            _targetRotation = Quaternion.LookRotation(direction);
            
            // Определяем направление поворота
            Vector3 cross = Vector3.Cross(transform.forward, direction);
            bool turnLeft = cross.y < 0;
            
            // Запускаем соответствующую анимацию
            if (turnLeft && HasParameter(_leftTurnTrigger))
            {
                _animator.SetTrigger(_leftTurnTrigger);
                _isTurning = true;
                _waitingForTurnComplete = true;
                _lastTurnTime = Time.time;
                Stop(); // Останавливаем движение на время поворота
            }
            else if (!turnLeft && HasParameter(_rightTurnTrigger))
            {
                _animator.SetTrigger(_rightTurnTrigger);
                _isTurning = true;
                _waitingForTurnComplete = true;
                _lastTurnTime = Time.time;
                Stop(); // Останавливаем движение на время поворота
            }
        }
        
        // Методы для проигрывания анимаций действий
        public void PlayPickupAnimation()
        {
            if (_animator != null && HasParameter(_pickupTrigger))
            {
                // Сначала сбрасываем триггер, если он был установлен ранее
                _animator.ResetTrigger(_pickupTrigger);
                // Затем устанавливаем триггер
                _animator.SetTrigger(_pickupTrigger);
            }
        }
        
        public void PlayPayAnimation()
        {
            if (_animator != null && HasParameter(_payTrigger))
            {
                // Сначала сбрасываем триггер, если он был установлен ранее
                _animator.ResetTrigger(_payTrigger);
                // Затем устанавливаем триггер
                _animator.SetTrigger(_payTrigger);
            }
        }
        
        public void PlayWaveAnimation()
        {
            if (_animator != null && HasParameter(_waveTrigger))
            {
                // Сначала сбрасываем триггер, если он был установлен ранее
                _animator.ResetTrigger(_waveTrigger);
                // Затем устанавливаем триггер
                _animator.SetTrigger(_waveTrigger);
            }
        }
        
        // Метод для проигрывания произвольной анимации по имени триггера
        public void PlayActionAnimation(string triggerName)
        {
            if (_animator != null && !string.IsNullOrEmpty(triggerName) && HasParameter(triggerName))
            {
                // Сначала сбрасываем триггер, если он был установлен ранее
                _animator.ResetTrigger(triggerName);
                // Затем устанавливаем триггер
                _animator.SetTrigger(triggerName);
            }
        }
        
        // Метод для установки произвольного bool параметра
        public void SetAnimationBool(string parameterName, bool value)
        {
            if (_animator != null && HasParameter(parameterName))
            {
                _animator.SetBool(parameterName, value);
            }
        }
        
        // Метод для установки произвольного float параметра
        public void SetAnimationFloat(string parameterName, float value)
        {
            if (_animator != null && HasParameter(parameterName))
            {
                _animator.SetFloat(parameterName, value);
            }
        }
        
        // Методы для сброса триггеров анимаций
        public void ResetAllActionTriggers()
        {
            if (_animator == null) return;
            
            if (HasParameter(_pickupTrigger))
                _animator.ResetTrigger(_pickupTrigger);
            if (HasParameter(_payTrigger))
                _animator.ResetTrigger(_payTrigger);
            if (HasParameter(_waveTrigger))
                _animator.ResetTrigger(_waveTrigger);
        }
        
        public void ResetPickupTrigger()
        {
            if (_animator != null && HasParameter(_pickupTrigger))
            {
                _animator.ResetTrigger(_pickupTrigger);
            }
        }
        
        public void ResetPayTrigger()
        {
            if (_animator != null && HasParameter(_payTrigger))
            {
                _animator.ResetTrigger(_payTrigger);
            }
        }
        
        // Вспомогательные методы для проверки параметров Animator
        private bool HasParameter(string parameterName)
        {
            if (_animator == null) return false;
            
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.name == parameterName)
                    return true;
            }
            return false;
        }
        
        // Новый публичный метод для обновления ссылки на Animator
        public void UpdateAnimatorReference()
        {
            FindAnimator();
        }
        
        // Методы для проверки состояния анимаций действий
        public bool IsPlayingActionAnimation(string stateName)
        {
            if (_animator == null) return false;
            
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(stateName);
        }
        
        public bool IsActionAnimationComplete(string stateName, float normalizedTimeThreshold = 0.95f)
        {
            if (_animator == null) return true; // Если нет аниматора, считаем что "завершено"
            
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            
            // Проверяем, что мы в нужном состоянии и анимация почти завершена
            if (stateInfo.IsName(stateName))
            {
                return stateInfo.normalizedTime >= normalizedTimeThreshold && !_animator.IsInTransition(0);
            }
            
            // Если мы не в этом состоянии, проверяем следующее состояние (может быть переход)
            AnimatorStateInfo nextStateInfo = _animator.GetNextAnimatorStateInfo(0);
            if (_animator.IsInTransition(0) && nextStateInfo.IsName(stateName))
            {
                // Мы переходим в это состояние, анимация еще не завершена
                return false;
            }
            
            // Мы не в этом состоянии и не переходим в него - значит анимация завершена или не началась
            return true;
        }
        
        public bool IsPickupAnimationComplete(float normalizedTimeThreshold = 0.95f)
        {
            return IsActionAnimationComplete(_pickupStateName, normalizedTimeThreshold);
        }
        
        public bool IsPayAnimationComplete(float normalizedTimeThreshold = 0.95f)
        {
            return IsActionAnimationComplete(_payStateName, normalizedTimeThreshold);
        }
        
        /// <summary>
        /// Устанавливает приоритет obstacle avoidance для NavMeshAgent
        /// Меньшие значения = более высокий приоритет (0-99)
        /// </summary>
        public void SetAvoidancePriority(int priority)
        {
            if (_navAgent != null)
            {
                _navAgent.avoidancePriority = Mathf.Clamp(priority, 0, 99);
                Debug.Log($"CustomerLocomotion: Set avoidance priority to {_navAgent.avoidancePriority} for {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Возвращает текущий приоритет obstacle avoidance
        /// </summary>
        public int GetAvoidancePriority()
        {
            return _navAgent != null ? _navAgent.avoidancePriority : 50;
        }
        
        private void FindAnimator()
        {
            // Ищем Animator сначала на текущем объекте, затем в дочерних
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            
            if (_animator == null)
            {
                Debug.LogWarning("CustomerLocomotion: Animator component not found yet. It may be loaded later with the character model.", this);
            }
        }
    }
} 