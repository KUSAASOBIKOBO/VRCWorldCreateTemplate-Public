
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SwitchGameObjectActiveStateInteractButton : UdonSharpBehaviour
    {
        public bool isLocal = true;
        public bool localState = false;
        [UdonSynced(UdonSyncMode.None)] public bool globalState = false;
        public GameObject[] onObject;
        public GameObject[] offObject;

        private void OnEnable()
        {
            Reflect();
        }

        public override void Interact()
        {
            Execute();
        }

        public void Execute()
        {
            if(isLocal)
            {
                localState = !localState;
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                globalState = !globalState;
                RequestSerialization();
            }
            Reflect();
        }

        public void Reflect()
        {
            if(isLocal)
            {
                foreach (GameObject tmp in offObject)
                {
                    if (tmp != null) tmp.SetActive(!localState);
                }

                foreach (GameObject tmp in onObject)
                {
                    if (tmp != null) tmp.SetActive(localState);
                }
            }
            else
            {
                foreach (GameObject tmp in offObject)
                {
                    if (tmp != null) tmp.SetActive(!globalState);
                }

                foreach (GameObject tmp in onObject)
                {
                    if (tmp != null) tmp.SetActive(globalState);
                }
            }
        }

        public override void OnDeserialization()
        {
            Reflect();
        }
    }
}
