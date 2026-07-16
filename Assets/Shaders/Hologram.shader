// ============================================================================
//  Guiri/Hologram — fantasma de colocación de muebles
//
//  Holograma translúcido con scanlines ascendentes y borde fresnel brillante.
//  Sustituye al tinte con alpha sobre los materiales originales del prefab,
//  que en materiales opacos se IGNORA (el ghost se veía sólido).
//
//  Lo aplica PlayerController.CreateGhost automáticamente a todos los
//  renderers del clon. El tinte verde-OK / rojo-MAL de TintGhost sigue
//  funcionando igual: entra por la propiedad _Color de siempre.
//
//  Guardar en Assets (p. ej. Assets/Shaders/Hologram.shader).
// ============================================================================
Shader "Guiri/Hologram"
{
    Properties
    {
        _Color            ("Color (con alpha)",     Color)         = (0.3, 1.0, 0.3, 0.45)
        _ScanlineDensity  ("Densidad de scanlines", Range(0, 200)) = 60
        _ScanSpeed        ("Velocidad de scanlines",Range(0, 10))  = 2
        _FresnelPower     ("Fuerza del borde",      Range(0.5, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha  // transparencia normal: el rojo se ve rojo
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float  _ScanlineDensity;
            float  _ScanSpeed;
            float  _FresnelPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNrm : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNrm = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 nrm     = normalize(i.worldNrm);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                // Borde fresnel: brilla en las siluetas, como un holograma.
                float rim = pow(1.0 - saturate(abs(dot(nrm, viewDir))), _FresnelPower);

                // Scanlines horizontales subiendo (basadas en la Y del mundo:
                // continuas aunque el mueble tenga varias piezas).
                float scan = 0.7 + 0.3 * sin(i.worldPos.y * _ScanlineDensity
                                             - _Time.y * _ScanSpeed * 10.0);

                fixed3 col = _Color.rgb * (1.0 + rim * 1.5);
                float  a   = _Color.a * scan * lerp(0.65, 1.4, rim);

                return fixed4(col, saturate(a));
            }
            ENDCG
        }
    }

    Fallback Off
}
