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
                    ReorderMenu(descriptor.expressionsMenu, layoutData, "", visited, keysCache, objIdCache, debugLog, detailedLog, autoAddNewItemsToRoot, ctx.AssetContainer);

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
            if (menu == null || menu.controls == null) return;
            
            if (!visited.Add(menu)) return;

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
        }

        private class PoolItem{
            public VRCExpressionsMenu.Control Ctrl;
            public string Key0;
            public string Key1;
            public string Key2;
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
            bool autoAddNewItemsToRoot,
            UnityEngine.Object assetContainer
        ){
            if (rootMenu == null || layout == null || layout.Items.Count == 0) return;

            var parsedItems = new List<ParsedLayoutItem>(layout.Items.Count);
            foreach (var item in layout.Items){
                string mk = !string.IsNullOrEmpty(item.Type) ? item.Type : item.Key;
                string[] parts = mk.Split(new[] { ':' }, 5);
                string typeStr = parts.Length > 0 ? parts[0] : "";
                string nameStr = parts.Length > 1 ? parts[1] : item.DisplayName;
                
                string paramStr = parts.Length > 2 ? parts[2] : "";
                string valStr = parts.Length > 3 ? parts[3] : "1.00";
                string counterStr = parts.Length > 4 ? parts[4] : "0";

                if (!string.IsNullOrEmpty(item.SourceObjId)){
                    try{
                        string[] idParts = item.SourceObjId.Split(new[] { ":__index__:" }, System.StringSplitOptions.None);
                        if (UnityEditor.GlobalObjectId.TryParse(idParts[0], out var gid)){
                            var obj = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                            if (obj is GameObject go){
                                var menuItem = go.GetComponent<ModularAvatarMenuItem>();
                                if (menuItem != null){
                                    nameStr = string.IsNullOrEmpty(menuItem.label) ? go.name : menuItem.label;
                                    var c = menuItem.Control ?? new VRCExpressionsMenu.Control();
                                    typeStr = c.type.ToString();
                                    paramStr = c.parameter?.name ?? "";
                                    valStr = c.value.ToString("F2");
                                    mk = $"{typeStr}:{nameStr}:{paramStr}:{valStr}:{counterStr}";
                                }
                                else {
                                    var installer = go.GetComponent<ModularAvatarMenuInstaller>();
                                    if (installer != null){
                                        if (installer.menuToAppend != null){
                                            nameStr = installer.menuToAppend.name;
                                            typeStr = VRCExpressionsMenu.Control.ControlType.SubMenu.ToString();
                                        }
                                        else {
                                            nameStr = go.name;
                                            typeStr = VRCExpressionsMenu.Control.ControlType.SubMenu.ToString();
                                        }
                                    }
                                }
                            }
                            else if (obj is ModularAvatarMenuItem menuItem){
                                nameStr = string.IsNullOrEmpty(menuItem.label) ? (menuItem.gameObject != null ? menuItem.gameObject.name : nameStr) : menuItem.label;
                                var c = menuItem.Control ?? new VRCExpressionsMenu.Control();
                                typeStr = c.type.ToString();
                                paramStr = c.parameter?.name ?? "";
                                valStr = c.value.ToString("F2");
                                mk = $"{typeStr}:{nameStr}:{paramStr}:{valStr}:{counterStr}";
                            }
                            else if (obj is ModularAvatarMenuInstaller installer){
                                if (installer.menuToAppend != null){
                                    nameStr = installer.menuToAppend.name;
                                    typeStr = VRCExpressionsMenu.Control.ControlType.SubMenu.ToString();
                                }
                                else if (installer.gameObject != null){
                                    nameStr = installer.gameObject.name;
                                    typeStr = VRCExpressionsMenu.Control.ControlType.SubMenu.ToString();
                                }
                            }
                            else if (obj is VRCExpressionsMenu menuAsst){
                                if (idParts.Length > 1 && int.TryParse(idParts[1], out int idx)){
                                    if (idx >= 0 && idx < menuAsst.controls.Count){
                                        var c = menuAsst.controls[idx];
                                        nameStr = c.name;
                                        typeStr = c.type.ToString();
                                        paramStr = c.parameter?.name ?? "";
                                        valStr = c.value.ToString("F2");
                                        mk = $"{typeStr}:{nameStr}:{paramStr}:{valStr}:{counterStr}";
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
                    Key2 = $"{typeStr}:{nameStr}"
                });
            }

            if (debugLog) Debug.Log("[MenuManagerPlugin] Flattening More menus...");
            FlattenMoreMenus(rootMenu, new HashSet<VRCExpressionsMenu>(), parsedItems, keysCache, objIdCache, detailedLog);

            var pool = new List<PoolItem>();
            var consumed = new HashSet<ParsedLayoutItem>();
            
            if (debugLog) Debug.Log("[MenuManagerPlugin] Extracting mapped controls...");
            ExtractMappedControls(rootMenu, parsedItems, visited, pool, keysCache, objIdCache, consumed, detailedLog);
            
            if (debugLog) Debug.Log("[MenuManagerPlugin] Rebuilding menu levels...");
            RebuildMenuLevel(rootMenu, "", "", layout, pool, parsedItems, detailedLog, assetContainer);
            
            if (pool.Count > 0){
                foreach (var leftover in pool){
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping leftover control (acts as inventory): {leftover.Ctrl.name}");
                }
            }

            ApplyDynamicOverflowRecursive(rootMenu, new HashSet<VRCExpressionsMenu>(), assetContainer);
        }

        private static void ApplyDynamicOverflowRecursive(VRCExpressionsMenu menu, HashSet<VRCExpressionsMenu> visited, UnityEngine.Object assetContainer) {
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;
            
            ApplyDynamicOverflow(menu, assetContainer);
            
            for (int i = 0; i < menu.controls.Count; i++){
                var ctrl = menu.controls[i];
                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    ApplyDynamicOverflowRecursive(ctrl.subMenu, visited, assetContainer);
                }
            }
        }

        private static Texture2D _overflowIconCache = null;

        private static Texture2D GetOverflowIcon() {
            if (_overflowIconCache != null) return _overflowIconCache;
            string iconName = "overflow.png";
            string assetName = System.IO.Path.GetFileNameWithoutExtension(iconName);
            var guids = UnityEditor.AssetDatabase.FindAssets($"{assetName} t:Texture2D");
            foreach (var g in guids){
                string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(iconName, System.StringComparison.OrdinalIgnoreCase)){
                    _overflowIconCache = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                    if (p.Contains("Lyra")) break;
                }
            }
            return _overflowIconCache;
        }

        private static void ApplyDynamicOverflow(VRCExpressionsMenu menu, UnityEngine.Object assetContainer) {
            int max = 8;
            Texture2D overflowIcon = GetOverflowIcon();

            while (menu.controls != null && menu.controls.Count > max) {
                var overflow = menu.controls.GetRange(max - 1, menu.controls.Count - (max - 1));
                menu.controls.RemoveRange(max - 1, menu.controls.Count - (max - 1));

                var moreMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                moreMenu.name = "More";
                if (assetContainer != null) UnityEditor.AssetDatabase.AddObjectToAsset(moreMenu, assetContainer);
                moreMenu.controls = new List<VRCExpressionsMenu.Control>();
                moreMenu.controls.AddRange(overflow);

                var moreCtrl = new VRCExpressionsMenu.Control {
                    name = "More",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = moreMenu,
                    icon = overflowIcon
                };
                menu.controls.Add(moreCtrl);

                menu = moreMenu;
            }
        }

        private static void ExtractMappedControls(VRCExpressionsMenu menu, List<ParsedLayoutItem> parsedItems, HashSet<VRCExpressionsMenu> visited, List<PoolItem> pool, Dictionary<VRCExpressionsMenu.Control, string> keysCache, Dictionary<VRCExpressionsMenu.Control, string> objIdCache, HashSet<ParsedLayoutItem> consumed, bool detailedLog){
            if (menu == null || menu.controls == null || !visited.Add(menu)) return;

            for (int i = menu.controls.Count - 1; i >= 0; i--){
                var ctrl = menu.controls[i];

                string key0 = objIdCache.TryGetValue(ctrl, out var oid) ? oid : "";
                string key1 = keysCache.TryGetValue(ctrl, out var k) ? k : GenerateControlKey(ctrl);
                string key2 = $"{ctrl.type}:{ctrl.name}";

                ParsedLayoutItem match = FindParsedMatchByKeys(parsedItems, key0, key1, key2, consumed, ctrl);

                if (match != null){
                    consumed.Add(match);
                    if (detailedLog) Debug.Log($"[MenuManagerPlugin] Extracted to pool: {ctrl.name} (Matched Key: {match.Original.Key}, SourceObjId: {(key0.Length > 0 ? "YES" : "fallback")})");
                    menu.controls.RemoveAt(i);
                    pool.Add(new PoolItem { Ctrl = ctrl, Key0 = key0, Key1 = key1, Key2 = key2 });

                    if (match.Original.IsSubMenu && match.Original.IsDynamic){
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] Skipping extraction for children of dynamic folder: {ctrl.name}");
                        continue;
                    }
                }

                if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null){
                    ExtractMappedControls(ctrl.subMenu, parsedItems, visited, pool, keysCache, objIdCache, consumed, detailedLog);
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

                    ParsedLayoutItem match = FindParsedMatchByKeys(parsedItems, key0, key1, key2, null, ctrl);

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

        private static void RebuildMenuLevel(VRCExpressionsMenu currentMenu, string currentPath, string legacyPath, MenuLayoutData layout, List<PoolItem> pool, List<ParsedLayoutItem> parsedItems, bool detailedLog, UnityEngine.Object assetContainer){
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
                            if (assetContainer != null) UnityEditor.AssetDatabase.AddObjectToAsset(ctrl.subMenu, assetContainer);
                        }

                        if (item.IsDynamic){
                            if (detailedLog) Debug.Log($"[MenuManagerPlugin] Dynamic folder detected: {ctrl.name}. Keeping internal structure.");
                        }
                        else{
                            string subPath = string.IsNullOrEmpty(currentPath) ? item.Key : currentPath + "/" + item.Key;
                            string legPath = string.IsNullOrEmpty(legacyPath) ? (item.DisplayName ?? ctrl.name) : legacyPath + "/" + (item.DisplayName ?? ctrl.name);
                            RebuildMenuLevel(ctrl.subMenu, subPath, legPath, layout, pool, parsedItems, detailedLog, assetContainer);
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
                        if (assetContainer != null) UnityEditor.AssetDatabase.AddObjectToAsset(virtualSub, assetContainer);
                        var virtualCtrl = new VRCExpressionsMenu.Control{
                            name = item.DisplayName,
                            type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = virtualSub,
                            icon = item.CustomIcon
                        };
                        string subPath = string.IsNullOrEmpty(currentPath) ? item.Key : currentPath + "/" + item.Key;
                        string legPath = string.IsNullOrEmpty(legacyPath) ? item.DisplayName : legacyPath + "/" + item.DisplayName;
                        RebuildMenuLevel(virtualSub, subPath, legPath, layout, pool, parsedItems, detailedLog, assetContainer);
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
                if (string.IsNullOrEmpty(currentPath)) {
                    bool autoAddNewItemsToRoot = EditorPrefs.GetBool("Lyra.MenuManager.AutoAddNewItemsToRoot", false);
                    if (autoAddNewItemsToRoot) {
                        newControls.AddRange(currentMenu.controls);
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] autoAddNewItemsToRoot is ON. Appending {currentMenu.controls.Count} unmapped items to root.");
                    } else {
                        if (detailedLog) Debug.Log($"[MenuManagerPlugin] autoAddNewItemsToRoot is OFF. Discarding {currentMenu.controls.Count} unmapped items from root.");
                    }
                } else {
                    newControls.AddRange(currentMenu.controls);
                }
            }
            currentMenu.controls = newControls;
        }

        private static VRCExpressionsMenu.Control FetchFromPool(List<PoolItem> pool, ParsedLayoutItem parsed){
            if (parsed == null) return null;

            int idx = -1;

            if (idx < 0 && !string.IsNullOrEmpty(parsed.Key0))
                for (int i = 0; i < pool.Count; i++) if (!string.IsNullOrEmpty(pool[i].Key0) && pool[i].Key0 == parsed.Key0) { idx = i; break; }

            if (idx < 0) for (int i = 0; i < pool.Count; i++) if (pool[i].Key1 == parsed.Key1) { idx = i; break; }

            if (idx < 0) {
                int lastColon = parsed.Key1.LastIndexOf(':');
                if (lastColon > 0) {
                    string baseKey = parsed.Key1.Substring(0, lastColon);
                    for (int i = 0; i < pool.Count; i++) {
                        string pKey = pool[i].Key1;
                        int pLastColon = pKey.LastIndexOf(':');
                        if (pLastColon > 0 && pKey.Substring(0, pLastColon) == baseKey) { idx = i; break; }
                    }
                }
            }

            if (idx < 0) {
                string[] lParts = parsed.Key1.Split(':');
                if (lParts.Length >= 3) {
                    string lTypeNameParam = $"{lParts[0]}:{lParts[1]}:{lParts[2]}";
                    for (int i = 0; i < pool.Count; i++) {
                        string[] pParts = pool[i].Key1.Split(':');
                        if (pParts.Length >= 3 && $"{pParts[0]}:{pParts[1]}:{pParts[2]}" == lTypeNameParam) { idx = i; break; }
                    }
                }
            }

            if (idx < 0){
                int bestScore = -1;
                for (int i = 0; i < pool.Count; i++){
                    if (pool[i].Key2 == parsed.Key2){
                        int score = 0;
                        bool poolHasIcon = pool[i].Ctrl.icon != null;
                        bool layoutHasIcon = parsed.Original.CustomIcon != null;
                        if (poolHasIcon == layoutHasIcon) score += 1;
                        
                        string[] pParts = pool[i].Key1.Split(':');
                        string[] lParts = parsed.Key1.Split(':');
                        if (pParts.Length >= 3 && lParts.Length >= 3 && pParts[2] == lParts[2]) score += 2;

                        if (score > bestScore){
                            bestScore = score;
                            idx = i;
                        }
                    }
                }
            }

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
            return result;
        }

        private static ParsedLayoutItem FindParsedMatchByKeys(List<ParsedLayoutItem> parsedItems, string key0, string key1, string key2, HashSet<ParsedLayoutItem> consumed = null, VRCExpressionsMenu.Control sourceCtrl = null){
            if (!string.IsNullOrEmpty(key0)){
                for (int j = 0; j < parsedItems.Count; j++)
                    if (!string.IsNullOrEmpty(parsedItems[j].Key0) && parsedItems[j].Key0 == key0 && (consumed == null || !consumed.Contains(parsedItems[j]))) return parsedItems[j];
            }

            for (int j = 0; j < parsedItems.Count; j++)
                if (parsedItems[j].Key1 == key1 && (consumed == null || !consumed.Contains(parsedItems[j]))) return parsedItems[j];

            int lastColon = key1.LastIndexOf(':');
            if (lastColon > 0) {
                string baseKey = key1.Substring(0, lastColon);
                for (int j = 0; j < parsedItems.Count; j++) {
                    string pKey = parsedItems[j].Key1;
                    int pLastColon = pKey.LastIndexOf(':');
                    if (pLastColon > 0 && pKey.Substring(0, pLastColon) == baseKey && (consumed == null || !consumed.Contains(parsedItems[j]))) return parsedItems[j];
                }
            }

            string[] sParts = key1.Split(':');
            if (sParts.Length >= 3) {
                string sTypeNameParam = $"{sParts[0]}:{sParts[1]}:{sParts[2]}";
                for (int j = 0; j < parsedItems.Count; j++) {
                    string[] pParts = parsedItems[j].Key1.Split(':');
                    if (pParts.Length >= 3 && $"{pParts[0]}:{pParts[1]}:{pParts[2]}" == sTypeNameParam && (consumed == null || !consumed.Contains(parsedItems[j]))) return parsedItems[j];
                }
            }

            ParsedLayoutItem bestMatch = null;
            int bestScore = -1;
            for (int j = 0; j < parsedItems.Count; j++){
                if (parsedItems[j].Key2 == key2 && (consumed == null || !consumed.Contains(parsedItems[j]))){
                    if (sourceCtrl == null) return parsedItems[j];

                    int score = 0;
                    bool ctrlHasIcon = sourceCtrl.icon != null;
                    bool layoutHasIcon = parsedItems[j].Original.CustomIcon != null;
                    if (ctrlHasIcon == layoutHasIcon) score += 1;

                    string[] pParts = parsedItems[j].Key1.Split(':');
                    if (pParts.Length >= 3 && sParts.Length >= 3 && pParts[2] == sParts[2]) score += 2;

                    if (score > bestScore){
                        bestScore = score;
                        bestMatch = parsedItems[j];
                    }
                }
            }

            return bestMatch;
        }

        public static string GenerateControlKey(VRCExpressionsMenu.Control ctrl){
            string paramName = ctrl.parameter?.name ?? "";
            return $"{ctrl.type}:{ctrl.name}:{paramName}:{ctrl.value:F2}";
        }
    }
}

#endif
