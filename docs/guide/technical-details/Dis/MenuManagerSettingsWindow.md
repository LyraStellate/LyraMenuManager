# MenuManagerSettingsWindow.cs

## 概要
ツールの見た目（UIテーマ）や挙動をユーザーの好みに合わせて変更・記憶するための専用設定ウィンドウを提供するエディタスクリプトです。

## 主な役割と機能
- **EditorPrefs を利用した状態保存**: PC環境（プロジェクトやシーンを跨ぐ）ごとに行う設定を保存。
- **UIテーマ（VRC Style）の切り替え**: VRChatライクなブルーグリーン基調のテーマと、標準のダークテーマの切り替え。
- **カラーパレット設定**: メイン・サブカラー、文字色などを部分ごとに手動でオーバーライド可能。

## 実装の詳細とサンプルコード

![ダミー画像：Settings Window全体](https://placehold.jp/3d4070/ffffff/500x600.png?text=ダミー画像：Settings+Windowのスクリーンショット)

### EditorPrefs による値の保存と共有
Unityのエディター固有のグローバル変数領域である `EditorPrefs` に値を読み書きします。
```csharp
public class MenuManagerSettingsWindow : EditorWindow {
    private void OnGUI(){
        EditorGUI.BeginChangeCheck();

        // トグル状態の読み込みと表示
        bool vrcStyle = EditorPrefs.GetBool("Lyra.MenuManager.VRCStyle", true);
        
        // GUI背景色を動的に変更してボタンを装飾
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = vrcStyle ? new Color(0.4f, 0.8f, 0.9f) : new Color(0.6f, 0.6f, 0.6f);
        
        if (GUILayout.Button(vrcStyle ? "VRC Style UI : ON" : "OFF")){
            vrcStyle = !vrcStyle;
        }
        GUI.backgroundColor = prevBg;

        // 値が変更されたら保存してメインウィンドウに即時反映
        if (EditorGUI.EndChangeCheck()){
            EditorPrefs.SetBool("Lyra.MenuManager.VRCStyle", vrcStyle);
            NotifyMain();
        }
    }
}
```

### カラーパレット（カラーピッカー）の実装
VRC Style用と標準用のそれぞれのカラーテーマのプリセットを持ち、独自のカラーキーでオーバーライド情報を管理します。
```csharp
private void DrawColorField(string label, string fullKeySuffix, Color defaultColor){
    // "Lyra.MenuManager.Color.VRC.BG_DARK" のようなキーを構築
    string fullKey = "Lyra.MenuManager.Color." + fullKeySuffix;
    Color currColor = defaultColor;
    
    // 保存済みのカラー情報があれば HTML Hex コードからパースして上書き
    if (EditorPrefs.HasKey(fullKey)){
        if (ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(fullKey), out Color parsed))
            currColor = parsed;
    }

    EditorGUI.BeginChangeCheck();
    Color newColor = EditorGUILayout.ColorField(new GUIContent(label), currColor, true, true, true);
    
    // ユーザーがピッカーで変更した場合、Hex形式の文字列にしてGlobal領域へ保存
    if (EditorGUI.EndChangeCheck()){
        EditorPrefs.SetString(fullKey, ColorUtility.ToHtmlStringRGBA(newColor));
        NotifyMain();
    }
}
```

### リアルタイム即時反映 (NotifyMain)
設定ウィンドウでカラーやテーマが変更された際、すでに開かれている `MenuManager` ウィンドウがあれば即座に再描画（Repaint）をリクエストする仕組みです。
```csharp
private void NotifyMain(){
    // バックグラウンドで開いているMenuManagerウィンドウをすべて取得
    var managerWindows = Resources.FindObjectsOfTypeAll<MenuManager>();
    foreach (var win in managerWindows){
        if (win != null){
            win.LoadSettings(); // 保存されたEditorPrefsから最新カラーをフェッチ
            win.Repaint();      // OnGUIの強制再呼び出し
        }
    }
}
```
