
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GameObjectList : UdonSharpBehaviour
    {
        [Header("GameObjectのリスト")] public GameObject[] elementList;
    }
}
