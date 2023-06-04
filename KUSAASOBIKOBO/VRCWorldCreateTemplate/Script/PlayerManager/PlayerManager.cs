
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    /*＜説明＞
     * 送信者と受信者を特定して任意のスクリプトの任意のメソッドを実行させることができます。
     * さらに、メソッドの実行依頼を受信したプレイヤーが最大80件までスタックしておくことができます。
     * 即時実行されないため、自分の処理が終わってから依頼されたメソッドの実行を行いたい実装にも利用可能です。
     * 
     * 任意のスクリプトで特定のプレイヤーに対してのみ任意のメソッドを実行させることもできます。（isToMeOnly）
     * 実行依頼を受けたプレイヤーは誰から依頼が送信されたのかをplayerIdで知ることができます。
     * 全員に送信して各プレイヤーのローカルでplayerIdを対象に処理を実行することも可能です。
     */
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerManager : UdonSharpBehaviour
    {
        private int[] requestListSendPlayerIndex = new int[80] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        private int[] requestListReceivePlayerIndex = new int[80] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        private int maxRequestNum = 80;//最大同時リクエスト受注数（上のリクエストリスト配列の個数を入れる）
        /*MEMO:UdonではList型が使えないため配列をあらかじめ用意して自前のaddとremoveを用意して処理しています。*/

        [Header("プレイヤーデータベース（これがないと動きません）")] public PlayerDatabase PlayerDB;
        [Header("レシーブプレイヤー(設定しているワールド人数の倍の数必要です。最大80)")] public Player[] _player; 
        [Header("受け取ったプレイヤーを記録しない")] public bool isIgnoreSaveReceivePlayerIndex = false;
        [Header("送ったプレイヤーを記録しない")] public bool isIgnoreSaveSendPlayerIndex = false;
        [Header("自分が送ったときは記録しない")] public bool isIgnoreMyRequest = false;
        [Header("自分に送られたときだけ記録する")] public bool isToMeOnly = false;
        [Header("連続した同一のリクエストを除外する")] public bool isIgnoreMultipleRequest = false;

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        void Start()
        {
            int index_tmp = 0;
            foreach (Player tmp in _player)
            {
                if(tmp != null) tmp.index = index_tmp;
                index_tmp++;
            }
        }

        private void AddRequestList(int[] requestList, int value)
        {
            if (DebugText != null) DebugText.text += "\nAddRequestList:Start";
            if (requestList[maxRequestNum-1] != -1) return; //受注可能最大リクエスト数をオーバーしているなら登録をしない
            for(int i = maxRequestNum-1; i >= 1 ; i--)
            {
                requestList[i] = requestList[i - 1];
            }
            requestList[0] = value;
            if (DebugText != null) DebugText.text += "\nAddRequestList:AddValue:" + requestList[0];
        }

        private void RemoveRequestList(int[] requestList)
        {
            if (DebugText != null) DebugText.text += "\nRemoveRequestList:Start RemoveValue:" + requestList[0];
            for (int i = 1; i < maxRequestNum; i++)
            {
                requestList[i - 1] = requestList[i];
            }
            requestList[maxRequestNum - 1] = -1; //最大リクエスト数に達している場合requestList[maxRequestNum - 1]は更新されないので-1で2つ残らないように上書き
            if (DebugText != null) DebugText.text += "\nRemoveRequestList:Finish";
        }

        private void ClearRequestList(int[] requestList)
        {
            for (int i = 0; i < maxRequestNum; i++)
            {
                requestList[i] = -1;
            }
        }

        public void AddRequestList(int sendPlayerIndex, int receivePlayerIndex) //リクエストリストに積みます。overrideで存在する同名のメソッドでint[]とvalueを指定して[0]の要素に乗せます。
        {
            if (DebugText != null) DebugText.text += "\nAddRequestList:Start sendPlayerIndex:" + sendPlayerIndex + " receivePlayerIndex:" + receivePlayerIndex;
            if (sendPlayerIndex < 0 || receivePlayerIndex < 0) return;
            if (isIgnoreMyRequest && sendPlayerIndex == GetMyIndex()) return;
            if (isToMeOnly && receivePlayerIndex != GetMyIndex()) return;
            if (isIgnoreMultipleRequest && requestListSendPlayerIndex[0] == sendPlayerIndex && requestListReceivePlayerIndex[0] == receivePlayerIndex) return;
            if (!isIgnoreSaveSendPlayerIndex)
            {
                if (DebugText != null) DebugText.text += "\nAddRequestList:AddrequestListSendPlayerIndex" + sendPlayerIndex;
                AddRequestList(requestListSendPlayerIndex, sendPlayerIndex);
            }
            if (!isIgnoreSaveReceivePlayerIndex)
            {
                if (DebugText != null) DebugText.text += "\nAddRequestList:AddrequestListReceivePlayerIndex" + receivePlayerIndex;
                AddRequestList(requestListReceivePlayerIndex, receivePlayerIndex);
            }
        }

        /*以下、任意のスクリプトから呼び出して使う想定のメソッド*/
        public int[] GetPlayerIdList() //playerIdのリストを返します。VRCPlayerApiのGetPlayersの順序ではなくワールド内の全プレイヤー共通のplayerId昇順にindexが振り直されています。
        {
            if (PlayerDB == null) return null;
            return PlayerDB.playerIdList; 
        }

        public string[] GetDisplayNameList() //displayNameのリストを返します。VRCPlayerApiのGetPlayersの順序ではなくワールド内の全プレイヤー共通のplayerId昇順にindexが振り直されています。
        {
            if (PlayerDB == null) return null;
            return PlayerDB.displayNameList; 
        }

        public int GetPlayerNum() //インスタンス内のプレイヤー人数を返します。VRCPlayerApiのGetPlayersの要素数と原則同じですが、Join、Leave時にリストと一緒に更新しているので要素数がずれることがないようにこちらで参照することを推奨します。
        {
            if (PlayerDB == null) return -1;
            return PlayerDB.playerNum; 
        }

        public int GetMyIndex() //自分のindexを返します
        {
            if (PlayerDB == null) return -1;
            return PlayerDB.GetMyIndex();
        }

        public int GetPlayerIdFromIndex(int index) ///indexからplayerIdに変換します。不正値の場合は-1で返します。
        {
            if (PlayerDB == null) return -1;
            return PlayerDB.GetPlayerIdFromIndex(index);
        }

        public int GetPlayerIndexFromPlayerId(int playerId) ///playerIdからplayerIndexに変換します。不正値の場合-1で返します。
        {
            if (PlayerDB == null) return -1;
            return PlayerDB.GetPlayerIndexFromPlayerId(playerId);
        }

        public bool SendRequest(int playerId) //対象プレイヤーにリクエストを送信
        {
            if (PlayerDB == null) return false;
            if (DebugText != null) DebugText.text += "\nSendRequest:Start MyIndex" + GetMyIndex();
            int indexTmp = -1;
           for (int i = 0; i < PlayerDB.playerIdList.Length; i++)
            {
                if(PlayerDB.playerIdList[i] == playerId)
                {
                    indexTmp = i;
                    break;
                }
            }

            if (indexTmp < 0)
            {
                if (DebugText != null) DebugText.text += "\nSendRequest:End result=false";
                return false;
            }

            if(_player != null && _player[indexTmp] != null)
            {
                _player[indexTmp].SendRequest(GetMyIndex());
            }
            if (DebugText != null) DebugText.text += "\nSendRequest:End resurlt=true";
            return true;
        }

        public int GetSendPlayerId() //受信しているSendPlayerIdを受け取る。(Updateで呼び続ける想定です。-1の時は何も受け取っていない意味になります。receiveも記録している場合は必ず同フレームに受け取ってください。)
        {
            if (DebugText != null && requestListSendPlayerIndex[0] != -1) DebugText.text += "\nGetSendPlayerId:Start(Get) MyIndex" + GetMyIndex();
            int tmp = -1;
            if (requestListSendPlayerIndex[0] != -1) tmp = GetPlayerIdFromIndex(requestListSendPlayerIndex[0]);
            else return tmp;
            RemoveRequestList(requestListSendPlayerIndex);
            if (DebugText != null) DebugText.text += "\nGetSendPlayerId:End tmp=" + tmp;
            return tmp;
        }

        public int GetReceivePlayerId() //受信しているReceivePlayerIdを受け取る。(Updateで呼び続ける想定です。-1の時は何も受け取っていない意味になります。sendも記録している場合は必ず同フレームに受け取ってください。)
        {
            if (DebugText != null && requestListReceivePlayerIndex[0] != -1) DebugText.text += "\nGetSendPlayerId:Start(Get) MyIndex" + GetMyIndex();
            int tmp = -1;
            if (requestListReceivePlayerIndex[0] != -1) tmp = GetPlayerIdFromIndex(requestListReceivePlayerIndex[0]);
            else return tmp;
            RemoveRequestList(requestListReceivePlayerIndex);
            if (DebugText != null) DebugText.text += "\nGetReceivePlayerId:End tmp=" + tmp;
            return tmp;
        }

        public void ResetRequestList() //リクエストリストをすべて削除します
        {
            ClearRequestList(requestListSendPlayerIndex);
            ClearRequestList(requestListReceivePlayerIndex);
        }
    }
}
