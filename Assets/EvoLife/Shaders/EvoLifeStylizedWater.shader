Shader "EvoLife/StylizedWater"
{
    Properties
    {
        _Color ("Color", Color) = (0.18, 0.45, 0.62, 0.72)
        _WaveHeight ("Wave Height", Float) = 0.04
        _WaveSpeed ("Wave Speed", Float) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _WaveHeight;
            float _WaveSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float wave : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 local = v.vertex;
                float wave = sin((_Time.y * _WaveSpeed) + local.x * 3.2 + local.z * 2.4);
                local.y += wave * _WaveHeight;
                o.vertex = UnityObjectToClipPos(local);
                o.wave = wave * 0.5 + 0.5;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = _Color;
                color.rgb += i.wave * 0.08;
                return color;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Color"
}
