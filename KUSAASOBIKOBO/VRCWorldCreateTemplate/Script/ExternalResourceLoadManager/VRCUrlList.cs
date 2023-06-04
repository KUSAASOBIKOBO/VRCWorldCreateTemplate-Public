using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VRCUrlList : UdonSharpBehaviour
    {
        [Header("VRCUrlのリスト")] public VRCUrl[] elementList;
    }
}
