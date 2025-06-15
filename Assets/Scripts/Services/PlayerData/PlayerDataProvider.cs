using UnityEngine;
using BehaviourInject;

namespace Supermarket.Services.PlayerData
{
    public class PlayerDataProvider : MonoBehaviour, IPlayerDataProvider
    {
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterController _characterController;
        
        void Awake()
        {
            if (_playerTransform == null)
            {
                // Пытаемся найти на том же GameObject
                _playerTransform = transform;
            }
            
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }
        }
        
        public Vector3 GetPlayerPosition()
        {
            return _playerTransform != null ? _playerTransform.position : Vector3.zero;
        }
        
        public Vector3 GetPlayerRotation()
        {
            return _playerTransform != null ? _playerTransform.eulerAngles : Vector3.zero;
        }
        
        public void SetPlayerPosition(Vector3 position)
        {
            if (_playerTransform != null)
            {
                if (_characterController != null)
                {
                    _characterController.enabled = false;
                    _playerTransform.position = position;
                    _characterController.enabled = true;
                }
                else
                {
                    _playerTransform.position = position;
                }
            }
        }
        
        public void SetPlayerRotation(Vector3 rotation)
        {
            if (_playerTransform != null)
            {
                _playerTransform.eulerAngles = rotation;
            }
        }
    }
} 