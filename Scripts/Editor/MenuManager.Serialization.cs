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

            if (_isMoveMode){
                if (!silent) EditorUtility.DisplayDialog("Menu Manager",
                    "移動モード中は保存できません。\n貼り付けまたはキャンセルしてください。", "OK");
                return;
            }

            var layoutData = _avatar.GetComponent<MenuLayoutData>();
            if (layoutData == null){
                Undo.AddComponent<MenuLayoutData>(_avatar.gameObject);
                layoutData = _avatar.GetComponent<MenuLayoutData>();
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var allItems = new List<MenuLayoutData.ItemLayout>();

            SerializeNode(_rootNode, "", allItems);

            var invDummy = new MenuNode();
            invDummy.Entries.AddRange(_inventory);
            SerializeNode(invDummy, "__INVENTORY__", allItems);

            if (layoutData.BaseLayout == null){
                layoutData.BaseLayout = CreateNewLayoutAsset(_avatar.gameObject.name + "_Base");
            }
            MenuManagerAuth.SaveLayoutData(layoutData.BaseLayout, layoutData.ExtendedLayout, allItems);

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
                    $"※ 内部保存は廃止され、データはすべてアセットに保存されました。\n" +
                    $"※ ビルド時にNDMFプラグインが自動でメニューを並び替えます。",
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
            if (entry.SourceMenuItem != null) return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(entry.SourceMenuItem.gameObject).ToString();
            if (entry.SourceInstaller != null) return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(entry.SourceInstaller.gameObject).ToString();
            if (entry.SourceAsset != null) return UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(entry.SourceAsset).ToString() + ":__index__:" + entry.SourceIndex;
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
