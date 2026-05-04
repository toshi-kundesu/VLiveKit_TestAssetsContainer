#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LiveStageAnimatorBinder))]
[CanEditMultipleObjects]
public sealed class LiveStageAnimatorBinderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);
        DrawShowControls();

        EditorGUILayout.Space(6f);
        DrawStageCollectControls();
    }

    private void DrawShowControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Live Show Controls", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Dry Run", GUILayout.Height(28f)))
            {
                RunForEachBinder(binder => binder.PreviewLiveTimelineBindings());
            }

            if (GUILayout.Button("Bind Performers", GUILayout.Height(28f)))
            {
                RunForEachBinder(binder => binder.BindPerformersToLiveTimelines());
            }
        }

        if (GUILayout.Button("Strike Bindings", GUILayout.Height(24f)))
        {
            if (EditorUtility.DisplayDialog(
                    "Strike Live Bindings",
                    "Clear AnimationTrack performer bindings on the configured live timelines?",
                    "Strike",
                    "Cancel"))
            {
                RunForEachBinder(binder => binder.ClearLiveTimelineBindings());
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStageCollectControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Stage Collect Helpers", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Find Live Directors"))
            {
                RunForEachBinder(binder => binder.CollectLiveDirectorsFromChildren());
            }

            if (GUILayout.Button("Selected Performers"))
            {
                RunForEachBinder(binder => binder.CollectPerformersFromSelection());
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Child Animators"))
            {
                RunForEachBinder(binder => binder.CollectPerformersFromChildAnimators());
            }

            if (GUILayout.Button("Direct Children"))
            {
                RunForEachBinder(binder => binder.CollectPerformersFromDirectChildren());
            }
        }

        if (GUILayout.Button("Clean Missing Stage Entries"))
        {
            RunForEachBinder(binder => binder.RemoveMissingStageEntries());
        }

        EditorGUILayout.EndVertical();
    }

    private void RunForEachBinder(Action<LiveStageAnimatorBinder> action)
    {
        serializedObject.ApplyModifiedProperties();

        foreach (var inspectedObject in serializedObject.targetObjects)
        {
            var binder = inspectedObject as LiveStageAnimatorBinder;
            if (binder == null)
            {
                continue;
            }

            action(binder);
        }

        serializedObject.Update();
    }
}
#endif
