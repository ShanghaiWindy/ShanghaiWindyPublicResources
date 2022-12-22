using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace JBooth.MicroVerseCore
{
    [InitializeOnLoadAttribute]
    public static class MicroVersePlayModeStateChanged
    {
        // register an event handler when the class is initialized
        static MicroVersePlayModeStateChanged()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (MicroVerse.instance != null)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    MicroVerse.instance.enabled = false;
                }
            }
        }
    }

    class TerrainAssetProcessor : AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            MicroVerse instance = GameObject.FindObjectOfType<MicroVerse>();
            if (instance != null)
            {
                instance.SaveBackToTerrain(); // your save terrain function
            }
            return paths;
        }
    }

    public enum HeightMapResolution
    {
        [InspectorName("33 x 33")]
        k33 = 33,
        [InspectorName("65 x 65")]
        k65 = 65,
        [InspectorName("129 x 129")]
        k129 = 129,
        [InspectorName("257 x 257")]
        k257 = 257,
        [InspectorName("513 x 513")]
        k513 = 513,
        [InspectorName("1025 x 1025")]
        k1025 = 1025,
        [InspectorName("2049 x 2049")]
        k2049 = 2049,
        [InspectorName("4097 x 4097")]
        k4097 = 4097
    }

    public enum SplatResolution
    {
        [InspectorName("32 x 32")]
        k32 = 32,
        [InspectorName("64 x 64")]
        k64 = 64,
        [InspectorName("128 x 128")]
        k128 = 128,
        [InspectorName("256 x 256")]
        k256 = 258,
        [InspectorName("512 x 512")]
        k512 = 512,
        [InspectorName("1024 x 1024")]
        k1024 = 1024,
        [InspectorName("2048 x 2048")]
        k2048 = 2048,
        [InspectorName("4096 x 4096")]
        k4096 = 4096
    }

    
    [CustomEditor(typeof(MicroVerse))]
    public class MicroVerseEditor : Editor
    {
        void DoTerrainSyncGUI()
        {
            if (MicroVerse.instance == null)
                return;
            MicroVerse.instance.SyncTerrainList();
            if (MicroVerse.instance.terrains == null || MicroVerse.instance.terrains.Length == 0 || MicroVerse.instance.terrains[0] == null)
                return;
           
            var src = MicroVerse.instance.terrains[0];

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.HelpBox("Changing any property here will update the properties of all terrains", MessageType.Info);

            var drawTreesAndFoliage = src.drawTreesAndFoliage;
            var alphaMapResolution = (SplatResolution) src.terrainData.alphamapResolution;
            var heightmapResolution = (HeightMapResolution)src.terrainData.heightmapResolution;
            var basemapDistance = src.basemapDistance;
            var baseMapResolution = (SplatResolution)src.terrainData.baseMapResolution;
            var detailObjectDensity = src.detailObjectDensity;
            var detailObjectDistance = src.detailObjectDistance;
            var treeDistance = src.treeDistance;
            var pixelError = src.heightmapPixelError;
            var detailRes = src.terrainData.detailResolution;
            var detailResPerPatch = src.terrainData.detailResolutionPerPatch;


            drawTreesAndFoliage = EditorGUILayout.Toggle("Draw Trees and Foliage", drawTreesAndFoliage);
            heightmapResolution = (HeightMapResolution)EditorGUILayout.EnumPopup(new GUIContent("HeightMap Resolution"), heightmapResolution);
            alphaMapResolution = (SplatResolution) EditorGUILayout.EnumPopup(new GUIContent("AlphaMap Resolution"), alphaMapResolution);
            baseMapResolution = (SplatResolution)EditorGUILayout.EnumPopup(new GUIContent("BaseMap Resolution"), baseMapResolution);
            if (!MicroVerse.instance.IsUsingMicroSplat())
                basemapDistance = EditorGUILayout.Slider("Base Map Distance", basemapDistance, 0, 20000);

            detailRes = EditorGUILayout.DelayedIntField("Detail Resolution", detailRes);
            detailResPerPatch = EditorGUILayout.DelayedIntField("Detail Resolution Per Patch", detailResPerPatch);
            detailObjectDensity = EditorGUILayout.Slider("Detail Density", detailObjectDensity, 0, 1);
            detailObjectDistance = EditorGUILayout.Slider("Detail Distance", detailObjectDistance, 0, 250);
            treeDistance = EditorGUILayout.Slider("Tree Distance", treeDistance, 0, 5000);
            pixelError = EditorGUILayout.Slider("Pixel Error", pixelError, 1, 200);

            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < MicroVerse.instance.terrains.Length; ++i)
                {
                    
                    var t = MicroVerse.instance.terrains[i];
                    var size = t.terrainData.size;
                    t.drawTreesAndFoliage = drawTreesAndFoliage;
                    
                    if (t.terrainData.alphamapResolution != (int)alphaMapResolution)
                        t.terrainData.alphamapResolution = (int)alphaMapResolution;
                    if (t.terrainData.heightmapResolution != (int)heightmapResolution)
                        t.terrainData.heightmapResolution = (int)heightmapResolution;
                    if (t.terrainData.baseMapResolution != (int)baseMapResolution)
                        t.terrainData.baseMapResolution = (int)baseMapResolution;
                    t.basemapDistance = basemapDistance;
                    t.detailObjectDensity = detailObjectDensity;
                    t.detailObjectDistance = detailObjectDistance;
                    t.treeDistance = treeDistance;
                    t.heightmapPixelError = pixelError;
                    t.terrainData.size = size;
                    t.terrainData.SetDetailResolution(detailRes, detailResPerPatch);
                    EditorUtility.SetDirty(t);
                    EditorUtility.SetDirty(t.terrainData);
                    MicroVerse.instance?.Invalidate();
                    MicroVerse.instance?.RequestHeightSaveback();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

#if __MICROSPLAT__

        // TODO: Once MicroSplat is updated with this code and time has passed,
        // remove it and call into MicroSplat, because this shouldn't live in MV..
        public class LayerSort
        {
            public TerrainLayer terrainLayer;
            public Color[] propDataValues = null;
            public MicroSplat.TextureArrayConfig.TextureEntry source = null;
            public MicroSplat.TextureArrayConfig.TextureEntry source2 = null;
            public MicroSplat.TextureArrayConfig.TextureEntry source3 = null;
        }



        static bool IsInConfig(MicroSplat.TextureArrayConfig config, TerrainLayer l)
        {
            foreach (var c in config.sourceTextures)
            {
                if (c.terrainLayer == l)
                    return true;
            }
            return false;
        }


        static Color[] GetPropDataValues(MicroSplat.MicroSplatPropData pd, int textureIndex)
        {
            pd.RevisionData();
            Color[] c = new Color[MicroSplat.MicroSplatPropData.sMaxAttributes];
            for (int i = 0; i < MicroSplat.MicroSplatPropData.sMaxAttributes; ++i)
            {
                c[i] = pd.GetValue(textureIndex, i);
            }
            return c;
        }

        static void SetPropDataValues(MicroSplat.MicroSplatPropData pd, int textureIndex, Color[] c)
        {
            pd.RevisionData();
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(pd, "Changed Value");
#endif
            for (int i = 0; i < c.Length; ++i)
            {
                pd.SetValue(textureIndex, i, c[i]);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(pd);
#endif
        }

        public static void SyncMicroSplat()
        {
            var mv = MicroVerse.instance;
            if (mv == null)
                return;
            mv.Modify(true);
            if (mv.terrains.Length == 0)
                return;
            var mst = mv.terrains[0].GetComponent<MicroSplat.MicroSplatTerrain>();

            MicroSplat.MicroSplatPropData propData = mst.propData;
            if (propData == null && mst.templateMaterial != null)
            {
                propData = MicroSplatShaderGUI.FindOrCreatePropTex(mst.templateMaterial);
            }
            if (propData == null)
            {
                Debug.LogError("Could not find or create propdata");
            }

            ITextureModifier[] texMods = mv.GetComponentsInChildren<ITextureModifier>(true);
            List<TerrainLayer> layers = new List<TerrainLayer>();
            foreach (var terrain in mv.terrains)
            {
                foreach (var texMod in texMods)
                {
                    texMod.InqTerrainLayers(terrain, layers);
                }
            }
            layers.RemoveAll(item => item == null);
            layers = layers.Distinct().OrderBy(x => x.name).ToList();

            var terrainLayers = mv.terrains[0].terrainData.terrainLayers;

            MatchAndSortTerrainLayers(mv.msConfig, propData, layers, terrainLayers);

            mv.Modify(true);
        }

        static void MatchAndSortTerrainLayers(MicroSplat.TextureArrayConfig config, MicroSplat.MicroSplatPropData propData,
            List<TerrainLayer> mvLayers, TerrainLayer[] terrainLayers)
        {
            // Go through the mvlayers and add any new ones
            for (int i = 0; i < mvLayers.Count; ++i)
            {
                if (!IsInConfig(config, mvLayers[i]))
                {
                    config.AddTerrainLayer(mvLayers[i]);
                }
            }

            // build sortable list of layers so we can sync them in alphabetical order
            List<LayerSort> layers = new List<LayerSort>();
            for (int i = 0; i < config.sourceTextures.Count; ++i)
            {
                if (mvLayers.Contains(config.sourceTextures[i].terrainLayer))
                {
                    LayerSort ls = new LayerSort();
                    ls.terrainLayer = config.sourceTextures[i].terrainLayer;
                    if (propData != null)
                        ls.propDataValues = GetPropDataValues(propData, i);
                    ls.source = config.sourceTextures[i];
                    if (config.sourceTextures2 != null && i < config.sourceTextures2.Count) ls.source2 = config.sourceTextures2[i];
                    if (config.sourceTextures3 != null && i < config.sourceTextures3.Count) ls.source3 = config.sourceTextures3[i];
                    layers.Add(ls);
                }
            }

            layers.Sort((x, y) => x.terrainLayer.name.CompareTo(y.terrainLayer.name));

            config.sourceTextures.Clear();
            config.sourceTextures2?.Clear();
            config.sourceTextures3?.Clear();
            // move propdata around and setup the textures
            for (int i = 0; i < layers.Count; ++i)
            {
                var l = layers[i];
                if (propData != null)
                    SetPropDataValues(propData, i, l.propDataValues);
                config.sourceTextures.Add(l.source);
                if (l.source2 != null) config.sourceTextures2?.Add(l.source2);
                if (l.source3 != null) config.sourceTextures3?.Add(l.source3);
            }
            MicroSplat.TextureArrayConfigEditor.CompileConfig(config);
        }

#endif // microsplat

        public override void OnInspectorGUI()
        {
            var mv = (MicroVerse)target;
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("explicitTerrains"));
#if __MICROVERSE_MASKS__
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bufferCaptureTarget"));
#endif
            EditorGUILayout.PropertyField(serializedObject.FindProperty("options"));
#if __MICROSPLAT__
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("msConfig"));
                serializedObject.ApplyModifiedProperties();
                if (mv.msConfig != null)
                {
                    mv.SyncTerrainList();
                    ITextureModifier[] texMods = mv.GetComponentsInChildren<ITextureModifier>();
                    List<TerrainLayer> layers = new List<TerrainLayer>();
                    foreach (var terrain in mv.terrains)
                    {
                        foreach (var texMod in texMods)
                        {
                            texMod.InqTerrainLayers(terrain, layers);
                        }
                    }
                    layers.RemoveAll(item => item == null);
                    layers = layers.Distinct().OrderBy(x=>x.name).ToList();
                    bool inSync = true;
                    for (int i = 0; i < layers.Count; ++i)
                    {
                        var layer = layers[i];
                        if (mv.msConfig.sourceTextures.Count != layers.Count || Object.ReferenceEquals(mv.msConfig.sourceTextures[i].terrainLayer, layer) == false)
                        {
                            inSync = false;
                            break;
                        }
                    }
                    if (!inSync)
                    {
                        EditorGUILayout.HelpBox("Terrain Layers are not in sync with the MicroSplat texture array config, please update them", MessageType.Error);
                        if (GUILayout.Button("Update Texture Arrays"))
                        {
                            SyncMicroSplat();
                        }
                    }
                    if (inSync && layers.Count != mv.msConfig.sourceTextures.Count && layers.Count != 0)
                    {
                        
                        if (GUILayout.Button("Remove unused layers from texture arrays"))
                        {
                            SyncMicroSplat();
                        }

                    }
                }
                else
                {
                    if (GUILayout.Button("Convert to MicroSplat"))
                    {
                        mv.options.settings.keepLayersInSync = true;
                        mv.Modify(true);
                        EditorUtility.SetDirty(mv);
                        mv.msConfig = JBooth.MicroSplat.MicroSplatTerrainEditor.ConvertTerrains(mv.terrains, mv.terrains[0].terrainData.terrainLayers);

                    }


                }

            }

            
#else

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Install MicroSplat"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/terrain/microsplat-96478");
            }


#if USING_URP
            if (GUILayout.Button("URP Module"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/terrain/microsplat-urp-2021-support-205510");
            }
#elif USING_HDRP
            if (GUILayout.Button("HDRP Module"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/terrain/microsplat-hdrp-2021-support-206311");
            }
#endif
            EditorGUILayout.EndHorizontal();

#endif // microsplat

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Sync to Terrain (Save)", GUILayout.Height(64)))
            {
                mv.SaveBackToTerrain();
            }

            DoTerrainSyncGUI();
        }
    }
}
        