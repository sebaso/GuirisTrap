// ============================================================================
//  Guiri/Water_Style2 — Réplica exacta de WaterGuiri0 (Escala Corregida)
//  Añadidos multiplicadores para ajustar el tamaño gigante del patrón.
// ============================================================================
Shader "Guiri/Water_Style2"
{
    Properties
    {
        [Header(Flow Settings)]
        _FlowTexture ("Flow Texture (Normal Map)", 2D) = "bump" {}
        _Size ("Size (Tiling)", Vector) = (0.49, 2, 0, 0)
        _FlowScale ("Multiplicador Escala Flow", Range(0.001, 0.5)) = 0.05
        _FlowStrength ("Flow Strength", Vector) = (0.0075, 0.0075, 0, 0)
        _FlowSpeed ("Flow Speed", Vector) = (2, 2, 0, 0)

        [Header(Foam Settings)]
        _FoamTexture ("Foam Texture", 2D) = "white" {}
        _Foam_Distance ("Foam Tiling (UV)", Vector) = (2, 2, 0, 0)
        _FoamScale ("Multiplicador Escala Espuma", Range(0.001, 0.5)) = 0.05
        _Foam_Distance_1 ("Foam Cutoff", Float) = 0.17

        [Header(Colores)]
        _Water_Color ("Water Color", Color) = (0.0, 0.47, 1.0, 1)
        _Light_Foam_Color ("Light Foam Color", Color) = (0.87, 0.98, 0.98, 1)
        _Dark_Foam_Color ("Dark Foam Color", Color) = (0.0, 0.12, 0.61, 1)
        _Dark_Float_Intensity ("Dark Float Intensity", Float) = 0.1
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

            sampler2D _FlowTexture;
            float4 _Size;
            float _FlowScale;
            float4 _FlowStrength;
            float4 _FlowSpeed;

            sampler2D _FoamTexture;
            float4 _Foam_Distance;
            float _FoamScale;
            float _Foam_Distance_1;

            fixed4 _Water_Color;
            fixed4 _Light_Foam_Color;
            fixed4 _Dark_Foam_Color;
            float _Dark_Float_Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = worldPos.xz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                // 1. CÁLCULO DEL FLOW MAP (Multiplicado por _FlowScale para agrandar la textura)
                float2 flowUV = (i.uv * _FlowScale) * _Size.xy + t * _FlowSpeed.xy * 0.05;
                
                float4 flowTex = tex2D(_FlowTexture, flowUV);
                float2 flowNormal = flowTex.rg * 2.0 - 1.0;

                float2 uvOffset = flowNormal * _FlowStrength.xy;

                // 2. APLICAR DISTORSIÓN (Multiplicado por _FoamScale para hacer la espuma mucho más grande)
                float2 foamUV = ((i.uv * _FoamScale) + uvOffset) * _Foam_Distance.xy;
                foamUV += t * _FlowSpeed.xy * 0.02;

                // 3. TEXTURA DE ESPUMA Y CORTE
                float foamVal = tex2D(_FoamTexture, foamUV).r;
                float foamMask = step(_Foam_Distance_1, foamVal);

                // 4. BANDA OSCURA
                float darkFoamMask = step(_Foam_Distance_1 * (1.0 - _Dark_Float_Intensity), foamVal);
                float edgeMask = saturate(darkFoamMask - foamMask);

                // 5. COMPOSICIÓN
                fixed4 finalColor = _Water_Color;
                finalColor.rgb = lerp(finalColor.rgb, _Dark_Foam_Color.rgb, edgeMask * _Dark_Foam_Color.a);
                finalColor.rgb = lerp(finalColor.rgb, _Light_Foam_Color.rgb, foamMask * _Light_Foam_Color.a);

                float finalAlpha = lerp(_Water_Color.a, 1.0, saturate(foamMask + edgeMask * 0.5));

                return fixed4(finalColor.rgb, finalAlpha);
            }
            ENDCG
        }
    }
    Fallback Off
}