Shader "Guiri/SkyGradient"
{
    Properties
    {
        _ZenithColor  ("Color del cenit", Color)   = (0.25, 0.55, 0.9, 1)
        _HorizonColor ("Color del horizonte", Color) = (0.95, 0.85, 0.65, 1)
        _GroundColor  ("Color bajo el horizonte", Color) = (0.45, 0.40, 0.38, 1)

        _HorizonBand  ("Anchura de la banda del horizonte", Range(0.01, 1)) = 0.28
        _ZenithFalloff("Dureza del degradado", Range(0.5, 4)) = 1.6

        // El grano es lo que le da el aire pintado a mano: sin él el degradado
        // se ve digital y se le notan las bandas en pantallas normales.
        _Grain        ("Grano / textura de papel", Range(0, 0.06)) = 0.015
        _Exposure     ("Exposición", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off  ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _ZenithColor, _HorizonColor, _GroundColor;
            half  _HorizonBand, _ZenithFalloff, _Grain, _Exposure;

            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            // Ruido barato para el grano. No hace falta que sea bonito, solo que
            // rompa las bandas del degradado.
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y; // -1 abajo, 0 horizonte, 1 arriba

                // Banda cálida pegada al horizonte, que es de donde sale el aire
                // de ilustración: el degradado no va de arriba abajo sin más,
                // sino que se concentra cerca del horizonte.
                float t = saturate(h / max(_HorizonBand, 0.001));
                t = pow(t, _ZenithFalloff);

                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, t);

                // Por debajo del horizonte, un color plano (casi nunca se ve,
                // pero evita que se cuele el cenit invertido si la cámara baja).
                float below = saturate(-h * 8.0);
                sky = lerp(sky, _GroundColor.rgb, below);

                sky *= _Exposure;

                float g = (hash(i.pos.xy) - 0.5) * _Grain;
                sky += g;

                return half4(sky, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
