
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Components.Video;
using System;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    /*＜説明＞
     * 同期機構を持たない動画プレイヤーを用いて外部読み込み動画の音だけをBGMとして再生するスクリプトです。
     * 組み込みでBGMを設定しなくて良いのでワールド容量の削減をしつつ高音質のBGMが設定できるようになります。
     * 使用するBGMは予めmp4動画にしてyoutubeの限定公開アップロード等しておく必要があります。
     * 動画プレイヤーの複数設置時にエラーで読み込まれなくなる問題を防ぐためにリロード機能がついています。
     * 複数のURLを指定してSetBGMAndPlayを呼び出すことで一つのスクリプト制御で複数のBGMを管理できます。
     * 場所ごとにBGMを変える目的でColliderHitGimmickなどと組み合わせて使用することを想定しています。
     */
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalBGMManager : UdonSharpBehaviour
    {
        [Header("VRCAVProVideoPlayerスクリプト")] public VRCAVProVideoPlayer videoPlayer;
        [Header("外部読み込みBGM用動画URL")] public VRCUrl[] urls;
        [Header("再生を行うBGMのindex"), SerializeField] private int currentBgmIndex = 0;
        [Header("次に読み込むBGMManager")] public ExternalBGMManager nextBGMManager;
        [Header("別スクリプトから読み込み完了を参照するBGMManager")] public ExternalBGMManager rootBGMManager;
        [Header("初回アクティブ化と同時に再生する")] public bool isPlayOnStart = true;
        [Header("連続再生するか")] public bool isContinuous = true;
        [Header("全てのURLを順番に再生")] public bool isIndexRotation = false;
        [Header("読み込み失敗時永遠に動画再読み込みする")] public bool isRetryInfinity = true;
        [Header("再読み込みまでの待機秒数")] public float retryWaitSecond = 0;
        [Header("再読み込み回数(isRetryInfinity有効時は無視)")] public byte retryCountMax = 10;

        [Header("ExternalResourceLoadManagerのロード中は待つ")] public ExternalResourceLoadManager ExResourceLoadManager;

        private bool waitForResponse;
        private float waitForResponseCount = 0.0f;
        public float waitForResponseMax = 5.0f;
        private bool isError = false;
        private byte retryCount = 0;
        private float retryWaitSecondCount = 0;
        private bool reloadMyself = false;
        [NonSerialized] public bool isFailure = false; //読み込み失敗で終了したときに立つフラグ
        [NonSerialized] public bool isFinish = true; //読み込み完了フラグ
        [NonSerialized] public bool forcedStop = false; //読み込みを強制終了させるフラグ(このコード内にこの値を変更するメソッドはありません。一度フラグを立てるとずっと読み込みを強制停止するようになります)

        private VRCUrl stopUrl = new VRCUrl("skip");

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        public void Start() { if (isPlayOnStart) LoadVideo(); } //開始時にロード
        public override void OnVideoEnd() { if (isContinuous) LoadVideo(false); } //曲が終了したらリロード
        public void Update() {
            if (waitForResponse)
            {
                if (waitForResponseCount > 0.0f)
                {
                    waitForResponseCount -= Time.deltaTime;
                }
                else
                {
                    waitForResponse = false;
                }
            }
            if (isError) LoadVideo(); 
        } //エラー発生時はリロード

        public void LoadVideo(bool isCallNext = true)
        {
            if (forcedStop)
            {
                reloadMyself = false;
                StopLoad(isCallNext);
                return;
            }
            reloadMyself = !isCallNext;
            isFinish = false;
            if (ExResourceLoadManager != null && !ExResourceLoadManager.GetIsFinish())
            {
                isError = true;
                return;
            }
            if (waitForResponse)
            {
                isError = true;
                return;
            }
            if (!isRetryInfinity && retryCount >= retryCountMax)
            {
                StopLoad(isCallNext);
                return;
            }
            if (retryWaitSecondCount > 0)
            {
                retryWaitSecondCount -= Time.deltaTime;
                return;
            }

            isError = false;

            if (urls[currentBgmIndex] != null)
            {
                if (urls[currentBgmIndex].Get() == "skip" || urls[currentBgmIndex].Get() == "") StopLoad(isCallNext);
                else
                {
                    waitForResponse = true;
                    waitForResponseCount = waitForResponseMax;
                    videoPlayer.LoadURL(urls[currentBgmIndex]);
                }
            }
        }

        public override void OnVideoReady()
        {
            waitForResponse = false;
            if (forcedStop)
            {
                reloadMyself = false;
                StopLoad();
                return;
            }
            retryCount = 0;
            videoPlayer.Play();

            if (isIndexRotation)
            {
                currentBgmIndex++;
                if (currentBgmIndex > urls.Length - 1)
                {
                    currentBgmIndex = 0;
                }
                else
                {
                    while (urls[currentBgmIndex].Get() == "skip" || urls[currentBgmIndex].Get() == "")
                    {
                        currentBgmIndex++;
                        if (currentBgmIndex > urls.Length - 1)
                        {
                            currentBgmIndex = 0;
                            break;
                        }
                    }
                }
            }
            if(reloadMyself) FinishLoad(false);
            else FinishLoad();
        }

        public override void OnVideoError(VideoError videoError)
        {
            waitForResponse = false;
            if (forcedStop)
            {
                reloadMyself = false;
                StopLoad();
                return;
            }
            isError = true;
            retryWaitSecondCount = retryWaitSecond;
            retryCount++;
        }

        public void SetBGMAndPlay(int index)
        {
            currentBgmIndex = index;
            isFinish = false;
            Reload();
        }

        public void Reload()
        {
            if(DebugText != null) DebugText.text += "\nBGM:Reload";
            isFinish = false;
            //waitForResponse = false;
            isFailure = false;
            retryCount = 0;
            LoadVideo();
        }

        public void Stop()
        {
            if (DebugText != null) DebugText.text += "\nBGM:Stop";
            videoPlayer.Stop();
        }

        public void StopLoad(bool isCallNext = true)
        {
            /*if(waitForResponse)
            {
                Stop();
            }
            else
            {*/
                if (isCallNext)
                {
                    if (nextBGMManager != null)
                    {
                        nextBGMManager.Reload();
                    }
                    else
                    {
                        if (rootBGMManager != null) rootBGMManager.isFinish = true;
                    }
                }
                else
                {
                    isFinish = true;
                }

                Stop();
                isFailure = true;
                isError = false;
                urls[currentBgmIndex] = stopUrl;
                //if (urls[0] != null && urls[0].Get() == "skip") this.gameObject.SetActive(false);
            //}
        }

        public void FinishLoad(bool isCallNext = true)
        {
            if (isCallNext)
            {
                if (nextBGMManager != null)
                {
                    nextBGMManager.Reload();
                }
                else
                {
                    if (rootBGMManager != null) rootBGMManager.isFinish = true;
                }
            }
            else
            {
                isFinish = true;
            }

            isFailure = false;
        }
    }
}
