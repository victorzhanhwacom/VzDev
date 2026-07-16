using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

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
            public Color hoverColor = new Color(0.25f, 0.85f, 1f, 1f);
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
        private readonly Material maskMaterialSelected;
        private readonly Material maskMaterialHover;
        private readonly Material compositeMaterial;
        private readonly Color hoverColor;
        private readonly Color selectedColor;
        private readonly float thickness;

        private class MaskPassData
        {
            public IReadOnlyCollection<Renderer> selected;
            public IReadOnlyCollection<Renderer> hover;
            public Material maskMaterialSelected;
            public Material maskMaterialHover;
        }

        private class CompositePassData
        {
            public TextureHandle maskTex;
            public Material compositeMaterial;
            public Color hoverColor;
            public Color selectedColor;
            public float thickness;
        }

        public OutlinePass(OutlineRendererFeature.Settings settings)
        {
            maskMaterialSelected = CoreUtils.CreateEngineMaterial(settings.maskShader);
            maskMaterialSelected.SetVector("_WriteMask", new Vector4(1, 0, 0, 0)); // 固定寫 R，永不改變

            maskMaterialHover = CoreUtils.CreateEngineMaterial(settings.maskShader);
            maskMaterialHover.SetVector("_WriteMask", new Vector4(0, 1, 0, 0)); // 固定寫 G，永不改變

            compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);
            hoverColor = settings.hoverColor;
            selectedColor = settings.selectedColor;
            thickness = settings.thickness;

            // 告訴 URP 這個 Pass 需要場景法線與深度紋理，URP 會自動排入對應的 DepthNormals Pass
            ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!HighlightRegistry.HasAny()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var selected = HighlightRegistry.Get(HighlightGroup.Selected);
            var hover = HighlightRegistry.Get(HighlightGroup.Hover);

            TextureHandle maskHandle = renderGraph.CreateTexture(new TextureDesc(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = true,
                clearColor = Color.clear,
                name = "_HighlightMaskTex"
            });

            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Outline_Mask", out var passData))
            {
                passData.selected = selected;
                passData.hover = hover;
                passData.maskMaterialSelected = maskMaterialSelected;
                passData.maskMaterialHover = maskMaterialHover;

                builder.SetRenderAttachment(maskHandle, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((MaskPassData data, RasterGraphContext ctx) =>
                {
                    foreach (var r in data.selected)
                    {
                        if (r == null) continue;
                        ctx.cmd.DrawRenderer(r, data.maskMaterialSelected, 0, 0);
                    }
                    foreach (var r in data.hover)
                    {
                        if (r == null) continue;
                        ctx.cmd.DrawRenderer(r, data.maskMaterialHover, 0, 0);
                    }
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline_Composite", out var passData))
            {
                passData.maskTex = maskHandle;
                passData.compositeMaterial = compositeMaterial;
                passData.hoverColor = hoverColor;
                passData.selectedColor = selectedColor;
                passData.thickness = thickness;

                builder.UseTexture(maskHandle, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) =>
                {
                    data.compositeMaterial.SetTexture("_MaskTex", data.maskTex);
                    data.compositeMaterial.SetColor("_HoverColor", data.hoverColor);
                    data.compositeMaterial.SetColor("_SelectedColor", data.selectedColor);
                    data.compositeMaterial.SetFloat("_Thickness", data.thickness);
                    Blitter.BlitTexture(ctx.cmd, data.maskTex, new Vector4(1, 1, 0, 0), data.compositeMaterial, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline_Composite", out var passData))
            {
                passData.maskTex = maskHandle;
                passData.compositeMaterial = compositeMaterial;
                passData.hoverColor = hoverColor;
                passData.selectedColor = selectedColor;
                passData.thickness = thickness;

                builder.UseTexture(maskHandle, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) =>
                {
                    data.compositeMaterial.SetTexture("_MaskTex", data.maskTex);
                    data.compositeMaterial.SetColor("_HoverColor", data.hoverColor);
                    data.compositeMaterial.SetColor("_SelectedColor", data.selectedColor);
                    data.compositeMaterial.SetFloat("_Thickness", data.thickness);
                    Blitter.BlitTexture(ctx.cmd, data.maskTex, new Vector4(1, 1, 0, 0), data.compositeMaterial, 0);
                });
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(maskMaterialSelected);
            CoreUtils.Destroy(maskMaterialHover);
            CoreUtils.Destroy(compositeMaterial);
        }
    }
}