Shader "Guiri/WaterWindWaker"
{
    Properties
    {
        [Header(Color por profundidad)]
        _ShallowColor ("Color en la orilla", Color)      = (0.45, 0.85, 0.88, 0.55)
        _DeepColor    ("Color en lo hondo", Color)       = (0.04, 0.32, 0.62, 0.95)
        _DepthFade    ("Profundidad del degradado", Range(0.1, 20)) = 4.0
        _ClarityFade  ("Distancia hasta opacarse", Range(0.1, 20))  = 2.5

        [Header(Bandas de color (Wind Waker))]
        [Toggle(_TOONBANDS_ON)] _UseToonBands ("Usar bandas de color", Float) = 1
        _ToonBands    ("Cuántas bandas", Range(2, 8)) = 3
        _BandSoftness ("Suavidad entre bandas", Range(0.001, 0.3)) = 0.04

        [Header(Celdas de la superficie)]
        [Toggle(_CELLS_ON)] _UseCells ("Usar celdas", Float) = 1
        _CellColor    ("Color de las celdas", Color) = (1, 1, 1, 1)
        _CellScale    ("Tamaño de las celdas", Range(0.05, 3)) = 0.35
        _CellWidth    ("Grosor del trazo", Range(0.01, 0.6)) = 0.12
        _CellStrength ("Cuánto se ven", Range(0, 1)) = 0.35
        _CellSpeed    ("Velocidad", Range(0, 2)) = 0.25
        _CellDeepFade ("Se apagan en lo hondo", Range(0, 1)) = 0.5

        [Header(Olas)]
        _WaveA ("Ola A (dirX, dirZ, largo, alto)", Vector) = (1, 0.35, 6.0, 0.10)
        _WaveB ("Ola B (dirX, dirZ, largo, alto)", Vector) = (-0.6, 1, 3.4, 0.06)
        _WaveC ("Ola C (dirX, dirZ, largo, alto)", Vector) = (0.5, -0.8, 1.7, 0.03)
        _WaveSpeed ("Velocidad de las olas", Range(0, 3)) = 0.6

        [Header(Espuma de orilla)]
        _FoamColor     ("Color de la espuma", Color) = (1, 1, 1, 1)
        _FoamDistance  ("Anchura de la franja", Range(0.05, 6)) = 1.2
        _FoamIntensity ("Cuánto se ve", Range(0, 1)) = 0.45
        _FoamSoftness  ("Suavidad del borde", Range(0.01, 1)) = 0.45
        _FoamNoiseScale("Detalle de la espuma", Range(0.5, 30)) = 8.0

        [Header(Vaiven de la orilla)]
        _ShoreWaveSpeed  ("Velocidad del vaivén", Range(0, 3)) = 0.45
        _ShoreWaveAmount ("Cuánto avanza y retrocede", Range(0, 1)) = 0.5
        _ShoreWaveBands  ("Cuántas líneas de ola", Range(0.5, 8)) = 2.0

        [Header(Brillos)]
        _SpecColor2   ("Color del brillo", Color) = (1, 1, 1, 1)
        _Smoothness   ("Dureza del brillo", Range(0, 1)) = 0.85
        _SpecStrength ("Fuerza del brillo", Range(0, 2)) = 0.6
        _NormalStrength ("Relieve de las olas", Range(0, 3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _TOONBANDS_ON
            #pragma shader_feature_local_fragment _CELLS_ON
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor, _FoamColor, _SpecColor2, _CellColor;
                float4 _WaveA, _WaveB, _WaveC;
                float  _DepthFade, _ClarityFade, _WaveSpeed;
                float  _FoamDistance, _FoamIntensity, _FoamSoftness, _FoamNoiseScale;
                float  _ShoreWaveSpeed, _ShoreWaveAmount, _ShoreWaveBands;
                float  _Smoothness, _SpecStrength, _NormalStrength;
                float  _ToonBands, _BandSoftness;
                float  _CellScale, _CellWidth, _CellStrength, _CellSpeed, _CellDeepFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                float  fogCoord    : TEXCOORD3;
            };

            // ---------- Ruido ----------
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                float3 a = frac(p.xyx * float3(123.34, 234.34, 345.65));
                a += dot(a, a + 34.45);
                return frac(float2(a.x * a.y, a.y * a.z));
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float foamNoise(float2 p, float t)
            {
                float n = valueNoise(p + float2(t * 0.35, t * 0.18)) * 0.65;
                n += valueNoise(p * 2.7 - float2(t * 0.22, t * 0.31)) * 0.35;
                return n;
            }

            // ---------- Celdas ----------
            // Voronoi guardando las DOS distancias más cercanas. El borde de
            // celda es F2-F1: vale casi 0 justo en la frontera entre dos celdas
            // y crece hacia dentro, así que da un trazo limpio y de grosor
            // constante. Pintar el borde a partir de F1 solo daría manchas
            // redondas, que es lo que hacía el shader viejo.
            float CellEdges(float2 uv, float t)
            {
                float2 cell = floor(uv);
                float2 f = frac(uv);

                float f1 = 8.0, f2 = 8.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 punto = hash22(cell + offset);

                        // Cada punto se mueve en su propio círculo: el patrón
                        // ondula sin desplazarse en bloque, que es lo que hace
                        // que parezca agua y no una tela arrastrada.
                        punto = 0.5 + 0.45 * sin(t + 6.2831 * punto);

                        float d = length(offset + punto - f);

                        if (d < f1) { f2 = f1; f1 = d; }
                        else if (d < f2) { f2 = d; }
                    }
                }

                float borde = f2 - f1;
                return 1.0 - smoothstep(0.0, max(_CellWidth, 0.001), borde);
            }

            // ---------- Olas ----------
            void Wave(float4 w, float2 pos, float t, inout float height, inout float2 slope)
            {
                float2 dir = normalize(w.xy + 1e-5);
                float  lambda = max(w.z, 0.01);
                float  amp = w.w;

                float k = 6.2831853 / lambda;
                float phase = dot(dir, pos) * k + t * _WaveSpeed * k;

                height += sin(phase) * amp;
                slope  += dir * cos(phase) * amp * k;
            }

            void SampleWaves(float2 pos, float t, out float height, out float3 normal)
            {
                height = 0;
                float2 slope = 0;

                Wave(_WaveA, pos, t, height, slope);
                Wave(_WaveB, pos, t, height, slope);
                Wave(_WaveC, pos, t, height, slope);

                slope *= _NormalStrength;
                normal = normalize(float3(-slope.x, 1.0, -slope.y));
            }

            // ---------- Bandas ----------
            // Escalona un valor continuo en N pasos, pero con el borde suavizado
            // para que no se vea aliasing en los cortes.
            float Posterize(float v, float bandas, float suavidad)
            {
                float escala = max(bandas, 1.0);
                float escalado = v * escala;
                float paso = floor(escalado);
                float resto = escalado - paso;

                return (paso + smoothstep(0.5 - suavidad * escala, 0.5 + suavidad * escala, resto)) / escala;
            }

            // ---------- Vértice ----------
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                float h;
                float3 n;
                SampleWaves(positionWS.xz, _Time.y, h, n);

                positionWS.y += h;

                OUT.positionWS = positionWS;
                OUT.normalWS   = n;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                OUT.fogCoord   = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            // ---------- Fragmento ----------
            half4 frag (Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterEye = IN.positionCS.w;
                float waterDepth = max(sceneEye - waterEye, 0);

                // --- Color por profundidad ---
                float depthT = saturate(waterDepth / _DepthFade);

                #ifdef _TOONBANDS_ON
                    // Aquí está media alma del look Wind Waker: el degradado del
                    // agua no es continuo, son franjas planas de color.
                    depthT = Posterize(depthT, _ToonBands, _BandSoftness);
                #endif

                half4 col = lerp(_ShallowColor, _DeepColor, depthT);

                float alpha = lerp(_ShallowColor.a, _DeepColor.a,
                                   saturate(waterDepth / _ClarityFade));

                // --- Celdas ---
                #ifdef _CELLS_ON
                {
                    float2 uv = IN.positionWS.xz * _CellScale;
                    float edges = CellEdges(uv, _Time.y * _CellSpeed);

                    // En lo hondo se apagan: si no, el mar entero parece una red
                    // y cansa la vista. En Wind Waker el detalle vive cerca de la
                    // costa y el fondo es color plano.
                    float cerca = 1.0 - saturate(waterDepth / _DepthFade) * _CellDeepFade;

                    col.rgb = lerp(col.rgb, _CellColor.rgb, edges * _CellStrength * cerca);
                }
                #endif

                // --- Espuma de orilla ---
                float shore = 1.0 - saturate(waterDepth / _FoamDistance);

                float vaiven = sin(waterDepth * _ShoreWaveBands * 3.1416
                                 - _Time.y * _ShoreWaveSpeed * 6.2831);
                shore += vaiven * _ShoreWaveAmount * 0.25 * shore;

                float n = foamNoise(IN.positionWS.xz * _FoamNoiseScale * 0.1, _Time.y);

                float foam = smoothstep(1.0 - _FoamSoftness, 1.0,
                                        saturate(shore) * (0.6 + n * 0.8));
                foam *= _FoamIntensity;

                col.rgb = lerp(col.rgb, _FoamColor.rgb, foam);
                alpha = saturate(alpha + foam * 0.8);

                // --- Luz ---
                float3 normalWS = normalize(IN.normalWS);
                Light main = GetMainLight();

                float ndotl = saturate(dot(normalWS, main.direction));
                float diffuse = lerp(0.75, 1.0, smoothstep(0.0, 0.6, ndotl));
                col.rgb *= main.color * diffuse;

                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 halfDir = normalize(main.direction + viewDir);
                float  spec = pow(saturate(dot(normalWS, halfDir)), exp2(_Smoothness * 10.0) + 1.0);
                col.rgb += _SpecColor2.rgb * spec * _SpecStrength;

                col.rgb = MixFog(col.rgb, IN.fogCoord);

                return half4(col.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
