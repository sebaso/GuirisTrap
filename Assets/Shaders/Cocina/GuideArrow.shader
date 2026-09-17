Shader "Guiri/GuideArrow"
{
    Properties
    {
        _Color     ("Color",               Color)       = (1, 0.55, 0.1, 1)
        _Bands     ("Bandas",              Range(1, 8)) = 3
        _FlowSpeed ("Velocidad del flujo", Range(0, 6)) = 2.5
        _BaseAlpha ("Opacidad base",       Range(0, 1)) = 0.45
        _Intensity ("Intensidad",          Range(0, 4)) = 1.8
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+10"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One   // aditivo
        ZWrite Off
        ZTest Always         // visible a través de la geometría
        Cull Off             // la flecha es plana: visible por ambas caras

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float  _Bands;
            float  _FlowSpeed;
            float  _BaseAlpha;
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

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Bandas que viajan hacia la punta (uv.y crece hacia la punta).
                float wave = frac(i.uv.y * _Bands - _Time.y * _FlowSpeed);
                float band = smoothstep(0.0, 0.25, wave) * smoothstep(0.6, 0.3, wave);

                // Más brillante cuanto más cerca de la punta.
                float tip = lerp(0.35, 1.0, i.uv.y);

                float a = (_BaseAlpha + band) * tip * _Intensity;
                return fixed4(_Color.rgb, saturate(a) * _Color.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
