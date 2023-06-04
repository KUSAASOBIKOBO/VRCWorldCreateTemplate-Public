
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class InteractButton : UdonSharpBehaviour
    {
        public UdonSharpBehaviour script;
        public string methodName;

        public override void Interact()
        {
            if(script != null) script.SendCustomEvent(methodName);
        }
    }
}
