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
            audioListeners = FindObjectsOfType<AudioListener>();
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