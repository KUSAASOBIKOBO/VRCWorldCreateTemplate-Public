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
    public class MultiAreaSyncer : UdonSharpBehaviour
    {
        private bool isGet = false;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(DeserializationIsOpen))] private bool isOpen;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(DeserializationAreaPassword))] private int areaPassword;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(DeserializationAreaState))] private string areaState; //TODO:bit値を保存にしたい
        public UdonSharpBehaviour script;
        public string isOpenValueChangedCallMethodName;
        public string areaPasswordChangedCallMethodName;
        public string areaStateChangedCallMethodName;
        public string ownerInitMethodName;

        public bool DeserializationIsOpen
        {
            get => isOpen;

            set
            {
                bool isChanged = false;
                if (isOpen != value) isChanged = true;
                isOpen = value;
                if(isChanged)
                {
                    if (script != null && isOpenValueChangedCallMethodName != "") script.SendCustomEvent(isOpenValueChangedCallMethodName);
                }
            }
        }

        public int DeserializationAreaPassword
        {
            get => areaPassword;

            set
            {
                bool isChanged = false;
                if (areaPassword != value) isChanged = true;
                areaPassword = value;
                if (isChanged)
                {
                    if (script != null && areaPasswordChangedCallMethodName != "") script.SendCustomEvent(areaPasswordChangedCallMethodName);
                }
            }
        }

        public string DeserializationAreaState 
        {
            get => areaState;

            set
            {
                bool isChanged = false;
                if (areaState != value) isChanged = true;
                areaState = value;
                if (isChanged)
                {
                    if (script != null && areaStateChangedCallMethodName != "") script.SendCustomEvent(areaStateChangedCallMethodName);
                }
            }
        }

        public override void OnDeserialization()
        {
            isGet = true;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) //OnPlayerJoinedタイミングでこのオブジェクトのオーナーならこのインスタンスで最初にこのオブジェクトを初期化する人
            {
                isGet = true;
                if (script != null && ownerInitMethodName != "") script.SendCustomEvent(ownerInitMethodName);
            }
            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SyncRequestOwner");
        }

        public void SyncRequestOwner()
        {
            RequestSerialization();
        }

        public bool GetIsOpen()
        {
            return isOpen;
        }

        public int GetAreaPassword()
        {
            return areaPassword;
        }

        public string GetAreaState()
        {
            return areaState;
        }

        public void SetIsOpen(bool value, bool isForce = false)
        {
            if (!isGet && !isForce) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            isOpen = value;
            RequestSerialization();
        }

        public void SetAreaPassword(int value, bool isForce = false)
        {
            if (!isGet && !isForce) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            areaPassword = value;
            RequestSerialization();
        }

        public void SetAreaState(string value, bool isForce = false)
        {
            if (!isGet && !isForce) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            areaState = value;
            RequestSerialization();
        }

        public bool GetIsGetIsOpen()
        {
            return isGet;
        }

        public bool GetIsGetAreaPassword()
        {
            return isGet;
        }

        public bool GetIsGetAreaState()
        {
            return isGet;
        }
    }
}