
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    public class WarpCollider : UdonSharpBehaviour
    {
        [Header("コライダーヒットによるワープを行わない")] public bool isInteractOnly = false;


        void Start()
        {

        }
    }
}
