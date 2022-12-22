Shader "Hidden/MicroVerse/SplinePathTexture"
{
    Properties
    {
        [HideInInspector]_WeightMap ("Data Texture", 2D) = "white" {}
        [HideInInspector]_IndexMap ("Data Texture", 2D) = "white" {}
        [HideInInspector]_SplineSDF("Spline SDF", 2D) = "black" {}
        [HideInInspector]_SplatNoiseTexture("Noise", 2D) = "grey" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _EMBANKMENT

            #pragma shader_feature_local_fragment _ _BLENDSMOOTHSTEP _BLENDEASEOUT _BLENDEASEIN _BLENDEASEINOUT _BLENDEASEINOUTELASTIC
            #pragma shader_feature_local _ _SPLATNOISE _SPLATFBM _SPLATWORLEY _SPLATWORM _SPLATWORMFBM _SPLATNOISETEXTURE


            #include "UnityCG.cginc"

            struct vertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            #include "Packages/com.jbooth.microverse/Scripts/Shaders/SplatMerge.cginc"
            #include "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"

            sampler2D _WeightMap;
            sampler2D _IndexMap;
            sampler2D _SplineSDF;
            sampler2D _SplatNoiseTexture;
            float4 _SplatNoiseTexture_ST;
            float _SplatNoiseChannel;
            float4 _SplineSDF_TexelSize;


            float _Width;
            float _Smoothness;
            float _Channel;
            float _EmbankmentChannel;
            float _HeightWidth;
            float _HeightSmoothness;
            float4 _NoiseParams;
            float2 _NoiseUV;
            
            float InverseLerp(float a, float b, float t)
            {
                return ((t - a) / max(0.001, (b - a)));
            }

            float Blend(float width, float smoothness, float d)
            {
                #if _BLENDSMOOTHSTEP
                    return 1.0 - smoothstep(width, width + smoothness, d);
                #else
                    float v = saturate(InverseLerp(width, width + smoothness, d));
                    #if _BLENDEASEOUT
                        v = 1.0 - (1-v) * (1-v);
                    #elif _BLENDEASEIN
                        v = v * v;
                    #elif _BLENDEASEINOUT
                        v = v < 0.5 ? 2 * v * v : 1 - pow(-2 * v + 2, 2) * 0.5;
                    #elif _BLENDEASEINOUTELASTIC
                        float c5 = (2 * 3.14159265359) / 4.5;
                        return v == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? -(pow(2, 20 * v - 10) *
                            sin((20 * v - 11.125) * c5)) / 2
                            : (pow(2, -20 * v + 10) * sin((20 * v - 11.125) * c5)) / 2 + 1;
                    #endif

                    
                    return 1 - v;
                #endif
                
            }


            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            FragmentOutput frag(v2f i)
            {
                half4 weightMap = tex2D(_WeightMap, i.uv);

                float totalWeight = weightMap.x + weightMap.y + weightMap.z + weightMap.w;

                //clip(totalWeight >= 1 ? -1 : 1);

                half4 indexMap = tex2D(_IndexMap, i.uv) * 32;
                float2 data = tex2D(_SplineSDF, i.uv).xy;



                float result = Blend(_Width, _Smoothness, data.g);
                float embank = Blend(_HeightWidth, _HeightSmoothness, data.g);

                #if _SPLATNOISE
                   result *= 1 + Noise(i.uv + _NoiseUV, _NoiseParams);
                #elif _SPLATFBM
                   result *= 1 + NoiseFBM(i.uv + _NoiseUV, _NoiseParams);
                #elif _SPLATWORLEY
                   result *= 1 + NoiseWorley(i.uv + _NoiseUV, _NoiseParams);
                #elif _SPLATWORM
                   result *= 1 + NoiseWorm(i.uv + _NoiseUV, _NoiseParams);
                #elif _SPLATWORMFBM
                   result *= 1 + NoiseWormFBM(i.uv + _NoiseUV, _NoiseParams);
                #elif _SPLATTEXTURE
                   result *= 1 + (tex2D(_SplatNoiseTexture, (i.uv + _NoiseUV) * _SplatNoiseTexture_ST.xy + _SplatNoiseTexture_ST.zw)[_SplatNoiseChannel] * 2.0 - 1.0) * _NoiseParams.y + _NoiseParams.w;
                #endif

                float area = result;
                float ch = _Channel;

                // this is pretty basic, basically we just take the greater area
                // and choose the texture. No blending controls- but with a height
                // map shader this should look nice and not show the jaggies
                // the crap BiRP unity shader shows.

                #if _EMBANKMENT
                    area = max(result, embank);
                    if (result + 0.1 < embank)
                        ch = _EmbankmentChannel;
                #endif
                
                FragmentOutput o = FilterSplatWeights(area, weightMap, indexMap, ch);
                o.indexMap /= 32;
                return o;
            }
            ENDCG
        }
    }
}