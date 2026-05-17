#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

public sealed class TimecodeMonitorWindow : EditorWindow
{
    private const int LtcRecordLengthSeconds = 10;
    private const double RepaintInterval = 1d / 30d;

    private GUIStyle timecodeStyle;
    private GUIStyle statusStyle;
    private GUIStyle sectionStyle;
    private GUIStyle panelStyle;
    private Vector2 scroll;
    private double lastRepaintTime;

    private readonly EditorLtcReader ltcReader = new EditorLtcReader();
    private readonly EditorOscReceiver oscReceiver = new EditorOscReceiver();
    private readonly EditorOscSender oscSender = new EditorOscSender();

    private string[] inputDevices = Array.Empty<string>();
    private int selectedDeviceIndex;
    private int ltcSampleRate = 44100;
    private LTCread.FrameRate ltcFrameRate = LTCread.FrameRate.FPS30;
    private float ltcThreshold = 0.01f;

    private int oscReceiverPort = 10000;
    private LTCread.FrameRate oscReceiverFrameRate = LTCread.FrameRate.FPS30;
    private string oscTimecodeAddress = "/timecode";
    private string oscTotalSecondsAddress = "/total_seconds";
    private string oscHourAddress = "/hour";
    private string oscMinuteAddress = "/minute";
    private string oscSecondAddress = "/second";
    private string oscFrameAddress = "/frame";

    private string oscSenderHost = "127.0.0.1";
    private int oscSenderPort = 10000;
    private LTCread.FrameRate oscSenderFrameRate = LTCread.FrameRate.FPS30;
    private string oscSenderTimecodeAddress = "/timecode";
    private bool oscSenderSendSplitFields;
    private bool oscSenderSendTotalSeconds = true;

    [MenuItem("toshi/VLiveKit/Test Assets/Timecode Monitor")]
    private static void Open()
    {
        GetWindow<TimecodeMonitorWindow>("Timecode Monitor");
    }

    private void OnEnable()
    {
        RefreshInputDevices();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        ltcReader.Stop();
        oscReceiver.Stop();
        oscSender.Stop();
    }

    private void OnGUI()
    {
        EnsureStyles();

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scrollView.scrollPosition;
            DrawHeader();
            DrawEditorLtcReader();
            DrawEditorOscReceiver();
            DrawEditorOscSender();
            DrawSceneComponents();
        }
    }

    private void OnEditorUpdate()
    {
        ltcReader.Tick();
        oscSender.Tick();

        if (EditorApplication.timeSinceStartup - lastRepaintTime < RepaintInterval)
            return;

        lastRepaintTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Timecode Monitor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Monitor LTC and OSC timecode directly in this editor window.", statusStyle);
        EditorGUILayout.Space(6);
    }

    private void DrawEditorLtcReader()
    {
        using (new EditorGUILayout.VerticalScope(panelStyle))
        {
            EditorGUILayout.LabelField("Window LTC Reader", sectionStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(inputDevices.Length == 0 || ltcReader.IsRunning))
                    selectedDeviceIndex = EditorGUILayout.Popup("Input", Mathf.Clamp(selectedDeviceIndex, 0, Mathf.Max(0, inputDevices.Length - 1)), inputDevices);

                using (new EditorGUI.DisabledScope(ltcReader.IsRunning))
                {
                    if (GUILayout.Button("Refresh", GUILayout.Width(78)))
                        RefreshInputDevices();
                }
            }

            using (new EditorGUI.DisabledScope(ltcReader.IsRunning))
            {
                ltcFrameRate = (LTCread.FrameRate)EditorGUILayout.EnumPopup("FPS", ltcFrameRate);
                ltcSampleRate = Mathf.Max(8000, EditorGUILayout.IntField("Sample Rate", ltcSampleRate));
                ltcThreshold = EditorGUILayout.Slider("Signal Threshold", ltcThreshold, 0.001f, 0.25f);
            }

            DrawTimecodeBox(
                ltcReader.CurrentTimeCode,
                ltcReader.IsRunning
                    ? $"Device: {Fallback(ltcReader.DeviceName, "None")}  Gain: {ltcReader.Gain:F5}  Signal: {(ltcReader.HasSignal ? "Yes" : "No")}"
                    : GetLtcIdleStatus());

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(inputDevices.Length == 0 || ltcReader.IsRunning))
                {
                    if (GUILayout.Button("Start LTC Reader"))
                        StartLtcReader();
                }

                using (new EditorGUI.DisabledScope(!ltcReader.IsRunning))
                {
                    if (GUILayout.Button("Stop"))
                        ltcReader.Stop();
                }
            }
        }
    }

    private void DrawEditorOscReceiver()
    {
        using (new EditorGUILayout.VerticalScope(panelStyle))
        {
            EditorGUILayout.LabelField("Window OSC Receiver", sectionStyle);

            using (new EditorGUI.DisabledScope(oscReceiver.IsRunning))
            {
                oscReceiverPort = Mathf.Clamp(EditorGUILayout.IntField("Port", oscReceiverPort), 1, 65535);
                oscReceiverFrameRate = (LTCread.FrameRate)EditorGUILayout.EnumPopup("FPS", oscReceiverFrameRate);
                oscTimecodeAddress = EditorGUILayout.TextField("Timecode", oscTimecodeAddress);
                oscTotalSecondsAddress = EditorGUILayout.TextField("Total Seconds", oscTotalSecondsAddress);
            }

            DrawTimecodeBox(
                oscReceiver.CurrentTimeCode,
                oscReceiver.IsRunning
                    ? $"Port: {oscReceiverPort}  Seconds: {oscReceiver.CurrentSeconds:F3}  Last: {Fallback(oscReceiver.LastAddress, "None")}  Received: {(oscReceiver.HasReceived ? "Yes" : "No")}"
                    : "Receives OSC int, float, or string values without Play Mode.");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(oscReceiver.IsRunning))
                {
                    if (GUILayout.Button("Start OSC Receiver"))
                        oscReceiver.Start(new OscReceiverSettings(
                            oscReceiverPort,
                            (int)oscReceiverFrameRate,
                            oscTimecodeAddress,
                            oscTotalSecondsAddress,
                            oscHourAddress,
                            oscMinuteAddress,
                            oscSecondAddress,
                            oscFrameAddress));
                }

                using (new EditorGUI.DisabledScope(!oscReceiver.IsRunning))
                {
                    if (GUILayout.Button("Stop"))
                        oscReceiver.Stop();
                }
            }
        }
    }

    private void DrawEditorOscSender()
    {
        using (new EditorGUILayout.VerticalScope(panelStyle))
        {
            EditorGUILayout.LabelField("Window OSC Sender", sectionStyle);

            using (new EditorGUI.DisabledScope(oscSender.IsRunning))
            {
                oscSenderHost = EditorGUILayout.TextField("Host", oscSenderHost);
                oscSenderPort = Mathf.Clamp(EditorGUILayout.IntField("Port", oscSenderPort), 1, 65535);
                oscSenderFrameRate = (LTCread.FrameRate)EditorGUILayout.EnumPopup("FPS", oscSenderFrameRate);
                oscSenderTimecodeAddress = EditorGUILayout.TextField("Timecode", oscSenderTimecodeAddress);
                oscSenderSendTotalSeconds = EditorGUILayout.Toggle("Send Total Seconds", oscSenderSendTotalSeconds);
                oscSenderSendSplitFields = EditorGUILayout.Toggle("Send H/M/S/F", oscSenderSendSplitFields);
            }

            DrawTimecodeBox(
                oscSender.CurrentTimeCode,
                oscSender.IsRunning
                    ? $"Sending to {oscSenderHost}:{oscSenderPort}  Seconds: {oscSender.CurrentSeconds:F3}"
                    : "Sends editor-clock timecode as OSC. Use this for loopback tests.");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(oscSender.IsRunning))
                {
                    if (GUILayout.Button("Start OSC Sender"))
                        StartOscSender();
                }

                using (new EditorGUI.DisabledScope(!oscSender.IsRunning))
                {
                    if (GUILayout.Button("Stop"))
                        oscSender.Stop();
                }

                using (new EditorGUI.DisabledScope(oscSender.IsRunning))
                {
                    if (GUILayout.Button("Send Once"))
                        SendOscOnce();
                }
            }
        }
    }

    private void DrawSceneComponents()
    {
        using (new EditorGUILayout.VerticalScope(panelStyle))
        {
            EditorGUILayout.LabelField("Scene Components", sectionStyle);
            DrawLtcReaders();
            DrawOscReceivers();
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
            DrawTimecodeBox(
                reader.CurrentTimeCode,
                $"Object: {reader.name}  Device: {Fallback(reader.SelectedDeviceName, "None")}  FPS: {reader.FramesPerSecond}  Gain: {reader.Gain:F5}  Signal: {(reader.HasSignal ? "Yes" : "No")}");
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
            DrawTimecodeBox(
                receiver.CurrentTimeCode,
                $"Object: {receiver.name}  Seconds: {receiver.CurrentTimeSeconds:F3}  FPS: {receiver.FramesPerSecond}  Last: {Fallback(receiver.LastAddress, "None")}  Received: {(receiver.HasReceived ? "Yes" : "No")}");
        }
    }

    private void DrawTimecodeBox(string timecode, string detail)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(timecode, timecodeStyle, GUILayout.Height(42));
        EditorGUILayout.LabelField(detail, statusStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void StartLtcReader()
    {
        RefreshInputDevices();
        if (inputDevices.Length == 0)
            return;

        selectedDeviceIndex = Mathf.Clamp(selectedDeviceIndex, 0, inputDevices.Length - 1);
        ltcReader.Start(inputDevices[selectedDeviceIndex], ltcSampleRate, (int)ltcFrameRate, ltcThreshold);
    }

    private void StartOscSender()
    {
        oscSender.Start(new OscSenderSettings(
            oscSenderHost,
            oscSenderPort,
            (int)oscSenderFrameRate,
            oscSenderTimecodeAddress,
            oscTotalSecondsAddress,
            oscHourAddress,
            oscMinuteAddress,
            oscSecondAddress,
            oscFrameAddress,
            oscSenderSendTotalSeconds,
            oscSenderSendSplitFields));
    }

    private void SendOscOnce()
    {
        using (var sender = new EditorOscSender())
        {
            sender.Start(new OscSenderSettings(
                oscSenderHost,
                oscSenderPort,
                (int)oscSenderFrameRate,
                oscSenderTimecodeAddress,
                oscTotalSecondsAddress,
                oscHourAddress,
                oscMinuteAddress,
                oscSecondAddress,
                oscFrameAddress,
                oscSenderSendTotalSeconds,
                oscSenderSendSplitFields));
            sender.SendNow();
        }
    }

    private void RefreshInputDevices()
    {
        inputDevices = Microphone.devices ?? Array.Empty<string>();
        selectedDeviceIndex = Mathf.Clamp(selectedDeviceIndex, 0, Mathf.Max(0, inputDevices.Length - 1));
    }

    private string GetLtcIdleStatus()
    {
        return inputDevices.Length == 0
            ? "No microphone input devices found."
            : "Reads LTC from a microphone/input device without Play Mode.";
    }

    private static string Fallback(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private void EnsureStyles()
    {
        if (timecodeStyle != null && statusStyle != null && sectionStyle != null && panelStyle != null)
            return;

        timecodeStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        statusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true
        };

        sectionStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11
        };

        panelStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 7, 8),
            margin = new RectOffset(4, 4, 4, 6)
        };
    }

    private sealed class EditorLtcReader
    {
        private string deviceName = "";
        private AudioClip audioClip;
        private int sampleRate;
        private int fps = 30;
        private float threshold;
        private int lastAudioPos;
        private int sameAudioLevelCount;
        private int lastAudioLevel;
        private int lastBitCount;
        private string bitPattern = "";
        private string currentTimeCode = "00:00:00;00";
        private double currentSeconds;
        private float gain;
        private bool hasSignal;

        public bool IsRunning => audioClip != null;
        public string DeviceName => deviceName;
        public string CurrentTimeCode => currentTimeCode;
        public double CurrentSeconds => currentSeconds;
        public float Gain => gain;
        public bool HasSignal => hasSignal;

        public void Start(string selectedDeviceName, int selectedSampleRate, int selectedFps, float selectedThreshold)
        {
            Stop();
            if (string.IsNullOrEmpty(selectedDeviceName))
                return;

            deviceName = selectedDeviceName;
            sampleRate = Mathf.Max(8000, selectedSampleRate);
            fps = Mathf.Max(1, selectedFps);
            threshold = Mathf.Max(0.0001f, selectedThreshold);
            audioClip = Microphone.Start(deviceName, true, LtcRecordLengthSeconds, sampleRate);
            lastAudioPos = 0;
            sameAudioLevelCount = 0;
            lastAudioLevel = 0;
            lastBitCount = 0;
            bitPattern = "";
            currentTimeCode = "00:00:00;00";
            currentSeconds = 0d;
        }

        public void Stop()
        {
            if (!string.IsNullOrEmpty(deviceName))
                Microphone.End(deviceName);

            audioClip = null;
            deviceName = "";
            hasSignal = false;
            gain = 0f;
        }

        public void Tick()
        {
            if (audioClip == null || string.IsNullOrEmpty(deviceName))
                return;

            var waveData = GetUpdatedAudio();
            if (waveData.Length == 0)
                return;

            gain = 0f;
            var channels = Mathf.Max(1, audioClip.channels);
            for (var i = 0; i < waveData.Length; i += channels)
                gain += Mathf.Abs(waveData[i]);

            gain /= Mathf.Max(1, waveData.Length / channels);
            hasSignal = gain >= threshold;
            if (!hasSignal)
                return;

            DecodeAudio(waveData, channels);
        }

        private float[] GetUpdatedAudio()
        {
            var nowAudioPos = Microphone.GetPosition(deviceName);
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

        private void DecodeAudio(float[] waveData, int channels)
        {
            var pos = 0;
            var bitThreshold = Mathf.Max(2, Mathf.RoundToInt(audioClip.frequency / (fps * 103.333f)));
            while (pos < waveData.Length)
            {
                var count = CheckAudioLevelChanged(waveData, ref pos, channels);
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

        private int CheckAudioLevelChanged(float[] data, ref int pos, int channels)
        {
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
            const string syncWord = "0011111111111101";
            while (bitPattern.Length >= 80)
            {
                var syncPos = bitPattern.IndexOf(syncWord, StringComparison.Ordinal);
                if (syncPos < 0)
                    return;

                var frameEnd = syncPos + syncWord.Length;
                if (frameEnd < 80)
                {
                    bitPattern = bitPattern.Substring(frameEnd);
                    continue;
                }

                var frameBits = bitPattern.Substring(frameEnd - 80, 80);
                bitPattern = bitPattern.Substring(frameEnd);
                if (TryDecodeBitsToFrame(frameBits, out var decodedTimeCode, out var seconds))
                {
                    currentTimeCode = decodedTimeCode;
                    currentSeconds = seconds;
                    return;
                }
            }
        }

        private bool TryDecodeBitsToFrame(string bits, out string decodedTimeCode, out double seconds)
        {
            var frames = DecodeBits(bits, 0, 4) + DecodeBits(bits, 8, fps >= 60 ? 3 : 2) * 10;
            var secs = DecodeBits(bits, 16, 4) + DecodeBits(bits, 24, 3) * 10;
            var mins = DecodeBits(bits, 32, 4) + DecodeBits(bits, 40, 3) * 10;
            var hours = DecodeBits(bits, 48, 4) + DecodeBits(bits, 56, 2) * 10;
            var dropFrame = bits.Length > 10 && bits[10] == '1';

            if (frames >= fps || secs >= 60 || mins >= 60 || hours >= 24)
            {
                decodedTimeCode = currentTimeCode;
                seconds = currentSeconds;
                return false;
            }

            var separator = dropFrame ? ";" : ":";
            decodedTimeCode = string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}{3}{4:D2}", hours, mins, secs, separator, frames);
            seconds = hours * 3600d + mins * 60d + secs + frames / (double)fps;
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
    }

    private sealed class EditorOscReceiver
    {
        private readonly object stateLock = new object();
        private UdpClient udpClient;
        private Thread receiveThread;
        private volatile bool running;
        private OscReceiverSettings settings;
        private float hour;
        private float minute;
        private float second;
        private float frame;
        private float totalSeconds;
        private bool useTotalSeconds;
        private string currentTimeCode = "00:00:00:00";
        private double currentSeconds;
        private string lastAddress = "";
        private bool hasReceived;

        public bool IsRunning => running;
        public string CurrentTimeCode { get { lock (stateLock) return currentTimeCode; } }
        public double CurrentSeconds { get { lock (stateLock) return currentSeconds; } }
        public string LastAddress { get { lock (stateLock) return lastAddress; } }
        public bool HasReceived { get { lock (stateLock) return hasReceived; } }

        public void Start(OscReceiverSettings receiverSettings)
        {
            Stop();
            settings = receiverSettings;

            try
            {
                udpClient = new UdpClient(settings.Port);
                running = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();
            }
            catch (Exception exception)
            {
                running = false;
                Debug.Log($"VLiveKit OSC receiver could not start on port {settings.Port}: {exception.Message}");
            }
        }

        public void Stop()
        {
            running = false;
            udpClient?.Close();
            udpClient = null;

            if (receiveThread != null && receiveThread.IsAlive)
                receiveThread.Join(100);

            receiveThread = null;
        }

        private void ReceiveLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    var bytes = udpClient.Receive(ref endpoint);
                    if (TryParseOscMessage(bytes, out var address, out var value))
                        ApplyOscValue(address, value);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    if (running)
                        Thread.Sleep(5);
                }
                catch (Exception)
                {
                    Thread.Sleep(5);
                }
            }
        }

        private void ApplyOscValue(string address, object value)
        {
            lock (stateLock)
            {
                lastAddress = address;
                hasReceived = true;

                if (address == settings.HourAddress) { hour = ToFloat(value, hour); useTotalSeconds = false; }
                else if (address == settings.MinuteAddress) { minute = ToFloat(value, minute); useTotalSeconds = false; }
                else if (address == settings.SecondAddress) { second = ToFloat(value, second); useTotalSeconds = false; }
                else if (address == settings.FrameAddress) { frame = ToFloat(value, frame); useTotalSeconds = false; }
                else if (address == settings.TotalSecondsAddress) { totalSeconds = ToFloat(value, totalSeconds); useTotalSeconds = true; }
                else if (address == settings.TimecodeAddress && value is string text && TryParseTimeCode(text, settings.Fps, out var parsedHour, out var parsedMinute, out var parsedSecond, out var parsedFrame, out var seconds))
                {
                    hour = parsedHour;
                    minute = parsedMinute;
                    second = parsedSecond;
                    frame = parsedFrame;
                    totalSeconds = (float)seconds;
                    useTotalSeconds = true;
                }

                currentSeconds = useTotalSeconds
                    ? totalSeconds
                    : hour * 3600d + minute * 60d + second + frame / Math.Max(1, settings.Fps);
                currentTimeCode = FormatTimecode(currentSeconds, settings.Fps);
            }
        }
    }

    private sealed class EditorOscSender : IDisposable
    {
        private UdpClient udpClient;
        private IPEndPoint endpoint;
        private OscSenderSettings settings;
        private double startEditorTime;
        private double lastSendTime;
        private string currentTimeCode = "00:00:00:00";
        private double currentSeconds;

        public bool IsRunning => udpClient != null;
        public string CurrentTimeCode => currentTimeCode;
        public double CurrentSeconds => currentSeconds;

        public void Start(OscSenderSettings senderSettings)
        {
            Stop();
            settings = senderSettings;
            try
            {
                udpClient = new UdpClient();
                endpoint = new IPEndPoint(ResolveHost(settings.Host), settings.Port);
                startEditorTime = EditorApplication.timeSinceStartup;
                lastSendTime = 0d;
                SendNow();
            }
            catch (Exception exception)
            {
                Stop();
                Debug.Log($"VLiveKit OSC sender could not start for {settings.Host}:{settings.Port}: {exception.Message}");
            }
        }

        public void Stop()
        {
            udpClient?.Close();
            udpClient = null;
            endpoint = null;
        }

        public void Tick()
        {
            if (udpClient == null)
                return;

            var interval = 1d / Math.Max(1, settings.Fps);
            if (EditorApplication.timeSinceStartup - lastSendTime < interval)
                return;

            SendNow();
        }

        public void SendNow()
        {
            if (udpClient == null || endpoint == null)
                return;

            currentSeconds = Math.Max(0d, EditorApplication.timeSinceStartup - startEditorTime);
            currentTimeCode = FormatTimecode(currentSeconds, settings.Fps);

            Send(settings.TimecodeAddress, currentTimeCode);
            if (settings.SendTotalSeconds)
                Send(settings.TotalSecondsAddress, (float)currentSeconds);

            if (settings.SendSplitFields)
            {
                var parts = ToTimeParts(currentSeconds, settings.Fps);
                Send(settings.HourAddress, parts.Hour);
                Send(settings.MinuteAddress, parts.Minute);
                Send(settings.SecondAddress, parts.Second);
                Send(settings.FrameAddress, parts.Frame);
            }

            lastSendTime = EditorApplication.timeSinceStartup;
        }

        public void Dispose()
        {
            Stop();
        }

        private void Send(string address, object value)
        {
            if (string.IsNullOrWhiteSpace(address))
                return;

            var bytes = BuildOscMessage(address, value);
            try
            {
                udpClient.Send(bytes, bytes.Length, endpoint);
            }
            catch (SocketException exception)
            {
                Debug.Log($"VLiveKit OSC sender could not send to {endpoint}: {exception.Message}");
            }
        }

        private static IPAddress ResolveHost(string host)
        {
            if (IPAddress.TryParse(host, out var parsed))
                return parsed;

            var addresses = Dns.GetHostAddresses(host);
            return addresses.Length > 0 ? addresses[0] : IPAddress.Loopback;
        }
    }

    private readonly struct OscReceiverSettings
    {
        public readonly int Port;
        public readonly int Fps;
        public readonly string TimecodeAddress;
        public readonly string TotalSecondsAddress;
        public readonly string HourAddress;
        public readonly string MinuteAddress;
        public readonly string SecondAddress;
        public readonly string FrameAddress;

        public OscReceiverSettings(int port, int fps, string timecodeAddress, string totalSecondsAddress, string hourAddress, string minuteAddress, string secondAddress, string frameAddress)
        {
            Port = port;
            Fps = Mathf.Max(1, fps);
            TimecodeAddress = timecodeAddress;
            TotalSecondsAddress = totalSecondsAddress;
            HourAddress = hourAddress;
            MinuteAddress = minuteAddress;
            SecondAddress = secondAddress;
            FrameAddress = frameAddress;
        }
    }

    private readonly struct OscSenderSettings
    {
        public readonly string Host;
        public readonly int Port;
        public readonly int Fps;
        public readonly string TimecodeAddress;
        public readonly string TotalSecondsAddress;
        public readonly string HourAddress;
        public readonly string MinuteAddress;
        public readonly string SecondAddress;
        public readonly string FrameAddress;
        public readonly bool SendTotalSeconds;
        public readonly bool SendSplitFields;

        public OscSenderSettings(string host, int port, int fps, string timecodeAddress, string totalSecondsAddress, string hourAddress, string minuteAddress, string secondAddress, string frameAddress, bool sendTotalSeconds, bool sendSplitFields)
        {
            Host = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
            Port = port;
            Fps = Mathf.Max(1, fps);
            TimecodeAddress = timecodeAddress;
            TotalSecondsAddress = totalSecondsAddress;
            HourAddress = hourAddress;
            MinuteAddress = minuteAddress;
            SecondAddress = secondAddress;
            FrameAddress = frameAddress;
            SendTotalSeconds = sendTotalSeconds;
            SendSplitFields = sendSplitFields;
        }
    }

    private readonly struct TimeParts
    {
        public readonly int Hour;
        public readonly int Minute;
        public readonly int Second;
        public readonly int Frame;

        public TimeParts(int hour, int minute, int second, int frame)
        {
            Hour = hour;
            Minute = minute;
            Second = second;
            Frame = frame;
        }
    }

    private static bool TryParseTimeCode(string value, int fps, out int hour, out int minute, out int second, out int frame, out double seconds)
    {
        hour = 0;
        minute = 0;
        second = 0;
        frame = 0;
        seconds = 0d;

        var parts = value?.Split(':', ';');
        if (parts == null || parts.Length != 4)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out hour) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minute) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out second) ||
            !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out frame))
            return false;

        seconds = hour * 3600d + minute * 60d + second + frame / (double)Mathf.Max(1, fps);
        return true;
    }

    private static string FormatTimecode(double seconds, int fps)
    {
        var parts = ToTimeParts(seconds, fps);
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}:{3:00}", parts.Hour, parts.Minute, parts.Second, parts.Frame);
    }

    private static TimeParts ToTimeParts(double seconds, int fps)
    {
        fps = Mathf.Max(1, fps);
        var totalFrames = Mathf.Max(0, (int)Math.Floor(seconds * fps));
        var frame = totalFrames % fps;
        var totalSeconds = totalFrames / fps;
        var second = totalSeconds % 60;
        var minute = (totalSeconds / 60) % 60;
        var hour = (totalSeconds / 3600) % 24;
        return new TimeParts(hour, minute, second, frame);
    }

    private static float ToFloat(object value, float fallback)
    {
        if (value is int intValue)
            return intValue;
        if (value is float floatValue)
            return floatValue;
        if (value is string text && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return fallback;
    }

    private static bool TryParseOscMessage(byte[] bytes, out string address, out object value)
    {
        address = "";
        value = null;
        var offset = 0;
        if (!TryReadOscString(bytes, ref offset, out address))
            return false;

        if (!TryReadOscString(bytes, ref offset, out var tags) || string.IsNullOrEmpty(tags) || tags[0] != ',')
            return false;

        for (var i = 1; i < tags.Length; i++)
        {
            switch (tags[i])
            {
                case 'i':
                    if (offset + 4 > bytes.Length) return false;
                    value = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, offset));
                    return true;
                case 'f':
                    if (offset + 4 > bytes.Length) return false;
                    value = ReadBigEndianFloat(bytes, offset);
                    return true;
                case 's':
                    return TryReadOscString(bytes, ref offset, out var textValue) && SetValue(textValue, out value);
            }
        }

        return false;
    }

    private static bool TryReadOscString(byte[] bytes, ref int offset, out string value)
    {
        value = "";
        if (offset >= bytes.Length)
            return false;

        var start = offset;
        while (offset < bytes.Length && bytes[offset] != 0)
            offset++;

        if (offset >= bytes.Length)
            return false;

        value = Encoding.UTF8.GetString(bytes, start, offset - start);
        offset++;
        while (offset % 4 != 0)
            offset++;

        return offset <= bytes.Length;
    }

    private static float ReadBigEndianFloat(byte[] bytes, int offset)
    {
        var buffer = new[] { bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset] };
        return BitConverter.ToSingle(buffer, 0);
    }

    private static bool SetValue(string source, out object value)
    {
        value = source;
        return true;
    }

    private static byte[] BuildOscMessage(string address, object value)
    {
        using (var stream = new MemoryStream())
        {
            WriteOscString(stream, address);

            if (value is int intValue)
            {
                WriteOscString(stream, ",i");
                WriteInt(stream, intValue);
            }
            else if (value is float floatValue)
            {
                WriteOscString(stream, ",f");
                WriteFloat(stream, floatValue);
            }
            else
            {
                WriteOscString(stream, ",s");
                WriteOscString(stream, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
            }

            return stream.ToArray();
        }
    }

    private static void WriteOscString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? "");
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte(0);
        while (stream.Position % 4 != 0)
            stream.WriteByte(0);
    }

    private static void WriteInt(Stream stream, int value)
    {
        var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteFloat(Stream stream, float value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        stream.Write(bytes, 0, bytes.Length);
    }
}
#endif
