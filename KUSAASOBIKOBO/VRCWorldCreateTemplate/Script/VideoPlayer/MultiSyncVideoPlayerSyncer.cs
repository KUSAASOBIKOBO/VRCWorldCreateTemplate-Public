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
    public class MultiSyncVideoPlayerSyncer : UdonSharpBehaviour
    {
        private bool isGet = false;
        [UdonSynced(UdonSyncMode.None)] private VRCUrl multiUserUrlsPc;
        [UdonSynced(UdonSyncMode.None)] private VRCUrl multiUserUrlsQuest;
        [UdonSynced(UdonSyncMode.None)] private bool multiUserIsStreamingVideo = false;
        [UdonSynced(UdonSyncMode.None)] private bool multiUserIsPauseVideo = false;
        [UdonSynced(UdonSyncMode.None)] private float multiUserCurrentTime = 0.0f;
        [UdonSynced(UdonSyncMode.None)] private string multiUserCurrentDateTime = "2023-01-01 00:00:00";
        public UdonSharpBehaviour script;
        public string methodName;

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) //OnPlayerJoinedタイミングでこのオブジェクトのオーナーならこのインスタンスで最初にこのオブジェクトを初期化する人
            {
                isGet = true;
            }
            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SyncRequestOwner");
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {

        }

        public void Sync()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SyncRequestOwner");
        }

        public void SyncRequestOwner()
        {
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            isGet = true;
            if (script != null) script.SendCustomEvent(methodName);
            if (DebugText != null) DebugText.text = "MultiSyncVideoPlayerSyncer:OnDeserialization\n";
        }

        public VRCUrl GetMultiUserUrlsPc()
        {
            return multiUserUrlsPc;
        }

        public VRCUrl GetMultiUserUrlsQuest()
        {
            return multiUserUrlsQuest;
        }

        public bool GetMultiUserIsStreamingVideo()
        {
            return multiUserIsStreamingVideo;
        }

        public bool GetMultiUserIsPauseVideo()
        {
            return multiUserIsPauseVideo;
        }

        public float GetMultiUserCurrentTime()
        {
            return multiUserCurrentTime;
        }

        public string GetMultiUserCurrentDateTime()
        {
            return multiUserCurrentDateTime;
        }

        public void SetMultiUserUrlsPc(VRCUrl value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserUrlsPc = value;
            RequestSerialization();
        }

        public void SetMultiUserUrlsQuest(VRCUrl value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserUrlsQuest = value;
            RequestSerialization();
        }

        public void SetMultiUserIsStreamingVideo(bool value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserIsStreamingVideo = value;
            RequestSerialization();
        }

        public void SetMultiUserIsPauseVideo(bool value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserIsPauseVideo = value;
            RequestSerialization();
        }

        public void SetMultiUserCurrentTime(float value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserCurrentTime = value;
            RequestSerialization();
        }

        public void SetMultiUserCurrentDateTime(string value)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserCurrentDateTime = value;
            RequestSerialization();
        }

        public void Set(VRCUrl _multiUserUrlsPc, VRCUrl _multiUserUrlsQuest, bool _multiUserIsStreamingVideo, bool _multiUserIsPauseVideo, float _multiUserCurrentTime, string _multiUserCurrentDateTime)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserUrlsPc = _multiUserUrlsPc;
            multiUserUrlsQuest = _multiUserUrlsQuest;
            multiUserIsStreamingVideo = _multiUserIsStreamingVideo;
            multiUserIsPauseVideo = _multiUserIsPauseVideo;
            multiUserCurrentTime = _multiUserCurrentTime;
            multiUserCurrentDateTime = _multiUserCurrentDateTime;
            RequestSerialization();
        }

        public void Set(VRCUrl _multiUserUrlsPc, VRCUrl _multiUserUrlsQuest, bool _multiUserIsStreamingVideo, float _multiUserCurrentTime, string _multiUserCurrentDateTime)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserUrlsPc = _multiUserUrlsPc;
            multiUserUrlsQuest = _multiUserUrlsQuest;
            multiUserIsStreamingVideo = _multiUserIsStreamingVideo;
            multiUserCurrentTime = _multiUserCurrentTime;
            multiUserCurrentDateTime = _multiUserCurrentDateTime;
            RequestSerialization();
        }

        public void Set(VRCUrl _multiUserUrlsPc, VRCUrl _multiUserUrlsQuest, bool _multiUserIsStreamingVideo)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserUrlsPc = _multiUserUrlsPc;
            multiUserUrlsQuest = _multiUserUrlsQuest;
            multiUserIsStreamingVideo = _multiUserIsStreamingVideo;
            RequestSerialization();
        }

        public void Set(VRCUrl _multiUserUrlsPc, VRCUrl _multiUserUrlsQuest, bool _multiUserIsStreamingVideo, bool _multiUserIsPauseVideo)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserUrlsPc = _multiUserUrlsPc;
            multiUserUrlsQuest = _multiUserUrlsQuest;
            multiUserIsStreamingVideo = _multiUserIsStreamingVideo;
            multiUserIsPauseVideo = _multiUserIsPauseVideo;
            RequestSerialization();
        }

        public void Set(float _multiUserCurrentTime, string _multiUserCurrentDateTime)
        {
            if (!isGet) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            multiUserCurrentTime = _multiUserCurrentTime;
            multiUserCurrentDateTime = _multiUserCurrentDateTime;
            RequestSerialization();
        }

        public bool GetIsGet()
        {
            return isGet;
        }
    }
}