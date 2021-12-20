Shader "Unlit/ModelGrass" {
    Properties {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1)
        _Albedo2 ("Albedo 2", Color) = (1, 1, 1)
        _AOColor ("Ambient Occlusion", Color) = (1, 1, 1)
        _TipColor ("Tip Color", Color) = (1, 1, 1)
        _WindStrength ("Wind Strength", Range(0.5, 50.0)) = 1
        _Scale ("Scale", Range(0.0, 2.0)) = 0.0
        _Droop ("Droop", Range(0.0, 10.0)) = 0.0
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
            #include "Random.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float noiseVal : TEXCOORD1;
            };

            struct GrassData {
                float4 position;
                float2 uv;
                float displacement;
            };

            sampler2D _WindTex;
            float4 _Albedo1, _Albedo2, _AOColor, _TipColor;
            StructuredBuffer<GrassData> positionBuffer;
            StructuredBuffer<bool> voteBuffer;
            float _WindStrength, _CullingBias, _DisplacementStrength, _LODCutoff, _Scale, _Droop;

            float4 RotateAroundYInDegrees (float4 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }
            
            float4 RotateAroundXInDegrees (float4 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.yz), vertex.xw).zxyw;
            }

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
            
                float idHash = randValue(instanceID);
                idHash = randValue(idHash * 100000);

                float4 animationDirection = float4(0.0f, 0.0f, 1.0f, 0.0f);
                animationDirection = normalize(RotateAroundYInDegrees(animationDirection, idHash * 180.0f));

                float4 localPosition = RotateAroundXInDegrees(v.vertex, 90.0f);
                localPosition = RotateAroundYInDegrees(localPosition, idHash * 180.0f);
                localPosition += _Scale * v.uv.y * v.uv.y * v.uv.y;

                float4 grassPosition = positionBuffer[instanceID].position;
                
                float4 worldUV = float4(positionBuffer[instanceID].uv, 0, 0);
                
                float swayVariance = lerp(0.8, 1.0, idHash);
                float movement = v.uv.y * v.uv.y * v.uv.y * (tex2Dlod(_WindTex, worldUV).r - _Droop);
                movement *= swayVariance;
                
                localPosition.x += movement * animationDirection.x;
                localPosition.z += movement * animationDirection.y;
                
                float4 worldPosition = float4(grassPosition.xyz + localPosition, 1.0f);

                worldPosition.y -= positionBuffer[instanceID].displacement;
                worldPosition.y *= 1.0f + positionBuffer[instanceID].position.w;
                worldPosition.y += positionBuffer[instanceID].displacement;
                
                o.vertex = UnityObjectToClipPos(worldPosition);
                /*
                if (voteBuffer[instanceID] == 0)
                    o.vertex = 0;
                */
                o.uv = v.uv;
                o.noiseVal = tex2Dlod(_WindTex, worldUV).r;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float4 col = lerp(_Albedo1, _Albedo2, i.uv.y);
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float ndotl = DotClamped(lightDir, normalize(float3(0, 1, 0)));

                float4 ao = lerp(_AOColor, 1.0f, i.uv.y);
                float4 tip = lerp(0.0f, _TipColor, i.uv.y * i.uv.y * i.uv.y);

                //return i.noiseVal;
                return (col + tip) * ndotl * ao;
            }

            ENDCG
        }
    }
}
