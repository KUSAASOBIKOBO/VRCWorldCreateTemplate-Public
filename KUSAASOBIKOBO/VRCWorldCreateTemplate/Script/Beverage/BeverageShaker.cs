
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BeverageShaker : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectDistribution))] public float[] distribution;

        public BeverageList _beverageList;

        public float allowableLimit;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectBodyStatus))] public int bodyStatus;
        public GameObject[] body;
        public BoxCollider hitbox;
        public ParticleSystem particle;
        public Text infomationText;
        public string infomationTitle = "シェイカーの中身";

        public float colorAlpha = 180.0f;

        public BeverageGlass _beverageGlass;

        public bool isPouring;
        private GameObject pouringObject;
        public float pouringSpeed = 0.004f;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIce))] bool ice;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectFakeIce))] bool fakeIce;
        public GameObject iceObject;
        public GameObject fakeIceObject;
        public int closeBodyStatus = 0;
        public int openBodyStatus = 1;
        public int iceLessBodyStatus = 2;
        public int iceBodyStatus = 3;
        public int fakeIceBodyStatus = 4;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIsShake))] bool isShake;
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

         //Effect
         public ParticleSystem finishParticle; //完成した時に散るパーティクル

        private bool isExecutedThisFrame_PouringOwner = false;


        void Start()
        {
            SyncRequest();
            SwitchBodyPrefab();
            ShowDistribution();
        }

        void LateUpdate()
        {
            //Update実行中のプレイヤー数分だけリクエストが来るため、1フレームに何度も処理しないようにフラグ処理する。
            //UpdateにOwnerを埋めない理由はactiveInHierarchyがFalseのオブジェクトにアタッチされたスクリプトでUpdateが実行されないため、OwnerがactiveInHierarchyをFalseにしていた場合実行されなくなってしまうから。
            isExecutedThisFrame_PouringOwner = false;
        }

        void Update()
        {
            if(_beverageGlass == null) return;

            //shake音を遅延してON/OFFする
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                if(isShakeCounter > 0.0f)
                {
                    isShakeCounter -= Time.deltaTime;
                    if(isShakeCounter <= 0.0f)
                    {
                        if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                            StopShakeOwner();
                            //shake音を止める
                            StopSound(shakeSound);
                        }
                        else
                        {
                            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                            StopShakeOwner();
                            //shake音を止める
                            StopSound(shakeSound);
                            //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StopShakeOwner");
                        }
                    }
                }
            }

            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Shake(); //Pickup時に基本オーナーになってるはずでピックアップ中にほかの人が蓋を閉めたらオーナーが変わっるがその人が計算するだけ
            if(_beverageGlass.isUse)
            {
                if(_beverageGlass.IsEmpty()){
                    ResetAll();
                }
            }

            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                PouringOwner();
            }
            // else
            // {
            //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "PouringOwner");
            // }
            
        }

        public void PouringOwner()
        {
            if(_beverageList != null　&& distribution.Length != _beverageList.beverageNameList.Length) distribution = new float[_beverageList.beverageNameList.Length];
            if(isPouring)
            {
                 if(isExecutedThisFrame_PouringOwner) return;
                isExecutedThisFrame_PouringOwner = true;
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                shakeCount = 0;
                if(_beverageList != null)
                {
                    if(pouringObject != null && pouringObject != particle.gameObject){
                        if(_beverageList.bottleParticle.Length == _beverageList._beverageBottle.Length)
                        {
                            for(int i=0;i<_beverageList.bottleParticle.Length;i++)
                            {
                                if(_beverageList.bottleParticle[i].gameObject == pouringObject)
                                {
                                    distribution[_beverageList._beverageBottle[i].index] += pouringSpeed * Time.deltaTime;
                                }
                            } 
                        }
                    }
                }
            }
            isPouring = false;
            ShowDistribution();
            RequestSerialization();
        }

        public float[] ReflectDistribution
        {
            get => distribution;

            set
            {
                distribution = value;
                ShowDistribution();
            }
        }

        public int ReflectBodyStatus
        {
            get => bodyStatus;

            set
            {
                bodyStatus = value;
                //ボディステータスを見た目に反映
                SwitchBodyPrefab();
                if(bodyStatus == closeBodyStatus)
                {
                    shakeLatestPos = this.gameObject.transform.position;
                }
            }
        }

        public bool ReflectIce
        {
            get => ice;

            set
            {
                ice = value;
                //見た目はボディステータス変更時に同期
            }
        }

        public bool ReflectFakeIce
        {
            get => fakeIce;

            set
            {
                fakeIce = value;
                //見た目はボディステータス変更時に同期
            }
        }

        public bool ReflectIsShake
        {
            get => isShake;

            set
            {
                isShake = value;
                if(isShake){
                    //音を鳴らす
                    MakeSound(shakeSound,false);
                    Debug.Log("Sound:ReflectIsShake");
                }
                else
                {
                    //音を止める
                    StopSound(shakeSound);
                }
            }
        }

        public void SetOriginalColor()
        {
            if(_beverageGlass == null) return;
            if(_beverageList != null) 
            {
                int repertory = distribution.Length;
                float alpha = colorAlpha/255.0f;
                if(repertory != _beverageList.beverageColorList.Length) return;
                
                float red = 0.0f;
                float green = 0.0f;
                float blue = 0.0f;

                float iceCompensate = 0.0f;//氷補正(通常の氷を使って作ったオリジナルカクテルは本来の色より若干薄くなる。レシピのカクテルは影響を受けない)
                if(ice) iceCompensate = 5.0f/255.0f;
                red += iceCompensate;
                green += iceCompensate;
                blue += iceCompensate;

                float totalDistribution = 0.0f;
                for(int i=0; i<repertory; i++)
                {
                    totalDistribution += distribution[i];
                }

                if(totalDistribution == 0.0f) totalDistribution = 1.0f;//0割り回避

                for(int i=0; i<repertory; i++)
                {
                    if(distribution[i] != 0.0f){
                        float concentration = distribution[i]/totalDistribution;
                        float redTmp = _beverageList.beverageColorList[i].r*concentration;
                        float greenTmp = _beverageList.beverageColorList[i].g*concentration;
                        float blueTmp = _beverageList.beverageColorList[i].b*concentration;           

                        red += redTmp;
                        green += greenTmp;
                        blue += blueTmp;
                        Debug.Log("red:"+red*255.0f);
                        Debug.Log("green:"+green*255.0f);
                        Debug.Log("blue:"+blue*255.0f);


                        //red = (red*255.0f + _beverageList.beverageColorList[i].r*(distribution[i]/totalDistribution)*255.0f)/255.0f/255.0f;
                        //green = (green*255.0f + _beverageList.beverageColorList[i].g*(distribution[i]/totalDistribution)*255.0f)/255.0f/255.0f;
                        //blue = (blue*255.0f + _beverageList.beverageColorList[i].b*(distribution[i]/totalDistribution)*255.0f)/255.0f/255.0f;
                    }
                }
                _beverageGlass.SetColor(new Color(red,green,blue,alpha));
                _beverageGlass.SetMyColor();
            }
        }

        public void Shake()
        {
            if (shakeCount >= shakeCountMax) return;
            if(_beverageGlass == null) return;
            if(!ice && !fakeIce) return;
            if(bodyStatus != closeBodyStatus) return;
            if(_beverageGlass.IsEmpty()) return;
            Debug.Log("shake");

            if(finishParticle != null && finishParticle.gameObject.activeSelf) finishParticle.gameObject.SetActive(false);
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
                if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                    StartShakeOwner();
                    //shake音を鳴らす
                    MakeSound(shakeSound,false);
                    Debug.Log("Sound:Shake()_x");
                }
                else
                {
                    if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    StartShakeOwner();
                    //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StartShakeOwner");
                }
            }
            else if (speed.z >= Sensitivity)
            {
                shakeCount += speed.z * 0.03f;
                isShakeCounter = isShakeCounterMax;
                if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                    StartShakeOwner();
                    //shake音を鳴らす
                    MakeSound(shakeSound,false);
                    Debug.Log("Sound:Shake()_y");
                }
                else
                {
                    if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    StartShakeOwner();
                    //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StartShakeOwner");
                }
            }

            Debug.Log("speed.x * 0.03f:"+speed.x);

            Debug.Log("speed.z * 0.03f >= Sensitivity:"+speed.z);
            if (shakeCount >= shakeCountMax)
            {
                shakeCount = shakeCountMax;
                if(_beverageList != null) 
                {
                    _beverageGlass.SetIndex(_beverageList.CheckRecipe(this));
                    Debug.Log("FixedIndex:"+_beverageGlass.index);
                    if(_beverageGlass.index < 0)
                    {
                        SetOriginalColor();
                    }
                    else
                    {
                        _beverageGlass.SetBeverageColor();
                    }
                    if(finishParticle != null) finishParticle.gameObject.SetActive(true);
                }
            }
        }
        
        public void ResetAll()
        {
            ResetAllOwner();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_trashSound");
            // if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
            //     ResetAllOwner();
            // }
            // else
            // {
            //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ResetAllOwner");
            // }
        }

        public void ResetAllOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            ResetDistribution();
            ice = false;
            fakeIce = false;
            shakeCount = 0;
            if(bodyStatus == iceBodyStatus || bodyStatus == fakeIceBodyStatus)
            {
                bodyStatus = iceLessBodyStatus;
            }
            if(_beverageGlass != null) _beverageGlass.ForcedEmpty();
            SwitchBodyPrefab();
            RequestSerialization();
        }
        
        public void AddIce()
        {
            AddIceOwner();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_putIceSound");
            // if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
            //     AddIceOwner();
            // }
            // else
            // {
            //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "AddIceOwner");
            // }
        }

        public void AddIceOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if(ice || fakeIce) return;
            ice = true;
            bodyStatus = iceBodyStatus;
            SwitchBodyPrefab();
            RequestSerialization();
        }
        
        public void AddFakeIce()
        {
            AddFakeIceOwner();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_putIceSound");
            // if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
            //     AddFakeIceOwner();
            // }
            // else
            // {
            //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "AddFakeIceOwner");
            // }
        }

        public void AddFakeIceOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if(ice || fakeIce) return;
            fakeIce = true;
            bodyStatus = fakeIceBodyStatus;
            SwitchBodyPrefab();
            RequestSerialization();
        }

        public void SwitchBodyStatus()
        {
            SwitchBodyStatusOwner();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_capSound");
            // if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
            //     SwitchBodyStatusOwner();
            // }
            // else
            // {
            //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SwitchBodyStatusOwner");
            // }
        }

        public void SwitchBodyStatusOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if(bodyStatus == closeBodyStatus)
            {
                bodyStatus = openBodyStatus;
            }
            else if(bodyStatus == openBodyStatus)
            {
                if(ice)
                {
                    bodyStatus = iceBodyStatus;
                }else if(fakeIce)
                {
                    bodyStatus = fakeIceBodyStatus;
                }else{
                    bodyStatus = iceLessBodyStatus;
                }
            }
            else if(bodyStatus == iceLessBodyStatus)
            {
                bodyStatus = closeBodyStatus;
                shakeLatestPos = this.gameObject.transform.position;    
            }
            else if(bodyStatus == iceBodyStatus)
            {
                bodyStatus = closeBodyStatus;
                shakeLatestPos = this.gameObject.transform.position;
            }
            else if(bodyStatus == fakeIceBodyStatus)
            {
                bodyStatus = closeBodyStatus;
                shakeLatestPos = this.gameObject.transform.position;
            }

            SwitchBodyPrefab();
            RequestSerialization();
        }

        public void SwitchBodyPrefab()
        {
            if(bodyStatus < 0) bodyStatus = 0;
            if(bodyStatus >= body.Length) bodyStatus = body.Length - 1;
            if(finishParticle != null && finishParticle.gameObject.activeSelf)finishParticle.gameObject.SetActive(false);
            for(int i=0; i < body.Length; i++)
            {
                if(i == bodyStatus){
                    body[i].SetActive(true);
                } 
                else
                {
                    body[i].SetActive(false);
                }
            }

            if(hitbox != null && (bodyStatus == iceLessBodyStatus || bodyStatus == iceBodyStatus || bodyStatus == fakeIceBodyStatus)) hitbox.gameObject.SetActive(true);
            else hitbox.gameObject.SetActive(false);
        }
        
        public void ResetDistribution()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                ResetDistributionOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                ResetDistributionOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ResetDistributionOwner");
            }
        }

        public void ResetDistributionOwner()
        {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                int repertory = distribution.Length;
                if(repertory == 0) return;
                for(int i=0; i<repertory; i++)
                {
                   distribution[i] = 0.0f;
                }
                RequestSerialization();
                ShowDistribution();
        }

        public void ShowDistribution()
        {
            if(infomationText == null) return;
            infomationText.text = infomationTitle + "\n";
            int repertory = distribution.Length;
            float totalDistribution = 0.0f;

            for(int i=0; i<repertory; i++)
            {
                totalDistribution += distribution[i];
            }

            if(totalDistribution == 0.0f) totalDistribution = 1.0f;//0割り回避

            if(_beverageList != null)
            {
                if(repertory != _beverageList.beverageNameList.Length) return;
                for(int i=0; i<repertory; i++)
                {
                    if(distribution[i] != 0.0f){
                        infomationText.text += _beverageList.beverageNameList[i] + " : " + (int)((distribution[i]/totalDistribution)*100.0f) + "%\n";
                    }
                }
            }
        }

        public void StartShakeOwner()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            isShake = true;
            RequestSerialization();
        }
        
        public void StopShakeOwner()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            isShake = false;
            RequestSerialization();
        }

        public override void OnDrop()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StopShakeOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StopShakeOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StopShakeOwner");
            }
        }

        public void StartPouringOwner()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            isPouring = true;
            RequestSerialization();
        }
        
        public void StopPouringOwner()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            isPouring = false;
            RequestSerialization();
        }

        private void OnParticleCollision(GameObject other)
        {
            if(hitbox == null || !hitbox.gameObject.activeSelf) return;
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StartPouringOwner();
            }
            else
            {
                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StartPouringOwner();
            }
            pouringObject = other;
        }

        private void OnTriggerEnter(Collider other) //要isTrigger 
        {
            if(other.gameObject == iceObject)
            {
                AddIce();
            }
            else if(other.gameObject == fakeIceObject)
            {
                AddFakeIce();
            }
        }

        public void MakeSound(AudioClip sound, bool atOnce = true)
        {
            if (_audioSource != null && sound != null)
            {
                if(_audioSource.isPlaying && _audioSource.clip == sound && !atOnce) return;//ループサウンド再生中に同一のループサウンドの再生命令が来ても無視する。
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
                if(_audioSource.isPlaying && _audioSource.clip == sound) _audioSource.Stop();
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

        public void Sync(){
            RequestSerialization();
        }

        public void SyncRequest(){
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Sync");
        }
    }
}
