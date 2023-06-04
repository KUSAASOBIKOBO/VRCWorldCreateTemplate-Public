
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
//using UnityEngine.UI;
//using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BeverageShaker2DistributionManager : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.None)/*, FieldChangeCallback(nameof(ReflectDistribution))*/] public float[] distribution;
        public BeverageShaker2 _beverageShaker;
        public bool gotSync = false;

        //public Text DebugText;

        void Start()
        {
            SyncRequest();
            if (!gotSync && _beverageShaker._beverageGlass._beverageList != null && distribution.Length != _beverageShaker._beverageGlass._beverageList.beverageNameList.Length) distribution = new float[_beverageShaker._beverageGlass._beverageList.beverageNameList.Length];
        }
        /*
        public float[] ReflectDistribution
        {
            get => distribution;

            set
            {
                distribution = value;
                gotSync = true;
                if (DebugText != null) DebugText.text += "GetDistribution length:" + distribution.Length + "\n";
                if (_beverageShaker != null)
                {
                    if (_beverageShaker._beverageGlass._beverageList != null && distribution.Length != _beverageShaker._beverageGlass._beverageList.beverageNameList.Length) distribution = new float[_beverageShaker._beverageGlass._beverageList.beverageNameList.Length];
                    _beverageShaker.distribution = distribution;
                    _beverageShaker.ShowDistribution();
                }
            }
        }*/

        public override void OnDeserialization()
        {
            gotSync = true;
            //if (DebugText != null) DebugText.text += "OnDeserialization length:" + distribution.Length + "\n";
            if (_beverageShaker != null)
            {
                if (_beverageShaker._beverageGlass._beverageList != null && distribution.Length != _beverageShaker._beverageGlass._beverageList.beverageNameList.Length) distribution = new float[_beverageShaker._beverageGlass._beverageList.beverageNameList.Length];
                _beverageShaker.distribution = distribution;
                _beverageShaker.ShowDistribution();
            }
        }

        public void AddDistribution(int index, float value)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (_beverageShaker._beverageGlass._beverageList != null && distribution.Length != _beverageShaker._beverageGlass._beverageList.beverageNameList.Length) distribution = new float[_beverageShaker._beverageGlass._beverageList.beverageNameList.Length];
            distribution[index] += value;
            RequestSerialization();
            if (_beverageShaker != null) _beverageShaker.distribution = distribution;
        }

        public void ResetDistribution()
        {
            int repertory = distribution.Length;
            if (repertory == 0) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            for (int i = 0; i < repertory; i++)
            {
                distribution[i] = 0.0f;
            }
            RequestSerialization();
            if (_beverageShaker != null) _beverageShaker.distribution = distribution;
        }

        public void Sync()
        {
            RequestSerialization();
        }

        public void SyncRequest()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Sync");
        }
    }
}
