using Core.Interfaces;

namespace Supermarket.Services.Game
{
    public interface IInteractionService
    {
        IInteractable CurrentFocusedInteractable { get; }
        void SetFocusedInteractable(IInteractable interactable);
        void ClearFocusedInteractable();
    }
} 