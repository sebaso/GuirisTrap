Shader "Guiri/StationIndicator"
{
    Properties
    {
        _Color      ("Color",           Color)        = (1, 0.1, 0.5, 1)
        _RingCount  ("Anillos",         Range(1, 6))  = 3
        _Speed      ("Velocidad",       Range(0, 5))  = 1.2
        _Intensity  ("Intensidad",      Range(0, 4))  = 1.6
        _CenterGlow ("Brillo central",  Range(0, 2))  = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+10"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One   // aditivo: suma luz, queda brillante
        ZWrite Off
        ZTest Always         // visible a través de la geometría
        Cull Off             // visible por ambas caras del quad

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float  _RingCount;
            float  _Speed;
            float  _Intensity;
            float  _CenterGlow;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Distancia radial: 0 en el centro del quad, 1 en el borde.
                float2 c    = i.uv - 0.5;
                float  dist = length(c) * 2.0;

                // Anillos expandiéndose hacia fuera (efecto sonar).
                float wave = frac(dist * _RingCount - _Time.y * _Speed);
                float ring = smoothstep(0.0, 0.15, wave) * smoothstep(0.45, 0.2, wave);

                // Los anillos se desvanecen hacia el borde y mueren fuera del círculo.
                float edgeFade = saturate(1.0 - dist);
                edgeFade *= edgeFade;

                // Brillo central con latido.
                float pulse = 0.75 + 0.25 * sin(_Time.y * 3.0);
                float glow  = _CenterGlow * pulse * saturate(1.0 - dist * 2.2);

                float a = (ring * edgeFade + glow) * _Intensity;
                return fixed4(_Color.rgb, saturate(a) * _Color.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
