#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using nadena.dev.modular_avatar.core;

[assembly: ExportsPlugin(typeof(Lyra.MenuManagerPlugin))]

namespace Lyra{
    public class MenuManagerPlugin : Plugin<MenuManagerPlugin>{
        public override string QualifiedName => "lyra.menu-manager";
        public override string DisplayName => "Lyra Menu Manager";

        protected override void Configure(){
            var seq = InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("com.uminato.changelocomotionkai");

            try{
                var afterPlugins = new HashSet<string>();

                try{
                    string raw = EditorPrefs.GetString("Lyra.MenuManager.Settings.AfterPlugins", "");
                    if (!string.IsNullOrEmpty(raw)){
                        var parts = raw.Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in parts){
                            var qn = p.Trim();
                            if (!string.IsNullOrEmpty(qn))
                                afterPlugins.Add(qn);
                        }
                    }
                }
                catch (System.Exception e){
                    Debug.LogWarning($"[MenuManagerPlugin] Failed to load project-wide AfterPlugins from EditorPrefs: {e.Message}");
                }

                var allData = UnityEngine.Object.FindObjectsOfType<MenuLayoutData>();
                foreach (var data in allData){
                    if (data.RunAfterPlugins != null){
                        foreach (var qn in data.RunAfterPlugins){
                            if (!string.IsNullOrEmpty(qn))
                                afterPlugins.Add(qn);
                        }
                    }
                }

                foreach (var pluginName in afterPlugins){
                    seq = seq.AfterPlugin(pluginName);
                    Debug.Log($"[MenuManagerPlugin] AfterPlugin 制約を追加: {pluginName}");
                }
            }
            catch (System.Exception e){
                Debug.LogWarning($"[MenuManagerPlugin] AfterPlugin 設定の収集に失敗: {e.Message}");
            }

            seq.Run("Reorder Menus", ctx =>{
                    var avatarRoot = ctx.AvatarRootObject;
                    if (avatarRoot == null) return;

                    var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
                    if (descriptor == null || descriptor.expressionsMenu == null) return;

                    var layoutData = avatarRoot.GetComponent<MenuLayoutData>();
                    if (layoutData == null || !layoutData.IsEnabled || layoutData.Items == null || layoutData.Items.Count == 0){
                        return;
                    }

                    bool debugLog = layoutData.EnableDebugLog;
                    bool detailedLog = layoutData.EnableDetailedDebugLog;

                    bool autoAddNewItemsToRoot = EditorPrefs.GetBool("Lyra.MenuManager.AutoAddNewItemsToRoot", false);

                    if (debugLog) Debug.Log($"[MenuManagerPlugin] メニュー並び替えを開始: {layoutData.Items.Count} アイテム (avatar='{avatarRoot.name}')");

                    var visited = new HashSet<VRCExpressionsMenu>();

                    var keysCache = new Dictionary<VRCExpressionsMenu.Control, string>();
                    var objIdCache = new Dictionary<VRCExpressionsMenu.Control, string>();
                    if (debugLog) Debug.Log("[MenuManagerPlugin] Assigning control keys for root menu...");
                    AssignControlKeys(descriptor.expressionsMenu, keysCache, objIdCache, avatarRoot, new Dictionary<string, int>(), new HashSet<VRCExpressionsMenu>());

                    if (debugLog) Debug.Log("[MenuManagerPlugin] Starting ReorderMenu...");
                    ReorderMenu(descriptor.expressionsMenu, layoutData, "", visited, keysCache, objIdCache, debugLog, detailedLog, autoAddNewItemsToRoot);

                    UnityEngine.Object.DestroyImmediate(layoutData);

                    if (debugLog) Debug.Log("[MenuManagerPlugin] メニュー並び替え完了。");
                });
        }

        private static void AssignControlKeys(
            VRCExpressionsMenu menu,
            Dictionary<VRCExpressionsMenu.Control, string> keysCache,
            Dictionary<VRCExpressionsMenu.Control, string> objIdCache,
            GameObject avatarRoot,
            Dictionary<string, int> keyCounts,
            HashSet<VRCExpressionsMenu> visited
        ){
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;

            for (int i = 0; i < menu.controls.Count; i++){
                var ctrl = menu.controls[i];
                string paramName = ctrl.parameter?.name ?? "";
                string baseKey = $"{ctrl.type}:{ctrl.name}:{paramName}:{ctrl.value:F2}";
                if (!keyCounts.ContainsKey(baseKey)) keyCounts[baseKey] = 0;
                keysCache[ctrl] = baseKey + ":" + keyCounts[baseKey]++;

                string menuGid = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(menu).ToString();
                objIdCache[ctrl] = menuGid + ":__index__:" + i;

                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    AssignControlKeys(ctrl.subMenu, keysCache, objIdCache, avatarRoot, keyCounts, visited);
                }
            }
        }

        private class ParsedLayoutItem{
            public MenuLayoutData.ItemLayout Original;
            public string Key0;
            public string Key1;
            public string Key2;
            public string Key3;
        }

        private class PoolItem{
            public VRCExpressionsMenu.Control Ctrl;
            public string Key0;
            public string Key1;
            public string Key2;
            public string Key3;
        }

        private static void ReorderMenu(
            VRCExpressionsMenu rootMenu,
            MenuLayoutData layout,
            string currentPath,
            HashSet<VRCExpressionsMenu> visited,
            Dictionary<VRCExpressionsMenu.Control, string> keysCache,
            Dictionary<VRCExpressionsMenu.Control, string> objIdCache,
            bool debugLog,
            bool detailedLog,
            bool autoAddNewItemsToRoot
        ){
            if (rootMenu == null || layout == null || layout.Items.Count == 0) return;

            var parsedItems = new List<ParsedLayoutItem>(layout.Items.Count);
            foreach (var item in layout.Items){
                string mk = !string.IsNullOrEmpty(item.Type) ? item.Type : item.Key;
                string[] parts = mk.Split(new[] { ':' }, 4);
                string typeStr = parts.Length > 0 ? parts[0] : "";
                string nameStr = parts.Length > 1 ? parts[1] : item.DisplayName;

                if (!string.IsNullOrEmpty(item.SourceObjId)){
                    try{
                        string[] idParts = item.SourceObjId.Split(new[] { ":__index__:" }, System.StringSplitOptions.None);
                        if (UnityEditor.GlobalObjectId.TryParse(idParts[0], out var gid)){
                            var obj = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                            if (obj is GameObject go){
                                var menuItem = go.GetComponent<ModularAvatarMenuItem>();
                                if (menuItem != null && menuItem.Control != null && !string.IsNullOrEmpty(menuItem.Control.name)){
                                    nameStr = menuItem.Control.name;
                                    typeStr = menuItem.Control.type.ToString();
                                }
                                else {
                                    var installer = go.GetComponent<ModularAvatarMenuInstaller>();
                                    if (installer != null && installer.menuToAppend != null){
                                        nameStr = installer.menuToAppend.name;
                                        typeStr = VRCExpressionsMenu.Control.ControlType.SubMenu.ToString();
                                    }
                                }
                            }
                            else if (obj is VRCExpressionsMenu menuAsst){
                                if (idParts.Length > 1 && int.TryParse(idParts[1], out int idx)){
                                    if (idx >= 0 && idx < menuAsst.controls.Count){
                                        var c = menuAsst.controls[idx];
                                        nameStr = c.name;
                                        typeStr = c.type.ToString();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) {
                        if (detailedLog) Debug.LogWarning($"[MenuManagerPlugin] ID resolving failed for ({item.DisplayName}): {ex.Message}");
                    }
                }

                parsedItems.Add(new ParsedLayoutItem{
                    Original = item,
                    Key0 = item.SourceObjId ?? "",
                    Key1 = mk,
                    Key2 = $"{typeStr}:{nameStr}",
                    Key3 = nameStr
                });
            }

            if (!autoAddNewItemsToRoot){
                if (debugLog) Debug.Log("[MenuManagerPlugin] AutoAddNewItemsToRoot is OFF; mapping unregistered controls to virtual inventory.");
                InjectInventoryMappingsForUnregisteredControls(rootMenu, parsedItems, keysCache, objIdCache, detailedLog);
            }

            if (debugLog) Debug.Log("[MenuManagerPlugin] Flattening More menus...");
            FlattenMoreMenus(rootMenu, new HashSet<VRCExpressionsMenu>(), parsedItems, keysCache, objIdCache, detailedLog);

            var pool = new List<PoolItem>();
            if (debugLog) Debug.Log("[MenuManagerPlugin] Extracting mapped controls...");
            ExtractMappedControls(rootMenu, parsedItems, visited, pool, keysCache, objIdCache, detailedLog);
            
            if (debugLog) Debug.Log("[MenuManagerPlugin] Rebuilding menu levels...");
            RebuildMenuLevel(rootMenu, "", "", layout, pool, parsedItems, detailedLog);
            
            if (pool.Count > 0){
                foreach (var leftover in pool){
                    bool anyInventoryMatch = false;
                    ParsedLayoutItem mappedItem = FindParsedMatch(parsedItems, leftover, out anyInventoryMatch);

                    if (mappedItem == null || (!mappedItem.Original.ParentPath.StartsWith("__INVENTORY__") && !anyInventoryMatch)){
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Restoring unmapped leftover control: {leftover.Ctrl.name}");
                        rootMenu.controls.Add(leftover.Ctrl);
                    }
                    else{
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping leftover control in inventory: {leftover.Ctrl.name}");
                    }
                }
            }
        }

        private static void InjectInventoryMappingsForUnregisteredControls(
            VRCExpressionsMenu rootMenu,
            List<ParsedLayoutItem> parsedItems,
            Dictionary<VRCExpressionsMenu.Control, string> keysCache,
            Dictionary<VRCExpressionsMenu.Control, string> objIdCache,
            bool detailedLog
        ){
            if (rootMenu == null || rootMenu.controls == null) return;

            var visited = new HashSet<VRCExpressionsMenu>();
            InjectInventoryMappingsRecursive(rootMenu, parsedItems, keysCache, objIdCache, visited, detailedLog);
        }

        private static void InjectInventoryMappingsRecursive(
            VRCExpressionsMenu menu,
            List<ParsedLayoutItem> parsedItems,
            Dictionary<VRCExpressionsMenu.Control, string> keysCache,
            Dictionary<VRCExpressionsMenu.Control, string> objIdCache,
            HashSet<VRCExpressionsMenu> visited,
            bool detailedLog,
            bool parentIsManaged = false
        ){
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;

            for (int i = 0; i < menu.controls.Count; i++){
                var ctrl = menu.controls[i];
                string key0 = objIdCache.TryGetValue(ctrl, out var oid) ? oid : "";
                string key1 = keysCache.TryGetValue(ctrl, out var k) ? k : GenerateControlKey(ctrl);
                string key2 = $"{ctrl.type}:{ctrl.name}";
                string key3 = ctrl.name;

                ParsedLayoutItem match = FindParsedMatchByKeys(parsedItems, key0, key1, key2, key3);

                if (match != null){
                    if (match.Original.IsSubMenu && match.Original.IsDynamic){
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping inventory crawl for children of dynamic folder: {ctrl.name}");
                        continue;
                    }
                    
                    if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                        InjectInventoryMappingsRecursive(ctrl.subMenu, parsedItems, keysCache, objIdCache, visited, detailedLog, true);
                    }
                    continue;
                }

                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    InjectInventoryMappingsRecursive(ctrl.subMenu, parsedItems, keysCache, objIdCache, visited, detailedLog, parentIsManaged);
                }

                if (parentIsManaged){
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Preserving unregistered control in managed folder: {ctrl.name}");
                    continue;
                }

                var dummy = new MenuLayoutData.ItemLayout{
                    Type = key1,
                    Key = Guid.NewGuid().ToString("N"),
                    ParentPath = "__INVENTORY__",
                    Order = 0,
                    IsSubMenu = ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu,
                    DisplayName = ctrl.name,
                    CustomIcon = ctrl.icon,
                    IsAutoOverflow = false,
                    SourceObjId = key0
                };

                parsedItems.Add(new ParsedLayoutItem{
                    Original = dummy,
                    Key0 = key0,
                    Key1 = key1,
                    Key2 = key2,
                    Key3 = key3
                });

                if (detailedLog) Debug.Log($"[MenuManagerPlugin] Treating unregistered control as inventory-only: {ctrl.name}");
            }
        }

        private static void MoveUnmappedControlsToRoot(
            VRCExpressionsMenu rootMenu,
            List<ParsedLayoutItem> parsedItems,
            Dictionary<VRCExpressionsMenu.Control, string> keysCache,
            bool detailedLog
        ){
            if (rootMenu == null || rootMenu.controls == null) return;

            var visited = new HashSet<VRCExpressionsMenu>();
            var unmapped = new List<VRCExpressionsMenu.Control>();

            CollectUnmappedControls(rootMenu, parsedItems, keysCache, visited, unmapped, detailedLog, true);

            if (unmapped.Count > 0){
                if (detailedLog) Debug.Log($"[MenuManagerPlugin] Moving {unmapped.Count} unmapped controls to root.");
                foreach (var ctrl in unmapped){
                    rootMenu.controls.Add(ctrl);
                }
            }
        }

        private static void CollectUnmappedControls(
            VRCExpressionsMenu menu,
            List<ParsedLayoutItem> parsedItems,
            Dictionary<VRCExpressionsMenu.Control, string> keysCache,
            HashSet<VRCExpressionsMenu> visited,
            List<VRCExpressionsMenu.Control> unmapped,
            bool detailedLog,
            bool isRoot
        ){
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;

            for (int i = menu.controls.Count - 1; i >= 0; i--){
                var ctrl = menu.controls[i];

                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    CollectUnmappedControls(ctrl.subMenu, parsedItems, keysCache, visited, unmapped, detailedLog, false);
                }

                if (isRoot) continue;

                string key1 = keysCache.TryGetValue(ctrl, out var k) ? k : GenerateControlKey(ctrl);
                string key2 = $"{ctrl.type}:{ctrl.name}";
                string key3 = ctrl.name;

                ParsedLayoutItem match = FindParsedMatchByKeys(parsedItems, "", key1, key2, key3);

                if (match == null){
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Detected new unmapped control '{ctrl.name}' in submenu '{menu.name}', moving to root.");
                    menu.controls.RemoveAt(i);
                    unmapped.Add(ctrl);
                }
            }
        }

        private static void ExtractMappedControls(VRCExpressionsMenu menu, List<ParsedLayoutItem> parsedItems, HashSet<VRCExpressionsMenu> visited, List<PoolItem> pool, Dictionary<VRCExpressionsMenu.Control, string> keysCache, Dictionary<VRCExpressionsMenu.Control, string> objIdCache, bool detailedLog){
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;

            for (int i = menu.controls.Count - 1; i >= 0; i--){
                var ctrl = menu.controls[i];

                string key0 = objIdCache.TryGetValue(ctrl, out var oid) ? oid : "";
                string key1 = keysCache.TryGetValue(ctrl, out var k) ? k : GenerateControlKey(ctrl);
                string key2 = $"{ctrl.type}:{ctrl.name}";
                string key3 = ctrl.name;

                ParsedLayoutItem match = FindParsedMatchByKeys(parsedItems, key0, key1, key2, key3);

                if (match != null){
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Extracted to pool: {ctrl.name} (Matched Key: {match.Original.Key}, SourceObjId: {(key0.Length > 0 ? "YES" : "fallback")})");
                    menu.controls.RemoveAt(i);
                    pool.Add(new PoolItem { Ctrl = ctrl, Key0 = key0, Key1 = key1, Key2 = key2, Key3 = key3 });

                    if (match.Original.IsSubMenu && match.Original.IsDynamic){
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping extraction for children of dynamic folder: {ctrl.name}");
                        continue;
                    }
                }

                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    ExtractMappedControls(ctrl.subMenu, parsedItems, visited, pool, keysCache, objIdCache, detailedLog);
                }
            }
        }

        private static void FlattenMoreMenus(VRCExpressionsMenu menu, HashSet<VRCExpressionsMenu> visited, List<ParsedLayoutItem> parsedItems, Dictionary<VRCExpressionsMenu.Control, string> keysCache, Dictionary<VRCExpressionsMenu.Control, string> objIdCache, bool detailedLog){
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;

            for (int i = menu.controls.Count - 1; i >= 0; i--){
                var ctrl = menu.controls[i];
                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    string key0 = objIdCache.TryGetValue(ctrl, out var oid) ? oid : "";
                    string key1 = keysCache.TryGetValue(ctrl, out var k) ? k : GenerateControlKey(ctrl);
                    string key2 = $"{ctrl.type}:{ctrl.name}";
                    string key3 = ctrl.name;

                    ParsedLayoutItem match = FindParsedMatchByKeys(parsedItems, key0, key1, key2, key3);

                    if (match != null && match.Original.IsDynamic){
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping FlattenMoreMenus for children of dynamic folder: {ctrl.name}");
                        continue;
                    }

                    FlattenMoreMenus(ctrl.subMenu, visited, parsedItems, keysCache, objIdCache, detailedLog);

                    string n = ctrl.name ?? "";
                    if (n == "Next" || n == "More" || n == "…(More)" || n == "... (More)" || n == "..." || n.EndsWith("More)")){
                        if (match == null || (match.Original.IsAutoOverflow && !match.Original.IsDynamic)){
                            if (detailedLog) Debug.Log($"[MenuManagerPlugin] Flattening More folder: {ctrl.name}");
                            menu.controls.RemoveAt(i);
                            if (ctrl.subMenu.controls != null){
                                menu.controls.InsertRange(i, ctrl.subMenu.controls);
                            }
                        }
                    }
                }
            }
        }

        private static void RebuildMenuLevel(VRCExpressionsMenu currentMenu, string currentPath, string legacyPath, MenuLayoutData layout, List<PoolItem> pool, List<ParsedLayoutItem> parsedItems, bool detailedLog){
            var itemsInPath = layout.Items
                .Where(item => item.ParentPath == currentPath || (!string.IsNullOrEmpty(legacyPath) && item.ParentPath == legacyPath))
                .OrderBy(item => item.Order)
                .ToList();

            if (itemsInPath.Count == 0) return;

            var newControls = new List<VRCExpressionsMenu.Control>();

            foreach (var item in itemsInPath){
                var parsed = parsedItems.Find(p => p.Original == item);
                var ctrl = FetchFromPool(pool, parsed);

                if (ctrl != null){
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Placed existing control: {ctrl.name} at path: '{currentPath}'");
                    if (item.IsSubMenu){
                        if (ctrl.subMenu == null){
                            if (detailedLog) Debug.Log($"[MenuManagerPlugin] SubMenu reference missing for {ctrl.name}, creating new instance.");
                            ctrl.subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                            ctrl.subMenu.name = item.DisplayName ?? ctrl.name;
                        }

                        if (item.IsDynamic){
                            if (detailedLog) Debug.Log($"[MenuManagerPlugin] Dynamic folder detected: {ctrl.name}. Keeping internal structure.");
                        }
                        else{
                            string subPath = string.IsNullOrEmpty(currentPath) ? item.Key : currentPath + "/" + item.Key;
                            string legPath = string.IsNullOrEmpty(legacyPath) ? (item.DisplayName ?? ctrl.name) : legacyPath + "/" + (item.DisplayName ?? ctrl.name);
                            RebuildMenuLevel(ctrl.subMenu, subPath, legPath, layout, pool, parsedItems, detailedLog);
                        }
                    }
                    
                    if (item.CustomIcon != null){
                        ctrl.icon = item.CustomIcon;
                    }

                    if (!string.IsNullOrEmpty(item.DisplayName)) {
                        ctrl.name = item.DisplayName;
                    }

                    if (item.Key == "__BUILD_TIME__" || (!string.IsNullOrEmpty(item.Type) && item.Type == "__BUILD_TIME__")){
                        ctrl.name = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                    }
                    
                    newControls.Add(ctrl);
                }
                else if (item.IsSubMenu){
                    string tk = !string.IsNullOrEmpty(item.Type) ? item.Type : "";
                    bool isCustom = tk.Contains(":__custom__:");
                    bool isOverflow = item.IsAutoOverflow;
                    if (isCustom || isOverflow){
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Creating {(isCustom ? "custom folder" : "overflow folder")}: {item.DisplayName} at path: '{currentPath}'");
                        var virtualSub = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                        virtualSub.name = item.DisplayName;
                        var virtualCtrl = new VRCExpressionsMenu.Control{
                            name = item.DisplayName,
                            type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = virtualSub,
                            icon = item.CustomIcon
                        };
                        string subPath = string.IsNullOrEmpty(currentPath) ? item.Key : currentPath + "/" + item.Key;
                        string legPath = string.IsNullOrEmpty(legacyPath) ? item.DisplayName : legacyPath + "/" + item.DisplayName;
                        RebuildMenuLevel(virtualSub, subPath, legPath, layout, pool, parsedItems, detailedLog);
                        newControls.Add(virtualCtrl);
                    }
                    else{
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping deleted menu entry: {item.DisplayName} at path: '{currentPath}'");
                    }
                }
                else if (item.Key == "__BUILD_TIME__" || (!string.IsNullOrEmpty(item.Type) && item.Type == "__BUILD_TIME__")){
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Inserting build time control at path: '{currentPath}'");
                    var virtualCtrl = new VRCExpressionsMenu.Control{
                        name = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        icon = item.CustomIcon
                    };
                    newControls.Add(virtualCtrl);
                }
            }

            if (currentMenu.controls != null){
                newControls.AddRange(currentMenu.controls);
            }
            currentMenu.controls = newControls;
        }

        private static VRCExpressionsMenu.Control FetchFromPool(List<PoolItem> pool, ParsedLayoutItem parsed){
            if (parsed == null) return null;

            int idx = -1;

            if (idx < 0 && !string.IsNullOrEmpty(parsed.Key0))
                for (int i = 0; i < pool.Count; i++) if (!string.IsNullOrEmpty(pool[i].Key0) && pool[i].Key0 == parsed.Key0) { idx = i; break; }

            if (idx < 0) for (int i = 0; i < pool.Count; i++) if (pool[i].Key1 == parsed.Key1) { idx = i; break; }

            if (idx < 0) for (int i = 0; i < pool.Count; i++) if (pool[i].Key2 == parsed.Key2) { idx = i; break; }

            if (idx < 0) for (int i = 0; i < pool.Count; i++) if (pool[i].Key3 == parsed.Key3) { idx = i; break; }

            if (idx >= 0){
                var ctrl = pool[idx];
                pool.RemoveAt(idx);
                return ctrl.Ctrl;
            }
            return null;
        }

        private static ParsedLayoutItem FindParsedMatch(List<ParsedLayoutItem> parsedItems, PoolItem leftover, out bool anyInventoryMatch){
            anyInventoryMatch = false;
            ParsedLayoutItem result = null;

            if (!string.IsNullOrEmpty(leftover.Key0)){
                for (int j = 0; j < parsedItems.Count; j++){
                    if (!string.IsNullOrEmpty(parsedItems[j].Key0) && parsedItems[j].Key0 == leftover.Key0){
                        result = parsedItems[j];
                        if (parsedItems[j].Original.ParentPath.StartsWith("__INVENTORY__")) anyInventoryMatch = true;
                        return result;
                    }
                }
            }

            for (int j = 0; j < parsedItems.Count; j++){
                if (parsedItems[j].Key1 == leftover.Key1){
                    if (result == null) result = parsedItems[j];
                    if (parsedItems[j].Original.ParentPath.StartsWith("__INVENTORY__")) anyInventoryMatch = true;
                }
            }
            if (result != null) return result;

            for (int j = 0; j < parsedItems.Count; j++){
                if (parsedItems[j].Key2 == leftover.Key2){
                    if (result == null) result = parsedItems[j];
                    if (parsedItems[j].Original.ParentPath.StartsWith("__INVENTORY__")) anyInventoryMatch = true;
                }
            }
            if (result != null) return result;

            for (int j = 0; j < parsedItems.Count; j++){
                if (parsedItems[j].Key3 == leftover.Key3){
                    if (result == null) result = parsedItems[j];
                    if (parsedItems[j].Original.ParentPath.StartsWith("__INVENTORY__")) anyInventoryMatch = true;
                }
            }
            return result;
        }

        private static ParsedLayoutItem FindParsedMatchByKeys(List<ParsedLayoutItem> parsedItems, string key0, string key1, string key2, string key3){
            if (!string.IsNullOrEmpty(key0)){
                for (int j = 0; j < parsedItems.Count; j++)
                    if (!string.IsNullOrEmpty(parsedItems[j].Key0) && parsedItems[j].Key0 == key0) return parsedItems[j];
            }

            for (int j = 0; j < parsedItems.Count; j++)
                if (parsedItems[j].Key1 == key1) return parsedItems[j];

            for (int j = 0; j < parsedItems.Count; j++)
                if (parsedItems[j].Key2 == key2) return parsedItems[j];

            for (int j = 0; j < parsedItems.Count; j++)
                if (parsedItems[j].Key3 == key3) return parsedItems[j];

            return null;
        }

        public static string GenerateControlKey(VRCExpressionsMenu.Control ctrl){
            string paramName = ctrl.parameter?.name ?? "";
            return $"{ctrl.type}:{ctrl.name}:{paramName}:{ctrl.value:F2}";
        }
    }
}

#endif
