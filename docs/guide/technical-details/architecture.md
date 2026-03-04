# Architecture Overview

## 非破壊型ワークフロー
本ツールは非破壊アプローチを採用しています。設定ファイルはプレイモード開始時やVRChatへのビルド時に自動適用されるため、ユーザーがMAの設定を書き換えたり、手動でプレハブを解体・破壊して中身を手作業で並び替えるリスクや手間は一切ありません。

---

## 処理フロー

### データのパースと結合ツリーの生成フェーズ
ユーザーが `MenuManager` のウィンドウ（`MenuManager.cs`）を開いたとき、以下のステップでアバターの内部からメニュー構造を抽出します。

1. **VRCExpressionsMenu の展開**

`VRCAvatarDescriptor` にセットされている公式のルートメニューを起点に、再帰的に全てのサブメニューノードを展開します。

2. **Modular Avatar の仮想統合**

`GetComponentsInChildren<ModularAvatarMenuInstaller>(true)` によってアバター内のどこかに配置されているMAの設計図をすべて拾い上げます。

3. **パスの合成**

各インストーラーが持つ `installPath` プロパティ（例: `Props/Weapon/Sword`）を仮想のフォルダとしてメモリ上に合成し、元のメニューツリーにぶら下げます。

```csharp
// メモリ上にMenuNodeとして仮想化されたツリーの一部
public class MenuNode {
    public string Name;
    public VRCExpressionsMenu.Control NativeControl; // VRC公式コントロール
    public ModularAvatarMenuItem MAMenuItem; // MAのメニューアイテム
    public string InstallPath; // 評価済みの階層パス
    public List<MenuNode> Children = new List<MenuNode>();
}
```

### レイアウトのシリアライズと保存
エディターでメニューの編集を行い、保存ボタンを押すと、画面上のMenuNodeツリー構造がフラットな1次元の配列データへと変換され、アバター直下に配置される `MenuLayoutData` 内にシリアライズされて保存されます。

- **階層パス**

各要素がどこに所属しているかを `/` 区切りの文字列で表現し直します。
- **配置順序**

Orderによってフォルダ内でのインデックス値が振られます。
- **インベントリへの一時退避**

インベントリに置かれたアイテムは `ParentPath` が `__INVENTORY__` で始まる特殊領域へ割り当てられます。
- **キー**

同じ名前のアイテムを混同しないよう、`Control.Type : Control.Name : Parameter.Name : Control.Value` から一意の識別子を生成します。

### NDMFプラグインによるビルド時再構成
`MenuManagerPlugin.cs` は、NDMFのアーキテクチャに依存しています。

Modular Avatar本体がすべてのアニメーションやメニューを結合して最終的な `VRCExpressionsMenu` を１つにまとめた後、NDMF処理一番最後のOptimizingフェーズでフックします。
```csharp
// Modular Avatarなど他の主要プラグインのTransformingフェーズ後（Optimizing）に実行
InPhase(BuildPhase.Optimizing).Run("Reorder Menus", ctx => { ... });
```
ここで、`MenuLayoutData` の記述通りに、完成した巨大な `VRCExpressionsMenu` の中からコントロールをすべて抜き出し、`Order` 順にツリーにつなぎ合わせ直します。

### 超過メニューの自処理
VRChatの仕様上、1つの階層に定義できるアイテムの最大数は8つまでに制限されています。
本ツールのエディターではこれを自由に超えてアイテムを放り込むことができますが、保存した際やビルド時において、8つを超過したアイテムは自動的に子フォルダへと押し出されます。

---

## アーキテクチャ図解

```text
[ エディター (編集時) ]
1. VRC Avatar Descriptor
   ├── VRCExpressionsMenu
   ├── ModularAvatarMenuInstaller
   └── MenuLayoutData
           └── ItemLayout [ "Key", "ParentPath", "Order" ... ]

       ↓ ↓ ↓ (ビルド実行 / プレイモード開始) ↓ ↓ ↓

[ NDMFビルドプロセス ]
2. Transforming Phase
   MA がすべての MenuInstaller をマージし、巨大な1つの VRCExpressionsMenu を生成する。
   (※この時点ではMAが追加した順の自動配置)

3. Optimizing Phase
   MenuManagerPlugin.cs が起動。
   MenuLayoutData を読み取り、巨大な VRCExpressionsMenu の中の Control を取得。
   Order と ParentPath の定義通りに、上限を維持してフォルダを再構築する。

4. クリーンアップ
   MenuLayoutData コンポーネント自身を DestroyImmediate し、アバタービルド時に影響を及ぼさないよう痕跡を消去する。
```
