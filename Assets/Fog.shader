Shader "Hidden/EdgeDetect" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {

        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            #include "UnityCG.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vp(VertexData v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex, _CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;

            fixed4 fp(v2f i) : SV_Target {
                int x, y;
                fixed4 col = tex2D(_MainTex, i.uv);
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                depth = Linear01Depth(depth);

                float viewDistance = depth * _ProjectionParams.z;

                float density = 0.05f;

                float fogFactor = (density / log(2)) * viewDistance;
                fogFactor = exp2(-fogFactor);

                return lerp(float4(0.5f, 0.5f, 0.5f, 1), col, saturate(fogFactor));
            }
            ENDCG
        }
    }
}