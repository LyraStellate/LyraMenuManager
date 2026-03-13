#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Lyra;

namespace Lyra.Editor{
    public partial class MenuManager{
        private void SaveLayout(bool silent = false){
            if (_avatar == null || _rootNode == null){
                if (!silent) EditorUtility.DisplayDialog("Menu Manager",
                    "アバターまたはメニューが読み込まれていません。", "OK");
                return;
            }

            var layoutData = _avatar.GetComponent<MenuLayoutData>();
            if (layoutData == null){
                Undo.AddComponent<MenuLayoutData>(_avatar.gameObject);
                layoutData = _avatar.GetComponent<MenuLayoutData>();
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var allItems = new List<MenuLayoutData.ItemLayout>();

            var flattenedEntries = FlattenMoreMenus(_rootNode.Entries);
            var rootDummy = new MenuNode { Entries = flattenedEntries };
            SerializeNode(rootDummy, "", allItems);

            var flattenedInventory = FlattenMoreMenus(_inventory);
            var invDummy = new MenuNode { Entries = flattenedInventory };
            SerializeNode(invDummy, "__INVENTORY__", allItems);

            if (layoutData.BaseLayout == null){
                layoutData.BaseLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Base");
            }
            else if (layoutData.ExtendedLayout == null && !silent) {
                int choice = EditorUtility.DisplayDialogComplex("保存方法の確認",
                    "Baseレイアウトが既に存在します。どのように保存しますか？\n\n",
                    "上書き保存", "キャンセル", "その他の保存...");

                if (choice == 1) return;

                if (choice == 2) {
                    int subChoice = EditorUtility.DisplayDialogComplex("その他の保存方法",
                        "保存方法を選択してください。",
                        "新規保存", "キャンセル", "差分のみ保存");
                    
                    if (subChoice == 1) return;
                    
                    if (subChoice == 0) {
                        layoutData.BaseLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Base_" + DateTime.Now.ToString("MMddHHmm"));
                        layoutData.ExtendedLayout = null;
                    } 
                    else if (subChoice == 2) {
                        if (!MenuManagerAuthGuard.CanUseExtended()){
                            EditorUtility.DisplayDialog("Menu Manager",
                                "差分保存はPro版限定機能です。", "OK");
                            return;
                        }
                        if (layoutData.ExtendedLayout == null) {
                            layoutData.ExtendedLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Extended");
                        }
                    }
                }
            }
            MenuManagerAuthGuard.GuardedSaveLayout(layoutData.BaseLayout, layoutData.ExtendedLayout, allItems);

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
                    $"※ ビルド時にプラグインが自動でメニューを並び替えます。",
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
                    IsSubMenu = entry.Type == VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    DisplayName = entry.Name ?? "(no name)",
                    CustomIcon = entry.Icon,
                    IsAutoOverflow = entry.IsAutoOverflow,
                    IsDynamic = entry.IsDynamic,
                    SourceObjId = GetSourceObjId(entry)
                };
                items.Add(item);

                if (entry.SubMenu != null){
                    string subPath = string.IsNullOrEmpty(currentPath)
                        ? item.Key
                        : currentPath + "/" + item.Key;
                    SerializeNode(entry.SubMenu, subPath, items);
                }
            }
        }

        private string GetSourceObjId(MenuEntry entry){
            if (entry.SourceMenuItem != null) return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(entry.SourceMenuItem).ToString();
            if (entry.SourceAsset != null) return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(entry.SourceAsset).ToString() + ":__index__:" + entry.SourceIndex;
            if (entry.SourceInstaller != null) return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(entry.SourceInstaller).ToString();
            return "";
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
    }
}

#endif
