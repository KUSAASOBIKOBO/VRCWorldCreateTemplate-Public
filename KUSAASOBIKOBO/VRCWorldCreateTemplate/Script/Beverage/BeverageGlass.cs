
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace KUSAASOBIKOBO
{
    /*
    LiquidShaderおよびそのシェーダーコントロールに関するソースコードはbitshiftprogrammer様考案によるロジックを利用しております。
    下記、bitshiftprogrammer様のソースコードより転載

    Please do support www.bitshiftprogrammer.com by joining the facebook page : fb.com/BitshiftProgrammer
    Legal Stuff:
    This code is free to use no restrictions but attribution would be appreciated.
    Any damage caused either partly or completly due to usage this stuff is not my responsibility*/

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BeverageGlass : UdonSharpBehaviour
    {
        //DrinkStatus
        public float surface_Full;
        public float surface_Empty;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectSerface))] public float surface_Now;

        //ActionStatus
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIsHold))] private bool isHold;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIsUse))] public bool isUse;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIsHot))] public bool isHot;
        public bool isPouring;
        private GameObject pouringObject;
        public float drinkingSpeed = 0.002f;
        public float pouringSpeed = 0.004f;

        //LiquidStatus
        public Renderer rend;
        public float MaxWobble = 0.1f;
        public float WobbleSpeed = 10.0f;
        public float RecoveryRate = 1.55f;
        Vector3 prevPos;
        Vector3 prevRot;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;

        //DrinkType
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectIndex))] public int index = 0;
        public BeverageList _beverageList;

        //AssociatedObjects
        public BoxCollider hitbox;
        public ParticleSystem particle;
        public ParticleSystem steamParticle;
        public Text infomationText;
        public float LiquidParticleConcentration = 3.0f;
        public float LiquidParticleAlpha = 255.0f;

        //Sound
        public AudioSource _audioSource;
        public AudioClip drinkingSound;
        public AudioClip pickingupSound;
        public AudioClip puttingSound;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectColor))] public Color color;

        public GameObject infomationBoard;
        public GameObject infomationBoardPos;

        private bool isExecutedThisFrame_DrinkingOwner = false;
        private bool isExecutedThisFrame_PouringOwner = false;

        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectPosition))] private Vector3 position;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectRotation))] private Quaternion localRotation;
        // [UdonSynced(UdonSyncMode.None)] private byte pickupHand; //0:NONE, 1:LEFT, 2:RIGHT
        // [UdonSynced(UdonSyncMode.None)] private int pickupPlayerId;

        void Start()
        {
            SyncRequest();
            if(index < 0)
            {
                SetMyColor();
            }
            else
            {
                SetBeverageColor();
            }
            rend.material.SetFloat("_FillAmount", surface_Now);
            if(infomationText != null) _beverageList.SetBeverageName(index, infomationText);
        }

        void LateUpdate()
        {
            //Update実行中のプレイヤー数分だけリクエストが来るため、1フレームに何度も処理しないようにフラグ処理する。
            //UpdateにOwnerを埋めない理由はactiveInHierarchyがFalseのオブジェクトにアタッチされたスクリプトでUpdateが実行されないため、OwnerがactiveInHierarchyをFalseにしていた場合実行されなくなってしまうから。
            isExecutedThisFrame_DrinkingOwner = false;
            isExecutedThisFrame_PouringOwner = false;
        }

        void Update()
        {
            if(rend == null) return;

            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject) && isHold)
            {
                bool changed = false;
                if(position != this.gameObject.transform.position)
                {
                    changed = true;
                    position = this.gameObject.transform.position;
                } 
                if(localRotation != this.gameObject.transform.rotation)
                {
                    changed = true;
                    localRotation = this.gameObject.transform.rotation;
                } 
                if(changed) RequestSerialization();
            }

            //実験的なコード（対象プレイヤーのオフセット付きハンド位置に同期する処理）
            // if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
            // {
            //     if(!isHold)
            //     {
            //         pickupHand = 0;
            //         pickupPlayerId = 0;
            //         RequestSerialization();
            //     }
            //     if(pickupHand != 0 && localRotation != this.gameObject.transform.rotation)
            //     {
            //         localRotation = this.gameObject.transform.rotation;
            //         RequestSerialization();
            //     } 
            // }
            // else
            // {
            //     if(pickupHand != 0)
            //     {
            //         VRCPlayerApi tmp_player = VRCPlayerApi.GetPlayerById(pickupPlayerId);
            //         if(tmp_player != null)
            //         {
            //             VRC_Pickup tmp_hand;
            //             switch(pickupHand)
            //             {
            //                 case 1:
            //                     tmp_hand = tmp_player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            //                     if(tmp_hand != null)
            //                     {
            //                         this.gameObject.transform.position = new Vector3(tmp_hand.gameObject.transform.position.x+position.x,tmp_hand.gameObject.transform.position.y+position.y,tmp_hand.gameObject.transform.position.z+position.z);
            //                     }
                                
            //                     break;

            //                 case 2:
            //                     tmp_hand = tmp_player.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            //                     if(tmp_hand != null)
            //                     {
            //                         this.gameObject.transform.position = new Vector3(tmp_hand.gameObject.transform.position.x+position.x,tmp_hand.gameObject.transform.position.y+position.y,tmp_hand.gameObject.transform.position.z+position.z);
            //                     }
            //                     break;
            //             }
            //         }
            //     }
            // }

            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                DrinkingOwner();
            }

            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                PouringOwner();
            }
            // else
            // {
            //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "PouringOwner");
            // }

            if(isHold)
            {
                // decreases the wobble over time
                wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * RecoveryRate);
                wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * RecoveryRate);

                // make a sine wave of the decreasing wobble
                float wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(WobbleSpeed * Time.time);
                float wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(WobbleSpeed * Time.time);

                // send it to the shader
                rend.material.SetFloat("_WobbleX", wobbleAmountX);
                rend.material.SetFloat("_WobbleZ", wobbleAmountZ);

                // Move Speed
                Vector3 moveSpeed = (prevPos - transform.position) / Time.deltaTime;
                Vector3 rotationDelta = transform.rotation.eulerAngles - prevRot;

                // add clamped speed to wobble
                wobbleAmountToAddX += Mathf.Clamp((moveSpeed.x + (rotationDelta.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
                wobbleAmountToAddZ += Mathf.Clamp((moveSpeed.z + (rotationDelta.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

                // save the last position
                prevPos = transform.position;
                prevRot = transform.rotation.eulerAngles;
            }

            if(particle != null){
                if(isUse)
                {
                    if(!IsEmpty()){
                        MakeSound(drinkingSound,false);
                        particle.gameObject.SetActive(true);
                    }else{
                        StopSound(drinkingSound);
                        particle.gameObject.SetActive(false); 
                    }
                }else if(particle.gameObject.activeSelf)
                {
                    StopSound(drinkingSound);
                    particle.gameObject.SetActive(false); 
                } 
            }
        }

        public void DrinkingOwner()
        {
            if(isExecutedThisFrame_DrinkingOwner) return;
            isExecutedThisFrame_DrinkingOwner = true;
            if(isUse)
            {
                if(!IsEmpty()){
                    surface_Now += drinkingSpeed * Time.deltaTime;
                    RequestSerialization();
                    rend.material.SetFloat("_FillAmount", surface_Now);           
                }
            }
        }

        public void PouringOwner()
        {
            if(isPouring)
            {
                if(isExecutedThisFrame_PouringOwner) return;
                isExecutedThisFrame_PouringOwner = true;
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

                if(_beverageList != null)
                {
                    if(pouringObject != null && (particle == null || pouringObject != particle.gameObject)){
                        if(_beverageList.shakerParticle.Length == _beverageList._beverageShaker.Length)
                        {
                            for(int i=0;i<_beverageList.shakerParticle.Length;i++)
                            {
                                if(_beverageList.shakerParticle[i].gameObject == pouringObject)
                                {
                                    if(_beverageList._beverageShaker[i]._beverageGlass != null) index = _beverageList._beverageShaker[i]._beverageGlass.index;
                                    else index = -1;
                                    SetCocktailColorOwner(i);
                                }
                            }
                        }

                        if(_beverageList.bottleParticle.Length == _beverageList._beverageBottle.Length)
                        {
                            for(int i=0;i<_beverageList.bottleParticle.Length;i++)
                            {
                                if(_beverageList.bottleParticle[i].gameObject == pouringObject)
                                {
                                    index = _beverageList._beverageBottle[i].index;
                                    SetBeverageColor();
                                }
                            } 
                        }

                        if(!IsFull()){
                            if(infomationText != null) _beverageList.SetBeverageName(index, infomationText);
                            {
                                surface_Now -= pouringSpeed * Time.deltaTime;
                            }
                            rend.material.SetFloat("_FillAmount", surface_Now);           
                        }
                    }
                }
            }
            isPouring = false;
            RequestSerialization();
        }

        public Vector3 ReflectPosition
        {
            get => position;

            set
            {
                position = value;
                this.gameObject.transform.position = position;
            }
        }

        public Quaternion ReflectRotation
        {
            get => localRotation;

            set
            {
                localRotation = value;
                this.gameObject.transform.localRotation = localRotation;
            }
        }

        public float ReflectSerface
        {
            get => surface_Now;

            set
            {
                surface_Now = value;
                rend.material.SetFloat("_FillAmount", surface_Now);
            }
        }

        public int ReflectIndex
        {
            get => index;

            set
            {
                index = value;
                if(_beverageList != null && infomationText != null) _beverageList.SetBeverageName(index, infomationText);
            }
        }

        public Color ReflectColor
        {
            get => color;

            set
            {
                color = value;
                SetMyColor();
            }
        }

        public bool ReflectIsHold
        {
            get => isHold;

            set
            {
                isHold = value;
                if(rend == null) return;
                if(!isHold){
                    rend.material.SetFloat("_WobbleX", 0.0f);
                    rend.material.SetFloat("_WobbleZ", 0.0f);
                }
                if(infomationBoard != null && infomationBoardPos != null) infomationBoard.transform.position = infomationBoardPos.transform.position;
            }
        }

        public bool ReflectIsUse
        {
            get => isUse;

            set
            {
                isUse = value;
            }
        }

        public bool ReflectIsHot
        {
            get => isHot;

            set
            {
                isHot = value;
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

        public void MakeSound_puttingSound()
        {
            MakeSound(puttingSound);
        }

        public void MakeSound_pickingupSound()
        {
            MakeSound(pickingupSound);
        }

        public void SetMyColor()
        {
            if(rend == null) return;
            if(_beverageList != null){
                rend.material.SetColor("_Colour", color);
                rend.material.SetColor("_TopColor", new Color(color.r + _beverageList.beverageTopColorDifference.x/255.0f,color.g + _beverageList.beverageTopColorDifference.y/255.0f,color.b + _beverageList.beverageTopColorDifference.z/255.0f,color.a));
                rend.material.SetColor("_FoamColor", new Color(color.r + _beverageList.beverageFoamColorDifference.x/255.0f,color.g + _beverageList.beverageFoamColorDifference.y/255.0f,color.b + _beverageList.beverageFoamColorDifference.z/255.0f,color.a));
                rend.material.SetColor("_RimColor", new Color(color.r + _beverageList.beverageRimColorDifference.x/255.0f,color.g + _beverageList.beverageRimColorDifference.y/255.0f,color.b + _beverageList.beverageRimColorDifference.z/255.0f,color.a));
            }
            if(particle != null){
                ParticleSystem.MainModule psmain = particle.main;
                psmain.startColor = new Color(color.r/LiquidParticleConcentration, color.g/LiquidParticleConcentration, color.b/LiquidParticleConcentration, LiquidParticleAlpha/255.0f);
            } 
            if(_beverageList != null && infomationText != null) _beverageList.SetBeverageName(index, infomationText);
        }

        public void SetBeverageColor()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                SetBeverageColorOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                SetBeverageColorOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SetBeverageColorOwner");
            }
        }

        public void SetBeverageColorOwner()
        {
            if(rend == null) return;
            if(index < 0) return;
            if(_beverageList != null) _beverageList.SetBeverageColor(index, rend.material);
            color = rend.material.GetColor("_Colour");
            RequestSerialization();

            if(particle != null){
                ParticleSystem.MainModule psmain = particle.main;
                psmain.startColor = new Color(color.r/LiquidParticleConcentration, color.g/LiquidParticleConcentration, color.b/LiquidParticleConcentration, LiquidParticleAlpha/255.0f);
            } 
            if(_beverageList != null && infomationText != null) _beverageList.SetBeverageName(index, infomationText);
        }


        public void SetCocktailColorOwner(int shakerIndex)
        { 
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

            if(rend == null) return;
            if(_beverageList != null) _beverageList.SetCocktailColor(shakerIndex, rend.material);
            color = rend.material.GetColor("_Colour");
            RequestSerialization();

            if(particle != null){
                ParticleSystem.MainModule psmain = particle.main;
                psmain.startColor = new Color(color.r/LiquidParticleConcentration, color.g/LiquidParticleConcentration, color.b/LiquidParticleConcentration, LiquidParticleAlpha/255.0f);
            } 
            if(_beverageList != null && infomationText != null) _beverageList.SetBeverageName(index, infomationText);
        }

        public void SetColor(Color externalColor) //原則オーナーが呼び出す必要があります。
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            color = externalColor;
            RequestSerialization();
        }  

        public void SetIndex(int externalIndex) //原則オーナーが呼び出す必要があります。
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            index = externalIndex;
            RequestSerialization();
        }

        public void StartisHoldOwner()
        {
            isHold = true;
            RequestSerialization();
        }

        public void StopisHoldOwner()
        {
            isHold = false;
            RequestSerialization();
            if(rend == null) return;
            rend.material.SetFloat("_WobbleX", 0.0f);
            rend.material.SetFloat("_WobbleZ", 0.0f);
            if(infomationBoard != null && infomationBoardPos != null) infomationBoard.transform.position = infomationBoardPos.transform.position;
        }  

        public override void OnPickup()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_pickingupSound");
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StartisHoldOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StartisHoldOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StartisHoldOwner");
            }

            // Vector3 playerPos_tmp = new Vector3(0.0f, 0.0f, 0.0f);
            // VRC_Pickup pickupLeftHand = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            // VRC_Pickup pickupRightHand = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
            // if(pickupLeftHand != null && pickupLeftHand.gameObject == this.gameObject)
            // {
            //     pickupHand = 1;
            //     pickupPlayerId = Networking.LocalPlayer.playerId;
            //     playerPos_tmp = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            // }else if(pickupLeftHand != null && pickupLeftHand.gameObject == this.gameObject)
            // {
            //     pickupHand = 2;
            //     pickupPlayerId = Networking.LocalPlayer.playerId;
            //     playerPos_tmp = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            // }else
            // {
            //     pickupHand = 0;
            //     pickupPlayerId = 0;
            // } 
            // if(pickupHand != 0)
            // {
            //     position = new Vector3(this.gameObject.transform.position.x-playerPos_tmp.x, this.gameObject.transform.position.y-playerPos_tmp.y, this.gameObject.transform.position.z-playerPos_tmp.z);
            // }
            // RequestSerialization();

        }

        public override void OnDrop()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_puttingSound");
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StopisHoldOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StopisHoldOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StopisHoldOwner");
            }
            // pickupHand = 0;
            // pickupPlayerId = 0;
            // RequestSerialization();
        }

        public override void OnPickupUseDown()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StartisUseOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StartisUseOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StartisUseOwner");
            }
        }

        public void StartisUseOwner()
        {
            isUse = true;
            RequestSerialization();
        }

        public void StopisUseOwner()
        {
            isUse = false;
            RequestSerialization();
        }

        public override void OnPickupUseUp()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StopisUseOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StopisUseOwner();   
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "StopisUseOwner");
            }
        }

        public void StartPouringOwner()
        {
            isPouring = true;
        }
        
        public void StopPouringOwner()
        {
            isPouring = false;
        }

        private void OnParticleCollision(GameObject other)
        {
            if(hitbox == null || !hitbox.gameObject.activeSelf) return;
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                StartPouringOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                StartPouringOwner();
            }
            pouringObject = other;
        }

        public bool IsEmpty()
        {
            if(surface_Now > surface_Empty) return true;
            else return false;
        }

        public bool IsFull()
        {
            if(surface_Now < surface_Full) return true;
            else return false;
        }

        public void ForcedEmpty()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)){
                ForcedEmptyOwner();
            }
            else
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                ForcedEmptyOwner();
                //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ForcedEmptyOwner");
            }
        }

        public void ForcedEmptyOwner()
        {
            surface_Now = surface_Empty;
            RequestSerialization();
            if(rend != null) rend.material.SetFloat("_FillAmount", surface_Now);
        }

        public void Sync(){
            RequestSerialization();
        }

        public void SyncRequest(){
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Sync");
        }

    }
}
