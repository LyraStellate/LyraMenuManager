#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Lyra.Editor{
    public partial class MenuManager{
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

        private void ShowContextMenu(int idx, List<MenuEntry> entries){
            var menu = new GenericMenu();
            bool has = idx >= 0 && idx < entries.Count;

            if (has){
                var e = entries[idx];
                if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                    menu.AddItem(new GUIContent("サブメニューを開く"), false, () => NavInto(idx));
                    menu.AddSeparator("");
                }

                if (idx > 0)
                    menu.AddItem(new GUIContent("上に移動"), false, () => ReorderEntry(idx, idx - 1));
                else
                    menu.AddDisabledItem(new GUIContent("上に移動"));

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

                    if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && (e.SourceInstaller != null || e.SourceAsset != null || e.SourceMenuItem != null || e.SourceLilyCalItem != null)){
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
            if (entries.Count < MAX_CONTROLS){
                menu.AddItem(new GUIContent("フォルダ追加"), false, () => AddNewSubMenu());
            }

            menu.ShowAsContext();
        }
    }
}

#endif
