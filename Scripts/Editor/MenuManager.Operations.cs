#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Lyra;

namespace Lyra.Editor{
    public partial class MenuManager{
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
            if (!CheckProLimit(_navStack.Count - 1)) return;
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
            if (!MenuManagerAuthGuard.GuardedNavInto(_navStack.Count)) return;

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

            var moving = entries[from];
            entries.RemoveAt(from);
            entries.Insert(to, moving);
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

            string newId = Guid.NewGuid().ToString("N");
            var virtualSub = new MenuNode { Name = "New Folder" };
            var virtualEntry = new MenuEntry{
                Name = "New Folder",
                Icon = GetIcon("folder.png"),
                Type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                SubMenu = virtualSub,
                IsDynamic = false,
                IsCustomFolder = true,
                PersistentId = newId
            };

            if (!MenuManagerAuthGuard.GuardedNavInto(_navStack.Count)) return;
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
            ExpandCustomFolderChildren(e, idx);
            
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
            ExpandCustomFolderChildren(e, idx);
            
            _selectedIdx = -1;
            _showDetail = false;
            _hasUnsavedChanges = true;
            _needsOverflowReeval = true;
            Repaint();
        }

        private void ExpandCustomFolderChildren(MenuEntry folder, int insertIdx){
            if (!folder.IsCustomFolder) return;
            if (folder.SubMenu == null || folder.SubMenu.Entries.Count == 0) return;

            var parentNode = _navStack[_navStack.Count - 1].Node;
            var children = new List<MenuEntry>(folder.SubMenu.Entries);
            folder.SubMenu.Entries.Clear();
            foreach (var child in children){
                InsertItemWithOverflow(parentNode, insertIdx, child);
                insertIdx++;
            }
        }

        private void ClearMenu(){
            _rootNode = null;
            _navStack.Clear();
            _inventory.Clear();
            _selectedIdx = -1;
            _selectedInventoryEntry = null;
            _showDetail = false;
            _editingNameIdx = -1;
            _hasUnsavedChanges = false;
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
    }
}

#endif
