
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CHGLimitedUserSyncer : UdonSharpBehaviour
    {
        public ColliderHitGimmick[] LimitedUserTargetCHG;
        public ColliderHitGimmick[] LimitedIgnoreUserTargetCHG;
        [UdonSynced(UdonSyncMode.None)] public string[] LimitedUsers = new string[1]{""};
        [UdonSynced(UdonSyncMode.None)] public string[] LimitedIgnoreUsers = new string[1]{""};

        public Text feedBackText;
        public bool feedBackLimitedUserName = false;
        public bool feedBackLimitedIgnoreUserName = false;

        void OnEnable()//Start
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Sync");
            SetTarget();
        }

        public void Sync()
        {
            RequestSerialization();
        }

        public void AddLimitedUser(string displayName, bool isHitToRemove = false)
        {
            foreach (string tmp in LimitedUsers)
            {
                if (tmp != displayName)
                {
                    if (isHitToRemove) RemoveLimitedUser(displayName);
                    return;//すでに登録済み
                }
            }

            int length_tmp = LimitedUsers.Length;
            if(length_tmp != 0)
            {
                for (int i = 0; i < length_tmp; i++)
                {
                    if(LimitedUsers[i] == "")
                    {
                        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                        LimitedUsers[i] = displayName;
                        RequestSerialization();
                        return;
                    }
                }
            }

            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            string[] LimitedUsers_backup = new string[length_tmp];
            for (int i = 0; i < length_tmp; i++)
            {
                LimitedUsers_backup[i] = LimitedUsers[i];
            }
            LimitedUsers = new string[length_tmp + 1];
            for (int i = 0; i < LimitedUsers_backup.Length; i++)
            {
                LimitedUsers[i] = LimitedUsers_backup[i];
            }
            LimitedUsers[length_tmp - 1] = displayName;
            RequestSerialization();
            SetTarget();
        }

        public void AddLimitedIgnoreUser(string displayName, bool isHitToRemove = false)
        {
            foreach (string tmp in LimitedIgnoreUsers)
            {
                if (tmp != displayName)
                {
                    if (isHitToRemove) RemoveLimitedIgnoreUser(displayName);
                    return;//すでに登録済み
                }
            }

            int length_tmp = LimitedIgnoreUsers.Length;
            if (length_tmp != 0)
            {
                for (int i = 0; i < length_tmp; i++)
                {
                    if (LimitedIgnoreUsers[i] == "")
                    {
                        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                        LimitedIgnoreUsers[i] = displayName;
                        RequestSerialization();
                        return;
                    }
                }
            }

            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            string[] LimitedUsers_backup = new string[length_tmp];
            for (int i = 0; i < length_tmp; i++)
            {
                LimitedUsers_backup[i] = LimitedIgnoreUsers[i];
            }
            LimitedIgnoreUsers = new string[length_tmp + 1];
            for (int i = 0; i < LimitedUsers_backup.Length; i++)
            {
                LimitedIgnoreUsers[i] = LimitedUsers_backup[i];
            }
            LimitedIgnoreUsers[length_tmp - 1] = displayName;
            RequestSerialization();
            SetTarget();
        }

        public void UpdateLimitedUser(string displayName, int index = 0)
        {
            if (LimitedUsers.Length < index + 1) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            LimitedUsers[index] = displayName;
            RequestSerialization();
            SetTarget();
        }

        public void UpdateLimitedIgnoreUser(string displayName, int index = 0)
        {
            if (LimitedIgnoreUsers.Length < index+1) return;
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            LimitedIgnoreUsers[index] = displayName;
            RequestSerialization();
            SetTarget();            
        }

        public void AddOrRemoveLocalUserToLimitedUser()
        {
            AddLimitedUser(Networking.LocalPlayer.displayName, true);
        }

        public void AddOrRemoveLocalUserToLimitedIgnoreUser()
        {
            AddLimitedIgnoreUser(Networking.LocalPlayer.displayName, true);
        }

        public void AddOrRemoveLocalUser()
        {
            AddOrRemoveLocalUserToLimitedUser();
            AddOrRemoveLocalUserToLimitedIgnoreUser();
        }

        public void UpdateLocalUserToLimitedUserIndexZero()
        {
            UpdateLimitedUser(Networking.LocalPlayer.displayName);
        }

        public void UpdateLocalUserToLimitedIgnoreUserIndexZero()
        {
            UpdateLimitedIgnoreUser(Networking.LocalPlayer.displayName);
        }

        public void UpdateLocalUser()
        {
            UpdateLocalUserToLimitedUserIndexZero();
            UpdateLocalUserToLimitedIgnoreUserIndexZero();
        }

        public void RemoveLimitedUser(string displayName)
        {
            int length_tmp = LimitedUsers.Length;
            if (length_tmp != 0)
            {
                for (int i = 0; i < length_tmp; i++)
                {
                    if (LimitedUsers[i] == displayName)
                    {
                        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                        LimitedUsers[i] = "";
                        RequestSerialization();
                        SetTarget();
                        return;
                    }
                }
            }
        }

        public void RemoveLimitedIgnoreUser(string displayName)
        {
            int length_tmp = LimitedIgnoreUsers.Length;
            if (length_tmp != 0)
            {
                for (int i = 0; i < length_tmp; i++)
                {
                    if (LimitedIgnoreUsers[i] == displayName)
                    {
                        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                        LimitedIgnoreUsers[i] = "";
                        RequestSerialization();
                        SetTarget();
                        return;
                    }
                }
            }
        }

        public void RemoveLocalUser()
        {
            RemoveLimitedUser(Networking.LocalPlayer.displayName);
            RemoveLimitedIgnoreUser(Networking.LocalPlayer.displayName);
        }

        public string GetLimitedUser(int index = 0)
        {
            if(LimitedUsers.Length > index)
            {
                return LimitedUsers[index];
            }
            return "";
        }

        public string GetLimitedIgnoreUser(int index = 0)
        {
            if (LimitedIgnoreUsers.Length > index)
            {
                return LimitedIgnoreUsers[index];
            }
            return "";
        }

        public override void OnDeserialization()
        {
            SetTarget();
        }

        public void SetTarget()
        {
            if (feedBackText != null)
            {
                feedBackText.text = "";
                if (feedBackLimitedUserName)
                {
                    foreach (string tmp in LimitedUsers)
                    {
                        if (tmp != "")
                        {
                            feedBackText.text += tmp + "\n";
                        }
                    }
                }
                feedBackText.text += "\n";
                if (feedBackLimitedIgnoreUserName)
                {
                    foreach (string tmp in LimitedIgnoreUsers)
                    {
                        if (tmp != "")
                        {
                            feedBackText.text += tmp + "\n";
                        }
                    }
                }
            }
            foreach (ColliderHitGimmick tmp in LimitedUserTargetCHG)
            {
                if (tmp != null)
                {
                    tmp.LimitedUsers = LimitedUsers;
                }
            }

            foreach (ColliderHitGimmick tmp in LimitedIgnoreUserTargetCHG)
            {
                if (tmp != null)
                {
                    tmp.LimitedIgnoreUsers = LimitedIgnoreUsers;
                }
            }
        }
    }
}
