#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace toshi.VLiveKit.TestAssetsContainer.Editor
{
    public sealed class GameViewScreenshotWindow : EditorWindow
    {
        private enum CaptureFrameStyle
        {
            None,
            Cinemascope,
            Photo
        }

        private enum OutputResolutionPreset
        {
            Current,
            FullHD1080p,
            UltraHD4K,
            Custom
        }

        private enum ResizeFitMode
        {
            Fit,
            Fill,
            Stretch
        }

        private const string MenuRoot = "toshi/VLiveKit/Test Assets/Game View Screenshot";
        private const string DefaultOutputFolder = "Captures/GameView";
        private const string DefaultFilePrefix = "GameView";
        private const string OutputFolderPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.OutputFolder";
        private const string FilePrefixPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.FilePrefix";
        private const string IncludeSceneNamePrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.IncludeSceneName";
        private const string OutputResolutionPresetPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.OutputResolutionPreset";
        private const string ResizeFitModePrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.ResizeFitMode";
        private const string CustomOutputWidthPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.CustomOutputWidth";
        private const string CustomOutputHeightPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.CustomOutputHeight";
        private const string LegacyAddLetterboxMetadataPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.AddLetterboxMetadata";
        private const string FrameStylePrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.FrameStyle";
        private const string ShowFrameMetadataPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.ShowFrameMetadata";
        private const string LetterboxHeightPercentPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.LetterboxHeightPercent";
        private const string PhotoWhiteMarginPercentPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.PhotoWhiteMarginPercent";
        private const string PhotoBlackBorderPercentPrefsKey = "toshi.VLiveKit.TestAssetsContainer.GameViewScreenshot.PhotoBlackBorderPercent";

        private string outputFolder = DefaultOutputFolder;
        private string filePrefix = DefaultFilePrefix;
        private bool includeSceneName = true;
        private OutputResolutionPreset outputResolutionPreset = OutputResolutionPreset.Current;
        private ResizeFitMode resizeFitMode = ResizeFitMode.Fit;
        private int customOutputWidth = 1920;
        private int customOutputHeight = 1080;
        private CaptureFrameStyle frameStyle = CaptureFrameStyle.None;
        private bool showFrameMetadata = true;
        private float letterboxHeightPercent = 10f;
        private float photoWhiteMarginPercent = 7f;
        private float photoBlackBorderPercent = 0.8f;
        private Camera metadataCamera;
        private string statusMessage = "Ready.";
        private string pendingCapturePath;
        private string lastCapturePath;
        private double pendingCaptureStartTime;
        private GameViewStateSnapshot pendingGameViewState;
        private GUIStyle statusStyle;
        private GUIStyle panelStyle;

        [MenuItem(MenuRoot + "/Open")]
        public static void Open()
        {
            GetWindow<GameViewScreenshotWindow>("Game View Screenshot");
        }

        [MenuItem(MenuRoot + "/Capture Now")]
        public static void CaptureNow()
        {
            var window = GetWindow<GameViewScreenshotWindow>("Game View Screenshot");
            window.CaptureGameView();
        }

        private void OnEnable()
        {
            outputFolder = EditorPrefs.GetString(OutputFolderPrefsKey, DefaultOutputFolder);
            filePrefix = EditorPrefs.GetString(FilePrefixPrefsKey, DefaultFilePrefix);
            includeSceneName = EditorPrefs.GetBool(IncludeSceneNamePrefsKey, true);
            outputResolutionPreset = LoadEnumPreference(OutputResolutionPresetPrefsKey, OutputResolutionPreset.Current);
            resizeFitMode = LoadEnumPreference(ResizeFitModePrefsKey, ResizeFitMode.Fit);
            customOutputWidth = EditorPrefs.GetInt(CustomOutputWidthPrefsKey, 1920);
            customOutputHeight = EditorPrefs.GetInt(CustomOutputHeightPrefsKey, 1080);
            frameStyle = LoadFrameStylePreference();
            showFrameMetadata = EditorPrefs.GetBool(ShowFrameMetadataPrefsKey, true);
            letterboxHeightPercent = EditorPrefs.GetFloat(LetterboxHeightPercentPrefsKey, 10f);
            photoWhiteMarginPercent = EditorPrefs.GetFloat(PhotoWhiteMarginPercentPrefsKey, 7f);
            photoBlackBorderPercent = EditorPrefs.GetFloat(PhotoBlackBorderPercentPrefsKey, 0.8f);
        }

        private void OnDisable()
        {
            RestorePendingGameViewState(true);
            EditorApplication.update -= WatchPendingCapture;
        }

        private void OnGUI()
        {
            EnsureStyles();

            EditorGUILayout.LabelField("Game View Screenshot", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Capture the current Game View from the Unity Editor without entering Play Mode.", statusStyle);
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(panelStyle))
            {
                EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    outputFolder = EditorGUILayout.TextField("Folder", outputFolder);
                    if (GUILayout.Button("Browse", GUILayout.Width(76)))
                        BrowseOutputFolder();
                }

                filePrefix = EditorGUILayout.TextField("File Prefix", filePrefix);
                includeSceneName = EditorGUILayout.Toggle("Include Scene Name", includeSceneName);
            }

            using (new EditorGUILayout.VerticalScope(panelStyle))
            {
                EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
                outputResolutionPreset = (OutputResolutionPreset)EditorGUILayout.EnumPopup("Output", outputResolutionPreset);

                using (new EditorGUI.DisabledScope(outputResolutionPreset == OutputResolutionPreset.Current))
                {
                    resizeFitMode = (ResizeFitMode)EditorGUILayout.EnumPopup("Fit Mode", resizeFitMode);

                    if (outputResolutionPreset == OutputResolutionPreset.Custom)
                    {
                        customOutputWidth = Mathf.Clamp(EditorGUILayout.IntField("Width", customOutputWidth), 16, 16384);
                        customOutputHeight = Mathf.Clamp(EditorGUILayout.IntField("Height", customOutputHeight), 16, 16384);
                    }

                    Vector2Int outputSize = GetTargetOutputSize();
                    EditorGUILayout.LabelField("Target", outputResolutionPreset == OutputResolutionPreset.Current ? "Current Game View" : $"{outputSize.x} x {outputSize.y}", statusStyle);
                }
            }

            using (new EditorGUILayout.VerticalScope(panelStyle))
            {
                EditorGUILayout.LabelField("Capture Style", EditorStyles.boldLabel);
                frameStyle = (CaptureFrameStyle)EditorGUILayout.EnumPopup("Style", frameStyle);

                using (new EditorGUI.DisabledScope(frameStyle == CaptureFrameStyle.None))
                {
                    switch (frameStyle)
                    {
                        case CaptureFrameStyle.Cinemascope:
                            letterboxHeightPercent = EditorGUILayout.Slider("Bar Height", letterboxHeightPercent, 4f, 18f);
                            break;
                        case CaptureFrameStyle.Photo:
                            photoWhiteMarginPercent = EditorGUILayout.Slider("White Margin", photoWhiteMarginPercent, 3f, 16f);
                            photoBlackBorderPercent = EditorGUILayout.Slider("Black Border", photoBlackBorderPercent, 0.3f, 2.5f);
                            break;
                    }

                    showFrameMetadata = EditorGUILayout.Toggle("Show Metadata", showFrameMetadata);
                    using (new EditorGUI.DisabledScope(!showFrameMetadata))
                    {
                        metadataCamera = (Camera)EditorGUILayout.ObjectField("Camera", metadataCamera, typeof(Camera), true);
                        EditorGUILayout.LabelField("Camera", metadataCamera != null ? metadataCamera.name : "Auto: Main Camera or highest-depth enabled camera", statusStyle);
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope(panelStyle))
            {
                EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(pendingCapturePath)))
                {
                    if (GUILayout.Button("Capture Game View", GUILayout.Height(34)))
                        CaptureGameView();
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(lastCapturePath) || !File.Exists(lastCapturePath)))
                {
                    if (GUILayout.Button("Reveal Last Capture", GUILayout.Height(24)))
                        EditorUtility.RevealInFinder(lastCapturePath);
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(statusMessage, statusStyle);
        }

        private void CaptureGameView()
        {
            SavePreferences();

            string folderPath = ResolveOutputFolder(outputFolder);
            string fileName = BuildFileName(filePrefix, includeSceneName);
            string capturePath = Path.Combine(folderPath, fileName);

            try
            {
                Directory.CreateDirectory(folderPath);
            }
            catch (Exception exception)
            {
                statusMessage = $"Could not create output folder. {exception.Message}";
                Debug.Log($"[VLiveKit] Game View screenshot output folder was unavailable: {exception.Message}");
                Repaint();
                return;
            }

            pendingGameViewState = GameViewStateSnapshot.Capture();
            pendingCapturePath = capturePath;
            pendingCaptureStartTime = EditorApplication.timeSinceStartup;
            statusMessage = $"Capturing Game View to {capturePath}";
            ShowNotification(new GUIContent("Capturing Game View"));
            Repaint();

            InternalEditorUtility.RepaintAllViews();
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += () => CaptureScreenshot(capturePath);

            EditorApplication.update -= WatchPendingCapture;
            EditorApplication.update += WatchPendingCapture;
        }

        private void WatchPendingCapture()
        {
            if (string.IsNullOrEmpty(pendingCapturePath))
            {
                EditorApplication.update -= WatchPendingCapture;
                return;
            }

            if (File.Exists(pendingCapturePath))
            {
                lastCapturePath = pendingCapturePath;
                pendingCapturePath = null;
                RestorePendingGameViewState(true);
                bool resized = TryResizeCapture(lastCapturePath);
                bool appliedStyle = TryApplyCaptureStyle(lastCapturePath);
                statusMessage = $"Saved {lastCapturePath}";
                ImportCaptureIfNeeded(lastCapturePath);
                ShowNotification(new GUIContent(resized || appliedStyle ? "Game View screenshot saved with options" : "Game View screenshot saved"));
                Debug.Log(resized || appliedStyle
                    ? $"[VLiveKit] Saved Game View screenshot with options. Resolution: {GetResolutionLogLabel(resized)}. Style: {frameStyle}. {lastCapturePath}"
                    : $"[VLiveKit] Saved Game View screenshot: {lastCapturePath}");
                Repaint();
                EditorApplication.update -= WatchPendingCapture;
                return;
            }

            double elapsedSeconds = EditorApplication.timeSinceStartup - pendingCaptureStartTime;
            if (elapsedSeconds > 30d)
            {
                string timedOutPath = pendingCapturePath;
                pendingCapturePath = null;
                RestorePendingGameViewState(true);
                statusMessage = $"Capture request finished, but the file was not found. {timedOutPath}";
                Debug.Log($"[VLiveKit] Game View screenshot file was not found after capture request: {timedOutPath}");
                Repaint();
                EditorApplication.update -= WatchPendingCapture;
                return;
            }

            if (elapsedSeconds > 8d)
            {
                statusMessage = $"Capture requested. Waiting for Unity to write {pendingCapturePath}";
                Repaint();
            }
        }

        private void CaptureScreenshot(string capturePath)
        {
            try
            {
                ScreenCapture.CaptureScreenshot(capturePath, 1);
                RestorePendingGameViewState(false);
            }
            catch (Exception exception)
            {
                if (pendingCapturePath == capturePath)
                    pendingCapturePath = null;

                RestorePendingGameViewState(true);
                statusMessage = $"Could not request Game View screenshot. {exception.Message}";
                Debug.Log($"[VLiveKit] Game View screenshot request failed: {exception.Message}");
                Repaint();
                EditorApplication.update -= WatchPendingCapture;
            }
        }

        private void BrowseOutputFolder()
        {
            string currentPath = ResolveOutputFolder(outputFolder);
            string selectedPath = EditorUtility.OpenFolderPanel("Game View Screenshot Folder", currentPath, "");
            if (string.IsNullOrEmpty(selectedPath))
                return;

            outputFolder = ToProjectRelativePath(selectedPath);
            SavePreferences();
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(OutputFolderPrefsKey, string.IsNullOrWhiteSpace(outputFolder) ? DefaultOutputFolder : outputFolder.Trim());
            EditorPrefs.SetString(FilePrefixPrefsKey, string.IsNullOrWhiteSpace(filePrefix) ? DefaultFilePrefix : filePrefix.Trim());
            EditorPrefs.SetBool(IncludeSceneNamePrefsKey, includeSceneName);
            EditorPrefs.SetInt(OutputResolutionPresetPrefsKey, (int)outputResolutionPreset);
            EditorPrefs.SetInt(ResizeFitModePrefsKey, (int)resizeFitMode);
            EditorPrefs.SetInt(CustomOutputWidthPrefsKey, Mathf.Clamp(customOutputWidth, 16, 16384));
            EditorPrefs.SetInt(CustomOutputHeightPrefsKey, Mathf.Clamp(customOutputHeight, 16, 16384));
            EditorPrefs.SetInt(FrameStylePrefsKey, (int)frameStyle);
            EditorPrefs.SetBool(ShowFrameMetadataPrefsKey, showFrameMetadata);
            EditorPrefs.SetFloat(LetterboxHeightPercentPrefsKey, Mathf.Clamp(letterboxHeightPercent, 4f, 18f));
            EditorPrefs.SetFloat(PhotoWhiteMarginPercentPrefsKey, Mathf.Clamp(photoWhiteMarginPercent, 3f, 16f));
            EditorPrefs.SetFloat(PhotoBlackBorderPercentPrefsKey, Mathf.Clamp(photoBlackBorderPercent, 0.3f, 2.5f));
        }

        private void RestorePendingGameViewState(bool clearState)
        {
            pendingGameViewState?.Restore();
            if (clearState)
                pendingGameViewState = null;
        }

        private static string BuildFileName(string prefix, bool useSceneName)
        {
            string safePrefix = SanitizeFileName(string.IsNullOrWhiteSpace(prefix) ? DefaultFilePrefix : prefix.Trim());
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            if (!useSceneName)
                return $"{safePrefix}_{timestamp}.png";

            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(sceneName))
                sceneName = "Untitled";

            return $"{safePrefix}_{SanitizeFileName(sceneName)}_{timestamp}.png";
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');

            return value.Replace(' ', '_');
        }

        private static string ResolveOutputFolder(string folder)
        {
            string trimmedFolder = string.IsNullOrWhiteSpace(folder) ? DefaultOutputFolder : folder.Trim();
            if (Path.IsPathRooted(trimmedFolder))
                return Path.GetFullPath(trimmedFolder);

            return Path.GetFullPath(Path.Combine(GetProjectRoot(), trimmedFolder));
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            string fullPath = Path.GetFullPath(absolutePath);
            string projectRoot = GetProjectRoot();
            string rootWithSeparator = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            return fullPath.Substring(rootWithSeparator.Length).Replace('\\', '/');
        }

        private static string GetProjectRoot()
        {
            DirectoryInfo assetsDirectory = Directory.GetParent(Application.dataPath);
            return assetsDirectory != null ? assetsDirectory.FullName : Directory.GetCurrentDirectory();
        }

        private static void ImportCaptureIfNeeded(string capturePath)
        {
            string relativePath = ToProjectRelativePath(capturePath);
            if (!relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return;

            AssetDatabase.ImportAsset(relativePath);
        }

        private static TEnum LoadEnumPreference<TEnum>(string key, TEnum fallback) where TEnum : struct, Enum
        {
            int savedValue = EditorPrefs.GetInt(key, Convert.ToInt32(fallback, CultureInfo.InvariantCulture));
            return Enum.IsDefined(typeof(TEnum), savedValue) ? (TEnum)(object)savedValue : fallback;
        }

        private Vector2Int GetTargetOutputSize()
        {
            switch (outputResolutionPreset)
            {
                case OutputResolutionPreset.FullHD1080p:
                    return new Vector2Int(1920, 1080);
                case OutputResolutionPreset.UltraHD4K:
                    return new Vector2Int(3840, 2160);
                case OutputResolutionPreset.Custom:
                    return new Vector2Int(Mathf.Clamp(customOutputWidth, 16, 16384), Mathf.Clamp(customOutputHeight, 16, 16384));
                default:
                    return Vector2Int.zero;
            }
        }

        private string GetResolutionLogLabel(bool resized)
        {
            if (!resized)
                return "Current";

            Vector2Int targetSize = GetTargetOutputSize();
            return $"{targetSize.x}x{targetSize.y} {resizeFitMode}";
        }

        private bool TryResizeCapture(string capturePath)
        {
            if (outputResolutionPreset == OutputResolutionPreset.Current)
                return false;

            Vector2Int targetSize = GetTargetOutputSize();
            if (targetSize.x <= 0 || targetSize.y <= 0)
                return false;

            try
            {
                ResizeCapture(capturePath, targetSize.x, targetSize.y, resizeFitMode);
                return true;
            }
            catch (Exception exception)
            {
                Debug.Log($"[VLiveKit] Game View screenshot resize could not be applied: {exception.Message}");
                return false;
            }
        }

        private static void ResizeCapture(string capturePath, int targetWidth, int targetHeight, ResizeFitMode fitMode)
        {
            byte[] sourceBytes = File.ReadAllBytes(capturePath);
            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D resizedTexture = null;

            try
            {
                if (!ImageConversion.LoadImage(sourceTexture, sourceBytes))
                    throw new InvalidOperationException("PNG could not be loaded.");

                int sourceWidth = sourceTexture.width;
                int sourceHeight = sourceTexture.height;
                if (sourceWidth == targetWidth && sourceHeight == targetHeight)
                    return;

                Color32[] sourcePixels = sourceTexture.GetPixels32();
                Color32[] targetPixels = new Color32[targetWidth * targetHeight];
                var black = new Color32(0, 0, 0, 255);

                for (int i = 0; i < targetPixels.Length; i++)
                    targetPixels[i] = black;

                switch (fitMode)
                {
                    case ResizeFitMode.Stretch:
                        ResampleStretch(sourcePixels, sourceWidth, sourceHeight, targetPixels, targetWidth, targetHeight);
                        break;
                    case ResizeFitMode.Fill:
                        ResampleFill(sourcePixels, sourceWidth, sourceHeight, targetPixels, targetWidth, targetHeight);
                        break;
                    default:
                        ResampleFit(sourcePixels, sourceWidth, sourceHeight, targetPixels, targetWidth, targetHeight);
                        break;
                }

                resizedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
                resizedTexture.SetPixels32(targetPixels);
                resizedTexture.Apply(false, false);
                File.WriteAllBytes(capturePath, resizedTexture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceTexture);
                if (resizedTexture != null)
                    UnityEngine.Object.DestroyImmediate(resizedTexture);
            }
        }

        private static CaptureFrameStyle LoadFrameStylePreference()
        {
            if (EditorPrefs.HasKey(FrameStylePrefsKey))
            {
                int savedValue = EditorPrefs.GetInt(FrameStylePrefsKey, (int)CaptureFrameStyle.None);
                if (Enum.IsDefined(typeof(CaptureFrameStyle), savedValue))
                    return (CaptureFrameStyle)savedValue;
            }

            return EditorPrefs.GetBool(LegacyAddLetterboxMetadataPrefsKey, false)
                ? CaptureFrameStyle.Cinemascope
                : CaptureFrameStyle.None;
        }

        private bool TryApplyCaptureStyle(string capturePath)
        {
            if (frameStyle == CaptureFrameStyle.None)
                return false;

            try
            {
                switch (frameStyle)
                {
                    case CaptureFrameStyle.Cinemascope:
                        ApplyCinemascopeFrame(capturePath);
                        return true;
                    case CaptureFrameStyle.Photo:
                        ApplyPhotoFrame(capturePath);
                        return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Debug.Log($"[VLiveKit] Game View screenshot style could not be applied: {exception.Message}");
                return false;
            }
        }

        private void ApplyCinemascopeFrame(string capturePath)
        {
            byte[] sourceBytes = File.ReadAllBytes(capturePath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, sourceBytes))
                    throw new InvalidOperationException("PNG could not be loaded.");

                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;
                int barHeight = Mathf.Clamp(Mathf.RoundToInt(height * Mathf.Clamp(letterboxHeightPercent, 4f, 18f) / 100f), 12, Mathf.Max(12, height / 3));

                var black = new Color32(0, 0, 0, 255);
                FillRect(pixels, width, height, 0, 0, width, barHeight, black);
                FillRect(pixels, width, height, 0, height - barHeight, width, barHeight, black);

                int scale = Mathf.Clamp(Mathf.RoundToInt(height / 360f), 2, 6);
                int margin = Mathf.Max(8, scale * 5);
                int lineHeight = 8 * scale;
                int maxTextWidth = Mathf.Max(1, width - margin * 2);
                var white = new Color32(255, 255, 255, 255);

                if (showFrameMetadata)
                {
                    string sceneLine = $"SCENE {GetSceneDisplayName()}";
                    string cameraLine = BuildCameraMetadataLine(GetMetadataCamera());

                    DrawPixelText(pixels, width, height, margin, margin, TrimToWidth(sceneLine, maxTextWidth, scale), scale, white);
                    DrawPixelText(pixels, width, height, margin, height - barHeight + margin, TrimToWidth(cameraLine, maxTextWidth, scale), scale, white);

                    if (barHeight >= margin * 2 + lineHeight * 2)
                    {
                        string imageLine = string.Format(CultureInfo.InvariantCulture, "IMAGE {0} X {1} PX", width, height);
                        DrawPixelText(pixels, width, height, margin, height - barHeight + margin + lineHeight, TrimToWidth(imageLine, maxTextWidth, scale), scale, white);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(capturePath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private void ApplyPhotoFrame(string capturePath)
        {
            byte[] sourceBytes = File.ReadAllBytes(capturePath);
            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D framedTexture = null;

            try
            {
                if (!ImageConversion.LoadImage(sourceTexture, sourceBytes))
                    throw new InvalidOperationException("PNG could not be loaded.");

                int sourceWidth = sourceTexture.width;
                int sourceHeight = sourceTexture.height;
                int baseSize = Mathf.Min(sourceWidth, sourceHeight);
                int scale = Mathf.Clamp(Mathf.RoundToInt(sourceHeight / 360f), 2, 6);
                int textMargin = Mathf.Max(8, scale * 5);
                int lineHeight = 8 * scale;
                int whiteMargin = Mathf.Clamp(Mathf.RoundToInt(baseSize * Mathf.Clamp(photoWhiteMarginPercent, 3f, 16f) / 100f), 16, Mathf.Max(16, baseSize / 3));

                if (showFrameMetadata)
                    whiteMargin = Mathf.Max(whiteMargin, textMargin * 2 + lineHeight);

                int blackBorder = Mathf.Clamp(Mathf.RoundToInt(baseSize * Mathf.Clamp(photoBlackBorderPercent, 0.3f, 2.5f) / 100f), 2, Mathf.Max(2, baseSize / 20));
                int offset = whiteMargin + blackBorder;
                int targetWidth = sourceWidth + offset * 2;
                int targetHeight = sourceHeight + offset * 2;

                Color32[] sourcePixels = sourceTexture.GetPixels32();
                Color32[] targetPixels = new Color32[targetWidth * targetHeight];
                var white = new Color32(255, 255, 255, 255);
                var black = new Color32(0, 0, 0, 255);

                for (int i = 0; i < targetPixels.Length; i++)
                    targetPixels[i] = white;

                FillRect(targetPixels, targetWidth, targetHeight, whiteMargin, whiteMargin, sourceWidth + blackBorder * 2, sourceHeight + blackBorder * 2, black);
                CopyPixels(sourcePixels, sourceWidth, sourceHeight, targetPixels, targetWidth, offset, offset);

                if (showFrameMetadata)
                {
                    int maxTextWidth = Mathf.Max(1, targetWidth - whiteMargin * 2);
                    string sceneLine = $"SCENE {GetSceneDisplayName()}";
                    string cameraLine = BuildCameraMetadataLine(GetMetadataCamera());

                    DrawPixelText(targetPixels, targetWidth, targetHeight, whiteMargin, Mathf.Max(4, (whiteMargin - lineHeight) / 2), TrimToWidth(sceneLine, maxTextWidth, scale), scale, black);
                    DrawPixelText(targetPixels, targetWidth, targetHeight, whiteMargin, targetHeight - whiteMargin + Mathf.Max(4, (whiteMargin - lineHeight) / 2), TrimToWidth(cameraLine, maxTextWidth, scale), scale, black);
                }

                framedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
                framedTexture.SetPixels32(targetPixels);
                framedTexture.Apply(false, false);
                File.WriteAllBytes(capturePath, framedTexture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceTexture);
                if (framedTexture != null)
                    UnityEngine.Object.DestroyImmediate(framedTexture);
            }
        }

        private string BuildCameraMetadataLine(Camera camera)
        {
            if (camera == null)
                return "CAMERA NONE";

            Vector2 sensorSize = camera.sensorSize;
            float focalLength = camera.usePhysicalProperties
                ? camera.focalLength
                : SensorHeightToFocalLength(camera.fieldOfView, sensorSize.y);

            return string.Format(
                CultureInfo.InvariantCulture,
                "CAMERA {0} | SENSOR {1:0.0} X {2:0.0} MM | LENS {3:0.0} MM | FOV {4:0.0} DEG | {5}",
                camera.name,
                sensorSize.x,
                sensorSize.y,
                focalLength,
                camera.fieldOfView,
                camera.usePhysicalProperties ? "PHYSICAL CAMERA" : "FOV DERIVED");
        }

        private Camera GetMetadataCamera()
        {
            if (metadataCamera != null && metadataCamera.isActiveAndEnabled)
                return metadataCamera;

            if (Camera.main != null && Camera.main.isActiveAndEnabled)
                return Camera.main;

            Camera bestCamera = null;
#if UNITY_2023_1_OR_NEWER
            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
#endif
            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.isActiveAndEnabled)
                    continue;

                if (bestCamera == null || camera.depth > bestCamera.depth)
                    bestCamera = camera;
            }

            return bestCamera;
        }

        private static float SensorHeightToFocalLength(float verticalFovDegrees, float sensorHeightMm)
        {
            float halfFovRadians = Mathf.Max(0.01f, verticalFovDegrees) * Mathf.Deg2Rad * 0.5f;
            return sensorHeightMm / (2f * Mathf.Tan(halfFovRadians));
        }

        private static string GetSceneDisplayName()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return string.IsNullOrWhiteSpace(sceneName) ? "Untitled" : sceneName;
        }

        private static void FillRect(Color32[] pixels, int textureWidth, int textureHeight, int x, int y, int width, int height, Color32 color)
        {
            int minX = Mathf.Clamp(x, 0, textureWidth);
            int maxX = Mathf.Clamp(x + width, 0, textureWidth);
            int minY = Mathf.Clamp(y, 0, textureHeight);
            int maxY = Mathf.Clamp(y + height, 0, textureHeight);

            for (int row = minY; row < maxY; row++)
            {
                int rowStart = row * textureWidth;
                for (int column = minX; column < maxX; column++)
                    pixels[rowStart + column] = color;
            }
        }

        private static void CopyPixels(Color32[] sourcePixels, int sourceWidth, int sourceHeight, Color32[] targetPixels, int targetWidth, int targetX, int targetY)
        {
            for (int row = 0; row < sourceHeight; row++)
            {
                int sourceRowStart = row * sourceWidth;
                int targetRowStart = (targetY + row) * targetWidth + targetX;
                Array.Copy(sourcePixels, sourceRowStart, targetPixels, targetRowStart, sourceWidth);
            }
        }

        private static void ResampleStretch(Color32[] sourcePixels, int sourceWidth, int sourceHeight, Color32[] targetPixels, int targetWidth, int targetHeight)
        {
            float scaleX = sourceWidth / (float)targetWidth;
            float scaleY = sourceHeight / (float)targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                float sourceY = (y + 0.5f) * scaleY - 0.5f;
                int targetRowStart = y * targetWidth;
                for (int x = 0; x < targetWidth; x++)
                {
                    float sourceX = (x + 0.5f) * scaleX - 0.5f;
                    targetPixels[targetRowStart + x] = SampleBilinear(sourcePixels, sourceWidth, sourceHeight, sourceX, sourceY);
                }
            }
        }

        private static void ResampleFit(Color32[] sourcePixels, int sourceWidth, int sourceHeight, Color32[] targetPixels, int targetWidth, int targetHeight)
        {
            float scale = Mathf.Min(targetWidth / (float)sourceWidth, targetHeight / (float)sourceHeight);
            int drawWidth = Mathf.Clamp(Mathf.RoundToInt(sourceWidth * scale), 1, targetWidth);
            int drawHeight = Mathf.Clamp(Mathf.RoundToInt(sourceHeight * scale), 1, targetHeight);
            int offsetX = (targetWidth - drawWidth) / 2;
            int offsetY = (targetHeight - drawHeight) / 2;
            float inverseScaleX = sourceWidth / (float)drawWidth;
            float inverseScaleY = sourceHeight / (float)drawHeight;

            for (int y = 0; y < drawHeight; y++)
            {
                float sourceY = (y + 0.5f) * inverseScaleY - 0.5f;
                int targetRowStart = (offsetY + y) * targetWidth + offsetX;
                for (int x = 0; x < drawWidth; x++)
                {
                    float sourceX = (x + 0.5f) * inverseScaleX - 0.5f;
                    targetPixels[targetRowStart + x] = SampleBilinear(sourcePixels, sourceWidth, sourceHeight, sourceX, sourceY);
                }
            }
        }

        private static void ResampleFill(Color32[] sourcePixels, int sourceWidth, int sourceHeight, Color32[] targetPixels, int targetWidth, int targetHeight)
        {
            float scale = Mathf.Max(targetWidth / (float)sourceWidth, targetHeight / (float)sourceHeight);
            float sourceRectWidth = targetWidth / scale;
            float sourceRectHeight = targetHeight / scale;
            float sourceStartX = (sourceWidth - sourceRectWidth) * 0.5f;
            float sourceStartY = (sourceHeight - sourceRectHeight) * 0.5f;
            float inverseScale = 1f / scale;

            for (int y = 0; y < targetHeight; y++)
            {
                float sourceY = sourceStartY + (y + 0.5f) * inverseScale - 0.5f;
                int targetRowStart = y * targetWidth;
                for (int x = 0; x < targetWidth; x++)
                {
                    float sourceX = sourceStartX + (x + 0.5f) * inverseScale - 0.5f;
                    targetPixels[targetRowStart + x] = SampleBilinear(sourcePixels, sourceWidth, sourceHeight, sourceX, sourceY);
                }
            }
        }

        private static Color32 SampleBilinear(Color32[] pixels, int width, int height, float x, float y)
        {
            x = Mathf.Clamp(x, 0f, width - 1f);
            y = Mathf.Clamp(y, 0f, height - 1f);

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            float tx = x - x0;
            float ty = y - y0;

            Color32 c00 = pixels[y0 * width + x0];
            Color32 c10 = pixels[y0 * width + x1];
            Color32 c01 = pixels[y1 * width + x0];
            Color32 c11 = pixels[y1 * width + x1];

            float r0 = Mathf.Lerp(c00.r, c10.r, tx);
            float r1 = Mathf.Lerp(c01.r, c11.r, tx);
            float g0 = Mathf.Lerp(c00.g, c10.g, tx);
            float g1 = Mathf.Lerp(c01.g, c11.g, tx);
            float b0 = Mathf.Lerp(c00.b, c10.b, tx);
            float b1 = Mathf.Lerp(c01.b, c11.b, tx);
            float a0 = Mathf.Lerp(c00.a, c10.a, tx);
            float a1 = Mathf.Lerp(c01.a, c11.a, tx);

            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(r0, r1, ty)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(g0, g1, ty)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(b0, b1, ty)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a0, a1, ty)));
        }

        private static string TrimToWidth(string text, int maxWidth, int scale)
        {
            if (MeasurePixelTextWidth(text, scale) <= maxWidth)
                return text;

            const string suffix = "...";
            int suffixWidth = MeasurePixelTextWidth(suffix, scale);
            while (!string.IsNullOrEmpty(text) && MeasurePixelTextWidth(text, scale) + suffixWidth > maxWidth)
                text = text.Substring(0, text.Length - 1);

            return string.IsNullOrEmpty(text) ? suffix : text + suffix;
        }

        private static int MeasurePixelTextWidth(string text, int scale)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int width = 0;
            foreach (char character in text)
                width += GetGlyphWidth(char.ToUpperInvariant(character)) * scale + scale;

            return Mathf.Max(0, width - scale);
        }

        private static void DrawPixelText(Color32[] pixels, int textureWidth, int textureHeight, int x, int yFromTop, string text, int scale, Color32 color)
        {
            int cursorX = x;
            foreach (char sourceCharacter in text)
            {
                char character = char.ToUpperInvariant(sourceCharacter);
                string[] glyph = GetGlyph(character);
                int glyphWidth = GetGlyphWidth(character);

                for (int row = 0; row < glyph.Length; row++)
                {
                    string glyphRow = glyph[row];
                    for (int column = 0; column < Mathf.Min(glyphWidth, glyphRow.Length); column++)
                    {
                        if (glyphRow[column] == ' ')
                            continue;

                        FillRectFromTop(pixels, textureWidth, textureHeight, cursorX + column * scale, yFromTop + row * scale, scale, scale, color);
                    }
                }

                cursorX += (glyphWidth + 1) * scale;
            }
        }

        private static void FillRectFromTop(Color32[] pixels, int textureWidth, int textureHeight, int x, int yFromTop, int width, int height, Color32 color)
        {
            int textureY = textureHeight - yFromTop - height;
            FillRect(pixels, textureWidth, textureHeight, x, textureY, width, height, color);
        }

        private static int GetGlyphWidth(char character)
        {
            return character == ' ' ? 3 : 5;
        }

        private static string[] GetGlyph(char character)
        {
            switch (character)
            {
                case 'A': return new[] { " XXX ", "X   X", "X   X", "XXXXX", "X   X", "X   X", "X   X" };
                case 'B': return new[] { "XXXX ", "X   X", "X   X", "XXXX ", "X   X", "X   X", "XXXX " };
                case 'C': return new[] { " XXX ", "X   X", "X    ", "X    ", "X    ", "X   X", " XXX " };
                case 'D': return new[] { "XXXX ", "X   X", "X   X", "X   X", "X   X", "X   X", "XXXX " };
                case 'E': return new[] { "XXXXX", "X    ", "X    ", "XXXX ", "X    ", "X    ", "XXXXX" };
                case 'F': return new[] { "XXXXX", "X    ", "X    ", "XXXX ", "X    ", "X    ", "X    " };
                case 'G': return new[] { " XXX ", "X   X", "X    ", "X XXX", "X   X", "X   X", " XXX " };
                case 'H': return new[] { "X   X", "X   X", "X   X", "XXXXX", "X   X", "X   X", "X   X" };
                case 'I': return new[] { "XXXXX", "  X  ", "  X  ", "  X  ", "  X  ", "  X  ", "XXXXX" };
                case 'J': return new[] { "XXXXX", "   X ", "   X ", "   X ", "   X ", "X  X ", " XX  " };
                case 'K': return new[] { "X   X", "X  X ", "X X  ", "XX   ", "X X  ", "X  X ", "X   X" };
                case 'L': return new[] { "X    ", "X    ", "X    ", "X    ", "X    ", "X    ", "XXXXX" };
                case 'M': return new[] { "X   X", "XX XX", "X X X", "X   X", "X   X", "X   X", "X   X" };
                case 'N': return new[] { "X   X", "XX  X", "X X X", "X  XX", "X   X", "X   X", "X   X" };
                case 'O': return new[] { " XXX ", "X   X", "X   X", "X   X", "X   X", "X   X", " XXX " };
                case 'P': return new[] { "XXXX ", "X   X", "X   X", "XXXX ", "X    ", "X    ", "X    " };
                case 'Q': return new[] { " XXX ", "X   X", "X   X", "X   X", "X X X", "X  X ", " XX X" };
                case 'R': return new[] { "XXXX ", "X   X", "X   X", "XXXX ", "X X  ", "X  X ", "X   X" };
                case 'S': return new[] { " XXXX", "X    ", "X    ", " XXX ", "    X", "    X", "XXXX " };
                case 'T': return new[] { "XXXXX", "  X  ", "  X  ", "  X  ", "  X  ", "  X  ", "  X  " };
                case 'U': return new[] { "X   X", "X   X", "X   X", "X   X", "X   X", "X   X", " XXX " };
                case 'V': return new[] { "X   X", "X   X", "X   X", "X   X", "X   X", " X X ", "  X  " };
                case 'W': return new[] { "X   X", "X   X", "X   X", "X   X", "X X X", "XX XX", "X   X" };
                case 'X': return new[] { "X   X", "X   X", " X X ", "  X  ", " X X ", "X   X", "X   X" };
                case 'Y': return new[] { "X   X", "X   X", " X X ", "  X  ", "  X  ", "  X  ", "  X  " };
                case 'Z': return new[] { "XXXXX", "    X", "   X ", "  X  ", " X   ", "X    ", "XXXXX" };
                case '0': return new[] { " XXX ", "X   X", "X  XX", "X X X", "XX  X", "X   X", " XXX " };
                case '1': return new[] { "  X  ", " XX  ", "  X  ", "  X  ", "  X  ", "  X  ", " XXX " };
                case '2': return new[] { " XXX ", "X   X", "    X", "   X ", "  X  ", " X   ", "XXXXX" };
                case '3': return new[] { "XXXX ", "    X", "    X", " XXX ", "    X", "    X", "XXXX " };
                case '4': return new[] { "   X ", "  XX ", " X X ", "X  X ", "XXXXX", "   X ", "   X " };
                case '5': return new[] { "XXXXX", "X    ", "X    ", "XXXX ", "    X", "    X", "XXXX " };
                case '6': return new[] { " XXX ", "X    ", "X    ", "XXXX ", "X   X", "X   X", " XXX " };
                case '7': return new[] { "XXXXX", "    X", "   X ", "  X  ", " X   ", " X   ", " X   " };
                case '8': return new[] { " XXX ", "X   X", "X   X", " XXX ", "X   X", "X   X", " XXX " };
                case '9': return new[] { " XXX ", "X   X", "X   X", " XXXX", "    X", "    X", " XXX " };
                case ':': return new[] { "     ", "  X  ", "  X  ", "     ", "  X  ", "  X  ", "     " };
                case '.': return new[] { "     ", "     ", "     ", "     ", "     ", " XX  ", " XX  " };
                case ',': return new[] { "     ", "     ", "     ", "     ", " XX  ", " XX  ", " X   " };
                case '-': return new[] { "     ", "     ", "     ", " XXX ", "     ", "     ", "     " };
                case '_': return new[] { "     ", "     ", "     ", "     ", "     ", "     ", "XXXXX" };
                case '/': return new[] { "    X", "   X ", "   X ", "  X  ", " X   ", " X   ", "X    " };
                case '|': return new[] { "  X  ", "  X  ", "  X  ", "  X  ", "  X  ", "  X  ", "  X  " };
                case '(': return new[] { "   X ", "  X  ", " X   ", " X   ", " X   ", "  X  ", "   X " };
                case ')': return new[] { " X   ", "  X  ", "   X ", "   X ", "   X ", "  X  ", " X   " };
                case ' ': return new[] { "   ", "   ", "   ", "   ", "   ", "   ", "   " };
                default: return new[] { " XXX ", "X   X", "    X", "   X ", "  X  ", "     ", "  X  " };
            }
        }

        private void EnsureStyles()
        {
            if (statusStyle != null && panelStyle != null)
                return;

            statusStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            panelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 7, 8),
                margin = new RectOffset(4, 4, 4, 6)
            };
        }

        private sealed class GameViewStateSnapshot
        {
            private readonly EditorWindow gameView;
            private readonly Type gameViewType;
            private readonly bool hasTargetDisplay;
            private readonly int targetDisplay;
            private readonly bool hasSelectedSizeIndex;
            private readonly int selectedSizeIndex;

            private GameViewStateSnapshot(EditorWindow gameView, Type gameViewType)
            {
                this.gameView = gameView;
                this.gameViewType = gameViewType;
                hasTargetDisplay = TryReadInt(gameViewType, gameView, "targetDisplay", "m_TargetDisplay", out targetDisplay);
                hasSelectedSizeIndex = TryReadInt(gameViewType, gameView, "selectedSizeIndex", "m_SelectedSizeIndex", out selectedSizeIndex);
            }

            public static GameViewStateSnapshot Capture()
            {
                Type gameViewType = FindGameViewType();
                EditorWindow gameView = FindOpenGameView(gameViewType);
                return gameView != null && gameViewType != null ? new GameViewStateSnapshot(gameView, gameViewType) : null;
            }

            public void Restore()
            {
                if (gameView == null || gameViewType == null)
                    return;

                try
                {
                    if (hasTargetDisplay)
                        TryWriteInt(gameViewType, gameView, "targetDisplay", "m_TargetDisplay", targetDisplay);

                    if (hasSelectedSizeIndex)
                        TryWriteInt(gameViewType, gameView, "selectedSizeIndex", "m_SelectedSizeIndex", selectedSizeIndex);

                    gameView.Repaint();
                }
                catch (Exception exception)
                {
                    Debug.Log($"[VLiveKit] Game View settings could not be restored after screenshot capture: {exception.Message}");
                }
            }

            private static Type FindGameViewType()
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType("UnityEditor.GameView");
                    if (type != null)
                        return type;
                }

                return null;
            }

            private static EditorWindow FindOpenGameView(Type gameViewType)
            {
                if (gameViewType == null)
                    return null;

                foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    if (window != null && gameViewType.IsInstanceOfType(window))
                        return window;
                }

                return null;
            }

            private static bool TryReadInt(Type type, object target, string propertyName, string fieldName, out int value)
            {
                value = 0;
                var property = FindProperty(type, propertyName);
                if (property != null && property.CanRead)
                {
                    try
                    {
                        object propertyValue = property.GetValue(target, null);
                        if (propertyValue is int intValue)
                        {
                            value = intValue;
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                var field = FindField(type, fieldName);
                if (field != null)
                {
                    try
                    {
                        object fieldValue = field.GetValue(target);
                        if (fieldValue is int intValue)
                        {
                            value = intValue;
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                return false;
            }

            private static bool TryWriteInt(Type type, object target, string propertyName, string fieldName, int value)
            {
                var property = FindProperty(type, propertyName);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        property.SetValue(target, value, null);
                        return true;
                    }
                    catch (Exception)
                    {
                    }
                }

                var field = FindField(type, fieldName);
                if (field != null)
                {
                    try
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                    catch (Exception)
                    {
                    }
                }

                return false;
            }

            private static System.Reflection.PropertyInfo FindProperty(Type type, string name)
            {
                const System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic;

                for (Type current = type; current != null; current = current.BaseType)
                {
                    var property = current.GetProperty(name, flags);
                    if (property != null)
                        return property;
                }

                return null;
            }

            private static System.Reflection.FieldInfo FindField(Type type, string name)
            {
                const System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic;

                for (Type current = type; current != null; current = current.BaseType)
                {
                    var field = current.GetField(name, flags);
                    if (field != null)
                        return field;
                }

                return null;
            }
        }
    }
}
#endif
