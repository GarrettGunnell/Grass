Shader "Unlit/BillboardGrass" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        
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

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<float3> positionBuffer;

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;

                float3 position = positionBuffer[instanceID];
            
                float3 localPosition = v.vertex + float3(0.0f, 0.5f, 0.0f);
                float3 worldPosition = positionBuffer[instanceID] + localPosition;

                o.vertex = UnityObjectToClipPos(worldPosition);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(-(0.5 - col.a));
                
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float ndotl = DotClamped(lightDir, normalize(float3(0, 1, 0)));
                
                return col * ndotl;
            }

            ENDCG
        }
    }
}
