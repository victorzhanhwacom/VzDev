Shader "Custom/HeatmapVolume"
{
    Properties
    {
        _StepCount     ("Ray Steps",       Range(16, 128)) = 64
        _StepSize      ("Step Size",       Range(0.001, 0.1)) = 0.02
        _Density       ("Density",         Range(0.0, 5.0)) = 1.0
        _AlphaThreshold("Alpha Threshold", Range(0.0, 1.0)) = 0.01

        _TempColor0 ("Temp Color 0", Color) = (0,0,1,1)
        _TempColor1 ("Temp Color 1", Color) = (0,1,1,1)
        _TempColor2 ("Temp Color 2", Color) = (0,1,0,1)
        _TempColor3 ("Temp Color 3", Color) = (1,1,0,1)
        _TempColor4 ("Temp Color 4", Color) = (1,0.5,0,1)
        _TempColor5 ("Temp Color 5", Color) = (1,0,0,1)
        _TempColor6 ("Temp Color 6", Color) = (1,0,1,1)
        _TempColor7 ("Temp Color 7", Color) = (1,1,1,1)

        _TempStop0 ("Temp Stop 0", Range(0,1)) = 0.0
        _TempStop1 ("Temp Stop 1", Range(0,1)) = 0.15
        _TempStop2 ("Temp Stop 2", Range(0,1)) = 0.3
        _TempStop3 ("Temp Stop 3", Range(0,1)) = 0.45
        _TempStop4 ("Temp Stop 4", Range(0,1)) = 0.6
        _TempStop5 ("Temp Stop 5", Range(0,1)) = 0.75
        _TempStop6 ("Temp Stop 6", Range(0,1)) = 0.88
        _TempStop7 ("Temp Stop 7", Range(0,1)) = 1.0

        _ActiveStops ("Active Stop Count", Range(2, 8)) = 6

        _TempMin ("Min Temperature", Float) = 0.0
        _TempMax ("Max Temperature", Float) = 100.0

        [Header(Edge Flow)]
        _FlowTime      ("Flow Time (set by C#)", Float) = 0.0
        _FlowSpeed     ("Flow Speed",      Range(0.0, 5.0)) = 0.6
        _FlowStrength  ("Flow Strength",   Range(0.0, 2.0)) = 0.35
        _FlowScale     ("Flow Scale",      Range(0.1, 8.0)) = 2.0
        _FlowOctaves   ("Flow Octaves",    Range(1,   4  )) = 3
        _EdgeBand      ("Edge Band Width", Range(0.0, 1.0)) = 0.4
        _FlowDirection ("Flow Direction",  Vector) = (0.2, 1.0, 0.1, 0.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "HeatmapVolumePass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ----------------------------------------------------------------
            // Constant Buffers
            // ----------------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _RaymarchPack;   // x=StepCount  y=Density  z=AlphaThreshold  w=unused
                float4 _TempRangePack;  // x=TempMin  y=TempMax  z=ActiveStops  w=unused
                float4 _FlowPack;       // x=unused  y=FlowSpeed  z=FlowStrength  w=FlowScale
                float4 _FlowPack2;      // x=FlowOctaves  y=EdgeBand  zw=unused
                float4 _FlowDirection;
                float4 _TempColor0, _TempColor1, _TempColor2, _TempColor3;
                float4 _TempColor4, _TempColor5, _TempColor6, _TempColor7;
                float4 _TempStopPack0;
                float4 _TempStopPack1;
                float4 _BaseTempPack;   // x=BaseTemp  y=BaseDensityScale
            CBUFFER_END

            #define _StepCount       _RaymarchPack.x
            #define _Density         _RaymarchPack.y
            #define _AlphaThreshold  _RaymarchPack.z

            #define _TempMin         _TempRangePack.x
            #define _TempMax         _TempRangePack.y
            #define _ActiveStops     _TempRangePack.z

            #define _FlowTime        _Time.y
            #define _FlowSpeed       _FlowPack.y
            #define _FlowStrength    _FlowPack.z
            #define _FlowScale       _FlowPack.w

            #define _FlowOctaves     _FlowPack2.x
            #define _EdgeBand        _FlowPack2.y

            #define _BaseTemp        _BaseTempPack.x
            #define _BaseDensityScale _BaseTempPack.y

            // ----------------------------------------------------------------
            // Heat Sources
            // ----------------------------------------------------------------
            #define MAX_HEAT_SOURCES 32
            CBUFFER_START(HeatSourceBuffer)
                float4 _HeatSourcePositions[MAX_HEAT_SOURCES];
                float4 _HeatSourceParams[MAX_HEAT_SOURCES];
                float4 _HeatSourceCountPack;
            CBUFFER_END
            #define _HeatSourceCount (int)_HeatSourceCountPack.x

            // ----------------------------------------------------------------
            // Structs
            // ----------------------------------------------------------------
            struct Attributes { float4 positionOS : POSITION; };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            // ----------------------------------------------------------------
            // Noise
            // ----------------------------------------------------------------
            float3 hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7,  74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            float vnoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float a  = dot(hash33(i               ), float3(1,0,0));
                float b  = dot(hash33(i + float3(1,0,0)), float3(1,0,0));
                float c  = dot(hash33(i + float3(0,1,0)), float3(1,0,0));
                float d  = dot(hash33(i + float3(1,1,0)), float3(1,0,0));
                float e  = dot(hash33(i + float3(0,0,1)), float3(1,0,0));
                float ff = dot(hash33(i + float3(1,0,1)), float3(1,0,0));
                float g  = dot(hash33(i + float3(0,1,1)), float3(1,0,0));
                float h  = dot(hash33(i + float3(1,1,1)), float3(1,0,0));
                return lerp(lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y),
                            lerp(lerp(e,ff,u.x),lerp(g,h,u.x),u.y),u.z);
            }

            float fbm(float3 p, int oct)
            {
                float v = 0.0, amp = 0.5, freq = 1.0;
                if (oct >= 1) { v += amp * vnoise(p * freq); amp *= 0.5; freq *= 2.1; }
                if (oct >= 2) { v += amp * vnoise(p * freq); amp *= 0.5; freq *= 2.1; }
                if (oct >= 3) { v += amp * vnoise(p * freq); amp *= 0.5; freq *= 2.1; }
                if (oct >= 4) { v += amp * vnoise(p * freq); }
                return v;
            }

            // ----------------------------------------------------------------
            // Curl Noise — 模擬不可壓縮流體渦旋
            //
            // 原理：對純量噪聲場取旋度 (curl = ∇ × F)，
            //       結果向量場的散度為零，雲體不會憑空膨脹或收縮。
            //       每個點的流向由鄰近噪聲梯度決定，天然形成渦旋，
            //       視覺效果就是真實煙霧 / 熱氣的捲動。
            //
            // 實作用有限差分近似偏微分，EPS 為差分步長。
            // ----------------------------------------------------------------
            static const float CURL_EPS = 0.1;

            float3 CurlNoise(float3 p, int oct)
            {
                // 六個方向採樣，分別算三個偏微分對
                float nx_py = fbm(p + float3( 0,      CURL_EPS, 0     ), oct);
                float nx_my = fbm(p + float3( 0,     -CURL_EPS, 0     ), oct);
                float nx_pz = fbm(p + float3( 0,      0,        CURL_EPS), oct);
                float nx_mz = fbm(p + float3( 0,      0,       -CURL_EPS), oct);

                float ny_px = fbm(p + float3( CURL_EPS, 0,      0     ), oct);
                float ny_mx = fbm(p + float3(-CURL_EPS, 0,      0     ), oct);
                float ny_pz = nx_pz;   // 共用，省一次採樣
                float ny_mz = nx_mz;

                float nz_px = ny_px;   // 共用
                float nz_mx = ny_mx;
                float nz_py = nx_py;   // 共用
                float nz_my = nx_my;

                // curl = ∇ × F，中央差分
                float cx = (nx_py - nx_my) - (nx_pz - nx_mz);
                float cy = (ny_pz - ny_mz) - (ny_px - ny_mx);
                float cz = (nz_px - nz_mx) - (nz_py - nz_my);

                return float3(cx, cy, cz) / (2.0 * CURL_EPS);
            }

            // ----------------------------------------------------------------
            // FluidWarp：每個點的流動偏移
            //   ① Curl noise 主渦流（每點各自捲動方向不同）
            //   ② 熱浮力（y 軸正方向隨位置 sin 呼吸，模擬熱對流）
            // ----------------------------------------------------------------
            float3 FluidWarp(float3 wsPos)
            {
                float t  = _FlowTime * _FlowSpeed;

                int oct = 1;
                if (_FlowOctaves >= 2.0) oct = 2;
                if (_FlowOctaves >= 3.0) oct = 3;
                if (_FlowOctaves >= 4.0) oct = 4;

                float3 p = wsPos * _FlowScale * 0.5;

                // 兩層 curl，p1 用不同相位偏移，頻率略高（細碎渦旋疊在大渦旋上）
                float3 p0 = p  + float3(0.0,        t * 0.07, 0.0);
                float3 p1 = p  + float3(t * 0.05,   0.0,      t * 0.03) + float3(31.7, 17.3, 5.1);

                float3 c0 = CurlNoise(p0, oct);
                float3 c1 = CurlNoise(p1 * 1.7, max(1, oct - 1)); // 高頻層用少一階 oct 省 GPU

                float3 curl = c0 * 0.7 + c1 * 0.3;

                // 熱浮力：y 軸 sin 呼吸，振幅隨水平位置微變化讓邊緣不整齊
                float buoy = sin(t * 0.31 + wsPos.x * 0.4 + wsPos.z * 0.3) * 0.35
                           + sin(t * 0.19 + wsPos.z * 0.5)                  * 0.15;
                curl.y += buoy;

                return curl * _FlowStrength * 0.35;
            }

            // ----------------------------------------------------------------
            // BreathNoise：密度呼吸，和 FluidWarp 獨立驅動
            //   用極低頻 FBM + 慢速漂移，讓雲體透明度緩慢起伏，
            //   不與形狀變化同步，避免機械感。
            // ----------------------------------------------------------------
            float BreathNoise(float3 wsPos)
            {
                float t  = _FlowTime * _FlowSpeed;
                // 極低頻採樣座標，緩慢 sin 偏移模擬呼吸
                float3 p = wsPos * 0.28
                         + float3(sin(t * 0.17) * 0.12,
                                  t * 0.04,
                                  cos(t * 0.13) * 0.12);
                float n      = fbm(p, 1);
                float detail = fbm(p * 0.5 + float3(7.3, 2.1, 4.9), 1);
                return saturate(n * 0.65 + detail * 0.35);
            }

            // ----------------------------------------------------------------
            // Heat helpers
            // ----------------------------------------------------------------
            float3 WorldToLocal(float3 ws)
            {
                return mul(unity_WorldToObject, float4(ws, 1.0)).xyz;
            }

            bool InsideBox(float3 p) { return all(abs(p) <= 0.5); }

            bool RayAABB(float3 ro, float3 rd, out float tmin, out float tmax)
            {
                float3 inv = 1.0 / rd;
                float3 t0  = (-0.5 - ro) * inv;
                float3 t1  = ( 0.5 - ro) * inv;
                float3 lo  = min(t0, t1);
                float3 hi  = max(t0, t1);
                tmin = max(max(lo.x, lo.y), lo.z);
                tmax = min(min(hi.x, hi.y), hi.z);
                return tmin < tmax && tmax > 0.0;
            }

            float SourceInfluence(int i, float3 wsPos)
            {
                float3 srcPos = _HeatSourcePositions[i].xyz;
                float  radius = _HeatSourceParams[i].x;
                float  fallof = max(_HeatSourceParams[i].y, 0.001);
                float  dist   = length(wsPos - srcPos);
                return pow(saturate(1.0 - dist / radius), fallof);
            }

            void SampleField(float3 wsPos, out float totalTemp, out float maxInf)
            {
                float weightedSum = 0.0;
                float totalWeight = 0.0;
                maxInf = 0.0;

                for (int i = 0; i < _HeatSourceCount && i < MAX_HEAT_SOURCES; i++)
                {
                    float inf = SourceInfluence(i, wsPos);
                    weightedSum += _HeatSourcePositions[i].w * inf;
                    totalWeight += inf;
                    maxInf       = max(maxInf, inf);
                }

                totalTemp = (totalWeight > 0.0001)
                    ? weightedSum / totalWeight
                    : _BaseTemp;
            }

            float NormalizeTemp(float t)
            {
                return saturate((t - _TempMin) / max(_TempMax - _TempMin, 0.001));
            }

            float4 TempToColor(float nt)
            {
                float  stops[8]  = { _TempStopPack0.x, _TempStopPack0.y,
                                     _TempStopPack0.z, _TempStopPack0.w,
                                     _TempStopPack1.x, _TempStopPack1.y,
                                     _TempStopPack1.z, _TempStopPack1.w };
                float4 colors[8] = { _TempColor0, _TempColor1, _TempColor2, _TempColor3,
                                     _TempColor4, _TempColor5, _TempColor6, _TempColor7 };
                int n = max(2, min((int)_ActiveStops, 8));
                if (nt <= stops[0]) return colors[0];
                for (int i = 1; i < n; i++)
                {
                    if (nt <= stops[i])
                    {
                        float t = (nt - stops[i-1]) / max(stops[i] - stops[i-1], 0.0001);
                        return lerp(colors[i-1], colors[i], t);
                    }
                }
                return colors[n-1];
            }

            // ----------------------------------------------------------------
            // Vertex
            // ----------------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // ----------------------------------------------------------------
            // Fragment
            // ----------------------------------------------------------------
            float4 frag(Varyings IN) : SV_Target
            {
                float3 camWS  = _WorldSpaceCameraPos;
                float3 fragWS = IN.positionWS;

                float3 roOS = WorldToLocal(camWS);
                float3 rdOS = normalize(WorldToLocal(fragWS) - roOS);

                float tmin, tmax;
                if (!RayAABB(roOS, rdOS, tmin, tmax)) discard;
                tmin = max(tmin, 0.0);

                float stepSz = (tmax - tmin) / _StepCount;
                float3 stepV = rdOS * stepSz;
                float3 curOS = roOS + rdOS * (tmin + stepSz * 0.5);

                float3 stepWS   = mul((float3x3)unity_ObjectToWorld, rdOS * stepSz);
                float  stepSzWS = length(stepWS);

                float4 accum = float4(0,0,0,0);

                for (int s = 0; s < (int)_StepCount; s++)
                {
                    if (!InsideBox(curOS)) { curOS += stepV; continue; }

                    float3 wsPos = mul(unity_ObjectToWorld, float4(curOS, 1.0)).xyz;

                    // ── ① 流體渦旋位移：curl noise 讓每個點各自往不同方向捲動 ──
                    //    warpedWS 是「氣體流動後的採樣座標」，
                    //    查溫度場用它，讓熱雲形狀本身跟著渦流扭曲。
                    float3 warpedWS = wsPos + FluidWarp(wsPos);

                    // ── ② 查溫度場 ────────────────────────────────────────────
                    float rawTemp, maxInf;
                    SampleField(warpedWS, rawTemp, maxInf);

                    float normTemp = NormalizeTemp(rawTemp);
                    float4 col     = TempToColor(normTemp);

                    // ── ③ 密度呼吸：邊緣透明度隨 BreathNoise 緩慢起伏 ────────
                    float eb       = max(_EdgeBand, 0.01);
                    float edgePeak = 1.0 - smoothstep(0.0, eb, maxInf);
                    float presence = smoothstep(0.0, 0.1, maxInf);

                    float breath    = BreathNoise(wsPos);
                    float breathMask = presence * (0.25 + 0.75 * edgePeak);
                    float driftMod   = 1.0 - _FlowStrength * 0.5 * (1.0 - breath) * breathMask;

                    // ── ④ Beer-Lambert 積分 ───────────────────────────────────
                    float sigmaBase = _BaseDensityScale * _Density;
                    float sigmaSrc  = maxInf * _Density * driftMod;
                    float sigma     = lerp(sigmaBase, sigmaSrc, saturate(maxInf));
                    float alpha     = 1.0 - exp(-sigma * stepSzWS);

                    accum.rgb += (1.0 - accum.a) * col.rgb * alpha;
                    accum.a   += (1.0 - accum.a) * alpha;

                    if (accum.a >= 0.99) break;
                    curOS += stepV;
                }

                if (accum.a < _AlphaThreshold) discard;
                return accum;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
