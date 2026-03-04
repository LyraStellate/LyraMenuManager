#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Lyra;

namespace Lyra.Editor{
    public partial class MenuManager{
        private void DrawInventoryArea(){
            if (!_showInventory){
                var narrow = EditorGUILayout.BeginVertical(GUILayout.Width(30));
                EditorGUI.DrawRect(narrow, CRUMB_BG);
                DrawBorder(narrow, SEPARATOR, 1f);

                EditorGUILayout.Space(8);
                if (GUILayout.Button("▶", _sBtnNoPadding, GUILayout.Width(24), GUILayout.Height(100))){
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
            EditorGUILayout.LabelField(" インベントリ", _sHeaderLeft);
            if (GUILayout.Button("◀", _sBtnNoPadding, GUILayout.Width(24), GUILayout.Height(24))){
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
                        _needsOverflowReeval = true;
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
            EditorGUILayout.LabelField(" Extra Option", _sSmallLeft);
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
            if (GUILayout.Button("現在の階層\nを回収", _sBtnSmall, GUILayout.Height(36))){
                Undo.RecordObject(this, "Collect To Inventory");
                _inventory.AddRange(FlattenMoreMenus(CurEntries()).Where(e => !e.IsBuildTime));
                CurEntries().Clear();
                _hasUnsavedChanges = true;
                _selectedIdx = -1;
                _needsOverflowReeval = true;
                Repaint();
            }
            if (GUILayout.Button("全回収", _sBtnSmall, GUILayout.Height(36))){
                Undo.RecordObject(this, "Collect All recursive");
                _inventory.AddRange(FlattenMoreMenus(_rootNode.Entries).Where(e => !e.IsBuildTime));
                _rootNode.Entries.Clear();
                _selectedIdx = -1;
                NavToLevel(0);
                _hasUnsavedChanges = true;
                _needsOverflowReeval = true;
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
                if (entry.IsDynamic) {
                    result.Add(entry);
                }
                else if (IsMoreSubMenu(entry)){
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
            return entry.IsAutoOverflow && !entry.IsDynamic;
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
                var labelStyle = entry.IsCustomFolder ? _sLabelItalicLeft : _sLabelLeft;
                
                bool isEmptyFolder = entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (entry.SubMenu == null || entry.SubMenu.Entries.Count == 0);

                var prevContentColor = GUI.contentColor;
                if (entry.IsEditorOnly) GUI.contentColor = new Color(1f, 0.35f, 0.35f);
                else if (entry.IsNewEntry) GUI.contentColor = new Color(0.3f, 0.6f, 1.0f);
                else if (entry.IsAutoOverflow) GUI.contentColor = Color.gray;
                else if (isEmptyFolder) GUI.contentColor = new Color(1f, 0.85f, 0.2f);

                if (_editingInventoryNameEntry == entry){
                    GUI.SetNextControlName("InventoryRenameField");
                    _editingInventoryNameStr = EditorGUI.TextField(labelRect, _editingInventoryNameStr, _sTextFieldLeft);
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
                GUI.contentColor = prevContentColor;

                var tc = TypeColor(entry.Type);
                var badgeRect = new Rect(itemRect.xMax - 38, itemRect.y + 6, 32, 16);
                EditorGUI.DrawRect(badgeRect, tc * 0.25f);
                DrawBorder(badgeRect, tc * 0.6f, 1f);
                var tmpColor = GUI.contentColor;
                GUI.contentColor = tc;
                GUI.Label(badgeRect, TypeShort(entry.Type), _sBadgeBold);
                GUI.contentColor = tmpColor;

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

                    if (capEntry.IsBuildTime) {
                        menu.AddDisabledItem(new GUIContent("名前を編集"));
                    }
                    else {
                        menu.AddItem(new GUIContent("名前を編集"), false, () => {
                            if (!CheckProLimit(depth)) return;
                            _editingInventoryNameEntry = capEntry;
                            _editingInventoryNameStr = capEntry.Name ?? "";
                            _focusInventoryNameField = true;
                            Repaint();
                        });
                    }

                    menu.AddSeparator("");

                    if (!capEntry.IsBuildTime && capEntry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (capEntry.SourceInstaller != null || capEntry.SourceAsset != null || capEntry.SourceMenuItem != null)){
                        menu.AddItem(new GUIContent("リセット"), false, () => ResetSubmenu(capEntry, capList));
                    }

                    if (capEntry.IsCustomFolder || capEntry.IsBuildTime){
                        menu.AddItem(new GUIContent("削除"), false, () => {
                            Undo.RecordObject(this, "Delete Folder");

                            if (capEntry.SubMenu != null && capEntry.SubMenu.Entries.Count > 0){
                                int idx = capList.IndexOf(capEntry);
                                for (int j = 0; j < capEntry.SubMenu.Entries.Count; j++){
                                    capList.Insert(idx + 1 + j, capEntry.SubMenu.Entries[j]);
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
    }
}

#endif
