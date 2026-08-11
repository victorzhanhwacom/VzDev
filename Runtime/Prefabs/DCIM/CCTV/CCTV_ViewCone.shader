Shader "VzDev/CCTV View Cone"
{
    Properties
    {
        [MainColor]
        _BaseColor ("Base Color", Color) =
            (0.05, 0.85, 1.0, 1.0)

        _EdgeColor ("Edge Color", Color) =
            (0.1, 1.0, 1.0, 1.0)

        _Alpha ("Base Alpha", Range(0, 1)) =
            0.055

        _EdgeStrength ("Edge Strength", Range(0, 5)) =
            2.5

        _EdgePower ("Edge Power", Range(0.1, 8)) =
            2.0


        // -----------------------------
        // Scan
        // -----------------------------

        _ScanColor ("Scan Color", Color) =
            (0.2, 1.0, 1.0, 1.0)

        _ScanSpeed ("Scan Speed", Range(0, 5)) =
            0.65

        _ScanWidth ("Scan Width", Range(0.01, 1)) =
            0.08

        _ScanStrength ("Scan Strength", Range(0, 5)) =
            2.0

        _ScanCount ("Scan Count", Range(1, 5)) =
            2


        // -----------------------------
        // Pulse
        // -----------------------------

        _PulseSpeed ("Pulse Speed", Range(0, 5)) =
            1.2

        _PulseStrength ("Pulse Strength", Range(0, 1)) =
            0.15


        // -----------------------------
        // Distance
        // -----------------------------

        _Range ("View Range", Float) =
            15.0

        _DistanceFade ("Distance Fade", Range(0.1, 5)) =
            1.5


        // -----------------------------
        // Center Falloff
        // -----------------------------

        _CenterStrength ("Center Strength", Range(0, 1)) =
            0.35
    }


    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }


        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off

        Cull Off


        Pass
        {
            Name "CCTV View Cone"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 positionOS : TEXCOORD1;

                float3 normalWS : TEXCOORD2;

                float3 viewDirWS : TEXCOORD3;
            };


            CBUFFER_START(UnityPerMaterial)

            float4 _BaseColor;
            float4 _EdgeColor;
            float4 _ScanColor;

            float _Alpha;

            float _EdgeStrength;
            float _EdgePower;

            float _ScanSpeed;
            float _ScanWidth;
            float _ScanStrength;
            float _ScanCount;

            float _PulseSpeed;
            float _PulseStrength;

            float _Range;
            float _DistanceFade;

            float _CenterStrength;

            CBUFFER_END


            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS =
                    positionInputs.positionCS;

                output.positionWS =
                    positionInputs.positionWS;

                output.positionOS =
                    input.positionOS.xyz;

                output.normalWS =
                    normalize(normalInputs.normalWS);

                output.viewDirWS =
                    GetWorldSpaceViewDir(
                        positionInputs.positionWS
                    );

                return output;
            }


            half4 frag(Varyings input) : SV_Target
            {
                // =====================================================
                // 基本資料
                // =====================================================

                float3 normalWS =
                    normalize(input.normalWS);

                float3 viewDir =
                    normalize(input.viewDirWS);


                // =====================================================
                // 1. Fresnel Edge
                // =====================================================

                float fresnel =
                    1.0 -
                    saturate(
                        abs(dot(normalWS, viewDir))
                    );

                fresnel =
                    pow(fresnel, _EdgePower);


                // =====================================================
                // 2. CCTV 距離
                // =====================================================

                float distance01 =
                    saturate(
                        input.positionOS.z /
                        max(_Range, 0.001)
                    );


                // 從 CCTV 往遠端逐漸變淡
                float distanceFade =
                    1.0 -
                    smoothstep(
                        0.55,
                        1.0,
                        distance01
                    );


                // =====================================================
                // 3. CCTV 掃描波
                //
                // 從攝影機 → 遠端移動
                // =====================================================

                float scanPosition =
                    frac(
                        _Time.y *
                        _ScanSpeed
                    );


                float scan =
                    distance01 -
                    scanPosition;


                // 讓 scan 落在 -1 ~ 1
                scan =
                    abs(
                        frac(scan) - 0.5
                    ) * 2.0;


                // Sharp luminous band
                float scanBand =
                    1.0 -
                    smoothstep(
                        0.0,
                        _ScanWidth,
                        scan
                    );


                // =====================================================
                // 4. 第二層掃描波
                //
                // 避免看起來只是單純一條線
                // =====================================================

                float scan2Position =
                    frac(
                        _Time.y *
                        _ScanSpeed *
                        0.45
                        + 0.35
                    );


                float scan2 =
                    distance01 -
                    scan2Position;


                scan2 =
                    abs(
                        frac(scan2) - 0.5
                    ) * 2.0;


                float scanBand2 =
                    1.0 -
                    smoothstep(
                        0.0,
                        _ScanWidth * 2.5,
                        scan2
                    );


                // =====================================================
                // 5. 柔和掃描光暈
                // =====================================================

                float scanGlow =
                    scanBand *
                    0.8
                    +
                    scanBand2 *
                    0.35;


                // =====================================================
                // 6. 中央區域比較淡
                //
                // 讓 View Cone 不會像一塊玻璃
                // =====================================================

                float centerFade =
                    1.0 -
                    fresnel;

                centerFade =
                    lerp(
                        1.0,
                        centerFade,
                        _CenterStrength
                    );


                // =====================================================
                // 7. 呼吸燈
                // =====================================================

                float pulse =
                    sin(
                        _Time.y *
                        _PulseSpeed
                    ) *
                    0.5
                    + 0.5;


                pulse =
                    lerp(
                        1.0,
                        pulse,
                        _PulseStrength
                    );


                // =====================================================
                // 8. 最終顏色
                // =====================================================

                float3 color =
                    _BaseColor.rgb;


                // Edge Glow
                color +=
                    _EdgeColor.rgb *
                    fresnel *
                    _EdgeStrength;


                // Scan Light
                color +=
                    _ScanColor.rgb *
                    scanGlow *
                    _ScanStrength;


                // Pulse
                color *= pulse;


                // =====================================================
                // 9. 最終 Alpha
                // =====================================================

                float alpha =
                    _Alpha;


                // 基礎距離衰減
                alpha *=
                    distanceFade;


                // 中央區域淡化
                alpha *=
                    centerFade;


                // Edge
                alpha +=
                    fresnel *
                    _Alpha *
                    _EdgeStrength;


                // Scan
                alpha +=
                    scanGlow *
                    _Alpha *
                    _ScanStrength;


                return half4(
                    color,
                    saturate(alpha)
                );
            }

            ENDHLSL
        }
    }
}