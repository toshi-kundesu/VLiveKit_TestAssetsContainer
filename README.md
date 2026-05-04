# VLiveKit Test Assets Container

Unity でのバーチャルライブ制作、検証、デバッグに使うテスト用アセットと補助ツールをまとめた Unity プロジェクト / UPM 風パッケージです。

本番用アセットではなく、VLiveKit 周辺の開発時に「すぐ試す」「見た目や負荷を確認する」「仮素材で組む」ためのコンテナとして使います。

## 動作環境

- Unity 2022.3.9f1
- High Definition Render Pipeline 14.0.8
- TextMesh Pro 3.0.6
- Timeline 1.7.5

## 内容

主なアセットは `Assets/toshi.VLiveKit/TestAssetsContainer` に入っています。

| パス | 内容 |
| --- | --- |
| `Runtime` | 実行時に使えるデバッグ / 検証用コンポーネント |
| `Editor` | VLiveKit 向けの Editor 拡張 |
| `Scenes` | 検証用シーン |
| `HDRI` | ライティング確認用 HDRI |
| `UVCheckerTexture` | UV 確認用テクスチャ |
| `VJUI` | VJ / ライブ演出向け UI サンプル |
| `mixamo` | モーション / キャラクター確認用素材 |
| `ThirdParty` | 検証に使うサードパーティ製パッケージ |

## 主な機能

- FPS、フレーム時間、メモリ、GC Alloc、PC 状態などを表示する `FpsDisplay`
- カメラ向けのレベルゲージ、録画インジケータ、レターボックス、シーン名表示などのオーバーレイ部品
- LTC タイムコード読み取りと Timeline 同期用コンポーネント
- Timeline の AnimationTrack に Animator を割り当てる Editor 補助
- Shader の global float / color / vector parameter を管理するコンポーネント
- AudioListener 一括無効化、フレームレート設定、オブジェクト切り替えなどの検証用ユーティリティ
- HDRP の追加プロパティ表示を有効化する推奨設定メニュー
- UPM 風パッケージ構成を作る `Tools/VLiveKit/Create Package Setup`

## 使い方

1. Unity Hub からこのリポジトリを開きます。
2. Unity バージョンは `2022.3.9f1` を使用します。
3. 必要に応じて `Assets/toshi.VLiveKit/TestAssetsContainer/Scenes/FPSDisplay.unity` などの検証シーンを開きます。
4. Editor メニューの `Tools/VLiveKit/Recommended Settings/Open` から推奨設定を適用できます。

`Assets/toshi.VLiveKit/TestAssetsContainer/package.json` には UPM パッケージ風のメタ情報があります。別プロジェクトに持ち込む場合は、まずこのフォルダ単位で移植する想定です。

## Editor メニュー

| メニュー | 内容 |
| --- | --- |
| `Tools/VLiveKit/Recommended Settings/Open` | 推奨 Editor 設定ウィンドウを開く |
| `Tools/VLiveKit/Recommended Settings/Apply Recommended Settings` | HDRP の Additional Properties を表示状態にする |
| `Tools/VLiveKit/Create Package Setup` | package.json と asmdef の雛形を作成する |

## 注意

- このリポジトリには検証用、仮置き用、デバッグ用の素材が含まれます。
- 商用利用や再配布を行う場合は、同梱されている各サードパーティ素材のライセンスを必ず確認してください。
- `Library`、`Temp`、`Logs`、`UserSettings` など Unity の生成物は環境依存です。
- 一部の既存 C# コメントや Inspector 表示文字列には文字化けしている箇所があります。動作確認時は表示名に注意してください。

## サードパーティ

このリポジトリには以下のサードパーティ素材 / パッケージが含まれています。

| 名称 | 用途 | ライセンス | URL |
| --- | --- | --- | --- |
| Vat Baker | VAT 生成、AnimationClip 検証 | MIT | https://github.com/fuqunaga/VatBaker |
| NeoLowMan | ローポリ humanoid モデル | Unlicense | https://github.com/keijiro/NeoLowMan |
| lilToon | Toon shader | MIT | https://github.com/lilxyzw/lilToon |
| Sth Shader | Shader / texture utility | パッケージ内の表記を確認 | https://github.com/Santarh/SthShader |
| Poly Haven HDRI | ライティング確認用 HDRI | CC0 | https://polyhaven.com/hdris |
| VJUI | VJ / UI 検証用 prefab | Unlicense | https://github.com/keijiro/VJUI |

詳細は各フォルダ内の `LICENSE`、`LICENSE.txt`、`package.json`、または配布元を確認してください。

## ライセンス

このリポジトリ独自のコードは `LICENSE` に従い Unlicense です。

サードパーティ製の素材、シェーダー、パッケージはそれぞれのライセンスに従います。
