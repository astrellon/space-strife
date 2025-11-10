// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Orbits/StarasBackground"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _MainColour("Main Colour", Color) = (0.6,0.4,0.5,1.0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
                half2 uv : TEXCOORD0;
            };

            uniform float _RandomOffset;
            uniform float4x4 _Matrix;
            uniform float _NumInstances;
            uniform float4 _ScaleOffset;
            sampler2D _MainTex;
            float3 _MainColour;

            //  3 out, 1 in...
            float4 hash31(float p)
            {
                float3 p3 = frac(float3(p, p, p) * float3(.1031, .1030, .0973));
                p3 += dot(p3, p3.yzx+33.33);
                return float4(frac((p3.xxy+p3.yzz)*p3.zyx), 0);
            }

            v2f vert(appdata_base v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float4 offset = hash31(instanceID + _RandomOffset);
                float4 wpos = mul(_Matrix, v.vertex + offset * _ScaleOffset);
                o.pos = UnityObjectToClipPos(wpos);
                o.uv = v.texcoord;
                o.color = float4(offset.xyz * 0.4 + _MainColour, offset.x * 0.3 + 0.7);

                float4 twinkle = hash31(_Time.x * 0.0175 + _RandomOffset + instanceID) * float4(1, 0.5, 0.1, 0);
                o.color += twinkle;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color * tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}