
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerDatabase : UdonSharpBehaviour
    {
        [NonSerialized] public int[] playerIdList = new int[80] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }; //この配列のindexがPlayerのindexになります。
        [NonSerialized] public string[] displayNameList = new string[80] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
        [NonSerialized] public int playerNum = 1;
        [NonSerialized] public VRCPlayerApi[] players = new VRCPlayerApi[80];

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            RefreshList(player, true);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            RefreshList(player, false);
        }

        private void RefreshList(VRCPlayerApi ignorePlayer, bool forceFlag)
        {
            for (int i = 0; i < players.Length; i++) players[i] = null;
            VRCPlayerApi.GetPlayers(players);
            for (int i = 0; i < playerIdList.Length; i++) playerIdList[i] = -1;
            for (int i = 0; i < displayNameList.Length; i++) displayNameList[i] = "";
            int tmpIndex = 0;
            foreach (VRCPlayerApi tmp in players)
            {
                if (tmp != null)
                {
                    if (forceFlag || (ignorePlayer != null && tmp.playerId != ignorePlayer.playerId))
                    {
                        playerIdList[tmpIndex] = tmp.playerId;
                        tmpIndex++;
                    }
                }
            }

            //プレイヤー人数を保存
            if (tmpIndex != 0) playerNum = tmpIndex;

            /* playerIdListの-1以外の部分を昇順にソート */
        int playerId_tmp = -1;
            for (int i = 0; i < playerIdList.Length; ++i)
            {
                if (playerIdList[i] < 0) break;
                for (int j = i + 1; j < playerIdList.Length; ++j)
                {
                    if (playerIdList[j] < 0) break;
                    if (playerIdList[i] > playerIdList[j])
                    {
                        playerId_tmp = playerIdList[i];
                        playerIdList[i] = playerIdList[j];
                        playerIdList[j] = playerId_tmp;
                    }
                }
            }

            tmpIndex = 0;
            foreach (int tmp in playerIdList)
            {
                if (tmp > 0)
                {
                    displayNameList[tmpIndex] = VRCPlayerApi.GetPlayerById(tmp).displayName;
                    tmpIndex++;
                }
            }
        }

        public int GetMyIndex() //自分のindexを返します
        {
            return GetPlayerIndexFromPlayerId(Networking.LocalPlayer.playerId);
        }

        public int GetPlayerIdFromIndex(int index) ///indexからplayerIdに変換します。不正値の場合は-1で返します。
        {
            if (index >= 80) return -1;//playerIndexの最大値は79です。
            return playerIdList[index];
        }

        public int GetPlayerIndexFromPlayerId(int playerId) ///playerIdからplayerIndexに変換します。不正値の場合-1で返します。
        {
            int length_tmp = playerIdList.Length;
            int indexTmp = -1;
            for (int i = 0; i < length_tmp; i++)
            {
                if (playerIdList[i] == playerId)
                {
                    indexTmp = i;
                    break;
                }
            }
            return indexTmp;
        }
    }
}
