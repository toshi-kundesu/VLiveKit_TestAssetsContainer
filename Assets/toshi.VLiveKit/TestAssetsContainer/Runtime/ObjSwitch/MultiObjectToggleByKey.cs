// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.

using UnityEngine;
using System.Collections.Generic;

namespace toshi.VLiveKit.Utility
{
    public class MultiObjectToggleByKey : MonoBehaviour
    {
        [System.Serializable]
        public class ToggleObject
        {
            public GameObject targetObject;    // アクティブ/非アクティブを切り替える対象のオブジェクト
            public KeyCode toggleKey;          // 切り替えに使用するキー
        }

        [SerializeField]
        private List<ToggleObject> toggleObjects = new List<ToggleObject>(); // 複数のオブジェクトとキーのリスト

        private void Update()
        {
            foreach (ToggleObject toggleObject in toggleObjects)
            {
                // 指定したキーが押されたかをチェック
                if (Input.GetKeyDown(toggleObject.toggleKey))
                {
                    // オブジェクトのアクティブ状態を切り替え
                        toggleObject.targetObject.SetActive(!toggleObject.targetObject.activeSelf);
                    }
            }
        }
    }
}
