#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Lyra;

namespace Lyra.Editor{
    public partial class MenuManager{
        private void RebuildMenu(List<MenuLayoutData.ItemLayout> tempLayoutItems = null){
            if (_avatar == null) { ClearMenu(); return; }

            _inventory.Clear();

            var savedPath = new List<string>();
            foreach (var crumb in _navStack){
                savedPath.Add(crumb.Name);
            }

            try{
                GetInstallersMaps(out var menuToInstallers, out var rootInstallers);

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
            var consumed = new HashSet<MenuLayoutData.ItemLayout>();

            ExtractMappedEntries(_rootNode, layoutItems, pool, consumed, MatchMode.Strict);
            ExtractMappedEntries(_rootNode, layoutItems, pool, consumed, MatchMode.Relaxed);

            var newEntries = new List<MenuEntry>();
            CollectRemainingEntries(_rootNode, pool, newEntries);

            RebuildNodeLevel(_rootNode, "", "", layoutItems, pool);

            var invDummy = new MenuNode();
            RebuildNodeLevel(invDummy, "__INVENTORY__", "__INVENTORY__", layoutItems, pool);
            _inventory.AddRange(invDummy.Entries);

            if (pool.Count > 0){
                _inventory.AddRange(pool);
            }

            foreach (var ne in newEntries){
                ne.IsNewEntry = true;
                MarkNewEntryRecursive(ne);
                if (_autoAddNewItemsToRoot){
                    _rootNode.Entries.Add(ne);
                }
                else {
                    _inventory.Add(ne);
                }
            }

            ExtractEditorOnly(_rootNode, _inventory);
            ApplyOverflowMoreRecursive(_rootNode);
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

        private void GetInstallersMaps(out Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>> menuToInstallers, out List<ModularAvatarMenuInstaller> rootInstallers){
            menuToInstallers = new Dictionary<VRCExpressionsMenu, List<ModularAvatarMenuInstaller>>();
            rootInstallers = new List<ModularAvatarMenuInstaller>();

            if (_avatar == null) return;

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
                    MarkNewEntryRecursive(e);
                    collected.Insert(0, e);
                }
            }
        }

        private enum MatchMode { Strict, Relaxed }

        private void MarkNewEntryRecursive(MenuEntry entry){
            if (entry == null) return;
            entry.IsNewEntry = true;
            if (entry.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && entry.SubMenu != null){
                foreach (var sub in entry.SubMenu.Entries){
                    MarkNewEntryRecursive(sub);
                }
            }
        }

        private void ExtractMappedEntries(MenuNode node, List<MenuLayoutData.ItemLayout> layoutItems, List<MenuEntry> pool, HashSet<MenuLayoutData.ItemLayout> consumed, MatchMode mode){
            if (node == null || node.Entries == null) return;

            for (int i = node.Entries.Count - 1; i >= 0; i--){
                var e = node.Entries[i];

                var match = layoutItems.FirstOrDefault(item => !consumed.Contains(item) && 
                    (mode == MatchMode.Strict ? MatchEntryStrict(e, item) : MatchEntryRelaxed(e, item)));

                if (match != null){
                    consumed.Add(match);
                    node.Entries.RemoveAt(i);
                    e.IsDynamic = match.IsDynamic;
                    pool.Add(e);

                    if (!(match.IsSubMenu && match.IsDynamic)){
                        if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                            ExtractMappedEntries(e.SubMenu, layoutItems, pool, consumed, mode);
                        }
                    }
                }
                else{
                    if (e.Type == VRCExpressionsMenu.Control.ControlType.SubMenu && e.SubMenu != null){
                        ExtractMappedEntries(e.SubMenu, layoutItems, pool, consumed, mode);
                    }
                }
            }
        }

        private void RebuildNodeLevel(MenuNode currentNode, string currentPath, string legacyPath, List<MenuLayoutData.ItemLayout> layoutItems, List<MenuEntry> pool){
            var itemsInPath = layoutItems
                .Where(item => item.ParentPath == currentPath || (!string.IsNullOrEmpty(legacyPath) && item.ParentPath == legacyPath))
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

                    if (!string.IsNullOrEmpty(item.DisplayName)) e.Name = item.DisplayName;

                    if (item.IsSubMenu){
                        if (e.SubMenu == null) e.SubMenu = new MenuNode { Name = e.Name ?? item.DisplayName };
                        
                        if (item.IsDynamic) {
                        }
                        else {
                            string subPath = string.IsNullOrEmpty(currentPath) ? (item.Key) : currentPath + "/" + (item.Key);
                            string legPath = string.IsNullOrEmpty(legacyPath) ? (e.Name ?? item.DisplayName) : legacyPath + "/" + (e.Name ?? item.DisplayName);
                            RebuildNodeLevel(e.SubMenu, subPath, legPath, layoutItems, pool);
                        }
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
                        string subPath = string.IsNullOrEmpty(currentPath) ? item.Key : currentPath + "/" + item.Key;
                        string legPath = string.IsNullOrEmpty(legacyPath) ? item.DisplayName : legacyPath + "/" + item.DisplayName;
                        RebuildNodeLevel(virtualSub, subPath, legPath, layoutItems, pool);
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

        private bool MatchEntryStrict(MenuEntry e, MenuLayoutData.ItemLayout itemLayout){
            string entryID = GetSourceObjId(e);
            string layoutID = itemLayout.SourceObjId ?? "";

            if (!string.IsNullOrEmpty(layoutID)){
                if (entryID == layoutID) return true;
                if (e.SourceMenuItem != null && UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(e.SourceMenuItem.gameObject).ToString() == layoutID) return true;
                if (e.SourceInstaller != null && UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(e.SourceInstaller.gameObject).ToString() == layoutID) return true;
            }

            if (!string.IsNullOrEmpty(itemLayout.Key) && !string.IsNullOrEmpty(e.PersistentId)){
                if (e.PersistentId == itemLayout.Key) return true;
            }

            return false;
        }

        private bool MatchEntryRelaxed(MenuEntry e, MenuLayoutData.ItemLayout itemLayout){
            string typeKey = !string.IsNullOrEmpty(itemLayout.Type) ? itemLayout.Type : itemLayout.Key;
            if (GenerateTypeKey(e) == typeKey) return true;
{
                string[] parts = typeKey.Split(new[] { ':' }, 5);
                string typeStr = parts.Length > 0 ? parts[0] : "";
                string nameStr = parts.Length > 1 ? parts[1] : itemLayout.DisplayName;
                if (e.Type.ToString() == typeStr && e.Name == nameStr) return true;
            }

            return false;
        }

        private MenuEntry FetchEntryFromPool(List<MenuEntry> pool, MenuLayoutData.ItemLayout itemLayout){
            string layoutID = itemLayout.SourceObjId ?? "";
            int idx = -1;

            if (!string.IsNullOrEmpty(layoutID)){
                idx = pool.FindIndex(e => {
                    string entryID = GetSourceObjId(e);
                    if (entryID == layoutID) return true;
                    if (e.SourceMenuItem != null && UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(e.SourceMenuItem.gameObject).ToString() == layoutID) return true;
                    if (e.SourceInstaller != null && UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(e.SourceInstaller.gameObject).ToString() == layoutID) return true;
                    return false;
                });
                
                if (idx >= 0) {
                    var e = pool[idx];
                    pool.RemoveAt(idx);
                    e.PersistentId = itemLayout.Key;
                    return e;
                }
            }

            if (idx < 0 && !string.IsNullOrEmpty(itemLayout.Key)) {
                idx = pool.FindIndex(e => !string.IsNullOrEmpty(e.PersistentId) && e.PersistentId == itemLayout.Key);
            }

            if (idx < 0) {
                string typeKey = !string.IsNullOrEmpty(itemLayout.Type) ? itemLayout.Type : itemLayout.Key;
                idx = pool.FindIndex(e => GenerateTypeKey(e) == typeKey);
            }

            if (idx < 0) {
                string typeKey = !string.IsNullOrEmpty(itemLayout.Type) ? itemLayout.Type : itemLayout.Key;
                string[] parts = typeKey.Split(new[] { ':' }, 5);
                string typeStr = parts.Length > 0 ? parts[0] : "";
                string nameStr = parts.Length > 1 ? parts[1] : itemLayout.DisplayName;
                idx = pool.FindIndex(e => e.Type.ToString() == typeStr && e.Name == nameStr);
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
    }
}

#endif
