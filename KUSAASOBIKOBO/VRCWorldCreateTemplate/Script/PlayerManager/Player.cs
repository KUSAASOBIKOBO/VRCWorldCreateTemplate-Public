
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Player : UdonSharpBehaviour
    {
        [Header("レシーバーインデックス")] public int index = -1; //PlayerManager上のindex

        [Header("プレイヤーマネージャー")] public PlayerManager _playerManager;

        public void SendRequest(int sendPlayerIndex)
        {
            if (sendPlayerIndex < 0 || sendPlayerIndex >= 80) return;

            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ExeFromPlayerIndex" + sendPlayerIndex);
        }

        public void ExeFromPlayerIndex0()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(0, index);
        }
        public void ExeFromPlayerIndex1()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(1, index);
        }
        public void ExeFromPlayerIndex2()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(2, index);
        }
        public void ExeFromPlayerIndex3()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(3, index);
        }
        public void ExeFromPlayerIndex4()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(4, index);
        }
        public void ExeFromPlayerIndex5()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(5, index);
        }
        public void ExeFromPlayerIndex6()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(6, index);
        }
        public void ExeFromPlayerIndex7()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(7, index);
        }
        public void ExeFromPlayerIndex8()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(8, index);
        }
        public void ExeFromPlayerIndex9()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(9, index);
        }
        public void ExeFromPlayerIndex10()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(10, index);
        }
        public void ExeFromPlayerIndex11()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(11, index);
        }
        public void ExeFromPlayerIndex12()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(12, index);
        }
        public void ExeFromPlayerIndex13()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(13, index);
        }
        public void ExeFromPlayerIndex14()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(14, index);
        }
        public void ExeFromPlayerIndex15()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(15, index);
        }
        public void ExeFromPlayerIndex16()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(16, index);
        }
        public void ExeFromPlayerIndex17()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(17, index);
        }
        public void ExeFromPlayerIndex18()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(18, index);
        }
        public void ExeFromPlayerIndex19()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(19, index);
        }
        public void ExeFromPlayerIndex20()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(20, index);
        }
        public void ExeFromPlayerIndex21()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(21, index);
        }
        public void ExeFromPlayerIndex22()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(22, index);
        }
        public void ExeFromPlayerIndex23()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(23, index);
        }
        public void ExeFromPlayerIndex24()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(24, index);
        }
        public void ExeFromPlayerIndex25()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(25, index);
        }
        public void ExeFromPlayerIndex26()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(26, index);
        }
        public void ExeFromPlayerIndex27()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(27, index);
        }
        public void ExeFromPlayerIndex28()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(28, index);
        }
        public void ExeFromPlayerIndex29()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(29, index);
        }
        public void ExeFromPlayerIndex30()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(30, index);
        }
        public void ExeFromPlayerIndex31()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(31, index);
        }
        public void ExeFromPlayerIndex32()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(32, index);
        }
        public void ExeFromPlayerIndex33()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(33, index);
        }
        public void ExeFromPlayerIndex34()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(34, index);
        }
        public void ExeFromPlayerIndex35()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(35, index);
        }
        public void ExeFromPlayerIndex36()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(36, index);
        }
        public void ExeFromPlayerIndex37()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(37, index);
        }
        public void ExeFromPlayerIndex38()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(38, index);
        }
        public void ExeFromPlayerIndex39()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(39, index);
        }
        public void ExeFromPlayerIndex40()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(40, index);
        }
        public void ExeFromPlayerIndex41()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(41, index);
        }
        public void ExeFromPlayerIndex42()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(42, index);
        }
        public void ExeFromPlayerIndex43()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(43, index);
        }
        public void ExeFromPlayerIndex44()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(44, index);
        }
        public void ExeFromPlayerIndex45()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(45, index);
        }
        public void ExeFromPlayerIndex46()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(46, index);
        }
        public void ExeFromPlayerIndex47()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(47, index);
        }
        public void ExeFromPlayerIndex48()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(48, index);
        }
        public void ExeFromPlayerIndex49()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(49, index);
        }
        public void ExeFromPlayerIndex50()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(50, index);
        }
        public void ExeFromPlayerIndex51()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(51, index);
        }
        public void ExeFromPlayerIndex52()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(52, index);
        }
        public void ExeFromPlayerIndex53()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(53, index);
        }
        public void ExeFromPlayerIndex54()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(54, index);
        }
        public void ExeFromPlayerIndex55()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(55, index);
        }
        public void ExeFromPlayerIndex56()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(56, index);
        }
        public void ExeFromPlayerIndex57()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(57, index);
        }
        public void ExeFromPlayerIndex58()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(58, index);
        }
        public void ExeFromPlayerIndex59()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(59, index);
        }
        public void ExeFromPlayerIndex60()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(60, index);
        }
        public void ExeFromPlayerIndex61()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(61, index);
        }
        public void ExeFromPlayerIndex62()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(62, index);
        }
        public void ExeFromPlayerIndex63()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(63, index);
        }
        public void ExeFromPlayerIndex64()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(64, index);
        }
        public void ExeFromPlayerIndex65()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(65, index);
        }
        public void ExeFromPlayerIndex66()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(66, index);
        }
        public void ExeFromPlayerIndex67()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(67, index);
        }
        public void ExeFromPlayerIndex68()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(68, index);
        }
        public void ExeFromPlayerIndex69()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(69, index);
        }
        public void ExeFromPlayerIndex70()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(70, index);
        }
        public void ExeFromPlayerIndex71()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(71, index);
        }
        public void ExeFromPlayerIndex72()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(72, index);
        }
        public void ExeFromPlayerIndex73()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(73, index);
        }
        public void ExeFromPlayerIndex74()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(74, index);
        }
        public void ExeFromPlayerIndex75()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(75, index);
        }
        public void ExeFromPlayerIndex76()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(76, index);
        }
        public void ExeFromPlayerIndex77()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(77, index);
        }
        public void ExeFromPlayerIndex78()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(78, index);
        }
        public void ExeFromPlayerIndex79()
        {
            if (index < 0) return;
            _playerManager.AddRequestList(79, index);
        }
    }
}
