using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [System.Serializable]
    public class FilterSet : ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize(){}

        public void OnAfterDeserialize()
        {
            if (version == 0)
            {
                slopeFilter.range *= 1.57894736842f;
                slopeFilter.smoothness *= 1.57894736842f;
            }
            version = 1;
        }
        public FalloffFilter falloffFilter = new FalloffFilter();

        [Range(0, 1)] public float weight = 1;
        public Noise weightNoise = new Noise();
        public Noise weight2Noise = new Noise();
        public Noise weight3Noise = new Noise();

        public enum NoiseOp
        {
            Add,
            Subtract,
            Multiply,
            Overlay
        }

        public NoiseOp weight2NoiseOp = NoiseOp.Add;
        public NoiseOp weight3NoiseOp = NoiseOp.Add;


        [System.Serializable]
        public class Filter
        {
            public Filter(Vector2 range, Vector2 smoothness)
            {
                this.range = range;
                this.smoothness = smoothness;
            }
            public bool enabled;
            [Range(0, 1)]
            public float weight = 1;
            public Vector2 range = new Vector2(0, 1);
            public Vector2 smoothness = new Vector2(1, 1);
            public Noise noise = new Noise();
        }
        public int version = 0;

        public Filter heightFilter = new Filter(new Vector2(0, 500), new Vector2(20, 20));
        public Filter slopeFilter = new Filter(new Vector2(0, 18), new Vector2(4, 4));
        public Filter angleFilter = new Filter(new Vector2(0, 90), new Vector2(12, 12));
        public Filter curvatureFilter = new Filter(new Vector2(0.6f, 1), new Vector2(0.1f, 0.1f));
 
        public bool NeedCurvatureMap() { return (curvatureFilter.enabled); }

        [System.Serializable]
        public class TextureFilter
        {
            public TerrainLayer layer;
            [Range(0, 1)] public float weight;
        }
        public bool textureFilterEnabled;
        [Range(0,1)] public float otherTextureWeight = 1;
        public List<TextureFilter> textureFilters = new List<TextureFilter>();

        public float[] GetTextureWeights(TerrainLayer[] layers)
        {
            var weights = new float[32];
            for (int i = 0; i < 32; ++i) weights[i] = 1.0f - otherTextureWeight;

            for (int i = 0; i < layers.Length; ++i)
            {
                foreach (var tf in textureFilters)
                {
                    if (ReferenceEquals(tf.layer, layers[i]))
                    {
                        weights[i] = 1.0f - tf.weight;
                    }
                }
            }
            return weights;
        }

       

        static int _Transform = Shader.PropertyToID("_Transform");
        static int _RealSize = Shader.PropertyToID("_RealSize");
        static int _Weight = Shader.PropertyToID("_Weight");
        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");
        static int _WeightNoise = Shader.PropertyToID("_WeightNoise");
        static int _WeightNoise2 = Shader.PropertyToID("_WeightNoise2");

        static int _WeightNoiseChannel = Shader.PropertyToID("_WeightNoiseChannel");
        static int _WeightNoiseTexture = Shader.PropertyToID("_WeightNoiseTexture");
        static int _Weight2Noise = Shader.PropertyToID("_Weight2Noise");
        static int _Weight2Noise2 = Shader.PropertyToID("_Weight2Noise2");
        static int _Weight2NoiseTexture = Shader.PropertyToID("_Weight2NoiseTexture");
        static int _Weight2NoiseChannel = Shader.PropertyToID("_Weight2NoiseChannel");
        static int _Weight3Noise = Shader.PropertyToID("_Weight3Noise");
        static int _Weight3Noise2 = Shader.PropertyToID("_Weight3Noise2");
        static int _Weight3NoiseTexture = Shader.PropertyToID("_Weight3NoiseTexture");
        static int _Weight3NoiseChannel = Shader.PropertyToID("_Weight3NoiseChannel");
        static int _Weight2NoiseOp = Shader.PropertyToID("_Weight2NoiseOp");
        static int _Weight3NoiseOp = Shader.PropertyToID("_Weight3NoiseOp");

        static int _HeightWeight = Shader.PropertyToID("_HeightWeight");
        static int _HeightRange = Shader.PropertyToID("_HeightRange");
        static int _HeightSmoothness = Shader.PropertyToID("_HeightSmoothness");
        static int _HeightNoise = Shader.PropertyToID("_HeightNoise");
        static int _HeightNoise2 = Shader.PropertyToID("_HeightNoise");
        static int _HeightNoiseTexture = Shader.PropertyToID("_HeightNoiseTexture");
        static int _HeightNoiseChannel = Shader.PropertyToID("_HeightNoiseChannel");
        static int _SlopeWeight = Shader.PropertyToID("_SlopeWeight");
        static int _SlopeRange = Shader.PropertyToID("_SlopeRange");
        static int _SlopeSmoothness = Shader.PropertyToID("_SlopeSmoothness");
        static int _SlopeNoise = Shader.PropertyToID("_SlopeNoise");
        static int _SlopeNoise2 = Shader.PropertyToID("_SlopeNoise2");
        static int _SlopeNoiseTexture = Shader.PropertyToID("_SlopeNoiseTexture");
        static int _SlopeNoiseChannel = Shader.PropertyToID("_SlopeNoiseChannel");
        static int _AngleWeight = Shader.PropertyToID("_AngleWeight");
        static int _AngleRange = Shader.PropertyToID("_AngleRange");
        static int _AngleSmoothness = Shader.PropertyToID("_AngleSmoothness");
        static int _AngleNoise = Shader.PropertyToID("_AngleNoise");
        static int _AngleNoise2 = Shader.PropertyToID("_AngleNoise2");
        static int _AngleNoiseTexture = Shader.PropertyToID("_AngleNoiseTexture");
        static int _AngleNoiseChannel = Shader.PropertyToID("_AngleNoiseChannel");

        static int _CurvatureWeight = Shader.PropertyToID("_CurvatureWeight");
        static int _CurvatureRange = Shader.PropertyToID("_CurvatureRange");
        static int _CurvatureSmoothness = Shader.PropertyToID("_CurvatureSmoothness");
        static int _CurvatureNoise = Shader.PropertyToID("_CurvatureNoise");
        static int _CurvatureNoise2 = Shader.PropertyToID("_CurvatureNoise2");
        static int _CurvatureNoiseTexture = Shader.PropertyToID("_CurvatureNoiseTexture");
        static int _CurvatureNoiseChannel = Shader.PropertyToID("_CurvatureNoiseChannel");
        
        public void PrepareMaterial(Transform transform, Terrain terrain, Material material, List<string> keywords)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FilterSet::PrepareMaterial");
            falloffFilter.PrepareMaterial(material, terrain, transform, keywords);

            var terrainData = terrain.terrainData;
            var realHeight = terrainData.heightmapScale.y * 2;
            material.SetMatrix(_Transform, TerrainUtil.ComputeStampMatrix(terrain, transform)); ;
            material.SetVector(_RealSize, TerrainUtil.ComputeTerrainSize(terrain));
            

            var noisePos = terrain.transform.position;
            noisePos.x /= terrain.terrainData.size.x;
            noisePos.z /= terrain.terrainData.size.z;

            material.SetVector(_NoiseUV, new Vector2(noisePos.x, noisePos.z));

            material.SetFloat(_Weight, weight);
            if (weightNoise.noiseType != Noise.NoiseType.None)
            {
                material.SetVector(_WeightNoise, weightNoise.GetParamVector());
                material.SetVector(_WeightNoise2, weightNoise.GetParam2Vector());
                material.SetTexture(_WeightNoiseTexture, weightNoise.texture);
                material.SetTextureOffset(_WeightNoiseTexture, weightNoise.GetTextureParams());
                material.SetFloat(_WeightNoiseChannel, (int)weightNoise.channel);
            }
            if (weight2Noise.noiseType != Noise.NoiseType.None)
            {
                material.SetVector(_Weight2Noise, weight2Noise.GetParamVector());
                material.SetVector(_Weight2Noise2, weight2Noise.GetParam2Vector());
                material.SetTexture(_Weight2NoiseTexture, weight2Noise.texture);
                material.SetTextureOffset(_Weight2NoiseTexture, weight2Noise.GetTextureParams());
                material.SetFloat(_Weight2NoiseChannel, (int)weight2Noise.channel);
                material.SetFloat(_Weight2NoiseOp, (int)weight2NoiseOp);
            }
            if (weight3Noise.noiseType != Noise.NoiseType.None)
            {
                material.SetVector(_Weight3Noise, weight3Noise.GetParamVector());
                material.SetVector(_Weight3Noise2, weight3Noise.GetParam2Vector());
                material.SetTexture(_Weight3NoiseTexture, weight3Noise.texture);
                material.SetTextureOffset(_Weight3NoiseTexture, weight3Noise.GetTextureParams());
                material.SetFloat(_Weight3NoiseChannel, (int)weight3Noise.channel);

            }


            if (heightFilter.enabled)
            {
                material.SetFloat(_HeightWeight, heightFilter.weight);
                material.SetVector(_HeightRange, heightFilter.range / realHeight);
                material.SetVector(_HeightSmoothness, heightFilter.smoothness / realHeight);
                material.SetVector(_HeightNoise, heightFilter.noise.GetParamVector());
                material.SetVector(_HeightNoise2, heightFilter.noise.GetParam2Vector());
                material.SetTexture(_HeightNoiseTexture, heightFilter.noise.texture);
                material.SetTextureOffset(_HeightNoiseTexture, heightFilter.noise.GetTextureParams());
                material.SetFloat(_HeightNoiseChannel, (int)heightFilter.noise.channel);
            }
            if (slopeFilter.enabled)
            {
                material.SetFloat(_SlopeWeight, slopeFilter.weight);
                material.SetVector(_SlopeRange, slopeFilter.range * Mathf.Deg2Rad);
                material.SetVector(_SlopeSmoothness, slopeFilter.smoothness * Mathf.Deg2Rad);
                material.SetVector(_SlopeNoise, slopeFilter.noise.GetParamVector());
                material.SetVector(_SlopeNoise2, slopeFilter.noise.GetParam2Vector());
                material.SetTexture(_SlopeNoiseTexture, slopeFilter.noise.texture);
                material.SetTextureOffset(_SlopeNoiseTexture, slopeFilter.noise.GetTextureParams());
                material.SetFloat(_SlopeNoiseChannel, (int)slopeFilter.noise.channel);
            }
            if (angleFilter.enabled)
            {
                material.SetFloat(_AngleWeight, angleFilter.weight);
                material.SetVector(_AngleRange, angleFilter.range * Mathf.Deg2Rad);
                material.SetVector(_AngleSmoothness, angleFilter.smoothness * Mathf.Deg2Rad);
                material.SetVector(_AngleNoise, angleFilter.noise.GetParamVector());
                material.SetVector(_AngleNoise2, angleFilter.noise.GetParam2Vector());
                material.SetTexture(_AngleNoiseTexture, angleFilter.noise.texture);
                material.SetTextureOffset(_AngleNoiseTexture, angleFilter.noise.GetTextureParams());
                material.SetFloat(_AngleNoiseChannel, (int)angleFilter.noise.channel);
            }

            if (curvatureFilter.enabled)
            {
                material.SetFloat(_CurvatureWeight, curvatureFilter.weight);
                material.SetVector(_CurvatureRange, curvatureFilter.range);
                material.SetVector(_CurvatureSmoothness, curvatureFilter.smoothness);
                material.SetVector(_CurvatureNoise, curvatureFilter.noise.GetParamVector());
                material.SetVector(_CurvatureNoise2, curvatureFilter.noise.GetParam2Vector());
                material.SetTexture(_CurvatureNoiseTexture, curvatureFilter.noise.texture);
                material.SetTextureOffset(_CurvatureNoiseTexture, curvatureFilter.noise.GetTextureParams());
                material.SetFloat(_CurvatureNoiseChannel, (int)curvatureFilter.noise.channel);
            }

            if (heightFilter.enabled)
            {
                keywords.Add("_HEIGHTFILTER");
                heightFilter.noise.EnableKeyword(material, "_HEIGHT", keywords);
            }
            if (slopeFilter.enabled)
            {
                keywords.Add("_SLOPEFILTER");
                slopeFilter.noise.EnableKeyword(material, "_SLOPE", keywords);
            }
            if (angleFilter.enabled)
            {
                keywords.Add("_ANGLEFILTER");
                angleFilter.noise.EnableKeyword(material, "_ANGLE", keywords);
            }
            if (curvatureFilter.enabled)
            {
                keywords.Add("_CURVATUREFILTER");
                curvatureFilter.noise.EnableKeyword(material, "_CURVATURE", keywords);
            }
            if (textureFilterEnabled)
            {
                keywords.Add("_TEXTUREFILTER");
            }

            weightNoise.EnableKeyword(material, "_WEIGHT", keywords);
            weight2Noise.EnableKeyword(material, "_WEIGHT2", keywords);
            weight3Noise.EnableKeyword(material, "_WEIGHT3", keywords);
            UnityEngine.Profiling.Profiler.EndSample();

        }

        static int _PlacementSDF = Shader.PropertyToID("_PlacementSDF");
        static int _PlacementSDF2 = Shader.PropertyToID("_PlacementSDF2");
        static int _PlacementSDF3 = Shader.PropertyToID("_PlacementSDF3");
        static int _DistancesFromTrees = Shader.PropertyToID("_DistancesFromTrees");
        static int _DistancesFromObject = Shader.PropertyToID("_DistancesFromObject");
        static int _DistancesFromParent = Shader.PropertyToID("_DistancesFromParent");
        static int _SDFClamp = Shader.PropertyToID("_SDFClamp");

        public static void PrepareSDFFilter(Material material, Transform transform,
                                     OcclusionData od, float ratio, bool sdfClamp,
                                     float minTree, float maxTree,
                                     float minObj, float maxObj,
                                     float minParent, float maxParent
                                     
            )
        {
            // because people think they have to specify a range, we unclamp the top of the range
            // so they get infinite if they move it to max.
            if (maxTree >= 255)
                maxTree = minTree;
            if (maxObj >= 255)
                maxObj = minObj;
            if (maxParent >= 255)
                maxParent = minParent;

            if (minTree > 0 || maxTree > 0)
            {
                material.SetVector(_DistancesFromTrees, new Vector2(minTree * ratio, maxTree * ratio));
                material.SetTexture(_PlacementSDF, od.treeSDF);
            }
            if (minObj > 0 || maxObj > 0)
            {
                material.SetVector(_DistancesFromObject, new Vector2(minObj * ratio, maxObj * ratio));
                material.SetTexture(_PlacementSDF2, od.objectSDF);
            }

            if (minParent > 0 || maxParent > 0 && transform.parent != null)
            {
                var parent = transform.parent.GetComponentInParent<ISpawner>(false);
                if (parent != null)
                {
                    var sdf3 = parent.GetSDF(od.terrain);
                    if (sdf3 != null)
                    {
                        material.SetVector(_DistancesFromParent, new Vector2(minParent * ratio, maxParent * ratio));
                        material.SetTexture(_PlacementSDF3, sdf3);
                    }
                }
            }
            material.SetFloat(_SDFClamp, sdfClamp ? 1.0f : 0);
        }


    }
}