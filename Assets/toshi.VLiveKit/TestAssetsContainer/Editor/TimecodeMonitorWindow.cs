#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class TimecodeMonitorWindow : EditorWindow
{
    private GUIStyle timecodeStyle;
    private GUIStyle statusStyle;
    private Vector2 scroll;

    [MenuItem("toshi/VLiveKit/Test Assets/Timecode Monitor")]
    private static void Open()
    {
        GetWindow<TimecodeMonitorWindow>("Timecode Monitor");
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        EnsureStyles();

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scrollView.scrollPosition;
            DrawHeader();
            DrawLtcReaders();
            DrawOscReceivers();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Timecode Monitor", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("Enter Play Mode to monitor live LTC or OSC timecode.", statusStyle);
            EditorGUILayout.Space(6);
        }
    }

    private void DrawLtcReaders()
    {
        var readers = FindObjectsByType<LTCread>(FindObjectsSortMode.None);
        EditorGUILayout.LabelField($"LTC Readers ({readers.Length})", EditorStyles.boldLabel);

        if (readers.Length == 0)
        {
            EditorGUILayout.LabelField("No LTCread components in the active scene.", statusStyle);
            EditorGUILayout.Space(8);
            return;
        }

        foreach (var reader in readers)
        {
            DrawBox(
                reader.name,
                reader.CurrentTimeCode,
                $"Device: {Fallback(reader.SelectedDeviceName, "None")}  FPS: {reader.FramesPerSecond}  Gain: {reader.Gain:F5}  Signal: {(reader.HasSignal ? "Yes" : "No")}");
        }
    }

    private void DrawOscReceivers()
    {
        var receivers = FindObjectsByType<LtcOscTimecodeReceiver>(FindObjectsSortMode.None);
        EditorGUILayout.LabelField($"OSC Receivers ({receivers.Length})", EditorStyles.boldLabel);

        if (receivers.Length == 0)
        {
            EditorGUILayout.LabelField("No LtcOscTimecodeReceiver components in the active scene.", statusStyle);
            return;
        }

        foreach (var receiver in receivers)
        {
            DrawBox(
                receiver.name,
                receiver.CurrentTimeCode,
                $"Seconds: {receiver.CurrentTimeSeconds:F3}  FPS: {receiver.FramesPerSecond}  Last: {Fallback(receiver.LastAddress, "None")}  Received: {(receiver.HasReceived ? "Yes" : "No")}");
        }
    }

    private void DrawBox(string title, string timecode, string detail)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(timecode, timecodeStyle, GUILayout.Height(48));
        EditorGUILayout.LabelField(detail, statusStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private static string Fallback(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private void EnsureStyles()
    {
        if (timecodeStyle != null && statusStyle != null)
            return;

        timecodeStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        statusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true
        };
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }
}
#endif
