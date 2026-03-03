using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using nadena.dev.modular_avatar.core;

namespace Lyra.Editor{
    [Serializable]
    public class MenuNode{
        public string Name;
        [SerializeField] public List<MenuEntry> Entries = new List<MenuEntry>();
    }

    [Serializable]
    public class MenuEntry{
        public string Name;
        public Texture2D Icon;
        public VRCExpressionsMenu.Control.ControlType Type;
        public string Parameter;
        public float Value;
        [SerializeReference] public MenuNode SubMenu;
        public VRCExpressionsMenu SourceAsset;
        public int SourceIndex;
        public VRCExpressionsMenu.Control.Label[] Labels;
        public ModularAvatarMenuInstaller SourceInstaller;
        public ModularAvatarMenuItem SourceMenuItem;
        public bool IsDynamic; 
        public bool IsCustomFolder;
        public bool IsEditorOnly;
        public bool IsBuildTime;
        public string UniqueId;
        public string PersistentId;
        public bool IsAutoOverflow;
        public bool IsNewEntry;
    }

    [Serializable]
    public class BreadcrumbEntry{
        [SerializeReference] public MenuNode Node;
        public string Name;
    }
}
