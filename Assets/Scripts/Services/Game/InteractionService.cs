using UnityEngine;
using Core.Interfaces;

namespace Supermarket.Services.Game
{
    public class InteractionService : IInteractionService
    {
        private IInteractable _currentFocusedInteractable;
        
        public IInteractable CurrentFocusedInteractable => _currentFocusedInteractable;
        
        public void SetFocusedInteractable(IInteractable interactable)
        {
            _currentFocusedInteractable = interactable;
        }
        
        public void ClearFocusedInteractable()
        {
            _currentFocusedInteractable = null;
        }
    }
} 