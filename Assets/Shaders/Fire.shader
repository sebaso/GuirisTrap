// ============================================================================
//  Guiri/Fire — llama procedural para el evento de incendio (v2)
//
//  Fuego animado 100% procedural (ruido en el fragment shader, sin texturas).
//
//  v2: BILLBOARD CILÍNDRICO. La v1 usaba billboard esférico (la llama se
//  encaraba del todo a la cámara, inclinación incluida), lo que con la cámara
//  isométrica del juego hacía que la llama se "tumbara" y se viera con una
//  perspectiva rara. Ahora la llama gira SOLO alrededor del eje vertical del
//  mundo: siempre de pie, mirando a la cámara solo en horizontal.
//
//  Además, la BASE de la llama queda anclada al origen del quad (antes estaba
//  centrada y el fuego flotaba). El _fireVfxOffset del FireEventManager ahora
//  marca dónde está la base del fuego: si lo veis alto, bajad su Y (p. ej.
//  de 1.2 a 0.8-0.9 para que nazca de la encimera).
//
//  Guardar en Assets/Shaders/Fire.shader (sustituye al anterior).
// ============================================================================
Shader "Guiri/Fire"
{
    Properties
    {
        _InnerColor ("Color interior", Color)       = (1.0, 0.92, 0.35, 1)
        _OuterColor ("Color exterior", Color)       = (1.0, 0.25, 0.00, 1)
        _Speed      ("Velocidad",      Range(0, 6)) = 2.2
        _Turbulence ("Turbulencia",    Range(0, 1)) = 0.55
        _Intensity  ("Intensidad",     Range(0, 4)) = 1.6
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+10"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"   // el billboard usa el origen del objeto
        }

        Blend SrcAlpha One   // aditivo: el fuego suma luz
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _InnerColor;
            fixed4 _OuterColor;
            float  _Speed;
            float  _Turbulence;
            float  _Intensity;

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

            // ---------- ruido procedural (value noise + fbm) ----------
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
                return 0.50 * vnoise(p)
                     + 0.30 * vnoise(p * 2.1)
                     + 0.20 * vnoise(p * 4.3);
            }
            // -----------------------------------------------------------

            v2f vert (appdata v)
            {
                v2f o;

                // Escala del transform (para respetar el tamaño del quad).
                float sx = length(unity_ObjectToWorld._m00_m10_m20);
                float sy = length(unity_ObjectToWorld._m01_m11_m21);

                // Origen del objeto en mundo.
                float3 originWS = float3(unity_ObjectToWorld._m03,
                                         unity_ObjectToWorld._m13,
                                         unity_ObjectToWorld._m23);

                // BILLBOARD CILÍNDRICO: dirección a la cámara APLASTADA al
                // plano horizontal → la llama gira solo alrededor del eje Y
                // del mundo y se mantiene siempre vertical.
                float3 toCam = _WorldSpaceCameraPos - originWS;
                toCam.y = 0.0;
                float len = max(length(toCam), 0.001); // cámara justo encima: evitar división por 0
                toCam /= len;

                float3 up    = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCam));

                // Base de la llama anclada al origen (+0.5*sy sube el quad
                // centrado para que su borde inferior quede en el origen).
                float3 worldPos = originWS
                                + right * (v.vertex.x * sx)
                                + up    * (v.vertex.y * sy + 0.5 * sy);

                o.pos = UnityWorldToClipPos(float4(worldPos, 1.0));
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv; // y: 0 base de la llama, 1 punta

                // Ruido que asciende (la llama "sube").
                float n = fbm(float2(uv.x * 3.0, uv.y * 2.5 - _Time.y * _Speed));

                // Posición horizontal respecto al centro, ondulada por el
                // ruido (más ondulación cuanto más arriba: lenguas de fuego).
                float x = (uv.x - 0.5) * 2.0;
                x += (n - 0.5) * _Turbulence * (0.3 + uv.y * 1.4);

                // Silueta: ancha en la base, estrecha hacia la punta.
                float width = max(0.05, 1.0 - uv.y * 0.85);
                float body  = 1.0 - abs(x) / width;
                body -= uv.y * 0.35;                       // se apaga hacia arriba
                body += (n - 0.5) * 0.35;                  // borde roto por el ruido
                float flame = smoothstep(0.05, 0.65, body);

                // Parpadeo global.
                float flicker = 0.85 + 0.15 * vnoise(float2(_Time.y * 7.0, 3.7));

                // Rampa de color: exterior rojizo → interior amarillo.
                fixed3 col = lerp(_OuterColor.rgb, _InnerColor.rgb, pow(flame, 2.2));

                float a = saturate(flame * flicker * _Intensity);
                return fixed4(col, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
