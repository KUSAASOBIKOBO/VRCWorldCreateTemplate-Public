
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using KUSAASOBIKOBO;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBOINFOMATION
{
    public enum InfomationWindowType
    {
        ALWAYS_VISIBLE,
        APPROACHING_VISIBLE,
        APPROACHING_INVISIBLE
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InfomationWindow : UdonSharpBehaviour
    {
        [Header("表示タイプ(常に表示、近づくと表示、近づくと非表示)")] public InfomationWindowType type = InfomationWindowType.ALWAYS_VISIBLE;
        [Header("反応距離(ALWAYS_VISIBLE時は無視)")] public float distance;
        [Header("Canvas")] public GameObject targetCanvas;
        [Header("アニメーター")] public Animator anim;


        void Start()
        {
            if (type == InfomationWindowType.APPROACHING_VISIBLE || type == InfomationWindowType.APPROACHING_INVISIBLE)
            {
                this.gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                if (anim != null) anim.SetBool("ScaleStatus", false);
            }
            else
            {
                this.gameObject.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
                if (anim != null) anim.SetBool("ScaleStatus", true);
            }
        }

        void Update()
        {
            if (type == InfomationWindowType.ALWAYS_VISIBLE) return;

            if (type == InfomationWindowType.APPROACHING_VISIBLE)
            {
                if (Vector3.Distance(this.transform.position, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position) <= distance)
                {
                    if (anim != null) anim.SetBool("ScaleStatus", true);
                    if (targetCanvas != null && !targetCanvas.activeSelf) targetCanvas.SetActive(true);
                }
                else
                {
                    if (anim != null) anim.SetBool("ScaleStatus", false);
                    if(this.gameObject.transform.localScale.x <= 0.0f)
                    {
                        if (targetCanvas != null && targetCanvas.activeSelf) targetCanvas.SetActive(false);
                    }
                }
            }
            else if (type == InfomationWindowType.APPROACHING_INVISIBLE)
            {
                if (Vector3.Distance(this.transform.position, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position) <= distance)
                {
                    if (anim != null) anim.SetBool("ScaleStatus", false);
                    if (this.gameObject.transform.localScale.x <= 0.0f)
                    {
                        if (targetCanvas != null && targetCanvas.activeSelf) targetCanvas.SetActive(false);
                    }
                }
                else
                {
                    if (anim != null) anim.SetBool("ScaleStatus", true);
                    if (targetCanvas != null && !targetCanvas.activeSelf) targetCanvas.SetActive(true);
                }
            }
        }
    }
}

