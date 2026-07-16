Shader "Hidden/VzDev/OutlineMask"
{
    Properties
    {
        _WriteMask("WriteMask", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OutlineMaskPass"
            ZTest Always
            ZWrite Off
            Cull Off
            ColorMask RG
            Blend One One    // 加法疊加：兩次寫入互不覆蓋，各通道獨立累加

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _WriteMask;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_WriteMask.xy, 0, 0);
            }
            ENDHLSL
        }
    }
}