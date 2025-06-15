using System.Collections.Generic;
using UnityEngine;

namespace Supermarket.Components
{
    [DisallowMultipleComponent]
    public class OutlineController : MonoBehaviour
    {
        public static readonly List<OutlineController> AllOutlines = new List<OutlineController>();

        [Tooltip("Controls whether the outline effect is currently active for this object.")]
        public bool IsOutlineEnabled;
        
        // We cache the renderer reference for performance, as it's used every frame by the outline pass.
        [HideInInspector] public Renderer Renderer;

        private void Awake()
        {
            Renderer = GetComponent<Renderer>();
            if (Renderer == null)
            {
                // Also check in children, as many models have the renderer on a child object.
                Renderer = GetComponentInChildren<Renderer>();
            }
        }

        private void OnEnable()
        {
            if (!AllOutlines.Contains(this))
            {
                AllOutlines.Add(this);
            }
        }

        private void OnDisable()
        {
            AllOutlines.Remove(this);
        }
    }
} 