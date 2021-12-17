Shader "Unlit/ModelGrass" {
    Properties {
        _Albedo ("Albedo", Color) = (1, 1, 1)
        _AOColor ("Ambient Occlusion", Color) = (1, 1, 1)
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
            #include "Random.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float saturationLevel : TEXCOORD1;
            };

            struct GrassData {
                float4 position;
            };

            float4 _Albedo, _AOColor;
            StructuredBuffer<GrassData> positionBuffer;
            float _Rotation, _WindStrength, _CullingBias, _DisplacementStrength, _LODCutoff;

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

            bool VertexIsBelowClipPlane(float3 p, int planeIndex, float bias) {
                float4 plane = unity_CameraWorldClipPlanes[planeIndex];

                return dot(float4(p, 1), plane) < bias;
            }   

            bool cullVertex(float3 p, float bias) {
                return  distance(_WorldSpaceCameraPos, p) > _LODCutoff ||
                        VertexIsBelowClipPlane(p, 0, bias) ||
                        VertexIsBelowClipPlane(p, 1, bias) ||
                        VertexIsBelowClipPlane(p, 2, bias) ||
                        VertexIsBelowClipPlane(p, 3, -max(1.0f, _DisplacementStrength));
            }

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
            
                
                float idHash = randValue(instanceID);

                float4 animationDirection = float4(0.0f, 0.0f, 1.0f, 0.0f);
                animationDirection = normalize(RotateAroundYInDegrees(animationDirection, idHash * 90.0f));

                float4 localPosition = RotateAroundXInDegrees(v.vertex, 90.0f);
                localPosition = RotateAroundYInDegrees(localPosition, idHash * 90.0f);

                float4 grassPosition = positionBuffer[instanceID].position;
                
                //localPosition.xz += grassPosition.w * v.uv.y * animationDirection.xz + float2(1.0f, 1.0f);
                
                float4 worldPosition = float4(grassPosition.xyz + localPosition, 1.0f);
            
                worldPosition.y *= 1.0f + 0.5f * positionBuffer[instanceID].position.w;
                
                o.vertex = UnityObjectToClipPos(worldPosition);
                o.uv = v.uv;
                o.saturationLevel = 1.0 - ((positionBuffer[instanceID].position.w - 1.0f) / 1.5f);
                o.saturationLevel = min(o.saturationLevel, 0.2f);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float4 col = _Albedo;
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float ndotl = DotClamped(lightDir, normalize(float3(0, 1, 0)));

                float saturation = lerp(1.0f, i.saturationLevel, i.uv.y * i.uv.y);
                col.r /= saturation;

                float4 ao = lerp(_AOColor, 1.0f, i.uv.y);
                
                return col * ndotl * ao;
            }

            ENDCG
        }
    }
}
