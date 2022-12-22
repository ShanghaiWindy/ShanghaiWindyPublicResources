using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [System.Serializable]
    public class FalloffFilter
    {
        public enum FilterType
        {
            Global = 0,
            Box,
            Range,
            Texture,
            SplineArea,
        }

        public enum FilterTypeNoGlobal
        {
            Box,
            Range,
            Texture,
            SplineArea,
        }

        public enum TextureChannel
        {
            R = 0,
            G,
            B,
            A
        }

        public FilterType filterType;
        public Texture2D texture;
        public TextureChannel textureChannel = TextureChannel.R;
        public Vector2 textureParams = new Vector2(1, 0); // amplitude, balance
        public Vector4 textureRotationScale = new Vector4(0, 1, 0, 0);


#if __MICROVERSE_SPLINES__
        public SplineArea splineArea;
        public float splineAreaFalloff;
        public float splineAreaFalloffBoost;
#endif


        public Easing easing = new Easing();
        public Noise noise = new Noise();

        public Vector2 falloffRange = new Vector2(0.8f, 1.0f);


        static int _Falloff = Shader.PropertyToID("_Falloff");
        static int _FalloffTexture = Shader.PropertyToID("_FalloffTexture");
        static int _FalloffTextureChannel = Shader.PropertyToID("_FalloffTextureChannel");
        static int _FalloffTextureParams = Shader.PropertyToID("_FalloffTextureParams");
        static int _FalloffTextureRotScale = Shader.PropertyToID("_FalloffTextureRotScale");
        static int _FalloffAreaRange = Shader.PropertyToID("_FalloffAreaRange");
        static int _FalloffAreaBoost = Shader.PropertyToID("_FalloffAreaBoost");

        public void PrepareMaterial(Material mat, Terrain terrain, Transform transform, List<string> keywords)
        {
            FalloffOverride fo = transform.GetComponentInParent<FalloffOverride>();
            FalloffFilter useFilter = this;
            if (fo != null)
            {
                useFilter = fo.filter;
            }

            if (useFilter.filterType != FilterType.Global)
            {
                easing.PrepareMaterial(mat, "_FALLOFF", keywords);
                noise.PrepareMaterial(mat, "_FALLOFF", "_Falloff", keywords);
            }


            if (useFilter.filterType == FilterType.Box)
            {
                keywords.Add("_USEFALLOFF");
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
            else if (useFilter.filterType == FilterType.Range)
            {
                keywords.Add("_USEFALLOFFRANGE");
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
            else if  (useFilter.filterType == FilterType.Texture)
            {
                keywords.Add("_USEFALLOFFTEXTURE");
                mat.SetTexture(_FalloffTexture, useFilter.texture);
                mat.SetFloat(_FalloffTextureChannel, (int)useFilter.textureChannel);
                mat.SetVector(_FalloffTextureParams, useFilter.textureParams);
                mat.SetVector(_FalloffTextureRotScale, useFilter.textureRotationScale);
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
#if __MICROVERSE_SPLINES__
            else if (useFilter.filterType == FilterType.SplineArea && useFilter.splineArea != null)
            {
                keywords.Add("_USEFALLOFFSPLINEAREA");
                mat.SetTexture(_FalloffTexture, useFilter.splineArea.GetSDF(terrain));
                mat.SetFloat(_FalloffAreaRange, useFilter.splineAreaFalloff);
                mat.SetFloat(_FalloffAreaBoost, useFilter.splineAreaFalloffBoost);
            }
#else
            else if (filterType == FilterType.SplineArea)
            {
                keywords.Add("_USEFALLOFFRANGE");
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
#endif
        }
    }
}
