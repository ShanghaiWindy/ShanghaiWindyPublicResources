using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace JBooth.MicroVerseCore
{
    public enum ContentType
    {
        Height = 0,
        Texture,
        Vegetation,
        Objects,
        Audio
    }

    public class BrowserContent : ScriptableObject
    {
        public ContentType contentType;
        public string author;
        public string packName;
        public string id;
    }

    [System.Serializable]
    public class ContentData
    {
        public GameObject prefab;
        public Texture2D previewImage;
        public GameObject previewAsset;
        public string stamp; // only used for height stamps
        public Texture2D previewGradient;
        public string description;
        
    }

    [CustomPropertyDrawer(typeof(ContentData))]
    public class ContentDataDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            var yH = base.GetPropertyHeight(property, label);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            position.height /= 9;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("prefab"));
            position.y += yH;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("previewImage"));
            position.y += yH;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("previewAsset"));
            position.y += yH;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("previewGradient"));
            position.y += yH;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("description"));

            position.y += yH;
            EditorGUI.PrefixLabel(position, new GUIContent("Stamp"));
            position.x = position.width - 64;
            position.width = 64;
            position.height *= 4;
            string guid = property.FindPropertyRelative("stamp").stringValue;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(path))
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            
            var nt = EditorGUI.ObjectField(position, tex, typeof(Texture2D), false);
            if (nt != tex)
            {
                if (nt == null)
                {
                    property.FindPropertyRelative("stamp").stringValue = null;
                }
                else
                {
                    string npath = AssetDatabase.GetAssetPath(nt);
                    string nguid = AssetDatabase.AssetPathToGUID(npath);
                    property.FindPropertyRelative("stamp").stringValue = nguid;
                }
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 9;
        }
    }

    

    [CreateAssetMenu(fileName = "Collection", menuName = "MicroVerse/ContentPack")]
    public class ContentCollection : BrowserContent
    {
        static Material stampPreviewMat;

        public Texture2D previewGradient;
        public ContentData[] contents;
        
        static Dictionary<string, Texture2D> cachedPreviews = new Dictionary<string, Texture2D>();

        public GUIContent[] GetContents()
        {
            var content = new GUIContent[contents.Length];
            for (int i = 0; i < contents.Length; ++i)
            {
                content[i] = new GUIContent("missing", Texture2D.blackTexture);

                if (contents[i] != null)
                {
                    if (contentType == ContentType.Height)
                    {
                        if (contents[i].stamp != null)
                        {
                            bool erased = true;
                            if (cachedPreviews.ContainsKey(contents[i].stamp))
                            {
                                var tex = cachedPreviews[contents[i].stamp];
                                if (tex != null)
                                {
                                    content[i] = new GUIContent(tex.name, tex);
                                    erased = false;
                                }
                                else
                                {
                                    cachedPreviews.Remove(contents[i].stamp);
                                }
                            }
                            if (erased)
                            {
                                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(new GUID(contents[i].stamp)));
                                var dst = RenderTexture.GetTemporary(96, 96, 0, RenderTextureFormat.ARGB32);
                                dst.name = tex.name;
                                if (stampPreviewMat == null)
                                {
                                    stampPreviewMat = new Material(Shader.Find("Hidden/MicroVerse/StampPreview2D"));
                                    stampPreviewMat.SetTexture("_Gradient", Resources.Load<Texture2D>("microverse_default_previewgradient"));
                                }

                                stampPreviewMat.SetTexture("_Stamp", tex);
                                if (contents[i].previewGradient != null)
                                {
                                    stampPreviewMat.SetTexture("_Gradient", contents[i].previewGradient);
                                }
                                else if (previewGradient != null)
                                {
                                    stampPreviewMat.SetTexture("_Gradient", previewGradient);
                                }
                                else
                                {
                                    stampPreviewMat.SetTexture("_Gradient", Resources.Load<Texture2D>("microverse_default_previewgradient"));
                                    
                                }

                                Graphics.Blit(null, dst, stampPreviewMat);
                                stampPreviewMat.SetTexture("_Stamp", null);
                                
                                var dtex = new Texture2D(96, 96, TextureFormat.ARGB32, false);
                                dtex.name = tex.name;

                                RenderTexture.active = dst;
                                dtex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                                dtex.Apply();
                                RenderTexture.active = null;
                                RenderTexture.ReleaseTemporary(dst);

                                cachedPreviews[contents[i].stamp] = dtex;
                                content[i] = new GUIContent(dst.name, dtex);
                            }
                        }
                    }
                    else
                    {
                        if (contents[i].prefab != null)
                        {
                            if (contents[i].previewImage != null)
                            {
                                content[i] = new GUIContent(contents[i].prefab.name, contents[i].previewImage);
                            }
                            else if (contents[i].previewAsset)
                            {
                                Texture tex = AssetPreview.GetAssetPreview(contents[i].previewAsset);
                                content[i] = new GUIContent(contents[i].prefab.name, tex);
                            }
                            else
                            {
                                Texture tex = AssetPreview.GetAssetPreview(contents[i].prefab);
                                content[i] = new GUIContent(contents[i].prefab.name, tex);
                            }
                        }
                    }
                }
            }
            
            return content;
        }
    }


}