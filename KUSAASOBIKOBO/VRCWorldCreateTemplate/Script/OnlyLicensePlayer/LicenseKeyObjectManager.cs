
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LicenseKeyObjectManager : UdonSharpBehaviour
    {
        public string[] licensePlayerDisplayName;
        public GameObject licenseKeyObject;
        public bool defaultStatus = false;
        void OnEnable()
        {
            if (licenseKeyObject == null) return;
            bool result = false;
            foreach (string tmp in licensePlayerDisplayName)
            {
                if (tmp == Networking.LocalPlayer.displayName)
                {
                    result = true;
                    break;
                }
            }
            if (result)
            {
                licenseKeyObject.SetActive(true);
            }
            else
            {
                licenseKeyObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            if (licenseKeyObject == null) return;
            licenseKeyObject.SetActive(defaultStatus);
        }
    }
}
