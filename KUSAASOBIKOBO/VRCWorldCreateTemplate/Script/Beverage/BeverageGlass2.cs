
using UdonSharp;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)] //VRCObjectSyncを利用する想定のためContinuousで設定
    public class BeverageGlass2 : UdonSharpBehaviour
    {
        //DrinkType
        public int index = 0;
        private int lastIndex;
        public BeverageList2 _beverageList;
        public BeverageGlass2Syncer _beverageGlass2Syncer;

        //DrinkStatus
        public float surface_Full = 0.0f;
        public float surface_Empty = 1.0f;
        public float surface_Now = 1.0f;
        //private float Lastsurface_Now = 0.0f;

        //ActionStatus
        private bool isUse;
        private bool isHold;
        public bool isHot;
        private bool lastIsHot;
        public float drinkingSpeed = 0.005f;
        public float pouringSpeed = 0.005f;

        //LiquidStatus
        public Renderer rend;
        public float MaxWobble = 0.1f;
        public float WobbleSpeed = 10.0f;
        public float RecoveryRate = 1.55f;
        Vector3 prevPos;
        Vector3 prevRot;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;

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

        public Color color;
        public Vector4 lastColorParam;

        public GameObject infomationBoard;
        public GameObject infomationBoardPos;

        private bool isExecutedThisFrame_DrinkingOwner = false;
        private bool isExecutedThisFrame_PouringOwner = false;

        //Extend
        public BeverageShaker2 _thisShaker;

        private bool lastInfomationTextActive;

#if UNITY_EDITOR
        public void DuplicateSyncer()
        {
            if(_beverageGlass2Syncer != null)
            {
                _beverageGlass2Syncer.DuplicateSyncer(index, surface_Now, isHot);
            }
        }
#endif
        void Start()
        {
            //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "SyncRequest"); // 同期変数ではない同期が必要なデータをオーナーから同期してもらうようにリクエストする
        }

        void LateUpdate()
        {
            //1フレームに何度も処理しないようにフラグ処理する。
            //UpdateにOwnerを埋めない理由はactiveInHierarchyがFalseのオブジェクトにアタッチされたスクリプトでUpdateが実行されないため、OwnerがactiveInHierarchyをFalseにしていた場合実行されなくなってしまうから。
            isExecutedThisFrame_DrinkingOwner = false;
            isExecutedThisFrame_PouringOwner = false;
        }

        void Update()
        {
            if (rend == null) return;

            //水面変化処理
            DrinkingOwner(); //isUseをローカルで立ててるプレイヤーがsurface_Nowを更新する

            if (infomationText != null)
            {
                bool aih_tmp = infomationText.gameObject.activeInHierarchy;
                if (!lastInfomationTextActive && aih_tmp)
                {
                    SetBeverageName();
                }
                lastInfomationTextActive = aih_tmp;
            }

            //水面が揺れる処理
            if (rend.material.GetFloat("_WobbleX") != 0 || rend.material.GetFloat("_WobbleZ") != 0)
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
        }

        public void DrinkingOwner()
        {
            if (isExecutedThisFrame_DrinkingOwner) return; //1フレームでの2重実行を防止
            isExecutedThisFrame_DrinkingOwner = true;
            if (isUse)
            {
                //if (!IsEmpty()) //グラスの傾きによってエンプティを超えて減らしたいシーンもあるので1を超えてなければ水面を減らすことは許容
                //{
                if(surface_Now < 1)
                {
                    if(_beverageGlass2Syncer != null)
                    {
                        _beverageGlass2Syncer.AddSurface_Now(drinkingSpeed * Time.deltaTime);
                    }
                }
                //}
                if (IsEmpty() && particle != null && particle.gameObject.activeSelf)
                {
                    if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopDrinkingSync"); //Emptyを超えてparticleは発生できない
                }
            }
        }

        public void PouringOwner(GameObject pouringObject)
        {
            if (isExecutedThisFrame_PouringOwner) return; //いくつparticleがヒットしても処理するのは一回
            isExecutedThisFrame_PouringOwner = true;

            bool isParticleOwner = false;//particleの発信元ボトルのUse状態を調べてローカルでUseを立てていたなら更新する。この時このグラスのオーナー権が移ることに注意。
            bool hit = false;
            if (_beverageList != null)
            {
                if (pouringObject != null && (particle == null || pouringObject != particle.gameObject))
                {
                    //シェイカーのparticleに対する処理
                    if (_beverageList.shakerParticle.Length == _beverageList._beverageShaker.Length)
                    {
                        for (int i = 0; i < _beverageList.shakerParticle.Length; i++)
                        {
                            if (_beverageList.shakerParticle[i].gameObject == pouringObject)
                            {
                                if (_beverageList._beverageShaker[i]._beverageGlass != null)
                                {
                                    hit = true;
                                    isParticleOwner = _beverageList._beverageShaker[i]._beverageGlass.GetIsUse();
                                    if (isParticleOwner)
                                    {
                                        SetIndex(_beverageList._beverageShaker[i]._beverageGlass.index);
                                        SetCocktailColorOwner(i);
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    //ボトルのparticleに対する処理
                    if (!hit)
                    {
                        if (_beverageList.bottleParticle.Length == _beverageList._beverageBottle.Length)
                        {
                            for (int i = 0; i < _beverageList.bottleParticle.Length; i++)
                            {
                                if (_beverageList.bottleParticle[i].gameObject == pouringObject)
                                {
                                    isParticleOwner = _beverageList._beverageBottle[i].GetIsUse();
                                    if (isParticleOwner)
                                    {
                                        SetIndex(_beverageList._beverageBottle[i].index);
                                        SetBeverageColor();
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    //水量を増やす
                    if (isParticleOwner)
                    {
                        if (!IsFull())
                        {
                            if (infomationText != null) _beverageList.SetBeverageName(index, infomationText);
                            {
                                if (_beverageGlass2Syncer != null)
                                {
                                    if (surface_Now > surface_Empty) _beverageGlass2Syncer.SetSurface_Now(surface_Empty);
                                    else _beverageGlass2Syncer.SubtractionSurface_Now(pouringSpeed * Time.deltaTime);
                                }
                            }
                        }
                    } 
                }
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

        public void MakeSound_puttingSound()
        {
            MakeSound(puttingSound);
        }

        public void MakeSound_pickingupSound()
        {
            MakeSound(pickingupSound);
        }

        public void MakeSound_drinkingSound()
        {
            MakeSound(drinkingSound, false);
        }

        public void showParticle()
        {
            if (particle != null)
            {
                if (!IsEmpty())
                {
                    particle.gameObject.SetActive(true);
                }
                else
                {
                    particle.gameObject.SetActive(false);
                }
            }
        }

        public void hideParticle()
        {
            if (particle != null)
            {
                particle.gameObject.SetActive(false);
            }
        }

        public void StopDrinkingSync()
        {
            hideParticle();
            StopSound(drinkingSound);
        }

        public void drinkingSync()
        {
            showParticle();
            if (!IsEmpty()) MakeSound_drinkingSound();
        }

        public void SetMyColor()
        {
            if (rend == null) return;
            if (_beverageList != null)
            {
                rend.material.SetColor("_Colour", color);
                rend.material.SetColor("_TopColor", new Color(color.r + _beverageList.beverageTopColorDifference.x / 255.0f, color.g + _beverageList.beverageTopColorDifference.y / 255.0f, color.b + _beverageList.beverageTopColorDifference.z / 255.0f, color.a));
                rend.material.SetColor("_FoamColor", new Color(color.r + _beverageList.beverageFoamColorDifference.x / 255.0f, color.g + _beverageList.beverageFoamColorDifference.y / 255.0f, color.b + _beverageList.beverageFoamColorDifference.z / 255.0f, color.a));
                rend.material.SetColor("_RimColor", new Color(color.r + _beverageList.beverageRimColorDifference.x / 255.0f, color.g + _beverageList.beverageRimColorDifference.y / 255.0f, color.b + _beverageList.beverageRimColorDifference.z / 255.0f, color.a));
            }
            SetParticleColor();
            SetBeverageName();

        }

        public void SetParticleColor()
        {
            if (particle != null)
            {
                ParticleSystem.MainModule psmain = particle.main;
                psmain.startColor = new Color(color.r / LiquidParticleConcentration, color.g / LiquidParticleConcentration, color.b / LiquidParticleConcentration, LiquidParticleAlpha / 255.0f);
            }
        }

        public void SetBeverageName()
        {
            if (_beverageList != null && infomationText != null && infomationText.gameObject.activeInHierarchy) _beverageList.SetBeverageName(index, infomationText);
        }

        public void SetBeverageColor()
        {
            if (rend == null) return;
            if (index < 0) return;
            if (_beverageList != null)
            {
                _beverageList.SetBeverageColor(index, rend.material);
                //TODO:isHotの受け渡し処理
            }
            SetColor(rend.material.GetColor("_Colour"));
            SetParticleColor();
            SetBeverageName();
        }


        public void SetBeverageColorLocal()
        {
            if (rend == null) return;
            if (index < 0) return;
            if (_beverageList != null)
            {
                _beverageList.SetBeverageColor(index, rend.material);
                //TODO:isHotの受け渡し処理
            }
            color = rend.material.GetColor("_Colour");
            SetParticleColor();
            SetBeverageName();
        }

        public void SetCocktailColorOwner(int shakerIndex)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

            if (rend == null) return;
            if (_beverageList != null) _beverageList.SetCocktailColor(shakerIndex, rend.material);
            SetColor(rend.material.GetColor("_Colour"));
            SetParticleColor();
            SetBeverageName();
        }

        public void SetColor(Color externalColor)
        {
            if (_beverageGlass2Syncer != null) _beverageGlass2Syncer.SetColor(externalColor);
        }

        public void SetIndex(int externalIndex)
        {
            if (_beverageGlass2Syncer != null) _beverageGlass2Syncer.SetIndex(externalIndex);
        }

        public void SetIsHot(bool externalValue)
        {
            if (_beverageGlass2Syncer != null) _beverageGlass2Syncer.SetIsHot(externalValue);
        }

        public override void OnPickup()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_pickingupSound");
            if (infomationBoard != null && infomationBoardPos != null) infomationBoard.transform.position = infomationBoardPos.transform.position;
            isHold = true;
        }

        public override void OnDrop()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MakeSound_puttingSound");
            isUse = false;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopDrinkingSync");
            isHold = false;
        }

        public override void OnPickupUseDown()
        {
            if (_thisShaker != null && _thisShaker.bodyStatus == _thisShaker.closeBodyStatus) return; //シェイカーの容器だった場合ふたを閉めてる時に入力を無効にする
            isUse = true;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "drinkingSync");
        }
        public override void OnPickupUseUp()
        {
            if (_thisShaker != null && _thisShaker.bodyStatus == _thisShaker.closeBodyStatus) return; //シェイカーの容器だった場合ふたを閉めてる時に入力を無効にする
            isUse = false;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StopDrinkingSync");
        }

        private void OnParticleCollision(GameObject other)
        {
            if (hitbox == null || !hitbox.gameObject.activeSelf) return;

            PouringOwner(other);
        }

        public bool IsEmpty()
        {
            if (surface_Now > surface_Empty) return true;
            else return false;
        }

        public bool IsFull()
        {
            if (surface_Now < surface_Full) return true;
            else return false;
        }

        public void ForcedEmpty()
        {
            if (_beverageGlass2Syncer != null) _beverageGlass2Syncer.SetSurface_Now(1.0f);
        }

        public bool GetIsUse()
        {
            return isUse;
        }

        public bool GetIsHold()
        {
            return isHold;
        }

        /*public void Sync()
        {
           
        }

        public void SyncRequest()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Sync");
        }*/

    }
#if UNITY_EDITOR
    [CustomEditor(typeof(BeverageGlass2))]//拡張するクラスを指定
    public class BeverageGlass2DuplicateSyncer : Editor
    {
        public override void OnInspectorGUI()
        {
            BeverageGlass2 _beverageGlass2 = target as BeverageGlass2;

            //ボタンを表示
            if (GUILayout.Button("初期値を確定"))
            {
                _beverageGlass2.DuplicateSyncer();
            }
            base.OnInspectorGUI();
        }

    }
#endif
}