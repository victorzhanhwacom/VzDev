using DG.Tweening;
using UnityEngine;

namespace VzDev.UnityAPI.Extensions
{
    public static class RendererExtension
    {
        #region 設定Emission顏色，需先自訂開啟Emissoin功能
        static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        static MaterialPropertyBlock _mpb;
        static MaterialPropertyBlock Mpb => _mpb ??= new MaterialPropertyBlock();

        /// <summary>與 Inspector HDR Color 的 Intensity 相同算法</summary>
        public static Color ToHDR(this Color c, float intensity)
        {
            float f = Mathf.Pow(2f, intensity);
            return new Color(c.r * f, c.g * f, c.b * f, c.a);
        }

        public static Color GetEmission(this Renderer r, int materialIndex = -1)
        {
            if (materialIndex < 0) r.GetPropertyBlock(Mpb);
            else r.GetPropertyBlock(Mpb, materialIndex);
            return Mpb.GetColor(EmissionColorID);
        }

        public static void SetEmission(this Renderer r, Color hdrColor, int materialIndex = -1)
        {
            if (materialIndex < 0)
            {
                r.GetPropertyBlock(Mpb);
                Mpb.SetColor(EmissionColorID, hdrColor);
                r.SetPropertyBlock(Mpb);
            }
            else
            {
                r.GetPropertyBlock(Mpb, materialIndex);
                Mpb.SetColor(EmissionColorID, hdrColor);
                r.SetPropertyBlock(Mpb, materialIndex);
            }
        }

        /// <summary>Tween Emission 顏色（Target = renderer，可用 renderer.DOKill() 停止）</summary>
        public static Tweener DOEmission(this Renderer r, Color hdrTarget, float duration, int materialIndex = -1)
        {
            return DOTween.To(
                    () => r.GetEmission(materialIndex),
                    c => r.SetEmission(c, materialIndex),
                    hdrTarget, duration)
                .SetTarget(r);
        }
        #endregion
    }
}