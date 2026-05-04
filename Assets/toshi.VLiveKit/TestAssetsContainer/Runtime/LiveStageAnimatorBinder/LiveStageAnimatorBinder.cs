using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class LiveStageAnimatorBinder : MonoBehaviour
{
    public enum CueMatchMode
    {
        SetlistOrder,
        TrackNameThenSetlist,
        TrackNameOnly,
    }

    public sealed class LiveBindingReport
    {
        public bool previewOnly;
        public int liveDirectorCount;
        public int cueCount;
        public int boundCount;
        public int clearedCount;
        public int animatorAddedCount;
        public int unchangedCount;
        public int skippedCount;
        public int warningCount;
        public readonly List<string> messages = new List<string>();

        public void AddInfo(string message)
        {
            messages.Add(message);
        }

        public void AddWarning(string message)
        {
            warningCount++;
            messages.Add("Warning: " + message);
        }
    }

    [Header("Live Timeline Directors")]
    [Tooltip("PlayableDirectors that run the live show TimelineAssets.")]
    [FormerlySerializedAs("directors")]
    public List<PlayableDirector> liveTimelineDirectors = new List<PlayableDirector>();

    [Header("Stage Performers")]
    [Tooltip("Stage performer GameObjects that should provide Animator components for AnimationTrack bindings.")]
    [FormerlySerializedAs("targets")]
    public List<GameObject> stagePerformers = new List<GameObject>();

    [Header("Cue Binding Options")]
    [Tooltip("SetlistOrder uses the performer list order. TrackNameThenSetlist tries cue name matching before falling back to list order.")]
    [FormerlySerializedAs("matchMode")]
    public CueMatchMode cueMatchMode = CueMatchMode.TrackNameThenSetlist;

    [Tooltip("Add an Animator component when a performer GameObject does not already have one.")]
    [FormerlySerializedAs("addAnimatorIfMissing")]
    public bool autoAddPerformerAnimator = true;

    [Tooltip("Replace cue bindings that already point at another object.")]
    [FormerlySerializedAs("overwriteExistingBindings")]
    public bool overwriteLiveBindings = true;

    [Tooltip("Skip muted Timeline tracks.")]
    [FormerlySerializedAs("skipMutedTracks")]
    public bool skipMutedCues = true;

    [Tooltip("Log each cue-level operation instead of only the show summary.")]
    [FormerlySerializedAs("logDetails")]
    public bool logCueDetails = false;

    [Header("Stage Auto Collect Fallback")]
    [Tooltip("When the live director list is empty, use PlayableDirectors on this GameObject and children for this run.")]
    [FormerlySerializedAs("useChildDirectorsWhenListIsEmpty")]
    public bool useChildDirectorsWhenSetlistEmpty = true;

    [Tooltip("When the performer list is empty, use child GameObjects that already have Animator components for this run.")]
    [FormerlySerializedAs("useChildAnimatorsWhenListIsEmpty")]
    public bool useChildPerformersWhenSetlistEmpty = true;

    [Tooltip("Include inactive stage objects when collecting live directors or stage performers.")]
    [FormerlySerializedAs("includeInactiveWhenCollecting")]
    public bool includeInactiveStageObjects = true;

    [ContextMenu("Live: Bind Performer Animators")]
    public void BindPerformersToLiveTimelines()
    {
        LogLiveReport(BindPerformersToLiveTimelines(false));
    }

    public LiveBindingReport BindPerformersToLiveTimelines(bool previewOnly)
    {
        var liveReport = new LiveBindingReport { previewOnly = previewOnly };
        var liveDirectors = GetLiveDirectorCandidates(liveReport);
        var stagePerformersForShow = GetStagePerformerCandidates(liveReport);

        if (liveDirectors.Count == 0)
        {
            liveReport.AddWarning("No PlayableDirector was found for the live show.");
            return liveReport;
        }

        if (stagePerformersForShow.Count == 0)
        {
            liveReport.AddWarning("No stage performer GameObject was found.");
            return liveReport;
        }

        foreach (var liveDirector in liveDirectors)
        {
            BindDirectorCues(liveDirector, stagePerformersForShow, previewOnly, liveReport);
        }

#if UNITY_EDITOR
        if (!previewOnly)
        {
            AssetDatabase.SaveAssets();
        }
#endif

        return liveReport;
    }

    [ContextMenu("Live: Preview Performer Bindings")]
    public void PreviewLiveTimelineBindings()
    {
        LogLiveReport(BindPerformersToLiveTimelines(true));
    }

    [ContextMenu("Live: Clear Performer Bindings")]
    public void ClearLiveTimelineBindings()
    {
        LogLiveReport(ClearLiveTimelineBindings(false));
    }

    public LiveBindingReport ClearLiveTimelineBindings(bool previewOnly)
    {
        var liveReport = new LiveBindingReport { previewOnly = previewOnly };
        var liveDirectors = GetLiveDirectorCandidates(liveReport);

        if (liveDirectors.Count == 0)
        {
            liveReport.AddWarning("No PlayableDirector was found for the live show.");
            return liveReport;
        }

        foreach (var liveDirector in liveDirectors)
        {
            var showTimeline = liveDirector.playableAsset as TimelineAsset;
            if (showTimeline == null)
            {
                liveReport.skippedCount++;
                liveReport.AddWarning(GetLiveDirectorLabel(liveDirector) + " does not reference a TimelineAsset.");
                continue;
            }

            liveReport.liveDirectorCount++;

            foreach (var animationCue in GetAnimationCues(showTimeline))
            {
                liveReport.cueCount++;

                if (skipMutedCues && animationCue.mutedInHierarchy)
                {
                    liveReport.skippedCount++;
                    AddCueDetail(liveReport, GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " skipped because the cue is muted.");
                    continue;
                }

                var currentCueBinding = liveDirector.GetGenericBinding(animationCue);
                if (currentCueBinding == null)
                {
                    liveReport.unchangedCount++;
                    continue;
                }

                if (!previewOnly)
                {
#if UNITY_EDITOR
                    Undo.RecordObject(liveDirector, "Clear Live Timeline Performer Binding");
#endif
                    liveDirector.SetGenericBinding(animationCue, null);
                    MarkStageDirty(liveDirector);
                }

                liveReport.clearedCount++;
                AddCueDetail(liveReport, GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " cleared " + GetStageObjectLabel(currentCueBinding) + ".");
            }
        }

        return liveReport;
    }

#if UNITY_EDITOR
    [ContextMenu("Live: Collect Live Directors From Children")]
    public void CollectLiveDirectorsFromChildren()
    {
        Undo.RecordObject(this, "Collect Live Timeline Directors");
        liveTimelineDirectors = GetComponentsInChildren<PlayableDirector>(includeInactiveStageObjects)
            .Where(liveDirector => liveDirector != null)
            .Distinct()
            .ToList();
        MarkStageDirty(this);
        Debug.Log("[LiveStageAnimatorBinder] Collected " + liveTimelineDirectors.Count + " live director(s).", this);
    }

    [ContextMenu("Live: Collect Performers From Selection")]
    public void CollectPerformersFromSelection()
    {
        Undo.RecordObject(this, "Collect Performers From Selection");
        stagePerformers = Selection.gameObjects
            .Where(performer => performer != null)
            .Distinct()
            .ToList();
        MarkStageDirty(this);
        Debug.Log("[LiveStageAnimatorBinder] Collected " + stagePerformers.Count + " selected performer(s).", this);
    }

    [ContextMenu("Live: Collect Performers From Child Animators")]
    public void CollectPerformersFromChildAnimators()
    {
        Undo.RecordObject(this, "Collect Child Animator Performers");
        stagePerformers = GetComponentsInChildren<Animator>(includeInactiveStageObjects)
            .Where(performerAnimator => performerAnimator != null)
            .Select(performerAnimator => performerAnimator.gameObject)
            .Distinct()
            .ToList();
        MarkStageDirty(this);
        Debug.Log("[LiveStageAnimatorBinder] Collected " + stagePerformers.Count + " child animator performer(s).", this);
    }

    [ContextMenu("Live: Collect Performers From Direct Children")]
    public void CollectPerformersFromDirectChildren()
    {
        Undo.RecordObject(this, "Collect Direct Child Performers");
        stagePerformers = transform.Cast<Transform>()
            .Where(stageSlot => stageSlot != null)
            .Select(stageSlot => stageSlot.gameObject)
            .Distinct()
            .ToList();
        MarkStageDirty(this);
        Debug.Log("[LiveStageAnimatorBinder] Collected " + stagePerformers.Count + " direct child performer(s).", this);
    }

    [ContextMenu("Live: Remove Missing Stage Entries")]
    public void RemoveMissingStageEntries()
    {
        Undo.RecordObject(this, "Remove Missing Live Stage Entries");
        liveTimelineDirectors = liveTimelineDirectors.Where(liveDirector => liveDirector != null).Distinct().ToList();
        stagePerformers = stagePerformers.Where(performer => performer != null).Distinct().ToList();
        MarkStageDirty(this);
        Debug.Log("[LiveStageAnimatorBinder] Removed missing stage entries.", this);
    }
#endif

    private void BindDirectorCues(
        PlayableDirector liveDirector,
        List<GameObject> performers,
        bool previewOnly,
        LiveBindingReport liveReport)
    {
        if (liveDirector == null)
        {
            liveReport.skippedCount++;
            return;
        }

        var showTimeline = liveDirector.playableAsset as TimelineAsset;
        if (showTimeline == null)
        {
            liveReport.skippedCount++;
            liveReport.AddWarning(GetLiveDirectorLabel(liveDirector) + " does not reference a TimelineAsset.");
            return;
        }

        liveReport.liveDirectorCount++;

        var usedPerformers = new HashSet<GameObject>();
        int setlistIndex = 0;

        foreach (var animationCue in GetAnimationCues(showTimeline))
        {
            liveReport.cueCount++;

            if (skipMutedCues && animationCue.mutedInHierarchy)
            {
                liveReport.skippedCount++;
                AddCueDetail(liveReport, GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " skipped because the cue is muted.");
                continue;
            }

            var performer = ResolveStagePerformer(animationCue, performers, usedPerformers, ref setlistIndex);
            if (performer == null)
            {
                liveReport.skippedCount++;
                liveReport.AddWarning(GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " has no matching performer.");
                continue;
            }

            var performerAnimator = performer.GetComponent<Animator>();
            bool willAddPerformerAnimator = false;
            if (performerAnimator == null)
            {
                if (!autoAddPerformerAnimator)
                {
                    liveReport.skippedCount++;
                    liveReport.AddWarning(performer.name + " does not have an Animator component.");
                    continue;
                }

                liveReport.animatorAddedCount++;
                willAddPerformerAnimator = true;

                if (!previewOnly)
                {
#if UNITY_EDITOR
                    performerAnimator = Undo.AddComponent<Animator>(performer);
#else
                    performerAnimator = performer.AddComponent<Animator>();
#endif
                    MarkStageDirty(performer);
                }
            }

            var currentCueBinding = liveDirector.GetGenericBinding(animationCue);
            if (!willAddPerformerAnimator && currentCueBinding == performerAnimator)
            {
                liveReport.unchangedCount++;
                AddCueDetail(liveReport, GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " is already bound to " + performer.name + ".");
                continue;
            }

            if (currentCueBinding != null && !overwriteLiveBindings)
            {
                liveReport.skippedCount++;
                AddCueDetail(
                    liveReport,
                    GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " kept existing binding " + GetStageObjectLabel(currentCueBinding) + ".");
                continue;
            }

            if (!previewOnly)
            {
#if UNITY_EDITOR
                Undo.RecordObject(liveDirector, "Bind Live Timeline Performer");
#endif
                liveDirector.SetGenericBinding(animationCue, performerAnimator);
                MarkStageDirty(liveDirector);
                MarkStageDirty(performer);
            }

            liveReport.boundCount++;
            string addedAnimatorSuffix = willAddPerformerAnimator ? " (Animator will be added)" : string.Empty;
            AddCueDetail(liveReport, GetLiveDirectorLabel(liveDirector) + " / " + animationCue.name + " => " + performer.name + addedAnimatorSuffix + ".");
        }
    }

    private List<PlayableDirector> GetLiveDirectorCandidates(LiveBindingReport liveReport)
    {
        var liveDirectors = liveTimelineDirectors != null
            ? liveTimelineDirectors.Where(liveDirector => liveDirector != null).Distinct().ToList()
            : new List<PlayableDirector>();

        if (liveDirectors.Count == 0 && useChildDirectorsWhenSetlistEmpty)
        {
            liveDirectors = GetComponentsInChildren<PlayableDirector>(includeInactiveStageObjects)
                .Where(liveDirector => liveDirector != null)
                .Distinct()
                .ToList();

            if (liveDirectors.Count > 0)
            {
                liveReport.AddInfo("Using " + liveDirectors.Count + " child live director(s) because the director list is empty.");
            }
        }

        return liveDirectors;
    }

    private List<GameObject> GetStagePerformerCandidates(LiveBindingReport liveReport)
    {
        var performers = stagePerformers != null
            ? stagePerformers.Where(performer => performer != null).Distinct().ToList()
            : new List<GameObject>();

        if (performers.Count == 0 && useChildPerformersWhenSetlistEmpty)
        {
            performers = GetComponentsInChildren<Animator>(includeInactiveStageObjects)
                .Where(performerAnimator => performerAnimator != null)
                .Select(performerAnimator => performerAnimator.gameObject)
                .Distinct()
                .ToList();

            if (performers.Count > 0)
            {
                liveReport.AddInfo("Using " + performers.Count + " child animator performer(s) because the performer list is empty.");
            }
        }

        return performers;
    }

    private IEnumerable<AnimationTrack> GetAnimationCues(TimelineAsset showTimeline)
    {
        return showTimeline.GetOutputTracks().OfType<AnimationTrack>();
    }

    private GameObject ResolveStagePerformer(
        AnimationTrack animationCue,
        List<GameObject> performers,
        HashSet<GameObject> usedPerformers,
        ref int setlistIndex)
    {
        if (cueMatchMode == CueMatchMode.TrackNameThenSetlist || cueMatchMode == CueMatchMode.TrackNameOnly)
        {
            var matchedPerformer = FindPerformerByCueName(animationCue, performers, usedPerformers);
            if (matchedPerformer != null)
            {
                usedPerformers.Add(matchedPerformer);
                return matchedPerformer;
            }
        }

        if (cueMatchMode == CueMatchMode.TrackNameOnly)
        {
            return null;
        }

        while (setlistIndex < performers.Count)
        {
            var performer = performers[setlistIndex];
            setlistIndex++;

            if (performer == null || usedPerformers.Contains(performer))
            {
                continue;
            }

            usedPerformers.Add(performer);
            return performer;
        }

        return null;
    }

    private GameObject FindPerformerByCueName(
        AnimationTrack animationCue,
        List<GameObject> performers,
        HashSet<GameObject> usedPerformers)
    {
        var cueName = NormalizeCueName(animationCue.name);
        if (string.IsNullOrEmpty(cueName))
        {
            return null;
        }

        var availablePerformers = performers.Where(performer => performer != null && !usedPerformers.Contains(performer)).ToList();

        var exactPerformer = availablePerformers.FirstOrDefault(performer => NormalizeCueName(performer.name) == cueName);
        if (exactPerformer != null)
        {
            return exactPerformer;
        }

        return availablePerformers.FirstOrDefault(performer =>
        {
            var performerName = NormalizeCueName(performer.name);
            return performerName.Length >= 3 &&
                   (cueName.Contains(performerName) || performerName.Contains(cueName));
        });
    }

    private static string NormalizeCueName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var cueChars = new List<char>(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                cueChars.Add(char.ToLowerInvariant(c));
            }
        }

        var normalizedCueName = new string(cueChars.ToArray());
        normalizedCueName = normalizedCueName.Replace("animationtrack", string.Empty);
        normalizedCueName = normalizedCueName.Replace("animation", string.Empty);
        normalizedCueName = normalizedCueName.Replace("animator", string.Empty);
        normalizedCueName = normalizedCueName.Replace("performer", string.Empty);
        normalizedCueName = normalizedCueName.Replace("stage", string.Empty);
        normalizedCueName = normalizedCueName.Replace("track", string.Empty);
        normalizedCueName = normalizedCueName.Replace("avatar", string.Empty);
        normalizedCueName = normalizedCueName.Replace("cue", string.Empty);
        return normalizedCueName;
    }

    private void LogLiveReport(LiveBindingReport liveReport)
    {
        foreach (var message in liveReport.messages)
        {
            if (message.StartsWith("Warning:", StringComparison.Ordinal))
            {
                Debug.LogWarning("[LiveStageAnimatorBinder] " + message.Substring("Warning:".Length).Trim(), this);
            }
            else if (logCueDetails || liveReport.previewOnly)
            {
                Debug.Log("[LiveStageAnimatorBinder] " + message, this);
            }
        }

        string mode = liveReport.previewOnly ? "Preview" : "Show Ready";
        Debug.Log(
            "[LiveStageAnimatorBinder] " + mode +
            " liveDirectors=" + liveReport.liveDirectorCount +
            ", cues=" + liveReport.cueCount +
            ", bound=" + liveReport.boundCount +
            ", cleared=" + liveReport.clearedCount +
            ", animatorAdded=" + liveReport.animatorAddedCount +
            ", unchanged=" + liveReport.unchangedCount +
            ", skipped=" + liveReport.skippedCount +
            ", warnings=" + liveReport.warningCount,
            this);
    }

    private void AddCueDetail(LiveBindingReport liveReport, string message)
    {
        if (logCueDetails || liveReport.previewOnly)
        {
            liveReport.AddInfo(message);
        }
    }

    private static string GetLiveDirectorLabel(PlayableDirector liveDirector)
    {
        return liveDirector != null ? liveDirector.name : "(missing live director)";
    }

    private static string GetStageObjectLabel(UnityEngine.Object stageObject)
    {
        return stageObject != null ? stageObject.name : "(none)";
    }

    private static void MarkStageDirty(UnityEngine.Object stageObject)
    {
#if UNITY_EDITOR
        if (stageObject == null)
        {
            return;
        }

        EditorUtility.SetDirty(stageObject);

        var stageComponent = stageObject as Component;
        if (stageComponent != null && stageComponent.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(stageComponent.gameObject.scene);
            return;
        }

        var stageGameObject = stageObject as GameObject;
        if (stageGameObject != null && stageGameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(stageGameObject.scene);
        }
#endif
    }
}
