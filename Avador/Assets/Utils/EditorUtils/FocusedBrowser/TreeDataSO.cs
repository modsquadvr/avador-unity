using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils.Editor
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Utils/TreeDataSO")]
    public class TreeDataSO : ScriptableObject
    {
        public List<AssetItemBones> ItemsBones = new List<AssetItemBones>();
        public int NextId = 2;

        public void SetItemsBones(List<AssetItemBones> new_list)
        {
            ItemsBones = new_list;
        }
    }

    [Serializable]
    public struct AssetItemBones
    {
        public int Id;
        public int ParentId;
        public string AssetPath;
        public bool Expanded;
        public bool IsFakeFolder;
        public string DisplayName;
        public float IconHueShift;

        public static AssetItemBones ConvertToBones(AssetTreeViewItem item, bool expanded)
        {
            return new AssetItemBones() 
            {
                Id = item.id,
                ParentId = item.parent.id,
                AssetPath = item.AssetPath,
                Expanded = expanded,
                IsFakeFolder = item.IsFakeFolder,
                DisplayName = item.displayName,
                IconHueShift = item.IconHueShift,
            };
        }
    }

}