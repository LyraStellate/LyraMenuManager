#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Lyra.Editor{
    public class MenuManagerDataManager : EditorWindow{
        private const string DATA_FOLDER = "Assets/Lyra/EditorTools/MenuManager/Data";

        private Vector2 _scrollPos;

        private List<AssetInfo> _unreferencedAssets;
        private bool _unreferencedScanned = false;

        private MenuLayoutDataAsset _searchTarget;
        private List<AvatarRefInfo> _referencingAvatars;

        private struct AssetInfo {
            public string Path;
            public MenuLayoutDataAsset Asset;
            public bool Selected;
        }

        private struct AvatarRefInfo {
            public VRCAvatarDescriptor Avatar;
            public string Role;
        }

        public static void ShowWindow(){
            var w = GetWindow<MenuManagerDataManager>("データ管理", true);
            w.minSize = new Vector2(400, 350);
            w.Show();
        }

        private void OnGUI(){
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Menu Manager データ管理", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("未参照アセットのクリーンアップ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "どのアバターからも参照されていない MenuLayoutDataAsset を検出します。\n" +
                "不要なアセットを選択して削除できます。",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("スキャン", GUILayout.Height(26))){
                ScanUnreferencedAssets();
            }

            if (_unreferencedScanned){
                if (_unreferencedAssets == null || _unreferencedAssets.Count == 0){
                    EditorGUILayout.HelpBox("未参照のアセットはありません。", MessageType.None);
                }
                else{
                    EditorGUILayout.LabelField($"{_unreferencedAssets.Count} 件の未参照アセット:");
                    EditorGUILayout.Space(2);

                    for (int i = 0; i < _unreferencedAssets.Count; i++){
                        var info = _unreferencedAssets[i];
                        EditorGUILayout.BeginHorizontal();
                        info.Selected = EditorGUILayout.ToggleLeft("", info.Selected, GUILayout.Width(16));
                        _unreferencedAssets[i] = info;

                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(info.Asset, typeof(MenuLayoutDataAsset), false);
                        EditorGUI.EndDisabledGroup();

                        int itemCount = info.Asset != null && info.Asset.Items != null ? info.Asset.Items.Count : 0;
                        EditorGUILayout.LabelField($"({itemCount} items)", EditorStyles.miniLabel, GUILayout.Width(70));

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(6);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("すべて選択", GUILayout.Width(90))){
                        for (int i = 0; i < _unreferencedAssets.Count; i++){
                            var a = _unreferencedAssets[i];
                            a.Selected = true;
                            _unreferencedAssets[i] = a;
                        }
                    }
                    if (GUILayout.Button("すべて解除", GUILayout.Width(90))){
                        for (int i = 0; i < _unreferencedAssets.Count; i++){
                            var a = _unreferencedAssets[i];
                            a.Selected = false;
                            _unreferencedAssets[i] = a;
                        }
                    }

                    GUILayout.FlexibleSpace();

                    int selectedCount = _unreferencedAssets.Count(a => a.Selected);
                    GUI.enabled = selectedCount > 0;
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button($"削除 ({selectedCount})", GUILayout.Width(100), GUILayout.Height(24))){
                        if (EditorUtility.DisplayDialog("確認",
                            $"選択した {selectedCount} 件のアセットを削除しますか？\nこの操作は取り消せません。",
                            "削除", "キャンセル")){
                            DeleteSelectedAssets();
                        }
                    }
                    GUI.backgroundColor = prevBg;
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(16);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), new Color(0.3f, 0.3f, 0.35f, 0.5f));
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("アセット参照元の検索", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "指定した MenuLayoutDataAsset がどのアバターから参照されているかを検索します。",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            _searchTarget = (MenuLayoutDataAsset)EditorGUILayout.ObjectField(
                "検索対象", _searchTarget, typeof(MenuLayoutDataAsset), false);
            
            GUI.enabled = _searchTarget != null;
            if (GUILayout.Button("検索", GUILayout.Width(60), GUILayout.Height(18))){
                SearchReferencingAvatars();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (_referencingAvatars != null){
                EditorGUILayout.Space(4);
                if (_referencingAvatars.Count == 0){
                    EditorGUILayout.HelpBox("このアセットを参照しているアバターはありません。", MessageType.None);
                }
                else{
                    EditorGUILayout.LabelField($"{_referencingAvatars.Count} 件のアバターが参照中:");
                    foreach (var r in _referencingAvatars){
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(r.Avatar, typeof(VRCAvatarDescriptor), true);
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.LabelField(r.Role, EditorStyles.miniLabel, GUILayout.Width(70));
                        if (GUILayout.Button("選択", GUILayout.Width(40))){
                            Selection.activeGameObject = r.Avatar.gameObject;
                            EditorGUIUtility.PingObject(r.Avatar.gameObject);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("閉じる", GUILayout.Width(100), GUILayout.Height(24))){
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
        }

        private void ScanUnreferencedAssets(){
            _unreferencedAssets = new List<AssetInfo>();
            _unreferencedScanned = true;

            var guids = AssetDatabase.FindAssets("t:MenuLayoutDataAsset", new[]{ DATA_FOLDER });
            if (guids.Length == 0) return;

            var allAssets = new Dictionary<MenuLayoutDataAsset, string>();
            foreach (var guid in guids){
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MenuLayoutDataAsset>(path);
                if (asset != null) allAssets[asset] = path;
            }

            var referenced = new HashSet<MenuLayoutDataAsset>();
            var allLayoutData = FindObjectsOfType<MenuLayoutData>(true);
            foreach (var ld in allLayoutData){
                if (ld.BaseLayout != null) referenced.Add(ld.BaseLayout);
                if (ld.ExtendedLayout != null) referenced.Add(ld.ExtendedLayout);
            }

            foreach (var kv in allAssets){
                if (!referenced.Contains(kv.Key)){
                    _unreferencedAssets.Add(new AssetInfo{
                        Path = kv.Value,
                        Asset = kv.Key,
                        Selected = false
                    });
                }
            }
        }

        private void DeleteSelectedAssets(){
            var toDelete = _unreferencedAssets.Where(a => a.Selected).ToList();
            foreach (var info in toDelete){
                AssetDatabase.DeleteAsset(info.Path);
            }
            AssetDatabase.Refresh();
            _unreferencedAssets.RemoveAll(a => a.Selected);
            if (_unreferencedAssets.Count == 0) _unreferencedAssets = null;
        }

        private void SearchReferencingAvatars(){
            _referencingAvatars = new List<AvatarRefInfo>();
            if (_searchTarget == null) return;

            var allLayoutData = FindObjectsOfType<MenuLayoutData>(true);
            foreach (var ld in allLayoutData){
                var avatar = ld.GetComponent<VRCAvatarDescriptor>();
                if (avatar == null) continue;

                if (ld.BaseLayout == _searchTarget){
                    _referencingAvatars.Add(new AvatarRefInfo{ Avatar = avatar, Role = "Base" });
                }
                else if (ld.ExtendedLayout == _searchTarget){
                    _referencingAvatars.Add(new AvatarRefInfo{ Avatar = avatar, Role = "Extended" });
                }
            }
        }
    }
}

#endif
