using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using VzDev.RenderingUtils.Outline;
using System.Linq;

namespace VzDev.RenderingUtils.Outline
{
    public class OutlineRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Shader maskShader;
            public Shader compositeShader;
            [ColorUsage(true, true), Tooltip("Hover 中的目標描邊顏色")]
            public Color outlineColor = new Color(0.25f, 0.85f, 1f, 1f);
            [ColorUsage(true, true), Tooltip("已選取目標的描邊顏色")]
            public Color selectedColor = new Color(1f, 0.65f, 0f, 1f);
            [Range(1f, 8f)] public float thickness = 2f;
        }

        public Settings settings = new Settings();
        private OutlinePass pass;

        public override void Create()
        {
            pass = new OutlinePass(settings)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 沒有任何群組有目標時，完全不排入 Pass，等同零額外 Draw Call
            if (!HighlightRegistry.HasAny()) return;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
        }
    }

    internal class OutlinePass : ScriptableRenderPass
    {
        private readonly Material maskMaterial;
        private readonly Material compositeMaterial;
        private readonly Color hoverColor;
        private readonly Color selectedColor;
        private readonly float thickness;

        private class GroupPassData
        {
            public IReadOnlyCollection<Renderer> targets;
            public Material maskMaterial;
        }

        private class CompositePassData
        {
            public TextureHandle maskTex;
            public Material compositeMaterial;
            public Color outlineColor;
            public float thickness;
        }

        public OutlinePass(OutlineRendererFeature.Settings settings)
        {
            maskMaterial = CoreUtils.CreateEngineMaterial(settings.maskShader);
            compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);
            hoverColor = settings.outlineColor;
            selectedColor = settings.selectedColor;
            thickness = settings.thickness;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!HighlightRegistry.HasAny()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var selected = HighlightRegistry.Get(HighlightGroup.Selected);
            var hover = HighlightRegistry.Get(HighlightGroup.Hover);

            RenderGroup(renderGraph, resourceData, cameraData, selected, selectedColor);

            // 排除已經在 Selected 裡的物件，避免 hover 的顏色蓋掉 selected 的顏色
            _hoverExclusiveBuffer.Clear();
            foreach (var r in hover)
            {
                if (r != null && !selected.Contains(r))
                    _hoverExclusiveBuffer.Add(r);
            }
            RenderGroup(renderGraph, resourceData, cameraData, _hoverExclusiveBuffer, hoverColor);
        }

        // 類別欄位，避免每幀 new HashSet 造成 GC Allocation
        private readonly HashSet<Renderer> _hoverExclusiveBuffer = new();

        private void RenderGroup(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            UniversalCameraData cameraData,
            IReadOnlyCollection<Renderer> renderers,
            Color color)
        {
            if (renderers.Count == 0) return;

            TextureHandle maskHandle = renderGraph.CreateTexture(new TextureDesc(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height)
            {
                colorFormat = GraphicsFormat.R8_UNorm,
                clearBuffer = true,
                clearColor = Color.clear,
                name = "_HighlightMaskTex"
            });

            using (var builder = renderGraph.AddRasterRenderPass<GroupPassData>("Outline_Mask", out var passData))
            {
                passData.targets = renderers;
                passData.maskMaterial = maskMaterial;

                builder.SetRenderAttachment(maskHandle, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((GroupPassData data, RasterGraphContext ctx) =>
                {
                    foreach (var r in data.targets)
                    {
                        if (r == null) continue; // 逐個保護，任一物件被銷毀不影響其他物件的繪製
                        ctx.cmd.DrawRenderer(r, data.maskMaterial, 0, 0);
                    }
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline_Composite", out var passData))
            {
                passData.maskTex = maskHandle;
                passData.compositeMaterial = compositeMaterial;
                passData.outlineColor = color;
                passData.thickness = thickness;

                builder.UseTexture(maskHandle, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) =>
                {
                    data.compositeMaterial.SetTexture("_MaskTex", data.maskTex);
                    data.compositeMaterial.SetColor("_OutlineColor", data.outlineColor);
                    data.compositeMaterial.SetFloat("_Thickness", data.thickness);
                    Blitter.BlitTexture(ctx.cmd, data.maskTex, new Vector4(1, 1, 0, 0), data.compositeMaterial, 0);
                });
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(compositeMaterial);
        }
    }
}