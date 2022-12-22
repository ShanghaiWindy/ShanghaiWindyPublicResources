using UnityEngine;
using System.Collections.Generic;

namespace JBooth.MicroVerseCore
{
    

    [ExecuteAlways]
    public class HeightStamp : Stamp, IHeightModifier
    {
        public enum CombineMode
        {
            Override = 0,
            Max = 1,
            Min = 2,
            Add = 3,
            Subtract = 4,
            Multiply = 5,
            Average = 6,
            Difference = 7,
            SqrtMultiply = 8,
        }

        public Texture2D stamp;
        public CombineMode mode = CombineMode.Max;

        public FalloffFilter falloff = new FalloffFilter();

        [Tooltip("Twists the stamp around the Y axis")]
        [Range(-90, 90)] public float twist = 0;
        [Tooltip("Erodes the slopes of the terrain")]
        [Range(0, 600)] public float erosion = 0;
        [Tooltip("Controls the scale of the erosion effect")]
        [Range(1, 90)] public float erosionSize = 4;

        public Vector2 remapRange = new Vector2(0, 1);
        public Vector4 scaleOffset = new Vector4(1, 1, 0, 0);
        [Range(0, 6)] public float mipBias = 0;

        public Material material { get; private set; }

        public void Dispose()
        {
            DestroyImmediate(material);
        }

        [SerializeField] int version = 0;
        public override void OnEnable()
        {
            if (version == 0 && mode == CombineMode.Max)
            {
                var pos = transform.position;
                pos.y = 0;
                transform.position = pos;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
            }
            else if (version == 1 && mode != HeightStamp.CombineMode.Override && mode != HeightStamp.CombineMode.Max)
            {
                var pos = transform.position;
                pos.y = 0;
                transform.position = pos;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
            }
            base.OnEnable();
            version = 2;
        }

        static Shader heightmapShader = null;

        public void Initialize(Terrain[] terrains)
        {
            if (stamp != null)
            {
                stamp.wrapMode = TextureWrapMode.Clamp;
            }
            if (heightmapShader == null)
            {
                heightmapShader = Shader.Find("Hidden/MicroVerse/HeightmapStamp");
            }
            material = new Material(heightmapShader);
        }

        public Bounds GetBounds()
        {
            FalloffOverride fo = GetComponentInParent<FalloffOverride>();
            var foType = falloff.filterType;
            if (fo != null)
            {
                foType = fo.filter.filterType;
            }
            

#if __MICROVERSE_SPLINES__
            if (foType == FalloffFilter.FilterType.SplineArea && falloff.splineArea != null)
            {
                return falloff.splineArea.GetBounds();
            }
#endif
            return TerrainUtil.GetBounds(transform);
        }

        static int _AlphaMapSize = Shader.PropertyToID("_AlphaMapSize");
        static int _PlacementMask = Shader.PropertyToID("_PlacementMask");
        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");

        // used by copy paste stamp
        public bool ApplyHeightStampAbsolute(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od, Vector2 heightRenorm)
        {
            material.SetVector("_HeightRenorm", heightRenorm);
            keywordBuilder.Clear();
            keywordBuilder.Add("_ABSOLUTEHEIGHT");
            PrepareMaterial(material, heightmapData, keywordBuilder.keywords);
            material.SetFloat(_AlphaMapSize, source.width);
            material.SetTexture(_PlacementMask, od.terrainMask);
            var noisePos = heightmapData.terrain.transform.position;
            noisePos.x /= heightmapData.terrain.terrainData.size.x;
            noisePos.z /= heightmapData.terrain.terrainData.size.z;

            material.SetVector(_NoiseUV, new Vector2(noisePos.x, noisePos.z));

            keywordBuilder.Assign(material);

            Graphics.Blit(source, dest, material);
            return true;
        }

        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od)
        {
            keywordBuilder.Clear();
            PrepareMaterial(material, heightmapData, keywordBuilder.keywords);
            material.SetFloat(_AlphaMapSize, source.width);
            material.SetTexture(_PlacementMask, od.terrainMask);
            var noisePos = heightmapData.terrain.transform.position;
            noisePos.x /= heightmapData.terrain.terrainData.size.x;
            noisePos.z /= heightmapData.terrain.terrainData.size.z;

            material.SetVector(_NoiseUV, new Vector2(noisePos.x, noisePos.z));

            keywordBuilder.Assign(material);

            Graphics.Blit(source, dest, material);
            return true;
        }

        static int _Transform = Shader.PropertyToID("_Transform");
        static int _RealSize = Shader.PropertyToID("_RealSize");
        static int _StampTex = Shader.PropertyToID("_StampTex");
        static int _MipBias = Shader.PropertyToID("_MipBias");
        static int _RemapRange = Shader.PropertyToID("_RemapRange");
        static int _ScaleOffset = Shader.PropertyToID("_ScaleOffset");
        static int _HeightRemap = Shader.PropertyToID("_HeightRemap");
        static int _CombineMode = Shader.PropertyToID("_CombineMode");
        static int _Twist = Shader.PropertyToID("_Twist");
        static int _Erosion = Shader.PropertyToID("_Erosion");
        static int _ErosionSize = Shader.PropertyToID("_ErosionSize");

        void PrepareMaterial(Material material, HeightmapData heightmapData, List<string> keywords)
        {
            var localPosition = heightmapData.WorldToTerrainMatrix.MultiplyPoint3x4(transform.position);
            var size = transform.lossyScale;

            material.SetMatrix(_Transform, TerrainUtil.ComputeStampMatrix(heightmapData.terrain, transform)); ;
            material.SetVector(_RealSize, TerrainUtil.ComputeTerrainSize(heightmapData.terrain));
            
            if (stamp != null)
            {
                stamp.wrapMode = (scaleOffset == new Vector4(1,1,0,0)) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            }
            material.SetTexture(_StampTex, stamp);
            material.SetFloat(_MipBias, mipBias);
            material.SetVector(_RemapRange, remapRange);
            material.SetVector(_ScaleOffset, scaleOffset);
            
            falloff.PrepareMaterial(material, heightmapData.terrain, transform, keywords);
           

            var y = localPosition.y;

            material.SetVector(_HeightRemap, new Vector2(y, y + size.y) / heightmapData.RealHeight);
            material.SetInt(_CombineMode, (int)mode);

            if (twist != 0)
            {
                keywords.Add("_TWIST");
                material.SetFloat(_Twist, twist);
            }
            if (erosion != 0)
            {
                keywords.Add("_EROSION");
                material.SetFloat(_Erosion, erosion);
                material.SetFloat(_ErosionSize, erosionSize);
            }
            

        }

        void OnDrawGizmosSelected()
        {
            if (MicroVerse.instance != null)
            {
                Gizmos.color = MicroVerse.instance.options.colors.heightStampColor;
                var pos = transform.position;
                Vector3 size = transform.lossyScale;
                pos.y += size.y/2;
                Gizmos.DrawWireCube(pos, size);
            }
        }
    }
}