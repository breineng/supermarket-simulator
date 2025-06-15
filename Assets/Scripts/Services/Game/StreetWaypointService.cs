using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BehaviourInject;

namespace Supermarket.Services.Game
{
    public class StreetWaypointService : MonoBehaviour, IStreetWaypointService
    {
        [Header("Waypoint Configuration")]
        [SerializeField] private Transform[] _streetWaypoints;
        [SerializeField] private Transform _storeEntrancePoint;
        
        [Header("Waypoint Selection")]
        [SerializeField] private bool _preferCloseWaypoints = true;
        [SerializeField] private float _maxDistanceForClose = 20f;
        [SerializeField] private int _maxCloseWaypoints = 3; // Максимум близких точек для выбора
        
        private List<Transform> _waypoints = new List<Transform>();

        void Awake()
        {
            // Инициализируем список waypoints
            if (_streetWaypoints != null && _streetWaypoints.Length > 0)
            {
                _waypoints.AddRange(_streetWaypoints.Where(wp => wp != null));
            }
            
            if (_waypoints.Count == 0)
            {
                Debug.LogWarning("StreetWaypointService: No waypoints configured! Street walking will not work properly.");
            }
        }

        public Transform GetRandomWaypoint()
        {
            if (_waypoints.Count == 0)
            {
                Debug.LogWarning("StreetWaypointService: No waypoints available for random selection");
                return null;
            }
            
            int randomIndex = Random.Range(0, _waypoints.Count);
            return _waypoints[randomIndex];
        }

        public Transform GetNextWaypoint(Vector3 currentPosition, Transform currentWaypoint = null)
        {
            if (_waypoints.Count == 0)
            {
                Debug.LogWarning("StreetWaypointService: No waypoints available");
                return null;
            }
            
            if (_waypoints.Count == 1)
            {
                return _waypoints[0];
            }
            
            List<Transform> availableWaypoints = new List<Transform>(_waypoints);
            
            // Исключаем текущий waypoint из выбора (чтобы не стоять на месте)
            if (currentWaypoint != null && availableWaypoints.Contains(currentWaypoint))
            {
                availableWaypoints.Remove(currentWaypoint);
            }
            
            if (availableWaypoints.Count == 0)
            {
                // Если все исключили, берем случайный
                return GetRandomWaypoint();
            }
            
            if (_preferCloseWaypoints)
            {
                // Найти ближайшие waypoints
                var closeWaypoints = availableWaypoints
                    .Where(wp => Vector3.Distance(currentPosition, wp.position) <= _maxDistanceForClose)
                    .OrderBy(wp => Vector3.Distance(currentPosition, wp.position))
                    .Take(_maxCloseWaypoints)
                    .ToList();
                
                if (closeWaypoints.Count > 0)
                {
                    // Выбираем случайный из близких
                    int randomIndex = Random.Range(0, closeWaypoints.Count);
                    return closeWaypoints[randomIndex];
                }
            }
            
            // Если нет близких waypoints или не используем предпочтение близких, выбираем случайный
            int randomAvailableIndex = Random.Range(0, availableWaypoints.Count);
            return availableWaypoints[randomAvailableIndex];
        }

        public List<Transform> GetAllWaypoints()
        {
            return new List<Transform>(_waypoints);
        }

        public bool HasWaypoints()
        {
            return _waypoints.Count > 0;
        }

        public Transform GetStoreEntrancePoint()
        {
            if (_storeEntrancePoint == null)
            {
                Debug.LogWarning("StreetWaypointService: Store entrance point not configured!");
            }
            return _storeEntrancePoint;
        }
        
        // Методы для динамического управления waypoints
        public void AddWaypoint(Transform waypoint)
        {
            if (waypoint != null && !_waypoints.Contains(waypoint))
            {
                _waypoints.Add(waypoint);
                Debug.Log($"StreetWaypointService: Added waypoint {waypoint.name}");
            }
        }
        
        public void RemoveWaypoint(Transform waypoint)
        {
            if (_waypoints.Contains(waypoint))
            {
                _waypoints.Remove(waypoint);
                Debug.Log($"StreetWaypointService: Removed waypoint {waypoint.name}");
            }
        }
        
        public void SetWaypoints(Transform[] waypoints)
        {
            _waypoints.Clear();
            if (waypoints != null)
            {
                _waypoints.AddRange(waypoints.Where(wp => wp != null));
                Debug.Log($"StreetWaypointService: Set {_waypoints.Count} waypoints");
            }
        }
        
        // Визуализация в редакторе
        void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;
            
            // Рисуем waypoints
            Gizmos.color = Color.blue;
            foreach (Transform waypoint in _waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawWireSphere(waypoint.position, 1f);
                    
                    #if UNITY_EDITOR
                    // Рисуем имя waypoint только в редакторе
                    UnityEditor.Handles.Label(waypoint.position + Vector3.up * 2f, waypoint.name);
                    #endif
                }
            }
            
            // Рисуем соединения между близкими waypoints
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] == null) continue;
                
                for (int j = i + 1; j < _waypoints.Count; j++)
                {
                    if (_waypoints[j] == null) continue;
                    
                    float distance = Vector3.Distance(_waypoints[i].position, _waypoints[j].position);
                    if (distance <= _maxDistanceForClose)
                    {
                        Gizmos.DrawLine(_waypoints[i].position, _waypoints[j].position);
                    }
                }
            }
            
            // Рисуем точку входа в магазин
            if (_storeEntrancePoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_storeEntrancePoint.position, Vector3.one * 2f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(_storeEntrancePoint.position + Vector3.up * 3f, "Store Entrance");
                #endif
            }
        }
    }
} 