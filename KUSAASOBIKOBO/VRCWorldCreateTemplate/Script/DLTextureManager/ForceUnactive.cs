
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ForceUnactive : UdonSharpBehaviour
    {
        [Header("このスクリプトを有効にする")] public bool isActive = true;
        [Header("アクティブになってから何秒後に非アクティブにするか")] public float waitTime = 0;

        private float waitTimeCount = 0;

        void OnEnable()
        {
            waitTimeCount = waitTime;
        }

        void Update()
        {
            if (!isActive) return;
            waitTimeCount--;
            if (waitTime <= 0)
            {
                this.gameObject.SetActive(false);
                waitTime = 0;
            }
        }
    }
}
