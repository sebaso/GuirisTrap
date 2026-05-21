Shader "Guiri/StationIndicator"
{
    Properties
    {
        _Color      ("Color",         Color)            = (1, 0.1, 0.5, 1)
        _LineCount  ("Line Count",    Float)            = 10
        _Speed      ("Speed",         Float)            = 1.5
        _LineWidth  ("Line Width",    Range(0.01, 0.5)) = 0.15
        _Intensity  ("Intensity",     Float)            = 2.5
        _FadeRadius ("Fade Radius",   Range(0.3, 1.0))  = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "StationIndicatorPass"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _LineCount;
                float  _Speed;
                float  _LineWidth;
                float  _Intensity;
                float  _FadeRadius;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv          = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // UV centradas en (0,0)
                float2 uv = input.uv - 0.5;

                // Distancia de Chebyshev → forma cuadrada perfecta
                float dist = max(abs(uv.x), abs(uv.y)) * 2.0;

                // Cuadrados concéntricos que se mueven hacia dentro
                float rings = frac(dist * _LineCount - _Time.y * _Speed);

                // Máscara de línea
                float LineWidth = step(1.0 - _LineWidth, rings);

                // Fade suave hacia los bordes
                float edgeFade = 1.0 - smoothstep(_FadeRadius * 0.6, _FadeRadius, dist);

                // Brillo suave en el centro
                float centerGlow = (1.0 - smoothstep(0.0, 0.25, dist)) * 0.4;

                // Pulso de intensidad global
                float pulse = 0.85 + 0.15 * sin(_Time.y * 3.0);

                float alpha = (LineWidth * edgeFade + centerGlow) * _Color.a * pulse;
                float3 col  = _Color.rgb * _Intensity;

                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
