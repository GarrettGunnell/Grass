Shader "Unlit/GeometryGrass" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Height("Grass Height", float) = 3
		_Width("Grass Width", range(0, 0.1)) = 0.05
    }

    SubShader {
        Cull Off
        Zwrite On

        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp
            #pragma geometry gp
            
            #pragma target 4.5

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "../Resources/Random.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g {
                float4 vertex : SV_POSITION;
            };

            struct g2f {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Height, _Width;

            v2g vp(VertexData v) {
                v2g o;

                o.vertex = v.vertex;
                
                return o;
            }

            [maxvertexcount(30)]
            void gp(point v2g points[1], inout TriangleStream<g2f> triStream) {
                int i;
                float4 root = points[0].vertex;

                const int vertexCount = 12;

                g2f v[vertexCount];

                for (i = 0; i < vertexCount; ++i) {
                    v[i].vertex = 0.0f;
                }

                float currentV = 0;
                float offsetV = 1.0f / ((vertexCount / 2) - 1);

                float currentHeightOffset = 0;
                float currentVertexHeight = 0;

                for (i = 0; i < vertexCount; ++i) {

                    float widthMod = 1.0f - float(i) / float(vertexCount);
                    widthMod = pow(widthMod * widthMod, 1.0f / 3.0f);
                    
                    if (i % 2 == 0) {
                        v[i].vertex = float4(root.x - (_Width * widthMod), root.y + currentVertexHeight, root.z, 1);
                    } else {
                        v[i].vertex = float4(root.x + (_Width * widthMod), root.y + currentVertexHeight, root.z, 1);

                        currentV += offsetV;
                        currentVertexHeight = currentV * _Height;
                    }

                    v[i].vertex = UnityObjectToClipPos(v[i].vertex);
                }

                for (i = 0; i < vertexCount - 2; ++i) {
                    triStream.Append(v[i]);
                    triStream.Append(v[i + 2]);
                    triStream.Append(v[i + 1]);
                }
            }

            fixed4 fp(g2f i) : SV_Target {
                return 1.0f;
            }

            ENDCG
        }
    }
}
