
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VolumeSwitchButton : UdonSharpBehaviour
    {
        public AudioSource _audioSource;
        public float onVolume;
        public float offVolume;
        public bool status = false;

        public GameObject onImage;
        public GameObject offImage;

        private void OnEnable()
        {
            SetVolume();
        }

        public override void Interact()
        {
            ChangeStatus();
        }

        public void SetVolume()
        {
            if (_audioSource == null) return;
            if (status)
            {
                _audioSource.volume = onVolume;
                if (onImage != null) onImage.SetActive(true);
                if (offImage != null) offImage.SetActive(false);
            }
            else
            {
               _audioSource.volume = offVolume;
                if (onImage != null) onImage.SetActive(false);
                if (offImage != null) offImage.SetActive(true);
            }
        }

        public void ChangeStatus()
        {
            if(status)
            {
                status = false;
            }
            else
            {
                status = true;
            }
            SetVolume();
        }
    }
}
