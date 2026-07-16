Shader "VzDev/StagingBoundingBox"
{
    Properties
    {
        [ColorUsage(true,true)] _FillColor("Fill Color", Color) = (0.4, 0.9, 1, 1)
        [ColorUsage(true,true)] _EdgeColor("Edge Color", Color) = (0.6, 1, 1, 1)
        _EdgeThickness("Edge Thickness (UV space)", Range(0.001, 0.1)) = 0.015
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _FillMinAlpha("Fill Min Alpha (面對鏡頭時)", Range(0, 1)) = 0.03
        _FillMaxAlpha("Fill Max Alpha (掠射角時)", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "StagingBoundingBoxPass"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            half4 _FillColor;
            half4 _EdgeColor;
            float _EdgeThickness;
            float _FresnelPower;
            float _FillMinAlpha;
            float _FillMaxAlpha;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);
                float3 normal = normalize(IN.normalWS);

                // Fresnel：正對鏡頭時 dot 接近 1，掠射角時接近 0，
                // 用 1-dot 的次方讓邊緣（掠射角）快速變亮，中心（正面）保持接近透明
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower);
                float fillAlpha = lerp(_FillMinAlpha, _FillMaxAlpha, fresnel);

                // UV 邊界距離：每個面是獨立的 0~1 UV 四邊形，
                // 距離四個邊界中最近的那個，小於門檻就判定為外框線
                float distToEdge = min(min(IN.uv.x, 1 - IN.uv.x), min(IN.uv.y, 1 - IN.uv.y));
                float edgeMask = 1 - smoothstep(0, _EdgeThickness, distToEdge);

                half4 col = lerp(_FillColor, _EdgeColor, edgeMask);
                float alpha = max(fillAlpha, edgeMask * _EdgeColor.a);
                col.a = alpha;

                return col;
            }
            ENDHLSL
        }
    }
}