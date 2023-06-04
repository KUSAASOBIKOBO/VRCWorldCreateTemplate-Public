
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using System;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class JstAudioClipSync : UdonSharpBehaviour
    {
        [Header("対象にするAudioSource")] public AudioSource targetAudio;
        [Header("TimeManager")] public TimeManager _timeManager;
        [Header("開始時間")] public string startTime ="00:00:00";

        void OnEnable()
        {
            SyncJst();
        }

        public void SyncJst()
        {
            if(targetAudio == null || _timeManager == null) return;
            /*DateTime jst = _timeManager.GetJst();
            float offset_tmp = jst.Hour*60*60+jst.Minute*60+jst.Second;
            float length = 0;
            if(targetAudio.clip != null) length = targetAudio.clip.length;
            else return;
            int animoffset_tmp = (int)(offset_tmp/length);
            float animoffset_tmp2 = offset_tmp - (float)animoffset_tmp*length;
            float animoffset_tmp3 = (float)TimeSpan.Parse(startTime).TotalSeconds%length;
            targetAudio.time = animoffset_tmp2+animoffset_tmp3; */

            TimeSpan jst = _timeManager.GetJst().TimeOfDay;
            float length = 0;
            if (targetAudio.clip != null) length = targetAudio.clip.length;
            float animoffset_tmp = (float)jst.TotalSeconds % length;
            float animoffset_tmp2 = (float)TimeSpan.Parse(startTime).TotalSeconds % length;
            float animoffset_tmp3 = (animoffset_tmp + animoffset_tmp2) % length;
            targetAudio.time = animoffset_tmp3 / length;
        }
    }
}
