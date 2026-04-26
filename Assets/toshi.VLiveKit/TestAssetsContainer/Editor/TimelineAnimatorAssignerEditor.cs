// TimelineAnimatorAssignerEditor.cs
// -------------------------------------------------------------
// TimelineAnimatorAssigner に “Assign Animators” ボタンを追加する
// ・UI Toolkit（CreateInspectorGUI）でモダンなデザイン
// ・IMGUI (OnInspectorGUI) でフォールバック
// -------------------------------------------------------------
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomEditor(typeof(TimelineAnimatorAssigner))]
public sealed class TimelineAnimatorAssignerEditor : Editor
{
    // ────────── UI Toolkit ──────────
    public override VisualElement CreateInspectorGUI()
    {
        // 既定のインスペクター
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        // スタイリッシュボタン
        var btn = new Button(Assign)
        {
            text  = "Assign Animators",
            tooltip = "PlayableDirector ➔ Animator バインドを一括設定",
        };
        btn.AddToClassList("taa-button");
        root.Add(btn);

        // 専用 USS があれば読み込む（任意）
        var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/TimelineAnimatorAssignerEditor.uss");
        if (style != null) root.styleSheets.Add(style);

        return root;
    }

    // ────────── IMGUI フォールバック ──────────
    public override void OnInspectorGUI()
    {
        // UI Toolkit が使える場合は IMGUI を描かない
        if (EditorGUIUtility.isProSkin) return;

        base.OnInspectorGUI();
        GUILayout.Space(8);

        var style = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 32,
            fontStyle   = FontStyle.Bold,
            fontSize    = 13,
        };
        var color = GUI.color;
        GUI.color = new Color(0.18f, 0.55f, 0.79f); // アクセントカラー

        if (GUILayout.Button("Assign Animators", style))
            Assign();

        GUI.color = color;
    }

    // ────────── 共通処理 ──────────
    private void Assign()
    {
        foreach (var obj in targets)
        {
            var assigner = obj as TimelineAnimatorAssigner;
            assigner?.AssignAnimatorsToTimelines();
        }
    }
}
#endif
