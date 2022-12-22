using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    class SceneBuildStripping : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {

            var mv = GameObject.FindObjectOfType<MicroVerse>();
            if (mv != null)
            {
                mv.CancelInvoke();
                var all = mv.GetComponentsInChildren<IModifier>();
                
                foreach (var m in all)
                {
                    m.StripInBuild();
                }
                if (Application.isPlaying)
                {
                    GameObject.Destroy(mv);
                }
                else
                {
                    GameObject.DestroyImmediate(mv);
                }
            }
        }
    }
}