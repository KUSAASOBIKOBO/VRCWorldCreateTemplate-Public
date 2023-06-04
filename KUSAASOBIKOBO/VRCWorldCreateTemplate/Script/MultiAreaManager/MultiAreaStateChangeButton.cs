
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MultiAreaStateChangeButton : UdonSharpBehaviour
    {
        public bool isLocal;//デフォルトはグローバルの値を書き換えます
        public int[] ActiveStateIndex = new int[1] { 0 };//どのパラメータを書き換えるかをActiveStateのindexで指定します
        public MultiAreaManager _multiAreaManager;

        public bool isConstTrue = false;//インタラクト時に元の値にかかわらずtrueにする設定です
        public bool isOnlyOwner = false;//エリアオーナー以外が押した場合発火しなくなる設定です(Globalのみ有効)
        public int[] constFalseActiveStateIndex;//インタラクト時に元の値にかかわらずfalseにするActiveStateのindexで指定します

        public override void Interact()
        {
            Execute();
        }

        public void Execute()
        {
            if (isLocal)
            {
                _multiAreaManager.AreaStateChangeLocal(ActiveStateIndex, isConstTrue, constFalseActiveStateIndex);
            }
            else
            {
                if (isOnlyOwner) _multiAreaManager.AreaStateChangeGlobalOnlyAreaOwner(ActiveStateIndex, isConstTrue, constFalseActiveStateIndex);
                else _multiAreaManager.AreaStateChangeGlobal(ActiveStateIndex, isConstTrue, constFalseActiveStateIndex);
            }
        }
    }
}
