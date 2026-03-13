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
    public partial class MenuManager : EditorWindow{
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

        [SerializeField] private bool _hasUnsavedChanges;
        [SerializeField]        private bool _needsOverflowReeval = false;
        private Vector2 _scrollPos = Vector2.zero;
        private Vector2 _detailScrollPos = Vector2.zero;
        private Vector2 _crumbScrollPos = Vector2.zero;

        private GUIStyle _sHeader, _sCrumb, _sLabel, _sSmall, _sBtn, _sCenter;
        private GUIStyle _sLabelItalic, _sLabelLeft, _sLabelItalicLeft, _sCenterLarge, _sBadge, _sTextFieldCenter;
        private GUIStyle _sHeaderLeft, _sSmallLeft, _sBtnNoPadding, _sBtnSmall, _sTextFieldLeft, _sBadgeBold, _sIconBtn, _sBtnIcon;
        private GUIStyle _sHeaderDropZone, _sCrumbSep, _sCrumbNorm, _sCrumbBold, _sCrumbHover, _sCenterPlus;
        private GUIStyle _sLabelItem;
        private GUIStyle _sBoldLabelItem;
        private GUIStyle _sMiniBtnCentered;
        private GUIStyle _sBadgeLabel, _sSettingsBtn;
        private bool _stylesOk;
        private bool _useVRcStyleUI = true;
        private bool _showInventory = true;
        private bool _autoAddNewItemsToRoot = false;

        private Dictionary<string, Texture2D> _iconCache;

        [MenuItem("Tools/Lyra Menu Manager/Menu Manager", false, 1000)]
        public static void ShowWindow(){
            var w = GetWindow<MenuManager>("Menu Manager");
            w.minSize = new Vector2(680, 750);
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
            var editors = ActiveEditorTracker.sharedTracker.activeEditors;
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

        private void OnGUI(){
            if (_navStack == null) _navStack = new List<BreadcrumbEntry>();
            if (_inventory == null) _inventory = new List<MenuEntry>();
            if (_extraOptions == null) _extraOptions = new List<MenuEntry>();

            InitStyles();

            if (_avatar == null && _rootNode != null){
                _avatar = null;
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
                    _navStack.Add(new BreadcrumbEntry { Node = _rootNode, Name = _rootNode.Name ?? (_avatar != null ? _avatar.gameObject.name : "Root") });
                }

                DrawInventoryArea();
            }

            EditorGUILayout.BeginVertical();

            if (_rootNode == null || _navStack.Count == 0){
                DrawDropZone();
                DrawAvatarList();
            }
            else{
                EditorGUILayout.BeginHorizontal();
                
                _crumbScrollPos = EditorGUILayout.BeginScrollView(_crumbScrollPos, GUIStyle.none, GUIStyle.none, GUILayout.Height(30));
                EditorGUILayout.BeginHorizontal();
                DrawBreadcrumbs();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                GUILayout.Space(8);
                DrawGlobalSettings();
                GUILayout.Space(12);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);

                DrawRadialWheel();
                DrawToolbar();

                if (_showDetail && (_selectedIdx >= 0 || _selectedInventoryEntry != null)){
                    _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);
                    DrawDetailPanel();
                    EditorGUILayout.EndScrollView();
                }
                else{
                    GUILayout.FlexibleSpace();
                }
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
    }
}

#endif