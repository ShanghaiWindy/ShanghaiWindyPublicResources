Shader "Hidden/MicroVerse/TreeSplatMod"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.jbooth.microverse/Scripts/Shaders/SplatMerge.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _IndexMap;
            sampler2D _WeightMap;
            float4 _IndexMap_TexelSize;
            float _Index;
            sampler2D _TreeSDF;
            float _RealHeight;
            float _Amount;
            float _Width;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            FragmentOutput frag (v2f i)
            {
                float sdf = tex2D(_TreeSDF, i.uv).r * 256;

                float w = 1.0 - saturate(sdf / _Width);

                w = smoothstep(0, 1, w);

                half4 indexes = tex2D(_IndexMap, i.uv) * 32;
                half4 weights = tex2D(_WeightMap, i.uv);

                w *= _Amount;

                // reduce weights of other textures by the amount of this texture

                weights *= 1.0 - w;
                FragmentOutput o = FilterSplatWeights(w, weights, indexes, _Index);
                o.indexMap /= 32;

                return o;
            }
            ENDCG
        }
    }
}
