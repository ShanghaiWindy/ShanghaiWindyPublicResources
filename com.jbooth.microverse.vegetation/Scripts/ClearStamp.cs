using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;

namespace JBooth.MicroVerseCore
{
    
    [ExecuteAlways]
    public class ClearStamp : Stamp, ITreeModifier, IDetailModifier
    {
        public bool clearTrees = true;
        public bool clearDetails = true;

        public FilterSet filterSet = new FilterSet();

        float[] textureLayerWeights;

        Material material;

        public bool NeedCurvatureMap() { return filterSet.NeedCurvatureMap(); }

        public bool NeedTreeClear() { return clearTrees; }
        public bool NeedDetailClear() { return clearDetails; }

        public override FilterSet GetFilterSet()
        {
            return filterSet;
        }

        public Bounds GetBounds()
        {
            FalloffOverride fo = GetComponentInParent<FalloffOverride>();
            var foType = filterSet.falloffFilter.filterType;
            if (fo != null)
            {
                foType = fo.filter.filterType;
            }
#if __MICROVERSE_SPLINES__
            if (foType == FalloffFilter.FilterType.SplineArea && filterSet.falloffFilter.splineArea != null)
            {
                return filterSet.falloffFilter.splineArea.GetBounds();
            }
#endif
            if (foType == FalloffFilter.FilterType.Global)
                return new Bounds(Vector3.zero, new Vector3(99999, 999999, 99999));
            else
            {
                return TerrainUtil.GetBounds(transform);
            }
        }

        public bool OccludesOthers()
        {
            return false;
        }

        public bool NeedSDF()
        {
            return false;
        }

        public bool UsesOtherTreeSDF() { return false; }
        public bool UsesOtherObjectSDF() { return false; }
        public bool NeedParentSDF() { return false; }
        public bool NeedToGenerateSDFForChilden() { return false; }
        public void SetSDF(Terrain t, RenderTexture rt) { }
        public RenderTexture GetSDF(Terrain t) { return null; }

        static Shader clearShader = null;
        public void Initialize(Terrain[] terrains)
        {
            if (clearShader == null)
            {
                clearShader = Shader.Find("Hidden/MicroVerse/ClearFilter");
            }
            material = new Material(clearShader);
            
        }

        public void InqTreePrototypes(List<TreePrototypeSerializable> trees)
        {
            
        }

        static int _Heightmap = Shader.PropertyToID("_Heightmap");
        static int _Normalmap = Shader.PropertyToID("_Normalmap");
        static int _Curvemap = Shader.PropertyToID("_Curvemap");

        public void ApplyTreeClear(TreeData td)
        {
            if (!clearTrees)
                return;
            keywordBuilder.Clear();
            keywordBuilder.Add("_RECONSTRUCTNORMAL");
            textureLayerWeights = filterSet.GetTextureWeights(td.terrain.terrainData.terrainLayers);
            material.SetTexture(_Heightmap, td.heightMap);
            material.SetTexture(_Normalmap, td.normalMap);
            material.SetTexture(_Curvemap, td.curveMap);
            filterSet.PrepareMaterial(this.transform, td.terrain, material, keywordBuilder.keywords);
            keywordBuilder.Assign(material);

            RenderTexture temp = RenderTexture.GetTemporary(td.treeClearMap.descriptor);
            material.SetFloat("_LayerIndex", td.layerIndex);
            temp.name = "TreeClear";
            Graphics.Blit(td.treeClearMap, temp, material);
            RenderTexture.ReleaseTemporary(td.treeClearMap);
            td.treeClearMap = temp;
            td.layerIndex++;
        }

        public void ApplyDetailClear(DetailData dd)
        {
            if (!clearDetails)
                return;

            keywordBuilder.Clear();
            keywordBuilder.Add("_RECONSTRUCTNORMAL");
            textureLayerWeights = filterSet.GetTextureWeights(dd.terrain.terrainData.terrainLayers);
            material.SetTexture(_Heightmap, dd.heightMap);
            material.SetTexture(_Normalmap, dd.normalMap);
            material.SetTexture(_Curvemap, dd.curveMap);
            filterSet.PrepareMaterial(this.transform, dd.terrain, material, keywordBuilder.keywords);
            keywordBuilder.Assign(material);

            RenderTexture temp = RenderTexture.GetTemporary(dd.clearMap.descriptor);
            material.SetFloat("_LayerIndex", dd.layerIndex);
            temp.name = "DetailClear";
            Graphics.Blit(dd.clearMap, temp, material);
            RenderTexture.ReleaseTemporary(dd.clearMap);
            dd.clearMap = temp;
            dd.layerIndex++;
        }


        public void ApplyTreeStamp(TreeData td, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
            td.layerIndex++;
        }

        
        public void ProcessTreeStamp(TreeData vd, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
            
        }

        public void Dispose()
        {
            DestroyImmediate(material);
            material = null;
        }


        void OnDrawGizmosSelected()
        {
            if (filterSet.falloffFilter.filterType != FalloffFilter.FilterType.Global &&
                filterSet.falloffFilter.filterType != FalloffFilter.FilterType.SplineArea)
            {
                if (MicroVerse.instance != null)
                {
                    Gizmos.color = MicroVerse.instance.options.colors.treeStampColor;
                    var pos = transform.position;
                    Vector3 size = transform.lossyScale;
                    pos.y += size.y / 2;
                    Gizmos.DrawWireCube(pos, size);
                }
            }
        }


        public void ApplyDetailStamp(DetailData dd, Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers, OcclusionData od)
        {
            dd.layerIndex++;
        }

        public void InqDetailPrototypes(List<DetailPrototypeSerializable> prototypes)
        {
            
        }

        
    }
}
