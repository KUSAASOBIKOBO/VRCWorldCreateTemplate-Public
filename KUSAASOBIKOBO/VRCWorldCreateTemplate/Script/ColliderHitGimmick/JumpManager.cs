
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace KUSAASOBIKOBO
{
    /*
     * ＜説明＞
     * ジャンプ可能な回数を変更するスクリプトです。
     * 無限ジャンプフラグをtrueにすると回数を無視して無制限にジャンプできます。
     * ジャンプ力はVRCWorldSettingsのjumpimpulseの値を参照しています。
     * ワールド内に複数置いても特に意味はありません。ジャンプ可能回数が一番大きいものが優先されます。
     * 特に意図がない場合は、VRCWorldのprefabに追加しておけば良いと思います。
     */
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class JumpManager : UdonSharpBehaviour
    {
        [Header("ジャンプ可能回数(無限ジャンプ中は無視)")] public int maxJumpNum = 1;
        [Header("無限ジャンプフラグ")] public bool infinityJump = false;

        private int jumpNum = 1; //ジャンプ回数

        private void Update()
        {
            if (!infinityJump && jumpNum != maxJumpNum && Networking.LocalPlayer.IsPlayerGrounded()) jumpNum = maxJumpNum;//接地したらジャンプ回数を最大値に戻す
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value) return; //ボタンを離したときは無視
            if (infinityJump || jumpNum > 0 && !Networking.LocalPlayer.IsPlayerGrounded())//接地していないときにジャンプ回数が残っていれば追加でジャンプ可能
            {
                Vector3 playerVelocity = Networking.LocalPlayer.GetVelocity();//現在のプレイヤーの速度を取得
                Networking.LocalPlayer.SetVelocity(new Vector3(playerVelocity.x, Networking.LocalPlayer.GetJumpImpulse(), playerVelocity.z));//y座標を除き現在の速度を維持。yはプレイヤーのジャンプ力を設定する。
            }
            if(!infinityJump && jumpNum > 0) jumpNum--;//ジャンプ回数を減らす(初回ジャンプを含む)
        }
    }
}
