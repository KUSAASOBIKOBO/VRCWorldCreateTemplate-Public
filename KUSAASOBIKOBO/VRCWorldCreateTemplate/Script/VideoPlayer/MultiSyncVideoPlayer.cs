using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Components.Video;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MultiSyncVideoPlayer : UdonSharpBehaviour
    {
        [Header("VRCAVProVideoPlayerスクリプト")] public VRCAVProVideoPlayer videoPlayer;
        [Header("Syncer")] public MultiSyncVideoPlayerSyncer[] _multiSyncVideoPlayerSyncer = new MultiSyncVideoPlayerSyncer[80];

        [Header("動画再生同期")] public PlayerManager2 playerManagerVideoPlaySync;
        [Header("ポーズ同期")] public PlayerManager2 playerManagerVideoStopPlaySync;
        [Header("再生位置同期")] public PlayerManager2 playerManagerVideoTimeSync;
        [Header("動画再生を精密同期")] public PlayerManager2 playerManagerVideoPlayPrecisionSync;

        [Header("TimeManager")] public TimeManager _timeManager;

        [Header("URLリスト")] public VRCUrlList urlList;
        [Header("OverlayUI")] public OverlayUIManager overlayUI;
        [Header("自分自身で他プレイヤーの再生リクエストを待機する")] public bool isCheckOtherPlayerRequest;

        [Header("バルクプレイリストURLインデックスリスト"), TextArea(1,1000)] public string bulkPlaylistUrlIndices;
        [Header("カレントURLPc")] public VRCUrl currentUrlPc;
        [Header("カレントURLQuest")] public VRCUrl currentUrlQuest;
        private float startCurrentTime = 0.0f;//動画の再生開始位置
        private DateTime startCurrentDateTime;//動画の再生開始オフセット
        //[Header("ユーザーごとのURL(PC用)"), UdonSynced(UdonSyncMode.None)] public VRCUrl[] multiUserUrlsPc;//ワールド内最大プレイヤー人数分必要です。デフォルト80
        //[Header("ユーザーごとのURL(Quest用)"), UdonSynced(UdonSyncMode.None)] public VRCUrl[] multiUserUrlsQuest;//ワールド内最大プレイヤー人数分必要です。デフォルト80
        //[Header("ユーザーごとのストリーミングか"), UdonSynced(UdonSyncMode.None)] public bool[] multiUserIsStreamingVideo = new bool[80] {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };//ユーザーごとのビデオ再生位置;//ワールド内最大プレイヤー人数分必要です。デフォルト80
        //[Header("ユーザーごとのポーズ中か"), UdonSynced(UdonSyncMode.None)] public bool[] multiUserIsPauseVideo = new bool[80] {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };//ユーザーごとのビデオ再生位置;//ワールド内最大プレイヤー人数分必要です。デフォルト80
        
        //[UdonSynced(UdonSyncMode.None)] private float[] multiUserCurrentTime = new float[80] {0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };//ユーザーごとのビデオ再生位置
        //[UdonSynced(UdonSyncMode.None)] private string[] multiUserCurrentDateTime = new string[80]{"0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00","0000-00-00 00:00:00"};//ユーザーごとのビデオ再生位置

        [Header("初回アクティブ化と同時に再生する")] public bool isPlayOnStart = true;
        [Header("動画終了時に同じ動画を再読み込みするか")] public bool isContinuous = true;
        [Header("読み込み失敗時永遠に動画再読み込みする")] public bool isRetryInfinity = true;
        [Header("ポーズ中か")] public  bool isPause = false;
        [Header("再読み込みまでの待機秒数")] public float retryWaitSecond = 0;
        [Header("何秒に1回自分の視聴中の状態同期を更新")] public float syncWaitSecond = 1;
        [Header("再読み込み回数(isRetryInfinity有効時は無視)")] public byte retryCountMax = 10;

        [Header("ExResourceLoadManagerのロード中は待つ(default:null)")] public ExternalResourceLoadManager ExResourceLoadManager;

        [Header("プレイリスト表示用Text")] public Text[] playListViewWithCategory;
        [Header("プレイリストタイトル表示用Text")] public Text[] playListTitleViewWithCategory;
        [Header("プレイリストタイトル")] public string[] categoryTitles;

        [Header("プレイリスト表示用Text")] public Text playListViewWithCategoryPage;
        [Header("プレイリストタイトル表示用Text")] public Text playListTitleViewWithCategoryPage;
        [Header("表示するページindex")] public int categoryPageIndex = 1;
        [Header("表示するページindex最小値（初期値）")] public int categoryPageRangeMin = 1;
        [Header("表示するページindex最大値")] public int categoryPageRangeMax = 1;

        [Header("透過ディスプレイのマテリアル")] public Material transparentDisplayMaterial;
        [Header("透過ディスプレイコントロールスライダー")] public Slider transparentDisplaySlider;

        [Header("すべてのSpeaker")] public AudioSource[] speakers;
        [Header("ボリュームコントロールスライダー")] public Slider volumeSlider;

        [Header("開始時にバルクプレイリストをプレイリストに分割する")] public bool isGeneratePlaylistRealtime = true;


        [Header("デバッグテキスト出力用UIText")] public Text DebugText;


        public string[] playlistUrlIndicesTitle;
        public int[] playlistUrlIndicesPc;
        public int[] playlistUrlIndicesQuest;
        public bool[] isStreamingVideo;
        public int[] categoryVideo;
        public int[] licenseKeyIndex;
        public GameObject[] licenseKeyObject1;
        public GameObject[] licenseKeyObject2;
        private bool waitForResponse;
        private bool isError = false;
        private byte retryCount = 0;
        private float retryWaitSecondCount = 0;
        private float syncWaitSecondCount = 0;
        [NonSerialized] public bool isFailure = true; //読み込み失敗で終了したときに立つフラグ(これが立っていると再生時間を同期しなくなる)
        [NonSerialized] public bool isFinish = true; //読み込み完了フラグ
        [NonSerialized] public bool forcedStop = false; //読み込みを強制終了させるフラグ(このコード内にこの値を変更するメソッドはありません。一度フラグを立てるとずっと読み込みを強制停止するようになります)

        private float waitForResponseCount = 0.0f;
        public float waitForResponseMax = 5.0f;

        [Header("同期中テキスト(自力待機時のみ使用)")] public string SyncingText = "他のプレイヤーから動画を同期中…";
        [Header("精密同期中テキスト")] public string PrecisionSyncingText = "精密同期待機中…";
        [Header("動画共有が来てから再生までの待機時間(自力待機時のみ使用)")] public float waitingForSynchronizationTime = 5.0f;
        private float waitingForSynchronizationTimeCount = 0.0f;
        private int syncerPlayerId = -1;

        private float waitingForSynchronizationTimeCount2 = 0.0f;
        private int syncerPlayerId2 = -1;

        private float waitingForSynchronizationTimeCount3 = 0.0f;
        private int syncerPlayerId3 = -1;

        private bool isPrecisionSyncReserve = false;

        [NonSerialized] public float offsetCounter = 0.0f;

        public void Start() {

            if (isGeneratePlaylistRealtime) GeneratePlaylist();

            if (isPlayOnStart)
            {
                SetVideoAndPlayWithPlaylistIndex(0);
            }

            if (playListViewWithCategory.Length == playListTitleViewWithCategory.Length && playListTitleViewWithCategory.Length == categoryTitles.Length)
            {
                int len_tmp = playListTitleViewWithCategory.Length;
                for (int i = 0; i < len_tmp; i++)
                {
                    if (playListTitleViewWithCategory[i] != null) playListTitleViewWithCategory[i].text = categoryTitles[i];
                }

                len_tmp = categoryVideo.Length;
                for (int i = 0; i < len_tmp; i++)
                {
                    if (categoryVideo[i] < playListViewWithCategory.Length && playListViewWithCategory[categoryVideo[i]] != null)
                    {
                        playListViewWithCategory[categoryVideo[i]].text += i + ":" + playlistUrlIndicesTitle[i] + "\n";
                    }
                }
            }
            SetCategoryPage();
        } 

        public void GeneratePlaylist()
        {
            if (bulkPlaylistUrlIndices != "")
            {
                string[] bulkSplit_tmp;
                string[] bulkSplit_tmp2;
                int bulksplit_index = 0;
                bulkSplit_tmp = bulkPlaylistUrlIndices.Split('\n');
                playlistUrlIndicesTitle = new string[bulkSplit_tmp.Length];
                playlistUrlIndicesPc = new int[bulkSplit_tmp.Length];
                playlistUrlIndicesQuest = new int[bulkSplit_tmp.Length];
                isStreamingVideo = new bool[bulkSplit_tmp.Length];
                categoryVideo = new int[bulkSplit_tmp.Length];
                licenseKeyIndex = new int[bulkSplit_tmp.Length];
                foreach (string tmp in bulkSplit_tmp)
                {
                    bulkSplit_tmp2 = tmp.Split(',');

                    if (bulkSplit_tmp2.Length >= 6)
                    {
                        playlistUrlIndicesTitle[bulksplit_index] = bulkSplit_tmp2[0];
                        playlistUrlIndicesPc[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[1], 10);
                        playlistUrlIndicesQuest[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[2], 10);
                        if (bulkSplit_tmp2[3] == "1") isStreamingVideo[bulksplit_index] = true;
                        else if (bulkSplit_tmp2[3] == "0") isStreamingVideo[bulksplit_index] = false;
                        categoryVideo[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[4], 10);
                        licenseKeyIndex[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[5], 10);
                        bulksplit_index++;
                    }
                    else if (bulkSplit_tmp2.Length >= 5)
                    {
                        playlistUrlIndicesTitle[bulksplit_index] = bulkSplit_tmp2[0];
                        playlistUrlIndicesPc[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[1], 10);
                        playlistUrlIndicesQuest[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[2], 10);
                        if (bulkSplit_tmp2[3] == "1") isStreamingVideo[bulksplit_index] = true;
                        else if (bulkSplit_tmp2[3] == "0") isStreamingVideo[bulksplit_index] = false;
                        Debug.Log("Convert.ToInt32(bulkSplit_tmp2[4],10):" + bulkSplit_tmp2[4]);
                        categoryVideo[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[4], 10);
                        licenseKeyIndex[bulksplit_index] = -1;
                        bulksplit_index++;
                    }
                    else if (bulkSplit_tmp2.Length >= 4)
                    {
                        playlistUrlIndicesTitle[bulksplit_index] = bulkSplit_tmp2[0];
                        playlistUrlIndicesPc[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[1], 10);
                        playlistUrlIndicesQuest[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[2], 10);
                        if (bulkSplit_tmp2[3] == "1") isStreamingVideo[bulksplit_index] = true;
                        else if (bulkSplit_tmp2[3] == "0") isStreamingVideo[bulksplit_index] = false;
                        categoryVideo[bulksplit_index] = 0;
                        licenseKeyIndex[bulksplit_index] = -1;
                        bulksplit_index++;
                    }
                    else if (bulkSplit_tmp2.Length >= 3)
                    {
                        playlistUrlIndicesTitle[bulksplit_index] = bulkSplit_tmp2[0];
                        playlistUrlIndicesPc[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[1], 10);
                        playlistUrlIndicesQuest[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[2], 10);
                        isStreamingVideo[bulksplit_index] = false;
                        categoryVideo[bulksplit_index] = 0;
                        licenseKeyIndex[bulksplit_index] = -1;
                        bulksplit_index++;
                    }
                    else if (bulkSplit_tmp2.Length >= 2)
                    {
                        playlistUrlIndicesTitle[bulksplit_index] = bulkSplit_tmp2[0];
                        playlistUrlIndicesPc[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[1], 10);
                        playlistUrlIndicesQuest[bulksplit_index] = Convert.ToInt32(bulkSplit_tmp2[1], 10);
                        isStreamingVideo[bulksplit_index] = false;
                        categoryVideo[bulksplit_index] = 0;
                        licenseKeyIndex[bulksplit_index] = -1;
                        bulksplit_index++;
                    }
                }
            }
        } //プレイリストを構築

        public void MoveTimeReset()
        {
            offsetCounter = 0.0f;
            //videoPlayer.SetTime(videoPlayer.GetTime() - offsetCounter);
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を+1秒進めました");
        }

        public void MoveTime_plus1()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() + 1.0f);
            offsetCounter += 1.0f;
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を+1秒進めました");
        }

        public void MoveTime_minus1()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() - 1.0f);
            offsetCounter -= 1.0f;
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を-1秒戻しました");
        }

        public void MoveTime_plus0_1()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() + 0.1f);
            offsetCounter += 0.1f;
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を+0.1秒進めました");
        }

        public void MoveTime_minus0_1()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() - 0.1f);
            offsetCounter -= 0.1f;
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を-0.1秒戻しました");
        }

        public void MoveTime_plus0_0_1()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() + 0.01f);
            offsetCounter += 0.01f;
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を+0.01秒進めました");
        }

        public void MoveTime_minus0_0_1()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() - 0.01f);
            offsetCounter -= 0.01f;
            //if (overlayUI != null) overlayUI.SetInfomation("再生位置を-0.01秒戻しました");
        }

        public void MoveTime_plus30()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() + 30.0f);
            if (overlayUI != null) overlayUI.SetInfomation("再生位置を+30秒進めました");
        }

        public void MoveTime_minus30()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() - 30.0f);
            if (overlayUI != null) overlayUI.SetInfomation("再生位置を-30秒戻しました");
        }

        public void MoveTime_plus10()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() + 10.0f);
            if (overlayUI != null) overlayUI.SetInfomation("再生位置を+10秒進めました");
        }

        public void MoveTime_minus10()
        {
            videoPlayer.SetTime(videoPlayer.GetTime() - 10.0f);
            if (overlayUI != null) overlayUI.SetInfomation("再生位置を-10秒戻しました");
        }

        public void PushResync()
        {
            Reload();
            if (overlayUI != null) overlayUI.SetInfomation("動画をリロードします");
        }

        public void SetTransparentDisplayMaterialValue()
        {
            if(transparentDisplaySlider != null && transparentDisplayMaterial != null)
            {
                transparentDisplayMaterial.SetFloat("_Alpha", transparentDisplaySlider.normalizedValue);
                if (overlayUI != null) overlayUI.SetInfomation("不透明度を変更：" + (transparentDisplayMaterial.GetFloat("_Alpha")*100).ToString("F1") + "%");
            }
        }

        public void SetVolume()
        {
            if(volumeSlider != null)
            {
                float volume_tmp = volumeSlider.normalizedValue;
                foreach(AudioSource tmp in speakers)
                {
                    tmp.volume = volume_tmp;
                }
                if (overlayUI != null) overlayUI.SetInfomation("音量を変更：" + (volume_tmp*100).ToString("F1") + "%");
            }
        }

        public void SetCategoryPage()
        {
            if(categoryPageIndex < categoryTitles.Length)
            {
                if(playListViewWithCategoryPage != null) playListViewWithCategoryPage.text = "";
                if(playListTitleViewWithCategoryPage != null) playListTitleViewWithCategoryPage.text = categoryTitles[categoryPageIndex];
                int len_tmp = categoryVideo.Length;
                for(int i=0; i < len_tmp; i++)
                {
                    if(playListViewWithCategoryPage != null && categoryVideo[i] == categoryPageIndex)
                    {
                        playListViewWithCategoryPage.text += i + ":" + playlistUrlIndicesTitle[i] + "\n";
                    }
                }
            }
        }

        public void NextCategoryPage()
        {
            categoryPageIndex++;
            if (categoryPageRangeMax > categoryTitles.Length-1) categoryPageRangeMax = categoryTitles.Length-1;
            if (categoryPageIndex >= categoryPageRangeMax+1)
            {
                if (categoryPageRangeMin < 1) categoryPageRangeMin = 1;
                categoryPageIndex = categoryPageRangeMin;
            }
            SetCategoryPage();
        }

        public void BeforeCategoryPage()
        {
            if(categoryPageIndex < categoryTitles.Length)
            {
                categoryPageIndex--;
                if (categoryPageRangeMin < 1) categoryPageRangeMin = 1;
                if (categoryPageIndex < categoryPageRangeMin)
                {
                    if (categoryPageRangeMax > categoryTitles.Length - 1) categoryPageRangeMax = categoryTitles.Length - 1;
                    categoryPageIndex = categoryPageRangeMax;
                }
            }
            SetCategoryPage();
        }

        public override void OnVideoEnd() { if (isContinuous) LoadVideo(); } //曲が終了したらリロード
        public void Update() {

            //チェック
            if(DebugText != null){
                /*
                DebugText.text = "isFailure"+isFailure;
                DebugText.text += "\nwaitForResponse"+waitForResponse;
                DebugText.text += "\nisFinish"+isFinish;
                DebugText.text += "\nforcedStop"+forcedStop;
                DebugText.text += "\nisError"+isError;
                DebugText.text += "\nisContinuous"+isContinuous;
                DebugText.text += "\nIsStream";
                foreach(bool tmp in multiUserIsStreamingVideo){
                    DebugText.text += "\n"+tmp;
                }
                */

                
                DebugText.text = "PCURLs\n";
                int debugTextIndex = 0;
                foreach(MultiSyncVideoPlayerSyncer tmp in _multiSyncVideoPlayerSyncer){
                    DebugText.text += debugTextIndex + ":" + tmp.GetMultiUserUrlsPc() + "\n";
                    debugTextIndex++;
                }/*
                DebugText.text += "\nQuestURLs";
                foreach(VRCUrl tmp in multiUserUrlsQuest){
                    if(tmp.Get() != "") DebugText.text += "\n"+tmp;
                }*/
            }


            if (isError)
            {
                LoadVideo();  //エラー発生時はリロード
            }
            else
            {
                /*処理中じゃないときにforceStopが立った場合一度停止する*/
                if(!isFinish && forcedStop){
                    StopLoad();
                }

                /*一定時間ごとに視聴状態を同期*/
                if(!isFailure && isFinish && syncWaitSecondCount <= 0)
                {
                    syncWaitSecondCount = syncWaitSecond;
                    //if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                    if(_multiSyncVideoPlayerSyncer != null)
                    {
                        int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                        if(myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                        {
                            _multiSyncVideoPlayerSyncer[myIndex_tmp].Set(currentUrlPc, currentUrlQuest, !isContinuous, videoPlayer.GetTime(), _timeManager.now.ToString());
                        }
                    }

                    /*if (myIndex_tmp != -1 && multiUserUrlsPc[myIndex_tmp] != null) multiUserUrlsPc[myIndex_tmp] = currentUrlPc;
                    if (myIndex_tmp != -1 && multiUserUrlsQuest[myIndex_tmp] != null) multiUserUrlsQuest[myIndex_tmp] = currentUrlQuest;
                    if (myIndex_tmp != -1 && multiUserIsStreamingVideo.Length > myIndex_tmp) multiUserIsStreamingVideo[myIndex_tmp] = !isContinuous;
                    //if (myIndex_tmp != -1 && multiUserIsPauseVideo[myIndex_tmp] != null) multiUserIsPauseVideo[myIndex_tmp] = isPause;
                    if (myIndex_tmp != -1 ) multiUserCurrentTime[myIndex_tmp] = videoPlayer.GetTime();
                    if (myIndex_tmp != -1 && _timeManager != null) multiUserCurrentDateTime[myIndex_tmp] = _timeManager.now.ToString();
                    Sync();*/
                }
                else
                {
                    syncWaitSecondCount -= Time.deltaTime;
                }
            }
            /*再生開始までにかかった時間をオフセットに加算する処理（同期元の再生開始までの遅延ものって使いにくかったので廃止）*/
            //if(startCurrentTime > 0.0f) startCurrentTime += Time.deltaTime;

            /*他プレイヤーからの再生リクエストを待機(通常はExResourceLoadManagerで監視するため不要です。複数台利用などで単体で置く必要がある場合のみここでチェックします)*/
            if(isCheckOtherPlayerRequest)
            {
                if(waitingForSynchronizationTimeCount <= 0.0f)
                {
                    if(syncerPlayerId != -1)
                    {
                        forcedStop = false; //他プレイヤーからの同期はforceStopの影響を受けない
                        SetVideoAndPlayWithPlayerIndex(playerManagerVideoPlaySync.GetPlayerIndexFromPlayerId(syncerPlayerId));
                        //startCurrentTime += waitingForSynchronizationTime;
                        syncerPlayerId = -1;
                    }
                    else
                    {
                        int sendPlayerId_tmp2 = -1;
                        sendPlayerId_tmp2 = playerManagerVideoPlaySync.GetSendPlayerId();
                        if (sendPlayerId_tmp2 != -1)//再生リクエストを受信したときの処理
                        {
                            SyncRequestSyncerFromPlayerId(sendPlayerId_tmp2);
                            syncerPlayerId = sendPlayerId_tmp2;
                            waitingForSynchronizationTimeCount = waitingForSynchronizationTime;
                        }
                    }
                }
                else
                {
                    if (overlayUI != null) overlayUI.SetInfomation(SyncingText);
                    waitingForSynchronizationTimeCount -= Time.deltaTime;
                }
            }
            /*他プレイヤーからの精密同期リクエストを待機*/
            int sendPlayerId_tmp4 = -1;
            if(playerManagerVideoPlayPrecisionSync != null) sendPlayerId_tmp4 = playerManagerVideoPlayPrecisionSync.GetSendPlayerId();
            if(sendPlayerId_tmp4 != -1){
                isPrecisionSyncReserve = true;
            }

            if(isPrecisionSyncReserve){
                if(_timeManager != null){
                    _timeManager.GetTime();
                    if(_timeManager.now.Second%10 == 9){
                        videoPlayer.SetTime(0);
                        isPrecisionSyncReserve = false;
                    }else{
                        if (overlayUI != null) overlayUI.SetInfomation(PrecisionSyncingText);
                    }
                }
            }

            /*他プレイヤーからのポーズ・ポーズ解除リクエストを待機*/
/*            int sendPlayerId_tmp = -1;
            sendPlayerId_tmp = playerManagerVideoStopPlaySync.GetSendPlayerId();
            if (sendPlayerId_tmp != -1)//リクエストを受信したときの処理
            {
                if(multiUserIsPauseVideo[playerManagerVideoStopPlaySync.GetPlayerIndexFromPlayerId(sendPlayerId_tmp)])
                {
                    Pause();
                }
                else
                {
                    ReleasePause();
                }
            }*/
            if(waitingForSynchronizationTimeCount2 <= 0.0f)
            {
                if(syncerPlayerId2 != -1)
                {
                    if(_multiSyncVideoPlayerSyncer[playerManagerVideoStopPlaySync.GetPlayerIndexFromPlayerId(syncerPlayerId2)].GetMultiUserIsPauseVideo())
                    {
                        Pause();
                    }
                    else
                    {
                        ReleasePause();
                        if(_timeManager != null) videoPlayer.SetTime(_multiSyncVideoPlayerSyncer[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId2)].GetMultiUserCurrentTime() + (float)((_timeManager.now - DateTime.Parse(_multiSyncVideoPlayerSyncer[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId2)].GetMultiUserCurrentDateTime())).TotalSeconds));
                        else videoPlayer.SetTime(_multiSyncVideoPlayerSyncer[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId2)].GetMultiUserCurrentTime());
                    }   
                    syncerPlayerId2 = -1;
                }
                else
                {
                    int sendPlayerId_tmp = -1;
                    sendPlayerId_tmp = playerManagerVideoStopPlaySync.GetSendPlayerId();
                    if (sendPlayerId_tmp != -1)//リクエストを受信したときの処理
                    {
                        syncerPlayerId2 = sendPlayerId_tmp;
                        waitingForSynchronizationTimeCount2 = waitingForSynchronizationTime;
                    }
                }
            }
            else
            {
                if (overlayUI != null) overlayUI.SetInfomation(SyncingText);
                waitingForSynchronizationTimeCount2 -= Time.deltaTime;
            }

            /*他プレイヤーからの再生位置同期リクエストを待機*/
/*            int sendPlayerId_tmp3 = -1;
            sendPlayerId_tmp3 = playerManagerVideoTimeSync.GetSendPlayerId();
            if (sendPlayerId_tmp3 != -1)//リクエストを受信したときの処理
            {
                videoPlayer.SetTime(multiUserCurrentTime[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId3)]);
            }*/

            if(waitingForSynchronizationTimeCount3 <= 0.0f)
            {
                if(syncerPlayerId3 != -1)
                {
                    //videoPlayer.SetTime(multiUserCurrentTime[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId3)]);
                    if(_timeManager != null) videoPlayer.SetTime(_multiSyncVideoPlayerSyncer[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId3)].GetMultiUserCurrentTime() + (float)((_timeManager.now - DateTime.Parse(_multiSyncVideoPlayerSyncer[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId3)].GetMultiUserCurrentDateTime())).TotalSeconds));
                    else videoPlayer.SetTime(_multiSyncVideoPlayerSyncer[playerManagerVideoTimeSync.GetPlayerIndexFromPlayerId(syncerPlayerId3)].GetMultiUserCurrentTime());
                    syncerPlayerId3 = -1;
                }
                else
                {
                    int sendPlayerId_tmp3 = -1;
                    sendPlayerId_tmp3 = playerManagerVideoTimeSync.GetSendPlayerId();
                    if (sendPlayerId_tmp3 != -1)//リクエストを受信したときの処理
                    {
                        syncerPlayerId3 = sendPlayerId_tmp3;
                        waitingForSynchronizationTimeCount3 = waitingForSynchronizationTime;
                    }
                }
            }
            else
            {
                if (overlayUI != null) overlayUI.SetInfomation(SyncingText);
                waitingForSynchronizationTimeCount3 -= Time.deltaTime;
            }
        }

        /*public void Sync(){
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            RequestSerialization();
        }*/

        public void LoadVideo()
        {
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            isFinish = false;
            if (ExResourceLoadManager != null && !ExResourceLoadManager.GetIsFinish())
            {
                isError = true;
                return;
            }
            if (waitForResponse){
                if(waitForResponseCount > 0.0f){
                    waitForResponseCount -= Time.deltaTime;
                    return;
                }else{
                    waitForResponse = false;
                }
            } 
            if (!isRetryInfinity && retryCount >= retryCountMax)
            {
                StopLoad();
                return;
            }
            if (retryWaitSecondCount > 0)
            {
                retryWaitSecondCount -= Time.deltaTime;
                return;
            }
            isError = false;

            if (_multiSyncVideoPlayerSyncer != null)
            {
                int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                if (myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                {
                    _multiSyncVideoPlayerSyncer[myIndex_tmp].Set(currentUrlPc, currentUrlQuest, !isContinuous);
                }
            }
            /*int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (myIndex_tmp != -1 && multiUserUrlsPc[myIndex_tmp] != null) multiUserUrlsPc[myIndex_tmp] = currentUrlPc;
            if (myIndex_tmp != -1 && multiUserUrlsQuest[myIndex_tmp] != null) multiUserUrlsQuest[myIndex_tmp] = currentUrlQuest;
            if (myIndex_tmp != -1 && multiUserIsStreamingVideo.Length > myIndex_tmp) multiUserIsStreamingVideo[myIndex_tmp] = !isContinuous;
            //if (myIndex_tmp != -1 && multiUserIsPauseVideo[myIndex_tmp] != null) multiUserIsPauseVideo[myIndex_tmp] = isPause;
            Sync();*/
#if !UNITY_EDITOR && UNITY_ANDROID
            if (currentUrlQuest != null)
            {
                if (currentUrlQuest.Get() == "skip" || currentUrlQuest.Get() == "") StopLoad();
                else{
                waitForResponse = true;
                waitForResponseCount = waitForResponseMax;
                videoPlayer.LoadURL(currentUrlQuest);
                }
            }
#endif

#if UNITY_EDITOR || !UNITY_ANDROID
            if (currentUrlPc != null)
            {
                if (currentUrlPc.Get() == "skip" || currentUrlPc.Get() == "") StopLoad();
                else
                {
                    waitForResponse = true;
                    waitForResponseCount = waitForResponseMax;
                    videoPlayer.LoadURL(currentUrlPc);
                }
            }
#endif
        }

        public override void OnVideoReady()
        {
            waitForResponse = false;
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            retryCount = 0;
            if (_multiSyncVideoPlayerSyncer != null)
            {
                int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                if (myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                {
                    _multiSyncVideoPlayerSyncer[myIndex_tmp].Set(currentUrlPc, currentUrlQuest, !isContinuous, isPause);
                }
            }

            /*int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
            if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            if (myIndex_tmp != -1 && multiUserUrlsPc[myIndex_tmp] != null) multiUserUrlsPc[myIndex_tmp] = currentUrlPc;
            if (myIndex_tmp != -1 && multiUserUrlsQuest[myIndex_tmp] != null) multiUserUrlsQuest[myIndex_tmp] = currentUrlQuest;
            if (myIndex_tmp != -1 && multiUserIsStreamingVideo.Length > myIndex_tmp) multiUserIsStreamingVideo[myIndex_tmp] = !isContinuous;
            if (myIndex_tmp != -1 && multiUserIsPauseVideo.Length > myIndex_tmp) multiUserIsPauseVideo[myIndex_tmp] = isPause;
            Sync();*/
            if (startCurrentTime != 0.0f)
            {
                if(_timeManager != null) videoPlayer.SetTime(startCurrentTime + (float)((_timeManager.now - startCurrentDateTime).TotalSeconds)); //再生開始位置をセット
                else videoPlayer.SetTime(startCurrentTime); //再生開始位置をセット
                startCurrentTime = 0.0f;
            }
            //if (DebugText != null) DebugText.text += "\nMyIndex = "+ playerManagerVideoPlaySync.GetMyIndex() + " myUrl = " + multiUserUrlsPc[playerManagerVideoPlaySync.GetMyIndex()];
            videoPlayer.Play();
            offsetCounter = 0.0f;
            if(isPause) videoPlayer.Pause();
            FinishLoad();
        }

        public override void OnVideoError(VideoError videoError)
        {
            waitForResponse = false;
            if (forcedStop)
            {
                StopLoad();
                return;
            }
            isError = true;
            retryWaitSecondCount = retryWaitSecond;
            retryCount++;
        }

        public void AddStartCurrentTime(float value)
        {
            startCurrentTime += value;
        }

        public void SetVideoAndPlay(VRCUrl url, bool isStreaming = false, bool Unreload = false)
        {
            SetVideoAndPlay(url, url, isStreaming, Unreload);
        }

        public void SetVideoAndPlay(VRCUrl PcUrl, VRCUrl QuestUrl, bool isStreaming = false, bool Unreload = false)
        {
            //Debug.Log("SetVideoAndPlay(PcUrl = " + PcUrl + ", QuestUrl" + QuestUrl);
            currentUrlPc = PcUrl;
            currentUrlQuest = QuestUrl;
            isContinuous = !isStreaming;
            startCurrentTime = 0.0f;
            if(!Unreload) Reload();
        }

        public string GetTitleWithPlaylistIndex(int index)
        {
            if (playlistUrlIndicesTitle.Length > index) return playlistUrlIndicesTitle[index];
            else return "ERROR";
        }

        public void SetVideoAndPlayWithPlaylistIndex(int index)
        {
            if (urlList != null && playlistUrlIndicesQuest.Length > index && urlList.elementList.Length > playlistUrlIndicesQuest[index]) currentUrlQuest = urlList.elementList[playlistUrlIndicesQuest[index]];
            if (urlList != null && playlistUrlIndicesPc.Length > index && urlList.elementList.Length > playlistUrlIndicesPc[index]) currentUrlPc = urlList.elementList[playlistUrlIndicesPc[index]];
            if (isStreamingVideo.Length > index && isStreamingVideo[index]) isContinuous = false;
            else isContinuous = true;
            startCurrentTime = 0.0f;
            Reload();
        }

        public VRCUrl GetQuestUrlFromPlayListIndex(int index)
        {
            if (urlList != null && playlistUrlIndicesQuest.Length > index && urlList.elementList.Length > playlistUrlIndicesQuest[index]) return urlList.elementList[playlistUrlIndicesQuest[index]];
            return null;
        }

        public VRCUrl GetPcUrlFromPlayListIndex(int index)
        {
            if (urlList != null && playlistUrlIndicesPc.Length > index && urlList.elementList.Length > playlistUrlIndicesPc[index]) return urlList.elementList[playlistUrlIndicesPc[index]];
            return null;
        }

        public bool GetIsStreamingFromPlayListIndex(int index)
        {
            if (isStreamingVideo.Length > index && isStreamingVideo[index]) return isStreamingVideo[index];
            return false;
        }

        public bool SetVideoAndPlayWithPlayerIndex(int playerIndex) //自分に同期しようとすると失敗
        {
            if (playerIndex == playerManagerVideoPlaySync.GetMyIndex()) return false;
            if (_multiSyncVideoPlayerSyncer == null || playerIndex >= _multiSyncVideoPlayerSyncer.Length) return false;
            if (playerIndex != -1) currentUrlPc = _multiSyncVideoPlayerSyncer[playerIndex].GetMultiUserUrlsPc();
            if (playerIndex != -1) currentUrlQuest = _multiSyncVideoPlayerSyncer[playerIndex].GetMultiUserUrlsQuest();
            if (playerIndex != -1) isContinuous = !_multiSyncVideoPlayerSyncer[playerIndex].GetMultiUserIsStreamingVideo();
            if (playerIndex != -1) isPause = _multiSyncVideoPlayerSyncer[playerIndex].GetMultiUserIsPauseVideo();
            if (playerIndex != -1) startCurrentTime = _multiSyncVideoPlayerSyncer[playerIndex].GetMultiUserCurrentTime();
            if (playerIndex != -1 && _timeManager != null) startCurrentDateTime = DateTime.Parse(_multiSyncVideoPlayerSyncer[playerIndex].GetMultiUserCurrentDateTime());
            Reload();
            return true;
        }

        public void Reload()
        {
            isFinish = false;
            //waitForResponse = false;
            isFailure = false;
            retryCount = 0;

            if (_multiSyncVideoPlayerSyncer != null)
            {
                SyncAllPlayerSyncer();
                int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                if (myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                {
                    _multiSyncVideoPlayerSyncer[myIndex_tmp].Set(currentUrlPc, currentUrlQuest, !isContinuous, videoPlayer.GetTime(), _timeManager.now.ToString());
                }
            }
            /*if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
            if (myIndex_tmp != -1 && multiUserUrlsPc.Length > myIndex_tmp) multiUserUrlsPc[myIndex_tmp] = currentUrlPc;
            if (myIndex_tmp != -1 && multiUserUrlsQuest.Length > myIndex_tmp) multiUserUrlsQuest[myIndex_tmp] = currentUrlQuest;
            if (myIndex_tmp != -1 && multiUserIsStreamingVideo.Length > myIndex_tmp) multiUserIsStreamingVideo[myIndex_tmp] = !isContinuous;
            //if (myIndex_tmp != -1 && multiUserIsPauseVideo.Length > myIndex_tmp) multiUserIsPauseVideo[myIndex_tmp] = isPause;
            if (myIndex_tmp != -1 && multiUserCurrentTime.Length > myIndex_tmp) multiUserCurrentTime[myIndex_tmp] = videoPlayer.GetTime();
            if (myIndex_tmp != -1 && _timeManager != null) multiUserCurrentDateTime[myIndex_tmp] = _timeManager.now.ToString();
            Sync();*/

            LoadVideo();
        }

        public void ForceReload(){
            isError = true;
            isPause = false;
        }

        public void Stop()
        {
            videoPlayer.Stop();
        }

        public void StopLoad()
        {
            /*if(waitForResponse)
            {
                Stop();
            }
            else
            {*/
                isFinish = true;
                Stop();
                isFailure = true;
                currentUrlPc = GetPcUrlFromPlayListIndex(0);
                currentUrlQuest = GetQuestUrlFromPlayListIndex(0);
                isContinuous = !GetIsStreamingFromPlayListIndex(0);
                //isError = false;

                //停止時に非アクティブにする処理(実装中止)
#if !UNITY_EDITOR && UNITY_ANDROID
            //if (currentUrlQuest != null && (currentUrlQuest.Get() == "skip" || currentUrlQuest.Get() == "")) this.gameObject.SetActive(false);
#endif

#if UNITY_EDITOR || !UNITY_ANDROID
                //if (currentUrlPc != null && (currentUrlPc.Get() == "skip" || currentUrlPc.Get() == "")) this.gameObject.SetActive(false);
#endif
            //}
        }

        public void FinishLoad()
        {
            isFinish = true;
            isFailure = false;
        }

        public void SyncMyVideoWithPlayerId(int playerId)
        {
            SyncRequestSyncerFromPlayerId(Networking.LocalPlayer.playerId);
            playerManagerVideoPlaySync.SendRequest(playerId);
        }


        public void PrecisionSyncMyVideoWithPlayerId(int playerId)
        {
            playerManagerVideoPlayPrecisionSync.SendRequest(playerId);
        }

        public void SyncMyVideoPauseWithPlayerId(int playerId)
        {
            playerManagerVideoStopPlaySync.SendRequest(playerId);
        }

        public void SyncMyVideoTimeWithPlayerId(int playerId)
        {
            if (_multiSyncVideoPlayerSyncer != null)
            {
                int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                if (myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                {
                    _multiSyncVideoPlayerSyncer[myIndex_tmp].Set(videoPlayer.GetTime(), _timeManager.now.ToString());
                }
            }
            /*int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
            if (myIndex_tmp != -1 && multiUserCurrentTime.Length > myIndex_tmp) multiUserCurrentTime[myIndex_tmp] = videoPlayer.GetTime();
            if (myIndex_tmp != -1 && _timeManager != null) multiUserCurrentDateTime[myIndex_tmp] = _timeManager.now.ToString();
            Sync();*/
            playerManagerVideoTimeSync.SendRequest(playerId);
        }

        public void Pause(bool isChangeStatusOnly = false)
        {
            if (_multiSyncVideoPlayerSyncer != null)
            {
                int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                if (myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                {
                    _multiSyncVideoPlayerSyncer[myIndex_tmp].SetMultiUserIsPauseVideo(true);
                }
            }
            /*int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
            if (myIndex_tmp != -1 && multiUserIsPauseVideo.Length > myIndex_tmp) multiUserIsPauseVideo[myIndex_tmp] = true;
            Sync();*/

            if (!isChangeStatusOnly)
            {
                isPause = true;
                videoPlayer.Pause();
                offsetCounter = 0.0f;
                if (overlayUI != null) overlayUI.SetInfomation("一時停止しました");
            }

        }

        public void ReleasePause(bool isChangeStatusOnly = false)
        {
            if (_multiSyncVideoPlayerSyncer != null)
            {
                int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
                if (myIndex_tmp >= 0 && myIndex_tmp < _multiSyncVideoPlayerSyncer.Length)
                {
                    _multiSyncVideoPlayerSyncer[myIndex_tmp].SetMultiUserIsPauseVideo(false);
                }
            }
            /*int myIndex_tmp = playerManagerVideoPlaySync.GetMyIndex();
            if (myIndex_tmp != -1 && multiUserIsPauseVideo.Length > myIndex_tmp) multiUserIsPauseVideo[myIndex_tmp] = false;
            Sync();*/
            offsetCounter = 0.0f;

            if(!isChangeStatusOnly)
            {
                isPause = false;
                videoPlayer.Play();
                if (overlayUI != null) overlayUI.SetInfomation("再生を再開しました");
            }
        }
        public bool CheckPlayListLicense(int _playListIndex)
        {
            if(licenseKeyIndex.Length > _playListIndex)
            {
                int lki_tmp = licenseKeyIndex[_playListIndex];
                if (lki_tmp == -1) return true;//license制限なし
                if(licenseKeyObject1.Length > lki_tmp && licenseKeyObject2.Length > lki_tmp)
                {
                    if (licenseKeyObject1[lki_tmp].activeSelf && licenseKeyObject2[lki_tmp].activeSelf) return true;//licenseあり
                }
                return false;//license切れ
            }
            return true;//無効なplayListIndexの場合license上は制限なし
        }

        public void SyncRequestSyncerFromPlayerId(int playerId)
        {
            int index = playerManagerVideoPlaySync.GetPlayerIndexFromPlayerId(playerId);
            if (index < _multiSyncVideoPlayerSyncer.Length && index >= 0)
            {
                _multiSyncVideoPlayerSyncer[index].Sync();
            }
        }
        public void SyncAllPlayerSyncer()
        {
            foreach (MultiSyncVideoPlayerSyncer tmp in _multiSyncVideoPlayerSyncer) tmp.Sync();
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(MultiSyncVideoPlayer))]//拡張するクラスを指定
    public class MultiSyncVideoPlayerPlaylistGenerater : Editor
    {
        public override void OnInspectorGUI()
        {
            MultiSyncVideoPlayer _multiSyncVideoPlayer = target as MultiSyncVideoPlayer;

            //ボタンを表示
            if (GUILayout.Button("バルクプレイリストからプレイリストを作成"))
            {
                _multiSyncVideoPlayer.GeneratePlaylist();
            }
            base.OnInspectorGUI();
        }

    }
#endif
}