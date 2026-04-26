

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// [RequireComponent(typeof(PlayableDirector))]
public class TimelineSync : MonoBehaviour
{
    public LTCread ltcReader; // LTCread スクリプトへの参照
    [SerializeField]
    private PlayableDirector director;
    private double lastLTCtime = 0;
    private int fps = 30;  // or any appropriate default value

    private void Start()
    {
        // director = GetComponent<PlayableDirector>();
        director.Play();
    }

    private void Update()
    {
        double currentLTCtime = ConvertTimeCodeToSeconds(ltcReader.CurrentTimeCode);
    //Debug.Log($"Current LTC Time: {currentLTCtime}");
    
    SyncTimeline(currentLTCtime);
    lastLTCtime = currentLTCtime;
    }

    // LTCタイムコードを秒数に変換する
    private double ConvertTimeCodeToSeconds(string timeCode) 
{
    string[] parts = timeCode.Split(':', ';');
    int hours = int.Parse(parts[0]);
    int minutes = int.Parse(parts[1]);
    int seconds = int.Parse(parts[2]);
    int frames = int.Parse(parts[3]);

    double frameDuration = 1.0 / fps; // fpsを60や30などの適切な値に設定する
    return hours * 3600 + minutes * 60 + seconds + frames * frameDuration; 
}

    // Timelineを指定した時間に同期させる
    private void SyncTimeline(double seconds)
    {
        //Debug.Log($"Director Time: {director.time}, Setting Time: {seconds}");
    director.time = seconds;
    //director.Evaluate();
    }
}



