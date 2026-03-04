 #if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Lyra;

namespace Lyra.Editor{
    public class MenuManager : EditorWindow{
        private const float WHEEL_RADIUS = 180f;
        private const float INNER_RADIUS = 55f;
        private const float ICON_SIZE = 36f;
        private const int MAX_CONTROLS = 8;

        private Color BG_DARK        => _cBgDark;
        private Color RING_BG        => _cRingBg;
        private Color SLICE_NORMAL   => _cSliceNormal;
        private Color SLICE_HOVER    => _cSliceHover;
        private Color SLICE_SELECTED => _cSliceSelected;
        private Color SLICE_DRAG_SRC => _cSliceDragSrc;
        private Color SLICE_DRAG_DST => _cSliceDragDst;
        private Color SLICE_DRAG_INTO=> _cSliceDragInto;
        private Color CENTER_DRAG    => _cCenterDrag;
        private Color ACCENT         => _cAccent;
        private Color ACCENT_SUB     => _cAccentSub;
        private Color TEXT_PRI       => _cTextPri;
        private Color TEXT_SEC       => _cTextSec;
        private Color SEPARATOR      => _cSeparator;
        private Color CRUMB_BG       => _cCrumbBg;
        private Color CENTER_BG      => _cCenterBg;
        private Color EMPTY_SLOT     => _cEmptySlot;

        private Color _cBgDark, _cRingBg, _cSliceNormal, _cSliceHover, _cSliceSelected, _cSliceDragSrc, _cSliceDragDst, _cSliceDragInto, _cCenterDrag, _cAccent, _cAccentSub, _cTextPri, _cTextSec, _cSeparator, _cCrumbBg, _cCenterBg, _cEmptySlot;

        private Color LoadColor(string key, Color def){
            string prefix = _useVRcStyleUI ? "VRC." : "Normal.";
            string fullKey = "Lyra.MenuManager.Color." + prefix + key;
            if (!EditorPrefs.HasKey(fullKey)) return def;
            if (ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(fullKey), out Color c)) return c;
            return def;
        }

        private void LoadColors(){
            _cBgDark        = LoadColor("BG_DARK", _useVRcStyleUI ? new Color(0.04f, 0.08f, 0.08f) : new Color(0.10f, 0.10f, 0.14f));
            _cRingBg        = LoadColor("RING_BG", _useVRcStyleUI ? new Color(0.102f, 0.349f, 0.380f, 0.97f) : new Color(0.14f, 0.15f, 0.20f, 0.97f));
            _cSliceNormal   = LoadColor("SLICE_NORMAL", _useVRcStyleUI ? new Color(0.094f, 0.231f, 0.251f, 0.88f) : new Color(0.18f, 0.20f, 0.28f, 0.88f));
            _cSliceHover    = LoadColor("SLICE_HOVER", _useVRcStyleUI ? new Color(0.133f, 0.294f, 0.314f, 0.95f) : new Color(0.28f, 0.42f, 0.78f, 0.92f));
            _cSliceSelected = LoadColor("SLICE_SELECTED", _useVRcStyleUI ? new Color(0.170f, 0.380f, 0.400f, 0.95f) : new Color(0.22f, 0.32f, 0.52f, 0.92f));
            _cSliceDragSrc  = LoadColor("SLICE_DRAG_SRC", _useVRcStyleUI ? new Color(0.05f, 0.10f, 0.10f, 0.60f) : new Color(0.55f, 0.32f, 0.18f, 0.80f));
            _cSliceDragDst  = LoadColor("SLICE_DRAG_DST", _useVRcStyleUI ? new Color(0.10f, 0.60f, 0.30f, 0.70f) : new Color(0.25f, 0.55f, 0.25f, 0.70f));
            _cSliceDragInto = LoadColor("SLICE_DRAG_INTO", _useVRcStyleUI ? new Color(0.20f, 0.50f, 0.50f, 0.85f) : new Color(0.50f, 0.28f, 0.68f, 0.85f));
            _cCenterDrag    = LoadColor("CENTER_DRAG", _useVRcStyleUI ? new Color(0.20f, 0.50f, 0.50f, 0.85f) : new Color(0.28f, 0.50f, 0.72f, 0.85f));
            _cAccent        = LoadColor("ACCENT", _useVRcStyleUI ? new Color(0.12f, 0.65f, 0.65f) : new Color(0.40f, 0.62f, 1.0f));
            _cAccentSub     = LoadColor("ACCENT_SUB", new Color(0.92f, 0.56f, 0.18f));
            _cTextPri       = LoadColor("TEXT_PRI", new Color(0.92f, 0.93f, 0.96f));
            _cTextSec       = LoadColor("TEXT_SEC", _useVRcStyleUI ? new Color(0.55f, 0.65f, 0.65f) : new Color(0.55f, 0.56f, 0.62f));
            _cSeparator     = LoadColor("SEPARATOR", _useVRcStyleUI ? new Color(0.102f, 0.349f, 0.380f, 0.55f) : new Color(0.28f, 0.28f, 0.36f, 0.55f));
            _cCrumbBg       = LoadColor("CRUMB_BG", _useVRcStyleUI ? new Color(0.05f, 0.12f, 0.12f, 0.97f) : new Color(0.12f, 0.12f, 0.16f, 0.97f));
            _cCenterBg      = LoadColor("CENTER_BG", _useVRcStyleUI ? new Color(0.22f, 0.22f, 0.22f, 1.0f) : new Color(0.11f, 0.12f, 0.17f, 1.0f));
            _cEmptySlot     = LoadColor("EMPTY_SLOT", _useVRcStyleUI ? new Color(0.05f, 0.10f, 0.10f, 0.35f) : new Color(0.16f, 0.16f, 0.22f, 0.35f));
        }

        private static readonly Color C_TOGGLE = new Color(0.30f, 0.72f, 0.42f);
        private static readonly Color C_BUTTON = new Color(0.62f, 0.38f, 0.82f);
        private static readonly Color C_RADIAL = new Color(0.82f, 0.62f, 0.22f);
        private static readonly Color C_2AXIS  = new Color(0.22f, 0.65f, 0.82f);
        private static readonly Color C_4AXIS  = new Color(0.82f, 0.32f, 0.42f);

        [SerializeField] private VRCAvatarDescriptor _avatar;
        [SerializeReference] private MenuNode _rootNode;
        [SerializeField] private List<BreadcrumbEntry> _navStack = new List<BreadcrumbEntry>();

        private int _hoverIdx = -1;
        [SerializeField] private int _selectedIdx = -1;
        private int _dragIdx = -1;
        private bool _isDragging;
        private bool _dragFromInventory;
        private Vector2 _dragStart;
        
        [SerializeField] private List<MenuEntry> _inventory = new List<MenuEntry>();
        [SerializeField] private List<MenuEntry> _extraOptions = new List<MenuEntry>();
        private Vector2 _inventoryScroll;
        private Dictionary<MenuEntry, bool> _inventoryFoldouts = new Dictionary<MenuEntry, bool>();
        private MenuEntry _dragInventoryEntry;
        private IList<MenuEntry> _dragInventoryList;
        private MenuEntry _selectedInventoryEntry;
        private bool _showDetail;
        private int _dropBorderIdx = -1;
        private int _dropCrumbIdx = -1;
        private int _editingNameIdx = -1;
        private string _editingNameStr = "";
        private bool _editingIsNewFolder = false;
        private bool _focusNameField = false;
        private MenuEntry _editingInventoryNameEntry;
        private string _editingInventoryNameStr = "";
        
        private MenuEntry _detailEditNameEntry;
        private string _detailEditNameStr = "";
        private bool _focusInventoryNameField = false;
        private double _lastInventorySelectTime;
        private double _lastRingSelectTime;

        private MenuEntry _readyToRenameInventory;
        private double _inventoryRenameDownTime;
        private int _readyToRenameRingIdx = -1;
        private double _ringRenameDownTime;

        [SerializeField] private bool _isMoveMode;
        [SerializeField] private MenuEntry _cutEntry;
        [SerializeReference] private MenuNode _cutSourceNode;
        [SerializeField] private bool _hasUnsavedChanges;
        [SerializeField] private bool _needsOverflowReeval;

        private Vector2 _scrollPos;

        private GUIStyle _sHeader, _sCrumb, _sLabel, _sSmall, _sBtn, _sCenter;
        private bool _stylesOk;
        private bool _useVRcStyleUI = true;
        private bool _showInventory = true;
        private bool _autoAddNewItemsToRoot = false;

        [MenuItem("Tools/Lyra/Menu Manager")]
        public static void ShowWindow(){
            var w = GetWindow<MenuManager>("Menu Manager");
            w.minSize = new Vector2(680, 800);
        }

        public static void ShowWindow(VRCAvatarDescriptor avatar){
            var w = GetWindow<MenuManager>("Menu Manager");
            w.minSize = new Vector2(680, 580);
            if (avatar != null && w._avatar != avatar){
                w._avatar = avatar;
                w.SaveLastAvatarIdentifier();
                w.ClearMenu();
                w.RebuildMenu();
            }
        }

        public void LoadSettings(){
            bool oldStyle = _useVRcStyleUI;
            _useVRcStyleUI = EditorPrefs.GetBool("Lyra.MenuManager.VRCStyle", true);
            _showInventory = EditorPrefs.GetBool("Lyra.MenuManager.ShowInventory", true);
            _autoAddNewItemsToRoot = EditorPrefs.GetBool("Lyra.MenuManager.AutoAddNewItemsToRoot", false);
            LoadColors();
            if (oldStyle != _useVRcStyleUI){
                _stylesOk = false;
            }
        }

        private void OnEnable(){
            if (_inventory == null) _inventory = new List<MenuEntry>();
            if (_navStack == null) _navStack = new List<BreadcrumbEntry>();
            if (_inventoryFoldouts == null) _inventoryFoldouts = new Dictionary<MenuEntry, bool>();

            LoadSettings();
            wantsMouseMove = true;
            Undo.undoRedoPerformed += Repaint;
            MenuManagerAuth.OnAuthChanged += Repaint;
            _stylesOk = false;
            
            EditorApplication.delayCall += RestoreLastAvatar;
        }

        private void SaveLastAvatarIdentifier(){
            if (_avatar == null) {
                EditorPrefs.DeleteKey("Lyra.MenuManager.LastAvatarGID");
                return;
            }
            string gid = GlobalObjectId.GetGlobalObjectIdSlow(_avatar).ToString();
            EditorPrefs.SetString("Lyra.MenuManager.LastAvatarGID", gid);
        }

        private void RestoreLastAvatar(){
            bool needsRebuild = false;

            if (_avatar == null){
                string gidStr = EditorPrefs.GetString("Lyra.MenuManager.LastAvatarGID", "");
                if (!string.IsNullOrEmpty(gidStr) && GlobalObjectId.TryParse(gidStr, out var gid)){
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
                    if (obj != null){
                        _avatar = obj.GetComponent<VRCAvatarDescriptor>();
                        if (_avatar != null) needsRebuild = true;
                    }
                }
            }
            else if (_rootNode == null){
                needsRebuild = true;
            }

            if (needsRebuild && _avatar != null){
                RebuildMenu();
            }
        }

        private void OnDisable(){
            Undo.undoRedoPerformed -= Repaint;
            MenuManagerAuth.OnAuthChanged -= Repaint;
        }

        private void OnSelectionChange(){
            FixVRCSDKUndoBugImmediate();
        }

        private void FixVRCSDKUndoBugImmediate(){
            var editors = Resources.FindObjectsOfTypeAll<UnityEditor.Editor>();
            foreach (var ed in editors){
                if (ed == null) continue;
                if (ed.GetType().Name == "VRCExpressionsMenuEditor"){
                    var field = ed.GetType().GetField("propControls", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && field.GetValue(ed) == null){
                        if (ed.serializedObject != null){
                            field.SetValue(ed, ed.serializedObject.FindProperty("controls"));
                        }
                    }
                }
            }
        }

        private void InitStyles(){
            if (_stylesOk) return;
            _sHeader = new GUIStyle(EditorStyles.boldLabel){
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TEXT_PRI }
            };
            _sCrumb = new GUIStyle(EditorStyles.miniButton){
                fontSize = 11,
                padding = new RectOffset(8, 8, 3, 3),
                normal = { textColor = ACCENT },
                hover = { textColor = Color.white }
            };
            _sLabel = new GUIStyle(EditorStyles.label){
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = TEXT_PRI }
            };
            _sSmall = new GUIStyle(EditorStyles.miniLabel){
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TEXT_SEC }
            };
            _sBtn = new GUIStyle(GUI.skin.button){
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(14, 14, 6, 6)
            };
            _sCenter = new GUIStyle(EditorStyles.label){
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = TEXT_SEC }
            };
            _stylesOk = true;
        }

        private void RebuildMenu(List<MenuLayoutData.ItemLayout> tempLayoutItems = null){
            if (_avatar == null) { ClearMenu(); return; }

            _inventory.Clear();

            var savedPath = new List<string>();
            foreach (var crumb in _navStack){
                savedPath.Add(crumb.Name);
            }

            try{
                var installers = _avatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true);

                var targetedInstallers = new HashSet<ModularAvatarMenuInstaller>();
                var installTargetType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.FullName == "nadena.dev.modular_avatar.core.ModularAvatarMenuInstallTarget");

                if (installTargetType != null){
                    var installTargetComponents = _avatar.GetComponentsInChildren(installTargetType, true);
                    var installerField = installTargetType.GetField("installer",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (installerField != null){
                        foreach (var t in installTargetComponents){
                            var inst = installerField.GetValue(t) as ModularAvatarMenuInstaller;
                            if (inst != null)
                                targetedInstallers.Add(inst);
                        }
                    }
                }

                var menuToInstallers = new Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>>();
                var rootInstallers = new List<ModularAvatarMenuInstaller>();

                foreach (var inst in installers){
                    if (!inst.enabled) continue;
                    if (targetedInstallers.Contains(inst)) continue;

                    if (inst.installTargetMenu != null){
                        if (!menuToInstallers.ContainsKey(inst.installTargetMenu))
                            menuToInstallers[inst.installTargetMenu] = new List<ModularAvatarMenuInstaller>();
                        menuToInstallers[inst.installTargetMenu].Add(inst);
                    }
                    else{
                        rootInstallers.Add(inst);
                    }
                }

                var visited = new HashSet<VRCExpressionsMenu>();
                _rootNode = BuildMenuNode(
                    _avatar.expressionsMenu, visited,
                    menuToInstallers, rootInstallers, true);
                _rootNode.Name = _avatar.gameObject.name;

                AssignUniqueIds(_rootNode, new Dictionary<string, int>());

                MarkEditorOnly(_rootNode, false);

                _navStack.Clear();
                _navStack.Add(new BreadcrumbEntry { Node = _rootNode, Name = _rootNode.Name });
                _selectedIdx = -1;
                _showDetail = false;

                if (savedPath.Count > 1){
                    var curr = _rootNode;
                    for (int i = 1; i < savedPath.Count; i++){
                        var targetName = savedPath[i];
                        bool found = false;
                        foreach (var e in curr.Entries){
                            if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && 
                                e.SubMenu != null && 
                                (e.Name ?? "") == (targetName ?? "")){
                                curr = e.SubMenu;
                                _navStack.Add(new BreadcrumbEntry { Node = curr, Name = targetName });
                                found = true;
                                break;
                            }
                        }
                        if (!found) break;
                    }
                }

                ApplyLayoutToTree(tempLayoutItems);
            }
            catch (Exception ex){
                Debug.LogError($"[MenuManager] メニュー構築失敗: {ex}");
                ClearMenu();
            }
            Repaint();
        }

        private void ApplyLayoutToTree(List<MenuLayoutData.ItemLayout> tempLayoutItems = null){
            if (_avatar == null || _rootNode == null) return;

            List<MenuLayoutData.ItemLayout> layoutItems = tempLayoutItems;
            if (layoutItems == null) {
                var layoutData = _avatar.GetComponent<MenuLayoutData>();
                if (layoutData == null || layoutData.Items.Count == 0) {
                    ExtractEditorOnly(_rootNode, _inventory);
                    ApplyOverflowMoreRecursive(_rootNode);
                    return;
                }
                layoutItems = layoutData.Items;
            }

            Debug.Log($"[MenuManager] レイアウトを復元: {layoutItems.Count} アイテム");

            _rootNode.Entries = FlattenMoreMenus(_rootNode.Entries);

            _inventory.Clear();
            var pool = new List<MenuEntry>();
            ExtractMappedEntries(_rootNode, layoutItems, pool);

            var newEntries = new List<MenuEntry>();
            CollectRemainingEntries(_rootNode, pool, newEntries);

            RebuildNodeLevel(_rootNode, "", layoutItems, pool);

            var invDummy = new MenuNode();
            RebuildNodeLevel(invDummy, "__INVENTORY__", layoutItems, pool);
            _inventory.AddRange(invDummy.Entries);

            if (pool.Count > 0){
                _inventory.AddRange(pool);
            }

            bool addedToRoot = false;
            foreach (var ne in newEntries){
                ne.IsNewEntry = true;
                MarkNewEntryRecursive(ne);
                if (_autoAddNewItemsToRoot){
                    _rootNode.Entries.Add(ne);
                    addedToRoot = true;
                }
                else {
                    _inventory.Add(ne);
                }
            }

            ExtractEditorOnly(_rootNode, _inventory);
            ApplyOverflowMoreRecursive(_rootNode);

            if (_autoAddNewItemsToRoot && addedToRoot) {
                EditorApplication.delayCall += () => {
                    if (this != null) {
                        SaveLayout(true);
                        Repaint();
                    }
                };
            }
        }

        private void ReevaluateOverflow(){
            if (_rootNode == null) return;
            
            var savedPath = new List<string>();
            foreach (var crumb in _navStack) savedPath.Add(crumb.Name);

            _rootNode.Entries = FlattenMoreMenus(_rootNode.Entries);
            
            ApplyOverflowMoreRecursive(_rootNode);

            _navStack.Clear();
            _navStack.Add(new BreadcrumbEntry { Node = _rootNode, Name = _rootNode.Name });
            var curr = _rootNode;
            for (int i = 1; i < savedPath.Count; i++){
                var targetName = savedPath[i];
                bool found = false;
                foreach (var e in curr.Entries){
                    if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null &&
                        (e.Name ?? "") == (targetName ?? "")){
                        curr = e.SubMenu;
                        _navStack.Add(new BreadcrumbEntry { Node = curr, Name = targetName });
                        found = true;
                        break;
                    }
                }
                if (!found) break; 
            }
        }

        private void MarkEditorOnly(MenuNode node, bool isParentEditorOnly){
            if (node == null || node.Entries == null) return;
            foreach (var e in node.Entries){
                bool isLocalEditorOnly = false;
                if (e.SourceInstaller != null && IsEditorOnly(e.SourceInstaller.gameObject))
                    isLocalEditorOnly = true;
                if (e.SourceMenuItem != null && IsEditorOnly(e.SourceMenuItem.gameObject))
                    isLocalEditorOnly = true;

                e.IsEditorOnly = isParentEditorOnly || isLocalEditorOnly;

                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    MarkEditorOnly(e.SubMenu, e.IsEditorOnly);
                }
            }
        }

        private void AssignUniqueIds(MenuNode node, Dictionary<string, int> counts){
            if (node == null || node.Entries == null) return;
            for (int i = 0; i < node.Entries.Count; i++){
                var e = node.Entries[i];
                string baseKey;
                if (e.IsCustomFolder) {
                    baseKey = $"SubMenu:{e.Name}:__custom__:1.00";
                } else {
                    baseKey = $"{e.Type}:{e.Name}:{e.Parameter ?? ""}:{e.Value:F2}";
                }
                if (!counts.ContainsKey(baseKey)) counts[baseKey] = 0;
                e.UniqueId = baseKey + ":" + counts[baseKey]++;
                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    AssignUniqueIds(e.SubMenu, counts);
                }
            }
        }

        private bool IsEditorOnly(GameObject go){
            if (go == null) return false;
            Transform t = go.transform;
            while (t != null){
                if (t.gameObject.CompareTag("EditorOnly")) return true;
                t = t.parent;
            }
            return false;
        }

        private void ExtractEditorOnly(MenuNode node, List<MenuEntry> extracted){
            if (node == null || node.Entries == null) return;

            for (int i = node.Entries.Count - 1; i >= 0; i--){
                var e = node.Entries[i];
                if (e.IsEditorOnly){
                    node.Entries.RemoveAt(i);
                    extracted.Add(e);
                }
                else if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    ExtractEditorOnly(e.SubMenu, extracted);
                }
            }
        }

        private void CollectRemainingEntries(MenuNode node, List<MenuEntry> pool, List<MenuEntry> collected){
            if (node != null && node.Entries != null){
                for (int i = node.Entries.Count - 1; i >= 0; i--){
                    var e = node.Entries[i];
                    node.Entries.RemoveAt(i);
                    collected.Insert(0, e);
                }
            }

            if (pool != null){
                foreach (var p in pool){
                    if (p.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && p.SubMenu != null && p.SubMenu.Entries != null){
                        for (int i = p.SubMenu.Entries.Count - 1; i >= 0; i--){
                            var e = p.SubMenu.Entries[i];
                            p.SubMenu.Entries.RemoveAt(i);
                            collected.Insert(0, e);
                        }
                    }
                }
            }
        }

        private void MarkNewEntryRecursive(MenuEntry entry){
            if (entry == null) return;
            entry.IsNewEntry = true;
            if (entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && entry.SubMenu != null){
                foreach (var sub in entry.SubMenu.Entries){
                    MarkNewEntryRecursive(sub);
                }
            }
        }

        private void ExtractMappedEntries(MenuNode node, List<MenuLayoutData.ItemLayout> layoutItems, List<MenuEntry> pool){
            if (node == null || node.Entries == null) return;

            for (int i = node.Entries.Count - 1; i >= 0; i--){
                var e = node.Entries[i];

                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    ExtractMappedEntries(e.SubMenu, layoutItems, pool);
                }

                if (layoutItems.Any(item => MatchEntry(e, item))){
                    node.Entries.RemoveAt(i);
                    pool.Add(e);
                }
            }
        }

        private void RebuildNodeLevel(MenuNode currentNode, string currentPath, List<MenuLayoutData.ItemLayout> layoutItems, List<MenuEntry> pool){
            var itemsInPath = layoutItems
                .Where(item => item.ParentPath == currentPath)
                .OrderBy(item => item.Order)
                .ToList();

            if (itemsInPath.Count == 0) return;

            var newEntries = new List<MenuEntry>();

            foreach (var item in itemsInPath){
                var e = FetchEntryFromPool(pool, item);

                if (e != null){
                    e.IsBuildTime = !string.IsNullOrEmpty(item.Type) ? item.Type == "__BUILD_TIME__" : item.Key == "__BUILD_TIME__";
                    string tk = !string.IsNullOrEmpty(item.Type) ? item.Type : "";
                    if (tk.Contains(":__custom__:")) e.IsCustomFolder = true;
                    if (item.IsSubMenu){
                        if (e.SubMenu == null) e.SubMenu = new MenuNode { Name = e.Name ?? item.DisplayName };
                        string subPath = string.IsNullOrEmpty(currentPath) ? (e.Name ?? item.DisplayName) : currentPath + "/" + (e.Name ?? item.DisplayName);
                        RebuildNodeLevel(e.SubMenu, subPath, layoutItems, pool);
                    }
                    newEntries.Add(e);
                }
                else if (item.IsSubMenu){
                    string tk = !string.IsNullOrEmpty(item.Type) ? item.Type : "";
                    bool isCustom = tk.Contains(":__custom__:");
                    bool isOverflow = item.IsAutoOverflow;
                    if (isCustom || isOverflow){
                        var virtualSub = new MenuNode { Name = item.DisplayName };
                        var virtualEntry = new MenuEntry{
                            Name = item.DisplayName,
                            Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                            SubMenu = virtualSub,
                            IsCustomFolder = isCustom,
                            Icon = item.CustomIcon,
                            UniqueId = item.Key,
                            PersistentId = item.Key,
                            IsAutoOverflow = isOverflow
                        };
                        string subPath = string.IsNullOrEmpty(currentPath) ? item.DisplayName : currentPath + "/" + item.DisplayName;
                        RebuildNodeLevel(virtualSub, subPath, layoutItems, pool);
                        newEntries.Add(virtualEntry);
                    }
                }
                else if ((!string.IsNullOrEmpty(item.Type) ? item.Type : item.Key) == "__BUILD_TIME__"){
                    var virtualEntry = new MenuEntry{
                        Name = item.DisplayName,
                        Type = VRCExpressionsMenu.Control.ControlType.Button,
                        Icon = item.CustomIcon,
                        IsBuildTime = true,
                        UniqueId = item.Key,
                        PersistentId = item.Key
                    };
                    newEntries.Add(virtualEntry);
                }
            }

            if (currentNode.Entries != null){
                newEntries.AddRange(currentNode.Entries);
            }
            currentNode.Entries = newEntries;
        }

        private bool MatchEntry(MenuEntry e, MenuLayoutData.ItemLayout itemLayout){
            if (!string.IsNullOrEmpty(e.PersistentId) && e.PersistentId == itemLayout.Key) return true;

            string typeKey = !string.IsNullOrEmpty(itemLayout.Type) ? itemLayout.Type : itemLayout.Key;
            if (GenerateTypeKey(e) == typeKey) return true;

            string[] parts = typeKey.Split(new[] { ':' }, 4);
            string typeStr = parts.Length > 0 ? parts[0] : "";
            string nameStr = parts.Length > 1 ? parts[1] : itemLayout.DisplayName;

            if (e.Type.ToString() == typeStr && e.Name == nameStr) return true;
            if (e.Name == nameStr) return true;

            return false;
        }

        private MenuEntry FetchEntryFromPool(List<MenuEntry> pool, MenuLayoutData.ItemLayout itemLayout){
            int idx = pool.FindIndex(e => !string.IsNullOrEmpty(e.PersistentId) && e.PersistentId == itemLayout.Key);

            if (idx < 0) {
                string typeKey = !string.IsNullOrEmpty(itemLayout.Type) ? itemLayout.Type : itemLayout.Key;
                idx = pool.FindIndex(e => GenerateTypeKey(e) == typeKey);
            }

            if (idx < 0) {
                string typeKey = !string.IsNullOrEmpty(itemLayout.Type) ? itemLayout.Type : itemLayout.Key;
                string[] parts = typeKey.Split(new[] { ':' }, 4);
                string typeStr = parts.Length > 0 ? parts[0] : "";
                string nameStr = parts.Length > 1 ? parts[1] : itemLayout.DisplayName;
                idx = pool.FindIndex(e => e.Type.ToString() == typeStr && e.Name == nameStr);
                if (idx < 0) idx = pool.FindIndex(e => e.Name == nameStr);
            }

            if (idx >= 0){
                var e = pool[idx];
                pool.RemoveAt(idx);
                e.PersistentId = itemLayout.Key;
                return e;
            }
            return null;
        }

        private MenuNode BuildMenuNode(
            VRCExpressionsMenu menu,
            HashSet<VRCExpressionsMenu> visited,
            Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>> menuToInstallers,
            List<ModularAvatarMenuInstaller> rootInstallers,
            bool isRoot){
            var node = new MenuNode();

            if (menu != null && !visited.Contains(menu)){
                visited.Add(menu);
                for (int i = 0; i < menu.controls.Count; i++){
                    node.Entries.Add(ConvertControl(
                        menu.controls[i], menu, i, visited,
                        menuToInstallers, rootInstallers));
                }

                if (menuToInstallers.TryGetValue(menu, out var targeted))
                    foreach (var inst in targeted)
                        AddInstallerEntries(node, inst, visited,
                            menuToInstallers, rootInstallers);
                visited.Remove(menu);
            }

            if (isRoot)
                foreach (var inst in rootInstallers)
                    AddInstallerEntries(node, inst, visited,
                        menuToInstallers, rootInstallers);

            return node;
        }

        private void ApplyOverflowMoreRecursive(MenuNode node){
            if (node == null || node.Entries == null) return;
            ApplyOverflowMore(node);
            for (int i = 0; i < node.Entries.Count; i++){
                var e = node.Entries[i];
                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    ApplyOverflowMoreRecursive(e.SubMenu);
                }
            }
        }

        private void ApplyOverflowMore(MenuNode node){
            while (node.Entries.Count > MAX_CONTROLS){
                var overflow = node.Entries.GetRange(MAX_CONTROLS - 1, node.Entries.Count - (MAX_CONTROLS - 1));
                node.Entries.RemoveRange(MAX_CONTROLS - 1, node.Entries.Count - (MAX_CONTROLS - 1));

                var moreNode = new MenuNode { Name = "More" };
                moreNode.Entries.AddRange(overflow);

                var moreEntry = new MenuEntry{
                    Name = "More",
                    Icon = GetIcon("overflow.png"),
                    Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    SubMenu = moreNode,
                    IsAutoOverflow = true
                };
                node.Entries.Add(moreEntry);

                node = moreNode;
            }
        }

        private MenuEntry ConvertControl(
            VRCExpressionsMenu.Control c,
            VRCExpressionsMenu src, int srcIdx,
            HashSet<VRCExpressionsMenu> visited,
            Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>> m2i,
            List<ModularAvatarMenuInstaller> ri){
            var e = new MenuEntry{
                Name = c.name,
                Icon = c.icon,
                Type = c.type,
                Parameter = c.parameter?.name ?? "",
                Value = c.value,
                SourceAsset = src,
                SourceIndex = srcIdx,
                Labels = c.labels
            };
            if (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu){
                if (c.subMenu != null){
                    e.SubMenu = BuildMenuNode(c.subMenu, visited, m2i, ri, false);
                    e.SubMenu.Name = c.name;
                }

                if (e.SubMenu == null){
                    e.SubMenu = new MenuNode { Name = c.name };
                    e.IsDynamic = true;
                }
                else if (e.SubMenu.Entries.Count == 0){
                    e.IsDynamic = true;
                }
            }
            return e;
        }

        private void AddInstallerEntries(
            MenuNode node,
            ModularAvatarMenuInstaller installer,
            HashSet<VRCExpressionsMenu> visited,
            Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>> m2i,
            List<ModularAvatarMenuInstaller> ri){
            var menuGroup = installer.GetComponent<ModularAvatarMenuGroup>();
            if (menuGroup != null){
                var root = menuGroup.targetObject != null ? menuGroup.targetObject : menuGroup.gameObject;
                foreach (Transform ch in root.transform){
                    var childMi = ch.GetComponent<ModularAvatarMenuItem>();
                    if (childMi != null){
                        var e = ConvertMAMenuItem(childMi, visited, m2i, ri);
                        e.SourceInstaller = installer;
                        node.Entries.Add(e);
                    }
                }
                return;
            }

            var menuItem = installer.GetComponent<ModularAvatarMenuItem>();
            if (menuItem != null){
                var e = ConvertMAMenuItem(menuItem, visited, m2i, ri);
                e.SourceInstaller = installer;
                e.SourceMenuItem = menuItem;
                node.Entries.Add(e);
                return;
            }

            if (installer.menuToAppend != null){
                if (!visited.Contains(installer.menuToAppend)){
                    visited.Add(installer.menuToAppend);
                    for (int i = 0; i < installer.menuToAppend.controls.Count; i++){
                        var e = ConvertControl(
                            installer.menuToAppend.controls[i],
                            installer.menuToAppend, i, visited, m2i, ri);
                        e.SourceInstaller = installer;
                        node.Entries.Add(e);
                    }

                    if (m2i.TryGetValue(installer.menuToAppend, out var sub))
                        foreach (var si in sub)
                            AddInstallerEntries(node, si, visited, m2i, ri);
                    visited.Remove(installer.menuToAppend);
                }
                return;
            }

            var dynEntry = new MenuEntry{
                Name = installer.gameObject.name,
                Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                SourceInstaller = installer,
                IsDynamic = true
            };
            node.Entries.Add(dynEntry);
        }

        private MenuEntry ConvertMAMenuItem(
            ModularAvatarMenuItem mi,
            HashSet<VRCExpressionsMenu> visited,
            Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>> m2i,
            List<ModularAvatarMenuInstaller> ri){
            var ctrl = mi.Control ?? new VRCExpressionsMenu.Control();
            string name = string.IsNullOrEmpty(mi.label) ? mi.gameObject.name : mi.label;

            var type = ctrl.type != 0
                ? ctrl.type
                : VRCExpressionsMenu.Control.ControlType.Toggle;

            var e = new MenuEntry{
                Name = name,
                Icon = ctrl.icon,
                Type = type,
                Parameter = ctrl.parameter?.name ?? "",
                Value = ctrl.value,
                SourceMenuItem = mi,
                Labels = ctrl.labels
            };

            if (type == VRCExpressionsMenu.Control.ControlType.SubMenu){
                if (mi.MenuSource == SubmenuSource.Children){
                    e.SubMenu = BuildNodeFromChildren(mi, visited, m2i, ri);
                    e.SubMenu.Name = name;
                }
                else if (mi.MenuSource == SubmenuSource.MenuAsset && ctrl.subMenu != null){
                    e.SubMenu = BuildMenuNode(ctrl.subMenu, visited, m2i, ri, false);
                    e.SubMenu.Name = name;
                }

                if (e.SubMenu == null){
                    e.SubMenu = new MenuNode { Name = name };
                    e.IsDynamic = true;
                }
                else if (e.SubMenu.Entries.Count == 0){
                    e.IsDynamic = true;
                }
            }
            return e;
        }

        private MenuNode BuildNodeFromChildren(
            ModularAvatarMenuItem parentItem,
            HashSet<VRCExpressionsMenu> visited,
            Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>> m2i,
            List<ModularAvatarMenuInstaller> ri){
            var node = new MenuNode();
            var root = parentItem.menuSource_otherObjectChildren != null
                ? parentItem.menuSource_otherObjectChildren
                : parentItem.gameObject;
            foreach (Transform ch in root.transform){
                var childMi = ch.GetComponent<ModularAvatarMenuItem>();
                if (childMi != null)
                    node.Entries.Add(ConvertMAMenuItem(childMi, visited, m2i, ri));
            }
            return node;
        }

        private void ClearMenu(){
            _rootNode = null;
            _navStack.Clear();
            _inventory.Clear();
            _selectedIdx = -1;
            _selectedInventoryEntry = null;
            _showDetail = false;
            _editingNameIdx = -1;
            CancelMoveMode();
            _hasUnsavedChanges = false;
        }

        private void CancelMoveMode(){
            _isMoveMode = false;
            _cutEntry = null;
            _cutSourceNode = null;
        }

        private void OnGUI(){
            if (_navStack == null) _navStack = new List<BreadcrumbEntry>();
            if (_inventory == null) _inventory = new List<MenuEntry>();
            if (_extraOptions == null) _extraOptions = new List<MenuEntry>();

            InitStyles();

            if (_avatar == null && _rootNode != null){
                ClearMenu();
            }

            DrawAvatarField();

            EditorGUILayout.BeginHorizontal();

            if (_rootNode != null && _navStack.Count > 0){
                bool corrupted = false;
                for (int i = 0; i < _navStack.Count; i++){
                    if (_navStack[i] == null || _navStack[i].Node == null){
                        corrupted = true;
                        break;
                    }
                }
                if (corrupted){
                    _navStack.Clear();
                    _navStack.Add(new BreadcrumbEntry { Node = _rootNode, Name = _rootNode.Name ?? _avatar?.gameObject.name ?? "Root" });
                }

                DrawInventoryArea();
            }

            EditorGUILayout.BeginVertical();

            if (_rootNode == null || _navStack.Count == 0){
                DrawDropZone();
            }
            else{
                DrawBreadcrumbs();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);

                string mainBtnText = _useVRcStyleUI ? "VRChatUI" : "NormalUI";
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = _useVRcStyleUI ? new Color(0.4f, 0.8f, 0.9f) : new Color(0.6f, 0.6f, 0.6f);
                if (GUILayout.Button(mainBtnText, GUILayout.Width(80), GUILayout.Height(30))){
                    _useVRcStyleUI = !_useVRcStyleUI;
                    EditorPrefs.SetBool("Lyra.MenuManager.VRCStyle", _useVRcStyleUI);
                    LoadSettings();
                    
                    var wins = Resources.FindObjectsOfTypeAll<MenuManagerSettings>();
                    foreach (var w in wins){
                        if (w != null) w.Repaint();
                    }
                }
                GUI.backgroundColor = prevBg;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(EditorGUIUtility.IconContent("SettingsIcon"), GUILayout.Width(36), GUILayout.Height(30))){
                    var wins = Resources.FindObjectsOfTypeAll<MenuManagerSettings>();
                    if (wins.Length > 0){
                        foreach (var w in wins){
                            if (w != null) w.Close();
                        }
                    }
                    else{
                        MenuManagerSettings.ShowWindow();
                    }
                }

                GUILayout.Space(12);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(4);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                DrawRadialWheel();
                DrawToolbar();
                if (_showDetail && (_selectedIdx >= 0 || _selectedInventoryEntry != null))
                    DrawDetailPanel();
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (_needsOverflowReeval){
                _needsOverflowReeval = false;
                ReevaluateOverflow();
                Repaint();
            }

            if (Event.current != null && Event.current.rawType == EventType.MouseUp){
                if (_isDragging){
                    _isDragging = false;
                    _dragFromInventory = false;
                    _dragIdx = -1;
                    _dropBorderIdx = -1;
                    _dropCrumbIdx = -1;
                    Repaint();
                }
            }

            if (Event.current != null && Event.current.type == EventType.MouseDown && Event.current.button == 0){
                if (_selectedIdx != -1 || _selectedInventoryEntry != null){
                    _selectedIdx = -1;
                    _selectedInventoryEntry = null;
                    _showDetail = false;
                    ForceEndInlineRename();
                    Repaint();
                }
            }
        }
        private void DrawAvatarField(){
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            EditorGUI.BeginChangeCheck();
            var av = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "", _avatar, typeof(VRCAvatarDescriptor), true, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2 + 4));
            if (EditorGUI.EndChangeCheck()){
                _avatar = av;
                SaveLastAvatarIdentifier();
                ClearMenu();
                if (_avatar != null) RebuildMenu();
            }

            if (_avatar != null){
                var layoutData = _avatar.GetComponent<MenuLayoutData>();
                if (layoutData != null){
                    EditorGUILayout.BeginVertical(GUILayout.Width(250));
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Base", GUILayout.Width(60));
                    EditorGUI.BeginChangeCheck();
                    var bAsset = (MenuLayoutDataAsset)EditorGUILayout.ObjectField(
                         "", layoutData.BaseLayout, typeof(MenuLayoutDataAsset), false);
                    if (EditorGUI.EndChangeCheck()){
                        Undo.RecordObject(layoutData, "Change Base Layout");
                        layoutData.BaseLayout = bAsset;
                        EditorUtility.SetDirty(layoutData);
                    }
                    if (layoutData.BaseLayout == null && GUILayout.Button("New", GUILayout.Width(40))){
                        layoutData.BaseLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Base");
                        EditorUtility.SetDirty(layoutData);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Extended", GUILayout.Width(60));
                    EditorGUI.BeginChangeCheck();
                    var eAsset = (MenuLayoutDataAsset)EditorGUILayout.ObjectField(
                         "", layoutData.ExtendedLayout, typeof(MenuLayoutDataAsset), false);
                    if (EditorGUI.EndChangeCheck()){
                        Undo.RecordObject(layoutData, "Change Extended Layout");
                        layoutData.ExtendedLayout = eAsset;
                        EditorUtility.SetDirty(layoutData);
                    }
                    if (layoutData.ExtendedLayout == null && GUILayout.Button("New", GUILayout.Width(40))){
                        layoutData.ExtendedLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Extended");
                        EditorUtility.SetDirty(layoutData);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }
            else{
                EditorGUILayout.HelpBox("アバターを選択してください", MessageType.Info);
            }

            GUILayout.Space(16);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        private MenuLayoutDataAsset CreateNewLayoutAsset(string suffix){
            string folderPath = "Assets/Lyra/EditorTools/MenuManager/Data";
            if (!AssetDatabase.IsValidFolder(folderPath)){
                string[] parts = folderPath.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++){
                    if (!AssetDatabase.IsValidFolder(current + "/" + parts[i])){
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current += "/" + parts[i];
                }
            }
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{suffix}.asset");
            var newAsset = CreateInstance<MenuLayoutDataAsset>();
            AssetDatabase.CreateAsset(newAsset, assetPath);
            AssetDatabase.SaveAssets();
            return newAsset;
        }

        private void DrawDropZone(){
            var evt = Event.current;
            var r = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
            r.x += 20;
            r.width -= 40;

            var bc = DragAndDrop.visualMode == DragAndDropVisualMode.Copy ? ACCENT : SEPARATOR;
            DrawBorder(r, bc, 2f);

            GUI.Label(r,
                "ここにアバターを\nドラッグ＆ドロップ\n\n⇩",
                new GUIStyle(_sHeader){
                    fontSize = 16,
                    normal = { textColor = TEXT_SEC },
                    wordWrap = true
                });

            if (evt.type == EventType.DragUpdated && r.Contains(evt.mousePosition)){
                DragAndDrop.visualMode = GrabAvatarFromDrag() != null
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform && r.Contains(evt.mousePosition)){
                DragAndDrop.AcceptDrag();
                var desc = GrabAvatarFromDrag();
                if (desc != null){
                    _avatar = desc;
                    ClearMenu();
                    RebuildMenu();
                }
                evt.Use();
            }
        }

        private void DrawInventoryArea(){
            if (!_showInventory){
                var narrow = EditorGUILayout.BeginVertical(GUILayout.Width(30));
                EditorGUI.DrawRect(narrow, CRUMB_BG);
                DrawBorder(narrow, SEPARATOR, 1f);

                EditorGUILayout.Space(8);
                if (GUILayout.Button("▶", new GUIStyle(_sBtn) { padding = new RectOffset(0, 0, 0, 0) }, GUILayout.Width(24), GUILayout.Height(100))){
                    _showInventory = true;
                    EditorPrefs.SetBool("Lyra.MenuManager.ShowInventory", true);
                }
                EditorGUILayout.EndVertical();
                return;
            }

            if (Event.current.type == EventType.MouseUp){
                if (!_isDragging){
                    _dragInventoryEntry = null;
                    _dragInventoryList = null;
                }
            }

            var r = EditorGUILayout.BeginVertical(GUILayout.Width(220));
            EditorGUI.DrawRect(r, CRUMB_BG);
            DrawBorder(r, SEPARATOR, 1f);
            var evt = Event.current;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(" インベントリ", new GUIStyle(_sHeader) { alignment = TextAnchor.MiddleLeft });
            if (GUILayout.Button("◀", new GUIStyle(_sBtn) { padding = new RectOffset(0, 0, 0, 0) }, GUILayout.Width(24), GUILayout.Height(24))){
                _showInventory = false;
                EditorPrefs.SetBool("Lyra.MenuManager.ShowInventory", false);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ACCENT);
            EditorGUILayout.Space(4);

            _inventoryScroll = EditorGUILayout.BeginScrollView(_inventoryScroll);

            if (_inventory.Count == 0){
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField(" (空)", _sLabel);
            }
            else{
                DrawInventoryNode(_inventory, 0);
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.EndScrollView();

            if (_isDragging && r.Contains(evt.mousePosition)){
                if (!_dragFromInventory && _dragIdx >= 0){
                    EditorGUI.DrawRect(r, new Color(0.2f, 0.7f, 0.2f, 0.2f));
                    if (evt.type == EventType.MouseUp){
                        Undo.RecordObject(this, "Move to Inventory");
                        var moving = CurEntries()[_dragIdx];
                        CurEntries().RemoveAt(_dragIdx);
                        _inventory.AddRange(FlattenMoreMenus(new List<MenuEntry> { moving }).Where(e => !e.IsBuildTime));
                        _isDragging = false;
                        _dragIdx = -1;
                        _hasUnsavedChanges = true;
                        evt.Use();
                        Repaint();
                    }
                }
                else if (_dragFromInventory && _dragInventoryEntry != null){
                    if (evt.type == EventType.MouseUp){
                        Undo.RecordObject(this, "Move to Inventory Bottom");
                        var moving = _dragInventoryEntry;
                        _dragInventoryList.Remove(moving);
                        _inventory.Add(moving);
                        
                        _isDragging = false;
                        _dragFromInventory = false;
                        _dragIdx = -1;
                        _hasUnsavedChanges = true;
                        evt.Use();
                        Repaint();
                    }
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField(" Extra Option", new GUIStyle(_sSmall) { alignment = TextAnchor.MiddleLeft });
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), SEPARATOR);
            EditorGUILayout.Space(4);
            
            if (_extraOptions == null) _extraOptions = new List<MenuEntry>();
            
            for (int i = _extraOptions.Count - 1; i >= 0; i--){
                if (_extraOptions[i] == null || _extraOptions[i].Name != "ビルド日時"){
                    if (_extraOptions[i] != null) _inventory.Add(_extraOptions[i]);
                    _extraOptions.RemoveAt(i);
                }
            }

            if (_extraOptions.Count == 0){
                Texture2D uploadIcon = GetIcon("upload.png");

                _extraOptions.Add(new MenuEntry{
                    Name = "ビルド日時",
                    Icon = uploadIcon,
                    Type = VRCExpressionsMenu.Control.ControlType.Button,
                    IsDynamic = false,
                    IsBuildTime = true
                });
            }

            DrawInventoryNode(_extraOptions, 0);

            EditorGUILayout.Space(60);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("現在の階層\nを回収", new GUIStyle(_sBtn) { fontSize = 11 }, GUILayout.Height(36))){
                Undo.RecordObject(this, "Collect To Inventory");
                _inventory.AddRange(FlattenMoreMenus(CurEntries()).Where(e => !e.IsBuildTime));
                CurEntries().Clear();
                _hasUnsavedChanges = true;
                _selectedIdx = -1;
                Repaint();
            }
            if (GUILayout.Button("全回収", new GUIStyle(_sBtn) { fontSize = 11 }, GUILayout.Height(36))){
                Undo.RecordObject(this, "Collect All recursive");
                _inventory.AddRange(FlattenMoreMenus(_rootNode.Entries).Where(e => !e.IsBuildTime));
                _rootNode.Entries.Clear();
                _selectedIdx = -1;
                NavToLevel(0);
                _hasUnsavedChanges = true;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            if (evt.type == EventType.ContextClick && r.Contains(evt.mousePosition)){
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("フォルダ追加"), false, () => {
                    Undo.RecordObject(this, "Add Folder to Inventory");
                    _inventory.Add(new MenuEntry{
                        Name = "New Folder",
                        Icon = GetIcon("folder.png"),
                        Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        SubMenu = new MenuNode { Name = "New Folder" },
                        IsCustomFolder = true
                    });
                    _hasUnsavedChanges = true;
                    Repaint();
                });
                menu.ShowAsContext();
                evt.Use();
            }

            EditorGUILayout.EndVertical();
        }

        private List<MenuEntry> FlattenMoreMenus(IList<MenuEntry> source){
            var result = new List<MenuEntry>();
            foreach (var entry in source){
                if (IsMoreSubMenu(entry)){
                    result.AddRange(FlattenMoreMenus(entry.SubMenu.Entries));
                }
                else{
                    if (entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && entry.SubMenu != null){
                        entry.SubMenu.Entries = FlattenMoreMenus(entry.SubMenu.Entries);
                    }
                    result.Add(entry);
                }
            }
            return result;
        }

        private bool IsMoreSubMenu(MenuEntry entry){
            if (entry == null || entry.Type != VRCExpressionsMenu.Control.ControlType.SubMenu || entry.SubMenu == null) return false;
            return entry.IsAutoOverflow;
        }

        private void DrawInventoryNode(IList<MenuEntry> list, int depth){
            var evt = Event.current;
            for (int i = 0; i < list.Count; i++){
                var entry = list[i];
                var itemRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                int indent = depth * 16;
                itemRect.x += 8 + indent;
                itemRect.width -= 16 + indent;
                itemRect.y += 2;

                if (depth > 0){
                    EditorGUI.DrawRect(new Rect(itemRect.x - 10, itemRect.y + 4, 2, itemRect.height - 8), ACCENT_SUB * 0.4f);
                }

                bool isHover = itemRect.Contains(evt.mousePosition);
                bool isDraggingThis = _isDragging && _dragFromInventory && _dragInventoryEntry == entry;
                bool isSelected = _selectedInventoryEntry == entry;

                Color bg = isDraggingThis ? SLICE_DRAG_SRC : (isHover ? SLICE_HOVER : (isSelected ? SLICE_SELECTED : BG_DARK));
                EditorGUI.DrawRect(itemRect, bg);
                DrawBorder(itemRect, isSelected || isHover ? ACCENT * 0.5f : SEPARATOR, 1f);

                bool isSub = entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && entry.SubMenu != null;
                Rect labelRect = itemRect;
                labelRect.x += 5;
                labelRect.width -= 5;

                if (isSub){
                    Rect foldoutRect = new Rect(labelRect.x, labelRect.y + 4, 16, 16);
                    if (!_inventoryFoldouts.ContainsKey(entry)) _inventoryFoldouts[entry] = false;
                    _inventoryFoldouts[entry] = EditorGUI.Foldout(foldoutRect, _inventoryFoldouts[entry], GUIContent.none);
                    labelRect.x += 16;
                    labelRect.width -= 16;
                }

                if (entry.Icon != null){
                    Rect iconRect = new Rect(labelRect.x, labelRect.y + 2, 24, 24);
                    GUI.DrawTexture(iconRect, entry.Icon, ScaleMode.ScaleToFit);
                    labelRect.x += 28;
                    labelRect.width -= 28;
                }
                else{
                    DrawTypeIcon(new Vector2(labelRect.x + 12, labelRect.y + 14), entry, 20);
                    labelRect.x += 28;
                    labelRect.width -= 28;
                }

                var dispName = entry.IsDynamic ? $"*{entry.Name}" : entry.IsCustomFolder ? $".{entry.Name}" : entry.Name;
                var labelStyle = new GUIStyle(_sLabel);
                labelStyle.alignment = TextAnchor.MiddleLeft;
                if (entry.IsCustomFolder) labelStyle.fontStyle = FontStyle.Italic;
                
                bool isEmptyFolder = entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (entry.SubMenu == null || entry.SubMenu.Entries.Count == 0);

                if (entry.IsEditorOnly) labelStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
                else if (entry.IsNewEntry) labelStyle.normal.textColor = new Color(0.3f, 0.6f, 1.0f);
                else if (entry.IsAutoOverflow) labelStyle.normal.textColor = Color.gray;
                else if (isEmptyFolder) labelStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

                if (_editingInventoryNameEntry == entry){
                    GUI.SetNextControlName("InventoryRenameField");
                    _editingInventoryNameStr = EditorGUI.TextField(labelRect, _editingInventoryNameStr, new GUIStyle(EditorStyles.textField) { alignment = TextAnchor.MiddleLeft });
                    if (_focusInventoryNameField){
                        EditorGUI.FocusTextInControl("InventoryRenameField");
                        _focusInventoryNameField = false;
                    }

                    bool shouldCommit = false;
                    bool shouldCancel = false;

                    if (Event.current.isKey){
                        if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter){
                            shouldCommit = true;
                            Event.current.Use();
                        }
                        else if (Event.current.keyCode == KeyCode.Escape){
                            shouldCancel = true;
                            Event.current.Use();
                        }
                    }
                    else if (Event.current.type == EventType.MouseDown && !labelRect.Contains(Event.current.mousePosition)){
                        shouldCommit = true;
                    }

                    if (shouldCommit){
                        string finalName = string.IsNullOrWhiteSpace(_editingInventoryNameStr) ? GetOriginalName(entry) : _editingInventoryNameStr;
                        if (entry.Name != finalName){
                            Undo.RecordObject(this, "Rename Inventory Item");
                            entry.Name = finalName;
                            if (entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && entry.SubMenu != null){
                                entry.SubMenu.Name = finalName;
                            }
                            _hasUnsavedChanges = true;
                        }
                        _editingInventoryNameEntry = null;
                        GUI.FocusControl(null);
                        Repaint();
                    }
                    else if (shouldCancel){
                        _editingInventoryNameEntry = null;
                        GUI.FocusControl(null);
                        Repaint();
                    }
                }
                else{
                    GUI.Label(labelRect, dispName, labelStyle);
                }

                var tc = TypeColor(entry.Type);
                var badgeRect = new Rect(itemRect.xMax - 38, itemRect.y + 6, 32, 16);
                EditorGUI.DrawRect(badgeRect, tc * 0.25f);
                DrawBorder(badgeRect, tc * 0.6f, 1f);
                GUI.Label(badgeRect, TypeShort(entry.Type), new GUIStyle(_sSmall) { normal = { textColor = tc }, fontStyle = FontStyle.Bold });

                if (_isDragging && _dragFromInventory && _dragInventoryEntry != null && !isDraggingThis){
                    if (isHover && !IsDescendant(_dragInventoryEntry, entry)){
                        float relY = evt.mousePosition.y - itemRect.y;
                        int dropType = 0;

                        if (isSub && relY > itemRect.height * 0.25f && relY < itemRect.height * 0.75f){
                            dropType = 1;
                        }
                        else if (relY <= itemRect.height * 0.5f){
                            dropType = 0;
                        }
                        else{
                            dropType = 2;
                        }

                        if (dropType == 0) EditorGUI.DrawRect(new Rect(itemRect.x, itemRect.y - 1, itemRect.width, 3), ACCENT);
                        else if (dropType == 2) EditorGUI.DrawRect(new Rect(itemRect.x, itemRect.yMax - 2, itemRect.width, 3), ACCENT);
                        else if (dropType == 1) EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.7f, 0.2f, 0.35f));

                        if (evt.type == EventType.MouseUp && evt.button == 0){
                            Undo.RecordObject(this, "Reorder Inventory");
                            _dragInventoryList.Remove(_dragInventoryEntry);

                            if (dropType == 1){
                                entry.SubMenu.Entries.Add(_dragInventoryEntry);
                                _inventoryFoldouts[entry] = true;
                            }
                            else{
                                int insIdx = list.IndexOf(entry);
                                if (dropType == 2) insIdx++;
                                if (insIdx < 0) insIdx = 0;
                                if (insIdx > list.Count) insIdx = list.Count;
                                list.Insert(insIdx, _dragInventoryEntry);
                            }

                            _isDragging = false;
                            _dragFromInventory = false;
                            _dragIdx = -1;
                            _hasUnsavedChanges = true;
                            evt.Use();
                            Repaint();
                            return;
                        }
                    }
                }

                if (evt.type == EventType.MouseDown && evt.button == 0 && isHover){

                    if (_selectedInventoryEntry == entry && _editingInventoryNameEntry != entry && !entry.IsBuildTime){
                        if (labelRect.Contains(evt.mousePosition) && (EditorApplication.timeSinceStartup - _lastInventorySelectTime > 0.6f)){
                            _readyToRenameInventory = entry;
                            _inventoryRenameDownTime = EditorApplication.timeSinceStartup;
                        }
                    }

                    if (_selectedInventoryEntry != entry){
                        _lastInventorySelectTime = EditorApplication.timeSinceStartup;
                    }

                    _dragInventoryEntry = entry;
                    _dragInventoryList = list;
                    _dragStart = evt.mousePosition;
                    
                    _selectedInventoryEntry = entry;
                    _selectedIdx = -1;
                    _showDetail = true;
                    evt.Use();
                    Repaint();
                }
                else if (evt.type == EventType.MouseUp && evt.button == 0 && isHover){
                    if (_readyToRenameInventory == entry && !_isDragging){
                        if (EditorApplication.timeSinceStartup - _inventoryRenameDownTime < 0.4f){
                            if (labelRect.Contains(evt.mousePosition)){
                                _editingInventoryNameEntry = entry;
                                _editingInventoryNameStr = entry.Name ?? "";
                                _focusInventoryNameField = true;
                                evt.Use();
                                Repaint();
                            }
                        }
                    }
                    _readyToRenameInventory = null;
                }
                else if (evt.type == EventType.MouseDrag && evt.button == 0 && _dragInventoryEntry == entry){
                    if (!_isDragging && Vector2.Distance(_dragStart, evt.mousePosition) > 5f){
                        _isDragging = true;
                        _dragFromInventory = true;
                        _dragIdx = 999;
                        evt.Use();
                    }
                }
                else if (evt.type == EventType.ContextClick && isHover){
                    var menu = new GenericMenu();
                    var capList = list;
                    var capEntry = entry;
                    int entryIndex = capList.IndexOf(capEntry);

                    if (entryIndex > 0){
                        menu.AddItem(new GUIContent("上に移動"), false, () => {
                            Undo.RecordObject(this, "Move Inventory Item Up");
                            capList.RemoveAt(entryIndex);
                            capList.Insert(entryIndex - 1, capEntry);
                            _hasUnsavedChanges = true;
                            Repaint();
                        });
                    }
                    else menu.AddDisabledItem(new GUIContent("上に移動"));

                    if (entryIndex < capList.Count - 1){
                        menu.AddItem(new GUIContent("下に移動"), false, () => {
                            Undo.RecordObject(this, "Move Inventory Item Down");
                            capList.RemoveAt(entryIndex);
                            capList.Insert(entryIndex + 1, capEntry);
                            _hasUnsavedChanges = true;
                            Repaint();
                        });
                    }
                    else menu.AddDisabledItem(new GUIContent("下に移動"));

                    menu.AddSeparator("");
                    
                    if (capEntry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu){
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("サブフォルダ追加"), false, () => {
                            Undo.RecordObject(this, "Add Subfolder to Inventory");
                            if (capEntry.SubMenu == null) capEntry.SubMenu = new MenuNode { Name = capEntry.Name ?? "New Folder" };
                            _inventoryFoldouts[capEntry] = true;
                            if (capEntry.SubMenu.Entries == null) capEntry.SubMenu.Entries = new List<MenuEntry>();
                            
                            var newFolder = new MenuEntry{
                                Name = "New Folder",
                                Icon = GetIcon("folder.png"),
                                Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                SubMenu = new MenuNode { Name = "New Folder" },
                                IsCustomFolder = true
                            };

                            if (!MenuManagerAuth.ValidateLevel(depth + 1)) return;
                            capEntry.SubMenu.Entries.Add(newFolder);

                            _hasUnsavedChanges = true;
                            Repaint();
                        });
                    }

                    menu.AddItem(new GUIContent("名前を編集"), false, () => {
                        if (!CheckProLimitInventory(depth, "この階層での名前編集")) return;
                        _editingInventoryNameEntry = capEntry;
                        _editingInventoryNameStr = capEntry.Name ?? "";
                        _focusInventoryNameField = true;
                        Repaint();
                    });

                    menu.AddSeparator("");

                    if (!capEntry.IsBuildTime && capEntry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (capEntry.SourceInstaller != null || capEntry.SourceAsset != null || capEntry.SourceMenuItem != null)){
                        menu.AddItem(new GUIContent("リセット"), false, () => ResetSubmenu(capEntry, capList));
                    }

                    if (capEntry.IsCustomFolder || capEntry.IsBuildTime){
                        menu.AddItem(new GUIContent("削除"), false, () => {
                            Undo.RecordObject(this, "Delete Folder");

                            if (capEntry.SubMenu != null && capEntry.SubMenu.Entries.Count > 0){
                                int idx = capList.IndexOf(capEntry);
                                for (int i = 0; i < capEntry.SubMenu.Entries.Count; i++){
                                    capList.Insert(idx + 1 + i, capEntry.SubMenu.Entries[i]);
                                }
                                capEntry.SubMenu.Entries.Clear();
                            }
                            
                            capList.Remove(capEntry);
                            _hasUnsavedChanges = true;
                            Repaint();
                        });
                    }
                    else{
                        menu.AddDisabledItem(new GUIContent("削除 (作成したフォルダのみ可)"));
                    }

                    menu.ShowAsContext();
                    evt.Use();
                }

                if (isSub && _inventoryFoldouts.ContainsKey(entry) && _inventoryFoldouts[entry]){
                    DrawInventoryNode(entry.SubMenu.Entries, depth + 1);
                }
            }
        }

        private bool IsDescendant(MenuEntry potentialParent, MenuEntry target){
            if (potentialParent == target) return true;
            if (potentialParent.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && potentialParent.SubMenu != null){
                foreach (var e in potentialParent.SubMenu.Entries){
                    if (IsDescendant(e, target)) return true;
                }
            }
            return false;
        }

        private void DrawBreadcrumbs(){
            _dropCrumbIdx = -1;
            var r = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, CRUMB_BG);
            float x = r.x + 10;
            for (int i = 0; i < _navStack.Count; i++){
                var nm = string.IsNullOrEmpty(_navStack[i].Name) ? "Root" : _navStack[i].Name;
                var gc = new GUIContent(i == 0 ? nm : "› " + nm);
                var sz = _sCrumb.CalcSize(gc);
                var br = new Rect(x, r.y + 2, sz.x + 4, r.height - 4);
                if (i == _navStack.Count - 1)
                    EditorGUI.DrawRect(new Rect(br.x, br.yMax - 2, br.width, 2), ACCENT);

                var prevBg = GUI.backgroundColor;
                if (_isDragging && _dragIdx >= 0 && i < _navStack.Count - 1 && br.Contains(Event.current.mousePosition)){
                    _dropCrumbIdx = i;
                    GUI.backgroundColor = new Color(0.20f, 0.70f, 0.20f, 0.85f);

                    if (Event.current.rawType == EventType.MouseUp){
                        var targetNode = _navStack[i].Node;
                        if (_dragFromInventory && _dragInventoryEntry != null){
                            Undo.RecordObject(this, "Drop from Inventory");
                            var moving = _dragInventoryEntry;
                            _dragInventoryList.Remove(moving);
                            InsertItemWithOverflow(targetNode, targetNode.Entries.Count, moving);
                            _selectedInventoryEntry = null;
                            _selectedIdx = -1;
                            _hasUnsavedChanges = true;
                        }
                        else if (!_dragFromInventory && _dragIdx >= 0 && _dragIdx < CurEntries().Count){
                            Undo.RecordObject(this, "Move to Ancestor Menu");
                            var moving = CurEntries()[_dragIdx];
                            CurEntries().RemoveAt(_dragIdx);
                            InsertItemWithOverflow(targetNode, targetNode.Entries.Count, moving);
                            _selectedInventoryEntry = null;
                            _selectedIdx = -1;
                            _showDetail = false;
                            _hasUnsavedChanges = true;
                        }

                        _isDragging = false;
                        _dragFromInventory = false;
                        _dragIdx = -1;
                        _dropBorderIdx = -1;
                        _dropCrumbIdx = -1;
                        Event.current.Use();
                        GUI.backgroundColor = prevBg;
                        return;
                    }
                }

                if (GUI.Button(br, gc, _sCrumb))
                    NavToLevel(i);

                GUI.backgroundColor = prevBg;
                x += sz.x + 6;
            }
        }

        private void DrawRadialWheel(){
            if (_navStack == null || _navStack.Count == 0) return;
            var cur = _navStack[_navStack.Count - 1];
            if (cur == null || cur.Node == null) return;

            var entries = cur.Node.Entries;
            float ws = WHEEL_RADIUS * 2 + 80;
            var wr = GUILayoutUtility.GetRect(0, ws, GUILayout.ExpandWidth(true));
            
            float viewWidth = EditorGUIUtility.currentViewWidth - (_showInventory ? 220f : 30f) - 20f;
            var center = new Vector2(wr.x + viewWidth / 2f, wr.y + wr.height / 2f);
            
            var evt = Event.current;

            DrawDisc(center, WHEEL_RADIUS + 8, new Color(0.25f, 0.28f, 0.40f, 0.12f));
            DrawDisc(center, WHEEL_RADIUS + 3, RING_BG);

            int sliceN;
            float startA;
            bool isRoot = _navStack.Count == 1;
            int dummyOffset = _useVRcStyleUI ? (isRoot ? 2 : 1) : 0;

            if (_useVRcStyleUI){
                sliceN = isRoot ? (2 + entries.Count) : (1 + entries.Count);
                startA = 90f + (180f / sliceN);
            }
            else{
                sliceN = MAX_CONTROLS;
                startA = 90f;
            }
            float step = 360f / sliceN;

            _dropBorderIdx = -1;
            if (_isDragging && _dragIdx >= 0){
                float d = (evt.mousePosition - center).magnitude;
                if (d > INNER_RADIUS - 20f && d < WHEEL_RADIUS + 40f){
                    int maxBorders = _useVRcStyleUI ? sliceN : entries.Count;
                    _dropBorderIdx = CalcNearestBorder(
                        evt.mousePosition, center, sliceN, startA, step, maxBorders);

                    if (_useVRcStyleUI && _dropBorderIdx >= 0 && _dropBorderIdx < dummyOffset){
                        _dropBorderIdx = dummyOffset + entries.Count;
                    }

                    if (!_dragFromInventory && _dragIdx >= 0 && _dropBorderIdx >= 0){
                        if (_dropBorderIdx == dummyOffset + _dragIdx || _dropBorderIdx == dummyOffset + _dragIdx + 1){
                            _dropBorderIdx = -1;
                        }
                    }
                }
            }

            _hoverIdx = -1;
            if (InRing(evt.mousePosition, center) && _dropBorderIdx < 0){
                _hoverIdx = CalcSlice(evt.mousePosition, center, sliceN, startA);
            }

            for (int i = 0; i < sliceN; i++){
                float a0 = startA - i * step;
                float a1 = a0 - step;
                float mid = (a0 + a1) / 2f;
                float midR = mid * Mathf.Deg2Rad;
                float itemR = (INNER_RADIUS + WHEEL_RADIUS) / 2f;
                var ic = center + new Vector2(Mathf.Cos(midR), -Mathf.Sin(midR)) * itemR;

                bool isDummyBack = _useVRcStyleUI && i == 0;
                bool isDummyQA = _useVRcStyleUI && isRoot && i == 1;
                bool isRealItem = !_useVRcStyleUI ? i < entries.Count : i >= dummyOffset;
                int entryIdx = isRealItem ? i - dummyOffset : -1;

                bool has = isRealItem;
                MenuEntry entry = isRealItem ? entries[entryIdx] : null;

                bool hover = (_hoverIdx == i);

                Color col;
                bool isDragIntoSub = _isDragging && hover && _dragIdx != entryIdx
                    && has && entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu
                    && entry.SubMenu != null;
                bool isDragIntoBack = _isDragging && hover && isDummyBack && !isRoot;

                if (_isDragging && hover && _dropBorderIdx < 0 && (has || isDummyBack)){
                    if (isDragIntoSub || isDragIntoBack)
                        col = new Color(0.20f, 0.70f, 0.20f, 0.85f);
                    else
                        col = new Color(0.80f, 0.20f, 0.20f, 0.85f);
                }
                else{
                    if (isDummyBack || isDummyQA){
                        if (_useVRcStyleUI)
                            col = hover ? new Color(0.07f, 0.28f, 0.28f, 0.9f) : new Color(0.04f, 0.15f, 0.15f, 0.8f);
                        else
                            col = hover ? new Color(0.28f, 0.42f, 0.78f, 0.92f) : new Color(0.18f, 0.20f, 0.28f, 0.88f);
                    }
                    else if (!isRealItem){
                        bool isFirstEmpty = !_useVRcStyleUI && i == dummyOffset + entries.Count;
                        bool anyEmptyHovered = !_useVRcStyleUI && _hoverIdx >= dummyOffset + entries.Count;

                        if (_isDragging && _dragFromInventory && isFirstEmpty && anyEmptyHovered){
                            col = new Color(0.22f, 0.62f, 0.22f, 0.65f);
                        }
                        else if (_isDragging && _dragFromInventory){
                            col = EMPTY_SLOT;
                        }
                        else{
                            col = hover ? new Color(0.22f, 0.42f, 0.22f, 0.45f) : EMPTY_SLOT;
                        }
                    }
                    else if (_isDragging && _dragIdx == entryIdx)
                        col = SLICE_DRAG_SRC;
                    else if (hover)
                        col = SLICE_HOVER;
                    else if (_selectedIdx == entryIdx && isRealItem)
                        col = SLICE_SELECTED;
                    else
                        col = SLICE_NORMAL;
                }

                DrawSlice(center, INNER_RADIUS, WHEEL_RADIUS, a0, a1, col);

                float bRad = a0 * Mathf.Deg2Rad;
                DrawHandleLine(
                    center + new Vector2(Mathf.Cos(bRad), -Mathf.Sin(bRad)) * INNER_RADIUS,
                    center + new Vector2(Mathf.Cos(bRad), -Mathf.Sin(bRad)) * WHEEL_RADIUS,
                    SEPARATOR, 1.5f);

                if (isRealItem){
                    if (entry.Icon != null){
                        var ir = new Rect(ic.x - ICON_SIZE / 2f, ic.y - ICON_SIZE / 2f - 8,
                            ICON_SIZE, ICON_SIZE);
                        GUI.DrawTexture(ir, entry.Icon, ScaleMode.ScaleToFit);
                    }
                    else{
                        DrawTypeIcon(ic - new Vector2(0, 8), entry);
                    }

                    var labelRect = new Rect(ic.x - 52, ic.y + ICON_SIZE / 2f - 10, 104, 28);
                    
                    if (_editingNameIdx == entryIdx){
                        GUI.SetNextControlName("InlineRenameField");

                        if (Event.current.type == EventType.KeyDown){
                            if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter){
                                ApplyInlineRename(entry);
                                _editingNameIdx = -1;
                                _editingIsNewFolder = false;
                                GUI.FocusControl(null);
                                Event.current.Use();
                            }
                            else if (Event.current.keyCode == KeyCode.Escape){
                                ForceEndInlineRename(true);
                                Event.current.Use();
                            }
                        }

                        _editingNameStr = EditorGUI.TextField(labelRect, _editingNameStr, new GUIStyle(EditorStyles.textField) { alignment = TextAnchor.MiddleCenter });
                        
                        if (_focusNameField){
                            EditorGUI.FocusTextInControl("InlineRenameField");
                            _focusNameField = false;
                        }
                    }
                    else{
                        string displayName = entry.Name ?? "(no name)";
                        if (entry.IsDynamic) displayName = "*" + displayName;
                        else if (entry.IsCustomFolder) displayName = "." + displayName;
                        
                        var labelStyle = new GUIStyle(_sLabel);
                        if (entry.IsCustomFolder) labelStyle.fontStyle = FontStyle.Italic;
                        bool isEmptyFolder = entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (entry.SubMenu == null || entry.SubMenu.Entries.Count == 0);

                        if (entry.IsEditorOnly) labelStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
                        else if (entry.IsNewEntry) labelStyle.normal.textColor = new Color(0.3f, 0.6f, 1.0f);
                        else if (entry.IsAutoOverflow) labelStyle.normal.textColor = Color.gray;
                        else if (isEmptyFolder) labelStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
                        
                        GUI.Label(labelRect, displayName, labelStyle);
                    }

                    var tc = TypeColor(entry.Type);
                    var badge = new Rect(ic.x - 22, ic.y + ICON_SIZE / 2f + 14, 44, 14);
                    EditorGUI.DrawRect(badge, tc * 0.35f);
                    GUI.Label(badge, TypeShort(entry.Type),
                        new GUIStyle(_sSmall) { normal = { textColor = tc } });
                }
                else if (isDummyBack){
                    string txt = isRoot ? "HOME" : "Back";
                    GUI.Label(new Rect(ic.x - 52, ic.y - 10, 104, 20), txt, _sLabel);
                }
                else if (isDummyQA){
                    GUI.Label(new Rect(ic.x - 52, ic.y - 10, 104, 20), "Quick Actions", _sLabel);
                }
                else{
                    GUI.Label(new Rect(ic.x - 10, ic.y - 8, 20, 16), "+",
                        new GUIStyle(_sCenter){
                            fontSize = 18,
                            normal = { textColor = new Color(1, 1, 1, 0.12f) }
                        });
                }
            }

            float firstBRad = startA * Mathf.Deg2Rad;
            DrawHandleLine(
                center + new Vector2(Mathf.Cos(firstBRad), -Mathf.Sin(firstBRad)) * INNER_RADIUS,
                center + new Vector2(Mathf.Cos(firstBRad), -Mathf.Sin(firstBRad)) * WHEEL_RADIUS,
                SEPARATOR, 1.5f);

            bool dragOnCenter = _isDragging && _dragIdx >= 0
                && (evt.mousePosition - center).magnitude < INNER_RADIUS
                && _navStack.Count > 1;

            Color centerBgCol = dragOnCenter ? new Color(0.20f, 0.70f, 0.20f, 0.85f) : CENTER_BG;

            DrawDisc(center, INNER_RADIUS, centerBgCol);
            DrawWireDisc(center, INNER_RADIUS, dragOnCenter ? Color.white * 0.8f : ACCENT * 0.45f, 1.5f);

            string cTxt;
            Color cCol;
            if (dragOnCenter){
                cTxt = "親へ";
                cCol = Color.white;
            }
            else if (_useVRcStyleUI){
                cTxt = $"{entries.Count}/{MAX_CONTROLS}";
                cCol = TEXT_SEC;
            }
            else if (_navStack.Count > 1){
                cTxt = "Back";
                cCol = ACCENT;
            }
            else{
                cTxt = $"{entries.Count}/{MAX_CONTROLS}";
                cCol = TEXT_SEC;
            }
            GUI.Label(new Rect(center.x - 30, center.y - 10, 60, 20), cTxt,
                new GUIStyle(_sCenter){
                    fontSize = 12,
                    normal = { textColor = cCol }
                });

            HandleInput(evt, center, entries);

            if (_dropBorderIdx >= 0){
                float borderAngle = (startA - _dropBorderIdx * step) * Mathf.Deg2Rad;
                var bDir = new Vector2(Mathf.Cos(borderAngle), -Mathf.Sin(borderAngle));
                DrawHandleLine(
                    center + bDir * (INNER_RADIUS - 3),
                    center + bDir * (WHEEL_RADIUS + 4),
                    new Color(1f, 0.85f, 0.2f), 5f);
            }

            if (evt.type == EventType.MouseMove) Repaint();
        }

        private void HandleInput(Event evt, Vector2 center, List<MenuEntry> entries){
            bool isRoot = _navStack.Count == 1;
            int dummyOffset = _useVRcStyleUI ? (isRoot ? 2 : 1) : 0;
            int sliceN = _useVRcStyleUI ? dummyOffset + entries.Count : MAX_CONTROLS;

            switch (evt.type){
                case EventType.MouseDown when evt.button == 0:{
                    if (_editingNameIdx >= 0){
                        bool isHoveredReal = (!_useVRcStyleUI ? _hoverIdx < entries.Count : _hoverIdx >= dummyOffset);
                        int hoveredEntryIdx = isHoveredReal ? _hoverIdx - dummyOffset : -1;

                        if (hoveredEntryIdx != _editingNameIdx){
                            ForceEndInlineRename();
                            Repaint();
                        }
                    }

                    float d = (evt.mousePosition - center).magnitude;
                    if (d < INNER_RADIUS && _navStack.Count > 1){
                        ForceEndInlineRename();
                        _navStack.RemoveAt(_navStack.Count - 1);
                        _selectedInventoryEntry = null;
                        _selectedIdx = -1;
                        _showDetail = false;
                        evt.Use();
                        Repaint();
                        return;
                    }

                    if (_hoverIdx >= 0 && _hoverIdx < sliceN){
                        bool isDummyBack = _useVRcStyleUI && _hoverIdx == 0;
                        bool isDummyQA = _useVRcStyleUI && isRoot && _hoverIdx == 1;
                        bool isRealItem = !_useVRcStyleUI ? _hoverIdx < entries.Count : _hoverIdx >= dummyOffset;
                        int entryIdx = isRealItem ? _hoverIdx - dummyOffset : -1;

                        if (evt.clickCount == 2){
                            if (isRealItem){
                                var e = entries[entryIdx];
                                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                                    NavInto(entryIdx);
                                    evt.Use();
                                    Repaint();
                                    break;
                                }
                            }
                            else if (!_useVRcStyleUI && !isRealItem){
                                AddNewSubMenu();
                                evt.Use();
                                break;
                            }
                        }

                        if (isDummyBack){
                            if (!isRoot && evt.clickCount == 1){
                                ForceEndInlineRename();
                                _navStack.RemoveAt(_navStack.Count - 1);
                                _selectedInventoryEntry = null;
                                _selectedIdx = -1;
                                _showDetail = false;
                                evt.Use();
                                Repaint();
                                return;
                            }
                            evt.Use();
                            return;
                        }

                        if (isDummyQA){
                            evt.Use();
                            return;
                        }

                        if (isRealItem && evt.clickCount == 1){
                            var e = entries[entryIdx];
                            float step = 360f / sliceN;
                            float startA = _useVRcStyleUI ? 90f + (180f / sliceN) : 90f;
                            float a0 = startA - _hoverIdx * step;
                            float mid = a0 - step / 2f;
                            float midR = mid * Mathf.Deg2Rad;
                            float itemR = (INNER_RADIUS + WHEEL_RADIUS) / 2f;
                            var ic = center + new Vector2(Mathf.Cos(midR), -Mathf.Sin(midR)) * itemR;

                            var content = new GUIContent(e.Name ?? "(no name)");
                            var sz = _sLabel.CalcSize(content);
                            float w = Mathf.Min(sz.x, 104f);
                            float h = _sLabel.CalcHeight(content, 104f);
                            float centerY = ic.y + ICON_SIZE / 2f + 4f; 
                            var textRect = new Rect(ic.x - w / 2f, centerY - h / 2f, w, h);

                            if (_selectedIdx == entryIdx && _selectedInventoryEntry == null && _editingNameIdx != entryIdx && !e.IsBuildTime){
                                if (textRect.Contains(evt.mousePosition) && (EditorApplication.timeSinceStartup - _lastRingSelectTime > 0.6f)){
                                    _readyToRenameRingIdx = entryIdx;
                                    _ringRenameDownTime = EditorApplication.timeSinceStartup;
                                }
                            }

                            if (_selectedIdx != entryIdx || _selectedInventoryEntry != null){
                                _lastRingSelectTime = EditorApplication.timeSinceStartup;
                            }

                            _dragIdx = entryIdx;
                            _dragStart = evt.mousePosition;
                            _selectedInventoryEntry = null;
                            _selectedIdx = entryIdx;
                            _showDetail = true;
                            evt.Use();
                            Repaint();
                        }
                    }
                    break;
                }

                case EventType.MouseDrag when evt.button == 0 && _dragIdx >= 0:
                    if (!_isDragging && Vector2.Distance(evt.mousePosition, _dragStart) > 8f)
                        _isDragging = true;
                    evt.Use();
                    Repaint();
                    break;

                case EventType.MouseUp when evt.button == 0:
                    if (_readyToRenameRingIdx >= 0 && !_isDragging){
                        if (EditorApplication.timeSinceStartup - _ringRenameDownTime < 0.4f){
                            if (_hoverIdx >= 0 && _hoverIdx < sliceN){
                                bool hReal = !_useVRcStyleUI ? _hoverIdx < entries.Count : _hoverIdx >= dummyOffset;
                                int hEntryIdx = hReal ? _hoverIdx - dummyOffset : -1;
                                if (hEntryIdx == _readyToRenameRingIdx){
                                    StartInlineRename(_readyToRenameRingIdx, entries[_readyToRenameRingIdx]);
                                    evt.Use();
                                    Repaint();
                                }
                            }
                        }
                    }
                    _readyToRenameRingIdx = -1;

                    if (_dragIdx < 0 && !_dragFromInventory) break;

                    if (_isDragging && _dragFromInventory && _dragInventoryEntry != null){
                        var moving = _dragInventoryEntry;
                        float dropDist = (evt.mousePosition - center).magnitude;

                        if (dropDist < INNER_RADIUS && _navStack.Count > 1){
                            var targetNode = _navStack[_navStack.Count - 2].Node;
                            Undo.RecordObject(this, "Drop from Inventory");
                            _dragInventoryList.Remove(moving);
                            InsertItemWithOverflow(targetNode, targetNode.Entries.Count, moving);
                            _selectedInventoryEntry = null;
                            _selectedIdx = -1;
                            _hasUnsavedChanges = true;
                        }
                        else if (_dropBorderIdx >= 0){
                            int insertIdx = _dropBorderIdx - dummyOffset;
                            Undo.RecordObject(this, "Drop from Inventory");
                            _dragInventoryList.Remove(moving);
                            InsertItemWithOverflow(CurNode(), insertIdx, moving);
                            _selectedInventoryEntry = null;
                            _selectedIdx = -1;
                            _hasUnsavedChanges = true;
                        }
                        else if (_hoverIdx >= 0 && _hoverIdx < sliceN){
                            bool hReal = !_useVRcStyleUI ? _hoverIdx < entries.Count : _hoverIdx >= dummyOffset;
                            int hEntryIdx = hReal ? _hoverIdx - dummyOffset : -1;

                            if (_useVRcStyleUI && _hoverIdx == 0 && _navStack.Count > 1){
                                var targetNode = _navStack[_navStack.Count - 2].Node;
                                Undo.RecordObject(this, "Drop from Inventory");
                                _dragInventoryList.Remove(moving);
                                InsertItemWithOverflow(targetNode, targetNode.Entries.Count, moving);
                                _selectedInventoryEntry = null;
                                _selectedIdx = -1;
                                _hasUnsavedChanges = true;
                            }
                            else if (hReal && entries[hEntryIdx].Type == VRCExpressionsMenu.Control.ControlType.SubMenu
                                && entries[hEntryIdx].SubMenu != null){
                                Undo.RecordObject(this, "Drop from Inventory");
                                _dragInventoryList.Remove(moving);
                                InsertItemWithOverflow(entries[hEntryIdx].SubMenu, entries[hEntryIdx].SubMenu.Entries.Count, moving);
                                _selectedInventoryEntry = null;
                                _selectedIdx = -1;
                                _hasUnsavedChanges = true;
                            }
                            else if (!_useVRcStyleUI && !hReal){
                                Undo.RecordObject(this, "Drop from Inventory");
                                _dragInventoryList.Remove(moving);
                                InsertItemWithOverflow(CurNode(), entries.Count, moving);
                                _selectedInventoryEntry = null;
                                _selectedIdx = -1;
                                _hasUnsavedChanges = true;
                            }
                        }
                    }

                    if (_isDragging && !_dragFromInventory && _dragIdx >= 0 && _dragIdx < entries.Count){
                        float dropDist = (evt.mousePosition - center).magnitude;

                        if (dropDist < INNER_RADIUS && _navStack.Count > 1){
                            MoveEntryToParent(_dragIdx);
                        }
                        else if (_dropBorderIdx >= 0){
                            int insertIdx = _dropBorderIdx - dummyOffset;
                            if (insertIdx < 0) insertIdx = 0;
                            if (insertIdx > entries.Count) insertIdx = entries.Count;

                            if (insertIdx > _dragIdx) insertIdx--;

                            if (insertIdx != _dragIdx && insertIdx >= 0){
                                Undo.RecordObject(this, "Reorder Menu Item");
                                var moving = entries[_dragIdx];
                                entries.RemoveAt(_dragIdx);
                                entries.Insert(insertIdx, moving);
                                _selectedIdx = insertIdx;
                                _hasUnsavedChanges = true;
                                _needsOverflowReeval = true;
                            }
                        }
                        else if (_hoverIdx >= 0 && _hoverIdx < sliceN){
                            bool hReal = !_useVRcStyleUI ? _hoverIdx < entries.Count : _hoverIdx >= dummyOffset;
                            int hEntryIdx = hReal ? _hoverIdx - dummyOffset : -1;

                            if (_useVRcStyleUI && _hoverIdx == 0 && _navStack.Count > 1){
                                MoveEntryToParent(_dragIdx);
                            }
                            else if (hReal && hEntryIdx != _dragIdx
                                && entries[hEntryIdx].Type == VRCExpressionsMenu.Control.ControlType.SubMenu
                                && entries[hEntryIdx].SubMenu != null){
                                MoveEntryIntoSubmenu(_dragIdx, hEntryIdx);
                            }
                            else if (!_useVRcStyleUI && !hReal){
                                Undo.RecordObject(this, "Move Menu Item");
                                var moving = entries[_dragIdx];
                                entries.RemoveAt(_dragIdx);
                                InsertItemWithOverflow(CurNode(), entries.Count, moving);
                                _selectedInventoryEntry = null;
                                _selectedIdx = -1;
                                _hasUnsavedChanges = true;
                            }
                        }
                    }
                    _isDragging = false;
                    _dragFromInventory = false;
                    _dragIdx = -1;
                    _dropBorderIdx = -1;
                    _dropCrumbIdx = -1;
                    evt.Use();
                    Repaint();
                    break;

                case EventType.MouseDown when evt.button == 1:
                    if (_hoverIdx >= 0 && _hoverIdx < sliceN){
                        bool hReal = !_useVRcStyleUI ? _hoverIdx < entries.Count : _hoverIdx >= dummyOffset;
                        int hEntryIdx = hReal ? _hoverIdx - dummyOffset : -1;

                        if (hReal){
                            ShowContextMenu(hEntryIdx, entries);
                            evt.Use();
                        }
                        else if (!_useVRcStyleUI && !hReal){
                            ShowContextMenu(-1, entries);
                            evt.Use();
                        }
                    }
                    break;
            }
        }

        private void DrawToolbar(){
            EditorGUILayout.Space(6);
            
            float viewWidth = EditorGUIUtility.currentViewWidth - (_showInventory ? 220f : 30f) - 20f;
            float centerPaddingMoveMode = Mathf.Max(0, (viewWidth - 300f) / 2f);
            float centerPaddingNormal = Mathf.Max(0, (viewWidth - 425f) / 2f);
            float centerPaddingSave = Mathf.Max(0, (viewWidth - 260f) / 2f);

            if (_isMoveMode && _cutEntry != null){
                var moveBar = EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(
                    new Rect(moveBar.x + 16, moveBar.y, moveBar.width - 32, 28),
                    new Color(0.45f, 0.30f, 0.10f, 0.5f));
                
                GUILayout.Space(Mathf.Max(0, (viewWidth - 400f) / 2f));
                EditorGUILayout.LabelField(
                    $"移動中: 「{_cutEntry.Name}」 → 目的の階層に移動して貼り付け",
                    new GUIStyle(EditorStyles.label){
                        normal = { textColor = ACCENT_SUB },
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                    });
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(centerPaddingMoveMode);
                var curEntries = CurEntries();
                GUI.enabled = curEntries.Count < MAX_CONTROLS;
                if (GUILayout.Button("ここに移動", _sBtn, GUILayout.Width(140))){
                    PasteCutEntry();
                }
                GUI.enabled = true;
                if (GUILayout.Button("キャンセル", _sBtn, GUILayout.Width(140))){
                    if (_cutSourceNode != null && _cutEntry != null)
                        _cutSourceNode.Entries.Add(_cutEntry);
                    CancelMoveMode();
                    Repaint();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else{
                EditorGUILayout.BeginHorizontal();
                centerPaddingNormal = Mathf.Max(0, (viewWidth - 300f) / 2f);
                GUILayout.Space(centerPaddingNormal);

                var entries = CurEntries();
                GUI.enabled = entries.Count < MAX_CONTROLS;
                if (GUILayout.Button("フォルダ作成", _sBtn, GUILayout.Width(200)))
                    AddNewSubMenu();
                GUI.enabled = true;
                
                if (GUILayout.Button("リロード", _sBtn, GUILayout.Width(100)))
                    RebuildMenu();

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(centerPaddingSave);

            var applyStyle = new GUIStyle(_sBtn){
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = _hasUnsavedChanges
                ? new Color(0.3f, 0.8f, 0.4f)
                : new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button("保存", applyStyle, GUILayout.Width(260))){
                SaveLayout();
            }
            GUI.backgroundColor = prevBg;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailPanel(){
            var entries = CurEntries();
            MenuEntry e = null;
            bool isFromInventory = false;

            if (_selectedInventoryEntry != null){
                e = _selectedInventoryEntry;
                isFromInventory = true;
            }
            else if (_selectedIdx >= 0 && _selectedIdx < entries.Count){
                e = entries[_selectedIdx];
            }

            if (e == null) return;

            EditorGUILayout.Space(8);
            var pr = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(pr.x + 16, pr.y, pr.width - 32, 1), SEPARATOR);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("選択中のアイテム", _sHeader);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (!isFromInventory){
                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    if (GUILayout.Button("サブメニューを開く", _sBtn, GUILayout.Width(160))){
                        NavInto(_selectedIdx);
                        GUIUtility.ExitGUI();
                    }
                }
                if (!_isMoveMode){
                    if (GUILayout.Button("別の階層に移動", _sBtn, GUILayout.Width(160))){
                        StartMoveMode(_selectedIdx);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(10, 1), SEPARATOR);
            EditorGUILayout.Space(4);

            bool isProRestricted = !isFromInventory && MenuManagerAuth.RequestStatusText(_navStack.Count - 1) != null;
            if (isProRestricted){
                EditorGUILayout.HelpBox("🛡️ 第3階層以上の編集は Pro 版限定機能です。", MessageType.Warning);
                if (GUILayout.Button("認証ウィンドウを開く", GUILayout.Height(24))){
                    MenuManagerAuthWindow.ShowWindow();
                }
                EditorGUILayout.Space(8);
            }

            EditorGUI.BeginDisabledGroup(isProRestricted);

            GUI.SetNextControlName("DetailPanelNameField");
            bool isFocused = GUI.GetNameOfFocusedControl() == "DetailPanelNameField";

            if (isFocused){
                if (_detailEditNameEntry != e){
                    if (_detailEditNameEntry == null){
                        _detailEditNameEntry = e;
                        _detailEditNameStr = e.Name ?? "";
                    }
                    else{
                        _detailEditNameEntry = null;
                        GUI.FocusControl(null);
                        EditorGUILayout.TextField("名前", e.Name ?? "");
                        goto SkipEdit;
                    }
                }

                if (Event.current.type == EventType.KeyDown){
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter){
                        string finalName = string.IsNullOrWhiteSpace(_detailEditNameStr) ? GetOriginalName(e) : _detailEditNameStr;
                        if (finalName != e.Name){
                            Undo.RecordObject(this, "Rename Menu Item");
                            e.Name = finalName;
                            if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                                e.SubMenu.Name = finalName;
                            }
                            _hasUnsavedChanges = true;
                        }
                        _detailEditNameEntry = null;
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.Escape){
                        _detailEditNameEntry = null;
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                }

                _detailEditNameStr = EditorGUILayout.TextField("名前", _detailEditNameStr);
            SkipEdit:;
            }
            else{
                if (_detailEditNameEntry != null && _detailEditNameEntry == e){
                    string finalName = string.IsNullOrWhiteSpace(_detailEditNameStr) ? GetOriginalName(e) : _detailEditNameStr;
                    if (finalName != e.Name){
                        Undo.RecordObject(this, "Rename Menu Item");
                        e.Name = finalName;
                        if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                            e.SubMenu.Name = finalName;
                        }
                        _hasUnsavedChanges = true;
                    }
                    _detailEditNameEntry = null;
                }
                
                EditorGUILayout.TextField("名前", e.Name ?? "");
            }
            EditorGUILayout.LabelField("タイプ", e.Type.ToString());
            EditorGUILayout.LabelField("パラメータ",
                string.IsNullOrEmpty(e.Parameter) ? "(none)" : e.Parameter);
            EditorGUILayout.LabelField("値", e.Value.ToString("F2"));

            EditorGUI.BeginChangeCheck();
            var newIcon = (Texture2D)EditorGUILayout.ObjectField("アイコン", e.Icon, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (EditorGUI.EndChangeCheck()){
                Undo.RecordObject(this, "Set Icon");
                e.Icon = newIcon;
                _hasUnsavedChanges = true;
                Repaint();
            }

            if (e.Icon != null){
                EditorGUILayout.Space(4);
                var ir = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));
                GUI.DrawTexture(ir, e.Icon, ScaleMode.ScaleToFit);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);
            UnityEngine.Object srcMA = null;
            if (e.SourceInstaller != null) srcMA = e.SourceInstaller.gameObject;
            else if (e.SourceMenuItem != null) srcMA = e.SourceMenuItem.gameObject;

            if (srcMA != null){
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("MA ソース:", GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(srcMA, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("選択", GUILayout.Width(40))){
                    Selection.activeObject = srcMA;
                    EditorGUIUtility.PingObject(srcMA);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (e.SourceAsset != null){
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("アセット:", GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(e.SourceAsset, typeof(VRCExpressionsMenu), true);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("選択", GUILayout.Width(40))){
                    Selection.activeObject = e.SourceAsset;
                    EditorGUIUtility.PingObject(e.SourceAsset);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private string GetOriginalName(MenuEntry e){
            if (e.SourceMenuItem != null) return e.SourceMenuItem.gameObject.name;
            if (e.SourceInstaller != null) return e.SourceInstaller.gameObject.name;
            if (e.SourceAsset != null && e.SourceIndex >= 0 && e.SourceIndex < e.SourceAsset.controls.Count){
                return e.SourceAsset.controls[e.SourceIndex].name;
            }
            return "New Folder";
        }

        private List<MenuEntry> CurEntries(){
            if (_navStack == null || _navStack.Count == 0) return new List<MenuEntry>();
            var last = _navStack[_navStack.Count - 1];
            if (last == null || last.Node == null) return new List<MenuEntry>();
            return last.Node.Entries;
        }

        private MenuNode CurNode(){
            if (_navStack == null || _navStack.Count == 0) return null;
            var last = _navStack[_navStack.Count - 1];
            return last?.Node;
        }

        private void ApplyInlineRename(MenuEntry e){
            string finalName = string.IsNullOrWhiteSpace(_editingNameStr) ? GetOriginalName(e) : _editingNameStr;
            if (e.Name == finalName) return;

            Undo.RecordObject(this, "Rename Menu Item (Inline)");

            e.Name = finalName;
            if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                e.SubMenu.Name = finalName;
            }

            _hasUnsavedChanges = true;
        }

        private void StartInlineRename(int idx, MenuEntry e, bool isNewFolder = false){
            if (!CheckProLimit(_navStack.Count, "名前の編集")) return;
            _editingNameIdx = idx;
            _editingNameStr = e.Name ?? "";
            _focusNameField = true;
            _editingIsNewFolder = isNewFolder;
        }

        private void ForceEndInlineRename(bool cancel = false){
            if (_editingNameIdx >= 0){
                var entries = CurEntries();
                if (cancel){
                    if (_editingIsNewFolder && _editingNameIdx < entries.Count){
                        entries.RemoveAt(_editingNameIdx);
                        _hasUnsavedChanges = true;
                        _needsOverflowReeval = true;
                    }
                }
                else{
                    if (_editingNameIdx < entries.Count){
                        ApplyInlineRename(entries[_editingNameIdx]);
                    }
                }
                
                _editingNameIdx = -1;
                _editingIsNewFolder = false;
                GUI.FocusControl(null);
            }
        }

        private void NavToLevel(int lv){
            ForceEndInlineRename();
            if (lv < 0 || lv >= _navStack.Count) return;
            _navStack.RemoveRange(lv + 1, _navStack.Count - lv - 1);
            _selectedInventoryEntry = null;
            _selectedIdx = -1;
            _showDetail = false;
            Repaint();
        }

        private void NavInto(int idx){
            if (!MenuManagerAuth.TryNavInto(_navStack.Count)) return;

            ForceEndInlineRename();
            var entries = CurEntries();
            if (idx < 0 || idx >= entries.Count){
                Debug.LogWarning($"[MenuManager] NavInto: idx={idx} 範囲外 (entries.Count={entries.Count})");
                return;
            }
            var e = entries[idx];
            if (e.SubMenu == null){
                Debug.LogWarning($"[MenuManager] NavInto: '{e.Name}' の SubMenu が null です (Type={e.Type})");
                return;
            }
            _navStack.Add(new BreadcrumbEntry{
                Node = e.SubMenu,
                Name = e.Name ?? "(SubMenu)"
            });
            _selectedInventoryEntry = null;
            _selectedIdx = -1;
            _showDetail = false;
            Repaint();
        }

        private void ReorderEntry(int from, int to){
            var entries = CurEntries();
            if (from < 0 || from >= entries.Count || to < 0 || to >= entries.Count) return;

            Undo.RecordObject(this, "Reorder Menu Item");

            var e = entries[from];

            var tmp = entries[from];
            entries.RemoveAt(from);
            entries.Insert(to, tmp);
            _selectedInventoryEntry = null;
            _selectedIdx = to;
            _hasUnsavedChanges = true;
            _needsOverflowReeval = true;
            Repaint();
        }

        private void MoveEntryIntoSubmenu(int entryIdx, int subMenuIdx){
            var entries = CurEntries();
            if (entryIdx < 0 || entryIdx >= entries.Count) return;
            if (subMenuIdx < 0 || subMenuIdx >= entries.Count) return;

            var target = entries[subMenuIdx];
            if (target.SubMenu == null) return;

            Undo.RecordObject(this, "Move into SubMenu");

            var moving = entries[entryIdx];
            entries.RemoveAt(entryIdx);
            InsertItemWithOverflow(target.SubMenu, target.SubMenu.Entries.Count, moving);

            _selectedInventoryEntry = null;
            _selectedIdx = -1;
            _showDetail = false;
            _hasUnsavedChanges = true;
            Repaint();
        }

        private void MoveEntryToParent(int entryIdx){
            var entries = CurEntries();
            if (entryIdx < 0 || entryIdx >= entries.Count) return;
            if (_navStack.Count < 2) return;

            var parentNode = _navStack[_navStack.Count - 2].Node;

            Undo.RecordObject(this, "Move to Parent");

            var moving = entries[entryIdx];
            entries.RemoveAt(entryIdx);
            InsertItemWithOverflow(parentNode, parentNode.Entries.Count, moving);

            _selectedInventoryEntry = null;
            _selectedIdx = -1;
            _showDetail = false;
            _hasUnsavedChanges = true;
            Repaint();
        }

        private void InsertItemWithOverflow(MenuNode parentNode, int insertIdx, MenuEntry itemToAdd){
            insertIdx = Mathf.Clamp(insertIdx, 0, parentNode.Entries.Count);
            parentNode.Entries.Insert(insertIdx, itemToAdd);
            _needsOverflowReeval = true;
        }

        private void AddNewSubMenu(){
            if (CurEntries().Count >= MAX_CONTROLS) return;

            Undo.RecordObject(this, "Add Virtual SubMenu");

            var virtualSub = new MenuNode { Name = "New Folder" };
            var virtualEntry = new MenuEntry{
                Name = "New Folder",
                Icon = GetIcon("folder.png"),
                Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                SubMenu = virtualSub,
                IsDynamic = false,
                IsCustomFolder = true,
                UniqueId = "SubMenu:New Folder:::0.00:" + Guid.NewGuid().ToString("N")
            };

            if (!MenuManagerAuth.ValidateLevel(_navStack.Count)) return;
            CurEntries().Add(virtualEntry);

            _hasUnsavedChanges = true;
            _needsOverflowReeval = true;

            int newIdx = CurEntries().Count - 1;
            StartInlineRename(newIdx, CurEntries()[newIdx], true);
            Repaint();
        }

        private void DeleteEntry(int idx){
            var entries = CurEntries();
            if (idx < 0 || idx >= entries.Count) return;

            Undo.RecordObject(this, "Move to Inventory");

            var e = entries[idx];
            entries.RemoveAt(idx);
            
            if (e.IsCustomFolder){
                if (e.SubMenu != null && e.SubMenu.Entries.Count > 0){
                    var parentNode = _navStack[_navStack.Count - 1].Node;
                    var childrenToMove = new List<MenuEntry>(e.SubMenu.Entries);
                    e.SubMenu.Entries.Clear();
                    foreach (var child in childrenToMove){
                        InsertItemWithOverflow(parentNode, idx, child);
                        idx++;
                    }
                }
            }
            
            if (!e.IsBuildTime) _inventory.Add(e);

            _selectedIdx = -1;
            _showDetail = false;
            _hasUnsavedChanges = true;
            _needsOverflowReeval = true;
            Repaint();
        }

        private void DeleteFolderCompletely(int idx){
            var entries = CurEntries();
            if (idx < 0 || idx >= entries.Count) return;

            Undo.RecordObject(this, "Delete Folder Completely");

            var e = entries[idx];
            entries.RemoveAt(idx);
            
            if (e.IsCustomFolder){
                if (e.SubMenu != null && e.SubMenu.Entries.Count > 0){
                    var parentNode = _navStack[_navStack.Count - 1].Node;
                    var childrenToMove = new List<MenuEntry>(e.SubMenu.Entries);
                    e.SubMenu.Entries.Clear();
                    foreach (var child in childrenToMove){
                        InsertItemWithOverflow(parentNode, idx, child);
                        idx++;
                    }
                }
            }
            
            _selectedIdx = -1;
            _showDetail = false;
            _hasUnsavedChanges = true;
            _needsOverflowReeval = true;
            Repaint();
        }

        private void ShowContextMenu(int idx, List<MenuEntry> entries){
            var menu = new GenericMenu();
            bool has = idx >= 0 && idx < entries.Count;

            if (has){
                var e = entries[idx];
                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    menu.AddItem(new GUIContent("サブメニューを開く"), false, () => NavInto(idx));
                    menu.AddSeparator("");
                }

                menu.AddItem(idx > 0
                        ? new GUIContent("上に移動") : new GUIContent("上に移動"),
                    false,
                    idx > 0 ? (GenericMenu.MenuFunction)(() => ReorderEntry(idx, idx - 1)) : null);
                if (idx <= 0) menu.AddDisabledItem(new GUIContent("上に移動"));

                if (idx < entries.Count - 1)
                    menu.AddItem(new GUIContent("下に移動"), false,
                        () => ReorderEntry(idx, idx + 1));
                else
                    menu.AddDisabledItem(new GUIContent("下に移動"));

                menu.AddSeparator("");
                if (e.IsBuildTime){
                    menu.AddDisabledItem(new GUIContent("名前を編集"));
                }
                else{
                    menu.AddItem(new GUIContent("名前を編集"), false, () => StartInlineRename(idx, e));
                }

                menu.AddSeparator("");
                if (!e.IsBuildTime){
                    UnityEngine.Object src = e.SourceMenuItem != null
                        ? (UnityEngine.Object)e.SourceMenuItem.gameObject
                        : e.SourceAsset;
                    if (src != null)
                        menu.AddItem(new GUIContent("ソースを選択"), false, () =>{
                            Selection.activeObject = src;
                            EditorGUIUtility.PingObject(src);
                        });

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("インベントリへ移動"), false, () => DeleteEntry(idx));

                    if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (e.SourceInstaller != null || e.SourceAsset != null || e.SourceMenuItem != null)){
                        menu.AddItem(new GUIContent("リセット"), false, () => ResetSubmenu(e, entries));
                    }
                }

                if (e.IsCustomFolder || e.IsBuildTime){
                    menu.AddItem(new GUIContent("削除"), false, () => DeleteFolderCompletely(idx));
                }
                else{
                    menu.AddDisabledItem(new GUIContent("削除 (作成したフォルダのみ可)"));
                }
            }
            else if (entries.Count < MAX_CONTROLS){
                menu.AddItem(new GUIContent("フォルダ追加"), false, () => AddNewSubMenu());
            }
            if (has && !_isMoveMode){
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("別の階層に移動"), false, () => StartMoveMode(idx));
            }

            menu.ShowAsContext();
        }

        private void ResetSubmenu(MenuEntry target, IList<MenuEntry> parentList){
            Undo.RecordObject(this, "Reset Submenu");

            var freshNode = new MenuNode();
            var visited = new HashSet<VRCExpressionsMenu>();
            var m2i = new Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>>();
            var ri = new List<ModularAvatarMenuInstaller>();

            if (_avatar != null){
                foreach (var installer in _avatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true)){
                    if (installer.menuToAppend != null){
                        if (!m2i.ContainsKey(installer.menuToAppend)) m2i[installer.menuToAppend] = new List<ModularAvatarMenuInstaller>();
                        m2i[installer.menuToAppend].Add(installer);
                    }
                    else if (installer.GetComponent<ModularAvatarMenuItem>() == null && installer.GetComponent<ModularAvatarMenuGroup>() == null){
                        ri.Add(installer);
                    }
                }
            }

            if (target.SourceInstaller != null){
                AddInstallerEntries(freshNode, target.SourceInstaller, visited, m2i, ri);
            }
            else if (target.SourceAsset != null && target.SourceAsset is VRCExpressionsMenu menu){
                freshNode = BuildMenuNode(menu, visited, m2i, ri, false);
            }
            else if (target.SourceMenuItem != null){
                freshNode.Entries.Add(ConvertMAMenuItem(target.SourceMenuItem, visited, m2i, ri));
            }

            var si = new HashSet<ModularAvatarMenuInstaller>();
            var smi = new HashSet<ModularAvatarMenuItem>();
            var sa = new HashSet<string>();

            if (target.SourceInstaller != null) si.Add(target.SourceInstaller);
            if (target.SourceMenuItem != null) smi.Add(target.SourceMenuItem);
            if (target.SourceAsset != null) sa.Add($"{target.SourceAsset.GetInstanceID()}:{target.SourceIndex}");

            GatherSourceIdentifiers(freshNode, si, smi, sa);

            RemoveEntriesBySourceId(_rootNode, si, smi, sa, target);
            RemoveEntriesBySourceId_Inventory(_inventory, si, smi, sa, target);

            if (freshNode.Entries.Count > 0){
                var newEntry = freshNode.Entries[0];
                newEntry.Name = target.Name; 
                if (parentList != null){
                    int idx = parentList.IndexOf(target);
                    if (idx >= 0){
                        parentList[idx] = newEntry;
                    }
                }
            }
            else {
                if (parentList != null){
                    int idx = parentList.IndexOf(target);
                    if (idx >= 0){
                        parentList[idx] = new MenuEntry { 
                            Name = target.Name, 
                            Icon = GetIcon("folder.png"),
                            Type = VRCExpressionsMenu.Control.ControlType.SubMenu, 
                            SubMenu = new MenuNode { Name = target.Name },
                            IsCustomFolder = true
                        };
                    }
                }
            }

            _selectedIdx = -1;
            _selectedInventoryEntry = null;
            _hasUnsavedChanges = true;
            _needsOverflowReeval = true;
            Repaint();
        }

        private void GatherSourceIdentifiers(MenuNode node, HashSet<ModularAvatarMenuInstaller> si, HashSet<ModularAvatarMenuItem> smi, HashSet<string> sa){
            if (node == null || node.Entries == null) return;
            foreach (var e in node.Entries){
                if (e.SourceInstaller != null) si.Add(e.SourceInstaller);
                if (e.SourceMenuItem != null) smi.Add(e.SourceMenuItem);
                if (e.SourceAsset != null) sa.Add($"{e.SourceAsset.GetInstanceID()}:{e.SourceIndex}");

                if (e.SubMenu != null) GatherSourceIdentifiers(e.SubMenu, si, smi, sa);
            }
        }

        private bool MatchesSourceId(MenuEntry e, HashSet<ModularAvatarMenuInstaller> si, HashSet<ModularAvatarMenuItem> smi, HashSet<string> sa){
            if (e.SourceInstaller != null && si.Contains(e.SourceInstaller)) return true;
            if (e.SourceMenuItem != null && smi.Contains(e.SourceMenuItem)) return true;
            if (e.SourceAsset != null && sa.Contains($"{e.SourceAsset.GetInstanceID()}:{e.SourceIndex}")) return true;
            return false;
        }

        private void RemoveEntriesBySourceId(MenuNode node, HashSet<ModularAvatarMenuInstaller> si, HashSet<ModularAvatarMenuItem> smi, HashSet<string> sa, MenuEntry exclude){
            if (node == null || node.Entries == null) return;
            for (int i = node.Entries.Count - 1; i >= 0; i--){
                var e = node.Entries[i];
                if (e == exclude) continue;
                if (MatchesSourceId(e, si, smi, sa)){
                    node.Entries.RemoveAt(i);
                }
                else if (e.SubMenu != null){
                    RemoveEntriesBySourceId(e.SubMenu, si, smi, sa, exclude);
                }
            }
        }

        private void RemoveEntriesBySourceId_Inventory(List<MenuEntry> entries, HashSet<ModularAvatarMenuInstaller> si, HashSet<ModularAvatarMenuItem> smi, HashSet<string> sa, MenuEntry exclude){
            if (entries == null) return;
            for (int i = entries.Count - 1; i >= 0; i--){
                var e = entries[i];
                if (e == exclude) continue;
                if (MatchesSourceId(e, si, smi, sa)){
                    entries.RemoveAt(i);
                }
                else if (e.SubMenu != null){
                    RemoveEntriesBySourceId(e.SubMenu, si, smi, sa, exclude);
                }
            }
        }

        private VRCAvatarDescriptor GrabAvatarFromDrag(){
            foreach (var o in DragAndDrop.objectReferences)
                if (o is GameObject go){
                    var d = go.GetComponent<VRCAvatarDescriptor>();
                    if (d != null) return d;
                }
            return null;
        }

        private bool InRing(Vector2 p, Vector2 c){
            float d = (p - c).magnitude;
            return d >= INNER_RADIUS && d <= WHEEL_RADIUS;
        }

        private Texture2D GetIcon(string iconName){
            string assetName = System.IO.Path.GetFileNameWithoutExtension(iconName);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Lyra/EditorTool/MenuManager/Icon/{iconName}");
            if (tex == null){
                var guids = AssetDatabase.FindAssets($"{assetName} t:Texture2D");
                foreach (var g in guids){
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.EndsWith(iconName, StringComparison.OrdinalIgnoreCase)){
                        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                        if (p.Contains("Lyra")) break;
                    }
                }
            }
            return tex;
        }

        private int CalcSlice(Vector2 p, Vector2 c, int n, float startA){
            var v = p - c;
            float a = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg;
            if (a < 0) a += 360f;
            float off = ((startA - a) % 360f + 360f) % 360f;
            return Mathf.FloorToInt(off / (360f / n)) % n;
        }

        private int CalcNearestBorder(Vector2 p, Vector2 c, int sliceCount, float startAngle, float step, int maxEntries){
            var v = p - c;
            float a = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg;
            if (a < 0) a += 360f;
            float off = ((startAngle - a) % 360f + 360f) % 360f;
            float withinSlice = off % step;
            int sliceIdx = Mathf.FloorToInt(off / step) % sliceCount;

            float threshold = step * 0.18f;
            int border = -1;
            if (withinSlice < threshold)
                border = sliceIdx;
            else if (withinSlice > step - threshold)
                border = (sliceIdx + 1) % sliceCount;

            if (border > maxEntries) border = -1;

            return border;
        }

        private Color TypeColor(VRCExpressionsMenu.Control.ControlType t){
            switch (t){
                case VRCExpressionsMenu.Control.ControlType.Toggle: return C_TOGGLE;
                case VRCExpressionsMenu.Control.ControlType.Button: return C_BUTTON;
                case VRCExpressionsMenu.Control.ControlType.SubMenu: return ACCENT_SUB;
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet: return C_RADIAL;
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: return C_2AXIS;
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: return C_4AXIS;
                default: return TEXT_SEC;
            }
        }

        private string TypeShort(VRCExpressionsMenu.Control.ControlType t){
            switch (t){
                case VRCExpressionsMenu.Control.ControlType.Toggle: return "Toggle";
                case VRCExpressionsMenu.Control.ControlType.Button: return "Button";
                case VRCExpressionsMenu.Control.ControlType.SubMenu: return "Sub ▶";
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet: return "Radial";
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: return "2Axis";
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: return "4Axis";
                default: return "?";
            }
        }

        private void DrawTypeIcon(Vector2 c, MenuEntry e, float s = 26){
            var tc = TypeColor(e.Type);
            var r = new Rect(c.x - s / 2, c.y - s / 2, s, s);
            EditorGUI.DrawRect(r, tc * 0.3f);
            DrawBorder(r, tc * 0.6f, 1f);

            string ico;
            switch (e.Type){
                case VRCExpressionsMenu.Control.ControlType.Toggle: ico = "◉"; break;
                case VRCExpressionsMenu.Control.ControlType.Button: ico = "◎"; break;
                case VRCExpressionsMenu.Control.ControlType.SubMenu: ico = "▶"; break;
                case VRCExpressionsMenu.Control.ControlType.RadialPuppet: ico = "◐"; break;
                case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet: ico = "✛"; break;
                case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet: ico = "✦"; break;
                default: ico = "?"; break;
            }
            GUI.Label(r, ico, new GUIStyle(_sCenter){
                fontSize = (int)(14 * (s / 26f)), normal = { textColor = tc }
            });
        }

        private void DrawDisc(Vector2 c, float r, Color col){
            Handles.BeginGUI();
            Handles.color = col;
            Handles.DrawSolidDisc(c, Vector3.forward, r);
            Handles.EndGUI();
        }

        private void DrawWireDisc(Vector2 c, float r, Color col, float t){
            Handles.BeginGUI();
            Handles.color = col;
            Handles.DrawWireDisc(c, Vector3.forward, r, t);
            Handles.EndGUI();
        }

        private void DrawSlice(Vector2 c, float ri, float ro, float a0, float a1, Color col){
            if (Event.current.type != EventType.Repaint) return;
            Handles.BeginGUI();
            Handles.color = col;
            Vector3 from = new Vector3(Mathf.Cos(a0 * Mathf.Deg2Rad), -Mathf.Sin(a0 * Mathf.Deg2Rad), 0);
            Handles.DrawSolidArc(c, Vector3.forward, from, (a0 - a1), ro);
            Handles.EndGUI();
        }

        private void DrawHandleLine(Vector2 a, Vector2 b, Color col, float t){
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            var prevColor = GUI.color;
            var prevMatrix = GUI.matrix;
            
            GUI.color = col;
            Vector2 dir = b - a;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float length = dir.magnitude;
            
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - t / 2f, length, t), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            
            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }

        private void DrawBorder(Rect r, Color c, float t){
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }
        private void StartMoveMode(int idx){
            var entries = CurEntries();
            if (idx < 0 || idx >= entries.Count) return;

            Undo.RecordObject(this, "Cut Menu Item");

            _cutEntry = entries[idx];
            _cutSourceNode = _navStack[_navStack.Count - 1].Node;
            _cutSourceNode.Entries.RemoveAt(idx);

            _isMoveMode = true;
            _selectedIdx = -1;
            _showDetail = false;
            _hasUnsavedChanges = true;
            Repaint();
        }

        private void PasteCutEntry(){
            if (_cutEntry == null) return;

            var curNode = _navStack[_navStack.Count - 1].Node;

            Undo.RecordObject(this, "Paste Menu Item");

            InsertItemWithOverflow(curNode, curNode.Entries.Count, _cutEntry);
            CancelMoveMode();
            _hasUnsavedChanges = true;
            Repaint();
        }

        private void SaveLayout(bool silent = false){
            if (_avatar == null || _rootNode == null){
                if (!silent) EditorUtility.DisplayDialog("Menu Manager",
                    "アバターまたはメニューが読み込まれていません。", "OK");
                return;
            }

            if (_isMoveMode){
                if (!silent) EditorUtility.DisplayDialog("Menu Manager",
                    "移動モード中は保存できません。\n貼り付けまたはキャンセルしてください。", "OK");
                return;
            }

            var layoutData = _avatar.GetComponent<MenuLayoutData>();
            if (layoutData == null){
                Undo.AddComponent<MenuLayoutData>(_avatar.gameObject);
                layoutData = _avatar.GetComponent<MenuLayoutData>();
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var allItems = new List<MenuLayoutData.ItemLayout>();

            SerializeNode(_rootNode, "", allItems);

            var invDummy = new MenuNode();
            invDummy.Entries.AddRange(_inventory);
            SerializeNode(invDummy, "__INVENTORY__", allItems);

            if (layoutData.BaseLayout == null){
                layoutData.BaseLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Base");
            }
            MenuManagerAuth.SaveLayoutData(layoutData.BaseLayout, layoutData.ExtendedLayout, allItems);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(layoutData);
            PrefabUtility.RecordPrefabInstancePropertyModifications(layoutData);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_avatar.gameObject.scene);

            _hasUnsavedChanges = false;

            var savedItems = new List<MenuLayoutData.ItemLayout>(allItems);

            Debug.Log($"[MenuManager] レイアウトを保存しました: {allItems.Count} アイテム (Saved to {(layoutData.ExtendedLayout != null ? "Extended" : "Base")})");
            if (!silent) {
                EditorUtility.DisplayDialog("Menu Manager",
                    $"メニューレイアウトを保存しました。\n\n" +
                    $"アイテム数: {allItems.Count}\n\n" +
                    $"保存先: {(layoutData.ExtendedLayout != null ? "Extended Layout" : "Base Layout")}\n\n" +
                    $"※ 内部保存は廃止され、データはすべてアセットに保存されました。\n" +
                    $"※ ビルド時にNDMFプラグインが自動でメニューを並び替えます。",
                    "OK");
            }

            RebuildMenu(savedItems);
            Repaint();
        }

        private void SerializeNode(MenuNode node, string currentPath, List<MenuLayoutData.ItemLayout> items){
            if (node == null) return;

            for (int i = 0; i < node.Entries.Count; i++){
                var entry = node.Entries[i];
                if (string.IsNullOrEmpty(entry.PersistentId)){
                    entry.PersistentId = System.Guid.NewGuid().ToString("N");
                }
                var item = new MenuLayoutData.ItemLayout{
                    Key = entry.IsBuildTime ? "__BUILD_TIME__" : entry.PersistentId,
                    Type = GenerateTypeKey(entry),
                    ParentPath = currentPath,
                    Order = i,
                    IsSubMenu = entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu,
                    DisplayName = entry.Name ?? "(no name)",
                    CustomIcon = entry.Icon,
                    IsAutoOverflow = entry.IsAutoOverflow,
                    IsDynamic = entry.IsDynamic
                };
                items.Add(item);

                if (entry.SubMenu != null){
                    string subPath = string.IsNullOrEmpty(currentPath)
                        ? (entry.Name ?? "SubMenu")
                        : currentPath + "/" + (entry.Name ?? "SubMenu");
                    SerializeNode(entry.SubMenu, subPath, items);
                }
            }
        }

        private string GenerateTypeKey(MenuEntry entry){
            if (entry.IsBuildTime) return "__BUILD_TIME__";
            if (entry.IsCustomFolder) return $"SubMenu:{entry.Name}:__custom__:1.00:0";
            if (!string.IsNullOrEmpty(entry.UniqueId)) return entry.UniqueId;
            string paramName = entry.Parameter ?? "";
            return $"{entry.Type}:{entry.Name}:{paramName}:{entry.Value:F2}";
        }

        private string GenerateEntryKey(MenuEntry entry){
            if (entry.IsBuildTime) return "__BUILD_TIME__";
            if (!string.IsNullOrEmpty(entry.PersistentId)) return entry.PersistentId;
            return System.Guid.NewGuid().ToString("N");
        }
        private bool CheckProLimit(int currentStackCount, string actionName = "この操作"){
            return MenuManagerAuth.ValidateLevel(currentStackCount - 1);
        }

        private bool CheckProLimitInventory(int depth, string actionName = "この操作"){
            return MenuManagerAuth.ValidateLevel(depth);
        }
    }
}

#endif