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
            #pragma vertex vert
            #pragma fragment frag
            
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

            struct GrassData {
                float4 position;
            };

            sampler2D _MainTex, _HeightMap;
            float4 _MainTex_ST;
            StructuredBuffer<GrassData> positionBuffer;

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;

                o.vertex = UnityObjectToClipPos(positionBuffer[instanceID].position);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return 1.0f;
            }

            ENDCG
        }
    }
}
