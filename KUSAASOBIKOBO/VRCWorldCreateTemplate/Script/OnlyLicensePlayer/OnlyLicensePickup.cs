
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    /*
    特定のプレイヤー以外がそのオブジェクトをアクティブにしたときコライダーを消すスクリプトです。
    対象にとるオブジェクト以外のオブジェクトにつけておくことによってTimeManagerと連携させて特定時間帯のみ有効にすることができます。
    このスクリプトがアタッチされたオブジェクトが非アクティブになったときに対象のコライダーをdefaultStatusの状態にします。


     */

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OnlyLicensePickup : UdonSharpBehaviour
    {
        public string[] licensePlayerDisplayName;
        public BoxCollider _collider;
        public bool defaultStatus = false;
        void OnEnable()
        {
            if (_collider == null) return;
            bool result = false;
            foreach(string tmp in licensePlayerDisplayName)
            {
                if (tmp == Networking.LocalPlayer.displayName)
                {
                    result = true;
                    break;
                }
            }
            if(result)
            {
                _collider.enabled = true;
            }
            else
            {
                _collider.enabled = false;
            }
        }
        
        void OnDisable()
        {
            if (_collider == null) return;
            _collider.enabled = defaultStatus;
        }
    }
}
