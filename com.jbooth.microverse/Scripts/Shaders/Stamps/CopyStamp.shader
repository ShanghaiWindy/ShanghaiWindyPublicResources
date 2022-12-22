Shader "Hidden/MicroVerse/CopyStamp"
{
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


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
                float2 stampUV: TEXCOORD1;
            };

            float2 _UVCenter;
            float2 _UVRange;
            sampler2D _Source;
            
            sampler2D _CurrentBuffer;

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.stampUV = lerp(_UVCenter - _UVRange, _UVCenter + _UVRange, v.uv);
                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                float4 current = tex2D(_CurrentBuffer, i.uv);
                float4 source = tex2D(_Source, i.stampUV);
                bool inside = (i.stampUV.x > 0 && i.stampUV.x < 1 && i.stampUV.y > 0 && i.stampUV.y < 1);

                return inside ? source : current;
                
            }
            ENDCG
        }
    }
}