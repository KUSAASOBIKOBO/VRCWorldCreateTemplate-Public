
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)] //VRCObjectSync付きオブジェクトに対応するため
    public class PickupButton : UdonSharpBehaviour
    {
        public UdonSharpBehaviour script;
        public string methodName;
        public bool isResetPosAndRot = true;
        public bool isSyncResetPosAndRot = false;
        private Vector3 position;
        private Quaternion rotation;

        void Start()
        {
            if (isResetPosAndRot)
            {
                position = this.gameObject.transform.localPosition;
                rotation = this.gameObject.transform.localRotation;
            }
        }

        public override void OnPickup()
        {
            script.SendCustomEvent(methodName);
            if (isResetPosAndRot)
            {
                if (isSyncResetPosAndRot)
                {
                    if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetPos");
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetRot");
                }
                else
                {
                    ResetPos();
                    ResetRot();
                }
            }
        }

        public override void OnDrop()
        {
            if (isResetPosAndRot)
            {
                if(isSyncResetPosAndRot)
                {
                    if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetPos");
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetRot");
                }
                else
                {
                    ResetPos();
                    ResetRot();
                }
            }
        }

        public void ResetPos()
        {
            this.gameObject.transform.localPosition = position;
        }

        public void ResetRot()
        {
            this.gameObject.transform.localRotation = rotation;
        }
    }
}
