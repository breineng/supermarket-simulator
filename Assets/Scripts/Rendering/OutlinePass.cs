using System.Collections.Generic;
using Supermarket.Components;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Supermarket.Rendering
{
    public class OutlinePass : ScriptableRenderPass
    {
        private readonly OutlineFeature.OutlineSettings _settings;
        private readonly Material _outlineMaterial;
        
        private const string PassTag = "OutlinePass";
        
        // Stencil reference values
        private const int StencilReference = 1;

        public OutlinePass(OutlineFeature.OutlineSettings settings)
        {
            _settings = settings;
            
            // Create a material from our hidden shader
            var shader = Shader.Find("Hidden/UniversalOutline");
            if (shader != null)
            {
                _outlineMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Configure the pass to clear the stencil buffer before executing.
            // This is important to ensure no data from previous frames affects the current outline.
            ConfigureClear(ClearFlag.Stencil, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_outlineMaterial == null) return;
            if (renderingData.cameraData.isSceneViewCamera) return; // Don't run in scene view

            var cmd = CommandBufferPool.Get(PassTag);

            using (new ProfilingScope(cmd, new ProfilingSampler(PassTag)))
            {
                // Set material properties from settings
                _outlineMaterial.SetColor("_Color", _settings.OutlineColor);
                _outlineMaterial.SetFloat("_Thickness", _settings.OutlineThickness);

                // We don't need to manually clear the render target here anymore, 
                // because ConfigureClear in the Configure method handles it for us.
                // cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                // cmd.ClearRenderTarget(false, true, Color.clear);

                // 1. RENDER STENCIL: Draw selected objects to the stencil buffer using Pass 0
                foreach (var outlineController in OutlineController.AllOutlines)
                {
                    if (outlineController.IsOutlineEnabled && outlineController.Renderer != null)
                    {
                        cmd.DrawRenderer(outlineController.Renderer, _outlineMaterial, 0, 0); 
                    }
                }
                
                // 2. RENDER OUTLINE: Now, draw the outlines using Pass 1, which has the stencil test
                foreach (var outlineController in OutlineController.AllOutlines)
                {
                    if (outlineController.IsOutlineEnabled && outlineController.Renderer != null)
                    {
                         cmd.DrawRenderer(outlineController.Renderer, _outlineMaterial, 0, 1);
                    }
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
} 