// ============================================================================
//  Guiri/FireFlame - llama procedural para el evento de incendio (v3)
//
//  Fuego 100% procedural: ruido fBm en el fragment shader, sin texturas.
//
//  Por que existe esta v3 (el Shader Graph "Fire_Cartton" no servia):
//    - El material estaba en modo OPACO (_Surface=0, _ZWrite=1), asi que el
//      canal alfa se descartaba y el quad se dibujaba como una tarjeta
//      rectangular solida en vez de una llama.
//    - El BaseColor del grafo venia de "Vertex Color", pero el Quad de Unity
//      no tiene stream COLOR, asi que el color nunca se modulaba por la silueta.
//    - Todo el ruido/twirl/mascara alimentaba solo el Alpha, con lo que al
//      descartarse el alfa casi no se veia nada del trabajo procedural.
//
//  Diseno:
//    - BILLBOARD CILINDRICO: la llama gira solo alrededor del eje Y del mundo,
//      por lo que siempre queda de pie y encara la camara solo en horizontal
//      (correcto para la camara isometrica del juego).
//    - La BASE de la llama queda anclada al origen del objeto: el offset del
//      FireEventManager marca donde nace el fuego, no su centro.
//    - Silueta ancha abajo y afilada arriba, con bordes rotos por ruido y
//      lenguas que ondulan mas cuanto mas alto.
//    - Rampa de color: nucleo amarillo-blanco -> naranja -> rojo exterior.
//    - Mezcla ADITIVA (SrcAlpha One) + ZWrite Off: el fuego suma luz.
//
//  Propiedades expuestas para ajustar el look sin tocar el shader.
// ============================================================================
Shader "Guiri/FireFlame"
{
    Properties
    {
        _InnerColor ("Color nucleo",   Color)           = (1.0, 0.95, 0.55, 1)
        _MidColor   ("Color medio",    Color)           = (1.0, 0.45, 0.05, 1)
        _OuterColor ("Color exterior", Color)           = (0.85, 0.10, 0.00, 0)
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

                float n1 = fbm(float2(uv.x * 3.0,       uv.y * 2.2 - t));
                float n2 = fbm(float2(uv.x * 5.5 + 7.3, uv.y * 3.4 - t * 1.6));

                float sway = (n1 - 0.5) * _Turbulence * (0.25 + uv.y * 1.6);
                sway += (n2 - 0.5) * _Turbulence * uv.y * 0.6;
                x += sway;

                float width = max(0.06, 1.0 - uv.y * 0.88);
                float body  = 1.0 - abs(x) / width;

                body -= uv.y * uv.y * 0.30;
                body += (n1 - 0.5) * 0.55 + (n2 - 0.5) * 0.25;

                float flame = saturate(body / max(_Softness, 0.001));

                float core = saturate(1.0 - abs(x) / max(width * 0.55, 0.001));
                core *= saturate(1.0 - uv.y * 1.35);
                core *= 0.65 + 0.35 * n2;

                float flicker = 1.0 - _Flicker * (0.5 + 0.5 * vnoise(float2(t * 3.1, 3.7)));

                float k = saturate(flame * 0.8 + core * 0.9);
                fixed3 col = lerp(_OuterColor.rgb, _MidColor.rgb, smoothstep(0.0, 0.55, k));
                col = lerp(col, _InnerColor.rgb, smoothstep(0.45, 1.0, k));

                float a = flame * flicker * _Intensity;
                a *= lerp(1.0, 0.85, uv.y);

                return fixed4(col * a, a);
            }
            ENDCG
        }
    }

    Fallback Off
}