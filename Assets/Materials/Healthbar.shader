Shader "Unlit/Healthbar"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
        _HealthColour ("Health Colour", Color) = (0,1,0,1)
        _UnhealthColour ("Unhealth Colour", Color) = (1,0,0,1)
        _HitColour ("Hit Colour", Color) = (1,1,1,1)
        _HealthPercent ("Health Percent", float) = 1.0
        _HitPercent ("Hit Percent", float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _HealthColour;
            fixed4 _UnhealthColour;
            fixed4 _HitColour;
            uniform half _HealthPercent;
            uniform half _HitPercent;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.uv.x < 0.02 || i.uv.x > 0.98 || i.uv.y < 0.2 || i.uv.y > 0.8)
                {
                    return fixed4(0,0,0,1);
                }
                fixed4 col = i.uv.x >= _HealthPercent ? (i.uv.x >= _HealthPercent + _HitPercent ? _UnhealthColour : _HitColour) : _HealthColour;
                return col;
            }
            ENDCG
        }
    }
}
