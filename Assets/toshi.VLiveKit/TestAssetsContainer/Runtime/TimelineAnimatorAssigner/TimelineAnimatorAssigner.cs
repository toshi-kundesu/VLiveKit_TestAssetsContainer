using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class TimelineAnimatorAssigner : MonoBehaviour
{
    [Header("PlayableDirector を持つオブジェクト")]
    public List<PlayableDirector> directors = new List<PlayableDirector>();

    [Header("割り当てるターゲット GameObject (順番必須)")]
    public List<GameObject> targets = new List<GameObject>();

#if UNITY_EDITOR
    [ContextMenu("Assign Animators To Timelines")]
    public void AssignAnimatorsToTimelines()
    {
        if (directors == null || directors.Count == 0 || targets == null || targets.Count == 0)
        {
            Debug.LogWarning("PlayableDirector または 対象 GameObject が設定されていません。");
            return;
        }

        foreach (var director in directors)
        {
            if (director == null || director.playableAsset == null) continue;

            var timeline = director.playableAsset as TimelineAsset;
            if (timeline == null) continue;

            var animationTracks = timeline.GetOutputTracks()
                                          .OfType<AnimationTrack>()
                                          .ToList();

            int index = 0; // ← ディレクターごとに先頭から割り当て

            foreach (var track in animationTracks)
            {
                if (index >= targets.Count)
                {
                    Debug.LogWarning($"{director.name} : 対象 GameObject が不足しています。残りの AnimationTrack はスキップしました。");
                    break;
                }

                var go = targets[index];
                if (go == null)
                {
                    Debug.LogWarning($"{director.name} : targets[{index}] が null です。スキップします。");
                    index++;
                    continue;
                }

                // Animator を取得 or 追加
                var animator = go.GetComponent<Animator>();
                if (animator == null)
                {
#if UNITY_EDITOR
                    animator = Undo.AddComponent<Animator>(go);
                    Debug.Log($"[{director.name}] {go.name} に Animator を追加しました。");
#else
                    animator = go.AddComponent<Animator>();
#endif
                }

                // Timeline の AnimationTrack にバインド
                director.SetGenericBinding(track, animator);

#if UNITY_EDITOR
                // 変更を Dirty 扱いにして保存対象に
                EditorUtility.SetDirty(director);
                EditorUtility.SetDirty(go);
                if (director.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
#endif
                index++;
            }
        }

#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
#endif
        Debug.Log("Timeline への Animator アサインが完了しました。");
    }
#endif
}
