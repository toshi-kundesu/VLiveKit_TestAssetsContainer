# VLiveKit Test Assets Container

VLiveKit の検証、仮組み、デバッグに使う test asset と utility をまとめた package です。

## Package

- Package name: `com.toshi.vlivekit.testassetscontainer`
- Version: `0.0.15`
- Unity: 2022.3
- Repository: https://github.com/toshi-kundesu/VLiveKit_TestAssetsContainer
- Package root: `Assets/toshi.VLiveKit/TestAssetsContainer`

## 主な内容

- FPS / frame time / memory / GC などの debug overlay
- Timeline、shader parameter、camera 向けの検証 component
- HDRI、UV checker、sample UI、third-party 検証 asset

## 依存・同梱 asset

- HDRP 14.0.8
- TextMesh Pro 3.0.6
- Timeline 1.7.5

## インストール

Unity の `Packages/manifest.json` の `dependencies` に追加します。

```json
{
  "dependencies": {
    "com.toshi.vlivekit.testassetscontainer": "https://github.com/toshi-kundesu/VLiveKit_TestAssetsContainer.git?path=/Assets/toshi.VLiveKit/TestAssetsContainer#main"
  }
}
```

VLiveKit sandbox では submodule として `Packages/VLiveKit_TestAssetsContainer` に配置し、`file:` 参照で読み込んでいます。

## 注意

- 本番配布 asset ではなく、開発・検証・仮組み向けの container です。

## License

この package 独自のコードと asset は repository の `LICENSE` に従います。third-party asset を含む場合は、それぞれの license / README を確認してください。
