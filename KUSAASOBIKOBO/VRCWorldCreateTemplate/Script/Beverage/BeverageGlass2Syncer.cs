
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
//using UnityEngine.UI;
//using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BeverageGlass2Syncer : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIndex))] public int index = 0;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectSurface_Now))] public float surface_Now = 1.0f;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIsHot))] public bool isHot;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectColor))] public Color color;
        public BeverageGlass2 _beverageGlass2;
        private bool gotIndex = false; //インデックスの初期値はBeverageGlass2の値を優先するが、すでに同期変数で受け取ったデータを持っているならこちらのデータを優先する
        private bool gotSurface_Now = false; //インデックスの初期値はBeverageGlass2の値を優先するが、すでに同期変数で受け取ったデータを持っているならこちらのデータを優先する
        private bool gotIsHot = false; //インデックスの初期値はBeverageGlass2の値を優先するが、すでに同期変数で受け取ったデータを持っているならこちらのデータを優先する
        private bool gotColor = false; //インデックスの初期値はBeverageGlass2の値を優先するが、すでに同期変数で受け取ったデータを持っているならこちらのデータを優先する

        //public Text DebugText;


        public int ReflectIndex
        {
            get => index;

            set
            {
                index = value;
                gotIndex = true;
                if (_beverageGlass2 != null)
                {
                    _beverageGlass2.index = index;
                    if (index >= 0) _beverageGlass2.SetBeverageColorLocal();
                    else _beverageGlass2.SetBeverageName();
                }
                
            }
        }

        public float ReflectSurface_Now
        {
            get => surface_Now;

            set
            {
                surface_Now = value;
                gotSurface_Now = true;
                if (_beverageGlass2 != null && _beverageGlass2.rend != null)
                {
                    _beverageGlass2.surface_Now = surface_Now;
                    _beverageGlass2.rend.material.SetFloat("_FillAmount", _beverageGlass2.surface_Now);
                }
            }
        }

        public bool ReflectIsHot
        {
            get => isHot;

            set
            {
                isHot = value;
                gotIsHot = true;
                if (_beverageGlass2 != null)
                {
                    _beverageGlass2.isHot = isHot;
                    if (_beverageGlass2.steamParticle != null)
                    {
                        if (_beverageGlass2.isHot)
                        {
                            _beverageGlass2.steamParticle.gameObject.SetActive(true);
                        }
                        else
                        {
                            _beverageGlass2.steamParticle.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public Color ReflectColor
        {
            get => color;

            set
            {
                color = value;
                gotColor = true;
                //if (DebugText != null) DebugText.text += "" + color + "\n";
                if (_beverageGlass2 != null)
                {
                    _beverageGlass2.color = color;
                    _beverageGlass2.SetMyColor();
                }
            }
        }

        public void SetSurface_Now(float externalColor)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            surface_Now = externalColor;
            RequestSerialization();
            if (_beverageGlass2 != null)
            {
                _beverageGlass2.surface_Now = surface_Now;
                if (_beverageGlass2.rend != null) _beverageGlass2.rend.material.SetFloat("_FillAmount", _beverageGlass2.surface_Now);
            }
        }

        public void SetColor(Color externalColor)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            color = externalColor;
            RequestSerialization();
            if (_beverageGlass2 != null)
            {
                _beverageGlass2.color = color;
                _beverageGlass2.SetMyColor();
            }
        }

        public void SetIndex(int externalIndex)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            index = externalIndex;
            RequestSerialization();
            if (_beverageGlass2 != null)
            {
                _beverageGlass2.index = index;
            }
        }

        public void SetIsHot(bool externalValue)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            isHot = externalValue;
            RequestSerialization();
            if (_beverageGlass2 != null)
            {
                _beverageGlass2.isHot = isHot;
                if (_beverageGlass2.steamParticle != null)
                {
                    if (_beverageGlass2.isHot)
                    {
                        _beverageGlass2.steamParticle.gameObject.SetActive(true);
                    }
                    else
                    {
                        _beverageGlass2.steamParticle.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void AddSurface_Now(float externalValue)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            surface_Now += externalValue;
            RequestSerialization();
            if (_beverageGlass2 != null)
            {
                _beverageGlass2.surface_Now = surface_Now;
                if (_beverageGlass2.rend != null) _beverageGlass2.rend.material.SetFloat("_FillAmount", _beverageGlass2.surface_Now);
            }
        }

        public void SubtractionSurface_Now(float externalValue)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            surface_Now -= externalValue;
            RequestSerialization();
            if (_beverageGlass2 != null)
            {
                _beverageGlass2.surface_Now = surface_Now;
                if (_beverageGlass2.rend != null) _beverageGlass2.rend.material.SetFloat("_FillAmount", _beverageGlass2.surface_Now);
            }
        }

        void OnEnable()//Start
        {
            SyncRequest();
            if (_beverageGlass2 != null)
            {
                if (!gotIndex && !gotColor)
                {
                    _beverageGlass2.SetBeverageColorLocal();
                }

                if (!gotSurface_Now)
                {
                    if (_beverageGlass2.rend != null) _beverageGlass2.rend.material.SetFloat("_FillAmount", _beverageGlass2.surface_Now);
                }
                   
                if (!gotIsHot)
                {
                    if (_beverageGlass2.steamParticle != null)
                    {
                        if (_beverageGlass2.isHot)
                        {
                            _beverageGlass2.steamParticle.gameObject.SetActive(true);
                        }
                        else
                        {
                            _beverageGlass2.steamParticle.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public void Sync()
        {
            RequestSerialization();
        }

        public void SyncRequest()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Sync");
        }
#if UNITY_EDITOR
        public void DuplicateSyncer(int _index, float _surface_Now, bool _isHot)
        {
                index = _index;
                surface_Now = _surface_Now;
                isHot = _isHot;
        }
#endif
    }
}
