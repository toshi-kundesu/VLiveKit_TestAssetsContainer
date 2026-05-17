# VLiveKit Test Assets Container

VLiveKit の検証、仮組み、デバッグに使う test asset と utility をまとめた package です。

## Package

- Package name: `com.toshi.vlivekit.testassetscontainer`
- Version: `0.1.6`
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
    "com.toshi.vlivekit.testassetscontainer": "0.1.6"
  }
}
```

VLiveKit sandbox では submodule として `Packages/VLiveKit_TestAssetsContainer` に配置し、`file:` 参照で読み込んでいます。

## 注意

- 本番配布 asset ではなく、開発・検証・仮組み向けの container です。

## Editor Windows

Open the package utility windows from:

- `toshi > VLiveKit > Project > Recommended Settings > Open`
- `toshi > VLiveKit > Test Assets > Game View Screenshot > Open`
- `toshi > VLiveKit > Test Assets > Game View Screenshot > Capture Now`
- `toshi > VLiveKit > Test Assets > Timecode Monitor`
- `toshi > VLiveKit > Test Assets > Scene Description > Show Current Scene Description`

Recommended Settings shows the current project status before applying changes. The camera layer section previews which existing layers will be kept and which empty user layers will receive `cam01` through `cam08`.

Timecode Monitor can read LTC from a microphone/input device, receive OSC
timecode, and send OSC timecode directly from the editor window without entering
Play Mode. It also lists active LTC readers and OSC timecode receivers in the
current scene for checking runtime rigs.

Game View Screenshot captures the current Game View to a PNG from the Unity
Editor without entering Play Mode. It writes timestamped files under
`Captures/GameView`, optionally includes the active scene name in the filename,
and restores the Game View display and resolution selection after the capture
request.

Use `Resolution` to keep the current Game View size or resize the saved PNG to
Full HD, 4K UHD, or a custom pixel size. `Fit Mode` controls whether different
aspect ratios are padded, cropped, or stretched. Resizing is applied after the
Game View capture and before the selected frame style, so it does not change the
Unity Game View settings.

Use `Capture Style` to choose a saved-image treatment. `Cinemascope` burns
black bars into the top and bottom of the PNG. `Photo` adds a white print-style
matte around the image with a black inner border. Enable `Show Metadata` to add
the active scene name and camera metadata such as camera name, sensor size,
focal length in millimeters, field of view, and whether the value comes from
Physical Camera settings or FOV-derived conversion.

## Scene Descriptions

Add `VLiveKitSceneDescription` to any GameObject in a test scene to show scene
notes when the scene is opened in the Unity Editor. Use the `Title` and
`Description` fields to explain what the scene demonstrates, required package
setup, expected inputs, and useful controls.

The component has a `Show On Scene Open` toggle for automatic popups and a
`Show Only Once Per Editor Session` toggle for scenes that should avoid repeated
prompts. The same description can be opened manually from
`toshi > VLiveKit > Test Assets > Scene Description > Show Current Scene Description`.

## Perlin Noise Motion

Add `PerlinNoiseMotion` to a GameObject when a test scene needs simple organic
motion without writing a one-off script. By default it offsets the object's
local position with Perlin noise during Play Mode. You can also drive rotation
or scale by setting their amplitude fields.

Use `Capture Current As Base` after placing the object to make the current
transform the rest pose. Enable `Animate In Edit Mode` only when you want the
object to move while authoring a scene.

## Timeline Motion Settings Checker

Add `TimelineMotionSettingsChecker` to a scene utility GameObject and assign
the main Timeline `PlayableDirector` to `Root Director`. Use the context menu
`Check Timeline Motion Settings` or leave `Check On Awake` enabled to scan
Timeline animation clips before playback.

The checker reports Timeline `AnimationPlayableAsset` clips whose `Remove Start
Offset` option is enabled. In the Unity Editor it also checks imported model
motion clips and reports any `ModelImporter` whose `Anim. Compression` setting
is not `Off`. Enable `Include Sub Timelines` to follow nested Timeline
directors referenced by Control Tracks.

## HDRP Sample Scene

The `HDRPSampleScene` folder contains the imported Unity 3D Sample Scene
(HDRP) assets used for sandbox validation. It is kept inside this package
instead of the project-level `Assets/TestAssets` folder so the sample scene
travels with the Test Assets Container submodule.

The large imported Unity sample folders are kept in the Git repository for
sandbox/submodule validation and are intentionally excluded from the npm
tarball so `com.toshi.vlivekit.testassetscontainer` stays publishable through
the npm registry.

This sample relies on HDRP, VFX Graph, Shader Graph, Cinemachine, Timeline,
and Input System package support. See `ThirdPartyNotices/HDRPSampleScene.md`
for the source and Unity Companion License note.

## HDRP Lit Feature Test Shaders

The `HDRPLitFeatureTests` folder contains small HDRP shader probes for learning
and validating individual parts of the HDRP Lit path before building custom
Lit-based materials.

- `Point Light Receive` isolates punctual direct lighting from the HDRP Lit
  light loop.
- `Reflection Probe Receive` isolates environment specular reflection from the
  HDRP Lit light loop.
- `Full Lit Forward` keeps the same minimal surface inputs with opaque Lit
  lighting enabled.
- `Mask Map Test` displays the packed Mask Map channels: Metallic, AO, Detail
  Mask, and Smoothness.
- `Scene Color Refraction` ports the GrabPass-style refraction sample to HDRP
  by projecting the refracted point and sampling `SampleCameraColor`.
- `Sobel Rim Light` ports the depth-buffer Sobel rim approach to HDRP by
  sampling `SampleCameraDepth` from view-space offsets around the object.

## Third-party Test Utilities

- `ThirdParty/com.jasonma.outlinenormalsmoother@1.1.0` vendors
  [OutlineNormalSmoother](https://github.com/JasonMa0012/OutlineNormalSmoother)
  by Jason Ma under the MIT License. It bakes smoothed outline normals for
  imported models, using the upstream `_Outline` suffix import rule. The
  original `LICENSE`, `README.md`, and package metadata are kept with the
  vendored files.
- `ThirdParty/jp.keijiro.klutter-tools@2.6.0` vendors
  [KlutterTools](https://github.com/keijiro/KlutterTools) by Keijiro Takahashi
  under the Unlicense. It provides miscellaneous Unity Editor helpers such as
  FPS capping, VFX Graph editor tweaks, `.cube` LUT import, Inspector Note, and
  Downloader. The original `LICENSE`, `README.md`, `CHANGELOG.md`, and package
  metadata are kept with the vendored files.

## HDRP Custom Post Processes

- `GT Tone Mapping` adds a volume override that ports the Gran Turismo /
  Uchimura tone curve from `yaoling1997/GT-ToneMapping` to HDRP under the MIT
  License. The upstream notice and license text are kept in
  `ThirdParty/yaoling1997.gt-tonemapping`.

To test it, add `toshi.VLiveKit.TestAssetsContainer.GTToneMapping` to HDRP
Global Settings > Custom Post Process Orders > After Post Process Blurs, then
add the Volume Override from `Post-processing > VLiveKit > GT Tone Mapping`.
Set HDRP's built-in Tonemapping override to `None` when using this as the final
tone mapping curve.

## License

この package 独自のコードと asset は repository の `LICENSE` に従います。third-party asset を含む場合は、それぞれの license / README を確認してください。
