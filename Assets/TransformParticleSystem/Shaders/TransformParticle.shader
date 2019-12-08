Shader "TPS/TransformParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct TransformParticle
            {
                int id;
                int targetId;
                float2 uv;

                int isActive;
                float3 targetPosition;

                float speed;
                float3 position;

                float4 color;

                int useTexture;
                float3 scale;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : TEXCOORD1;
                int texid : TEXCOORD2;
                int useTex : TEXCOORD3;
            };

            StructuredBuffer<TransformParticle> _Particles;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _BaseScale;

            UNITY_DECLARE_TEX2DARRAY(_Textures);

            v2f vert (appdata v, uint id : SV_InstanceID)
            {
                TransformParticle p = _Particles[id];

                v2f o;

                float3 s = _BaseScale * p.scale;// * p.isActive;
                v.vertex.xyz = (v.vertex.xyz * s) + p.position;

                o.vertex = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1.0));
                o.uv.xy = p.uv.xy;
                o.uv.z = p.targetId;
                o.color = p.color;
                o.texid = p.targetId;
                o.useTex = p.useTexture;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;
                
                if (i.useTex == 1)
                {
                    col = UNITY_SAMPLE_TEX2DARRAY(_Textures, i.uv);
                    col = pow(col, 1.5);
                }
                else
                {
                    col = i.color;
                }

                return col;
            }
            ENDCG
        }
    }
}
