
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmartPickup : UdonSharpBehaviour
    {
        //GlobalValue
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectUsingUserId))] private int usingUserId = 0;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectHoldingUserId))] private int holdingUserId = 0;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectHandType))] private byte handType = 0;//1:left, 2:right
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectPickupOffset))] private Vector3 offset;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectPickupRotation))] private Quaternion rotation;
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(ReflectPosition))] private Vector3 position;

        //LocalValue
        private bool isHolding = false;
        private bool isUsing = false;
        [Header("つかめる距離(中心からメートル単位)")] public float distance = 0.2f;
        private Quaternion rotationOffset;
        private Vector3 localOffset;

        public int ReflectUsingUserId
        {
            get => usingUserId;

            set
            {
                usingUserId = value;
            }
        }

        public int ReflectHoldingUserId
        {
            get => holdingUserId;

            set
            {
                holdingUserId = value;
                if(holdingUserId == 0) this.gameObject.transform.position = position;
                // else if(GetisHolding())
                // {
                //    FixOffset(position);
                // }
            }
        }

        public byte ReflectHandType
        {
            get => handType;

            set
            {
                handType = value;
            }
        }

        public Vector3 ReflectPickupOffset
        {
            get => offset;

            set
            {
                offset = value;
                // if(GetisHolding())
                // {
                //    FixOffset(position);
                // }
            }
        }

        public Vector3 ReflectPosition
        {
            get => position;

            set
            {
                position = value;
                if(GetisHolding())
                {
                   //FixOffset(position);
                }
                else
                {
                    this.gameObject.transform.position = position;
                }
                
            }
        }

        public Quaternion ReflectPickupRotation
        {
            get => rotation;

            set
            {
                rotation = value;
                this.gameObject.transform.localRotation = rotation;
            }
        }

        void Start()
        {
            SyncRequest();
        }

        private void FixOffset(Vector3 truePosition)
        {
            if(holdingUserId != 0)
            {
                if(holdingUserId == Networking.LocalPlayer.playerId) return;
                VRCPlayerApi tmp_player = VRCPlayerApi.GetPlayerById(holdingUserId);
                if(tmp_player != null)
                {
                    Vector3 handPosition;
                    if(handType == 1) handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    else if(handType == 2) handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    else
                    {
                        Debug.Log("Can not get handType.");
                        return;
                    }

                    if(handType == 1){
                        handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    } 
                    else if(handType == 2)
                    {
                        handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    } 
                    else
                    {
                        Debug.Log("Can not get handType.");
                        return;
                    }
                    localOffset = new Vector3(truePosition.x - (handPosition.x+offset.x), truePosition.y - (handPosition.y+offset.y), truePosition.z - (handPosition.z+offset.z));
                }
            }
        }

        void Update()
        {
            if(holdingUserId != 0)
            {
                VRCPlayerApi tmp_player = VRCPlayerApi.GetPlayerById(holdingUserId);
                if(tmp_player != null)
                {
                    Vector3 handPosition;
                    if(handType == 1) handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    else if(handType == 2) handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    else
                    {
                        Debug.Log("Can not get handType.");
                        return;
                    }

                    if(handType == 1){
                        handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    } 
                    else if(handType == 2)
                    {
                        handPosition = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    } 
                    else
                    {
                        Debug.Log("Can not get handType.");
                        return;
                    }
                    if(holdingUserId != Networking.LocalPlayer.playerId) this.gameObject.transform.position = new Vector3(handPosition.x+offset.x/*+localOffset.x*/, handPosition.y+offset.y/*+localOffset.y*/, handPosition.z+offset.z/*+localOffset.z*/);
                    else this.gameObject.transform.position = new Vector3(handPosition.x, handPosition.y, handPosition.z);

                    //自分がつかんでいるならオブジェクトの回転角を報告
                    if(holdingUserId == Networking.LocalPlayer.playerId)
                    {
                        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                        Quaternion rot_tmp;
                        if(handType == 1){
                            rot_tmp = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                        } 
                        else if(handType == 2)
                        {
                            rot_tmp = tmp_player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                            Debug.Log(rot_tmp);
                        } 
                        else
                        {
                            Debug.Log("Can not get handType.");
                            return;
                        }
                        rot_tmp = Quaternion.Euler(rot_tmp.eulerAngles.x + rotationOffset.eulerAngles.x, rot_tmp.eulerAngles.y + rotationOffset.eulerAngles.y, rot_tmp.eulerAngles.z + rotationOffset.eulerAngles.z);
                        this.gameObject.transform.localRotation = rot_tmp;
                        rotation = this.gameObject.transform.localRotation;
                        //position = this.gameObject.transform.position;
                        Sync();
                    }
                }
            }
        }

        public void PickupUseDown()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            holdingUserId = Networking.LocalPlayer.playerId;
            Sync();
        }

        public void PickupUseUp()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            handType = 0;
            holdingUserId = 0;
            Sync();
        }

        public void Pickup(VRC.Udon.Common.HandType _handtype, Vector3 objectPos, Vector3 handPos)
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            Vector3 BonePos;
            if(_handtype == VRC.Udon.Common.HandType.LEFT)
            {
                BonePos = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand);
                handType = 1;
            } 
            else if(_handtype == VRC.Udon.Common.HandType.RIGHT)
            {
                BonePos = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightHand);
                handType = 2;
            }else{
                BonePos = new Vector3(0.0f, 0.0f, 0.0f);
            }
            holdingUserId = Networking.LocalPlayer.playerId;
            offset = new Vector3(handPos.x - BonePos.x, handPos.y - BonePos.y, handPos.z - BonePos.z);
            position = this.gameObject.transform.position;
            Sync();
        }

        public void Drop()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            handType = 0;
            holdingUserId = 0;
            position = this.gameObject.transform.position;
            Sync();
        }

        public float CompareDistance(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(pos1, pos2);
        }

        public bool isInRange(float value, float range)
        {
            if(value <= range) return true;
            return false;
        }

        public override void InputGrab(bool value, UdonInputEventArgs args)
        { 
            if(value)
            {
                if(isHolding) return;
                isHolding = true;       //Grab入力した瞬間のフレームで街頭距離内になければピックアップしない

                Vector3 objectPosition = this.gameObject.transform.position;
                Quaternion objectRotation = this.gameObject.transform.rotation;
                Vector3 handPosition;
                VRC.Udon.Common.HandType _handtype = args.handType;

                if(_handtype == VRC.Udon.Common.HandType.LEFT){
                    handPosition = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    rotationOffset = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                    rotationOffset = Quaternion.Euler(objectRotation.eulerAngles.x - rotationOffset.eulerAngles.x, objectRotation.eulerAngles.y - rotationOffset.eulerAngles.y, objectRotation.eulerAngles.z - rotationOffset.eulerAngles.z);
                } 
                else if(_handtype == VRC.Udon.Common.HandType.RIGHT)
                {
                    handPosition = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    rotationOffset = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                    rotationOffset = Quaternion.Euler(objectRotation.eulerAngles.x - rotationOffset.eulerAngles.x, objectRotation.eulerAngles.y - rotationOffset.eulerAngles.y, objectRotation.eulerAngles.z - rotationOffset.eulerAngles.z);
                } 
                else
                {
                    Debug.Log("Can not get handType.");
                    return;
                }
                if(!isInRange(CompareDistance(objectPosition, handPosition), distance)) return;

                /*コントローラを振動させる*/
                Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, 1, 1);

                Pickup(_handtype, objectPosition, handPosition);
            }
            else
            {
                isHolding = false;
            }
        }
        public override void InputDrop(bool value, UdonInputEventArgs args)
        {
            if(value)
            {
                if(holdingUserId == Networking.LocalPlayer.playerId) Drop();
                if(usingUserId == Networking.LocalPlayer.playerId) PickupUseUp();
            }                
        }

        public void ForceDrop()
        {
            if(holdingUserId == Networking.LocalPlayer.playerId) Drop();
            if(usingUserId == Networking.LocalPlayer.playerId) PickupUseUp();
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if(value)
            {
                if(isUsing) return;
                isUsing = true;
                if(holdingUserId == Networking.LocalPlayer.playerId) PickupUseDown();
            }
            else
            {
                isUsing  = false;
                if(usingUserId == Networking.LocalPlayer.playerId) PickupUseUp();
            }
        }

        public bool GetisHolding()
        {
            if(holdingUserId != 0) return true;
            return false;
        }

        public bool GetisUsing()
        {
            if(usingUserId != 0) return true;
            return false;
        }

        public bool GetisHoldingLocal()
        {
            if(holdingUserId == Networking.LocalPlayer.playerId) return true;
            return false;
        }

        public bool GetisUsingLocal()
        {
            if(usingUserId == Networking.LocalPlayer.playerId) return true;
            return false;
        }

        public void Sync(){
            RequestSerialization();
        }

        public void SyncRequest(){
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Sync");
        }
    }
}
