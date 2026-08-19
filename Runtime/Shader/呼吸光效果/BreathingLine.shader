Shader "Custom/URP/BreathingLine"
{
    Properties
    {
        [HDR] _Color ("HDR Color (基礎顏色)", Color) = (1, 1, 1, 1)
        _MinIntensity ("最小亮度", Range(0, 10)) = 0.5
        _MaxIntensity ("最大亮度 (超過1會被Bloom吃到)", Range(0, 20)) = 4.0
        _PulseSpeed ("呼吸速度", Range(0.1, 10)) = 1.5
        _PulseSharpness ("呼吸曲線銳利度 (越大越像脈衝，越小越像正弦波)", Range(0.5, 8)) = 1.0

        _EdgeSoftness ("邊緣柔化 (沿線寬V方向)", Range(0.001, 1)) = 0.3
        _FlowSpeed ("流動速度 (沿線長U方向, 0=不流動)", Range(-10, 10)) = 0.0
        _FlowTiling ("流動紋理密度", Range(0.1, 20)) = 3.0

        [Toggle] _UseVertexColor ("使用 LineRenderer 頂點色相乘", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        // Additive blend讓多條線重疊時會自然疊加變亮，很適合發光效果
        Blend SrcAlpha One

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            // WebGL 建議關閉不必要的 multi_compile 以減少 shader variant 編譯量
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                float  fogCoord    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _MinIntensity;
                float _MaxIntensity;
                float _PulseSpeed;
                float _PulseSharpness;
                float _EdgeSoftness;
                float _FlowSpeed;
                float _FlowTiling;
                float _UseVertexColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = positionInputs.positionCS;
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.fogCoord = ComputeFogFactor(positionInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ---- 呼吸強度 (0~1之間先算基礎波形，再做銳利度調整) ----
                float t = _Time.y * _PulseSpeed;
                float wave = sin(t) * 0.5 + 0.5; // 0~1
                wave = pow(wave, _PulseSharpness);
                float intensity = lerp(_MinIntensity, _MaxIntensity, wave);

                // ---- 沿線長方向的流動效果 (選用) ----
                float flowOffset = _Time.y * _FlowSpeed;
                float flow = frac(IN.uv.x * _FlowTiling - flowOffset);
                float flowGlow = smoothstep(0.0, 0.5, flow) * smoothstep(1.0, 0.5, flow);
                // 當 _FlowSpeed 為 0 時，flowGlow 保持穩定的柔和輪廓，不會消失
                flowGlow = lerp(1.0, flowGlow, saturate(abs(_FlowSpeed) > 0.0001 ? 1.0 : 0.0));

                // ---- 沿線寬方向 (V, 0~1) 的邊緣柔化，讓線條中心亮、邊緣透明漸層 ----
                float edge = abs(IN.uv.y - 0.5) * 2.0; // 0=中心, 1=邊緣
                float edgeFade = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, edge);

                half4 baseColor = _Color;
                if (_UseVertexColor > 0.5)
                {
                    baseColor *= IN.color;
                }

                half3 finalRGB = baseColor.rgb * intensity * flowGlow;
                half finalAlpha = baseColor.a * edgeFade;

                half4 col = half4(finalRGB, finalAlpha);
                col.rgb = MixFog(col.rgb, IN.fogCoord);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
