
using UnityEngine;

namespace Interactables
{
    [RequireComponent(typeof(Animator))]
    public class DoorController : MonoBehaviour
    {
        private Animator _animator;
        private static readonly int Opened = Animator.StringToHash("Opened");

        private int _characterCount = 0;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Customer")) return;
            
            _characterCount++;
            UpdateDoorState();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Customer")) return;
            
            _characterCount--;
            if (_characterCount < 0)
            {
                _characterCount = 0;
            }
            UpdateDoorState();
        }

        private void UpdateDoorState()
        {
            _animator.SetBool(Opened, _characterCount > 0);
        }
    }
}
