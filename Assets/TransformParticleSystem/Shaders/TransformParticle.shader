// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "PortalWithNreal/TransformParticle"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct TransformParticle
            {
                int isActive;
                int targetId;
                float2 uv;

                float3 targetPosition;

                float speed;
                float3 position;

                int useTexture;
                float scale;

                float4 velocity;

                float3 horizontal;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                int useTex : TEXCOORD2;
            };

            StructuredBuffer<TransformParticle> _Particles;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _BaseScale;

            #define PI 3.1415926535

            UNITY_DECLARE_TEX2DARRAY(_Textures);

            float rand(float x)
            {
                return frac(sin(x) * 43758.5453);
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed3 rotate(fixed3 p, fixed3 rotation)
            {
                fixed3 a = normalize(rotation);
                fixed angle = length(rotation);

                if (abs(angle) < 0.001)
                {
                    return p;
                }

                fixed s = sin(angle);
                fixed c = cos(angle);
                fixed r = 1.0 - c;
                fixed3x3 m = fixed3x3(
                    a.x * a.x * r + c,
                    a.y * a.x * r + a.z * s,
                    a.z * a.x * r - a.y * s,
                    a.x * a.y * r - a.z * s,
                    a.y * a.y * r + c,
                    a.z * a.y * r + a.x * s,
                    a.x * a.z * r + a.y * s,
                    a.y * a.z * r - a.x * s,
                    a.z * a.z * r + c
                );
                return mul(m, p);
            }

            v2f vert(appdata v, uint id : SV_InstanceID)
            {
                TransformParticle p = _Particles[id];

                v2f o;

                float s = _BaseScale * p.scale * p.isActive;

                fixed r = 2.0 * (rand(p.targetPosition.xy) - 0.5);
                fixed3 r3 = fixed3(r, r, r) * (PI * 0.5 + _Time.y) * (p.speed * 0.1) * (1 - p.useTexture);
                v.vertex.xyz = rotate(v.vertex.xyz, r3);

                v.normal = rotate(v.normal, r3);

                v.vertex.xyz = (v.vertex.xyz * s) + p.position;

                o.vertex = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1.0));
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv.xy = p.uv.xy;
                o.uv.z = p.targetId;
                o.useTex = p.useTexture;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;

                if (i.useTex == 1)
                {
                    col = UNITY_SAMPLE_TEX2DARRAY(_Textures, i.uv);
                    col = pow(col, 2.2);
                }
                else
                {
                    float diff = clamp(dot(i.normal, normalize(float3(0.1, -1.0, 0))), 0.05, 0.8);
                    col = diff.xxxx;
                }

                return col;
            }
            ENDCG
        }
    }
}
