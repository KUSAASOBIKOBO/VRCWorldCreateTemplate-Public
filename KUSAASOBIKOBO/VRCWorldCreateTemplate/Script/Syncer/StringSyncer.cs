using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StringSyncer : UdonSharpBehaviour
    {
        private bool isGet = false;
        [UdonSynced(UdonSyncMode.None)] public string[] elementList;
        public UdonSharpBehaviour script;
        public string methodName;
        public string ownerInitMethodName;

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) //OnPlayerJoinedタイミングでこのオブジェクトのオーナーならこのインスタンスで最初にこのオブジェクトを初期化する人
            {
                isGet = true;
                if (script != null) script.SendCustomEvent(ownerInitMethodName);
            }
            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SyncRequestOwner");
        }

        public void SyncRequestOwner()
        {
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            isGet = true;
            if (script != null) script.SendCustomEvent(methodName);
            if (DebugText != null) DebugText.text = "StringSyncer:OnDeserialization\n";
        }

        public string[] Get()
        {
            return elementList;
        }

        public string Get(int index)
        {
            if (index >= 0 && index < elementList.Length) return elementList[index];
            else return "ERROR:index = "+ index;
        }

        public void Set(string value, int index)
        {
            if (!isGet) return;
            if (index >= 0 && index < elementList.Length)
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                elementList[index] = value;
                RequestSerialization();
            }
        }

        public void Set(string[] value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            elementList = value;
            RequestSerialization();
        }

        public bool GetIsGet()
        {
            return isGet;
        }
    }
}
