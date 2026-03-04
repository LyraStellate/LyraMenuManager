# MenuManagerPlugin.cs

## 概要
NDMFのフックプロセスとして動作し、ビルド時にエディター上で設定したメニュー構成を適用するスクリプトです。このプラグインはビルド後のデータを自動変換するため、元のソースデータやMAの設定を破壊しません。

## 処理のフェーズ指定
NDMFには複数の処理フェーズが存在しますが、このプラグインは `Optimizing` フェーズで実行されるよう指定されています。
```csharp
[assembly: ExportsPlugin(typeof(Lyra.MenuManagerPlugin))]
public class MenuManagerPlugin : Plugin<MenuManagerPlugin> {
    protected override void Configure() {
        // MA・Light Limit Changer 等の Transforming 系プラグインが
        // メニューをすべて生成し終えた後である Optimizing フェーズで実行する
        InPhase(BuildPhase.Optimizing)
            .Run("Reorder Menus", ctx => {
                var avatarRoot = ctx.AvatarRootObject;
                var layoutData = avatarRoot.GetComponent<MenuLayoutData>();
                if (layoutData == null || !layoutData.IsEnabled) return;

                // メニューの並び替え処理を実行
                ReorderMenu(descriptor.expressionsMenu, layoutData, "", new HashSet<VRCExpressionsMenu>());

                // 最後にビルド成果物からコンポーネント自身を消去する
                Object.DestroyImmediate(layoutData);
            });
    }
}
```

## 実装詳細

### 超過フォルダの自動解体
同一階層内にて、登録アイテム数がメニュー上限である8個を超過した場合、自動的に `Next` または `...(More)` という名前の子フォルダを生成して収納します。
MenuManagerは独自のレイアウトを適用するため、まずこれらの自動生成された退避フォルダを一旦解体し、1次配列の平坦なリストに戻します。
```csharp
private static void FlattenMoreMenus(VRCExpressionsMenu menu, HashSet<VRCExpressionsMenu> visited){
    for (int i = menu.controls.Count - 1; i >= 0; i--){
        var ctrl = menu.controls[i];
        if (ctrl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && ctrl.subMenu != null) {
            FlattenMoreMenus(ctrl.subMenu, visited);
            string n = ctrl.name ?? "";
            // 自動生成される代表的な名前のフォルダを探す
            if (n == "Next" || n == "More" || n.EndsWith("More)")) {
                menu.controls.RemoveAt(i); // Moreフォルダ自体を削除
                if (ctrl.subMenu.controls != null) {
                    // その中身を現在の階層に登録
                    menu.controls.InsertRange(i, ctrl.subMenu.controls);
                }
            }
        }
    }
}
```

### コントロールのプールとマッチング
次に、既に構築されているコントロールをすべて取り外し、レイアウトデータの `Key` と照合します。
MenuManagerは照合に以下の3段階のフォールバック機能を持たせています。

| 段階 | 条件 | キー | 用途 |
|------|------|-----|-----|
| 一段階目 | 正確な完全一致 | Type:Name:Param:Value | - |
| 二段階目 | タイプと名前の一致 | Type:Name | ParamがMAなどでビルド時に変動した場合の救済 |
| 三段階目 | 名前のみの一致 | Name | 大幅な変更があった場合の最終フォールバック |

```csharp
ParsedLayoutItem match = null;
for (int j = 0; j < parsedItems.Count; j++) if (parsedItems[j].Key1 == key1) { match = parsedItems[j]; break; }
if (match == null) for (int j = 0; j < parsedItems.Count; j++) if (parsedItems[j].Key2 == key2) { match = parsedItems[j]; break; }
if (match == null) for (int j = 0; j < parsedItems.Count; j++) if (parsedItems[j].Key3 == key3) { match = parsedItems[j]; break; }

if (match != null){
    menu.controls.RemoveAt(i);
    pool.Add(new PoolItem { Ctrl = ctrl, ... });
}
```

### ツリーの再構築とインベントリ処理
マッチしたコントロールをプールからピックアップし、`MenuLayoutData` に保存された `ParentPath` ごとの `Order` 順に沿って再配置していきます。
最後に、レイアウトで一切使われなかったプール内に残ったコントロールで、かつインベントリ (`__INVENTORY__`)に退避指定されていないものをルートに戻します。これにより新規で追加されレイアウト情報にまだ乗っていないコントロールを意図せず消去してしまう事態を防いでいます。
