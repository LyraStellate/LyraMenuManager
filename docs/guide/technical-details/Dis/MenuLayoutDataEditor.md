# MenuLayoutDataEditor.cs

## 概要
`MenuLayoutData` コンポーネントのUnityインスペクター画面をカスタマイズするためのエディタスクリプト（`[CustomEditor]`）です。
Unity標準の味気ないリスト表示を隠し、専用の設定や統計情報をわかりやすく表示する役割を持ちます。

## 主な役割と機能
- インスペクターUIのオーバーライド（`OnInspectorGUI`）
- 各種設定の有効化・無効化トグル
- 非表示にされている詳細データ（デバッグ情報）のフォールダウン表示
- NDMF (Non-Destructive Modular Framework) の言語切り替えウィジェットの統合

## 実装の詳細とサンプルコード

### カスタムエディタの定義
このスクリプトは `Editor` フォルダ内（ビルド対象外）に配置され、`[CustomEditor(typeof(MenuLayoutData))]` 属性を使って特定のコンポーネントの見た目を上書きします。
```csharp
[CustomEditor(typeof(MenuLayoutData))]
public class MenuLayoutDataEditor : UnityEditor.Editor {
    private const string PREF_KEY_SETTINGS = "Lyra.MenuManager.Inspector.Settings";
    private bool _showSettings;

    private void OnEnable(){
        // インスペクターの展開状態などを EditorPrefs でPC環境に記憶
        _showSettings = EditorPrefs.GetBool(PREF_KEY_SETTINGS, true);
    }
}
```

### インスペクターの描写 (OnInspectorGUI)
`OnInspectorGUI` メソッド内で、マニュアルへのリンクボタン、設定項目、統計情報を描画します。
以下は設定部分の具体的な実装サンプルです：
```csharp
public override void OnInspectorGUI() {
    serializedObject.Update();

    // 1. 各種リンクボタン（Unityのカスタムスタイルを利用）
    var linkStyle = new GUIStyle(EditorStyles.linkLabel){ richText = true };
    GUIContent manualContent = new GUIContent(" オンラインマニュアル", EditorGUIUtility.IconContent("_Help").image, "URL");
    if (GUILayout.Button(manualContent, linkStyle)) Application.OpenURL("...");

    // 2. 「Menu Manager を開く」ボタン
    if (GUILayout.Button("Open Menu Manager", GUILayout.Height(30))) {
        // 現在選択中のアバターを自動取得してウィンドウを開く
        var data = (MenuLayoutData)target;
        var avatar = data.GetComponentInParent<VRCAvatarDescriptor>();
        Lyra.Editor.MenuManager.ShowWindow(avatar);
    }

    // 3. 設定項目 (GUI.enabled による動的なグレーアウト)
    var isEnabledProp = serializedObject.FindProperty("IsEnabled");
    isEnabledProp.boolValue = EditorGUILayout.ToggleLeft("Enable", isEnabledProp.boolValue);

    GUI.enabled = isEnabledProp.boolValue; // IsEnabledがfalseなら、以下の項目を無効化
    var removeEmptyProp = serializedObject.FindProperty("RemoveEmptyFolders");
    removeEmptyProp.boolValue = EditorGUILayout.ToggleLeft("Remove Empty Folders", removeEmptyProp.boolValue);
    GUI.enabled = true; // 元に戻す

    // 4. NDMF言語スイッチャーの呼び出し
    nadena.dev.ndmf.ui.LanguageSwitcher.DrawImmediate();

    serializedObject.ApplyModifiedProperties();
}
```

### 統計情報の算出（デバッグ用）
デバッグ展開時には、保存された配列データをループ処理し、状況を可視化します。
```csharp
int total = items.Count;
int menuItems = items.Count(i => !i.ParentPath.StartsWith("__INVENTORY__"));
int invItems = total - menuItems;
int subMenus = items.Count(i => i.IsSubMenu);

// 一番深い階層の深度を文字列の分割(Split)で算出
int maxDepth = 0;
foreach (var item in items){
    if (string.IsNullOrEmpty(item.ParentPath) || item.ParentPath.StartsWith("__INVENTORY__")) continue;
    int d = item.ParentPath.Split('/').Length;
    if (d > maxDepth) maxDepth = d;
}
```
これにより、ユーザーは現在のメニューの大きさをインスペクターから直接把握することができます。
