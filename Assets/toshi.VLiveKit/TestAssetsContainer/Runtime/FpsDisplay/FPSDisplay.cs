using System;
#if !UNITY_WEBGL
using DiagnosticsProcess = System.Diagnostics.Process;
#endif
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using TMPro;

#if UNITY_2020_2_OR_NEWER
using Unity.Profiling;
#endif

[ExecuteAlways]
public class FpsDisplay : MonoBehaviour
{
    public enum DisplayMethod
    {
        OnGUI,
        UI_Text,
        UI_TextMeshPro
    }

    public enum ScreenAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    private enum LeakAlertState
    {
        None,
        Normal,
        Warning,
        Critical
    }

    private const float BytesToMb = 1f / (1024f * 1024f);
    private const float BytesToKb = 1f / 1024f;

    [Header("表示方式")]
    [SerializeField] private DisplayMethod displayMethod = DisplayMethod.OnGUI;

    [Header("計測")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.5f;
    [SerializeField] private bool measureInEditMode = false;
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("FPS / Frame")]
    [SerializeField] private bool showFps = true;
    [SerializeField] private string prefix = "FPS: ";
    [SerializeField] private string separator = "  ";
    [SerializeField, Range(0, 3)] private int decimalPlaces = 1;
    [SerializeField] private bool showMilliseconds = true;
    [SerializeField] private bool showAverage = false;
    [SerializeField] private bool showMinMax = false;
    [SerializeField] private bool showCpuGpuFrameTime = true;
    [SerializeField] private bool showFrameCapInfo = true;

    [Header("Memory")]
    [SerializeField] private bool showMemory = true;
    [SerializeField, Min(0.1f)] private float memoryUpdateInterval = 1f;
    [SerializeField] private bool showUnityMemory = true;
    [SerializeField] private bool showReservedMemory = true;
    [SerializeField] private bool showManagedMemory = true;
    [SerializeField] private bool showGraphicsDriverMemory = true;
    [SerializeField] private bool showGcAllocPerFrame = true;
    [SerializeField] private bool enableProfilerRecorders = true;

    [Header("Leak Watch")]
    [SerializeField] private bool showLeakWatch = true;
    [SerializeField, Min(5f)] private float leakWindowSeconds = 60f;
    [SerializeField, Min(1f)] private float minLeakAnalysisSeconds = 15f;
    [SerializeField, Min(0f)] private float leakWarningMbPerMin = 5f;
    [SerializeField, Min(0f)] private float leakCriticalMbPerMin = 20f;
    [SerializeField] private bool leakWatchUnityMemory = true;
    [SerializeField] private bool leakWatchManagedMemory = true;
    [SerializeField] private bool leakWatchGraphicsMemory = false;

    [Header("GC Alloc 警告")]
    [SerializeField] private bool useGcAllocAlerts = true;
    [SerializeField, Min(0)] private int gcAllocWarningKbPerFrame = 1;
    [SerializeField, Min(0)] private int gcAllocCriticalKbPerFrame = 64;

    [Header("Render Stats")]
    [SerializeField] private bool showRenderStats = false;
    [SerializeField] private bool showDrawCalls = true;
    [SerializeField] private bool showBatches = true;
    [SerializeField] private bool showSetPassCalls = true;
    [SerializeField] private bool showTriangles = false;
    [SerializeField] private bool showVertices = false;

    [Header("Object Counts / 重め")]
    [SerializeField] private bool showObjectCounts = false;
    [SerializeField, Min(1f)] private float objectCountSampleInterval = 5f;
    [SerializeField] private bool showLoadedAssetCounts = false;

    [Header("PC Status")]
    [SerializeField] private bool showPcStatus = true;
    [SerializeField, Min(0.5f)] private float pcStatusUpdateInterval = 1f;
    [SerializeField] private bool showHardwareSummary = true;
    [SerializeField] private bool showProcessStats = true;
    [SerializeField] private bool showBattery = true;
    [SerializeField] private bool showRuntime = true;
    [SerializeField, Range(0f, 100f)] private float processCpuWarningPercent = 70f;
    [SerializeField, Range(0f, 100f)] private float processCpuCriticalPercent = 90f;

    [Header("レイアウト")]
    [SerializeField] private ScreenAnchor anchor = ScreenAnchor.TopLeft;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(10f, -10f);
    [SerializeField] private Vector2 uiSize = new Vector2(860f, 320f);

    [Header("OnGUI 用")]
    [SerializeField] private int referenceWidth = 1920;
    [SerializeField] private int referenceHeight = 1080;
    [SerializeField] private Vector2 onGuiSize = new Vector2(860f, 320f);
    [SerializeField] private bool drawBackground = true;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private bool drawShadow = true;
    [SerializeField] private Vector2 shadowOffset = new Vector2(2f, 2f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.8f);

    [Header("フォント")]
    [SerializeField, Min(1)] private int fontSize = 28;
    [SerializeField] private Color fontColor = Color.white;

    [Header("警告色")]
    [SerializeField] private bool useColorThresholds = true;
    [SerializeField] private float warningFps = 45f;
    [SerializeField] private float criticalFps = 30f;
    [SerializeField] private Color goodColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.75f, 0.1f, 1f);
    [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("UI_Text / TMP 参照")]
    [SerializeField] private Text uiText;
    [SerializeField] private TextMeshProUGUI tmpText;

    private int frameCount;
    private float timeAcc;
    private float lastTimestamp;

    private float minFrameTime;
    private float maxFrameTime;

    private long totalFrameCount;
    private double totalTime;

    private float currentFps;
    private float currentMs;
    private float averageFps;
    private float windowMinFps;
    private float windowMaxFps;

    private double cpuFrameMs;
    private double gpuFrameMs;
    private readonly FrameTiming[] frameTimings = new FrameTiming[1];

    private float memoryTimeAcc;
    private float objectCountTimeAcc;
    private float pcStatusTimeAcc;

    private long gcHeapBytes;
    private long monoUsedBytes;
    private long monoHeapBytes;
    private long unityAllocatedBytes;
    private long unityReservedBytes;
    private long unityUnusedReservedBytes;
    private long gfxDriverBytes;
    private long gcAllocFrameBytes = -1;

    private long drawCalls = -1;
    private long batches = -1;
    private long setPassCalls = -1;
    private long triangles = -1;
    private long vertices = -1;

    private int sceneGameObjectCount;
    private int sceneComponentCount;
    private int loadedTextureCount;
    private int loadedMeshCount;
    private int loadedMaterialCount;

    private string hardwareSummary;
    private float processCpuPercent = -1f;
    private long processWorkingSetBytes = -1;
    private long processPrivateBytes = -1;
    private int processThreadCount = -1;
    private TimeSpan lastProcessCpuTime;
    private float lastProcessSampleTime = -1f;
    private float runtimeSeconds;
    private BatteryStatus batteryStatus = BatteryStatus.Unknown;
    private float batteryLevel = -1f;

    private MemorySample[] memoryHistory;
    private int memoryHistoryNextIndex;
    private int memoryHistoryCount;
    private bool leakAnalysisReady;
    private float leakGrowthMbPerMin;
    private string leakSource = string.Empty;
    private LeakAlertState leakAlertState = LeakAlertState.None;

    private string cachedText;
    private GUIStyle guiStyle;
    private readonly StringBuilder builder = new StringBuilder(512);

#if UNITY_2020_2_OR_NEWER
    private ProfilerRecorder gcAllocRecorder;
    private ProfilerRecorder drawCallsRecorder;
    private ProfilerRecorder batchesRecorder;
    private ProfilerRecorder setPassRecorder;
    private ProfilerRecorder trianglesRecorder;
    private ProfilerRecorder verticesRecorder;
#endif

    private struct MemorySample
    {
        public float time;
        public long unityAllocatedBytes;
        public long gcHeapBytes;
        public long monoUsedBytes;
        public long gfxDriverBytes;
    }

    private void Reset()
    {
        AutoAssignReferences();
        EnsureMemoryHistory();
        ResetStats();
        ForceRefresh();
    }

    private void Awake()
    {
        AutoAssignReferences();

        if (Application.isPlaying && dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        AutoAssignReferences();
        EnsureMemoryHistory();
        SetupProfilerRecorders();
        ResetStats();
        SampleMemoryStats(Time.realtimeSinceStartup);
        ForceRefresh();
    }

    private void OnDisable()
    {
        DisposeProfilerRecorders();
    }

    private void OnValidate()
    {
        updateInterval = Mathf.Max(0.05f, updateInterval);
        memoryUpdateInterval = Mathf.Max(0.1f, memoryUpdateInterval);
        objectCountSampleInterval = Mathf.Max(1f, objectCountSampleInterval);
        pcStatusUpdateInterval = Mathf.Max(0.5f, pcStatusUpdateInterval);

        referenceWidth = Mathf.Max(1, referenceWidth);
        referenceHeight = Mathf.Max(1, referenceHeight);
        fontSize = Mathf.Max(1, fontSize);

        leakWindowSeconds = Mathf.Max(5f, leakWindowSeconds);
        minLeakAnalysisSeconds = Mathf.Clamp(minLeakAnalysisSeconds, 1f, leakWindowSeconds);
        leakCriticalMbPerMin = Mathf.Max(leakWarningMbPerMin, leakCriticalMbPerMin);

        gcAllocCriticalKbPerFrame = Mathf.Max(gcAllocWarningKbPerFrame, gcAllocCriticalKbPerFrame);
        processCpuCriticalPercent = Mathf.Max(processCpuWarningPercent, processCpuCriticalPercent);

        AutoAssignReferences();
        EnsureMemoryHistory();

        if (isActiveAndEnabled)
        {
            SetupProfilerRecorders();
        }

        ForceRefresh();
    }

    private void Update()
    {
        if (!Application.isPlaying && !measureInEditMode)
        {
            SetPreviewText();
            return;
        }

        float now = Time.realtimeSinceStartup;

        if (lastTimestamp <= 0f)
        {
            lastTimestamp = now;
            CaptureFrameTimingIfNeeded();
            return;
        }

        float deltaTime = now - lastTimestamp;
        lastTimestamp = now;

        if (deltaTime <= 0f)
        {
            return;
        }

        // Editor復帰時などの極端な外れ値を避ける
        if (deltaTime > 5f)
        {
            ResetSamplingWindow();
            return;
        }

        bool shouldUpdateText = false;

        CaptureFrameTimingIfNeeded();

        frameCount++;
        timeAcc += deltaTime;

        minFrameTime = Mathf.Min(minFrameTime, deltaTime);
        maxFrameTime = Mathf.Max(maxFrameTime, deltaTime);

        memoryTimeAcc += deltaTime;
        objectCountTimeAcc += deltaTime;
        pcStatusTimeAcc += deltaTime;

        if (timeAcc >= updateInterval)
        {
            currentFps = frameCount / timeAcc;
            currentMs = frameCount > 0 ? (timeAcc / frameCount) * 1000f : 0f;

            windowMinFps = maxFrameTime > 0f ? 1f / maxFrameTime : 0f;
            windowMaxFps = minFrameTime < float.MaxValue ? 1f / minFrameTime : 0f;

            totalFrameCount += frameCount;
            totalTime += timeAcc;
            averageFps = totalTime > 0.0 ? (float)(totalFrameCount / totalTime) : 0f;

            ResetSamplingWindow();
            shouldUpdateText = true;
        }

        if ((showMemory || showLeakWatch || showGcAllocPerFrame || showRenderStats) &&
            memoryTimeAcc >= memoryUpdateInterval)
        {
            memoryTimeAcc = 0f;
            SampleMemoryStats(now);
            RecordMemoryHistory(now);
            shouldUpdateText = true;
        }

        if (showObjectCounts && objectCountTimeAcc >= objectCountSampleInterval)
        {
            objectCountTimeAcc = 0f;
            SampleObjectCounts();
            shouldUpdateText = true;
        }

        if (showPcStatus && pcStatusTimeAcc >= pcStatusUpdateInterval)
        {
            pcStatusTimeAcc = 0f;
            SamplePcStatus(now);
            shouldUpdateText = true;
        }

        if (shouldUpdateText)
        {
            UpdateDisplayText();
        }
    }

    private void OnGUI()
    {
        if (displayMethod != DisplayMethod.OnGUI)
        {
            return;
        }

        if (guiStyle == null)
        {
            InitGuiStyle();
        }

        string text = string.IsNullOrEmpty(cachedText) ? BuildInactiveText() : cachedText;

        float scale = Mathf.Min(
            (float)Screen.width / Mathf.Max(1, referenceWidth),
            (float)Screen.height / Mathf.Max(1, referenceHeight)
        );

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(scale, scale, 1f)
        );

        Rect rect = GetOnGuiRect();

        if (drawBackground)
        {
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        Color activeColor = GetActiveColor();

        if (drawShadow)
        {
            Color previousTextColor = guiStyle.normal.textColor;

            guiStyle.normal.textColor = shadowColor;
            Rect shadowRect = rect;
            shadowRect.position += shadowOffset;
            GUI.Label(shadowRect, text, guiStyle);

            guiStyle.normal.textColor = previousTextColor;
        }

        guiStyle.fontSize = fontSize;
        guiStyle.alignment = GetTextAnchor();
        guiStyle.normal.textColor = activeColor;

        GUI.Label(rect, text, guiStyle);

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    public void AutoAssignReferences()
    {
        if (!uiText)
        {
            uiText = GetComponent<Text>();
        }

        if (!tmpText)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
    }

    public void ForceRefresh()
    {
        AutoAssignReferences();
        InitGuiStyle();
        ApplyRectTransform();
        UpdateDisplayText();
    }

    public void ResetStats()
    {
        frameCount = 0;
        timeAcc = 0f;
        lastTimestamp = 0f;

        currentFps = 0f;
        currentMs = 0f;
        averageFps = 0f;
        windowMinFps = 0f;
        windowMaxFps = 0f;

        cpuFrameMs = 0.0;
        gpuFrameMs = 0.0;

        totalFrameCount = 0;
        totalTime = 0.0;

        memoryTimeAcc = 0f;
        objectCountTimeAcc = 0f;
        pcStatusTimeAcc = 0f;

        ResetSamplingWindow();
        ResetLeakWatch();
        ResetPcStatus();

        cachedText = BuildInactiveText();
        PushTextToUI();
    }

    public void ResetLeakWatch()
    {
        memoryHistoryNextIndex = 0;
        memoryHistoryCount = 0;

        leakAnalysisReady = false;
        leakGrowthMbPerMin = 0f;
        leakSource = string.Empty;
        leakAlertState = LeakAlertState.None;
    }

    public void ForceGarbageCollectionForDebug()
    {
        // 検証用。実行中に呼ぶと一時停止が発生する可能性あり。
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        SampleMemoryStats(Time.realtimeSinceStartup);
        ResetLeakWatch();
        ForceRefresh();
    }

    private void ResetSamplingWindow()
    {
        frameCount = 0;
        timeAcc = 0f;
        minFrameTime = float.MaxValue;
        maxFrameTime = 0f;
    }

    private void SetPreviewText()
    {
        string preview = BuildInactiveText();

        if (cachedText == preview)
        {
            return;
        }

        cachedText = preview;
        ApplyFontSizeAndColor();
        PushTextToUI();
    }

    private string BuildInactiveText()
    {
        return prefix + "--";
    }

    private void UpdateDisplayText()
    {
        cachedText = BuildText();
        ApplyFontSizeAndColor();
        PushTextToUI();
    }

    private string BuildText()
    {
        builder.Length = 0;

        if (showFps)
        {
            AppendFrameLine();
        }

        if (showCpuGpuFrameTime)
        {
            AppendNewLineIfNeeded();
            AppendCpuGpuLine();
        }

        if (showMemory)
        {
            AppendNewLineIfNeeded();
            AppendMemoryLine();
        }

        if (showPcStatus)
        {
            AppendNewLineIfNeeded();
            AppendPcStatusLine();
        }

        if (showLeakWatch)
        {
            AppendNewLineIfNeeded();
            AppendLeakLine();
        }

        if (showRenderStats)
        {
            AppendNewLineIfNeeded();
            AppendRenderStatsLine();
        }

        if (showObjectCounts)
        {
            AppendNewLineIfNeeded();
            AppendObjectCountsLine();
        }

        if (builder.Length == 0)
        {
            builder.Append(BuildInactiveText());
        }

        return builder.ToString();
    }

    private void AppendFrameLine()
    {
        int decimals = Mathf.Clamp(decimalPlaces, 0, 3);
        string fpsFormat = "F" + decimals;

        builder.Append(prefix);
        builder.Append(currentFps > 0f ? currentFps.ToString(fpsFormat) : "--");

        if (showMilliseconds && currentMs > 0f)
        {
            builder.Append(separator);
            builder.Append(currentMs.ToString("F2"));
            builder.Append(" ms");
        }

        if (showAverage && averageFps > 0f)
        {
            builder.Append(separator);
            builder.Append("Avg ");
            builder.Append(averageFps.ToString(fpsFormat));
        }

        if (showMinMax && windowMinFps > 0f && windowMaxFps > 0f)
        {
            builder.Append(separator);
            builder.Append("Min ");
            builder.Append(windowMinFps.ToString(fpsFormat));
            builder.Append(" / Max ");
            builder.Append(windowMaxFps.ToString(fpsFormat));
        }

        if (showFrameCapInfo)
        {
            builder.Append(separator);
            builder.Append("Target ");

            int targetFrameRate = Application.targetFrameRate;
            builder.Append(targetFrameRate < 0 ? "Default" : targetFrameRate.ToString());

            builder.Append(" / VSync ");
            builder.Append(QualitySettings.vSyncCount);
        }
    }

    private void AppendCpuGpuLine()
    {
        builder.Append("CPU ");
        AppendMsOrUnknown(cpuFrameMs);

        builder.Append(separator);
        builder.Append("GPU ");
        AppendMsOrUnknown(gpuFrameMs);
    }

    private void AppendMemoryLine()
    {
        builder.Append("Mem ");

        bool wroteAny = false;

        if (showUnityMemory)
        {
            builder.Append("Unity ");
            AppendMb(unityAllocatedBytes);
            wroteAny = true;

            if (showReservedMemory)
            {
                builder.Append(" / Res ");
                AppendMb(unityReservedBytes);

                builder.Append(" / FreePool ");
                AppendMb(unityUnusedReservedBytes);
            }
        }

        if (showManagedMemory)
        {
            if (wroteAny)
            {
                builder.Append(separator);
            }

            builder.Append("GC ");
            AppendMb(gcHeapBytes);

            builder.Append(" / Mono ");
            AppendMb(monoUsedBytes);
            builder.Append("/");
            AppendMb(monoHeapBytes);

            wroteAny = true;
        }

        if (showGraphicsDriverMemory)
        {
            if (wroteAny)
            {
                builder.Append(separator);
            }

            builder.Append("Gfx ");
            if (gfxDriverBytes > 0)
            {
                AppendMb(gfxDriverBytes);
            }
            else
            {
                builder.Append("--");
            }

            wroteAny = true;
        }

        if (showGcAllocPerFrame)
        {
            if (wroteAny)
            {
                builder.Append(separator);
            }

            builder.Append("GC/frame ");
            AppendBytesOrUnknown(gcAllocFrameBytes);
        }
    }

    private void AppendLeakLine()
    {
        builder.Append("Leak ");

        if (!leakAnalysisReady)
        {
            builder.Append("warming up");
            builder.Append(separator);
            builder.Append("window ");
            builder.Append(leakWindowSeconds.ToString("F0"));
            builder.Append("s");
            return;
        }

        builder.Append(string.IsNullOrEmpty(leakSource) ? "Delta" : leakSource);
        builder.Append(" ");

        if (leakGrowthMbPerMin >= 0f)
        {
            builder.Append("+");
        }

        builder.Append(leakGrowthMbPerMin.ToString("F1"));
        builder.Append(" MB/min");

        if (leakAlertState == LeakAlertState.Critical)
        {
            builder.Append(separator);
            builder.Append("CRITICAL");
        }
        else if (leakAlertState == LeakAlertState.Warning)
        {
            builder.Append(separator);
            builder.Append("WARN");
        }
        else
        {
            builder.Append(separator);
            builder.Append("OK");
        }
    }

    private void AppendRenderStatsLine()
    {
        builder.Append("Render ");

        bool wroteAny = false;

        if (showDrawCalls)
        {
            builder.Append("Draw ");
            AppendCountOrUnknown(drawCalls);
            wroteAny = true;
        }

        if (showBatches)
        {
            if (wroteAny) builder.Append(separator);
            builder.Append("Batch ");
            AppendCountOrUnknown(batches);
            wroteAny = true;
        }

        if (showSetPassCalls)
        {
            if (wroteAny) builder.Append(separator);
            builder.Append("SetPass ");
            AppendCountOrUnknown(setPassCalls);
            wroteAny = true;
        }

        if (showTriangles)
        {
            if (wroteAny) builder.Append(separator);
            builder.Append("Tri ");
            AppendCompactCount(triangles);
            wroteAny = true;
        }

        if (showVertices)
        {
            if (wroteAny) builder.Append(separator);
            builder.Append("Vert ");
            AppendCompactCount(vertices);
        }
    }

    private void AppendObjectCountsLine()
    {
        builder.Append("Objects ");
        builder.Append("GO ");
        builder.Append(sceneGameObjectCount);

        builder.Append(separator);
        builder.Append("Comp ");
        builder.Append(sceneComponentCount);

        if (showLoadedAssetCounts)
        {
            builder.Append(separator);
            builder.Append("Tex ");
            builder.Append(loadedTextureCount);

            builder.Append(separator);
            builder.Append("Mesh ");
            builder.Append(loadedMeshCount);

            builder.Append(separator);
            builder.Append("Mat ");
            builder.Append(loadedMaterialCount);
        }
    }

    private void AppendPcStatusLine()
    {
        builder.Append("PC ");

        bool wroteAny = false;

        if (showProcessStats)
        {
            builder.Append("AppCPU ");
            if (processCpuPercent >= 0f)
            {
                builder.Append(processCpuPercent.ToString("F0"));
                builder.Append("%");
            }
            else
            {
                builder.Append("--");
            }

            builder.Append(separator);
            builder.Append("AppRAM ");
            AppendBytesOrUnknown(processWorkingSetBytes);

            if (processPrivateBytes >= 0)
            {
                builder.Append(separator);
                builder.Append("Private ");
                AppendBytesOrUnknown(processPrivateBytes);
            }

            if (processThreadCount >= 0)
            {
                builder.Append(separator);
                builder.Append("Threads ");
                builder.Append(processThreadCount);
            }

            wroteAny = true;
        }

        if (showHardwareSummary)
        {
            if (wroteAny)
            {
                AppendNewLineIfNeeded();
                builder.Append("HW ");
            }

            builder.Append(string.IsNullOrEmpty(hardwareSummary) ? BuildHardwareSummary() : hardwareSummary);
            wroteAny = true;
        }

        if (showBattery)
        {
            if (wroteAny)
            {
                builder.Append(separator);
            }

            builder.Append("Battery ");
            if (batteryLevel >= 0f)
            {
                builder.Append((batteryLevel * 100f).ToString("F0"));
                builder.Append("% ");
                builder.Append(batteryStatus);
            }
            else
            {
                builder.Append(batteryStatus == BatteryStatus.Unknown ? "--" : batteryStatus.ToString());
            }

            wroteAny = true;
        }

        if (showRuntime)
        {
            if (wroteAny)
            {
                builder.Append(separator);
            }

            builder.Append("Up ");
            AppendDuration(runtimeSeconds);
        }
    }

    private void AppendNewLineIfNeeded()
    {
        if (builder.Length > 0)
        {
            builder.Append('\n');
        }
    }

    private void AppendMsOrUnknown(double ms)
    {
        if (ms > 0.0)
        {
            builder.Append(ms.ToString("F2"));
            builder.Append(" ms");
        }
        else
        {
            builder.Append("--");
        }
    }

    private void AppendMb(long bytes)
    {
        if (bytes <= 0)
        {
            builder.Append("0.0MB");
            return;
        }

        builder.Append((bytes * BytesToMb).ToString("F1"));
        builder.Append("MB");
    }

    private void AppendBytesOrUnknown(long bytes)
    {
        if (bytes < 0)
        {
            builder.Append("--");
            return;
        }

        if (bytes < 1024)
        {
            builder.Append(bytes);
            builder.Append("B");
        }
        else if (bytes < 1024 * 1024)
        {
            builder.Append((bytes * BytesToKb).ToString("F1"));
            builder.Append("KB");
        }
        else
        {
            builder.Append((bytes * BytesToMb).ToString("F2"));
            builder.Append("MB");
        }
    }

    private void AppendDuration(float seconds)
    {
        if (seconds < 0f)
        {
            builder.Append("--");
            return;
        }

        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1.0)
        {
            builder.Append((int)time.TotalHours);
            builder.Append("h ");
        }

        builder.Append(time.Minutes.ToString("00"));
        builder.Append("m ");
        builder.Append(time.Seconds.ToString("00"));
        builder.Append("s");
    }

    private void AppendCountOrUnknown(long value)
    {
        if (value < 0)
        {
            builder.Append("--");
        }
        else
        {
            builder.Append(value);
        }
    }

    private void AppendCompactCount(long value)
    {
        if (value < 0)
        {
            builder.Append("--");
            return;
        }

        if (value >= 1000000)
        {
            builder.Append((value / 1000000f).ToString("F1"));
            builder.Append("M");
        }
        else if (value >= 1000)
        {
            builder.Append((value / 1000f).ToString("F1"));
            builder.Append("K");
        }
        else
        {
            builder.Append(value);
        }
    }

    private void PushTextToUI()
    {
        if (displayMethod == DisplayMethod.UI_Text && uiText)
        {
            uiText.text = cachedText;
        }

        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText)
        {
            tmpText.text = cachedText;
        }
    }

    private void CaptureFrameTimingIfNeeded()
    {
        if (!showCpuGpuFrameTime)
        {
            return;
        }

        FrameTimingManager.CaptureFrameTimings();

        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);
        if (count <= 0)
        {
            return;
        }

        cpuFrameMs = frameTimings[0].cpuFrameTime;
        gpuFrameMs = frameTimings[0].gpuFrameTime;
    }

    private void SampleMemoryStats(float now)
    {
        gcHeapBytes = GC.GetTotalMemory(false);

        unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
        unityReservedBytes = Profiler.GetTotalReservedMemoryLong();
        unityUnusedReservedBytes = Profiler.GetTotalUnusedReservedMemoryLong();

        monoUsedBytes = Profiler.GetMonoUsedSizeLong();
        monoHeapBytes = Profiler.GetMonoHeapSizeLong();

#if UNITY_2020_2_OR_NEWER
        if (Application.isEditor || Debug.isDebugBuild)
        {
            gfxDriverBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();
        }
        else
        {
            gfxDriverBytes = 0;
        }
#else
        gfxDriverBytes = 0;
#endif

        UpdateProfilerCounterValues();
    }

    private void RecordMemoryHistory(float now)
    {
        if (!showLeakWatch)
        {
            return;
        }

        EnsureMemoryHistory();

        MemorySample sample = new MemorySample
        {
            time = now,
            unityAllocatedBytes = unityAllocatedBytes,
            gcHeapBytes = gcHeapBytes,
            monoUsedBytes = monoUsedBytes,
            gfxDriverBytes = gfxDriverBytes
        };

        memoryHistory[memoryHistoryNextIndex] = sample;
        memoryHistoryNextIndex = (memoryHistoryNextIndex + 1) % memoryHistory.Length;
        memoryHistoryCount = Mathf.Min(memoryHistoryCount + 1, memoryHistory.Length);

        AnalyzeMemoryGrowth(sample);
    }

    private void AnalyzeMemoryGrowth(MemorySample current)
    {
        if (memoryHistory == null || memoryHistoryCount <= 1)
        {
            leakAnalysisReady = false;
            leakAlertState = LeakAlertState.None;
            return;
        }

        bool foundOldSample = false;
        MemorySample oldest = default;
        float oldestAge = 0f;

        for (int i = 0; i < memoryHistoryCount; i++)
        {
            MemorySample sample = memoryHistory[i];
            float age = current.time - sample.time;

            if (age < minLeakAnalysisSeconds || age > leakWindowSeconds)
            {
                continue;
            }

            if (!foundOldSample || age > oldestAge)
            {
                foundOldSample = true;
                oldest = sample;
                oldestAge = age;
            }
        }

        if (!foundOldSample)
        {
            leakAnalysisReady = false;
            leakAlertState = LeakAlertState.None;
            return;
        }

        float minutes = Mathf.Max(0.001f, (current.time - oldest.time) / 60f);

        float bestGrowth = float.MinValue;
        string bestSource = string.Empty;

        if (leakWatchUnityMemory)
        {
            ConsiderLeakSource(
                "Unity",
                current.unityAllocatedBytes,
                oldest.unityAllocatedBytes,
                minutes,
                ref bestGrowth,
                ref bestSource
            );
        }

        if (leakWatchManagedMemory)
        {
            ConsiderLeakSource(
                "GC",
                current.gcHeapBytes,
                oldest.gcHeapBytes,
                minutes,
                ref bestGrowth,
                ref bestSource
            );

            ConsiderLeakSource(
                "Mono",
                current.monoUsedBytes,
                oldest.monoUsedBytes,
                minutes,
                ref bestGrowth,
                ref bestSource
            );
        }

        if (leakWatchGraphicsMemory && current.gfxDriverBytes > 0 && oldest.gfxDriverBytes > 0)
        {
            ConsiderLeakSource(
                "Gfx",
                current.gfxDriverBytes,
                oldest.gfxDriverBytes,
                minutes,
                ref bestGrowth,
                ref bestSource
            );
        }

        if (bestGrowth == float.MinValue)
        {
            leakAnalysisReady = false;
            leakAlertState = LeakAlertState.None;
            return;
        }

        leakAnalysisReady = true;
        leakGrowthMbPerMin = bestGrowth;
        leakSource = bestSource;

        if (leakGrowthMbPerMin >= leakCriticalMbPerMin)
        {
            leakAlertState = LeakAlertState.Critical;
        }
        else if (leakGrowthMbPerMin >= leakWarningMbPerMin)
        {
            leakAlertState = LeakAlertState.Warning;
        }
        else
        {
            leakAlertState = LeakAlertState.Normal;
        }
    }

    private static void ConsiderLeakSource(
        string sourceName,
        long currentBytes,
        long oldBytes,
        float minutes,
        ref float bestGrowth,
        ref string bestSource
    )
    {
        float growth = ((currentBytes - oldBytes) * BytesToMb) / minutes;

        if (growth > bestGrowth)
        {
            bestGrowth = growth;
            bestSource = sourceName;
        }
    }

    private void SampleObjectCounts()
    {
        // 重い。常時ONではなく検証時のみ推奨。
        sceneGameObjectCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
        sceneComponentCount = UnityEngine.Object.FindObjectsOfType<Component>().Length;

        if (showLoadedAssetCounts)
        {
            loadedTextureCount = Resources.FindObjectsOfTypeAll<Texture>().Length;
            loadedMeshCount = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            loadedMaterialCount = Resources.FindObjectsOfTypeAll<Material>().Length;
        }
    }

    private void ResetPcStatus()
    {
        hardwareSummary = BuildHardwareSummary();
        processCpuPercent = -1f;
        processWorkingSetBytes = -1;
        processPrivateBytes = -1;
        processThreadCount = -1;
        lastProcessCpuTime = TimeSpan.Zero;
        lastProcessSampleTime = -1f;
        runtimeSeconds = 0f;
        batteryStatus = BatteryStatus.Unknown;
        batteryLevel = -1f;
    }

    private void SamplePcStatus(float now)
    {
        runtimeSeconds = now;
        batteryLevel = SystemInfo.batteryLevel;
        batteryStatus = SystemInfo.batteryStatus;

        if (!showProcessStats)
        {
            return;
        }

#if !UNITY_WEBGL
        try
        {
            using (DiagnosticsProcess process = DiagnosticsProcess.GetCurrentProcess())
            {
                process.Refresh();

                processWorkingSetBytes = process.WorkingSet64;
                processPrivateBytes = process.PrivateMemorySize64;
                processThreadCount = process.Threads != null ? process.Threads.Count : -1;

                TimeSpan currentCpuTime = process.TotalProcessorTime;

                if (lastProcessSampleTime >= 0f)
                {
                    float elapsedSeconds = Mathf.Max(0.001f, now - lastProcessSampleTime);
                    double cpuSeconds = (currentCpuTime - lastProcessCpuTime).TotalSeconds;
                    processCpuPercent = Mathf.Clamp(
                        (float)(cpuSeconds / elapsedSeconds / Mathf.Max(1, SystemInfo.processorCount) * 100.0),
                        0f,
                        100f
                    );
                }

                lastProcessCpuTime = currentCpuTime;
                lastProcessSampleTime = now;
            }
        }
        catch
        {
            processCpuPercent = -1f;
            processWorkingSetBytes = -1;
            processPrivateBytes = -1;
            processThreadCount = -1;
        }
#else
        processCpuPercent = -1f;
        processWorkingSetBytes = -1;
        processPrivateBytes = -1;
        processThreadCount = -1;
#endif
    }

    private string BuildHardwareSummary()
    {
        StringBuilder summaryBuilder = new StringBuilder(256);

        summaryBuilder.Append(SystemInfo.processorCount);
        summaryBuilder.Append(" cores");

        if (SystemInfo.processorFrequency > 0)
        {
            summaryBuilder.Append(" @ ");
            summaryBuilder.Append((SystemInfo.processorFrequency / 1000f).ToString("F1"));
            summaryBuilder.Append("GHz");
        }

        summaryBuilder.Append(separator);
        summaryBuilder.Append("RAM ");
        summaryBuilder.Append(SystemInfo.systemMemorySize);
        summaryBuilder.Append("MB");

        summaryBuilder.Append(separator);
        summaryBuilder.Append("GPU ");
        summaryBuilder.Append(SystemInfo.graphicsDeviceName);

        if (SystemInfo.graphicsMemorySize > 0)
        {
            summaryBuilder.Append(" ");
            summaryBuilder.Append(SystemInfo.graphicsMemorySize);
            summaryBuilder.Append("MB");
        }

        summaryBuilder.Append(separator);
        summaryBuilder.Append(SystemInfo.operatingSystem);

        return summaryBuilder.ToString();
    }

    private void EnsureMemoryHistory()
    {
        int requiredLength = Mathf.CeilToInt(
            Mathf.Max(5f, leakWindowSeconds) / Mathf.Max(0.1f, memoryUpdateInterval)
        ) + 4;

        requiredLength = Mathf.Clamp(requiredLength, 8, 512);

        if (memoryHistory != null && memoryHistory.Length == requiredLength)
        {
            return;
        }

        memoryHistory = new MemorySample[requiredLength];
        memoryHistoryNextIndex = 0;
        memoryHistoryCount = 0;
        leakAnalysisReady = false;
    }

    private void SetupProfilerRecorders()
    {
#if UNITY_2020_2_OR_NEWER
        DisposeProfilerRecorders();

        if (!enableProfilerRecorders)
        {
            return;
        }

        TryStartRecorder(ref gcAllocRecorder, ProfilerCategory.Memory, "GC Allocated In Frame");

        TryStartRecorder(ref drawCallsRecorder, ProfilerCategory.Render, "Draw Calls Count");
        TryStartRecorder(ref batchesRecorder, ProfilerCategory.Render, "Batches Count");
        TryStartRecorder(ref setPassRecorder, ProfilerCategory.Render, "SetPass Calls Count");
        TryStartRecorder(ref trianglesRecorder, ProfilerCategory.Render, "Triangles Count");
        TryStartRecorder(ref verticesRecorder, ProfilerCategory.Render, "Vertices Count");
#endif
    }

    private void DisposeProfilerRecorders()
    {
#if UNITY_2020_2_OR_NEWER
        DisposeRecorder(ref gcAllocRecorder);
        DisposeRecorder(ref drawCallsRecorder);
        DisposeRecorder(ref batchesRecorder);
        DisposeRecorder(ref setPassRecorder);
        DisposeRecorder(ref trianglesRecorder);
        DisposeRecorder(ref verticesRecorder);
#endif
    }

#if UNITY_2020_2_OR_NEWER
    private static void TryStartRecorder(
        ref ProfilerRecorder recorder,
        ProfilerCategory category,
        string statName
    )
    {
        try
        {
            recorder = ProfilerRecorder.StartNew(category, statName, 1);

            if (!recorder.Valid)
            {
                recorder = default;
            }
        }
        catch
        {
            recorder = default;
        }
    }

    private static void DisposeRecorder(ref ProfilerRecorder recorder)
    {
        if (recorder.Valid)
        {
            recorder.Dispose();
        }

        recorder = default;
    }

    private static long ReadRecorderValue(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : -1;
    }
#endif

    private void UpdateProfilerCounterValues()
    {
#if UNITY_2020_2_OR_NEWER
        gcAllocFrameBytes = ReadRecorderValue(gcAllocRecorder);

        drawCalls = ReadRecorderValue(drawCallsRecorder);
        batches = ReadRecorderValue(batchesRecorder);
        setPassCalls = ReadRecorderValue(setPassRecorder);
        triangles = ReadRecorderValue(trianglesRecorder);
        vertices = ReadRecorderValue(verticesRecorder);
#else
        gcAllocFrameBytes = -1;

        drawCalls = -1;
        batches = -1;
        setPassCalls = -1;
        triangles = -1;
        vertices = -1;
#endif
    }

    private void InitGuiStyle()
    {
        if (guiStyle == null)
        {
            guiStyle = new GUIStyle();
        }

        guiStyle.fontSize = fontSize;
        guiStyle.normal.textColor = GetActiveColor();
        guiStyle.alignment = GetTextAnchor();
        guiStyle.clipping = TextClipping.Overflow;
        guiStyle.wordWrap = false;
        guiStyle.richText = false;
    }

    private void ApplyFontSizeAndColor()
    {
        Color activeColor = GetActiveColor();

        if (uiText)
        {
            uiText.fontSize = fontSize;
            uiText.color = activeColor;
            uiText.alignment = GetTextAnchor();
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.raycastTarget = false;
        }

        if (tmpText)
        {
            tmpText.fontSize = fontSize;
            tmpText.color = activeColor;
            tmpText.alignment = GetTmpAlignment();
            tmpText.enableWordWrapping = false;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.raycastTarget = false;
        }

        if (guiStyle != null)
        {
            guiStyle.fontSize = fontSize;
            guiStyle.normal.textColor = activeColor;
            guiStyle.alignment = GetTextAnchor();
        }
    }

    private Color GetActiveColor()
    {
        if (!useColorThresholds)
        {
            return fontColor;
        }

        if (leakAlertState == LeakAlertState.Critical)
        {
            return criticalColor;
        }

        if (useGcAllocAlerts && gcAllocFrameBytes >= gcAllocCriticalKbPerFrame * 1024L)
        {
            return criticalColor;
        }

        if (currentFps > 0f && currentFps < criticalFps)
        {
            return criticalColor;
        }

        if (showPcStatus && processCpuPercent >= processCpuCriticalPercent)
        {
            return criticalColor;
        }

        if (leakAlertState == LeakAlertState.Warning)
        {
            return warningColor;
        }

        if (showPcStatus && processCpuPercent >= processCpuWarningPercent)
        {
            return warningColor;
        }

        if (useGcAllocAlerts && gcAllocFrameBytes >= gcAllocWarningKbPerFrame * 1024L)
        {
            return warningColor;
        }

        if (currentFps > 0f && currentFps < warningFps)
        {
            return warningColor;
        }

        if (currentFps <= 0f)
        {
            return fontColor;
        }

        return goodColor;
    }

    private void ApplyRectTransform()
    {
        if (displayMethod == DisplayMethod.UI_Text && uiText)
        {
            ApplyRect(uiText.rectTransform);
        }

        if (displayMethod == DisplayMethod.UI_TextMeshPro && tmpText)
        {
            ApplyRect(tmpText.rectTransform);
        }

        ApplyFontSizeAndColor();
    }

    private void ApplyRect(RectTransform rectTransform)
    {
        if (!rectTransform)
        {
            return;
        }

        Vector2 anchorVector = GetAnchorVector();

        rectTransform.anchorMin = anchorVector;
        rectTransform.anchorMax = anchorVector;
        rectTransform.pivot = anchorVector;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = uiSize;
    }

    private Rect GetOnGuiRect()
    {
        float x;
        float y;

        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                x = referenceWidth + anchoredPosition.x - onGuiSize.x;
                y = -anchoredPosition.y;
                break;

            case ScreenAnchor.BottomLeft:
                x = anchoredPosition.x;
                y = referenceHeight - anchoredPosition.y - onGuiSize.y;
                break;

            case ScreenAnchor.BottomRight:
                x = referenceWidth + anchoredPosition.x - onGuiSize.x;
                y = referenceHeight - anchoredPosition.y - onGuiSize.y;
                break;

            case ScreenAnchor.TopLeft:
            default:
                x = anchoredPosition.x;
                y = -anchoredPosition.y;
                break;
        }

        return new Rect(x, y, onGuiSize.x, onGuiSize.y);
    }

    private Vector2 GetAnchorVector()
    {
        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                return new Vector2(1f, 1f);

            case ScreenAnchor.BottomLeft:
                return new Vector2(0f, 0f);

            case ScreenAnchor.BottomRight:
                return new Vector2(1f, 0f);

            case ScreenAnchor.TopLeft:
            default:
                return new Vector2(0f, 1f);
        }
    }

    private TextAnchor GetTextAnchor()
    {
        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                return TextAnchor.UpperRight;

            case ScreenAnchor.BottomLeft:
                return TextAnchor.LowerLeft;

            case ScreenAnchor.BottomRight:
                return TextAnchor.LowerRight;

            case ScreenAnchor.TopLeft:
            default:
                return TextAnchor.UpperLeft;
        }
    }

    private TextAlignmentOptions GetTmpAlignment()
    {
        switch (anchor)
        {
            case ScreenAnchor.TopRight:
                return TextAlignmentOptions.TopRight;

            case ScreenAnchor.BottomLeft:
                return TextAlignmentOptions.BottomLeft;

            case ScreenAnchor.BottomRight:
                return TextAlignmentOptions.BottomRight;

            case ScreenAnchor.TopLeft:
            default:
                return TextAlignmentOptions.TopLeft;
        }
    }
}
