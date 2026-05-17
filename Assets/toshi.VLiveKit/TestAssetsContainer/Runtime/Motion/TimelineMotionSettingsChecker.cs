using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace toshi.VLiveKit.TestAssetsContainer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("toshi/VLiveKit/Test Assets/Timeline Motion Settings Checker")]
    public sealed class TimelineMotionSettingsChecker : MonoBehaviour
    {
        [SerializeField] private PlayableDirector rootDirector;

        [Header("Check Timing")]
        [SerializeField] private bool checkOnAwake = true;

        [Header("Targets")]
        [SerializeField] private bool includeSubTimelines = true;

        [Header("Checks")]
        [SerializeField] private bool requireAnimationCompressionOff = true;
        [SerializeField] private bool requireRemoveStartOffsetOff = true;

        [Header("Log")]
        [SerializeField] private bool warnIfIssue = true;
        [SerializeField] private bool verboseLog;
        [SerializeField, TextArea] private string lastReport;

        private readonly HashSet<AnimationClip> animationClips = new();
        private readonly HashSet<PlayableDirector> visitedDirectors = new();
        private readonly HashSet<AnimationPlayableAsset> visitedAnimationAssets = new();
        private readonly HashSet<string> checkedImporterPaths = new();

        private int timelineCount;
        private int animationPlayableCount;
        private int compressionIssueCount;
        private int removeStartOffsetIssueCount;
        private int compressionSkippedCount;

        private void Reset()
        {
            if (rootDirector == null)
            {
                rootDirector = GetComponent<PlayableDirector>();
            }
        }

        private void Awake()
        {
            if (!checkOnAwake)
            {
                return;
            }

            CheckTimelineMotionSettings();
        }

        [ContextMenu("Check Timeline Motion Settings")]
        public void CheckTimelineMotionSettings()
        {
            ClearState();

            if (rootDirector == null)
            {
                lastReport = $"{name}: rootDirector is not assigned.";
                if (warnIfIssue)
                {
                    Debug.LogWarning(lastReport, this);
                }

                return;
            }

            CollectFromDirector(rootDirector);
            CheckAnimationCompression();
            BuildReport();

            if (verboseLog || HasIssue())
            {
                Debug.Log(lastReport, this);
            }
        }

        private void ClearState()
        {
            animationClips.Clear();
            visitedDirectors.Clear();
            visitedAnimationAssets.Clear();
            checkedImporterPaths.Clear();
            timelineCount = 0;
            animationPlayableCount = 0;
            compressionIssueCount = 0;
            removeStartOffsetIssueCount = 0;
            compressionSkippedCount = 0;
            lastReport = string.Empty;
        }

        private void CollectFromDirector(PlayableDirector director)
        {
            if (director == null || visitedDirectors.Contains(director))
            {
                return;
            }

            visitedDirectors.Add(director);

            if (director.playableAsset is not TimelineAsset timeline)
            {
                return;
            }

            timelineCount++;
            CollectFromTimeline(director, timeline);
        }

        private void CollectFromTimeline(PlayableDirector ownerDirector, TimelineAsset timeline)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track == null)
                {
                    continue;
                }

                if (track is AnimationTrack animationTrack && animationTrack.infiniteClip != null)
                {
                    animationClips.Add(animationTrack.infiniteClip);
                }

                foreach (var timelineClip in track.GetClips())
                {
                    if (timelineClip?.asset is AnimationPlayableAsset animationAsset)
                    {
                        CheckAnimationPlayableAsset(animationAsset, timeline, track, timelineClip);
                    }
                }
            }

            if (includeSubTimelines)
            {
                CollectSubTimelineDirectors(ownerDirector, timeline);
            }
        }

        private void CheckAnimationPlayableAsset(
            AnimationPlayableAsset animationAsset,
            TimelineAsset timeline,
            TrackAsset track,
            TimelineClip timelineClip)
        {
            if (animationAsset == null || visitedAnimationAssets.Contains(animationAsset))
            {
                return;
            }

            visitedAnimationAssets.Add(animationAsset);
            animationPlayableCount++;

            if (animationAsset.clip != null)
            {
                animationClips.Add(animationAsset.clip);
            }

            if (!requireRemoveStartOffsetOff || !animationAsset.removeStartOffset)
            {
                return;
            }

            removeStartOffsetIssueCount++;

            if (warnIfIssue)
            {
                Debug.LogWarning(
                    $"{name}: Remove Start Offset is ON: {FormatTimelineLocation(timeline, track, timelineClip)}",
                    animationAsset);
            }
        }

        private void CollectSubTimelineDirectors(PlayableDirector ownerDirector, TimelineAsset timeline)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track == null)
                {
                    continue;
                }

                if (track is not ControlTrack)
                {
                    continue;
                }

                foreach (var timelineClip in track.GetClips())
                {
                    if (timelineClip?.asset is not ControlPlayableAsset controlAsset)
                    {
                        continue;
                    }

                    if (!controlAsset.updateDirector)
                    {
                        continue;
                    }

                    CollectDirectorsFromControlAsset(ownerDirector, controlAsset);
                }
            }
        }

        private void CollectDirectorsFromControlAsset(PlayableDirector ownerDirector, ControlPlayableAsset controlAsset)
        {
            if (controlAsset.prefabGameObject != null)
            {
                CollectDirectorsFromGameObject(controlAsset.prefabGameObject, controlAsset.searchHierarchy);
            }

            if (ownerDirector == null)
            {
                return;
            }

            var source = ownerDirector.GetReferenceValue(
                controlAsset.sourceGameObject.exposedName,
                out var isValid) as GameObject;

            if (!isValid || source == null)
            {
                return;
            }

            CollectDirectorsFromGameObject(source, controlAsset.searchHierarchy);
        }

        private void CollectDirectorsFromGameObject(GameObject source, bool searchHierarchy)
        {
            if (source == null)
            {
                return;
            }

            if (searchHierarchy)
            {
                var directors = source.GetComponentsInChildren<PlayableDirector>(true);
                foreach (var director in directors)
                {
                    CollectFromDirector(director);
                }

                return;
            }

            CollectFromDirector(source.GetComponent<PlayableDirector>());
        }

        private void CheckAnimationCompression()
        {
            if (!requireAnimationCompressionOff)
            {
                return;
            }

            foreach (var clip in animationClips)
            {
                CheckAnimationCompression(clip);
            }
        }

        private void CheckAnimationCompression(AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path) || checkedImporterPaths.Contains(path))
            {
                return;
            }

            checkedImporterPaths.Add(path);

            if (AssetImporter.GetAtPath(path) is not ModelImporter modelImporter)
            {
                compressionSkippedCount++;
                return;
            }

            if (modelImporter.animationCompression == ModelImporterAnimationCompression.Off)
            {
                return;
            }

            compressionIssueCount++;

            if (warnIfIssue)
            {
                Debug.LogWarning(
                    $"{name}: Anim. Compression is not Off: {clip.name} ({modelImporter.animationCompression}) at {path}",
                    clip);
            }
#else
            compressionSkippedCount++;
#endif
        }

        private void BuildReport()
        {
            lastReport =
                $"{name}: Timeline motion settings check complete. " +
                $"timelines={timelineCount}, animationPlayables={animationPlayableCount}, " +
                $"clips={animationClips.Count}, compressionIssues={compressionIssueCount}, " +
                $"removeStartOffsetIssues={removeStartOffsetIssueCount}, compressionSkipped={compressionSkippedCount}";
        }

        private bool HasIssue()
        {
            return compressionIssueCount > 0 || removeStartOffsetIssueCount > 0;
        }

        private static string FormatTimelineLocation(TimelineAsset timeline, TrackAsset track, TimelineClip timelineClip)
        {
            var timelineName = timeline != null ? timeline.name : "(Timeline)";
            var trackName = track != null ? track.name : "(Track)";
            var clipName = timelineClip != null ? timelineClip.displayName : "(Clip)";
            return $"{timelineName}/{trackName}/{clipName}";
        }
    }
}
