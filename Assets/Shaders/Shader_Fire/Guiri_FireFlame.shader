// ============================================================================
//  Guiri/FireFlame - llama procedural para el evento de incendio (v4)
//
//  Fuego 100% procedural: ruido fBm con warp de dominio, sin texturas.
//
//  Invariantes que no romper:
//    - BILLBOARD CILINDRICO: gira solo alrededor del eje Y del mundo
//      (correcto para la camara isometrica).
//    - La BASE queda anclada al origen del objeto: el offset del
//      FireEventManager marca donde nace el fuego, no su centro.
//    - Blending ADITIVO (SrcAlpha One) + ZWrite Off: el fuego suma luz.
//      _Intensity escala el COLOR (luz), no el alfa (silueta).
//    - La punta se consume por si sola (body -= pow(uv.y, 1.4)); la llama
//      debe cerrarse antes del borde superior del quad, nunca recortarse.
//
//  v4: warp de dominio (remolinos), frecuencia lateral creciente con la
//  altura (la punta se rompe en lenguas), flamelets ridged en el nucleo,
//  borde firme (smoothstep), nucleo blanco-caliente y base azulada sutil
//  (_BaseColor.a controla la fuerza de esa mezcla).
// ============================================================================
Shader "Guiri/FireFlame"
{
    Properties
    {
        _InnerColor ("Color nucleo",   Color)           = (1.0, 0.95, 0.55, 1)
        _MidColor   ("Color medio",    Color)           = (1.0, 0.45, 0.05, 1)
        _OuterColor ("Color exterior", Color)           = (0.85, 0.10, 0.00, 0)
        _BaseColor  ("Color base (azulado)", Color)     = (0.25, 0.45, 1.00, 0.5)
        _Speed      ("Velocidad",      Range(0, 6))     = 1.9
        _Turbulence ("Turbulencia",    Range(0, 1))     = 0.5
        _Intensity  ("Intensidad",     Range(0, 4))     = 1.35
        _Softness   ("Suavidad borde", Range(0.05, 1))  = 0.42
        _Flicker    ("Parpadeo",       Range(0, 1))     = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+10"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline"  = "UniversalPipeline"
            "DisableBatching" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FireFlame"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _InnerColor;
            fixed4 _MidColor;
            fixed4 _OuterColor;
            fixed4 _BaseColor;
            float  _Speed;
            float  _Turbulence;
            float  _Intensity;
            float  _Softness;
            float  _Flicker;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float hash21 (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
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
                float v = 0.0;
                v += 0.5000 * vnoise(p);
                v += 0.2500 * vnoise(p * 2.03);
                v += 0.1250 * vnoise(p * 4.11);
                v += 0.0625 * vnoise(p * 8.07);
                return v / 0.9375;
            }

            v2f vert (appdata v)
            {
                v2f o;

                float sx = length(unity_ObjectToWorld._m00_m10_m20);
                float sy = length(unity_ObjectToWorld._m01_m11_m21);

                float3 originWS = float3(unity_ObjectToWorld._m03,
                                         unity_ObjectToWorld._m13,
                                         unity_ObjectToWorld._m23);

                float3 toCam = _WorldSpaceCameraPos - originWS;
                toCam.y = 0.0;
                float len = max(length(toCam), 0.0001);
                toCam /= len;

                float3 up    = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCam));

                float3 worldPos = originWS
                                + right * (v.vertex.x * sx)
                                + up    * (v.vertex.y * sy + 0.5 * sy);

                o.pos = UnityWorldToClipPos(float4(worldPos, 1.0));
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float t = _Time.y * _Speed;

                float x = (uv.x - 0.5) * 2.0;

                // Frecuencia lateral crece con la altura: masa solida abajo,
                // lengua pequenas y quebradas arriba.
                float fx = 3.0 + 2.5 * uv.y;

                // El patron sube mas rapido cuanto mas alto (gas acelerando).
                float rise = t * (1.0 + uv.y * 0.6);

                float n1 = fbm(float2(uv.x * fx,             uv.y * 1.6 - rise));
                float n2 = fbm(float2(uv.x * fx * 1.9 + 7.3, uv.y * 2.6 - rise * 1.8));

                // Warp del dominio: dos fbm lentos curvan la llama (remolinos
                // en vez de scroll recto). Solo desplazan en horizontal.
                float warpX = fbm(float2(uv.x * 2.0 + 3.1, uv.y * 1.3 - t * 0.55)) - 0.5;
                float warpY = fbm(float2(uv.x * 2.0 - 1.7, uv.y * 1.3 - t * 0.45)) - 0.5;

                float sway = warpX * _Turbulence * (0.35 + uv.y * 1.8);
                sway += 0.10 * _Turbulence * sin(t * 1.3 + n1 * 3.0) * uv.y;
                x += sway;

                // Silueta: base ancha y punta afilada (taper curvado, no cono).
                float width = 0.95 * pow(saturate(1.0 - uv.y), 0.62) + 0.05;
                float body  = 1.0 - abs(x) / width;

                body -= pow(uv.y, 1.4) * 0.95;
                body += (n1 - 0.5) * 0.55 + (n2 - 0.5) * 0.30;
                body += warpY * _Turbulence * 0.35;
                body += (vnoise(float2(t * 3.1, 3.7)) - 0.5) * _Flicker * uv.y * 0.5;

                float flame = saturate(body / max(_Softness, 0.001));
                flame = flame * flame * (3.0 - 2.0 * flame);

                // Nucleo estrecho y bajo, con filamentas ridged (lenguas
                // brillantes dentro del cuerpo).
                float tongues = 1.0 - abs(2.0 * n2 - 1.0);
                float core = saturate(1.0 - abs(x) / max(width * 0.5, 0.001));
                core *= saturate(1.0 - uv.y * 1.45);
                core *= 0.55 + 0.45 * tongues;

                // Parpadeo global de brillo (ruido independiente del aleteo).
                float flicker = 1.0 - _Flicker * (0.5 + 0.5 * vnoise(float2(t * 2.3, 9.1)));

                // Rampa de temperatura: rojo -> naranja -> amarillo ->
                // blanco-caliente en el centro del nucleo.
                float k = saturate(flame * 0.85 + core * 0.75);
                fixed3 col = lerp(_OuterColor.rgb, _MidColor.rgb, smoothstep(0.0, 0.5, k));
                col = lerp(col, _InnerColor.rgb, smoothstep(0.35, 0.85, k));

                float hot = saturate((core - 0.5) * 2.0);
                col = lerp(col, fixed3(1.0, 0.98, 0.90), hot * 0.65);

                // Base azulada sutil (gas recien encendido); _BaseColor.a
                // regula la fuerza de la mezcla (0 = apagarla).
                float blueBase = smoothstep(0.15, 0.5, flame) * saturate(1.0 - uv.y * 4.5);
                col = lerp(col, _BaseColor.rgb, blueBase * saturate(_BaseColor.a));

                float a = flame * lerp(1.0, 0.85, uv.y);

                return fixed4(col * _Intensity * flicker, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
