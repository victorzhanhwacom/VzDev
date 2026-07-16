Shader "VzDev/StagingWireframe"
{
    Properties
    {
        [ColorUsage(true,true)] _FillColor("Fill Color", Color) = (1, 0.4, 0.1, 0.25)
        [ColorUsage(true,true)] _EdgeColor("Edge Color", Color) = (1, 0.55, 0.15, 1)
        _EdgeThickness("Edge Thickness", Range(0.5, 4)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "StagingWireframePass"
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv2 : TEXCOORD1; // 重心座標的 (u, v) 兩軸，烘在 UV2
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            half4 _FillColor;
            half4 _EdgeColor;
            float _EdgeThickness;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 第三軸用 1-u-v 推回，跟前兩軸合起來就是完整重心座標 (u, v, w)，u+v+w = 1
                OUT.barycentric = float3(IN.uv2.x, IN.uv2.y, 1 - IN.uv2.x - IN.uv2.y);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // fwidth 抓每個重心分量在螢幕空間的變化率，越靠近三角面邊界（該分量趨近 0）
                // 且變化率夠大時，判定為邊線
                float3 d = fwidth(IN.barycentric);
                float3 edgeFactor = smoothstep(0, d * _EdgeThickness, IN.barycentric);
                float minEdge = min(edgeFactor.x, min(edgeFactor.y, edgeFactor.z));

                // minEdge 在三角面邊界處趨近 0，內部趨近 1
                float lineAlpha = 1 - minEdge;

                half4 col = lerp(_FillColor, _EdgeColor, lineAlpha);
                col.a = max(_FillColor.a, lineAlpha * _EdgeColor.a);
                return col;
            }
            ENDHLSL
        }
    }
}