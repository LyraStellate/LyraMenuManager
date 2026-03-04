using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lyra{
    [DisallowMultipleComponent]
    [AddComponentMenu("Lyra/LMM Layout Data")]
    public class MenuLayoutData : MonoBehaviour, VRC.SDKBase.IEditorOnly{
        [Serializable]
        public class ItemLayout{
            public string Type;

            public string Key;

            public string ParentPath = "";

            public int Order;

            public bool IsSubMenu;

            public string DisplayName;

            public Texture2D CustomIcon;

            public bool IsAutoOverflow;

            public bool IsDynamic;

            public string SourceObjId;
        }

        [Header("Assets")]
        public MenuLayoutDataAsset BaseLayout;
        public MenuLayoutDataAsset ExtendedLayout;

        public List<ItemLayout> Items {
            get {
                if (BaseLayout == null && ExtendedLayout == null) return new List<ItemLayout>();
                
                var baseList = BaseLayout != null ? BaseLayout.Items : new List<ItemLayout>();
                if (ExtendedLayout == null) return baseList;

                var resultDict = new Dictionary<string, ItemLayout>();
                foreach (var it in baseList) {
                    if (!string.IsNullOrEmpty(it.Key)) resultDict[it.Key] = it;
                }

                foreach (var it in ExtendedLayout.Items) {
                    if (!string.IsNullOrEmpty(it.Key)) resultDict[it.Key] = it;
                }

                return new List<ItemLayout>(resultDict.Values);
            }
        }

        public string LastSavedAt {
            get {
                if (ExtendedLayout != null) return ExtendedLayout.LastSavedAt;
                if (BaseLayout != null) return BaseLayout.LastSavedAt;
                return "";
            }
        }

        public bool IsEnabled = true;

        public bool RemoveEmptyFolders = false;

        public bool EnableDebugLog = false;

        public bool EnableDetailedDebugLog = false;

        public List<string> RunAfterPlugins = new List<string>();
    }
}