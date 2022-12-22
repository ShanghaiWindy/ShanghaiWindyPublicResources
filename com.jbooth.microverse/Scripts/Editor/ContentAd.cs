using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [CreateAssetMenu(fileName = "Ad", menuName = "MicroVerse/ContentAd")]
    public class ContentAd : BrowserContent
    {
        public Texture2D image;
        public string downloadPath;
        public bool requireInstalledObject;
        public GameObject installedObject;
    }

}