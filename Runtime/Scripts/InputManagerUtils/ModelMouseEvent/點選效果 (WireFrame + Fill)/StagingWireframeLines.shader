Shader "VzDev/StagingWireframeLines"
{
    Properties
    {
        [ColorUsage(true,true)] _EdgeColor("Edge Color", Color) = (1, 0.55, 0.15, 1)
        _EdgeThickness("Edge Thickness", Range(0.5, 4)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "WireframeLines"
            Tags { "LightMode"="UniversalForward" }
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
                float3 d = fwidth(IN.barycentric);
                float3 edgeFactor = smoothstep(0, d * _EdgeThickness, IN.barycentric);
                float minEdge = min(edgeFactor.x, min(edgeFactor.y, edgeFactor.z));
                float lineAlpha = 1 - minEdge;

                half4 col = _EdgeColor;
                col.a *= lineAlpha;
                return col;
            }
            ENDHLSL
        }
    }
}