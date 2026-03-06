# 更新履歴 / Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.4] - 2026-03-06

### Added
- 認証が通らない際のフォールバックとして、手動認証を可能にするボタンを追加

### Fixed
- アップロード時とPlayモードでのビルドでのメニューレイアウトが異なる問題を修正
- オブジェクトの参照をエディター全体からヒエラルキー単位に修正


## [0.8.3] - 2026-03-06

### Changed
- 古い無効なデータが蓄積し続ける問題を解消するため、メニューデータの保存挙動をデリートインサート方式に変更。

### Fixed
- ビルド時に保存済みのメニュー項目が正しく認識されず、ルートやインベントリに散らばってしまう不具合を修正。

## [0.8.0] - 2026-03-05

### Changed
- メニュー管理の内部 ID マッチングを大幅に強化、`GlobalObjectId`を最優先で参照するように変更し、アセット名変更への耐性を向上。
- 文字列キーへの依存を減らし、データの追跡精度を向上（下位互換性は維持）
- `MenuLayoutData` のシリアライズ形式と GUI スタイル定義を最適化し、エディタ実行時の GC 負荷とパフォーマンスを改善。

## [0.7.0] - 2026-03-05

### Changed
- 大規模なリファクタリングを実施
- 編集やビルド時のメニュー管理をIDベースに変更

### Fixed
- メニューの名称変更や同一メニュー存在時による内部メニュー溢れを修正

## [0.6.3] - 2026-03-04

### Changed
- レイアウトデータをBaseとExtendedに分離し、バリアントに対応

## [0.6.2] - 2026-03-04

### Changed
- 第3階層以上のデータが強制的に解体される問題を修正

## [0.6.0] - 2026-03-03

### Added
- 認証システムの中核ロジックを別アセンブリおよび DLL (`Lyra.MenuManager.Editor.Auth`) へ分離
- `MenuLayoutData` における複数レイアウト (`BaseLayout`, `ExtendedLayout`) のサポート
- 認証状態の変更を通知する `OnAuthChanged` イベントによる疎結合なアーキテクチャの導入
- インスペクター内での永続的な Foldout 状態の保存機能
- `MenuLayoutData` インスペクターにオンラインマニュアルおよびサポートへのリンクを追加
- 無料版での保存時に第3階層以上のデータが強制的に削除（解体）される問題を修正。これにより、Pro版で作成されたプロジェクトを無料版で開いた際のデータ消失やインベントリへの散乱を防ぎます。

### Changed
- レイアウトデータの保存方法をスクリプト内データコンテナからScriptableObjectへ変更
- 認証ウィンドウの UI レイアウトを改善（文字の浮き、メッセージの端切れを修正）
- プロジェクト全域の NDMF `.after` 実行順序設定を `Settings` ウィンドウに集約
- 認証アセンブリから `MenuManagerSettings` への直接参照を削除し、循環参照を回避
- コンパイルエラー防止のため、メインエディターアセンブリへの依存関係を整理

### Fixed
- カスタムフォルダのイタリック表示が正しく適用されない問題を修正
- 動的メニューや More フォルダがビルド時に正しく処理されない問題を修正
- 認証後の UI 再描画に関連する GUI エラーを解消
- 大規模なアバターでの保存処理の安定性を向上

## [0.5.0] - 2026-03-02

### Added
- package.json を追加
- 他の NDMF プラグインとの実行順序を制御する `RunAfterPlugins` 機能
- Assembly Definition (`Lyra.MenuManager`, `Lyra.MenuManager.Editor`) を導入
- 更新履歴 (`CHANGELOG.md`) を追加

### Changed
- プロジェクトのディレクトリ構造を再編成
- `MenuLayoutData` を `Scripts/` (Runtime) に分離
- エディタースクリプトを `Scripts/Editor/` に移動

## [0.3.0] - 2026-03-01

### Added
- メニューのリセット機能
- アイコン設定機能
- フォルダ用アイコン (`folder.png`) を追加
- ビルド時間用アイコン (`upload.png`) を追加
- 超過フォルダ用アイコン (`overflow.png`) を追加

### Changed
- リセット動作の挙動を改善
- Extra Optionの動作を修正
- アイコン画像のサイズを最適化

### Fixed
- Extra Option周りの不具合を修正

## [0.2.0] - 2026-03-01

### Fixed
- ビルド時のVRCSDK内エラーメッセージ表示を修正
- メニューアイテムの重複問題を修正
- ビルド時間関連の処理を修正

## [0.1.0] - 2026-02-28

### Added
- αリリース
