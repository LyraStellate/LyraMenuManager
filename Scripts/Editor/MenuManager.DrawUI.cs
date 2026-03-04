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
                    var badgeStyle = new GUIStyle(_sBadge) { normal = { textColor = tc } };
                    GUI.Label(badge, TypeShort(entry.Type), badgeStyle);
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
    }
}

#endif
