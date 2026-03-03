# 更新履歴 / Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
