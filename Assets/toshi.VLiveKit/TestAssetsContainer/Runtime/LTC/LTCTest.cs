// LTC Timecode Reader for Unity C#
// http://blog.mobilehackerz.jp/
// https://twitter.com/MobileHackerz

using System.Linq;
using UnityEngine;

public class LTCTest : MonoBehaviour {

    [SerializeField] private string deviceName;
    private string m_TimeCode = "00:00:00;00";

    // 内部で録音するバッファの長さ
    private const int DEVICE_REC_LENGTH = 10;
    
    private AudioClip m_LtcAudioInput;
    private int m_LastAudioPos;
    private int m_SameAudioLevelCount;
    private int m_LastAudioLevel;
    private int m_LastBitCount;
    private string m_BITPattern = "";

    [SerializeField, Range(0.0f, 1.0f)] private float m_AudioThreshold;
    private string m_Gain;

    private GUIStyle m_TimeCodeStyle;

    void Start() {
        string targetDevice = "";
        
        foreach (var device in Microphone.devices) {
            Debug.Log($"Device Name: {device}");
            if (device.Contains(deviceName)) {
                targetDevice = device;
            }
        }

        Debug.Log($"=== Device Set: {targetDevice} ===");
        m_LtcAudioInput = Microphone.Start(targetDevice, true, DEVICE_REC_LENGTH, 44100);
        m_TimeCodeStyle = new GUIStyle { 
            fontSize = 64,
            normal = { textColor = Color.white }
        };
    }
    
    void Update() {
        DecodeAudioToTcFrames();
    }
    
    private void OnGUI() {
        GUI.Label(new Rect(0, 0, 200, 100), m_TimeCode, m_TimeCodeStyle);
        GUI.Label(new Rect(0, 100, 200, 100), m_Gain, m_TimeCodeStyle);
    }
    
    // 現在までのオーディオ入力を取得しフレーム情報にデコードしていく
    private void DecodeAudioToTcFrames() {
        float[] waveData = GetUpdatedAudio(m_LtcAudioInput);
        
        if (waveData.Length == 0) {
            return;
        }

        var gain = waveData.Select(Mathf.Abs).Sum() / waveData.Length;
        m_Gain = $"{gain:F6}";
        if (gain < m_AudioThreshold) return;

        int pos = 0;
        int bitThreshold = m_LtcAudioInput.frequency / 3100; // 適当
        
        while (pos < waveData.Length) {
            int count = CheckAudioLevelChanged(waveData, ref pos, m_LtcAudioInput.channels);
            if (count <= 0) continue;
            
            if (count < bitThreshold) {
                // 「レベル変化までが短い」パターンが2回続くと1
                if (m_LastBitCount < bitThreshold) {
                    m_BITPattern += "1";
                    m_LastBitCount = bitThreshold; // 次はここを通らないように
                } else {
                    m_LastBitCount = count;
                }
            } else {
                // 「レベル変化までが長い」パターンは0
                m_BITPattern += "0";
                m_LastBitCount = count;
            }
        }

        // 1フレームぶん取れたかな？
        if (m_BITPattern.Length >= 80) {
            int bpos = m_BITPattern.IndexOf("0011111111111101"); // SYNC WORD
            if (bpos > 0) {
                string timeCodeBits = m_BITPattern.Substring(0, bpos + 16);
                m_BITPattern = m_BITPattern.Substring(bpos + 16);
                if (timeCodeBits.Length >= 80) {
                    timeCodeBits = timeCodeBits.Substring(timeCodeBits.Length - 80);
                    m_TimeCode = DecodeBitsToFrame(timeCodeBits);
                }
            }
        }

        // パターンマッチしなさすぎてビットパターンバッファ長くなっちゃったら削る
        if (m_BITPattern.Length > 160) {
            m_BITPattern = m_BITPattern.Substring(80);
        }
    }
    
    // マイク入力から録音データの生データを得る。
    // オーディオ入力が進んだぶんだけ処理して float[] に返す
    private float[] GetUpdatedAudio(AudioClip audioClip) {
        
        int nowAudioPos = Microphone.GetPosition(null);
        
        float[] waveData = new float[0];

        if (m_LastAudioPos < nowAudioPos) {
            int audioCount = nowAudioPos - m_LastAudioPos;
            waveData = new float[audioCount];
            audioClip.GetData(waveData, m_LastAudioPos);
        } else if (m_LastAudioPos > nowAudioPos) {
            int audioBuffer = audioClip.samples * audioClip.channels;
            int audioCount = audioBuffer - m_LastAudioPos;
            
            float[] wave1 = new float[audioCount];
            audioClip.GetData(wave1, m_LastAudioPos);
            
            float[] wave2 = new float[nowAudioPos];
            if (nowAudioPos != 0) {
                audioClip.GetData(wave2, 0);
            }

            waveData = new float[audioCount + nowAudioPos];
            wave1.CopyTo(waveData, 0);
            wave2.CopyTo(waveData, audioCount);
        }

        m_LastAudioPos = nowAudioPos;

        return waveData;
    }
    
    // 録音データの生データから、0<1, 1>0の変化が発生するまでのカウント数を得る。
    // もしデータの最後に到達したら-1を返す。
    private int CheckAudioLevelChanged(float[] data, ref int pos, int channels) {
        
        while (pos < data.Length) {
            int nowLevel = Mathf.RoundToInt(Mathf.Sign(data[pos]));
            
            // レベル変化があった
            if (m_LastAudioLevel != nowLevel) {
                int count = m_SameAudioLevelCount;
                m_SameAudioLevelCount = 0;
                m_LastAudioLevel = nowLevel;
                return count;
            }

            // 同じレベルだった
            m_SameAudioLevelCount++;
            pos += channels;
        }

        return -1;
    }
    
    // ---------------------------------------------------------------------------------
    // フレームデコード
    private int Decode1Bit(string b, int pos) {
        return int.Parse(b.Substring(pos, 1));
    }

    private int Decode2Bits(string b, int pos) {
        int r = 0;
        r += Decode1Bit(b, pos);
        r += Decode1Bit(b, pos + 1) * 2;
        return r;
    }

    private int Decode3Bits(string b, int pos) {
        int r = 0;
        r += Decode1Bit(b, pos);
        r += Decode1Bit(b, pos + 1) * 2;
        r += Decode1Bit(b, pos + 2) * 4;
        return r;
    }

    private int Decode4Bits(string b, int pos) {
        int r = 0;
        r += Decode1Bit(b, pos);
        r += Decode1Bit(b, pos + 1) * 2;
        r += Decode1Bit(b, pos + 2) * 4;
        r += Decode1Bit(b, pos + 3) * 8;
        return r;
    }

    private string DecodeBitsToFrame(string bits) {
        // https://en.wikipedia.org/wiki/Linear_timecode

        int frames = Decode4Bits(bits, 0) + Decode2Bits(bits, 8) * 10;
        int secs = Decode4Bits(bits, 16) + Decode3Bits(bits, 24) * 10;
        int mins = Decode4Bits(bits, 32) + Decode3Bits(bits, 40) * 10;
        int hours = Decode4Bits(bits, 48) + Decode2Bits(bits, 56) * 10;

        return $"{hours:D2}:{mins:D2}:{secs:D2};{frames:D2}";
    }
}