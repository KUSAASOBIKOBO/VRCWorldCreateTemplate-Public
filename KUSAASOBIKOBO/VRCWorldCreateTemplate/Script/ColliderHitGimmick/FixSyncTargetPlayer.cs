
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
    public class FixSyncTargetPlayer : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.None)] private int playerId = -1;
        public Text feedback;

        private void OnEnable()
        {
            if (feedback != null && playerId > 0)
            {
                VRCPlayerApi tmp = VRCPlayerApi.GetPlayerById(playerId);
                if(tmp != null) feedback.text = tmp.displayName;
            }
        }

        public void Set()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            playerId = Networking.LocalPlayer.playerId;
            RequestSerialization();
            if (feedback != null && playerId > 0)
            {
                VRCPlayerApi tmp = VRCPlayerApi.GetPlayerById(playerId);
                if (tmp != null) feedback.text = tmp.displayName;
            }
        }

        public void Clear()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            playerId = -1;
            RequestSerialization();
            if (feedback != null) feedback.text = "";
        }

        public string Get()
        {
            if (!this.gameObject.activeInHierarchy) return "";
            if (feedback != null && playerId > 0)
            {
                VRCPlayerApi tmp = VRCPlayerApi.GetPlayerById(playerId);
                if (tmp != null) return tmp.displayName;
            }
            return "";
        }

        public int GetPlayerId()
        {
            if (!this.gameObject.activeInHierarchy) return -1;
            return playerId;
        }

        public override void OnDeserialization()
        {
            if (feedback != null)
            {
                if(playerId > 0)
                {
                    VRCPlayerApi tmp = VRCPlayerApi.GetPlayerById(playerId);
                    if (tmp != null) feedback.text = tmp.displayName;
                }
                else
                {
                    feedback.text = "";
                }
            }
        }
    }
}
