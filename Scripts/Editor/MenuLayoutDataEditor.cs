#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRC.SDK3.Avatars.Components;
using Lyra.Editor;

namespace Lyra{
    [CustomEditor(typeof(MenuLayoutData))]
    public class MenuLayoutDataEditor : UnityEditor.Editor{
        private const string VERSION = "0.5.0";
        
        private const string PREF_KEY_HEADER = "Lyra.MenuManager.Inspector.Header";
        private const string PREF_KEY_SETTINGS = "Lyra.MenuManager.Inspector.Settings";
        private const string PREF_KEY_DEBUG = "Lyra.MenuManager.Inspector.Debug";
        private const string PREF_KEY_ADVANCED = "Lyra.MenuManager.Inspector.Advanced";

        private bool _showHeader;
        private bool _showSettings;
        private bool _showDebug;
        private bool _showAdvanced;
        private GameObject _draggedObject;

        private static readonly Color AccentColor = new Color(0.25f, 0.58f, 0.82f, 1f);

        private void OnEnable(){
            _showHeader = EditorPrefs.GetBool(PREF_KEY_HEADER, false);
            _showSettings = EditorPrefs.GetBool(PREF_KEY_SETTINGS, true);
            _showDebug = EditorPrefs.GetBool(PREF_KEY_DEBUG, false);
            _showAdvanced = EditorPrefs.GetBool(PREF_KEY_ADVANCED, false);
        }

        private void OnDisable(){
            EditorPrefs.SetBool(PREF_KEY_HEADER, _showHeader);
            EditorPrefs.SetBool(PREF_KEY_SETTINGS, _showSettings);
            EditorPrefs.SetBool(PREF_KEY_DEBUG, _showDebug);
            EditorPrefs.SetBool(PREF_KEY_ADVANCED, _showAdvanced);
        }

        public override void OnInspectorGUI(){
            serializedObject.Update();
            var data = (MenuLayoutData)target;
            bool isJa = nadena.dev.ndmf.localization.LanguagePrefs.Language.StartsWith("ja");

            EditorGUILayout.Space(2);

            EditorGUILayout.Space(2);

            var titleStyle = new GUIStyle(EditorStyles.foldout){
                fontStyle = FontStyle.Bold,
                fontSize = 13,
            };
            
            _showHeader = EditorGUILayout.Foldout(_showHeader, $"Lyra Menu Manager v{VERSION}", true, titleStyle);
            if (_showHeader){
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(4);

                var linkStyle = new GUIStyle(EditorStyles.linkLabel){
                    fontSize = 13,
                    richText = true,
                    margin = new RectOffset(0, 0, 1, 1), 
                    imagePosition = ImagePosition.ImageLeft
                };

                string url1 = "https://docs.lyrastellate.dev/menu-manager/";
                GUIContent content1 = new GUIContent(
                    isJa ? " オンラインマニュアル" : " Online Manual", 
                    EditorGUIUtility.IconContent("_Help").image, 
                    url1);
                if (GUILayout.Button(content1, linkStyle)){
                    Application.OpenURL(url1);
                }
                
                string url2 = "https://example.com";
                GUIContent content2 = new GUIContent(
                    isJa ? " サポートページ / GitHub" : " Support / GitHub", 
                    EditorGUIUtility.IconContent("BuildSettings.Web.Small").image, 
                    url2);
                if (GUILayout.Button(content2, linkStyle)){
                    Application.OpenURL(url2);
                }
                
                string url3 = "https://example.com";
                GUIContent content3 = new GUIContent(
                    isJa ? " 開発者を支援する" : " Support the Developer", 
                    EditorGUIUtility.IconContent("Favorite").image, 
                    url3);
                if (GUILayout.Button(content3, linkStyle)){
                    Application.OpenURL(url3);
                }
                
                EditorGUILayout.Space(4);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            string helpText = isJa
                ? "このコンポーネントはMenu Managerによって追加されました。\n" +
                  "メニューの並び替えレイアウトデータを保持しています。ビルド時にアバターへ適用し破棄されます。\n" +
                  "Menu Managerによるレイアウトを使用しない場合、設定から無効化するか、このコンポーネントを削除してください。"
                : "This component was added by Menu Manager.\n" +
                  "This holds the menu sorting layout data. It is applied to the avatar at build time and then discarded.\n" +
                  "If you do not use the Menu Manager layout, disable it in the settings or delete this component.";

            EditorGUILayout.HelpBox(helpText, MessageType.Info);

            EditorGUILayout.Space(8);

            var btnStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
            string openBtnText = isJa ? "Menu Manager を開く" : "Open Menu Manager";
            if (GUILayout.Button(openBtnText, btnStyle, GUILayout.Height(30))){
                var avatar = data.GetComponent<VRCAvatarDescriptor>()
                          ?? data.GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                    Lyra.Editor.MenuManager.ShowWindow(avatar);
                else
                    Lyra.Editor.MenuManager.ShowWindow();
            }

            EditorGUILayout.Space(12);

            var settingsStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            _showSettings = EditorGUILayout.Foldout(_showSettings, isJa ? "設定" : "Settings", true, settingsStyle);
            if (_showSettings){
                EditorGUI.indentLevel++;

                var isEnabledProp = serializedObject.FindProperty("IsEnabled");
                GUIContent enableLabel = isJa 
                    ? new GUIContent("有効", "オフにするとビルド時の自動並び替え処理を実行しません")
                    : new GUIContent("Enable", "If disabled, automatic menu reordering at build time will be skipped");
                var boldToggleStyle = new GUIStyle(EditorStyles.label);
                isEnabledProp.boolValue = EditorGUILayout.ToggleLeft(enableLabel, isEnabledProp.boolValue, boldToggleStyle);

                GUI.enabled = isEnabledProp.boolValue;

                var removeEmptyProp = serializedObject.FindProperty("RemoveEmptyFolders");
                GUIContent removeEmptyLabel = isJa
                    ? new GUIContent("空フォルダを削除", "ビルド時に中身のないサブメニューフォルダを自動的に除外します")
                    : new GUIContent("Remove Empty Folders", "Automatically removes empty submenu folders during build");
                removeEmptyProp.boolValue = EditorGUILayout.ToggleLeft(removeEmptyLabel, removeEmptyProp.boolValue);

                GUI.enabled = true;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            var advancedStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, isJa ? "高度な設定" : "Advanced", true, advancedStyle);
            if (_showAdvanced){
                EditorGUI.indentLevel++;

                string advHelp = isJa
                    ? "NDMF の .after 設定は、Menu Manager Settings ウィンドウで\n" +
                      "プロジェクト共通設定として管理されるようになりました。"
                    : ".after ordering is now managed as a project-wide setting\n" +
                      "from the Menu Manager Settings window.";
                EditorGUILayout.HelpBox(advHelp, MessageType.Info);
                EditorGUILayout.Space(4);

                if (GUILayout.Button(isJa ? "Menu Manager Settings を開く" : "Open Menu Manager Settings", GUILayout.Height(24))){
                    Lyra.Editor.MenuManagerSettings.ShowWindow();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);
            if (_showDebug){
                EditorGUI.indentLevel++;
                
                var debugLogProp = serializedObject.FindProperty("EnableDebugLog");
                GUIContent debugLogLabel = isJa 
                    ? new GUIContent("デバッグログを出力", "ビルド時の処理を詳細にコンソールへ出力します")
                    : new GUIContent("Enable Debug Log", "Outputs processing logs to console during build");
                EditorGUILayout.PropertyField(debugLogProp, debugLogLabel);

                if (debugLogProp.boolValue){
                    EditorGUI.indentLevel++;
                    var detailedDebugLogProp = serializedObject.FindProperty("EnableDetailedDebugLog");
                    GUIContent detailedDebugLogLabel = isJa 
                        ? new GUIContent("詳細デバッグ", "アイテム個別の処理状況まで詳細に出力します（重いです）")
                        : new GUIContent("Detailed Debug", "Outputs per-item processing details (Can be heavy)");
                    EditorGUILayout.PropertyField(detailedDebugLogProp, detailedDebugLogLabel);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(6);

                var items = data.Items;
                int total = items?.Count ?? 0;

                if (total > 0){
                    int menuItems = items.Count(i => !i.ParentPath.StartsWith("__INVENTORY__"));
                    int invItems = total - menuItems;
                    int subMenus = items.Count(i => i.IsSubMenu);
                    int icons = items.Count(i => i.CustomIcon != null);
                    int rootItems = items.Count(i => string.IsNullOrEmpty(i.ParentPath));
                    
                    int maxDepth = 0;
                    foreach (var item in items){
                        if (string.IsNullOrEmpty(item.ParentPath) || item.ParentPath.StartsWith("__INVENTORY__"))
                            continue;
                        int d = item.ParentPath.Split('/').Length;
                        if (d > maxDepth) maxDepth = d;
                    }

                    DrawRow(isJa ? "保存アイテム総数" : "Total Items Saved", $"{total}");
                    DrawRow(isJa ? "  メニュー / インベントリ" : "  Menus / Inventory", $"{menuItems} / {invItems}");
                    DrawRow(isJa ? "サブメニュー数" : "SubMenus", $"{subMenus}");
                    DrawRow(isJa ? "カスタムアイコン" : "Custom Icons", $"{icons}");
                    DrawRow(isJa ? "ルート階層の数" : "Root Items", $"{rootItems}");
                    DrawRow(isJa ? "最大階層の深さ" : "Max Hierarchy Depth", $"{maxDepth + 1}");
                }
                else{
                    EditorGUILayout.LabelField(isJa ? "保存されたデータはありません。" : "No data saved.");
                }

                EditorGUILayout.Space(2);
                string saveTxt = string.IsNullOrEmpty(data.LastSavedAt) ? (isJa ? "未保存" : "N/A") : data.LastSavedAt;
                DrawRow(isJa ? "最終保存日時" : "Last Saved At", saveTxt);

                EditorGUILayout.Space(6);
                EditorGUILayout.Space(6);
                var itemsProp = serializedObject.FindProperty("Items");
                if (itemsProp != null){
                    EditorGUILayout.PropertyField(itemsProp, new GUIContent(isJa ? "メニューデータ" : "Raw Items Data"), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(12);

            nadena.dev.ndmf.ui.LanguageSwitcher.DrawImmediate();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRow(string label, string value){
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }

        private static void AddAfterPlugin(SerializedProperty listProp, string qualifiedName){
            for (int i = 0; i < listProp.arraySize; i++)
                if (listProp.GetArrayElementAtIndex(i).stringValue == qualifiedName) return;
            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).stringValue = qualifiedName;
        }

        private struct PluginEntry { public string QualifiedName; public string DisplayName; }

        private static List<PluginEntry> FindNDMFPluginsInAssembly(Assembly assembly){
            var result = new List<PluginEntry>();
            try {
                var attrs = assembly.GetCustomAttributes(typeof(nadena.dev.ndmf.ExportsPlugin), false);
                foreach (nadena.dev.ndmf.ExportsPlugin attr in attrs){
                    try {
                        var pluginType = attr.PluginType;
                        if (pluginType.FullName == typeof(MenuManagerPlugin).FullName) continue;
                        var instanceProp = pluginType.GetProperty("Instance",
                            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                        object instance = null;
                        if (instanceProp != null){
                            try { instance = instanceProp.GetValue(null); } catch { }
                        }
                        if (instance == null){
                            try { instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(pluginType); } catch { }
                        }
                        if (instance == null) continue;
                        var qnProp = pluginType.GetProperty("QualifiedName");
                        var dnProp = pluginType.GetProperty("DisplayName");
                        string qn = qnProp?.GetValue(instance) as string;
                        string dn = dnProp?.GetValue(instance) as string;
                        if (!string.IsNullOrEmpty(qn))
                            result.Add(new PluginEntry { QualifiedName = qn, DisplayName = dn ?? qn });
                    } catch { }
                }
            } catch { }
            return result;
        }

        private static List<PluginEntry> FindAllNDMFPlugins(){
            var result = new List<PluginEntry>();
            var seen = new HashSet<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()){
                try {
                    var plugins = FindNDMFPluginsInAssembly(assembly);
                    foreach (var p in plugins){
                        if (seen.Add(p.QualifiedName))
                            result.Add(p);
                    }
                } catch { }
            }
            result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
            return result;
        }

        private static string GetPluginDisplayLabel(string qualifiedName){
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()){
                try {
                    var plugins = FindNDMFPluginsInAssembly(assembly);
                    foreach (var p in plugins)
                        if (p.QualifiedName == qualifiedName)
                            return $"{p.DisplayName} ({qualifiedName})";
                } catch { }
            }
            return qualifiedName;
        }
    }
}

#endif