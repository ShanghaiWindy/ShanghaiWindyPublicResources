Shader "Hidden/MicroVerse/RasterToTerrain"
{
    Properties
    {
        [HideInInspector]
        _Weights ("Weights", 2D) = "black" {}
        _Indexes ("indexes", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ _MAPCOUNT2 _MAPCOUNT3 _MAPCOUNT4 _MAPCOUNT5 _MAPCOUNT6 _MAPCOUNT7 _MAPCOUNT8

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

            sampler2D _Weights;
            sampler2D _Indexes;

            #if _MAPCOUNT2
                #define NUMCHANNELS 8
            #elif _MAPCOUNT3
                #define NUMCHANNELS 12
            #elif _MAPCOUNT4
                #define NUMCHANNELS 16
            #elif _MAPCOUNT5
                #define NUMCHANNELS 20
            #elif _MAPCOUNT6
                #define NUMCHANNELS 24
            #elif _MAPCOUNT7
                #define NUMCHANNELS 28
            #elif _MAPCOUNT8
                #define NUMCHANNELS 32
            #else
                #define NUMCHANNELS 4
            #endif

            struct FragmentOutput
            {
                half4 splat0 : SV_Target0;
                #if _MAPCOUNT2 || _MAPCOUNT3 || _MAPCOUNT4 || _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    half4 splat1 : SV_Target1;
                #endif
                #if _MAPCOUNT3 || _MAPCOUNT4 || _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    half4 splat2 : SV_Target2;
                #endif
                #if _MAPCOUNT4 || _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    half4 splat3 : SV_Target3;
                #endif
                #if _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    half4 splat4 : SV_Target4;
                #endif
                #if _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    half4 splat5 : SV_Target5;
                #endif
                #if  _MAPCOUNT7 || _MAPCOUNT8
                    half4 splat6 : SV_Target6;
                #endif
                #if _MAPCOUNT8
                    half4 splat7 : SV_Target7;
                #endif
            };
            

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            FragmentOutput frag(v2f i)
            {
                half4 weights = tex2D(_Weights, i.uv);
                half4 indexes = round(tex2D(_Indexes, i.uv) * 32);
                half allW[NUMCHANNELS];

                // clear weights to 0
                for (int i = 0; i < NUMCHANNELS; ++i) allW[i] = 0;
                allW[indexes.w] = weights.w;
                allW[indexes.z] = weights.z;
                allW[indexes.y] = weights.y;
                allW[indexes.x] = weights.x;

                // total one
                half totalW = 0;
                for (int i = 0; i < NUMCHANNELS; ++i) totalW += allW[i];
                for (int i = 0; i < NUMCHANNELS; ++i) allW[i] /= totalW;
             

                FragmentOutput o;
                o.splat0 = half4(allW[0], allW[1], allW[2], allW[3]);
                #if _MAPCOUNT2 || _MAPCOUNT3 || _MAPCOUNT4 || _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    o.splat1 = half4(allW[4], allW[5], allW[6], allW[7]);
                #endif
                #if _MAPCOUNT3 || _MAPCOUNT4 || _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    o.splat2 = half4(allW[8], allW[9], allW[10], allW[11]);
                #endif
                #if _MAPCOUNT4 || _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    o.splat3 = half4(allW[12], allW[13], allW[14], allW[15]);
                #endif
                #if _MAPCOUNT5 || _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    o.splat4 = half4(allW[16], allW[17], allW[18], allW[19]);
                #endif
                #if _MAPCOUNT6 || _MAPCOUNT7 || _MAPCOUNT8
                    o.splat5 = half4(allW[20], allW[21], allW[22], allW[23]);
                #endif
                #if  _MAPCOUNT7 || _MAPCOUNT8
                    o.splat6 = half4(allW[24], allW[25], allW[26], allW[27]);
                #endif
                #if _MAPCOUNT8
                    o.splat7 = half4(allW[28], allW[29], allW[30], allW[31]);
                #endif


                return o;

            }
            ENDCG
        }
    }
}