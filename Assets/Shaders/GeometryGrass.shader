Shader "Unlit/GeometryGrass" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Range(0.5, 50.0)) = 1
        _CullingBias ("Cull Bias", Range(0.1, 1.0)) = 0.5
        _LODCutoff ("LOD Cutoff", Range(10.0, 500.0)) = 100
    }

    SubShader {
        Cull Off
        Zwrite On

        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp
            
            #pragma target 4.5

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "../Resources/Random.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            struct g2f {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vp(VertexData v) {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                
                return o;
            }

            fixed4 fp(v2f i) : SV_Target {
                return 1.0f;
            }

            ENDCG
        }
    }
}
