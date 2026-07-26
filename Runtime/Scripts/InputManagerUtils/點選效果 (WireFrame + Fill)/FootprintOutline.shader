Shader "VzDev/FootprintOutline"
{
    // 仿 Cities:Skylines 2 選取樣式的地面足跡輪廓。
    // 單一 Pass、Unlit，靠 Vertex Color 區分「描邊/圓點(亮)」與「填色(暗)」，
    // 搭配場景既有的 Bloom 後製產生發光感；Pulse 動畫完全在 Shader 內以 _Time 計算，
    // 不需要 C# 每幀更新 Material，WebGL 相容（單一 Pass，無 Geometry Shader）。
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.3, 0.85, 1, 1)
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TintColor;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Vertex Color: RGB = 亮度倍率(供Bloom用), A = 基礎透明度
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                float3 rgb = IN.color.rgb * _TintColor.rgb * pulse;
                float alpha = IN.color.a * _TintColor.a;

                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
