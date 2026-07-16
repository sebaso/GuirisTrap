// ============================================================================
//  Guiri/StylizedWater — fusión del Water_Style del artista + estilo Wind Waker
//
//  Combina las dos aguas del proyecto en un solo shader ultra-configurable:
//
//  DEL AGUA DEL ARTISTA (Water_Style.shadergraph):
//    · Espuma de ORILLA por profundidad de escena: espuma clara pegada al
//      borde donde el agua toca arena/rocas, y una banda oscura más ancha.
//      (Requiere "Depth Texture" activado en el URP Asset — el shader graph
//      original ya lo necesitaba, así que debería estar ya activo.)
//    · Capa de espuma por TEXTURA (su Water_Voronoi) desplazándose.
//
//  DEL ESTILO WIND WAKER:
//    · Líneas de espuma serpenteantes de borde duro por toda la superficie.
//    · Manchas grandes de dos azules que derivan.
//    · Destellos de sol.
//    · Oleaje en vértices.
//
//  Coordenadas de MUNDO (XZ): estira el plano lo que quieras sin deformar.
//  Transparente con opacidad configurable (la espuma siempre opaca).
//
//  SETUP:
//    1. Material nuevo con este shader.
//    2. En "Espuma (textura)": arrastrar la Water_Voronoi del artista.
//    3. Si la espuma de orilla no aparece: URP Asset → marcar "Depth Texture".
// ============================================================================
Shader "Guiri/StylizedWater"
{
    Properties
    {
        [Header(Colores del agua)]
        _ColorShallow ("Azul claro",  Color) = (0.30, 0.78, 0.85, 1)
        _ColorDeep    ("Azul oscuro", Color) = (0.05, 0.38, 0.65, 1)
        _Opacity      ("Opacidad del agua", Range(0, 1)) = 0.9

        [Header(Manchas de color (Wind Waker))]
        _PatchScale ("Escala de las manchas", Range(0.005, 0.2)) = 0.04
        _PatchDrift ("Deriva de las manchas", Range(0, 0.5))     = 0.06

        [Header(Espuma procedural (lineas Wind Waker))]
        _FoamColor  ("Color de la espuma",      Color)            = (0.97, 1.0, 1.0, 1)
        _FoamScale  ("Escala de la espuma",     Range(0.01, 1.0)) = 0.12
        _FoamWidth  ("Grosor de las lineas",    Range(0.01, 0.2)) = 0.055
        _FoamSpeed  ("Velocidad de la espuma",  Range(0, 1))      = 0.18
        _FoamWobble ("Ondulacion de las lineas",Range(0, 1))      = 0.45

        [Header(Espuma por textura (voronoi del artista))]
        _FoamTex          ("Textura de espuma", 2D)              = "black" {}
        _FoamTexTiling    ("Tiling",            Range(0.01, 2))  = 0.15
        _FoamTexSpeed     ("Velocidad (XY)",    Vector)          = (0.03, 0.02, 0, 0)
        _FoamTexCutoff    ("Umbral (recorte)",  Range(0, 1))     = 0.65
        _FoamTexIntensity ("Intensidad",        Range(0, 1))     = 0.5

        [Header(Espuma de orilla (profundidad del artista))]
        [Toggle] _UseShoreFoam ("Activar espuma de orilla", Float) = 1
        _LightFoamColor    ("Espuma clara (borde)",   Color)        = (1, 1, 1, 1)
        _LightFoamDistance ("Distancia espuma clara", Range(0, 3))  = 0.35
        _DarkFoamColor     ("Espuma oscura (banda)",  Color)        = (0.55, 0.85, 0.9, 1)
        _DarkFoamDistance  ("Distancia espuma oscura",Range(0, 6))  = 1.2
        _ShoreWobble       ("Orilla organica (usa la textura)", Range(0, 1)) = 0.5

        [Header(Destellos)]
        _Sparkle      ("Cantidad de destellos", Range(0, 1))   = 0.5
        _SparkleScale ("Escala de destellos",   Range(0.1, 3)) = 0.8

        [Header(Oleaje en vertices)]
        _WaveHeight ("Altura de las olas",     Range(0, 1)) = 0.12
        _WaveFreq   ("Frecuencia de las olas", Range(0, 2)) = 0.35
        _WaveSpeed  ("Velocidad de las olas",  Range(0, 4)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorShallow, _ColorDeep, _FoamColor;
            fixed4 _LightFoamColor, _DarkFoamColor;
            float  _Opacity;
            float  _PatchScale, _PatchDrift;
            float  _FoamScale, _FoamWidth, _FoamSpeed, _FoamWobble;
            sampler2D _FoamTex;
            float4 _FoamTexSpeed;
            float  _FoamTexTiling, _FoamTexCutoff, _FoamTexIntensity;
            float  _UseShoreFoam, _LightFoamDistance, _DarkFoamDistance, _ShoreWobble;
            float  _Sparkle, _SparkleScale;
            float  _WaveHeight, _WaveFreq, _WaveSpeed;

            // Textura de profundidad de la cámara (URP la publica globalmente
            // cuando "Depth Texture" está activado en el URP Asset).
            sampler2D _CameraDepthTexture;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float3 worldPos  : TEXCOORD0;
                float4 screenPos : TEXCOORD1; // para la espuma de orilla
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

                // Oleaje: dos senos cruzados sobre XZ de mundo.
                float t = _Time.y * _WaveSpeed;
                wp.y += sin(wp.x * _WaveFreq + t)
                      * cos(wp.z * _WaveFreq * 0.8 + t * 1.27)
                      * _WaveHeight;

                o.worldPos  = wp;
                o.pos       = UnityWorldToClipPos(wp);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.worldPos.xz;
                float  t  = _Time.y;

                // --- Base: manchas de dos azules que derivan (WW) ---
                float patch = vnoise(uv * _PatchScale + t * _PatchDrift);
                fixed3 col = lerp(_ColorDeep.rgb, _ColorShallow.rgb,
                                  smoothstep(0.32, 0.68, patch));

                // --- Espuma procedural: líneas serpenteantes (WW) ---
                float2 wobble = (vnoise(uv * _FoamScale * 0.7 + t * 0.05) - 0.5)
                              * _FoamWobble * 4.0;

                float2 uvA = uv * _FoamScale + wobble + t * _FoamSpeed * float2( 0.7, 0.4);
                float2 uvB = uv * _FoamScale * 0.62 - wobble * 0.5
                           + t * _FoamSpeed * float2(-0.4, 0.6);

                float foam = foamLine(uvA, _FoamWidth);
                foam = max(foam, foamLine(uvB, _FoamWidth * 1.25) * 0.75);

                // --- Espuma por textura (voronoi del artista) ---
                float2 uvTex = uv * _FoamTexTiling + t * _FoamTexSpeed.xy;
                float voro = tex2D(_FoamTex, uvTex).r;
                float texFoam = step(_FoamTexCutoff, voro) * _FoamTexIntensity;
                foam = max(foam, texFoam);

                // --- Espuma de orilla por profundidad (técnica del artista) ---
                float shoreLight = 0.0;
                float shoreDark  = 0.0;
                if (_UseShoreFoam > 0.5)
                {
                    float rawDepth  = tex2Dproj(_CameraDepthTexture,
                                                UNITY_PROJ_COORD(i.screenPos)).r;
                    float sceneEye  = LinearEyeDepth(rawDepth);
                    float waterEye  = i.screenPos.w;
                    float diff      = sceneEye - waterEye; // metros de agua hasta el fondo/objeto

                    // Borde orgánico: la textura voronoi "muerde" la distancia.
                    float bite = lerp(1.0, voro * 2.0, _ShoreWobble);

                    shoreLight = step(diff, _LightFoamDistance * bite);
                    shoreDark  = step(diff, _DarkFoamDistance);
                }

                // --- Destellos de sol (WW) ---
                float sp = vnoise(uv * _SparkleScale + t * 0.35);
                float sparkle = smoothstep(0.96, 0.99, sp) * _Sparkle;

                // --- Composición ---
                col = lerp(col, _DarkFoamColor.rgb, shoreDark * _DarkFoamColor.a);
                float whiteFoam = saturate(foam + sparkle);
                col = lerp(col, _FoamColor.rgb, whiteFoam);
                col = lerp(col, _LightFoamColor.rgb, shoreLight * _LightFoamColor.a);

                // La espuma siempre se ve sólida; el agua, según opacidad.
                float alpha = lerp(_Opacity, 1.0,
                                   saturate(whiteFoam + shoreLight + shoreDark * 0.5));

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
