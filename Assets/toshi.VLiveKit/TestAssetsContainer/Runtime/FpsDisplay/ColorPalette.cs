
// Assets/toshi.VLiveKit/Utility/FpsDisplay/ColorPalette.cs
using UnityEngine;
using System.Collections.Generic;
using toshi.VLiveKit.Utility;

namespace toshi.VLiveKit.Photography
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "ScriptableObjects/ColorPalette", order = 1)]
    public class ColorPalette : ScriptableObject
    {
        public List<Color> colors = new List<Color>();
        // リファレンス先のメモ記入場所
        public string note;
    }
}   