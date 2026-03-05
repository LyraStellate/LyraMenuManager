#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lyra.Editor{
    public class MenuManagerSettings : EditorWindow{
        private Vector2 _scrollPos;
        private bool _showColorPalette = false;

        private const string PREF_KEY_ADVANCED_FOLDOUT = "Lyra.MenuManager.Settings.Advanced";
        private const string PREF_KEY_AFTER_PLUGINS = "Lyra.MenuManager.Settings.AfterPlugins";

        private bool _showAdvanced = false;
        private bool _showDebug = false;
        private GameObject _draggedObject;
        private List<string> _projectAfterPlugins = new List<string>();

        public static void ShowWindow(){
            var window = GetWindow<MenuManagerSettings>("Menu Manager Settings", true);
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(400, 800);
            window.ShowUtility();
        }

        private void OnEnable(){
            _showAdvanced = EditorPrefs.GetBool(PREF_KEY_ADVANCED_FOLDOUT, false);
            _projectAfterPlugins = LoadProjectAfterPlugins();
            MenuManagerAuth.OnAuthChanged += Repaint;
        }

        private void OnDisable(){
            EditorPrefs.SetBool(PREF_KEY_ADVANCED_FOLDOUT, _showAdvanced);
            SaveProjectAfterPlugins();
            MenuManagerAuth.OnAuthChanged -= Repaint;
        }

        private void OnGUI(){
            DrawProUnlockSection();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Menu Manager Settings", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            EditorGUILayout.Space(10);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            bool vrcStyle = EditorPrefs.GetBool("Lyra.MenuManager.VRCStyle", true);
            string btnText = vrcStyle ? "VRC Style UI : ON " : "VRC Style UI : OFF";
            
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = vrcStyle ? new Color(0.4f, 0.8f, 0.9f) : new Color(0.6f, 0.6f, 0.6f);
            
            if (GUILayout.Button(btnText, GUILayout.Height(32))){
                vrcStyle = !vrcStyle;
                EditorPrefs.SetBool("Lyra.MenuManager.VRCStyle", vrcStyle);
                NotifyMain();
            }
            GUI.backgroundColor = prevBg;
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            bool showInventory = EditorPrefs.GetBool("Lyra.MenuManager.ShowInventory", true);
            showInventory = EditorGUILayout.ToggleLeft(" インベントリパネルを表示", showInventory);

            EditorGUILayout.Space(5);

            bool autoAddRoot = EditorPrefs.GetBool("Lyra.MenuManager.AutoAddNewItemsToRoot", true);
            autoAddRoot = EditorGUILayout.ToggleLeft(" 新規アイテム追加時にルートに自動追加する", autoAddRoot);

            if (EditorGUI.EndChangeCheck()){
                EditorPrefs.SetBool("Lyra.MenuManager.VRCStyle", vrcStyle);
                EditorPrefs.SetBool("Lyra.MenuManager.ShowInventory", showInventory);
                EditorPrefs.SetBool("Lyra.MenuManager.AutoAddNewItemsToRoot", autoAddRoot);
                NotifyMain();
            }
            
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button("データ管理...", GUILayout.Height(28))){
                MenuManagerDataManager.ShowWindow();
            }
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(25);

            _showColorPalette = EditorGUILayout.Foldout(_showColorPalette, " カラーパレット設定", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            if (_showColorPalette){
                EditorGUILayout.Space(4);
                EditorGUI.indentLevel++;
                string prefix = vrcStyle ? "VRC." : "Normal.";

                DrawColorField("背景 (全体)", prefix + "BG_DARK", vrcStyle ? new Color(0.04f, 0.08f, 0.08f) : new Color(0.10f, 0.10f, 0.14f));
                DrawColorField("メニュー背景", prefix + "RING_BG", vrcStyle ? new Color(0.102f, 0.349f, 0.380f, 0.97f) : new Color(0.14f, 0.15f, 0.20f, 0.97f));
                DrawColorField("スライス (通常)", prefix + "SLICE_NORMAL", vrcStyle ? new Color(0.094f, 0.231f, 0.251f, 0.88f) : new Color(0.18f, 0.20f, 0.28f, 0.88f));
                DrawColorField("スライス (ホバー)", prefix + "SLICE_HOVER", vrcStyle ? new Color(0.133f, 0.294f, 0.314f, 0.95f) : new Color(0.28f, 0.42f, 0.78f, 0.92f));
                DrawColorField("スライス (選択)", prefix + "SLICE_SELECTED", vrcStyle ? new Color(0.170f, 0.380f, 0.400f, 0.95f) : new Color(0.22f, 0.32f, 0.52f, 0.92f));
                DrawColorField("セパレーター", prefix + "SEPARATOR", vrcStyle ? new Color(0.102f, 0.349f, 0.380f, 0.55f) : new Color(0.28f, 0.28f, 0.36f, 0.55f));
                DrawColorField("ドラッグ元", prefix + "SLICE_DRAG_SRC", vrcStyle ? new Color(0.05f, 0.10f, 0.10f, 0.60f) : new Color(0.55f, 0.32f, 0.18f, 0.80f));
                DrawColorField("ドラッグ先", prefix + "SLICE_DRAG_DST", vrcStyle ? new Color(0.10f, 0.60f, 0.30f, 0.70f) : new Color(0.25f, 0.55f, 0.25f, 0.70f));
                DrawColorField("ドラッグ挿入", prefix + "SLICE_DRAG_INTO", vrcStyle ? new Color(0.20f, 0.50f, 0.50f, 0.85f) : new Color(0.50f, 0.28f, 0.68f, 0.85f));
                DrawColorField("中央 (ドラッグ)", prefix + "CENTER_DRAG", vrcStyle ? new Color(0.20f, 0.50f, 0.50f, 0.85f) : new Color(0.28f, 0.50f, 0.72f, 0.85f));
                DrawColorField("アクセント", prefix + "ACCENT", vrcStyle ? new Color(0.12f, 0.65f, 0.65f) : new Color(0.40f, 0.62f, 1.0f));
                DrawColorField("アクセント (サブ)", prefix + "ACCENT_SUB", new Color(0.92f, 0.56f, 0.18f));
                DrawColorField("文字色 (メイン)", prefix + "TEXT_PRI", new Color(0.92f, 0.93f, 0.96f));
                DrawColorField("文字色 (サブ)", prefix + "TEXT_SEC", vrcStyle ? new Color(0.55f, 0.65f, 0.65f) : new Color(0.55f, 0.56f, 0.62f));
                DrawColorField("背景", prefix + "CRUMB_BG", vrcStyle ? new Color(0.05f, 0.12f, 0.12f, 0.97f) : new Color(0.12f, 0.12f, 0.16f, 0.97f));
                DrawColorField("メニュー中央背景", prefix + "CENTER_BG", vrcStyle ? new Color(0.22f, 0.22f, 0.22f, 1.0f) : new Color(0.11f, 0.12f, 0.17f, 1.0f));
                DrawColorField("空スロット", prefix + "EMPTY_SLOT", vrcStyle ? new Color(0.05f, 0.10f, 0.10f, 0.35f) : new Color(0.16f, 0.16f, 0.22f, 0.35f));

                EditorGUILayout.Space(12);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("パレットを初期化", GUILayout.Width(140), GUILayout.Height(24))){
                    if (EditorUtility.DisplayDialog("確認", "現在のテーマのカスタムカラー設定をすべてリセットしますか？", "はい", "キャンセル")){
                        ResetAllColors(prefix);
                        NotifyMain();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.Space(10);

            var advancedStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            bool isJa = nadena.dev.ndmf.localization.LanguagePrefs.Language.StartsWith("ja");
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, isJa ? "高度な設定" : "Advanced", true, advancedStyle);
            if (_showAdvanced){
                EditorGUI.indentLevel++;

                string advHelp = isJa
                    ? "他のNDMFプラグインとの干渉でエラーが発生する場合、\n" +
                      "問題のプラグインの後に実行するプラグイン順序(.after)をプロジェクト共通で設定します。\n" +
                      "干渉しているプラグインのコンポーネントをドロップするか、一覧から選択してください。\n" +
                      "なお、適用にはプロジェクトの再読み込みが必要です。"
                    : "If errors occur due to conflicts with other NDMF plugins,\n" +
                      "you can configure project-wide .after ordering for this plugin.\n" +
                      "Drop a component from the conflicting plugin, or select from the list.";
                EditorGUILayout.HelpBox(advHelp, MessageType.None);
                EditorGUILayout.Space(4);

                if (_projectAfterPlugins.Count > 0){
                    EditorGUILayout.LabelField(isJa ? "登録済みプラグイン:" : "Registered Plugins:", EditorStyles.boldLabel);
                    int removeIndex = -1;
                    for (int i = 0; i < _projectAfterPlugins.Count; i++){
                        EditorGUILayout.BeginHorizontal();
                        string qn = _projectAfterPlugins[i];
                        string displayLabel = GetPluginDisplayLabel(qn);
                        EditorGUILayout.LabelField(displayLabel, EditorStyles.miniLabel);
                        if (GUILayout.Button("×", GUILayout.Width(22), GUILayout.Height(18))){
                            removeIndex = i;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (removeIndex >= 0 && removeIndex < _projectAfterPlugins.Count){
                        _projectAfterPlugins.RemoveAt(removeIndex);
                        SaveProjectAfterPlugins();
                    }
                    EditorGUILayout.Space(4);
                }

                EditorGUILayout.BeginHorizontal();
                _draggedObject = (GameObject)EditorGUILayout.ObjectField(
                    isJa ? "オブジェクトから追加" : "Add from Object",
                    _draggedObject, typeof(GameObject), true);
                GUI.enabled = _draggedObject != null;
                if (GUILayout.Button(isJa ? "検出" : "Detect", GUILayout.Width(55))){
                    var monos = _draggedObject.GetComponents<MonoBehaviour>();
                    var baseAsmNames = new HashSet<string>();
                    foreach (var mono in monos){
                        if (mono == null) continue;
                        string asmName = mono.GetType().Assembly.GetName().Name;
                        baseAsmNames.Add(asmName);
                        foreach (var suffix in new[]{ ".core", ".runtime", ".Core", ".Runtime" }){
                            if (asmName.EndsWith(suffix)){
                                baseAsmNames.Add(asmName.Substring(0, asmName.Length - suffix.Length));
                                break;
                            }
                        }
                    }

                    var foundPlugins = new List<PluginEntry>();
                    var seen = new HashSet<string>();
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()){
                        try {
                            string asmName = asm.GetName().Name;
                            bool matches = false;
                            foreach (var bn in baseAsmNames){
                                if (asmName.StartsWith(bn) || bn.StartsWith(asmName)){ matches = true; break; }
                            }
                            if (!matches) continue;
                            var plugins = FindNDMFPluginsInAssembly(asm);
                            foreach (var p in plugins)
                                if (seen.Add(p.QualifiedName))
                                    foundPlugins.Add(p);
                        } catch { }
                    }

                    if (foundPlugins.Count == 1){
                        AddProjectAfterPlugin(foundPlugins[0].QualifiedName);
                        _draggedObject = null;
                    } else if (foundPlugins.Count > 1){
                        var menu = new GenericMenu();
                        foreach (var p in foundPlugins){
                            string pqn = p.QualifiedName;
                            menu.AddItem(new GUIContent($"{p.DisplayName} ({pqn})"), false, () => {
                                AddProjectAfterPlugin(pqn);
                            });
                        }
                        menu.ShowAsContext();
                        _draggedObject = null;
                    } else {
                        Debug.LogWarning(isJa
                            ? $"[MenuManager] '{_draggedObject.name}' のコンポーネントからNDMFプラグインが見つかりませんでした。"
                            : $"[MenuManager] No NDMF plugin found on '{_draggedObject.name}'.");
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button(isJa ? "プラグイン一覧から追加..." : "Add from Plugin List...")){
                    var allPlugins = FindAllNDMFPlugins();
                    var menu = new GenericMenu();
                    foreach (var p in allPlugins){
                        string pqn = p.QualifiedName;
                        bool exists = _projectAfterPlugins.Contains(pqn);
                        if (exists)
                            menu.AddDisabledItem(new GUIContent($"✓ {p.DisplayName} ({pqn})"));
                        else
                            menu.AddItem(new GUIContent($"{p.DisplayName} ({pqn})"), false, () => {
                                AddProjectAfterPlugin(pqn);
                            });
                    }
                    if (allPlugins.Count == 0)
                        menu.AddDisabledItem(new GUIContent(isJa ? "NDMFプラグインが見つかりません" : "No NDMF plugins found"));
                    menu.ShowAsContext();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            _showDebug = EditorGUILayout.Foldout(_showDebug, isJa ? "デバッグ" : "Debug", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
            if (_showDebug){
                EditorGUI.indentLevel++;
                var prevCol = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
                if (GUILayout.Button(isJa ? "認証状態をリセット" : "Reset Authentication", GUILayout.Width(160), GUILayout.Height(24))){
                    if (EditorUtility.DisplayDialog("確認", "認証状態をリセットしますか？\n(無料版の制限状態に戻ります)", "はい", "キャンセル")){
                        MenuManagerAuth.ResetAuth();
                        NotifyMain();
                    }
                }
                GUI.backgroundColor = prevCol;
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("閉じる", GUILayout.Width(100), GUILayout.Height(26))){
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawColorField(string label, string fullKeySuffix, Color defaultColor){
            string fullKey = "Lyra.MenuManager.Color." + fullKeySuffix;
            Color currColor = defaultColor;
            
            if (EditorPrefs.HasKey(fullKey)){
                if (ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(fullKey), out Color parsed))
                    currColor = parsed;
            }

            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUILayout.ColorField(new GUIContent(label), currColor, true, true, true);
            if (EditorGUI.EndChangeCheck()){
                EditorPrefs.SetString(fullKey, ColorUtility.ToHtmlStringRGBA(newColor));
                NotifyMain();
            }
        }

        private void ResetAllColors(string prefix){
            string[] keys = new string[] { "BG_DARK", "RING_BG", "SLICE_NORMAL", "SLICE_HOVER", "SLICE_SELECTED", "SLICE_DRAG_SRC", "SLICE_DRAG_DST", "SLICE_DRAG_INTO", "CENTER_DRAG", "ACCENT", "ACCENT_SUB", "TEXT_PRI", "TEXT_SEC", "SEPARATOR", "CRUMB_BG", "CENTER_BG", "EMPTY_SLOT" };
            foreach (var k in keys){
                EditorPrefs.DeleteKey("Lyra.MenuManager.Color." + prefix + k);
            }
        }

        private List<string> LoadProjectAfterPlugins(){
            var list = new List<string>();
            string raw = EditorPrefs.GetString(PREF_KEY_AFTER_PLUGINS, "");
            if (string.IsNullOrEmpty(raw)) return list;
            var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts){
                string qn = p.Trim();
                if (qn.Length == 0) continue;
                if (!list.Contains(qn)) list.Add(qn);
            }
            return list;
        }

        private void SaveProjectAfterPlugins(){
            if (_projectAfterPlugins == null){
                EditorPrefs.DeleteKey(PREF_KEY_AFTER_PLUGINS);
                return;
            }
            EditorPrefs.SetString(PREF_KEY_AFTER_PLUGINS, string.Join("|", _projectAfterPlugins));
        }

        private void AddProjectAfterPlugin(string qualifiedName){
            if (string.IsNullOrEmpty(qualifiedName)) return;
            if (_projectAfterPlugins == null) _projectAfterPlugins = new List<string>();
            if (_projectAfterPlugins.Contains(qualifiedName)) return;
            _projectAfterPlugins.Add(qualifiedName);
            SaveProjectAfterPlugins();
        }

        private struct PluginEntry { public string QualifiedName; public string DisplayName; }

        private static System.Collections.Generic.List<PluginEntry> FindNDMFPluginsInAssembly(Assembly assembly){
            var result = new System.Collections.Generic.List<PluginEntry>();
            try {
                var attrs = assembly.GetCustomAttributes(typeof(nadena.dev.ndmf.ExportsPlugin), false);
                foreach (nadena.dev.ndmf.ExportsPlugin attr in attrs){
                    try {
                        var pluginType = attr.PluginType;
                        if (pluginType.FullName == typeof(Lyra.MenuManagerPlugin).FullName) continue;
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

        private static System.Collections.Generic.List<PluginEntry> FindAllNDMFPlugins(){
            var result = new System.Collections.Generic.List<PluginEntry>();
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

        private void NotifyMain(){
            var managerWindows = Resources.FindObjectsOfTypeAll<MenuManager>();
            foreach (var win in managerWindows){
                if (win != null){
                    win.LoadSettings();
                    win.Repaint();
                }
            }
        }

        private void DrawProUnlockSection(){
            EditorGUILayout.Space(6);

            if (MenuManagerAuthHelper.CheckBit(99)){
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                var statusRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(statusRect, new Color(0.08f, 0.22f, 0.12f, 0.9f));

                var statusStyle = new GUIStyle(EditorStyles.label){
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = new Color(0.4f, 0.9f, 0.5f) }
                };
                GUI.Label(statusRect, " 有料版 認証済み", statusStyle);

                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
            }
            else{
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.12f, 0.65f, 0.65f);

                if (GUILayout.Button(" 有料版をロック解除", GUILayout.Height(32))){
                    MenuManagerAuthWindow.ShowWindow();
                }
                GUI.backgroundColor = prevBg;

                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            var sepRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            sepRect.x += 10;
            sepRect.width -= 20;
            EditorGUI.DrawRect(sepRect, new Color(0.3f, 0.3f, 0.35f, 0.5f));
        }
    }
}

#endif