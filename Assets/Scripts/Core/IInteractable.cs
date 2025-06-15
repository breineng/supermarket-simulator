using UnityEngine;
using Core.Models;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for objects that the player can interact with.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called when the player interacts with this object.
        /// </summary>
        /// <param name="interactor">The GameObject that initiated the interaction (e.g., the player).</param>
        void Interact(GameObject interactor);

        /// <summary>
        /// Provides a text prompt for the interaction (e.g., "Press E to use Computer").
        /// Can return null or empty if no prompt is needed.
        /// </summary>
        InteractionPromptData GetInteractionPrompt();

        /// <summary>
        /// Called when the interactable object comes into focus for the player.
        /// </summary>
        void OnFocus();

        /// <summary>
        /// Called when the interactable object loses focus for the player.
        /// </summary>
        void OnBlur();
    }
} 