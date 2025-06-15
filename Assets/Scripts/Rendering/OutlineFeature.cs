using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Supermarket.Rendering
{
    public class OutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlineSettings
        {
            [Tooltip("The color of the outline effect.")]
            public Color OutlineColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);

            [Tooltip("The thickness of the outline.")]
            [Range(0, 0.5f)]
            public float OutlineThickness = 0.02f;
            
            [Tooltip("The layer mask to determine which objects can be outlined.")]
            public LayerMask OutlineLayer;
        }

        public OutlineSettings Settings = new OutlineSettings();
        private OutlinePass _outlinePass;

        public override void Create()
        {
            _outlinePass = new OutlinePass(Settings);
            
            // The render pass event should be set to run after all opaque and skybox rendering is complete.
            _outlinePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // We only want to add the pass if there's something to outline.
            // This is a small optimization to avoid running the pass unnecessarily.
            if (Components.OutlineController.AllOutlines.Count > 0)
            {
                renderer.EnqueuePass(_outlinePass);
            }
        }
    }
} 