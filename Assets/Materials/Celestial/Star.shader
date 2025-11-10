Shader "Unlit/Star"
{
    Properties
    {
        [HDR] _Colour1 ("Colour1", Color) = (1,0,0,1)
        [HDR] _Colour2 ("Colour2", Color) = (0,1,0,1)
        [HDR] _FresnelColour ("Fresnel Colour", Color) = (1,0,0,1)
        _Persistance ("Persistance", float) = 3
        _Roughness ("Roughness", float) = 1
        _Scale ("Scale", float) = 1
        _Move ("Move", Vector) = (0, 1, 0, 0)
		_Fresnel ("Fresnel Coefficient", float) = 5.0
		_Reflectance ("Reflectance", float) = 1.0
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

            float _Persistance;
            float _Roughness;
            float _Scale;
            float _Fresnel;
            float _Reflectance;
            float _GlobalTime;
            fixed4 _FresnelColour;
            fixed4 _Colour1;
            fixed4 _Colour2;
            float3 _Move;

            #include "UnityCG.cginc"
            #include "../NoiseCommon.cginc"

            #ifndef OCTAVES
            #define OCTAVES 3
            #endif

            float sampleLayeredNoise(float3 value, float persistance, float roughness)
            {
                float noise = 0;
                float frequency = 1;
                float factor = 1;

                [unroll]
                for(int i = 0; i < OCTAVES; i++)
                {
                    noise = noise + SimplexNoise(value * frequency + i * 0.72354) * factor;
                    factor *= persistance;
                    frequency *= roughness;
                }

                return noise;
            }

#ifdef SHADER_API_D3D11
            // DX11 doesn't have normal semantics, I'm not sure why.
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : TEXCOORD1;
                float3 viewDir : POSITION1;
            };

            struct v2f
            {
                float4 vertex : POSITION;
                float4 localVertex : POSITION1;
                float3 normal : TEXCOORD1;
                float3 viewDir : POSITION2;
            };
#else
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 viewDir : NORMAL1;
            };

            struct v2f
            {
                float4 vertex : POSITION;
                float4 localVertex : POSITION1;
                float3 normal : NORMAL;
                float3 viewDir : NORMAL1;
            };
#endif

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localVertex = v.vertex;
                o.normal = normalize(v.normal);
                o.viewDir = normalize(ObjSpaceViewDir(v.vertex));//ObjSpaceViewDir is similar, but localspace.
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize (i.normal);
                float3 v = normalize (i.viewDir);
                float fr = pow(1.0f - dot(v, n), _Fresnel) * _Reflectance;

                // sample the texture
                float t = sampleLayeredNoise(i.localVertex * _Scale + _Move * _GlobalTime * 0.1, _Persistance, _Roughness);
                fixed4 col = lerp(_Colour1, _Colour2, clamp(t, 0, 1)) + _FresnelColour * fr;
                return col;
            }
            ENDCG
        }
    }
}
