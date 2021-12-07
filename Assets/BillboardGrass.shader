Shader "Unlit/BillboardGrass" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Range(0.5, 50.0)) = 1

        _GrassNoiseTex ("Saturation Map", 2D) = "white" {}
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
            #include "Random.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex, _HeightMap;
            float4 _MainTex_ST;
            StructuredBuffer<float4> positionBuffer;
            float _Rotation, _WindStrength;

            float4 RotateAroundYInDegrees (float4 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
            
                float3 localPosition = RotateAroundYInDegrees(v.vertex, _Rotation).xyz;

                float localWindVariance = min(max(0.4f, randValue(instanceID)), 0.75f);
                
                float cosTime;
                if (localWindVariance > 0.6f)
                    cosTime = cos(_Time.y * (_WindStrength - (positionBuffer[instanceID].w - 1.0f)));
                else
                    cosTime = cos(_Time.y * ((_WindStrength - (positionBuffer[instanceID].w - 1.0f)) + localWindVariance * 0.1f));
                    
    
                float trigValue = ((cosTime * cosTime) * 0.65f) - localWindVariance * 0.5f;
                
                localPosition.x += v.uv.y * trigValue * positionBuffer[instanceID].w * localWindVariance * 0.6f;
                localPosition.z += v.uv.y * trigValue * positionBuffer[instanceID].w * 0.4f;
                localPosition.y *= v.uv.y * (1.0f + positionBuffer[instanceID].w);
                
                float4 worldPosition = float4(positionBuffer[instanceID].xyz + localPosition, 1.0f);

                //worldPosition.y *= positionBuffer[instanceID].w;

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
