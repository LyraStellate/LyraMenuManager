#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Lyra;

namespace Lyra.Editor{
    public partial class MenuManager{
        private void InitStyles(){
            if (_stylesOk) return;
            _sHeader = new GUIStyle(EditorStyles.boldLabel){
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TEXT_PRI }
            };
            _sCrumb = new GUIStyle(EditorStyles.label){
                fontSize = 11,
                padding = new RectOffset(2, 2, 0, 0),
                normal = { textColor = TEXT_SEC },
                hover = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal,
                margin = new RectOffset(0, 0, 0, 0)
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
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 3, 3),
                fixedHeight = 26
            };
            _sCenter = new GUIStyle(EditorStyles.label){
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = TEXT_SEC }
            };
            _sLabelItalic = new GUIStyle(_sLabel){ fontStyle = FontStyle.Italic };
            _sLabelLeft = new GUIStyle(_sLabel){ alignment = TextAnchor.MiddleLeft };
            _sLabelItalicLeft = new GUIStyle(_sLabelItalic){ alignment = TextAnchor.MiddleLeft };
            _sCenterLarge = new GUIStyle(_sCenter){ fontSize = 12 };
            _sBadge = new GUIStyle(_sSmall);
            _sTextFieldCenter = new GUIStyle(EditorStyles.textField){ alignment = TextAnchor.MiddleCenter };

            _sHeaderLeft = new GUIStyle(_sHeader){ alignment = TextAnchor.MiddleLeft };
            _sSmallLeft = new GUIStyle(_sSmall){ alignment = TextAnchor.MiddleLeft };
            _sBtnNoPadding = new GUIStyle(_sBtn){ padding = new RectOffset(0, 0, 0, 0) };
            _sBtnSmall = new GUIStyle(_sBtn){ fontSize = 11 };
            _sTextFieldLeft = new GUIStyle(EditorStyles.textField){ alignment = TextAnchor.MiddleLeft };
            _sBadgeBold = new GUIStyle(_sSmall){ fontStyle = FontStyle.Bold };

            _sIconBtn = new GUIStyle(EditorStyles.miniButton){
                padding = new RectOffset(4, 4, 4, 4),
                fixedWidth = 26,
                fixedHeight = 26
            };

            _sBtnIcon = new GUIStyle(_sBtn){
                padding = new RectOffset(4, 4, 4, 4),
                fixedWidth = 30
            };

            _sHeaderDropZone = new GUIStyle(_sHeader){
                fontSize = 16,
                normal = { textColor = TEXT_SEC },
                wordWrap = true
            };
            
            _sCrumbSep = new GUIStyle(EditorStyles.miniLabel) { 
                normal = { textColor = TEXT_SEC * 0.7f },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(4, 4, 4, 0),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            
            _sCrumbNorm = new GUIStyle(EditorStyles.label) {
                fontSize = 11,
                padding = new RectOffset(4, 4, 0, 0),
                margin = new RectOffset(0, 0, 2, 0),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TEXT_SEC }
            };

            _sCrumbBold = new GUIStyle(EditorStyles.boldLabel) {
                fontSize = 11,
                padding = new RectOffset(4, 4, 0, 0),
                margin = new RectOffset(0, 0, 2, 0),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TEXT_PRI }
            };
            
            _sCrumbHover = new GUIStyle(_sCrumbNorm) {
                normal = { textColor = Color.white }
            };

            _sCenterPlus = new GUIStyle(_sCenter){
                fontSize = 18,
                normal = { textColor = new Color(1, 1, 1, 0.12f) }
            };

            _sMiniBtnCentered = new GUIStyle(EditorStyles.miniButton) {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _sLabelItem = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12
            };

            _sBoldLabelItem = new GUIStyle(_sLabelItem) {
                fontStyle = FontStyle.Bold
            };

            _sBadgeLabel = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };

            _sSettingsBtn = new GUIStyle(GUI.skin.button) {
                padding = new RectOffset(6, 6, 6, 6),
                fixedWidth = 34,
                fixedHeight = 34,
                margin = new RectOffset(2, 2, 2, 2)
            };

            _stylesOk = true;
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
                    bool isPro = MenuManagerAuthGuard.CanUseExtended();
                    if (isPro){
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
                    }
                    else{
                        EditorGUILayout.LabelField("※ Pro版でアンロック", EditorStyles.miniLabel);
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
                _sHeaderDropZone);

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

        private Vector2 _avatarListScrollPos;
        
        private List<VRCAvatarDescriptor> _cachedAvatars = new List<VRCAvatarDescriptor>();
        private double _lastAvatarRefreshTime = 0;

        private void DrawAvatarList() {
            if (EditorApplication.timeSinceStartup - _lastAvatarRefreshTime > 1.0) {
                _lastAvatarRefreshTime = EditorApplication.timeSinceStartup;
                var avatars = UnityEngine.Object.FindObjectsOfType<VRCAvatarDescriptor>(true);
                _cachedAvatars = avatars?
                    .Where(av => av != null && av.gameObject != null && av.gameObject.scene.IsValid())
                    .OrderByDescending(av => av.gameObject.activeInHierarchy)
                    .ThenBy(av => av.gameObject.name)
                    .ToList() ?? new List<VRCAvatarDescriptor>();
            }

            if (_cachedAvatars.Count == 0) return;
            var validAvatars = _cachedAvatars;

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24);
            EditorGUILayout.LabelField("シーン内のアバター", _sHeaderLeft);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            var listBgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(listBgRect, Color.black * 0.15f);

            _avatarListScrollPos = EditorGUILayout.BeginScrollView(_avatarListScrollPos, GUILayout.Height(400));
            
            EditorGUILayout.Space(12);

            var evt = Event.current;
            float viewWidth = EditorGUIUtility.currentViewWidth - 60;
            float cardWidth = (viewWidth / 2f) - 6; 
            float cardHeight = 44; 

            for (int i = 0; i < validAvatars.Count; i += 2) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(6);

                for (int j = 0; j < 2; j++) {
                    int idx = i + j;
                    if (idx >= validAvatars.Count) {
                        GUILayout.Space(cardWidth + 8);
                        continue;
                    }

                    var av = validAvatars[idx];
                    if (av == null) {
                        GUILayout.Space(cardWidth + 8);
                        continue;
                    }
                    var cardRect = GUILayoutUtility.GetRect(cardWidth, cardHeight);
                    
                    bool isSelected = (_avatar == av);
                    bool isHover = cardRect.Contains(evt.mousePosition);
                    bool isActive = av.gameObject.activeInHierarchy;

                    if (evt.type == EventType.MouseMove && isHover) Repaint();

                    Color cardBaseCol = new Color(0.28f, 0.28f, 0.28f);
                    Color bgCol = isSelected ? ACCENT * 0.45f : (isHover ? Color.white * 0.15f : cardBaseCol);
                    if (!isActive && !isSelected && !isHover) bgCol.a *= 0.6f;
                    EditorGUI.DrawRect(cardRect, bgCol);
                    
                    Color borderCol = isSelected ? ACCENT : (isHover ? Color.white * 0.3f : Color.white * 0.08f);
                    DrawBorder(cardRect, borderCol, 1f);

                    var nameRect = new Rect(cardRect.x + 8, cardRect.y + 6, cardRect.width - 52, 18);
                    var nameStyle = isSelected ? _sBoldLabelItem : _sLabelItem;
                    nameStyle.normal.textColor = isActive ? Color.white : Color.gray;
                    GUI.Label(nameRect, av.gameObject.name, nameStyle);

                    var layoutData = av.GetComponent<MenuLayoutData>();
                    if (layoutData != null) {
                        float badgeY = cardRect.y + 26;
                        float badgeX = cardRect.x + 8;
                        if (layoutData.BaseLayout != null) {
                            DrawBadge(new Rect(badgeX, badgeY, 34, 14), "Base", ACCENT);
                            badgeX += 38;
                        }
                        if (layoutData.ExtendedLayout != null) {
                            DrawBadge(new Rect(badgeX, badgeY, 30, 14), "Ext", ACCENT_SUB);
                        }
                    }

                    var selectBtnRect = new Rect(cardRect.xMax - 48, cardRect.y + 4, 44, cardHeight - 8);
                    if (GUI.Button(selectBtnRect, "選択", _sMiniBtnCentered)) {
                        Selection.activeGameObject = av.gameObject;
                        EditorGUIUtility.PingObject(av.gameObject);
                        evt.Use();
                    }
                    else if (evt.type == EventType.MouseDown && cardRect.Contains(evt.mousePosition) && !selectBtnRect.Contains(evt.mousePosition) && evt.button == 0) {
                        _avatar = av;
                        SaveLastAvatarIdentifier();
                        ClearMenu();
                        RebuildMenu();
                        evt.Use();
                    }

                    GUILayout.Space(6);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(6);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawBadge(Rect r, string text, Color color) {
            EditorGUI.DrawRect(r, color * 0.2f);
            DrawBorder(r, color * 0.5f, 1f);
            var style = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = color }
            };
            GUI.Label(r, text, style);
        }

        private int _totalCrumbWidth = 0;
        private void DrawBreadcrumbs(){
            _dropCrumbIdx = -1;
            var evt = Event.current;

            for (int i = 0; i < _navStack.Count; i++){
                bool isLast = (i == _navStack.Count - 1);
                var nm = string.IsNullOrEmpty(_navStack[i].Name) ? "Root" : _navStack[i].Name;
                
                if (i > 0) {
                    var sepContent = new GUIContent(">");
                    var szSep = _sCrumbSep.CalcSize(sepContent);
                    GUILayout.Label(sepContent, _sCrumbSep, GUILayout.Width(szSep.x), GUILayout.Height(28));
                }

                var gc = new GUIContent(nm);
                GUIStyle baseStyle = isLast ? _sCrumbBold : _sCrumbNorm;
                var sz = baseStyle.CalcSize(gc);
                
                var rc = GUILayoutUtility.GetRect(sz.x + 2, 28, GUILayout.ExpandWidth(false));
                
                bool isHover = rc.Contains(evt.mousePosition);
                GUIStyle currentStyle = isHover ? _sCrumbHover : baseStyle;
                
                if (isLast) {
                    var activeBg = ACCENT * 0.15f;
                    activeBg.a = 0.3f;
                    EditorGUI.DrawRect(rc, activeBg);
                    EditorGUI.DrawRect(new Rect(rc.x, rc.yMax - 1, rc.width, 2), ACCENT);
                }
                else if (isHover) {
                    EditorGUI.DrawRect(rc, Color.white * 0.1f);
                }

                if (_isDragging && _dragIdx >= 0 && !isLast && isHover){
                    _dropCrumbIdx = i;
                    EditorGUI.DrawRect(rc, new Color(0.20f, 0.70f, 0.20f, 0.4f)); 

                    if (evt.rawType == EventType.MouseUp){
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
                        evt.Use();
                        return;
                    }
                }

                if (GUI.Button(rc, gc, currentStyle)){
                    NavToLevel(i);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private GUIContent _settingsIconContent;
        private void DrawGlobalSettings(){
            if (_settingsIconContent == null || _settingsIconContent.image == null){
                _settingsIconContent = EditorGUIUtility.IconContent("SettingsIcon");
                _settingsIconContent.tooltip = "ビルド・詳細設定を開く";
            }
            if (GUILayout.Button(_settingsIconContent, _sSettingsBtn, GUILayout.Width(34), GUILayout.Height(34))){
                var wins = Resources.FindObjectsOfTypeAll<MenuManagerSettings>();
                bool anyOpen = false;
                foreach (var w in wins) if (w != null){ anyOpen = true; break; }
                if (anyOpen) {
                    foreach (var w in wins) if (w != null) w.Close();
                } else {
                    MenuManagerSettings.ShowWindow();
                }
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

            int prevHoverIdx = _hoverIdx;
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

                        _editingNameStr = EditorGUI.TextField(labelRect, _editingNameStr, _sTextFieldCenter);
                        
                        if (_focusNameField){
                            EditorGUI.FocusTextInControl("InlineRenameField");
                            _focusNameField = false;
                        }
                    }
                    else{
                        string displayName = entry.Name ?? "(no name)";
                        if (entry.IsDynamic) displayName = "*" + displayName;
                        else if (entry.IsCustomFolder) displayName = "." + displayName;
                        
                        var labelStyle = entry.IsCustomFolder ? _sLabelItalic : _sLabel;
                        bool isEmptyFolder = entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (entry.SubMenu == null || entry.SubMenu.Entries.Count == 0);

                        var prevContentColor = GUI.contentColor;
                        if (entry.IsEditorOnly) GUI.contentColor = new Color(1f, 0.35f, 0.35f);
                        else if (entry.IsNewEntry) GUI.contentColor = new Color(0.3f, 0.6f, 1.0f);
                        else if (entry.IsAutoOverflow) GUI.contentColor = Color.gray;
                        else if (isEmptyFolder) GUI.contentColor = new Color(1f, 0.85f, 0.2f);
                        
                        GUI.Label(labelRect, displayName, labelStyle);
                        GUI.contentColor = prevContentColor;
                    }

                    var tc = TypeColor(entry.Type);
                    var badge = new Rect(ic.x - 22, ic.y + ICON_SIZE / 2f + 14, 44, 14);
                    EditorGUI.DrawRect(badge, tc * 0.35f);
                    
                    var oldBadgeTC = _sBadge.normal.textColor;
                    _sBadge.normal.textColor = tc;
                    GUI.Label(badge, TypeShort(entry.Type), _sBadge);
                    _sBadge.normal.textColor = oldBadgeTC;
                }
                else if (isDummyBack){
                    string txt = isRoot ? "HOME" : "Back";
                    GUI.Label(new Rect(ic.x - 52, ic.y - 10, 104, 20), txt, _sLabel);
                }
                else if (isDummyQA){
                    GUI.Label(new Rect(ic.x - 52, ic.y - 10, 104, 20), "Quick Actions", _sLabel);
                }
                else{
                    GUI.Label(new Rect(ic.x - 10, ic.y - 8, 20, 16), "+", _sCenterPlus);
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
            var oldCenterTC = _sCenterLarge.normal.textColor;
            _sCenterLarge.normal.textColor = cCol;
            GUI.Label(new Rect(center.x - 30, center.y - 10, 60, 20), cTxt, _sCenterLarge);
            _sCenterLarge.normal.textColor = oldCenterTC;

            HandleInput(evt, center, entries);

            if (_dropBorderIdx >= 0){
                float borderAngle = (startA - _dropBorderIdx * step) * Mathf.Deg2Rad;
                var bDir = new Vector2(Mathf.Cos(borderAngle), -Mathf.Sin(borderAngle));
                DrawHandleLine(
                    center + bDir * (INNER_RADIUS - 3),
                    center + bDir * (WHEEL_RADIUS + 4),
                    new Color(1f, 0.85f, 0.2f), 5f);
            }

            if (evt.type == EventType.MouseMove) {
                if (prevHoverIdx != _hoverIdx) {
                    Repaint();
                }
            }
        }

        private void DrawToolbar(){
            EditorGUILayout.Space(6);
            
            EditorGUILayout.BeginHorizontal(GUILayout.Height(26));
            GUILayout.FlexibleSpace();
            
            var entries = CurEntries();
            GUI.enabled = entries.Count < MAX_CONTROLS;
            if (GUILayout.Button("フォルダ作成", _sBtn, GUILayout.Width(140)))
                AddNewSubMenu();
            GUI.enabled = true;
            
            GUILayout.Space(12);

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = _hasUnsavedChanges ? new Color(0.2f, 0.9f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);
            if (GUILayout.Button("保存", _sBtn, GUILayout.Width(120))){
                SaveLayout();
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(8);
            var reloadContent = EditorGUIUtility.IconContent("Refresh");
            reloadContent.tooltip = "再読み込み";
            if (GUILayout.Button(reloadContent, _sBtnIcon))
                RebuildMenu();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
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

            bool isProRestricted = !isFromInventory && MenuManagerAuthGuard.GetDepthStatusText(_navStack.Count - 1) != null;
            if (isProRestricted){
                EditorGUILayout.HelpBox("️ 第3階層以上の編集は 有料版限定機能です。", MessageType.Warning);
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

            EditorGUI.BeginChangeCheck();
            var newIcon = (Texture2D)EditorGUILayout.ObjectField("アイコン", e.Icon, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (EditorGUI.EndChangeCheck()){
                Undo.RecordObject(this, "Set Icon");
                e.Icon = newIcon;
                _hasUnsavedChanges = true;
                Repaint();
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();{
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("タイプ", e.Type.ToString());
                EditorGUILayout.LabelField("パラメータ",
                    string.IsNullOrEmpty(e.Parameter) ? "(none)" : e.Parameter);
                EditorGUILayout.LabelField("値", e.Value.ToString("F2"));
                EditorGUILayout.EndVertical();

                if (e.Icon != null){
                    GUILayout.Space(10);
                    var ir = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                    GUI.DrawTexture(ir, e.Icon, ScaleMode.ScaleToFit);
                }
            }
            EditorGUILayout.EndHorizontal();

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
    }
}

#endif
