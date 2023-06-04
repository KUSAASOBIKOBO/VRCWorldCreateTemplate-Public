using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MultiAreaLightingChangeButton : UdonSharpBehaviour
    {
        public bool isLocal;//デフォルトはグローバルの値を書き換えます
        public Color _color;
        public bool isUseTimeManagerLighting = false;
        [UdonSynced(UdonSyncMode.None)] public float globalRate = 1.0f;
        public float localRate = 1.0f;

        public MultiAreaManager _multiAreaManager;
        public Slider _slider;

        public override void Interact()
        {
            Execute();
        }

        public void Execute()
        {
            int r = (int)(_color.r * 255.0f * globalRate * localRate);
            int g = (int)(_color.g * 255.0f * globalRate * localRate);
            int b = (int)(_color.b * 255.0f * globalRate * localRate);

            if (isLocal)
            {
                if (!isUseTimeManagerLighting) _multiAreaManager.AreaLightingChangeLocal(r, g, b);
                else _multiAreaManager.AreaLightingChangeLocal(-1, -1, -1);
            }
            else
            {
                if (!isUseTimeManagerLighting) _multiAreaManager.AreaLightingChangeGlobal(r, g, b);
                else _multiAreaManager.AreaLightingChangeGlobal(-1, -1, -1);
            }
        }

        public void ChangeRate()
        {
            if (isLocal)
            {
                ChangeLocalRate();
            }
            else
            {
                ChangeGlobalRate();
            }
        }

        private void ChangeLocalRate()
        {
            if(_slider != null) localRate = _slider.normalizedValue;
        }

        private void ChangeGlobalRate()
        {
            if (_slider != null)
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                globalRate = _slider.normalizedValue;
                RequestSerialization();
            }
        }
    }
}

