using UnityEngine;
using Core.Interfaces;
using Core.Models;

namespace Supermarket.Components
{
    /// <summary>
    /// A simple component to attach to product prefabs, allowing them
    /// to hold a reference to their corresponding ProductConfig ScriptableObject.
    /// </summary>
    public class ProductHolder : MonoBehaviour, IInteractable
    {
        public ProductConfig Product;
        
        // IInteractable implementation for scanned items on cash desk
        public InteractionPromptData GetInteractionPrompt()
        {
            // Only show prompt if this product is on the scannable layer (on cash desk)
            if (gameObject.layer == LayerMask.NameToLayer("ScannableItem"))
            {
                return new InteractionPromptData("[ЛКМ] сканировать товар", PromptType.Complete);
            }
            return InteractionPromptData.Empty;
        }
        
        public void Interact(GameObject interactor)
        {
            // Scanning is handled by left mouse button, not by E key
            // This method is here just to satisfy the IInteractable interface
        }
        
        public void OnFocus()
        {
            // Visual highlighting is handled by OutlineController
        }
        
        public void OnBlur()
        {
            // Visual highlighting is handled by OutlineController
        }
    }
} 