// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.
// last update: 2024/11/25

using UnityEngine;

namespace toshi.VLiveKit.Utility
{
    public class CursorVisibleMgr : MonoBehaviour
    {
        [SerializeField]
        private KeyCode cursorVisibleSwitchKey = KeyCode.Escape;
        // エディタモードで有効にする？
        [SerializeField]
        private bool isEnableInEditor = false;
        // 非エディタモードで有効にする？
        [SerializeField]
        private bool isEnableInBuild = true;

        void Start()
        {
            #if UNITY_EDITOR
            if (isEnableInEditor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            #else
            // ビルド時のみ
            if (isEnableInBuild)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            #endif
        }

        void Update()
        {
            if (Input.GetKeyDown(cursorVisibleSwitchKey))
            {
                #if UNITY_EDITOR
                if (isEnableInEditor)
                {
                    Cursor.visible = !Cursor.visible;
                }
                #else
                if (isEnableInBuild)
                {
                    Cursor.visible = !Cursor.visible;
                }
                #endif
            }
        }

        void OnEnable()
        {
            #if UNITY_EDITOR
            if (isEnableInEditor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            #else
            if (isEnableInBuild)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            #endif
        }

        void OnDisable()
        {
            #if UNITY_EDITOR
            if (isEnableInEditor)
            {
                Cursor.visible = true;
            }
            #else
            if (isEnableInBuild)
            {
                Cursor.visible = true;
            }
            #endif
        }
    }
}