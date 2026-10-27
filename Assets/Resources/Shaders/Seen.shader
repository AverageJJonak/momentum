// THE LAST FEW, SEEN THROUGH THE WALLS. When a room is nearly clear the units
// still standing are marked with this, so hunting the last one is never a search
// of every corner you have already been down. Two passes, so it looks like an
// x-ray and not a blob:
//   HIDDEN  — only where something stands between you and the unit (ZTest
//             Greater): the whole body, solid amber lifted at the edges, with a
//             scan line climbing it. A glowing FIGURE behind the wall.
//   VISIBLE — where you can already see it (ZTest LEqual): the rim alone, a
//             bright edge round the unit, nothing painted over what it is doing.
// Additive, so it reads over anything. Amber on purpose: in this game's colour
// language amber is a unit's own light, and this is the unit giving itself
// away. Lives in Resources so builds keep it (nothing else references it, and
// Unity strips what nothing references). The two passes carry different
// LightMode tags because URP draws one pass per tag per object.
Shader "Momentum/Seen"
{
    Properties
    {
        _Color ("Colour", Color) = (2.2, 1.3, 0.35, 0.6)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+60" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "SeenHidden"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZTest Greater
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float rim : TEXCOORD0; float wy : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 view = normalize(_WorldSpaceCameraPos - wp);
                o.rim = 1.0 - saturate(dot(n, view));
                o.wy = wp.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float pulse = 0.8 + 0.2 * sin(_Time.y * 5.0);
                float band = frac(i.wy * 0.3 - _Time.y * 0.45);          // a scan line climbing the body
                float scan = smoothstep(0.86, 1.0, band) * 0.5;
                fixed4 c = _Color;
                c.a *= (0.42 + 0.58 * pow(i.rim, 1.4) + scan) * pulse;
                return c;
            }
            ENDCG
        }

        Pass
        {
            Name "SeenRim"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            ZTest LEqual
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float rim : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 view = normalize(_WorldSpaceCameraPos - wp);
                o.rim = 1.0 - saturate(dot(n, view));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float pulse = 0.8 + 0.2 * sin(_Time.y * 5.0);
                fixed4 c = _Color;
                c.a *= pow(i.rim, 3.0) * 1.5 * pulse;                    // the edge, and only the edge
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
