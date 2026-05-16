# Unity Package Samples

This folder contains imported Unity package samples used for VLiveKit sandbox
validation. They live in the Test Assets Container package instead of the
project-level `Assets/TestAssets` folder so sample dependencies and scenes travel
with the submodule.

The `UnitySamples.Editor.asmdef` file is an editor-only fallback assembly for
sample scripts that do not ship with their own assembly definition. Official
sample asmdefs inside child folders are preserved.

See `ThirdPartyNotice.md` for the Unity Companion License note.
