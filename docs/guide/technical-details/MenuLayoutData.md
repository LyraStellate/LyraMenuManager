# MenuLayoutData.cs

## 概要
アバタールートにアタッチされる `MonoBehaviour` クラスであり、メニューの配置情報をシリアライズして永続化するためのデータコンテナスクリプトです。

## 主な役割と機能

- **シーン内コンポーネント**

プレハブの一部として保存可能なデータストアとして機能します。Editorフォルダではなく、通常の実行対象スクリプトとして配置される必要があります。

- **ビルド時の参照先**

`MenuManagerPlugin`はこのコンポーネントを探し、そのデータを元にメニューを並び替えます。

## 実装詳細

### ItemLayout クラス
メニュー内の各アイテムの位置とプロパティを表現します。
```csharp
[Serializable]
public class ItemLayout {
    public string Key; //アイテム識別キー
    public string ParentPath = ""; //所属する階層パス
    public int Order; //0を起点とする同一階層内の順序
    public bool IsSubMenu; 
    public string DisplayName;
    public Texture2D CustomIcon;

    //特殊フラグ
    public bool IsBuildTime;
}
```
- **Key生成**

名前だけでなく、タイプやパラメータ名、初期値を組み合わせたコロン(`:`)区切りの文字列をキーとしています。これにより、同名のスイッチが複数あっても同一のアイテムを特定して配置を復元できます。

- **階層表現 (ParentPath)**

ファイルシステムのように、 `/` で区切るパス構造（例: `Props/Weapons`）を利用し階層化データに対応しています。

### フィールド変数
```csharp
public List<ItemLayout> Items = new List<ItemLayout>(); //レイアウト情報リスト
public string LastSavedAt; //最終保存日時
public bool IsEnabled = true; //有効無効設定
public bool RemoveEmptyFolders = false; //空フォルダ消去オプション
```
ユーザーが Menu Manager エディタ上で保存を実行するたび、エディタ画面上の現在のツリー構造がフラットな `List<ItemLayout>` の配列リストに変換され、この `Items` に保存されます。インベントリに入れられたメニュー項目は、`ParentPath` が `__INVENTORY__` で始まる特殊なパス情報とともに保存されます。
