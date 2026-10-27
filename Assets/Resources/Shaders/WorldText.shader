// World text: font-atlas lettering that lives in the level. Unlike the built-in
// GUI/Text shader (ZTest Always — labels through every wall), this one is
// depth-tested, sits a hair in front of whatever it is mounted on, takes its
// colour from the TextMesh vertex colours, and fades out with distance so far
// lettering never sparkles into grey lines. Lives in Resources so builds keep it.
Shader "Momentum/WorldText"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FadeStart ("Fade Start (m)", Float) = 60
        _FadeEnd ("Fade End (m)", Float) = 140
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        Cull Off
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "WorldText"
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float dist : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.dist = distance(wp, _WorldSpaceCameraPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = i.color;
                col.a *= tex2D(_MainTex, i.uv).a;
                col.a *= saturate((_FadeEnd - i.dist) / max(0.001, _FadeEnd - _FadeStart));
                return col;
            }
            ENDCG
        }
    }
    Fallback "GUI/Text Shader"
}
