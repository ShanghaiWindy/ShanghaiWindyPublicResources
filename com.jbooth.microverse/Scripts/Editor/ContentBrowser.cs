using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace JBooth.MicroVerseCore
{
    public class ContentBrowser : EditorWindow
    {
        [MenuItem("Window/MicroVerse/Content Browser")]
        public static void CreateWindow()
        {
            var w = EditorWindow.GetWindow<ContentBrowser>();
            w.Show();
            w.wantsMouseEnterLeaveWindow = true;
            w.wantsMouseMove = true;
            w.titleContent = new GUIContent("MicroVerse Browser");
        }

        public static List<T> LoadAllInstances<T>() where T : ScriptableObject
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<T>)
                        .ToList();
        }

        /// <summary>
        /// Subset of the Falloff filter type
        /// </summary>
        enum FalloffDefault
        {
            Box = FalloffFilter.FilterType.Box,
            Range = FalloffFilter.FilterType.Range,
        }

        enum Tab
        {
            Height = 0,
            Texture,
            Vegetation,
            Objects,
            Audio
        }

        /// <summary>
        /// Identifier for the generic data of a drag and drop operation
        /// </summary>
        private static string EnabledCollidersId = "Enabled Colliders";

        /// <summary>
        /// Identifier whether the draggable is a MicroVerse object or not.
        /// Had to be introduced in case the content browser remained open the entire session.
        /// Otherwise it would handle all objects that are dragged into the scene and eg remove them after the drop
        /// </summary>
        private static string IsMicroverseDraggableId = "MicroVerse Draggable";

        /// <summary>
        /// Whether shift was pressed during the start of the drag operation or not
        /// </summary>
        private static string WasShiftPressed = "Shift Pressed";

        /// <summary>
        /// Whether control was pressed during the start of the drag operation or not
        /// </summary>
        private static string WasControlPressed = "Control Pressed";

        GUIContent[] tabNames = new GUIContent[5] { new GUIContent("Height"), new GUIContent("Texturing"), new GUIContent("Vegetation"), new GUIContent("Object"), new GUIContent("Audio") };
        static Tab tab = Tab.Height;

        List<BrowserContent> filteredContent = null;
        List<BrowserContent> allContent = null;
        BrowserContent selectedContent;

        private static int headerWidth = 180;
        private static int listWidth = headerWidth + 10;
        private static int cellSize = 96;

        private Vector2 listScrollPosition = Vector2.zero;
        private Vector2 contentScrollPosition = Vector2.zero;

        private Color selectionColor = Color.green;

        private static FalloffDefault filterTypeDefault = FalloffDefault.Box;
        private static Vector3 heightStampDefaultScale = new Vector3(300, 120, 300);
        private static Vector3 textureStampDefaultScale = new Vector3(300, 120, 300);
        private static Vector3 vegetationStampDefaultScale = new Vector3(300, 120, 300);
        /// <summary>
        /// Selected item per tab
        /// </summary>
        private Dictionary<Tab, BrowserContent> selectedTabItems = new Dictionary<Tab, BrowserContent>();

        private bool helpBoxVisible = true;

        private GameObject CreateInstance(int selectedIdx, bool wasShiftPressed, bool wasControlPressed)
        {
            GameObject instance = null;

            var contentCollection = selectedContent as ContentCollection;
            if (contentCollection == null || selectedIdx < 0 || selectedIdx > contentCollection.contents.Length || contentCollection.contents[selectedIdx] == null)
            {
                return null;
            }

            var selected = contentCollection.contents[selectedIdx];

            if (selected.prefab != null)
            {
                instance = Instantiate(selected.prefab);
                instance.name = selected.prefab.name;

                if (tab == Tab.Height)
                {
                    instance.transform.localScale = heightStampDefaultScale;
                }
                else if (tab == Tab.Texture)
                {
                    instance.transform.localScale = textureStampDefaultScale;
                }
                else if (tab == Tab.Vegetation)
                {
                    instance.transform.localScale = vegetationStampDefaultScale;
#if __MICROVERSE_VEGETATION__
                    var trees = instance.GetComponentsInChildren<TreeStamp>();
                    var details = instance.GetComponentsInChildren<DetailStamp>();
                    foreach (var t in trees)
                    {
                        t.seed = (uint)Random.Range(0, 99);
                    }
                    foreach (var d in details)
                    {
                        d.prototype.noiseSeed = (int)Random.Range(0, 99);
                    }
#endif
                }
            }
            else if (selected.stamp != null && tab == Tab.Height)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(new GUID(selected.stamp)));
                if (tex != null)
                {
                    instance = new GameObject( tex.name + " (Height Stamp)");
                    HeightStamp heightStamp = instance.AddComponent<HeightStamp>();

                    heightStamp.stamp = tex;
                    heightStamp.mode = HeightStamp.CombineMode.Add;

                    heightStamp.falloff.filterType = (FalloffFilter.FilterType)filterTypeDefault;
                    heightStamp.falloff.falloffRange = new Vector2(0.8f, 1f);

                    // overrides
                    bool autoScaleTerrain = wasShiftPressed;
                    if (autoScaleTerrain)
                    {
                        Terrain[] terrains = MicroVerse.instance.GetComponentsInChildren<Terrain>();

                        Bounds worldBounds = TerrainUtil.ComputeTerrainBounds(terrains);

                        // scale
                        float x = worldBounds.size.x;
                        float y = worldBounds.size.y;
                        float z = worldBounds.size.z;

                        // if y is dynamic and depends on the current bounds
                        // however if it's very low or 0 in case it's the first terrain we use
                        // a heuristic to calculate a resonable height. the values are just arbitrary
                        float threshold = 10f;
                        if (y < threshold)
                        {
                            if (Terrain.activeTerrain)
                            {
                                y = TerrainUtil.ComputeTerrainSize(Terrain.activeTerrain).y * 0.1f;
                            }
                            else
                            {
                                y = threshold;
                            }
                        }

                        instance.transform.localScale = new Vector3(x, y, z);
                    }
                    else
                    {
                        instance.transform.localScale = heightStampDefaultScale;
                    }
                }
            }
            if (instance != null)
            {
                // hide instance initially, we don't want it visible at 0/0/0
                // instance.SetActive(false);

                // overrides
                // shift at start of drag operation: force falloff type global
                bool forceFalloffTypeGlobal = wasShiftPressed;
                if (forceFalloffTypeGlobal)
                {
                    FalloffOverride falloffOverride = instance.GetComponent<FalloffOverride>();
                    if (falloffOverride)
                    {
                        falloffOverride.filter.filterType = FalloffFilter.FilterType.Global;
                    }
                    else
                    {
                        Stamp[] stamps = instance.GetComponentsInChildren<Stamp>();
                        foreach (Stamp stamp in stamps)
                        {
                            if (stamp.GetFilterSet() == null)
                                continue;

                            stamp.GetFilterSet().falloffFilter.filterType = FalloffFilter.FilterType.Global;
                        }
                    }
                    
                }

                instance.transform.SetParent(MicroVerse.instance?.transform);
                MicroVerse.instance?.Invalidate();
            }

            return instance;
        }

        private void OnEnable()
        {
            SceneView.beforeSceneGui += this.OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.beforeSceneGui -= this.OnSceneGUI;
        }

        private void OnSceneGUI(SceneView obj)
        {
            HandleDragAndDropEvents();
        }

        private void HandleDragAndDropEvents()
        {
            if (Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform)
                return;

            if (EditorWindow.mouseOverWindow == this)
                return;

            object isMicroVerseDraggable = DragAndDrop.GetGenericData(IsMicroverseDraggableId);

            bool valid = isMicroVerseDraggable != null && isMicroVerseDraggable is bool && (bool)isMicroVerseDraggable == true;

            if (!valid)
                return;

            object wasShiftPressedObject = DragAndDrop.GetGenericData(WasShiftPressed);
            object wasControlPressedObject = DragAndDrop.GetGenericData(WasControlPressed);

            bool wasShiftPressed = wasShiftPressedObject != null && wasShiftPressedObject is bool && (bool)wasShiftPressedObject == true;
            bool wasControlPressed = wasControlPressedObject != null && wasControlPressedObject is bool && (bool)wasControlPressedObject == true;

            var index = (int)DragAndDrop.GetGenericData("index");
            if (index >= 0)
            {
                DragAndDrop.SetGenericData("index", -1);

                GameObject draggable = CreateInstance(index, wasShiftPressed, wasControlPressed);

                DragAndDrop.objectReferences = new GameObject[] { draggable };
                DragAndDrop.paths = null;

                // disable colliders, we don't want to raycast against self; store as generic data for re-enabling later
                Collider[] enabledColliders = draggable.GetComponentsInChildren<Collider>().Where(x => x.enabled == true).ToArray();
                foreach (Collider collider in enabledColliders)
                {
                    collider.enabled = false;
                }
                DragAndDrop.SetGenericData(EnabledCollidersId, enabledColliders);

            }


            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    // if( !instance.activeInHierarchy)
                    //    instance.SetActive(true);

                    Vector3 point = hit.point;

                    if (DragAndDrop.objectReferences.Length == 1)
                    {
                        if (DragAndDrop.objectReferences[0] is GameObject)
                        {
                            GameObject go = DragAndDrop.objectReferences[0] as GameObject;
                            if (tab == Tab.Height)
                            {
                                point.y = 0; // height stamps always at 0
                            }
                            go.transform.position = point;
                        }
                    }
                }
            }
            else if (Event.current.type == EventType.DragPerform)
            {

                DragAndDrop.SetGenericData(IsMicroverseDraggableId, false);
                DragAndDrop.SetGenericData(WasShiftPressed, false);
                DragAndDrop.SetGenericData(WasControlPressed, false);

                DragAndDrop.AcceptDrag();

                if (DragAndDrop.objectReferences.Length == 1)
                {
                    // re-enable colliders which were diabled before the drag operation
                    Collider[] enabledColliders = (Collider[])DragAndDrop.GetGenericData(EnabledCollidersId);
                    if (enabledColliders != null)
                    {
                        foreach (Collider collider in enabledColliders)
                        {
                            collider.enabled = true;
                        }
                    }

                    // reference to new object
                    GameObject go = DragAndDrop.objectReferences[0] as GameObject;

                    bool centerTerrain = wasControlPressed;
                    if (centerTerrain)
                    {
                        Terrain[] terrains = MicroVerse.instance.GetComponentsInChildren<Terrain>();

                        Bounds worldBounds = TerrainUtil.ComputeTerrainBounds(terrains);

                        // position
                        float y = worldBounds.center.y - worldBounds.size.y / 2f;

                        go.transform.transform.position = new Vector3(worldBounds.center.x, y, worldBounds.center.z);
                    }

                    // select new object
                    Selection.activeObject = go;
                }

                // cleanup drag
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
                DragAndDrop.SetGenericData(EnabledCollidersId, null);

                Event.current.Use();

                DragFinished();
                
            }
            /* note: doesn't seem to work, DragExited is also invoked after DragPerform
            // eg escape pressed
            else if (Event.current.type == EventType.DragExited)
            {
                if (DragAndDrop.objectReferences.Length == 1)
                {
                    GameObject go = DragAndDrop.objectReferences[0] as GameObject;
                    GameObject.DestroyImmediate(go);
                }

                // cleanup drag
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.visualMode = DragAndDropVisualMode.None;

                Event.current.Use();

                DragFinished();
            }
            */
        }


        private void DragFinished()
        {
            MicroVerse.instance?.Invalidate();
            MicroVerse.instance?.RequestHeightSaveback();
        }

        bool HasContentForAd(ContentAd ad, List<ContentCollection> content)
        {
            for (int i = 0; i < content.Count; ++i)
            {
                if (content[i].id == ad.id && !string.IsNullOrEmpty(ad.id))
                    return true;
                if (content[i].packName == ad.packName && !string.IsNullOrEmpty(ad.packName))
                    return true;
                
            }
            return false;
        }

        private void OnFocus()
        {
            allContent = LoadAllInstances<BrowserContent>();
        }

        private void OnDragStart(GUIContent item, int index)
        {
            
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.SetGenericData("index", index);
            DragAndDrop.SetGenericData(IsMicroverseDraggableId, true);

            // record if shift was pressed at the start of the drag operation
            bool wasShiftPressed = Event.current.shift;
            DragAndDrop.SetGenericData(WasShiftPressed, wasShiftPressed);

            // record if control was pressed at the start of the drag operation
            bool wasControlPressed = Event.current.control;
            DragAndDrop.SetGenericData(WasControlPressed, wasControlPressed);

            DragAndDrop.StartDrag("Dragging MyData");

        }

        void DrawToolbar()
        {
            if (tab == Tab.Height || tab == Tab.Texture || tab == Tab.Vegetation)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("Preset", EditorStyles.miniBoldLabel);

                    float prev = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 80f;
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            
                            if (tab == Tab.Height)
                            {
                                EditorGUILayout.PrefixLabel("Falloff Type");
                                filterTypeDefault = (FalloffDefault)EditorGUILayout.EnumPopup(filterTypeDefault, GUILayout.Width(120));
                                EditorGUILayout.LabelField("Size", GUILayout.Width(80));
                                heightStampDefaultScale = EditorGUILayout.Vector3Field("", heightStampDefaultScale, GUILayout.Width(200));
                            }
                            else if (tab == Tab.Texture)
                            {
                                EditorGUILayout.LabelField("Size", GUILayout.Width(80));
                                textureStampDefaultScale = EditorGUILayout.Vector3Field("", textureStampDefaultScale, GUILayout.Width(200));
                            }
                            else if (tab == Tab.Vegetation)
                            {
                                EditorGUILayout.LabelField("Size", GUILayout.Width(80));
                                vegetationStampDefaultScale = EditorGUILayout.Vector3Field("", vegetationStampDefaultScale,GUILayout.Width(200));
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUIUtility.labelWidth = prev;

                }
                EditorGUILayout.EndVertical();
            }
        }

        private void OnGUI()
        {
            if (allContent == null)
                allContent = LoadAllInstances<BrowserContent>();


            List<ContentAd> allAds = new List<ContentAd>();
            List<ContentCollection> allCollections = new List<ContentCollection>();
            filteredContent = new List<BrowserContent>();

            for (int i = 0; i < allContent.Count; ++i)
            {
                var ad = allContent[i] as ContentAd;
                var cc = allContent[i] as ContentCollection;
                if (ad != null)
                    allAds.Add(ad);
                if (cc != null)
                    allCollections.Add(cc);
            }

            // add valid content collections
            for (int i = 0; i < allCollections.Count; ++i)
            {
                var cc = allCollections[i];
                foreach (var ad in allAds)
                {
                    if (cc.id == ad.id && !string.IsNullOrEmpty(cc.id))
                    {
                        if (ad.requireInstalledObject && ad.installedObject == null)
                        {
                            allCollections.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }

            filteredContent.AddRange(allCollections);

            // add ads
            for (int i = 0; i < allAds.Count; ++i)
            {
                var ad = allAds[i];
                if (ad.requireInstalledObject && ad.installedObject == null || !HasContentForAd(ad, allCollections))
                {
                    filteredContent.Add(ad);
                }
            }
            BrowserContent oldContent = null;
            var oldTab = tab;
            EditorGUILayout.BeginHorizontal();
            {
                // tab bar
                tab = (Tab)GUILayout.Toolbar((int)tab, tabNames);

                // help button
                if (GUILayout.Button(new GUIContent("?", "Help information visibility"), EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    helpBoxVisible = !helpBoxVisible;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (tab != oldTab)
            {
                oldContent = selectedTabItems[oldTab];
            }

            // remove all content from the wrong tab
            for (int i = 0; i < filteredContent.Count; ++i)
            {
                if (tab == Tab.Height)
                {
                    if (filteredContent[i].contentType != ContentType.Height)
                    {
                        filteredContent.RemoveAt(i);
                        i--;
                    }
                }
                else if (tab == Tab.Texture)
                {
                    if (filteredContent[i].contentType != ContentType.Texture)
                    {
                        filteredContent.RemoveAt(i);
                        i--;
                    }
                }
                else if (tab == Tab.Vegetation)
                {
                    if (filteredContent[i].contentType != ContentType.Vegetation)
                    {
                        filteredContent.RemoveAt(i);
                        i--;
                    }
                }
                else if (tab == Tab.Objects)
                {
                    if (filteredContent[i].contentType != ContentType.Objects)
                    {
                        filteredContent.RemoveAt(i);
                        i--;
                    }
                }
                else if (tab == Tab.Audio)
                {
                    if (filteredContent[i].contentType != ContentType.Audio)
                    {
                        filteredContent.RemoveAt(i);
                        i--;
                    }
                }
            }

            foreach (var c in filteredContent)
            {
                if (oldContent != null && c.packName == oldContent.packName && c.author == oldContent.author)
                {
                    selectedTabItems[tab] = c;
                }
            }
            

            filteredContent = filteredContent.OrderByDescending(x => x.GetType().Name).ThenBy(x => x.packName).ToList();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical("box");

                listScrollPosition = GUILayout.BeginScrollView(listScrollPosition, GUILayout.Width(listWidth));
                {
                    selectedContent = null;
                    System.Type prevType = null;

                    foreach (BrowserContent item in filteredContent)
                    {
                        if (prevType != item.GetType())
                        {
                            if (prevType != null)
                            {
                                EditorGUILayout.Space();
                            }

                            string name = "";
                            if (item is ContentCollection)
                            {
                                name = "Installed";
                            }
                            else if (item is ContentAd)
                            {
                                name = "Optional";
                            }

                            if (!string.IsNullOrEmpty(name))
                            {
                                EditorGUILayout.PrefixLabel(name);
                            }

                            prevType = item.GetType();
                        }

                        Color prevColor = GUI.backgroundColor;
                        {
                            selectedContent = selectedTabItems.GetValueOrDefault(tab);

                            if (selectedContent == item)
                                GUI.backgroundColor = selectionColor;

                            if (GUILayout.Button(item.packName, GUILayout.Width(headerWidth)))
                            {
                                selectedContent = item;
                                selectedTabItems[tab] = selectedContent;
                            }
                        }
                        GUI.backgroundColor = prevColor;
                    }
                }
                GUILayout.EndScrollView();

                // evaluate selection per tab
                selectedContent = selectedTabItems.GetValueOrDefault(tab);

                // nothing selected => pick first one if available
                if (selectedContent == null && filteredContent.Count > 0)
                {
                    selectedContent = filteredContent[0];
                    selectedTabItems[tab] = selectedContent;

                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();

                    DrawToolbar();

                    if (selectedContent != null)
                    {
                        var selectedAd = selectedContent as ContentAd;
                        var selectedStamps = selectedContent as ContentCollection;
                        if (selectedStamps != null)
                        {
                            DrawSelectionGrid(selectedStamps.GetContents());
                        }
                        else if (selectedAd != null && !HasContentForAd(selectedAd, allCollections))
                        {
                            if (GUILayout.Button("Download", GUILayout.Width(420)))
                            {
                                var path = selectedAd.downloadPath;
                                if (path.Contains("assetstore.unity.com"))
                                {
                                    path += "?aid=25047";
                                }
                                Application.OpenURL(path);
                            }
                            Rect r = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(420), GUILayout.MaxHeight(280));
                            if (GUI.Button(r, ""))
                            {
                                var path = selectedAd.downloadPath;
                                if (path.Contains("assetstore.unity.com"))
                                {
                                    path += "?aid=25047";
                                }
                                Application.OpenURL(path);
                            }
                            if (selectedAd.image == null)
                            {
                                GUI.DrawTexture(r, Texture2D.whiteTexture);
                            }
                            else
                            {
                                GUI.DrawTexture(r, selectedAd.image);
                            }
                        }

                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();
        }
        const int infoWidth = 240;
        int selectedIndex = -1;
        private void DrawSelectionGrid(GUIContent[] gridItems)
        {
            int cellWidth = cellSize;
            int cellHeight = cellSize;
            float safetyMargin = cellWidth / 3f; // just some margin to keep the stamp preview almost fully visible; use div by 2 to keep it exactly visible; but 2 would have too much whitespace most of the time
            float gridWidth = EditorGUIUtility.currentViewWidth - listWidth - safetyMargin - infoWidth;
            int columnCount = Mathf.FloorToInt(gridWidth / cellWidth);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("Box");
            {
                contentScrollPosition = EditorGUILayout.BeginScrollView(contentScrollPosition, GUILayout.Width(gridWidth+30));
                {
                    int gridRows = Mathf.CeilToInt((float)gridItems.Length / columnCount);

                    GUIContent defaultItem = new GUIContent();

                    for (int row = 0; row < gridRows; row++)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            for (int column = 0; column < columnCount; column++)
                            {
                                int index = column + row * columnCount;
                                GUIContent item = index < gridItems.Length ? gridItems[index] : defaultItem;

                                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));

                                if (item != defaultItem)
                                {
                                    Texture image = item.image;
                                    string label = item.text;
                                    if (index == selectedIndex)
                                    {
                                        var outline = rect;
                                        outline.max += Vector2.one;
                                        outline.min -= Vector2.one;

                                        Texture2D texture = EditorGUIUtility.isProSkin ? Texture2D.whiteTexture : Texture2D.blackTexture;
                                        GUI.DrawTexture(outline, texture, ScaleMode.ScaleToFit, false);

                                    }
                                    // texture
                                    GUI.DrawTexture(rect, image != null ? image : Texture2D.blackTexture, ScaleMode.ScaleToFit, false);

                                    // label
                                    rect.height = GUIUtil.SelectionElementLabelStyle.CalcHeight(new GUIContent(label), rect.width);

                                    GUI.DrawTexture(rect, GUIUtil.LabelBackgroundTexture, ScaleMode.StretchToFill);
                                    GUI.Box(rect, label, GUIUtil.SelectionElementLabelStyle);

                                    if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                    {
                                        selectedIndex = index;

                                        Repaint();
                                    }
                                
                                    if (Event.current.type == EventType.MouseDrag && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                    {
                                        OnDragStart(item, index);

                                        Event.current.Use();
                                    }
                                }
                            }

                            // stretch, so that the content doesn't move position (flicker) when we resize the editorwindow
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal("Box", GUILayout.Width(infoWidth-20));
            {
                var contentCollection = selectedContent as ContentCollection;
                if (selectedIndex >= 0 && selectedIndex < contentCollection.contents.Length)
                {

                    var c = contentCollection.contents[selectedIndex];
                    
                    GUILayout.BeginVertical();
                    EditorGUILayout.HelpBox(contentCollection.GetContents()[selectedIndex].text, MessageType.None);
                    EditorGUILayout.Space();

                    EditorGUILayout.HelpBox(c.description, MessageType.None);


                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();

            if (helpBoxVisible)
            {
                GUILayout.BeginHorizontal("Box");
                EditorGUILayout.HelpBox("Keyboard shortcuts:\nShift at drag start: Set falloff override to global. For height stamps this scales the stamp to total terrain bounds.\nControl at drag start: For height stamps this positions the stamp at the center of the terrain.", MessageType.None);
                GUILayout.EndHorizontal();
            }
        }

    }

}
