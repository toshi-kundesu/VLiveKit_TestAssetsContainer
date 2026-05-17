// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.
// last update: 2024/11/25

using UnityEngine;
using toshi.VLiveKit.Utility;

namespace toshi.VLiveKit.Utility
{
    public class DisableAllAudioListeners : MonoBehaviour
    {
        [SerializeField]
        private AudioListener[] audioListeners;
        [SerializeField]
        private bool isDisableOnStart = true;
        void Start()
        {
            if (isDisableOnStart)
            {
                GetAudioListeners();
                DisableAudioListeners();
            }
            else
            {
                Debug.Log("DisableAllAudioListeners is not disable on start");
            }
        }

        [ContextMenu("GetAudioListeners")]
        private void GetAudioListeners()
        {
            audioListeners = FindSceneObjects<AudioListener>();
        }

        private static T[] FindSceneObjects<T>() where T : Object
        {
#if UNITY_2022_2_OR_NEWER || UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }

        [ContextMenu("DisableAudioListeners")]
        private void DisableAudioListeners()
        {
            foreach (AudioListener listener in audioListeners)
            {
                listener.enabled = false;
            }
        }

        [ContextMenu("EnableAudioListeners")]
        private void EnableAudioListeners()
        {
            foreach (AudioListener listener in audioListeners)
            {
                listener.enabled = true;
            }
        }
    }
}
