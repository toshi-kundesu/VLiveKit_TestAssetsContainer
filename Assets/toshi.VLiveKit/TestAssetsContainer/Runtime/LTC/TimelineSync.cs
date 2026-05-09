using UnityEngine;
using UnityEngine.Playables;

public class TimelineSync : MonoBehaviour
{
    [SerializeField] private LTCread ltcReader;
    [SerializeField] private PlayableDirector director;
    [SerializeField] private int fallbackFps = 30;
    private double lastLtcTime = -1d;

    private void Start()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        director?.Play();
    }

    private void Update()
    {
        if (ltcReader == null || director == null)
            return;

        var fps = ltcReader.FramesPerSecond > 0 ? ltcReader.FramesPerSecond : fallbackFps;
        if (!LTCread.TryParseTimeCode(ltcReader.CurrentTimeCode, fps, out var currentLtcTime))
            return;

        SyncTimeline(currentLtcTime);
        lastLtcTime = currentLtcTime;
    }

    private void SyncTimeline(double seconds)
    {
        if (Mathf.Approximately((float)lastLtcTime, (float)seconds))
            return;

        director.time = seconds;
        director.Evaluate();
    }
}
