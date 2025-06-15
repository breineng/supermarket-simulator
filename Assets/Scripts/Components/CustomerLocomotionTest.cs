using UnityEngine;
using Supermarket.Components;

namespace Supermarket.Components
{
    [RequireComponent(typeof(CustomerLocomotion))]
    public class CustomerLocomotionTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _waitTimeAtWaypoint = 2f;
        [SerializeField] private bool _loopWaypoints = true;
        [SerializeField] private bool _testAnimations = true;
        
        private CustomerLocomotion _locomotion;
        private int _currentWaypointIndex = 0;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;
        
        void Start()
        {
            _locomotion = GetComponent<CustomerLocomotion>();
            
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning("CustomerLocomotionTest: No waypoints set! Please add waypoints in the inspector.");
                enabled = false;
                return;
            }
            
            // Начинаем движение к первой точке
            MoveToNextWaypoint();
        }
        
        void Update()
        {
            if (_isWaiting)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= _waitTimeAtWaypoint)
                {
                    _isWaiting = false;
                    _waitTimer = 0f;
                    MoveToNextWaypoint();
                }
            }
            else if (_locomotion.HasReachedDestination())
            {
                OnReachedWaypoint();
            }
        }
        
        private void MoveToNextWaypoint()
        {
            if (_currentWaypointIndex >= _waypoints.Length)
            {
                if (_loopWaypoints)
                {
                    _currentWaypointIndex = 0;
                }
                else
                {
                    Debug.Log("CustomerLocomotionTest: Completed all waypoints.");
                    enabled = false;
                    return;
                }
            }
            
            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint != null)
            {
                _locomotion.SetDestination(targetWaypoint.position);
                Debug.Log($"Moving to waypoint {_currentWaypointIndex}: {targetWaypoint.name}");
            }
            
            _currentWaypointIndex++;
        }
        
        private void OnReachedWaypoint()
        {
            Debug.Log($"Reached waypoint {_currentWaypointIndex - 1}");
            
            // Тестируем анимации действий
            if (_testAnimations)
            {
                int randomAction = Random.Range(0, 3);
                switch (randomAction)
                {
                    case 0:
                        Debug.Log("Playing Pickup animation");
                        _locomotion.PlayPickupAnimation();
                        break;
                    case 1:
                        Debug.Log("Playing Pay animation");
                        _locomotion.PlayPayAnimation();
                        break;
                    case 2:
                        Debug.Log("Playing Wave animation");
                        _locomotion.PlayWaveAnimation();
                        break;
                }
            }
            
            _isWaiting = true;
        }
        
        // Методы для тестирования через Inspector кнопки
        [ContextMenu("Test Run Speed")]
        public void TestRunSpeed()
        {
            if (_locomotion != null)
            {
                _locomotion.UseRunSpeed = !_locomotion.UseRunSpeed;
                Debug.Log($"Run speed: {_locomotion.UseRunSpeed}");
            }
        }
        
        [ContextMenu("Test Stop/Resume")]
        public void TestStopResume()
        {
            if (_locomotion != null)
            {
                if (_locomotion.IsMoving)
                {
                    _locomotion.Stop();
                    Debug.Log("Stopped movement");
                }
                else
                {
                    _locomotion.Resume();
                    Debug.Log("Resumed movement");
                }
            }
        }
        
        [ContextMenu("Test All Animations")]
        public void TestAllAnimations()
        {
            if (_locomotion != null)
            {
                StartCoroutine(TestAnimationsCoroutine());
            }
        }
        
        [ContextMenu("Test Turn Animations")]
        public void TestTurnAnimations()
        {
            if (_locomotion != null)
            {
                StartCoroutine(TestTurnAnimationsCoroutine());
            }
        }
        
        private System.Collections.IEnumerator TestAnimationsCoroutine()
        {
            Debug.Log("Testing Pickup animation...");
            _locomotion.PlayPickupAnimation();
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Testing Pay animation...");
            _locomotion.PlayPayAnimation();
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Testing Wave animation...");
            _locomotion.PlayWaveAnimation();
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Animation test complete!");
        }
        
        private System.Collections.IEnumerator TestTurnAnimationsCoroutine()
        {
            _locomotion.Stop();
            
            Debug.Log("Testing Left Turn 90 animation...");
            Vector3 leftDirection = Quaternion.Euler(0, -90, 0) * transform.forward;
            _locomotion.FaceDirection(leftDirection, true);
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Testing Right Turn 90 animation...");
            Vector3 rightDirection = Quaternion.Euler(0, 90, 0) * transform.forward;
            _locomotion.FaceDirection(rightDirection, true);
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Testing 180 degree turn...");
            Vector3 backDirection = -transform.forward;
            _locomotion.FaceDirection(backDirection, true);
            yield return new WaitForSeconds(2f);
            
            Debug.Log("Turn animation test complete!");
            _locomotion.Resume();
        }
        
        void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;
            
            // Рисуем путь между waypoints
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;
                
                // Рисуем сферу на каждом waypoint
                Gizmos.DrawWireSphere(_waypoints[i].position, 0.5f);
                
                // Рисуем линию к следующему waypoint
                if (i < _waypoints.Length - 1 && _waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                }
                else if (_loopWaypoints && _waypoints[0] != null)
                {
                    // Рисуем линию от последнего к первому если включен loop
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[0].position);
                }
            }
            
            // Рисуем текущую позицию и цель
            if (Application.isPlaying && _locomotion != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }
} 