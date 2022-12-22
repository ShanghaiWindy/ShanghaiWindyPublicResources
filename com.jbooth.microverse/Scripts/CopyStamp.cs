using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    public class CopyStamp : ScriptableObject
    {
        [System.Serializable]
        public class TreeCopyData
        {
            public Texture2D positonsTex;
            public Texture2D randomsTex;
            public TreePrototypeSerializable[] prototypes;

            // serialized data
            public byte[] randomsData;
            public byte[] positionsData;
            [HideInInspector] public Vector2Int dataSize;

            public void Unpack()
            {
                if (positonsTex == null && positionsData != null && positionsData.Length != 0)
                {
                    positonsTex = new Texture2D(dataSize.x, dataSize.y, TextureFormat.RGBAHalf, false, true);
                    positonsTex.wrapMode = TextureWrapMode.Clamp;
                    positonsTex.LoadRawTextureData(positionsData);
                    positonsTex.Apply(false, true);
                    positonsTex.name = "CopyStampTreePositionsMap";
                }
                if (randomsTex == null && randomsData != null && randomsData.Length != 0)
                {
                    randomsTex = new Texture2D(dataSize.x, dataSize.y, TextureFormat.RGBAHalf, false, true);
                    randomsTex.wrapMode = TextureWrapMode.Clamp;
                    randomsTex.LoadRawTextureData(randomsData);
                    randomsTex.Apply(false, true);
                    randomsTex.name = "CopyStampRandomsMap";
                }
            }
        }

        [System.Serializable]
        public class DetailCopyData
        {
            [System.Serializable]
            public class Layer
            {
                public Texture2D texture;
                public byte[] bytes;
                public DetailPrototypeSerializable prototype;
                public Vector2Int dataSize;
            }
            public List<Layer> layers = new List<Layer>();

            public Layer FindOrCreateLayer(DetailPrototypeSerializable prototype)
            {
                foreach (var l in layers)
                {
                    if (l.prototype.Equals(prototype))
                        return l;
                }
                var nl = new Layer();
                nl.prototype = prototype;
                layers.Add(nl);
                return nl;
            }

            public void Unpack()
            {
                foreach (var l in layers)
                {
                    if (l.texture == null && l.bytes != null && l.bytes.Length > 0)
                    {
                        l.texture = new Texture2D(l.dataSize.x, l.dataSize.y, TextureFormat.R8, false, true);
                        l.texture.wrapMode = TextureWrapMode.Clamp;
                        l.texture.LoadRawTextureData(l.bytes);
                        l.texture.Apply(false, true);
                        l.texture.name = "CopyStampDetailMap";
                    }
                }

            }
        }

        public Texture2D heightMap;
        public Texture2D indexMap;
        public Texture2D weightMap;
        public TerrainLayer[] layers;
        public Vector2 heightRenorm;
        public TreeCopyData treeData;
        public DetailCopyData detailData;


        [HideInInspector] public byte[] heightData;
        [HideInInspector] public byte[] indexData;
        [HideInInspector] public byte[] weightData;
        [HideInInspector] public Vector2Int heightSize;
        [HideInInspector] public Vector2Int indexWeightSize;


        public static CopyStamp Create(
                Texture2D height,
                Texture2D index,
                Texture2D weight,
                TerrainLayer[] tLayers,
                Vector2 heightRenorm,
                TreeCopyData treeData,
                DetailCopyData detailData)
        {
            CopyStamp cs = CopyStamp.CreateInstance<CopyStamp>();
            cs.layers = tLayers;
            cs.heightRenorm = heightRenorm;
            cs.heightData = height != null ? height.GetRawTextureData() : null;
            cs.indexData = index != null ? index.GetRawTextureData() : null;
            cs.weightData = weight != null ? weight.GetRawTextureData() : null;
            if (height != null)
                cs.heightSize = new Vector2Int(height.width, height.height);
            if (index != null && weight != null)
                cs.indexWeightSize = new Vector2Int(index.width, index.height);
            cs.treeData = treeData;
            cs.detailData = detailData;
            return cs;

        }


        public void Unpack()
        {
            if (heightMap == null && heightData != null && heightData.Length != 0)
            {
                heightMap = new Texture2D(heightSize.x, heightSize.y, TextureFormat.R16, false, true);
                heightMap.wrapMode = TextureWrapMode.Clamp;
                heightMap.LoadRawTextureData(heightData);
                heightMap.Apply(false, true);
                heightMap.name = "CopyStampHeightMap";
            }
            if (indexMap == null && indexData != null && indexData.Length != 0)
            {
                indexMap = new Texture2D(indexWeightSize.x, indexWeightSize.y, TextureFormat.ARGB32, false, true);
                indexMap.LoadRawTextureData(indexData);
                indexMap.wrapMode = TextureWrapMode.Clamp;
                indexMap.filterMode = FilterMode.Point;
                indexMap.Apply(false, true);
                indexMap.name = "CopyStampIndexMap";
            }
            if (weightMap == null && weightData != null && weightData.Length != 0)
            {
                weightMap = new Texture2D(indexWeightSize.x, indexWeightSize.y, TextureFormat.ARGB32, false, true);
                weightMap.LoadRawTextureData(weightData);
                weightMap.wrapMode = TextureWrapMode.Clamp;
                weightMap.Apply(false, true);
                weightMap.name = "CopyStampWeightMap";
            }
            if (treeData != null)
                treeData.Unpack();
            if (detailData != null)
                detailData.Unpack();
        }
    }
}