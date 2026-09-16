Shader "Guiri/WindWakerWater"
{
    Properties
    {
        [Header(Colores)]
        _ColorShallow ("Azul claro",  Color) = (0.30, 0.78, 0.85, 1)
        _ColorDeep    ("Azul oscuro", Color) = (0.05, 0.38, 0.65, 1)
        _FoamColor    ("Espuma",      Color) = (0.97, 1.00, 1.00, 1)

        [Header(Manchas de color)]
        _PatchScale ("Escala de las manchas",   Range(0.005, 0.2)) = 0.04
        _PatchDrift ("Deriva de las manchas",   Range(0, 0.5))     = 0.06

        [Header(Espuma)]
        _FoamScale  ("Escala de la espuma",     Range(0.01, 1.0))  = 0.12
        _FoamWidth  ("Grosor de las lineas",    Range(0.01, 0.2))  = 0.055
        _FoamSpeed  ("Velocidad de la espuma",  Range(0, 1))       = 0.18
        _FoamWobble ("Ondulacion de las lineas",Range(0, 1))       = 0.45

        [Header(Destellos)]
        _Sparkle      ("Cantidad de destellos", Range(0, 1))       = 0.5
        _SparkleScale ("Escala de destellos",   Range(0.1, 3))     = 0.8

        [Header(Oleaje en vertices)]
        _WaveHeight ("Altura de las olas",      Range(0, 1))       = 0.12
        _WaveFreq   ("Frecuencia de las olas",  Range(0, 2))       = 0.35
        _WaveSpeed  ("Velocidad de las olas",   Range(0, 4))       = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorShallow, _ColorDeep, _FoamColor;
            float  _PatchScale, _PatchDrift;
            float  _FoamScale, _FoamWidth, _FoamSpeed, _FoamWobble;
            float  _Sparkle, _SparkleScale;
            float  _WaveHeight, _WaveFreq, _WaveSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            // ---------- ruido procedural ----------
            float hash21 (float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise (float2 p)
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

            float fbm (float2 p)
            {
                return 0.55 * vnoise(p)
                     + 0.30 * vnoise(p * 2.3)
                     + 0.15 * vnoise(p * 4.9);
            }

            // Línea de espuma: banda estrecha alrededor del nivel 0.5 del
            // ruido → líneas cerradas y serpenteantes, con borde DURO (toon).
            float foamLine (float2 uv, float width)
            {
                float n = fbm(uv);
                float d = abs(n - 0.5);
                return smoothstep(width, width * 0.55, d);
            }
            // --------------------------------------

            v2f vert (appdata v)
            {
                v2f o;

                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Oleaje suave: dos senos cruzados sobre XZ de mundo.
                float t = _Time.y * _WaveSpeed;
                wp.y += sin(wp.x * _WaveFreq + t)
                      * cos(wp.z * _WaveFreq * 0.8 + t * 1.27)
                      * _WaveHeight;

                o.worldPos = wp;
                o.pos = UnityWorldToClipPos(wp);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.worldPos.xz;
                float  t  = _Time.y;

                // --- Manchas grandes de color que derivan lentamente ---
                float patch = vnoise(uv * _PatchScale + t * _PatchDrift);
                fixed3 base = lerp(_ColorDeep.rgb, _ColorShallow.rgb,
                                   smoothstep(0.32, 0.68, patch));

                // --- Espuma: coordenadas onduladas para que las líneas
                //     "respiren" en vez de solo desplazarse ---
                float2 wobble = (vnoise(uv * _FoamScale * 0.7 + t * 0.05) - 0.5)
                              * _FoamWobble * 4.0;

                float2 uvFoamA = uv * _FoamScale + wobble
                               + t * _FoamSpeed * float2( 0.7,  0.4);
                float2 uvFoamB = uv * _FoamScale * 0.62 - wobble * 0.5
                               + t * _FoamSpeed * float2(-0.4,  0.6);

                float foam = foamLine(uvFoamA, _FoamWidth);
                foam = max(foam, foamLine(uvFoamB, _FoamWidth * 1.25) * 0.75);

                // --- Destellos de sol: puntitos blancos fugaces ---
                float sp = vnoise(uv * _SparkleScale + t * 0.35);
                float sparkle = smoothstep(0.96, 0.99, sp) * _Sparkle;

                fixed3 col = lerp(base, _FoamColor.rgb, saturate(foam + sparkle));
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
