
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class BeverageShaker2 : UdonSharpBehaviour
    {
        public float[] distribution;
        public BeverageShaker2DistributionManager _distributionSyncer;

        public float allowableLimit;

        public int bodyStatus; //OwnerSync
        public GameObject[] body;
        public GameObject top;
        public GameObject strainer;

        public BoxCollider hitbox;
        public ParticleSystem particle;
        public Text infomationText;
        public string infomationTitle = "シェイカーの中身";

        public float colorAlpha = 180.0f;

        public BeverageGlass2 _beverageGlass;

        public float pouringSpeed = 0.004f;

        bool ice; //OwnerSync
        bool fakeIce; //OwnerSync
        public GameObject iceObject;
        public GameObject fakeIceObject;
        public int closeBodyStatus = 0;
        public int openBodyStatus = 1;
        public int iceLessBodyStatus = 2;
        public int iceBodyStatus = 3;
        public int fakeIceBodyStatus = 4;

        bool isShake;
        private Vector3 shakeLatestPos;
        private Vector3 shakeSpeed;
        public float Sensitivity = 1f;
        public float shakeCountMax = 1f;
        private float shakeCount = 0f;

        private float isShakeCounter = 0.0f;
        private float isShakeCounterMax = 3.0f;

        //Sound
        public AudioSource _audioSource;
        public AudioClip putIceSound;
        public AudioClip trashSound;
        public AudioClip shakeSound;
        public AudioClip capSound;

        private bool lastInfomationTextActive;

        //Effect
        public ParticleSystem finishParticle; //完成した時に散るパーティクル

        private bool isExecutedThisFrame_PouringOwner = false;


        void OnEnable()//Start
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SyncRequest");
            //ShowDistribution();
        }

        void LateUpdate()
        {
            //1フレームに何度も処理しないようにフラグ処理する。
            isExecutedThisFrame_PouringOwner = false;
        }

        void Update()
        {
            if (_beverageGlass == null) return;

            if (infomationText != null)
            {
                bool aih_tmp = infomationText.gameObject.activeInHierarchy;
                if (!lastInfomationTextActive && aih_tmp)
                {
                    ShowDistribution();
                }
                lastInfomationTextActive = aih_tmp;
            }

            //shake音を遅延してON/OFFする
            if (_beverageGlass.GetIsHold())
            {
                Shake();
                if (isShakeCounter > 0.0f)
                {
                    isShakeCounter -= Time.deltaTime;
                    if (isShakeCounter <= 0.0f)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopShake");
                    }
                }
            }

            if (_beverageGlass.GetIsUse())
            {
                if (_beverageGlass.IsEmpty())
                {
                    ResetAll();
                }
            }
        }

        public void PouringOwner(GameObject pouringObject)
        {
            if (_distributionSyncer != null && _beverageGlass._beverageList != null && distribution.Length != _beverageGlass._beverageList.beverageNameList.Length) distribution = _distributionSyncer.distribution;
            if (isExecutedThisFrame_PouringOwner) return;
            isExecutedThisFrame_PouringOwner = true;
            shakeCount = 0;
            bool isParticleOwner = false;//particleの発信元ボトルのUse状態を調べてローカルでUseを立てていたなら更新する。この時このグラスのオーナー権が移ることに注意。
            if (_beverageGlass._beverageList != null)
            {
                if (pouringObject != null && (particle == null || pouringObject != particle.gameObject))
                {
                    if (_beverageGlass._beverageList.bottleParticle.Length == _beverageGlass._beverageList._beverageBottle.Length)
                    {
                        for (int i = 0; i < _beverageGlass._beverageList.bottleParticle.Length; i++)
                        {
                            if (_beverageGlass._beverageList.bottleParticle[i].gameObject == pouringObject)
                            {
                                isParticleOwner = _beverageGlass._beverageList._beverageBottle[i].GetIsUse();
                                if (isParticleOwner)
                                {
                                    if (_distributionSyncer != null) _distributionSyncer.AddDistribution(_beverageGlass._beverageList._beverageBottle[i].index, _beverageGlass.pouringSpeed * Time.deltaTime);
                                    ShowDistribution();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetOriginalColor()
        {
            if (_beverageGlass == null) return;
            if (_beverageGlass._beverageList != null)
            {
                int repertory = distribution.Length;
                float alpha = colorAlpha / 255.0f;
                if (repertory != _beverageGlass._beverageList.beverageColorList.Length) return;

                float red = 0.0f;
                float green = 0.0f;
                float blue = 0.0f;

                float iceCompensate = 0.0f;//氷補正(通常の氷を使って作ったオリジナルカクテルは本来の色より若干薄くなる。レシピのカクテルは影響を受けない)
                if (ice) iceCompensate = 5.0f / 255.0f;
                red += iceCompensate;
                green += iceCompensate;
                blue += iceCompensate;

                float totalDistribution = 0.0f;
                for (int i = 0; i < repertory; i++)
                {
                    totalDistribution += distribution[i];
                }

                if (totalDistribution == 0.0f) totalDistribution = 1.0f;//0割り回避

                for (int i = 0; i < repertory; i++)
                {
                    if (distribution[i] != 0.0f)
                    {
                        float concentration = distribution[i] / totalDistribution;
                        float redTmp = _beverageGlass._beverageList.beverageColorList[i].r * concentration;
                        float greenTmp = _beverageGlass._beverageList.beverageColorList[i].g * concentration;
                        float blueTmp = _beverageGlass._beverageList.beverageColorList[i].b * concentration;

                        red += redTmp;
                        green += greenTmp;
                        blue += blueTmp;
                        Debug.Log("red:" + red * 255.0f);
                        Debug.Log("green:" + green * 255.0f);
                        Debug.Log("blue:" + blue * 255.0f);

                    }
                }
                _beverageGlass.SetColor(new Color(red, green, blue, alpha));
                _beverageGlass.SetMyColor();
            }
        }

        public void Shake()
        {
            if (shakeCount >= shakeCountMax) return;
            if (_beverageGlass == null) return;
            if (!ice && !fakeIce) return;
            if (bodyStatus != closeBodyStatus) return;
            if (_beverageGlass.IsEmpty()) return;
            Debug.Log("shake");

            if (finishParticle != null && finishParticle.gameObject.activeSelf) finishParticle.gameObject.SetActive(false);
            Vector3 speed = ((this.gameObject.transform.position - shakeLatestPos) / Time.deltaTime);
            shakeLatestPos = this.gameObject.transform.position;
            if (speed.x < 0f)
            {
                speed.x = speed.x * (-1f);
            }
            if (speed.z < 0f)
            {
                speed.z = speed.z * (-1f);
            }
            if (speed.x >= Sensitivity)
            {
                shakeCount += speed.x * 0.03f;
                isShakeCounter = isShakeCounterMax;
                if (!isShake) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartShake");
            }
            else if (speed.z >= Sensitivity)
            {
                shakeCount += speed.z * 0.03f;
                isShakeCounter = isShakeCounterMax;
                if(!isShake) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartShake");
            }

            Debug.Log("speed.x * 0.03f:" + speed.x);

            Debug.Log("speed.z * 0.03f >= Sensitivity:" + speed.z);
            if (shakeCount >= shakeCountMax)
            {
                shakeCount = shakeCountMax;
                if (_beverageGlass._beverageList != null)
                {
                    _beverageGlass.SetIndex(_beverageGlass._beverageList.CheckRecipe(this));
                    Debug.Log("FixedIndex:" + _beverageGlass.index);
                    if (_beverageGlass.index < 0)
                    {
                        SetOriginalColor();
                    }
                    else
                    {
                        _beverageGlass.SetBeverageColor();
                    }
                    if (finishParticle != null) finishParticle.gameObject.SetActive(true);
                }
            }
        }

        public void ResetAll()
        {
            ResetDistribution();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetBodyStatus");
        }

        public void ResetBodyStatus()
        {
            ice = false;
            fakeIce = false;
            shakeCount = 0;
            if (bodyStatus == iceBodyStatus || bodyStatus == fakeIceBodyStatus)
            {
                bodyStatus = iceLessBodyStatus;
                SwitchBodyPrefab();
            }
            MakeSound_trashSound();
        }

        public void AddIce()
        {
            if (ice || fakeIce) return;
            ice = true;
            if (bodyStatus == iceLessBodyStatus)
            {
                bodyStatus = iceBodyStatus;
                SwitchBodyPrefab();
            }
            MakeSound_putIceSound();
        }

        public void AddFakeIce()
        {
            if (ice || fakeIce) return;
            fakeIce = true;
            if (bodyStatus == iceLessBodyStatus)
            {
                bodyStatus = iceBodyStatus;
                SwitchBodyPrefab();
            }
            MakeSound_putIceSound();
        }

        public void SwitchBodyStatus_open()
        {
            bodyStatus = openBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(false);
            SwitchBodyPrefab();
            MakeSound_capSound();
        }
        public void SwitchBodyStatus_close()
        {
            bodyStatus = closeBodyStatus;
            if (top != null) top.SetActive(false);
            if (strainer != null) strainer.SetActive(false);
            shakeLatestPos = this.gameObject.transform.position;
            SwitchBodyPrefab();
            MakeSound_capSound();
        }
        public void SwitchBodyStatus_iceLess()
        {
            bodyStatus = iceLessBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(true);
            SwitchBodyPrefab();
            MakeSound_capSound();
        }
        public void SwitchBodyStatus_ice()
        {
            bodyStatus = iceBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(true);
            SwitchBodyPrefab();
            MakeSound_capSound();
        }
        public void SwitchBodyStatus_fakeIce()
        {
            bodyStatus = fakeIceBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(true);
            SwitchBodyPrefab();
            MakeSound_capSound();
        }

        public void SwitchBodyStatus_open_global()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_open");
        }

        public void SwitchBodyStatus_close_global()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_close");
        }

        public void SwitchBodyStatus_fullopen_global()
        {
            if (ice)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_ice");
            }
            else if (fakeIce)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_fakeIce");
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_iceLess");
            }
        }

        public void SwitchBodyStatus()
        {
            if (bodyStatus == closeBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_open");
            }
            else if (bodyStatus == openBodyStatus)
            {
                if (ice)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_ice");
                }
                else if (fakeIce)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_fakeIce");
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_iceLess");
                }
            }
            else if (bodyStatus == iceLessBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_close");
            }
            else if (bodyStatus == iceBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_close");
            }
            else if (bodyStatus == fakeIceBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_close");
            }
        }

        public void SwitchBodyPrefab()
        {
            if (bodyStatus < 0) bodyStatus = 0;
            if (bodyStatus >= body.Length) bodyStatus = body.Length - 1;
            if (finishParticle != null && finishParticle.gameObject.activeSelf) finishParticle.gameObject.SetActive(false);
            for (int i = 0; i < body.Length; i++)
            {
                if (i == bodyStatus)
                {
                    body[i].SetActive(true);
                }
                else
                {
                    body[i].SetActive(false);
                }
            }

            if (hitbox != null && (bodyStatus == iceLessBodyStatus || bodyStatus == iceBodyStatus || bodyStatus == fakeIceBodyStatus)) hitbox.gameObject.SetActive(true);
            else hitbox.gameObject.SetActive(false);
        }

        public void ResetDistribution()
        {
            if(_distributionSyncer != null) _distributionSyncer.ResetDistribution();
            ShowDistribution();

            if(_beverageGlass != null)
            {
                _beverageGlass.ForcedEmpty();
            }
        }

        public void ShowDistribution()
        {
            if (infomationText == null) return;
            if (!infomationText.gameObject.activeInHierarchy) return;
            if (_distributionSyncer != null && _beverageGlass._beverageList != null && distribution.Length != _beverageGlass._beverageList.beverageNameList.Length) distribution = _distributionSyncer.distribution;
            infomationText.text = infomationTitle + "\n";
            int repertory = distribution.Length;
            float totalDistribution = 0.0f;

            for (int i = 0; i < repertory; i++)
            {
                totalDistribution += distribution[i];
            }

            if (totalDistribution == 0.0f) totalDistribution = 1.0f;//0割り回避

            if (_beverageGlass._beverageList != null)
            {
                if (repertory != _beverageGlass._beverageList.beverageNameList.Length) return;
                for (int i = 0; i < repertory; i++)
                {
                    if (distribution[i] != 0.0f)
                    {
                        infomationText.text += _beverageGlass._beverageList.beverageNameList[i] + " : " + (int)((distribution[i] / totalDistribution) * 100.0f) + "%\n";
                    }
                }
            }
        }

        public void StartShake()
        {
            isShake = true;
            MakeSound(shakeSound, false);
        }

        public void StopShake()
        {
            isShake = false;
            StopSound(shakeSound);
        }

        public override void OnDrop()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopShake");
        }

        private void OnParticleCollision(GameObject other)
        {
            if (hitbox == null || !hitbox.gameObject.activeSelf) return;
            PouringOwner(other);
        }

        private void OnTriggerEnter(Collider other) //要isTrigger 
        {
            //ヒットしたオブジェクトのオーナーに共有を任せる。Pickup時にオーナーが移る設定にしていれば原則オーナーではあるはず
            if (other.gameObject == iceObject)
            {
                if (Networking.IsOwner(Networking.LocalPlayer, other.gameObject)) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "AddIce");
            }
            else if (other.gameObject == fakeIceObject)
            {
                if (Networking.IsOwner(Networking.LocalPlayer, other.gameObject)) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "AddFakeIce");
            }
        }

        public void MakeSound(AudioClip sound, bool atOnce = true)
        {
            if (_audioSource != null && sound != null)
            {
                if (_audioSource.isPlaying && _audioSource.clip == sound && !atOnce) return;//ループサウンド再生中に同一のループサウンドの再生命令が来ても無視する。
                _audioSource.Stop();
                _audioSource.clip = sound;
                _audioSource.loop = !atOnce;
                _audioSource.Play();
            }
        }

        public void StopSound(AudioClip sound)
        {
            if (_audioSource != null && sound != null)
            {
                if (_audioSource.isPlaying && _audioSource.clip == sound) _audioSource.Stop();
            }
        }

        public void MakeSound_putIceSound()
        {
            MakeSound(putIceSound);
        }

        public void MakeSound_capSound()
        {
            MakeSound(capSound);
        }

        public void MakeSound_trashSound()
        {
            MakeSound(trashSound);
        }

        public void SyncIceOn()
        {
            ice = true;
        }

        public void SyncIceOff()
        {
            ice = false;
        }

        public void SyncFakeIceOn()
        {
            fakeIce = true;
        }

        public void SyncFakeIceOff()
        {
            fakeIce = false;
        }

        public void SwitchBodyStatus_open_silent()
        {
            bodyStatus = openBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(false);
            SwitchBodyPrefab();
        }
        public void SwitchBodyStatus_close_silent()
        {
            bodyStatus = closeBodyStatus;
            if (top != null) top.SetActive(false);
            if (strainer != null) strainer.SetActive(false);
            shakeLatestPos = this.gameObject.transform.position;
            SwitchBodyPrefab();
        }
        public void SwitchBodyStatus_iceLess_silent()
        {
            bodyStatus = iceLessBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(true);
            SwitchBodyPrefab();
        }
        public void SwitchBodyStatus_ice_silent()
        {
            bodyStatus = iceBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(true);
            SwitchBodyPrefab();
        }
        public void SwitchBodyStatus_fakeIce_silent()
        {
            bodyStatus = fakeIceBodyStatus;
            if (top != null) top.SetActive(true);
            if (strainer != null) strainer.SetActive(true);
            SwitchBodyPrefab();
        }

        public void SyncRequest()
        {
            if(ice) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SyncIceOn");
            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SyncIceOff");

            if (fakeIce) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SyncFakeIceOn");
            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SyncFakeIceOff");

            //各ステータスのインデックスは固定にしたくないためconstかenumでないとつかえないswitch文ではなくifelseで処理

            if(bodyStatus == openBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_open_silent");
            }
            else if (bodyStatus == closeBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_close_silent");
            }
            else if (bodyStatus == iceBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_ice_silent");
            }
            else if (bodyStatus == fakeIceBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_fakeIce_silent");
            }
            else if (bodyStatus == iceLessBodyStatus)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SwitchBodyStatus_iceLess_silent");
            }
        }
    }
}
