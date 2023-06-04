
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Components.Video;
using System;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DLTextureManager : UdonSharpBehaviour
    {
        [Header("VRCUnityVideoPlayerスクリプト")] public VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer videoPlayer;
        [Header("外部読み込みテクスチャ動画URL(skipと入力すると読み込みをスキップします)")] public VRCUrl[] urls;
        [Header("読み込みに用いる動画のindex"), SerializeField] private int currentIndex = 0;
        [Header("テクスチャに使う動画の再生位置"), SerializeField] private float captureTimePoint = 0.5f;
        [Header("次に読み込むテクスチャ")] public DLTextureManager nextDLTexture;
        [Header("別スクリプトから読み込み完了を参照するDLTextureManager")] public DLTextureManager rootDLTexture;

        [Header("初回アクティブ化と同時に再生する")] public bool isPlayOnStart = true;
        [Header("読み込み失敗時永遠に動画再読み込みする(有効推奨)")] public bool isRetryInfinity = true;
        [Header("再読み込みまでの待機秒数")] public float retryWaitSecond = 0;
        [Header("再読み込み回数(isRetryInfinity有効時は無視)")] public byte retryCountMax = 10;

        [Header("外部文字列データを読み込み")] public bool isLoadExternalData = false;
        [Header("LoadExternalData")] public LoadExternalData ExternalDataLoader;

        private bool waitForResponse = false; //重複してLoadUrlを呼ぶことを回避するフラグ
        private bool isError = false; //エラー発生時に再読み込みさせるためのフラグ
        private byte retryCount = 0; //再読み込み回数カウント
        private float retryWaitSecondCount = 0; //再読み込みまでの待機時間秒数カウント
        [NonSerialized] public bool isFailure = false; //読み込み失敗で終了したときに立つフラグ
        public bool isFinish = true; //読み込み完了フラグ(root専用)
        [NonSerialized] public bool forcedStop = false; //読み込みを強制終了させるフラグ(このコード内にこの値を変更するメソッドはありません。一度フラグを立てるとずっと読み込みを強制停止するようになります)
        public bool loaded = false;

        private float lastGetTime;

        public void Start() { if (isPlayOnStart) LoadVideo(); } //開始時にロード
        public void Update() {
            if (isError) LoadVideo();
            if (DebugText != null)
            {
                if (isFinished_before != isFinish)
                {
                    DebugText.text += "\n" + this + "isFinish Changed" + isFinish;
                    isFinished_before = isFinish;
                }
            }

        } //エラー発生時はリロード


        [Header("デバッグテキスト出力用UIText")] public Text DebugText;
        private bool isFinished_before = true;

        public void LoadVideo()
        {
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            isFinish = false;
            if (waitForResponse) return;
            if (!isRetryInfinity && retryCount >= retryCountMax)
            {
                StopLoad();
            }
            if (retryWaitSecondCount > 0)
            {
                retryWaitSecondCount -= Time.deltaTime;
                return;
            }

            waitForResponse = true;
            isError = false;
            if (urls[currentIndex] != null)
            {
                if (urls[currentIndex].Get() == "skip") SkipLoad();
                else videoPlayer.LoadURL(urls[currentIndex]);
            }
        }

        public override void OnVideoReady()
        {
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            waitForResponse = false;
            retryCount = 0;
            videoPlayer.Play();
            //Debug.Log("OnVideoReady");
        }

        public override void OnVideoStart()
        {
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            //Debug.Log("OnVideoStart");
            videoPlayer.SetTime(captureTimePoint);
            videoPlayer.Pause();
            FinishLoad();
        }

        public override void OnVideoError(VideoError videoError)
        {
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            waitForResponse = false;
            isError = true;
            retryWaitSecondCount = retryWaitSecond;
            retryCount++;
        }

        public void SetIndexAndReloadTexture(int index)
        {
            currentIndex = index;
            retryCount = 0;
            LoadVideo();
        }

        public void Reload()
        {
            if (DebugText != null) DebugText.text += "\n"+this+ "Reload()";
            isFinish = false;
            waitForResponse = false;
            isFailure = false;
            retryCount = 0;
            LoadVideo();
        }

        public void FinishLoad() //ロードが正常に終わったとき行う処理です。isLoadExternalDataがtrueの時はデータ読み込みを開始します
        {
            if (DebugText != null) DebugText.text += "\n" + this + "FinishLoad()";
            if (nextDLTexture != null)
            {
                nextDLTexture.gameObject.SetActive(true);
                nextDLTexture.Reload();
            }
            else
            {
                if (rootDLTexture != null) rootDLTexture.isFinish = true;
            }
            isFailure = false;
            if (isLoadExternalData && ExternalDataLoader != null)
            {
                ExternalDataLoader.StartLoad();
            }
            loaded = true;
            //this.gameObject.SetActive(false);
        }

        public void StopLoad() //ロードが失敗したときに行う処理です。強制停止時もこの処理が呼ばれます。
        {
            if (DebugText != null) DebugText.text += "\n" + this + "StopLoad()";
            if (nextDLTexture != null)
            {
                nextDLTexture.gameObject.SetActive(true);
                nextDLTexture.Reload();
            }
            else
            {
                if (rootDLTexture != null) rootDLTexture.isFinish = true;
            }
            isFailure = true;
            loaded = true;
            //this.gameObject.SetActive(false);
        }

        public void SkipLoad() //ロードをスキップしたときに行う処理です。StopLoadと違い、失敗扱いにはなりません。
        {
            if (DebugText != null) DebugText.text += "\n" + this + "SkipLoad()";
            if (nextDLTexture != null)
            {
                nextDLTexture.gameObject.SetActive(true);
                nextDLTexture.Reload();
            }
            else
            {
                if (rootDLTexture != null) rootDLTexture.isFinish = true;
                if (DebugText != null) DebugText.text += "\n" + rootDLTexture + "rootDLTexture.isFinish = " + rootDLTexture.isFinish;
            }
            isFailure = false;
            loaded = true;
            //this.gameObject.SetActive(false);
        }

        public bool ChangeTexture(float otherCaptureTimePoint)
        {
            //captureTimePoint = otherCaptureTimePoint;
            float before_time = videoPlayer.GetTime();
            float tmp = videoPlayer.GetTime() + otherCaptureTimePoint;
            if (tmp >= videoPlayer.GetDuration()) tmp = videoPlayer.GetDuration();
            videoPlayer.SetTime(tmp);
            //Debug.Log("ChangeTexture:videoPlayer.GetTime()"+videoPlayer.GetTime()+" / "+ videoPlayer.GetDuration());//テクスチャ読み込みずれデバッグ用
            return !(videoPlayer.GetTime() == before_time);
            /*if(!this.gameObject.activeSelf)this.gameObject.SetActive(true);
            else Reload();*/
        }

        public bool ChangeTextureFixedTime(float otherCaptureTimePoint)
        {
            float before_time = videoPlayer.GetTime();
            if (otherCaptureTimePoint >= videoPlayer.GetDuration()) otherCaptureTimePoint = videoPlayer.GetDuration();
            videoPlayer.SetTime(otherCaptureTimePoint+ videoPlayer.GetDuration()/20.0f);
            //Debug.Log("ChangeTexture:videoPlayer.GetTime()" + videoPlayer.GetTime() + " / " + videoPlayer.GetDuration());//テクスチャ読み込みずれデバッグ用
            return !(videoPlayer.GetTime() == before_time);
            /*if(!this.gameObject.activeSelf)this.gameObject.SetActive(true);
            else Reload();*/
        }

        public bool ChangeTextureIndex(int textureNum, int index, bool isResetGetTime = false)
        {
            bool result = false;
            float flameTime = videoPlayer.GetDuration() / textureNum;
            videoPlayer.SetTime((float)(((float)flameTime * (float)index) + ((float)flameTime * 0.5f)));//0.5で目的の動画位置の尺の半分まで進んだところを選び確実に取る
            if (isResetGetTime)
            {
                lastGetTime = videoPlayer.GetTime(); //初回は絶対falseで返す
                videoPlayer.Play();
            }
            videoPlayer.Pause();
            if (lastGetTime == videoPlayer.GetTime())
            {
                result = false;
            }
            else
            {
                result = true;
            }
            if (index == 0) result = true; //ビデオロード分の時間を初回ロード時に待たせるためフルカウントまで再セットさせる場合はfalse
            //Debug.Log("ChangeTexture:targetTime = "+ flameTime * index + ", videoPlayer.GetTime()" + videoPlayer.GetTime() + " / " + videoPlayer.GetDuration());//テクスチャ読み込みずれデバッグ用
            lastGetTime = videoPlayer.GetTime();
            return result;
            /*if(!this.gameObject.activeSelf)this.gameObject.SetActive(true);
            else Reload();*/
        }

        public bool IsSkiped()
        {
            if (urls[0] != null && urls[0].Get() == "skip") return true;
            return false;
        }
    }
}