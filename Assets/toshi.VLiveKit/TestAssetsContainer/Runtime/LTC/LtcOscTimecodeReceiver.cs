using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class LtcOscTimecodeReceiver : MonoBehaviour
{
    [Header("OSC")]
    [SerializeField] private int port = 10000;
    [SerializeField] private string hourAddress = "/hour";
    [SerializeField] private string minuteAddress = "/minute";
    [SerializeField] private string secondAddress = "/second";
    [SerializeField] private string frameAddress = "/frame";
    [SerializeField] private string totalSecondsAddress = "/total_seconds";
    [SerializeField] private string timecodeAddress = "/timecode";
    [SerializeField] private LTCread.FrameRate frameRate = LTCread.FrameRate.FPS30;

    [Header("Output")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private bool syncPlayableDirector = true;
    [SerializeField] private bool showOnGUI = true;
    [SerializeField] private Text timecodeText;
    [SerializeField] private int guiFontSize = 64;

    private readonly object stateLock = new object();
    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running;
    private GUIStyle guiStyle;
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

    public string CurrentTimeCode => currentTimeCode;
    public double CurrentTimeSeconds => currentSeconds;
    public string LastAddress => lastAddress;
    public bool HasReceived => hasReceived;
    public int FramesPerSecond => Mathf.Max(1, (int)frameRate);

    private void OnEnable()
    {
        StartServer();
    }

    private void OnDisable()
    {
        StopServer();
    }

    private void Update()
    {
        lock (stateLock)
        {
            currentTimeCode = BuildTimeCode();
            currentSeconds = useTotalSeconds
                ? totalSeconds
                : hour * 3600d + minute * 60d + second + frame / FramesPerSecond;
        }

        if (syncPlayableDirector && playableDirector != null)
            playableDirector.time = currentSeconds;

        if (timecodeText != null)
            timecodeText.text = currentTimeCode;
    }

    private void OnGUI()
    {
        if (!showOnGUI)
            return;

        EnsureGuiStyle();
        GUI.Label(new Rect(10, 10, 720, guiFontSize + 8), currentTimeCode, guiStyle);
        GUI.Label(new Rect(10, 20 + guiFontSize, 720, guiFontSize + 8), $"OSC {port} {lastAddress}", guiStyle);
    }

    private void StartServer()
    {
        StopServer();

        try
        {
            udpClient = new UdpClient(port);
            running = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
        }
        catch (Exception exception)
        {
            running = false;
            Debug.Log($"LTC OSC receiver could not start on port {port}: {exception.Message}");
        }
    }

    private void StopServer()
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

            if (address == hourAddress) { hour = ToFloat(value, hour); useTotalSeconds = false; }
            else if (address == minuteAddress) { minute = ToFloat(value, minute); useTotalSeconds = false; }
            else if (address == secondAddress) { second = ToFloat(value, second); useTotalSeconds = false; }
            else if (address == frameAddress) { frame = ToFloat(value, frame); useTotalSeconds = false; }
            else if (address == totalSecondsAddress) { totalSeconds = ToFloat(value, totalSeconds); useTotalSeconds = true; }
            else if (address == timecodeAddress && value is string text && TryParseTimeCode(text, out var parsedHour, out var parsedMinute, out var parsedSecond, out var parsedFrame, out var seconds))
            {
                hour = parsedHour;
                minute = parsedMinute;
                second = parsedSecond;
                frame = parsedFrame;
                currentSeconds = seconds;
                totalSeconds = (float)seconds;
                useTotalSeconds = true;
            }
        }
    }

    private string BuildTimeCode()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:00}:{1:00}:{2:00}:{3:00}",
            Mathf.FloorToInt(hour),
            Mathf.FloorToInt(minute),
            Mathf.FloorToInt(second),
            Mathf.FloorToInt(frame));
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

    private bool TryParseTimeCode(string value, out int parsedHour, out int parsedMinute, out int parsedSecond, out int parsedFrame, out double seconds)
    {
        parsedHour = 0;
        parsedMinute = 0;
        parsedSecond = 0;
        parsedFrame = 0;
        seconds = 0d;

        var parts = value?.Split(':', ';');
        if (parts == null || parts.Length != 4)
            return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedHour) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedMinute) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedSecond) ||
            !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedFrame))
            return false;

        seconds = parsedHour * 3600d + parsedMinute * 60d + parsedSecond + parsedFrame / (double)FramesPerSecond;
        return true;
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

    private void EnsureGuiStyle()
    {
        if (guiStyle != null)
        {
            guiStyle.fontSize = guiFontSize;
            return;
        }

        guiStyle = new GUIStyle
        {
            fontSize = guiFontSize,
            normal = { textColor = Color.white }
        };
    }
}
