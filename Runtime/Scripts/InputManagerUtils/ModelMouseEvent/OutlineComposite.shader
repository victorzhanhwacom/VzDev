Shader "Hidden/VzDev/OutlineComposite"
{
    Properties
    {
        _MaskTex("Mask", 2D) = "black" {}
        _HoverColor("Hover Color", Color) = (0.25, 0.85, 1, 1)
        _SelectedColor("Selected Color", Color) = (1, 0.65, 0, 1)
        _Thickness("Thickness", Range(1, 8)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OutlineCompositePass"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            float4 _MaskTex_TexelSize;
            half4  _HoverColor;
            half4  _SelectedColor;
            float  _Thickness;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv    = input.texcoord;
                float2 texel = _MaskTex_TexelSize.xy * _Thickness;

                float2 center = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).rg;
                float2 sum = 0;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( texel.x, 0)).rg;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-texel.x, 0)).rg;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(0,  texel.y)).rg;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(0, -texel.y)).rg;

                // edge.x = Selected 邊緣強度, edge.y = Hover 邊緣強度
                float2 edge = saturate(abs(sum - 4.0 * center));

                // 判斷該像素是否落在「Selected 物件的內部或邊緣」範圍內
                // center.x > 0.5：像素本身在 Selected 物件內部（非邊緣也算）
                // edge.x > 0.01：像素在 Selected 物件的邊緣上
                bool isSelectedRegion = (center.x > 0.5) || (edge.x > 0.01);

                half4 col;
                float alpha;

                if (isSelectedRegion)
                {
                    col = _SelectedColor;
                    alpha = edge.x;
                }
                else
                {
                    col = _HoverColor;
                    alpha = edge.y;
                }

                col.a *= alpha;
                return col;
            }
            ENDHLSL
        }
    }
}