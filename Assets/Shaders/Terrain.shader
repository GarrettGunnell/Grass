

Shader "Custom/Terrain" {
    Properties {
        _Albedo ("Albedo", Color) = (1, 1, 1)
        _TerrainTex ("Terrain Texture", 2D) = "white" {}
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "white" {}
        _TessellationEdgeLength ("Tessellation Edge Length", Range(1, 100)) = 50
        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "Height Map" {}
        _DisplacementStrength ("Displacement Strength", Range(0.1, 200)) = 5
        _NormalStrength ("Normals Strength", Range(0.0, 10)) = 1
    }

    CGINCLUDE
        float _TessellationEdgeLength;
        float _DisplacementStrength;

        sampler2D _HeightMap;
        float4 _HeightMap_TexelSize;

        struct TessellationFactors {
            float edge[3] : SV_TESSFACTOR;
            float inside : SV_INSIDETESSFACTOR;
        };

        float TessellationHeuristic(float3 cp0, float3 cp1) {
            //return 1.0f;
            float edgeLength = distance(cp0, cp1);
            float3 edgeCenter = (cp0 + cp1) * 0.5;
            float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

            return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * (viewDistance * 0.5));
        }
        bool TriangleIsBelowClipPlane(float3 p0, float3 p1, float3 p2, int planeIndex, float bias) {
            float4 plane = unity_CameraWorldClipPlanes[planeIndex];

            return dot(float4(p0, 1), plane) < bias && dot(float4(p1, 1), plane) < bias && dot(float4(p2, 1), plane) < bias;
        }

        bool cullTriangle(float3 p0, float3 p1, float3 p2, float bias) {
            return TriangleIsBelowClipPlane(p0, p1, p2, 0, bias) ||
                   TriangleIsBelowClipPlane(p0, p1, p2, 1, bias) ||
                   TriangleIsBelowClipPlane(p0, p1, p2, 2, bias) ||
                   TriangleIsBelowClipPlane(p0, p1, p2, 3, -_DisplacementStrength);
        }
    ENDCG

    SubShader {
        Pass {
            Tags {
                "LightMode" = "ForwardBase"
            }

            CGPROGRAM
            
            #pragma target 5.0

            #define SHADOWS_SCREEN
            
            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            
            #pragma vertex dummyvp
            #pragma hull hp
            #pragma domain dp
            #pragma geometry gp
            #pragma fragment fp

            sampler2D _TerrainTex, _NormalMap;
            float4 _TerrainTex_TexelSize, _TerrainTex_ST;
            float3 _Albedo;
            float _NormalStrength;

            struct TessellationControlPoint {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            struct VertexData {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2g {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 shadowCoords : TEXCOORD1;
            };

            TessellationControlPoint dummyvp(VertexData v) {
                TessellationControlPoint p;
                p.vertex = v.vertex;
                p.normal = v.normal;
                p.uv = v.uv;
                p.tangent = v.tangent;

                return p;
            }

            v2g vp(VertexData v) {
                v2g g;
                
                float displacement = tex2Dlod(_HeightMap, float4(v.uv, 0, 0)).r;
                float height = displacement * _DisplacementStrength;
                
                v.vertex.y = height;

                g.pos = UnityObjectToClipPos(v.vertex);
                g.normal = UnityObjectToWorldNormal(normalize(v.normal));
                g.shadowCoords = v.vertex;
                g.shadowCoords.xy = (float2(g.pos.x, -g.pos.y) + g.pos.w) * 0.5;
                g.shadowCoords.zw = g.pos.zw;
                g.uv = v.uv;

                return g;
            }

            struct g2f {
                v2g data;
                float2 barycentricCoordinates : TEXCOORD9;
            };

            TessellationFactors PatchFunction(InputPatch<TessellationControlPoint, 3> patch) {
                float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex);
                float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex);
                float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex);

                TessellationFactors f;
                float bias = -0.5 * _DisplacementStrength;
                if (cullTriangle(p0, p1, p2, bias)) {
                    f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
                } else {
                    f.edge[0] = TessellationHeuristic(p1, p2);
                    f.edge[1] = TessellationHeuristic(p2, p0);
                    f.edge[2] = TessellationHeuristic(p0, p1);
                    f.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p1, p2)) * (1 / 3.0);
                }
                return f;
            }

            [UNITY_domain("tri")]
            [UNITY_outputcontrolpoints(3)]
            [UNITY_outputtopology("triangle_cw")]
            [UNITY_partitioning("integer")]
            [UNITY_patchconstantfunc("PatchFunction")]
            TessellationControlPoint hp(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OUTPUTCONTROLPOINTID) {
                return patch[id];
            }

            [maxvertexcount(3)]
            void gp(triangle v2g g[3], inout TriangleStream<g2f> stream) {
                g2f g0, g1, g2;
                g0.data = g[0];
                g1.data = g[1];
                g2.data = g[2];

                g0.barycentricCoordinates = float2(1, 0);
                g1.barycentricCoordinates = float2(0, 1);
                g2.barycentricCoordinates = float2(0, 0);

                stream.Append(g0);
                stream.Append(g1);
                stream.Append(g2);
            }

            #define DP_INTERPOLATE(fieldName) data.fieldName = \
                data.fieldName = patch[0].fieldName * barycentricCoordinates.x + \
                                 patch[1].fieldName * barycentricCoordinates.y + \
                                 patch[2].fieldName * barycentricCoordinates.z;               

            [UNITY_domain("tri")]
            v2g dp(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DOMAINLOCATION) {
                VertexData data;
                DP_INTERPOLATE(vertex)
                DP_INTERPOLATE(normal)
                DP_INTERPOLATE(tangent)
                DP_INTERPOLATE(uv)

                return vp(data);
            }

            float3 fp(g2f f) : SV_TARGET {
                float3 col = tex2D(_TerrainTex, f.data.uv * _TerrainTex_ST.xy).rgb;
                col = pow(col, 1.5f);
                
                float3 normal;
                normal.xy = tex2D(_NormalMap, f.data.uv * _TerrainTex_ST.xy).wy * 2 - 1;
                normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                normal = normal.xzy;

                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float attenuation = tex2D(_ShadowMapTexture, f.data.shadowCoords.xy / f.data.shadowCoords.w);

                float2 du = float2(_HeightMap_TexelSize.x * 0.5, 0);
                float u1 = tex2D(_HeightMap, f.data.uv - du);
                float u2 = tex2D(_HeightMap, f.data.uv + du);

                float2 dv = float2(0, _HeightMap_TexelSize.y * 0.5);
                float v1 = tex2D(_HeightMap, f.data.uv - dv);
                float v2 = tex2D(_HeightMap, f.data.uv + dv);

                float3 centralDifference = float3(u1 - u2, 1, v1 - v2);
                centralDifference = normalize(centralDifference);

                normal += centralDifference;
                normal.xz *= _NormalStrength;
                normal = normalize(float3(normal.x, 1, normal.z));

                float ndotl = DotClamped(lightDir, normal);
                
                return col * _Albedo * attenuation * ndotl;
            }

            ENDCG
        }

        Pass {
            Tags {
                "LightMode" = "ShadowCaster"
            }

            CGPROGRAM
            
            #pragma target 5.0

            #include "UnityStandardBRDF.cginc"
			#include "UnityStandardUtils.cginc"

            #pragma vertex dummyvp
            #pragma hull hp
            #pragma domain dp
            #pragma fragment fp

            struct ShadowTessControlPoint {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct VertexData {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            ShadowTessControlPoint dummyvp(VertexData v) {
                ShadowTessControlPoint p;
                p.vertex = v.vertex;
                p.normal = v.normal;
                p.uv = v.uv;

                return p;
            };

            v2f vp(VertexData v) {
                v2f f;
                float displacement = tex2Dlod(_HeightMap, float4(v.uv.xy, 0, 0)).r;
                displacement = displacement * _DisplacementStrength;
                v.normal = normalize(v.normal);
                v.vertex.y = displacement;

                f.pos = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
                f.pos = UnityApplyLinearShadowBias(f.pos);
                f.uv = v.uv;

                return f;
            }

            TessellationFactors PatchFunction(InputPatch<ShadowTessControlPoint, 3> patch) {
                float3 p0 = mul(unity_ObjectToWorld, patch[0].vertex);
                float3 p1 = mul(unity_ObjectToWorld, patch[1].vertex);
                float3 p2 = mul(unity_ObjectToWorld, patch[2].vertex);

                TessellationFactors f;
                float bias = -0.5 * _DisplacementStrength;
                if (cullTriangle(p0, p1, p2, bias)) {
                    f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
                } else {
                    f.edge[0] = TessellationHeuristic(p1, p2);
                    f.edge[1] = TessellationHeuristic(p2, p0);
                    f.edge[2] = TessellationHeuristic(p0, p1);
                    f.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p1, p2)) * (1 / 3.0);
                }
                return f;
            }

            [UNITY_domain("tri")]
            [UNITY_outputcontrolpoints(3)]
            [UNITY_outputtopology("triangle_cw")]
            [UNITY_partitioning("integer")]
            [UNITY_patchconstantfunc("PatchFunction")]
            ShadowTessControlPoint hp(InputPatch<ShadowTessControlPoint, 3> patch, uint id : SV_OUTPUTCONTROLPOINTID) {
                return patch[id];
            }

            #define DP_INTERPOLATE(fieldName) data.fieldName = \
                data.fieldName = patch[0].fieldName * barycentricCoordinates.x + \
                                 patch[1].fieldName * barycentricCoordinates.y + \
                                 patch[2].fieldName * barycentricCoordinates.z;

            [UNITY_domain("tri")]
            v2f dp(TessellationFactors factors, OutputPatch<ShadowTessControlPoint, 3> patch, float3 barycentricCoordinates : SV_DOMAINLOCATION) {
                VertexData data;
                DP_INTERPOLATE(vertex)
                DP_INTERPOLATE(normal)
                DP_INTERPOLATE(uv)

                return vp(data);
            }

            half4 fp() : SV_TARGET {
                return 0;
            }

            ENDCG
        }
    }
}
