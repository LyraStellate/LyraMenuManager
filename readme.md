# Lyra Menu Manager

[![Booth](https://img.shields.io/badge/Booth-Download_/_Purchase-fc4d50?style=for-the-badge&logo=booth)](https://lyrastellate.booth.pm/items/8065097)

[English](#english) | [日本語](#japanese)

---

<h2 id="japanese">日本語</h2>

**Lyra Menu Manager** は、ギミックの追加によって煩雑になりがちなエクスプレッションメニューを整理する非破壊ツールです。
NDMFによって動作し、VRC Expressions Menu、MA Menu Installer、およびlilycalInventoryによって追加されるメニュー要素を一括管理します。

詳細や操作方法については[公式ドキュメント](https://docs.lyrastellate.dev/menu-manager)をご覧ください。

---

### ダウンロード・購入

本ツールは **Booth** にて配布および販売を行っております。
最新版のダウンロード、またはPro版のご購入は以下のページよりお願いいたします。

[**Booth 配布・販売ページ**](https://lyrastellate.booth.pm/items/8065097)

### 主な特徴

- **MAとLCIに対応:** 視覚的なメニュー編集を強力にサポートします。
- **2種類のUI:** VRC内と同じレイアウトのUIと、編集時に便利な固定スロットUIを用いてスムーズなメニュー配置が可能です。
- **直感的な操作:** ドラッグ＆ドロップ（D&D）によって順番の入れ替えやサブメニューへの移動が直感的に行えます。
- **インベントリシステム:** メニューアイテムの管理を補助するインベントリ機能を搭載。
- **完全非破壊:** コンポーネントを削除するだけで、全て元の状態に戻すことができる非破壊仕様です。

### 導入方法

#### 1. 必須アセットの確認

本ツールの動作には、以下のツールが事前にプロジェクトへインポートされている必要があります。

**必須:**
- [NDMF (Non-Destructive Modular Framework)](https://github.com/bdunderscore/ndmf)

**対応ツール:**
- [Modular Avatar](https://modular-avatar.nadena.dev/ja/)
- [lilycalInventory](https://booth.pm/ja/items/5993590)

#### 2. インストール

VPMパッケージをVCC（VRChat Creator Companion）またはALCOMに導入し、アバタープロジェクトに最新版の「Lyra Menu Manager」をインストールしてください。

### 無料版とPro版の違い

無料版では以下の機能が制限されます。

- 第2階層以上の深い階層での編集
- インベントリ一括回収機能
- 差分保存機能

> **Note**
> 今後、制限範囲が変更になる可能性があります。あらかじめご了承ください。

### 認証方法（Pro版）

Unityにインストール後、認証ウィンドウからBoothでの**注文番号**を入力して認証を行ってください。
ギフトとして受け取った場合は、ギフトの贈り主から注文番号を教えてもらい、それを入力してください。

### 注意事項

- 本ツールは、アバタールートの `Expressions Menu`、Modular Avatarの `MA Menu Installer`、および `lilycalInventory` によって追加されるメニューのみをサポートしています。
- 他ツールによるメニュー追加は、`MA Menu Installer` を介する場合のみサポートします。スクリプト単体でメニューに直接干渉するツールの場合は編集ができません。
- 上記のような直接干渉するスクリプトと併用する場合、空のオブジェクトに `Menu Manager Item Proxy` コンポーネントを追加し、`Menu Item Name` にスクリプトによって追加される予定のメニューアイテムの名前とタイプを設定することで制御可能になります。
  - 詳細は[ドキュメント内コンポーネントページ](https://docs.lyrastellate.dev/menu-manager/guide/components/MenuManagerItemProxy.html)をご覧ください。

### 免責事項

- 本ツールは継続的なアップデートを予定しておりますが、VRChat、Unity、Modular Avatar、lilycalInventory等の関連環境において大幅な仕様変更が行われた場合、やむを得ず本ツールのサポートを終了する場合がございます。あらかじめご了承ください。
- 本製品はダウンロード商品という性質上、いかなる理由であってもご購入後の返品および返金には一切応じかねます。万が一、製品に不具合等が見受けられた場合、またはご不明な点がございましたら、メッセージにてお気軽にお問い合わせください。
- 本規定の日本語版と英語版の間に相違または矛盾が生じた場合、日本語版の規定が優先して適用されるものとします。

### 更新履歴

詳細につきましては[ドキュメント内更新履歴](https://docs.lyrastellate.dev/menu-manager/guide/changelog.html)に記載しております。

- **[1.2.1]** - 2026-03-16: 名前空間エラーを修正
- **[1.2.0]** - 2026-03-15: lilycalInventoryに対応
- **[1.1.1]** - 2026-03-14: 不具合修正
- **[1.1.0]** - 2026-03-14: Menu Manager Item Proxyコンポーネントを追加
- **[1.0.0]** - 2026-03-13: リリース

---

<h2 id="english">English</h2>

**Lyra Menu Manager** is a non-destructive tool that organizes the often cluttered Expressions Menu when adding various gimmicks to your avatar.
It works with NDMF and centrally manages menu elements added by the base VRC Expressions Menu, MA Menu Installer, and lilycalInventory.

For more details and usage instructions, please visit the [Official Documentation](https://docs.lyrastellate.dev/menu-manager).

---

### Download & Purchase

This tool is distributed and sold on **Booth**.
To download the latest version or purchase the Pro version, please visit our Booth page below.

[**Get it on Booth**](https://lyrastellate.booth.pm/items/8065097)

### Key Features

- **Full support for MA and LCI:** Strongly supports visual menu editing.
- **Comfortable editing with 2 types of UI:** Smooth menu placement is possible using a UI with the same layout as in VRChat, and a fixed-slot UI that is convenient during editing.
- **Intuitive operations:** You can intuitively reorder items or move them to submenus via Drag & Drop (D&D).
- **Inventory system:** Equipped with an inventory feature to assist with menu item management.
- **Completely non-destructive:** A non-destructive design where you can restore everything to its original state simply by removing the component.

### Installation

#### 1. Required Assets

The following tools must be imported into your project before using this tool.

**Required:**
- [NDMF (Non-Destructive Modular Framework)](https://github.com/bdunderscore/ndmf)

**Supported Tools:**
- [Modular Avatar](https://modular-avatar.nadena.dev/en/)
- [lilycalInventory](https://booth.pm/ja/items/5993590)

#### 2. Installation

Add the VPM package to VCC (VRChat Creator Companion) or ALCOM, and install the latest version of "Lyra Menu Manager" to your avatar project.

### Free vs Pro Version

The Free version has the following limitations:

- Editing at depth levels of 2 or deeper
- Bulk inventory collection feature
- Differential save feature

> **Note**
> Please note that these limitations may change in the future.

### Authentication (Pro Version)

After installing in Unity, open the authentication window and enter your Booth **Order Number** to authenticate.
If you received this as a gift, please ask the sender for the order number and enter it.

### Notes

- This tool only supports the `Expressions Menu` on the avatar root, and menus added by Modular Avatar's `MA Menu Installer` or `lilycalInventory`.
- Menu additions by other tools are only supported if they go through the `MA Menu Installer`. It cannot edit menus that are directly manipulated by a standalone script.
- In cases where you use a standalone script that directly adds menus, you can control it by adding a `Menu Manager Item Proxy` component to an empty object, and setting the `Menu Item Name` and type to match what the script will add.
  - For more details, see the [Documentation Components Page](https://docs.lyrastellate.dev/menu-manager/guide/components/MenuManagerItemProxy.html).

### Disclaimer

- We plan to continuously update this tool. However, if there are major specification changes in related environments such as VRChat, Unity, Modular Avatar, or lilycalInventory, we may be forced to end support for this tool. Please understand this in advance.
- Due to the nature of downloadable products, we cannot accept any returns or refunds after purchase for any reason. If you find any defects in the product or have any questions, please feel free to contact us via message.
- If there is any discrepancy or conflict between the Japanese and English versions of these terms, the Japanese version shall prevail.

### Changelog

For details, please refer to the [Changelog in the Documentation](https://docs.lyrastellate.dev/menu-manager/guide/changelog.html).

- **[1.2.1]** - 2026-03-16: Fixed namespace errors
- **[1.2.0]** - 2026-03-15: Added support for lilycalInventory
- **[1.1.1]** - 2026-03-14: Bug fixes
- **[1.1.0]** - 2026-03-14: Added Menu Manager Item Proxy component
- **[1.0.0]** - 2026-03-13: Initial Release
