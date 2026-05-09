using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LTCread : MonoBehaviour
{
    public enum FrameRate
    {
        FPS24 = 24,
        FPS30 = 30,
        FPS60 = 60
    }

    [Header("Input")]
    [SerializeField] private Dropdown deviceDropdown;
    [SerializeField] private string initialDeviceName;
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private FrameRate frameRate = FrameRate.FPS30;
    [SerializeField, Range(0.0f, 1.0f)] private float audioThreshold = 0.01f;

    [Header("Display")]
    [SerializeField] private bool showOnGUI = true;
    [SerializeField] private Text timecodeText;
    [SerializeField] private Text ltcSecondsText;
    [SerializeField] private int guiFontSize = 64;

    private const int DeviceRecordLength = 10;
    private const string SyncWord = "0011111111111101";

    private AudioClip ltcAudioInput;
    private string selectedDeviceName;
    private int lastAudioPos;
    private int sameAudioLevelCount;
    private int lastAudioLevel;
    private int lastBitCount;
    private string bitPattern = "";
    private string timeCode = "00:00:00;00";
    private float gain;
    private double currentSeconds;
    private bool hasSignal;
    private GUIStyle timeCodeStyle;

    public string CurrentTimeCode => timeCode;
    public double CurrentTimeSeconds => currentSeconds;
    public float Gain => gain;
    public bool HasSignal => hasSignal;
    public string SelectedDeviceName => selectedDeviceName;
    public int FramesPerSecond => Mathf.Max(1, (int)frameRate);

    private void Start()
    {
        RefreshDeviceDropdown();
        StartMicrophone(ResolveInitialDevice());
        EnsureGuiStyle();
    }

    private void OnDestroy()
    {
        StopMicrophone();
    }

    private void Update()
    {
        DecodeAudioToTcFrames();
        UpdateText();
    }

    private void OnGUI()
    {
        if (!showOnGUI)
            return;

        EnsureGuiStyle();
        GUI.Label(new Rect(10, 10, 640, guiFontSize + 8), timeCode, timeCodeStyle);

        var deviceLabel = string.IsNullOrEmpty(selectedDeviceName) ? "No input device" : selectedDeviceName;
        GUI.Label(new Rect(10, 20 + guiFontSize, 900, guiFontSize + 8), deviceLabel, timeCodeStyle);
    }

    private void RefreshDeviceDropdown()
    {
        if (deviceDropdown == null)
            return;

        deviceDropdown.ClearOptions();
        deviceDropdown.AddOptions(Microphone.devices.ToList());
        deviceDropdown.onValueChanged.RemoveListener(OnDeviceDropdownValueChanged);
        deviceDropdown.onValueChanged.AddListener(OnDeviceDropdownValueChanged);
    }

    private string ResolveInitialDevice()
    {
        if (!string.IsNullOrEmpty(initialDeviceName))
        {
            var matchedDevice = Microphone.devices.FirstOrDefault(device => device.Contains(initialDeviceName));
            if (!string.IsNullOrEmpty(matchedDevice))
                return matchedDevice;
        }

        return Microphone.devices.Length > 0 ? Microphone.devices[0] : "";
    }

    private void OnDeviceDropdownValueChanged(int value)
    {
        if (deviceDropdown == null || value < 0 || value >= deviceDropdown.options.Count)
            return;

        StartMicrophone(deviceDropdown.options[value].text);
    }

    private void StartMicrophone(string deviceName)
    {
        StopMicrophone();

        if (string.IsNullOrEmpty(deviceName))
        {
            selectedDeviceName = "";
            return;
        }

        selectedDeviceName = deviceName;
        ltcAudioInput = Microphone.Start(selectedDeviceName, true, DeviceRecordLength, sampleRate);
        lastAudioPos = 0;
        sameAudioLevelCount = 0;
        lastAudioLevel = 0;
        lastBitCount = 0;
        bitPattern = "";
    }

    private void StopMicrophone()
    {
        if (!string.IsNullOrEmpty(selectedDeviceName))
            Microphone.End(selectedDeviceName);

        ltcAudioInput = null;
    }

    private void DecodeAudioToTcFrames()
    {
        if (ltcAudioInput == null || string.IsNullOrEmpty(selectedDeviceName))
        {
            hasSignal = false;
            return;
        }

        var waveData = GetUpdatedAudio(ltcAudioInput);
        if (waveData.Length == 0)
        {
            hasSignal = false;
            return;
        }

        gain = 0f;
        for (var i = 0; i < waveData.Length; i += Mathf.Max(1, ltcAudioInput.channels))
            gain += Mathf.Abs(waveData[i]);

        gain /= Mathf.Max(1, waveData.Length / Mathf.Max(1, ltcAudioInput.channels));
        hasSignal = gain >= audioThreshold;
        if (!hasSignal)
            return;

        var pos = 0;
        var bitThreshold = Mathf.Max(2, Mathf.RoundToInt(ltcAudioInput.frequency / (FramesPerSecond * 103.333f)));
        while (pos < waveData.Length)
        {
            var count = CheckAudioLevelChanged(waveData, ref pos, ltcAudioInput.channels);
            if (count <= 0)
                continue;

            if (count < bitThreshold)
            {
                if (lastBitCount < bitThreshold)
                {
                    bitPattern += "1";
                    lastBitCount = bitThreshold;
                }
                else
                {
                    lastBitCount = count;
                }
            }
            else
            {
                bitPattern += "0";
                lastBitCount = count;
            }

            TryExtractFrame();
        }

        if (bitPattern.Length > 320)
            bitPattern = bitPattern.Substring(bitPattern.Length - 160);
    }

    private float[] GetUpdatedAudio(AudioClip audioClip)
    {
        var nowAudioPos = Microphone.GetPosition(selectedDeviceName);
        if (nowAudioPos < 0)
            return Array.Empty<float>();

        var channels = Mathf.Max(1, audioClip.channels);
        float[] waveData;

        if (lastAudioPos < nowAudioPos)
        {
            var sampleCount = nowAudioPos - lastAudioPos;
            waveData = new float[sampleCount * channels];
            audioClip.GetData(waveData, lastAudioPos);
        }
        else if (lastAudioPos > nowAudioPos)
        {
            var tailSampleCount = audioClip.samples - lastAudioPos;
            var headSampleCount = nowAudioPos;
            var tail = new float[tailSampleCount * channels];
            var head = new float[headSampleCount * channels];

            audioClip.GetData(tail, lastAudioPos);
            if (headSampleCount > 0)
                audioClip.GetData(head, 0);

            waveData = new float[tail.Length + head.Length];
            tail.CopyTo(waveData, 0);
            head.CopyTo(waveData, tail.Length);
        }
        else
        {
            waveData = Array.Empty<float>();
        }

        lastAudioPos = nowAudioPos;
        return waveData;
    }

    private int CheckAudioLevelChanged(float[] data, ref int pos, int channels)
    {
        channels = Mathf.Max(1, channels);
        while (pos < data.Length)
        {
            var nowLevel = data[pos] >= 0f ? 1 : -1;
            if (lastAudioLevel != 0 && lastAudioLevel != nowLevel)
            {
                var count = sameAudioLevelCount;
                sameAudioLevelCount = 0;
                lastAudioLevel = nowLevel;
                return count;
            }

            lastAudioLevel = nowLevel;
            sameAudioLevelCount++;
            pos += channels;
        }

        return -1;
    }

    private void TryExtractFrame()
    {
        while (bitPattern.Length >= 80)
        {
            var syncPos = bitPattern.IndexOf(SyncWord, StringComparison.Ordinal);
            if (syncPos < 0)
                return;

            var frameEnd = syncPos + SyncWord.Length;
            if (frameEnd < 80)
            {
                bitPattern = bitPattern.Substring(frameEnd);
                continue;
            }

            var frameBits = bitPattern.Substring(frameEnd - 80, 80);
            bitPattern = bitPattern.Substring(frameEnd);
            if (TryDecodeBitsToFrame(frameBits, out var decodedTimeCode, out var seconds))
            {
                timeCode = decodedTimeCode;
                currentSeconds = seconds;
                return;
            }
        }
    }

    private bool TryDecodeBitsToFrame(string bits, out string decodedTimeCode, out double seconds)
    {
        var frames = DecodeBits(bits, 0, 4) + DecodeBits(bits, 8, FramesPerSecond >= 60 ? 3 : 2) * 10;
        var secs = DecodeBits(bits, 16, 4) + DecodeBits(bits, 24, 3) * 10;
        var mins = DecodeBits(bits, 32, 4) + DecodeBits(bits, 40, 3) * 10;
        var hours = DecodeBits(bits, 48, 4) + DecodeBits(bits, 56, 2) * 10;
        var dropFrame = bits.Length > 10 && bits[10] == '1';

        if (frames >= FramesPerSecond || secs >= 60 || mins >= 60 || hours >= 24)
        {
            decodedTimeCode = timeCode;
            seconds = currentSeconds;
            return false;
        }

        var separator = dropFrame ? ";" : ":";
        decodedTimeCode = string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}{3}{4:D2}", hours, mins, secs, separator, frames);
        seconds = ToSeconds(hours, mins, secs, frames, FramesPerSecond);
        return true;
    }

    private static int DecodeBits(string bits, int start, int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++)
        {
            if (bits[start + i] == '1')
                value += 1 << i;
        }

        return value;
    }

    public static bool TryParseTimeCode(string value, int fps, out double seconds)
    {
        seconds = 0d;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(':', ';');
        if (parts.Length != 4)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var secs) ||
            !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var frames))
            return false;

        seconds = ToSeconds(hours, minutes, secs, frames, Mathf.Max(1, fps));
        return true;
    }

    private static double ToSeconds(int hours, int minutes, int seconds, int frames, int fps)
    {
        return hours * 3600d + minutes * 60d + seconds + frames / (double)Mathf.Max(1, fps);
    }

    private void UpdateText()
    {
        if (timecodeText != null)
            timecodeText.text = timeCode;

        if (ltcSecondsText != null)
            ltcSecondsText.text = currentSeconds.ToString("F3", CultureInfo.InvariantCulture);
    }

    private void EnsureGuiStyle()
    {
        if (timeCodeStyle != null)
        {
            timeCodeStyle.fontSize = guiFontSize;
            return;
        }

        timeCodeStyle = new GUIStyle
        {
            fontSize = guiFontSize,
            normal = { textColor = Color.white }
        };
    }
}
