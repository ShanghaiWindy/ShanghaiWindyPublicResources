using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(FalloffOverride))]
    public class FalloffOverrideEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            GUIUtil.DrawHeaderLogo();
            FalloffOverride fo = (FalloffOverride)target;
            GUIUtil.DrawFalloffFilter(fo, fo.filter, fo.transform, true);
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                MicroVerse.instance?.Invalidate();
            }

        }
    }
}
