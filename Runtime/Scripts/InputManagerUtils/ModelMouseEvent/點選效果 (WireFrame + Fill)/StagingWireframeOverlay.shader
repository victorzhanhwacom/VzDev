Shader "VzDev/StagingWireframeOverlay"
{
    Properties
    {
        [ColorUsage(true,true)] _TintColor("Tint Color (fill overlay)", Color) = (1, 0.4, 0.1, 0.15)
        [ColorUsage(true,true)] _EdgeColor("Edge Color", Color) = (1, 0.55, 0.15, 1)
        _EdgeThickness("Edge Thickness", Range(0.5, 4)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        // Pass 0：半透明色調覆蓋層
        Pass
        {
            Name "TintOverlay"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            half4 _TintColor;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return _TintColor;
            }
            ENDHLSL
        }

        // Pass 1：三角網格線層（除錯模式：直接輸出重心座標當顏色，確認 UV2 是否正確傳入）
        Pass
        {
            Name "WireframeLines"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv2 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            half4 _EdgeColor;
            float _EdgeThickness;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.barycentric = float3(IN.uv2.x, IN.uv2.y, 1 - IN.uv2.x - IN.uv2.y);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // ---- 除錯輸出：直接顯示重心座標本身 ----
                // 正常應該在每個三角面內部看到紅/綠/藍三色互相漸層混合。
                // 如果整片都是同一種顏色（尤其純黑或純藍），代表 UV2 沒有正確傳入。
                return half4(IN.barycentric, 1);
            }
            ENDHLSL
        }
    }
}