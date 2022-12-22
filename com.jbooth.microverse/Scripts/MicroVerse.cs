
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;

namespace JBooth.MicroVerseCore
{

    [ExecuteAlways]
    public partial class MicroVerse : MonoBehaviour
    {
        public Options options = new Options();

        public delegate void TerrainLayersChanged(TerrainLayer[] newLayers);
        public static event TerrainLayersChanged OnTerrainLayersChanged;

        public static UnityEngine.Events.UnityEvent OnFinishedUpdating = new UnityEngine.Events.UnityEvent();
        public static UnityEngine.Events.UnityEvent OnBeginUpdating = new UnityEngine.Events.UnityEvent();
        public static UnityEngine.Events.UnityEvent OnCancelUpdating = new UnityEngine.Events.UnityEvent();

        [Tooltip("You can use this list to explicitly set the terrains instead of having them parented under the MicroVerse object")]
        public Terrain[] explicitTerrains;
        Terrain[] _terrains;
        public Terrain[] terrains
        {
            get
            {
                if (explicitTerrains != null && explicitTerrains.Length > 0)
                {
                    foreach (var t in explicitTerrains)
                    {
                        if (t == null)
                            return _terrains;
                    }
                    return explicitTerrains;
                }
                else
                {
                    return _terrains;
                }
            }
            private set { _terrains = value; }
        }

#if __MICROVERSE_MASKS__
        [Tooltip("Used by the mask module to capture the various buffers that MicroSplat uses and use them elsewhere in your project")]
        public BufferCaptureTarget bufferCaptureTarget;
#endif

        static MicroVerse _instance = null;

#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
        SpawnProcessor spawnProcessor;
#endif


        public static MicroVerse instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                _instance = FindObjectOfType<MicroVerse>();
                return _instance;
            }
        }

        private void Awake()
        {
            _instance = this;
            SyncTerrainList();
        }

        /// <summary>
        /// This is called to make sure the list of MicroVerse
        /// terrains is up to date.
        /// </summary>
        public void SyncTerrainList()
        {
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            if (spawnProcessor == null)
                spawnProcessor = new SpawnProcessor();
#endif

            if (explicitTerrains != null && explicitTerrains.Length > 0)
            {
                bool valid = true;
                foreach (var t in explicitTerrains)
                {
                    if (t == null) { valid = false; break; }
                    if (t.drawInstanced == false)
                        t.drawInstanced = true;
#if UNITY_2022_2_OR_NEWER
                    if (t.terrainData.detailScatterMode != DetailScatterMode.CoverageMode)
                        t.terrainData.SetDetailScatterMode(DetailScatterMode.CoverageMode);
#endif
                }
                if (valid)
                    return;
            }
            terrains = GetComponentsInChildren<Terrain>();
            if (terrains.Length > 0)
            {
                // make sure draw instance is on, we're stupidly slow
                // without it because unity forces CPU readbacks.

                foreach (var t in terrains)
                {
                    if (t.drawInstanced == false)
                        t.drawInstanced = true;
#if UNITY_2022_2_OR_NEWER
                    if (t.terrainData.detailScatterMode != DetailScatterMode.CoverageMode)
                        t.terrainData.SetDetailScatterMode(DetailScatterMode.CoverageMode);
#endif
                }
            }

        }

        // don't update more than once per frame.. We can't delay updating,
        // because if we do spline motion isn't smooth

        public enum InvalidateType
        {
            All,
            Splats,
            Tree
        }

        private InvalidateType invalidateType = InvalidateType.All;

        bool needUpdate = false;

        /// <summary>
        /// This gets called to request MicroVerse to update the terrain
        /// but it will only execute once per frame.
        /// </summary>
        /// <param name="type"></param>
        public void Invalidate(InvalidateType type = InvalidateType.All)
        {
            if (!needUpdate)
                invalidateType = type;
            else
            {
                if (invalidateType != type)
                    invalidateType = InvalidateType.All;
            }
            needUpdate = true;
        }

        void Update()
        {
            if (Application.isPlaying)
            {
                if (needUpdate)
                {
                    needUpdate = false;
                    Modify();
                }
#if __MICROVERSE_VEGETATION__
                spawnProcessor.ApplyTrees();
                spawnProcessor.ApplyDetails();
#endif
#if __MICROVERSE_OBJECTS__
                spawnProcessor.ApplyObjects();
#endif
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
                spawnProcessor.CheckDone();
#endif
            }
            else if (heightSavebackRequested)
            {
                RequestHeightSaveback();
            }
        }
        public void LateUpdate()
        {
            if (IsModifyingTerrain)
            {
                bool mod = false;
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
                if (SpawnProcessor.IsModifyingTerrain)
                    mod = true;
#endif
                if (!mod)
                {
                    IsModifyingTerrain = false;
                }
            }
        }

        bool firstUpdate = false;
        private void OnEnable()
        {
            firstUpdate = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += OnEditorUpdate;
            UnityEditor.Selection.selectionChanged += OnSelectionChange;
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
#endif
            if (!Application.isPlaying)
            {
                SyncTerrainList();
                //Modify(true); // causes issues when entering play mode, since it gets fired them
            }

        }

        

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= OnEditorUpdate;
            UnityEditor.Selection.selectionChanged -= OnSelectionChange;
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
#endif
        }

        public bool IsHeightSyncd { get; private set; }
        private bool _isModifyingTerrain = false;
        public bool IsModifyingTerrain {
            get { return _isModifyingTerrain; }
            set
            {
                var old = _isModifyingTerrain;
                _isModifyingTerrain = value;
                if (value == false && old == true && OnFinishedUpdating != null)
                {
                    OnFinishedUpdating.Invoke();
                }
            }
        }
        

        bool heightSavebackRequested = false;
        /// <summary>
        /// This gets called when the height has been changed by something
        /// to sync the data back from the GPU to the CPU for physics and such
        /// </summary>
        public void RequestHeightSaveback()
        {
            if (!IsHeightSyncd)
            {
                
                heightSavebackRequested = false;
                Profiler.BeginSample("Sync Height Map back to CPU");
                SyncTerrainList();

                foreach (var terrain in terrains)
                {
                    terrain.terrainData.SyncHeightmap();
                }
                IsHeightSyncd = true;
                Profiler.EndSample();
            }
        }
        /// <summary>
        /// Save everything back to the terrain, which is slow, because unity
        /// stores alpha maps as arrays instead of textures and uses a
        /// bloated 4 weights per texture format.
        /// </summary>
        public void SaveBackToTerrain()
        {
            Profiler.BeginSample("SyncBackTerrain");
            SyncTerrainList();
            
            foreach (var terrain in terrains)
            {
                terrain.terrainData.SyncTexture(TerrainData.AlphamapTextureName);
                terrain.terrainData.SyncHeightmap();
            }
            IsHeightSyncd = true;
            Profiler.EndSample();
        }


        bool DoTerrainLayersMatch(TerrainLayer[] a, TerrainLayer[] b)
        {
            if (a.Length != b.Length) { return false; }
            for (int i = 0; i < a.Length; ++i)
            {
                if (!ReferenceEquals(a[i], b[i]))
                    return false;

            }
            return true;
        }

        /// <summary>
        /// Syncs terrain layers across all terrains and lets external
        /// programs know if they've been changed.
        /// </summary>
        /// <param name="splatmapModifiers"></param>
        void SanatizeTerrainLayers(List<ITextureModifier> splatmapModifiers)
        {
            List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
            foreach (var terrain in terrains)
            {
                foreach (var sm in splatmapModifiers)
                {
                    sm.InqTerrainLayers(terrain, terrainLayers);
                }
            }
            terrainLayers.RemoveAll(item => item == null);
            var allLayers = terrainLayers.Distinct().OrderBy(x=>x?.name).ToArray();
            
            bool needsUpdate = false;
            foreach (var terrain in terrains)
            {
                if (!DoTerrainLayersMatch(allLayers, terrain.terrainData.terrainLayers))
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.terrainLayers = allLayers;
                }
            }
            if (OnTerrainLayersChanged != null)
                OnTerrainLayersChanged.Invoke(allLayers);

        }


        public class DataCache
        {
            public Dictionary<Terrain, RenderTexture> heightMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> normalMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, OcclusionData> occlusionDatas = new Dictionary<Terrain, OcclusionData>();
            public Dictionary<Terrain, RenderTexture> indexMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> weightMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> curvatureMaps = new Dictionary<Terrain, RenderTexture>();
#if __MICROVERSE_VEGETATION__
            public Dictionary<Terrain, TreeData> treeDatas = new Dictionary<Terrain, TreeData>();
            public Dictionary<Terrain, DetailData> detailDatas = new Dictionary<Terrain, DetailData>();
#endif
#if __MICROVERSE_OBJECTS__
            public Dictionary<Terrain, ObjectData> objectDatas = new Dictionary<Terrain, ObjectData>();
#endif

        }

        static ComputeShader heightSeemShader = null;

        public static bool noAsyncReadback { get; private set; }
        /// <summary>
        /// This is the actual function that does updates to the terrain.
        /// If you call it directly it will update all the height/splat maps
        /// immediately, but tree's and details are deferred due to Unity's
        /// terrible terrain API and GPU readback. You can request it
        /// to write immediately back to the CPU in the case of height and
        /// splats, which will make it slower, or force no async readbacks
        /// which will make it complete immediately but be really slow
        /// </summary>
        /// <param name="writeToCPU"></param>
        public void Modify(bool writeToCPU = false, bool noAsync = false)
        {
            noAsyncReadback = noAsync;
            if (!enabled)
            {
                return;
            }
            if (terrains.Length == 0)
            {
                return;
            }

            IsModifyingTerrain = true;
            CancelModify();
            if (OnBeginUpdating != null)
                OnBeginUpdating.Invoke();

            
            Profiler.BeginSample("MicroVerse::Modify Terrain");
            Profiler.BeginSample("Sync Terrain List");
            IsHeightSyncd = false;
            SyncTerrainList();
            Profiler.EndSample();
            Profiler.BeginSample("Init");
            Profiler.BeginSample("Find Stamps");

            var allModifiers = new List<IModifier>(GetComponentsInChildren<IModifier>());
            var heightmapModifiers = new List<IHeightModifier>(64);
            var splatmapModifiers = new List<ITextureModifier>(64);
            // filtering is faster than finding them. Note that when MS is enabled we
            // want textures to remain the same when objects are disabled, so we
            // have to scan in that case.
            if (IsUsingMicroSplat())
            {
                for (int i = 0; i < allModifiers.Count; ++i)
                {
                    var m = allModifiers[i];
                    if (m is IHeightModifier)
                    {
                        heightmapModifiers.Add(m as IHeightModifier);
                    }
                }
                GetComponentsInChildren<ITextureModifier>(true, splatmapModifiers);
            }
            else
            {
                for (int i = 0; i < allModifiers.Count; ++i)
                {
                    var m = allModifiers[i];
                    if (m is IHeightModifier)
                    {
                        heightmapModifiers.Add(m as IHeightModifier);
                    }
                    if (m is ITextureModifier)
                    {
                        splatmapModifiers.Add(m as ITextureModifier);
                    }
                }
            }

            // remove all with disabled components, this lets us
            // have meta-modifiers which pipe through to disabled components.
            heightmapModifiers.RemoveAll(p => p.IsEnabled() == false);
            
            allModifiers.RemoveAll(p => p.IsEnabled() == false);
            allModifiers = allModifiers.Distinct().ToList();
            Profiler.EndSample();
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            spawnProcessor.InitSystem();
#endif

            Profiler.BeginSample("Modify::InitModifiers");
            foreach (var m in allModifiers) { m.Initialize(terrains); }
            Profiler.EndSample();

            if (options.settings.keepLayersInSync || IsUsingMicroSplat())
            {
                Profiler.BeginSample("Modify::SanitizeTerrainLayers");
                SanatizeTerrainLayers(splatmapModifiers);
                Profiler.EndSample();
            }
            else
            {
                Profiler.BeginSample("Modify::GetLayers");
                foreach (var terrain in terrains)
                {
                    List<TerrainLayer> terrainLayers = new List<TerrainLayer>();

                    var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                    foreach (var sm in splatmapModifiers)
                    {
                        if (terrainBounds.Intersects(sm.GetBounds()))
                        {
                            sm.InqTerrainLayers(terrain, terrainLayers);
                        }
                    }
                    terrain.terrainData.terrainLayers = terrainLayers.Distinct().ToArray();
                }
                Profiler.EndSample();
            }
            // we strip these after getting the terrain layers. This lets you "reserve"
            // textures in an array based shader, means the layers don't get removed when
            // you toggle a component on and off (requiring an array rebuild), but may
            // mean people leaving disabled components around end up with more textures
            // on their terrain.
            splatmapModifiers.RemoveAll(p => p.IsEnabled() == false);

            // if we need curvature at all, we need it for all due to boundaries
            bool needCurvatureMap = false;
#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (bufferCaptureTarget != null && bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CurvatureMap))
            {
                needCurvatureMap = true;
            }
#endif

            Profiler.BeginSample("Modify::Scan Modifiers for Flags");
            // Grab all the assets needed from the modifiers
            // and make sure the data is on the terrain
            foreach (var terrain in terrains)
            {
                List<TerrainLayer> terrainLayers = new List<TerrainLayer>();

                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                foreach (var sm in splatmapModifiers)
                {
                    needCurvatureMap |= sm.NeedCurvatureMap();
                }

#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
                Profiler.BeginSample("Modify::SpawnProcessor::Init");
                spawnProcessor.InitTerrain(terrain, invalidateType, ref needCurvatureMap);
                Profiler.EndSample();
#endif

            }
            Profiler.EndSample(); // scan
            Profiler.EndSample(); // init
            Profiler.BeginSample("Modify::HeightMaps");
            DataCache dataCache = new DataCache();
            int heightMapRes = terrains[0].terrainData.heightmapResolution;
            int splatMapRes = terrains[0].terrainData.alphamapResolution;
            var maskSize = heightMapRes - 1;
            if (splatMapRes > maskSize)
                maskSize = splatMapRes;

            int odSize = maskSize;
            
            if (odSize > 1024)
                odSize = 1024;
            if (odSize < 512)
                odSize = 512;

            
            // do height maps
            foreach (var terrain in terrains)
            {
                var hmd = new HeightmapData(terrain);
                Vector3 rs = new Vector3(hmd.RealSize.x, hmd.RealHeight, hmd.RealSize.y);
                var tbs = terrain.terrainData.bounds;
                tbs.center = terrain.transform.position;
                tbs.center += new Vector3(tbs.size.x * 0.5f, 0, tbs.size.z * 0.5f);
                var od = new OcclusionData(terrain, odSize);
                dataCache.occlusionDatas.Add(terrain, od);
                dataCache.heightMaps.Add(terrain, GenerateHeightmap(hmd, heightmapModifiers, tbs, od));
            }
            Profiler.EndSample();
            // generate normals
            Profiler.BeginSample("Modify::GenerateNormals");
            foreach (var terrain in terrains)
            {
                dataCache.normalMaps.Add(terrain, MapGen.GenerateNormalMap(terrain, dataCache.heightMaps, splatMapRes, splatMapRes));
            }
            Profiler.EndSample();
            
            foreach (var terrain in terrains)
            {
                Profiler.BeginSample("Modify::CurveMaps");
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                var occlusionData = dataCache.occlusionDatas[terrain];
                var heightmapData = new HeightmapData(terrain);
                Vector3 realSize = new Vector3(heightmapData.RealSize.x, heightmapData.RealHeight, heightmapData.RealSize.y);

                RenderTexture curvatureGen = needCurvatureMap ? MapGen.GenerateCurvatureMap(terrain, dataCache.normalMaps, splatMapRes, splatMapRes) : null;
                dataCache.curvatureMaps[terrain] = curvatureGen;
                Profiler.EndSample();
                Profiler.BeginSample("Modify::SplatMaps");
                var heightmapGen = dataCache.heightMaps[terrain];
                var normalGen = dataCache.normalMaps[terrain];

                var splatmapData = new TextureData(terrain, 0, heightmapGen, normalGen, curvatureGen);
                GenerateSplatmaps(splatmapData, splatmapModifiers, terrainBounds, occlusionData);
                dataCache.indexMaps[terrain] = splatmapData.indexMap;
                dataCache.weightMaps[terrain] = splatmapData.weightMap;
                Profiler.EndSample();

            }

#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (needCurvatureMap && bufferCaptureTarget != null &&
                bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CurvatureMap))
            {
                foreach (var terrain in terrains)
                {
                    var nm = dataCache.curvatureMaps[terrain];
                    bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.CurvatureMap, nm);
                }
            }
#endif


#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            spawnProcessor.GenerateSpawnables(terrains, dataCache);

#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (bufferCaptureTarget != null)
            {
                if (bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CombinedTreeSDF))
                {
                    foreach (var terrain in terrains)
                    {
                        var nm = dataCache.occlusionDatas[terrain]?.treeSDF;
                        if (nm != null)
                            bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.CombinedTreeSDF, nm);
                    }
                }
                if (bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CombinedOcclusionMask))
                {
                    foreach (var terrain in terrains)
                    {
                        var nm = dataCache.occlusionDatas[terrain]?.terrainMask;
                        if (nm != null)
                            bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.CombinedOcclusionMask, nm);
                    }
                }
            }
#endif // UNITY_EDITOR && __MICROVERSE_MASKS__

#endif // vegetation

#if __MICROVERSE_MASKS__
            MaskData.ProcessMasks(terrains, dataCache);
#endif
            Profiler.BeginSample("MicroVerse::Seemer");
            // Not a huge fan of this, but there are a lot of resolution dependent
            // issues that might cause tiny cracks in the terrain, so seem them up
            if (heightSeemShader == null)
            {
                heightSeemShader = (ComputeShader)Resources.Load("MicroVerseHeightSeemer");
            }
            foreach (var terrain in terrains)
            {
                if (terrain.leftNeighbor != null)
                {
                    int kernelHandle = heightSeemShader.FindKernel("CSLeft");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeemShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeemShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.leftNeighbor]);
                    heightSeemShader.SetInt("_Width", hm.width - 1);
                    heightSeemShader.SetInt("_Height", hm.height - 1);

                    heightSeemShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.height/512.0f), 1, 1);
                }
                if (terrain.rightNeighbor != null)
                {
                    int kernelHandle = heightSeemShader.FindKernel("CSRight");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeemShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeemShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.rightNeighbor]);
                    heightSeemShader.SetInt("_Width", hm.width - 1);
                    heightSeemShader.SetInt("_Height", hm.height - 1);

                    heightSeemShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.height / 512.0f), 1, 1);
                }
                if (terrain.topNeighbor != null)
                {
                    int kernelHandle = heightSeemShader.FindKernel("CSUp");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeemShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeemShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.topNeighbor]);
                    heightSeemShader.SetInt("_Width", hm.width - 1);
                    heightSeemShader.SetInt("_Height", hm.height - 1);

                    heightSeemShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.width / 512.0f), 1, 1);
                }
                if (terrain.bottomNeighbor != null)
                {
                    int kernelHandle = heightSeemShader.FindKernel("CSDown");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeemShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeemShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.bottomNeighbor]);
                    heightSeemShader.SetInt("_Width", hm.width - 1);
                    heightSeemShader.SetInt("_Height", hm.height - 1);

                    heightSeemShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.width / 512.0f), 1, 1);
                }
            }
            Profiler.EndSample();

            
            foreach (var terrain in terrains)
            {
                Profiler.BeginSample("Modify::Raster Splats");
                var indexMap = dataCache.indexMaps[terrain];
                var weightMap = dataCache.weightMaps[terrain];
                var heightmapGen = dataCache.heightMaps[terrain];
                var normalGen = dataCache.normalMaps[terrain];
                var curvatureGen = dataCache.curvatureMaps[terrain];
                var occlusionData = dataCache.occlusionDatas[terrain];
                RasterizeSplatMaps(terrain, indexMap, weightMap, writeToCPU);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(indexMap);
                RenderTexture.ReleaseTemporary(weightMap);
                Profiler.EndSample();

                Profiler.BeginSample("Modify::CopyHeightMapToTerrain");
                RenderTexture.active = heightmapGen;
                terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, heightmapGen.width, heightmapGen.height),
                    new Vector2Int(0, 0), writeToCPU ? TerrainHeightmapSyncControl.HeightAndLod : TerrainHeightmapSyncControl.None);

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(curvatureGen);
                RenderTexture.ReleaseTemporary(normalGen);
                RenderTexture.ReleaseTemporary(heightmapGen);
                Profiler.EndSample();

                occlusionData.Dispose();
            }
            
            foreach (var h in allModifiers) { h.Dispose(); }

            if (firstUpdate)
            {
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.SyncHeightmap();
                }   
                firstUpdate = false;
            }

#if !__MICROVERSE_VEGETATION__

            IsModifyingTerrain = false;
            if (OnFinishedUpdating != null)
            {
                OnFinishedUpdating.Invoke();
            }
#endif

            Profiler.EndSample(); // all terrains
            

        }

        /// <summary>
        /// stops a modify operation in progress. Note that it cannot
        /// stop the vegetation CPU readback once it's started, because
        /// you cannot cancel async GPU readbacks or jobs, so if you
        /// start/stop a bunch of times in a frame those will build up.
        /// </summary>
        public void CancelModify()
        {
            if (OnCancelUpdating != null)
                OnCancelUpdating.Invoke();
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            spawnProcessor.Cancel();
#endif


        }


        private static void GenerateSplatmaps(TextureData splatmapData,
            List<ITextureModifier> splatmapModifiers, Bounds terrainBounds, OcclusionData od, bool writeToCPU = false)
        {
            if (splatmapModifiers.Count == 0)
                return;
            if (od.terrain.terrainData.terrainLayers.Length == 0)
                return;
            // make sure we actually have layers on the terrain
            // we're going to generate instead of just, say, a
            // copy stamp.
            List<TerrainLayer> layers = new List<TerrainLayer>();
            foreach (var sm in splatmapModifiers)
            {
                sm.InqTerrainLayers(splatmapData.terrain, layers);
            }
            if (layers.Count == 0)
                return;

            TerrainData terrainData = splatmapData.terrain.terrainData;


            RenderTexture indexMap0 = RenderTexture.GetTemporary(terrainData.alphamapWidth,
                terrainData.alphamapHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture weightMap0 = RenderTexture.GetTemporary(terrainData.alphamapWidth,
                terrainData.alphamapHeight, 0, RenderTextureFormat.ARGB32);

            RenderTexture indexMap1 = RenderTexture.GetTemporary(terrainData.alphamapWidth,
                terrainData.alphamapHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture weightMap1 = RenderTexture.GetTemporary(terrainData.alphamapWidth,
                terrainData.alphamapHeight, 0, RenderTextureFormat.ARGB32);

            indexMap0.name = "MicroVerse::GenerateSplats::indexMap0";
            indexMap1.name = "MicroVerse::GenerateSplats::indexMap1";
            weightMap0.name = "MicroVerse::GenerateSplats::weightMap0";
            weightMap1.name = "MicroVerse::GenerateSplats::weightMap1";

            RenderTexture.active = indexMap0;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = weightMap0;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = null;

            indexMap0.filterMode = FilterMode.Point;
            indexMap1.filterMode = FilterMode.Point;
            weightMap0.filterMode = FilterMode.Point;
            weightMap1.filterMode = FilterMode.Point;
            indexMap0.wrapMode = TextureWrapMode.Clamp;
            indexMap1.wrapMode = TextureWrapMode.Clamp;
            weightMap0.wrapMode = TextureWrapMode.Clamp;
            weightMap1.wrapMode = TextureWrapMode.Clamp;

            for (int i = splatmapModifiers.Count - 1; i >= 0; --i)
            {
                var splatmapModifier = splatmapModifiers[i];
                var bounds = splatmapModifier.GetBounds();
                bool inBounds = (bounds.Intersects(terrainBounds));

                if (inBounds)
                {
                    if (splatmapModifier.ApplyTextureStamp(indexMap0, indexMap1, weightMap0, weightMap1, splatmapData, od))
                    {
                        (indexMap0, indexMap1) = (indexMap1, indexMap0);
                        (weightMap0, weightMap1) = (weightMap1, weightMap0);
                    }
                }
            }

            RenderTexture.ReleaseTemporary(weightMap1);
            RenderTexture.ReleaseTemporary(indexMap1);
            splatmapData.indexMap = indexMap0;
            splatmapData.weightMap = weightMap0;
            

        }

        static Material rasterToTerrain = null;
        void RasterizeSplatMaps(Terrain terrain, RenderTexture indexMap, RenderTexture weightMap, bool writeToCPU)
        {
            var count = terrain.terrainData.alphamapTextureCount;
            if (count == 0)
                return;
            Profiler.BeginSample("Convert to Unity Format");
            // now we have to rasterize to the terrain system.
            if (rasterToTerrain == null)
            {
                rasterToTerrain = new Material(Shader.Find("Hidden/MicroVerse/RasterToTerrain"));
            }
            rasterToTerrain.SetTexture("_Weights", weightMap);
            rasterToTerrain.SetTexture("_Indexes", indexMap);


            var _mrt = new RenderBuffer[count];
            var rts = new RenderTexture[count];

            switch (count)
            {
                case 2:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT2");
                    break;
                case 3:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT3");
                    break;
                case 4:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT4");
                    break;
                case 5:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT5");
                    break;
                case 6:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT6");
                    break;
                case 7:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT7");
                    break;
                case 8:
                    rasterToTerrain.EnableKeyword("_MAPCOUNT8");
                    break;
            }

            var t = terrain.terrainData.GetAlphamapTexture(0);
            for (int i = 0; i < count; ++i)
            {
                var rt = RenderTexture.GetTemporary(t.width, t.height, 0, t.graphicsFormat);
                rt.name = "MicroVerse:BackToTerrain";
                rt.filterMode = FilterMode.Point;
                rts[i] = rt;
                _mrt[i] = rt.colorBuffer;
            }
            Graphics.SetRenderTarget(_mrt, rts[0].depthBuffer);

            Graphics.Blit(null, rasterToTerrain, 0);
            Profiler.EndSample();
            Profiler.BeginSample("Copy Alphamap To Terrain");
            for (int i = 0; i < count; ++i)
            {
                RenderTexture.active = rts[i];
                terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, i,
                    new RectInt(0, 0, rts[i].width, rts[i].height), new Vector2Int(0, 0),
                    !writeToCPU);

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rts[i]);
            }
            
            _mrt = null;
            
            RenderTexture.active = null;
            Profiler.EndSample();

        }


        private static RenderTexture GenerateHeightmap(HeightmapData heightmapData,
            List<IHeightModifier> heightmapModifiers, Bounds terrainBounds, OcclusionData od, bool writeToCPU = false)
        {
            var terrainData = heightmapData.terrain.terrainData;
            var heightmapTexture = terrainData.heightmapTexture;
            var desc = heightmapTexture.descriptor;
            desc.enableRandomWrite = true;
            var rt1 = RenderTexture.GetTemporary(desc);
            var rt2 = RenderTexture.GetTemporary(desc);
            rt1.wrapMode = TextureWrapMode.Clamp;
            rt2.wrapMode = TextureWrapMode.Clamp;

            rt1.name = "MicroVerse::GenerateHeights:rt1";
            rt2.name = "MicroVerse::GenerateHeights:rt2";

            RenderTexture.active = rt1;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = rt2;
            GL.Clear(false, true, Color.clear);

            foreach (var heightmapModifier in heightmapModifiers)
            {
                var hmbounds = heightmapModifier.GetBounds();
                if (hmbounds.Intersects(terrainBounds))
                {
                    if (heightmapModifier.ApplyHeightStamp(rt1, rt2, heightmapData, od))
                        (rt1, rt2) = (rt2, rt1);
                }
            }
            RenderTexture.active = rt1;
            
            RenderTexture.active = null;
            // ref can change!
            RenderTexture.ReleaseTemporary(rt2);
            return rt1;
        }
    }
}