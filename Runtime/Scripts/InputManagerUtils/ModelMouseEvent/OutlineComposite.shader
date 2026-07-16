Shader "Hidden/VzDev/OutlineComposite"
{
    Properties { _MaskTex("Mask", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
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
            half4  _OutlineColor;
            float  _Thickness;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv    = input.texcoord;
                float2 texel = _MaskTex_TexelSize.xy * _Thickness;

                float center = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
                float sum = 0;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2( texel.x, 0)).r;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(-texel.x, 0)).r;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(0,  texel.y)).r;
                sum += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv + float2(0, -texel.y)).r;

                // 中心與四周鄰域差異越大代表越接近邊界（4 個鄰域全同 = 內部或外部，差值為0）
                float edge = saturate(abs(sum - 4.0 * center));

                half4 col = _OutlineColor;
                col.a *= edge;
                return col;
            }
            ENDHLSL
        }
    }
}