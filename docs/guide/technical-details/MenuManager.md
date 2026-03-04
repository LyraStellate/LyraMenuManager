# MenuManager.cs

## 概要
本ツールの中核となるエディター拡張スクリプトです。
VRChatのラジアルメニューをUnityエディターのインスペクター上で再現し、直感的な操作でメニュー階層を編集するインターフェースを提供します。

## 主な役割と機能
- **レイアウトの描画**

スライス描画による円形メニューUI。

- **MA統合ツリー生成**

通常の `VRCExpressionsMenu` に加え、アバター内に散らばった `MA Menu Installer` コンポーネントを探索して仮想的な統合メニューツリーを合成。

- **同一階層上限のエミュレート**

8項目を超過した要素に対して、VRChatゲーム内と同様自動的に `...(More)` フォルダを生成して収納・展開します。

- **永続化**

スライスの並び替えやフォルダ作成・挿入といった配置情報を生成し、`MenuLayoutData` へのセーブを仲介。

## 実装詳細

### メニューの描画アルゴリズム
VRChatのラジアルメニューを模倣するため、`Handles.DrawSolidArc` などを多用して扇形を動的描画します。
通常時は8分割ですが、VRC Style UI時に項目が8個を下回る場合は自動的にスケールを調整します。
```csharp
private void DrawSlice(Rect rect, Vector2 center, float startAngle, float sweepAngle, float radiusInner, float radiusOuter, Color bgColor, bool isHover, bool isMore){
    Handles.color = bgColor;
    Handles.DrawSolidArc(center, Vector3.forward, Vector3.right, 360f, radiusOuter);
    
    Vector2 dir = new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad));
    Vector2 p1 = center + dir * radiusInner;
    Vector2 p2 = center + dir * radiusOuter;
    
    Handles.color = _colors["SEPARATOR"];
    Handles.DrawLine(p1, p2, 2.0f);
}
```

### メニューツリーの再帰的構築とMA結合
このツールは1つのMenuを展開するだけでなく、Modular Avatarの分散構造を結合します。
```csharp
private void RebuildMenu(){
    //標準ルートメニューのロード
    var rootObj = _descriptor.expressionsMenu;
    _menuTreeRoot = BuildMenuNode(rootObj, "");

    //Modular Avatar Menu Installer の検索と統合
    var installers = _targetAvatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true);
    foreach (var installer in installers){
        if (installer.menuToAppend != null){ //パスの文字列をパースし、_menuTreeRootの階層に仮想的につなぎ合わせる
            string targetPath = installer.installPath;
            AddInstallerEntries(_menuTreeRoot, installer.menuToAppend, targetPath);
        }
    }
    
    ApplyLayoutToTree(_menuTreeRoot, layoutData); //過去に保存したMenuLayoutDataとのすり合わせ
    ReevaluateOverflow(_menuTreeRoot); //超過項目の自動生成
}
```

### 項目上限数のエミュレート
VRChatのラジアルメニューは1フォルダにつき8スロット固定です。9項目以上ある場合は、自動的にオーバーフロー再起が行われます。
`MenuManager` はこれをシミュレートし右下の手前部分に固定の `...(More)` フォルダを生成して隔離します。
```csharp
private void ApplyOverflowMore(MenuNode node) {
    const int MAX_CONTROLS = 8;
    
    if (node.Children.Count <= MAX_CONTROLS) return;

    // 最初の7枠分までをこの階層に残す
    var keptChildren = node.Children.Take(MAX_CONTROLS - 1).ToList();
    var overflowing = node.Children.Skip(MAX_CONTROLS - 1).ToList();

    // 仮想の子フォルダ [More] ノードを作成
    var moreNode = new MenuNode {
        Name = "…(More)",
        IsVirtualFolder = true,
        Icon = BuiltinMoreIcon
    };

    moreNode.Children.AddRange(overflowing);
    node.Children = keptChildren;
    node.Children.Add(moreNode); // 8番目の要素として追加

    // 超過分がさらに8を超える場合、再帰的に超過フォルダの中に超過フォルダを作る
    ApplyOverflowMore(moreNode);
}
```
