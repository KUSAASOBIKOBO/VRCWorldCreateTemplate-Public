
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
    public class JstAnimationSync : UdonSharpBehaviour
    {
        [Header("対象にするAnimator")] public Animator targetAnimator;
        [Header("TimeManager")] public TimeManager _timeManager;
        [Header("対象にするAnimationLayer")] public int layer = 0;
        [Header("アニメーションの開始時間")] public string startTime ="00:00:00";

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        void OnEnable()
        {
            SyncJst();
        }

        public void SyncJst()
        {
            if(targetAnimator == null || _timeManager == null) return;
            /*DateTime jst = _timeManager.GetJst();
            float offset_tmp = jst.Hour*60*60+jst.Minute*60+jst.Second;
            targetAnimator.Update(0f);
            AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(layer);
            float length = stateInfo.length;
            int animoffset_tmp = (int)(offset_tmp/length);
            float animoffset_tmp2 = offset_tmp - (float)animoffset_tmp*length;
            float animoffset_tmp3 = (float)TimeSpan.Parse(startTime).TotalSeconds%length;
            targetAnimator.Play(stateInfo.shortNameHash, layer, animoffset_tmp2+animoffset_tmp3); */
            
            TimeSpan jst = _timeManager.GetJst().TimeOfDay;
            targetAnimator.Update(0f);
            AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(layer);
            float length = stateInfo.length;
            float animoffset_tmp = (float)jst.TotalSeconds%length;
            float animoffset_tmp2 = (float)TimeSpan.Parse(startTime).TotalSeconds%length;
            float animoffset_tmp3 = (animoffset_tmp+animoffset_tmp2)%length;
            targetAnimator.Play(stateInfo.shortNameHash, layer, animoffset_tmp3/length);
/*
            if (DebugText != null)
            {
                DebugText.text = "\njst:" + jst;
                DebugText.text += "\nlength:" + length;
                DebugText.text += "\nanimoffset_tmp:" + animoffset_tmp;
                DebugText.text += "\nanimoffset_tmp2:" + animoffset_tmp2;
                DebugText.text += "\nanimoffset_tmp3:" + animoffset_tmp3;
            }
 */
            //targetAnimator.SetFloat("MotionTime", animoffset_tmp3/length);
            
            
        }
    }
}
