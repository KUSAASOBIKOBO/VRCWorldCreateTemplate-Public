
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    /*
     
     */
    public class WarpManager : UdonSharpBehaviour
    {
        [Header("ワープ開始時に非アクティブ化にするオブジェクトリスト")] public GameObjectList unActiveObjectList;

        /*ワープ時に視界をフェードアウトし、ワープ後にフェードインで視界を復活させる処理*/
        [Header("ワープ時にフェードアウト・イン効果を発生させる")] public bool isFade = false;


        /*ワープ時に一度特定のエリアに移動させてからワープさせる処理*/
        [Header("ワープ時に一度特定のエリアに移動させてからワープさせる")] public bool isUseResetPosition = false;

        void Start()
        {

        }
    }
}
