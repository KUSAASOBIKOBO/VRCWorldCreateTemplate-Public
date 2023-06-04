
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System;

namespace KUSAASOBIKOBO
{
    public enum ExternalResorceLoadMode{
        UNLOADING,
        LOAD_BGM,
        LOAD_MULTIBGM,
        LOAD_COMMON,
        DUPLICATE_COMMON,
        DUPLICATE_COMMON_TX1,
        DUPLICATE_COMMON_TX2,
        DUPLICATE_COMMON_TX3,
        DUPLICATE_COMMON_TX4,
        DUPLICATE_COMMON_TX5,
        DUPLICATE_COMMON_TX6,
        DUPLICATE_COMMON_TX7,
        DUPLICATE_COMMON_TX8,
        DUPLICATE_COMMON_TX9,
        DUPLICATE_COMMON_TX10,
        DUPLICATE_COMMON_TX11,
        DUPLICATE_COMMON_TX12,
        DUPLICATE_COMMON_TX13,
        DUPLICATE_COMMON_TX14,
        DUPLICATE_COMMON_TX15,
        DUPLICATE_COMMON_TX16,
        DUPLICATE_COMMON_TX17,
        DUPLICATE_COMMON_TX18,
        DUPLICATE_COMMON_TX19,
        DUPLICATE_COMMON_TX20,
        LOAD_SKYBOX,
        DUPLICATE_SKYBOX,
        DUPLICATE_SKYBOX_FRONT,
        DUPLICATE_SKYBOX_RIGHT,
        DUPLICATE_SKYBOX_BACK,
        DUPLICATE_SKYBOX_LEFT,
        DUPLICATE_SKYBOX_UP,
        DUPLICATE_SKYBOX_DOWN,
        LOAD_DATA,
        SETUP_VIDEOPLAYER,
        ONLY_LOAD_COMMON,
        ONLY_LOAD_SKYBOX,
        QUICK_ONLY_LOAD_COMMON,
        QUICK_ONLY_LOAD_SKYBOX,
        QUICK_LOAD_COMMON,
        QUICK_LOAD_SKYBOX,
        QUICK_DUPLICATE_SKYBOX,
        QUICK_DUPLICATE_SKYBOX_FRONT,
        QUICK_DUPLICATE_SKYBOX_RIGHT,
        QUICK_DUPLICATE_SKYBOX_BACK,
        QUICK_DUPLICATE_SKYBOX_LEFT,
        QUICK_DUPLICATE_SKYBOX_UP,
        QUICK_DUPLICATE_SKYBOX_DOWN,
        QUICK_DUPLICATE_COMMON,
        QUICK_DUPLICATE_COMMON_TX1,
        QUICK_DUPLICATE_COMMON_TX2,
        QUICK_DUPLICATE_COMMON_TX3,
        QUICK_DUPLICATE_COMMON_TX4,
        QUICK_DUPLICATE_COMMON_TX5,
        QUICK_DUPLICATE_COMMON_TX6,
        QUICK_DUPLICATE_COMMON_TX7,
        QUICK_DUPLICATE_COMMON_TX8,
        QUICK_DUPLICATE_COMMON_TX9,
        QUICK_DUPLICATE_COMMON_TX10,
        QUICK_DUPLICATE_COMMON_TX11,
        QUICK_DUPLICATE_COMMON_TX12,
        QUICK_DUPLICATE_COMMON_TX13,
        QUICK_DUPLICATE_COMMON_TX14,
        QUICK_DUPLICATE_COMMON_TX15,
        QUICK_DUPLICATE_COMMON_TX16,
        QUICK_DUPLICATE_COMMON_TX17,
        QUICK_DUPLICATE_COMMON_TX18,
        QUICK_DUPLICATE_COMMON_TX19,
        QUICK_DUPLICATE_COMMON_TX20,
        LOAD_SPSKYBOX,
        DUPLICATE_SPSKYBOX,
        DUPLICATE_SPSKYBOX_FRONT,
        DUPLICATE_SPSKYBOX_RIGHT,
        DUPLICATE_SPSKYBOX_BACK,
        DUPLICATE_SPSKYBOX_LEFT,
        DUPLICATE_SPSKYBOX_UP,
        DUPLICATE_SPSKYBOX_DOWN,
        QUICK_LOAD_SPSKYBOX,
        QUICK_DUPLICATE_SPSKYBOX,
        QUICK_DUPLICATE_SPSKYBOX_FRONT,
        QUICK_DUPLICATE_SPSKYBOX_RIGHT,
        QUICK_DUPLICATE_SPSKYBOX_BACK,
        QUICK_DUPLICATE_SPSKYBOX_LEFT,
        QUICK_DUPLICATE_SPSKYBOX_UP,
        QUICK_DUPLICATE_SPSKYBOX_DOWN,
        ONLY_LOAD_SPSKYBOX,
        QUICK_ONLY_LOAD_SPSKYBOX,
    }

    public enum SetupVideoType
    {
        PLAYLIST_INDEX_PLAY,
        PLAYER_INDEX_PLAY,
        PCURL_AND_QUESTURL_AND_ISSTREAM_PLAY
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalResourceLoadManager : UdonSharpBehaviour
    {
        public string loadBGMText = "BGMをロードしています...";
        public string loadMultiBGMText = "空間BGMをロードしています...";
        public string loadCommonText = "背景をロードしています...";
        public string loadSkyboxText = "空の背景をロードしています...";
        public string loadDataText = "データをロードしています...";
        public string setupVideoText = "ビデオプレイヤー処理中...";
        [Header("動画同期中テキスト")] public string SyncingText = "他のプレイヤーから動画を同期中…";

        public VRCUrlList urlList;
        public OverlayUIManager overlayUI;
        public ExternalBGMManager exBGM;
        public MultiSyncVideoPlayer _multiSyncVideoPlayer;
        public SetupVideoType _setupVideoType = SetupVideoType.PLAYLIST_INDEX_PLAY;
        public int multiSyncVideoPlayerSetupIndex=0;//ビデオプレイヤーの初期読み込みプレイリストインデックス
        public int multiSyncVideoPlayerSetupPlayerIndex = 0;
        [Header("VideoURLPc")] public VRCUrl multiSyncVideoPlayerSetupUrlPc;
        [Header("VideoURLQuest")] public VRCUrl multiSyncVideoPlayerSetupUrlQuest;
        [Header("VideoisStream")] public bool multiSyncVideoPlayerSetupIsStreaming;

        public bool reservedVideoPlay = false;
        private SetupVideoType reserveSetupVideoType = SetupVideoType.PLAYLIST_INDEX_PLAY;
        private int reserveMultiSyncVideoPlayerSetupIndex = 0;//ビデオプレイヤーの初期読み込みプレイリストインデックス
        private int reserveMultiSyncVideoPlayerSetupPlayerIndex = 0;
        private VRCUrl reserveMultiSyncVideoPlayerSetupUrlPc;
        private VRCUrl reserveMultiSyncVideoPlayerSetupUrlQuest;
        private bool reserveMultiSyncVideoPlayerSetupIsStreaming;

        public ExternalBGMManager[] exMultiBGM = new ExternalBGMManager[10];

        public DLTextureManager[] commonDLT = new DLTextureManager[20];
        public RenderTexture[] commonRenderTexture = new RenderTexture[20];
        public Texture2D[] commonTexture = new Texture2D[20];
        public Texture2D commonTexture4096;
        public bool isDuplicateCommonTexture = true;
        //private bool isFitAndroidCommonRenderTexture = false;
        public bool isCommonTexture4096Mode = false;

        public DLTextureManager[] skyboxDLT = new DLTextureManager[6];
        public RenderTexture[] skyboxRenderTexture = new RenderTexture[6];
        public Texture2D[] skyboxTexture = new Texture2D[6];
        public bool isDuplicateSkyBoxTexture = true;
        //private bool isFitAndroidSkyboxRenderTexture = false;

        public DLTextureManager[] spSkyboxDLT = new DLTextureManager[6];
        public RenderTexture[] spSkyboxRenderTexture = new RenderTexture[6];
        public Texture2D[] spSkyboxTexture = new Texture2D[6];
        public bool isDuplicateSpSkyBoxTexture = true;
        //private bool isFitAndroidSpSkyboxRenderTexture = false;

        public DLTextureManager[] dataDLT = new DLTextureManager[10];
        public LoadExternalData[] loadExData = new LoadExternalData[10];
        public bool isWaitForDataLoading = true;
        public bool isUseStringLoader = false;
        public LoadExternalDataFromStringDownloader _stringLoader;
        public int StringLoaderUrlIndex = 0;


        public ImageDownloadSafe _imageDownloader;

        public DuplicateRenderTexture textureDuplicater;

        public StartUpManager _startUpManager;

        [NonSerialized] public bool dataLoadReserved = false;
        [NonSerialized] public bool skyboxLoadReserved = false;
        [NonSerialized] public bool spSkyboxLoadReserved = false;
        [NonSerialized] public bool commonLoadReserved = false;
        [NonSerialized] public bool quickSkyboxLoadReserved = false;
        [NonSerialized] public bool quickSpSkyboxLoadReserved = false;
        [NonSerialized] public bool quickCommonLoadReserved = false;
        [NonSerialized] public bool BGMLoadReserved = false;
        [NonSerialized] public bool multiBGMLoadReserved = false;
        [NonSerialized] public int[] reservedDataUrlIndex = new int[10];
        [NonSerialized] public int[] reservedSkyboxUrlIndex = new int[6];
        [NonSerialized] public int[] reservedCommonUrlIndex = new int[20];
        [NonSerialized] public int reservedQuickSkyBoxUrlIndex = 0;
        [NonSerialized] public int reservedQuickCommonUrlIndex = 0;
        [NonSerialized] public int[] reservedBGMUrlIndex = new int[10];
        [NonSerialized] public int[] reservedMultiBGMUrlIndex = new int[10];

        [NonSerialized] public int[] reservedSpSkyboxUrlIndex = new int[6];
        [NonSerialized] public int reservedQuickSpSkyBoxUrlIndex = 0;

        public FillTheTextureWithColor fillTexture_Common;
        public FillTheTextureWithColor fillTexture_Common4096;
        public FillTheTextureWithColor fillTexture_SkyBox;
        public FillTheTextureWithColor fillTexture_SpSkyBox;

        public bool loadedSkyBox = false;
        public bool loadedSpSkyBox = false;
        public bool loadedCommon = false;
        //public int commonPreloadId = 0;
        //public int skyBoxPreloadId = 0;
        public DLTextureManager quickDuplicateTargetCommonDLT;
        public RenderTexture quickDuplicateTargetCommonRender;
        public DLTextureManager quickDuplicateTargetSkyBoxDLT;
        public DLTextureManager quickDuplicateTargetSpSkyboxDLT;
        public RenderTexture quickDuplicateTargetSkyBoxRender;
        public RenderTexture quickDuplicateTargetSpSkyBoxRender;

        public int quickLoadWait = 0;
        private int quickLoadWaitCount;
        //public float quickLoadInterval = 0.08f;

        public int quickLoadWait_LowFPSRate = 3;



        private float debugCounter;

        [Header("動画共有が来てから再生までの待機時間")] public float multiSyncVideoPlayerWaitingForSynchronizationTime = 3.0f;
        private float multiSyncVideoPlayerWaitingForSynchronizationTimeCount = 0.0f;
        private int multiSyncVideoPlayerSyncerPlayerId = -1;

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        private ExternalResorceLoadMode mode = ExternalResorceLoadMode.UNLOADING;
        bool QDC_load_success_check = false;

        void Update()
        {
            switch (mode)
            {
                case ExternalResorceLoadMode.UNLOADING:
                    if(_startUpManager.GetIsFinish())
                    {
                        if(dataLoadReserved){
                            dataLoadReserved = false;
                            LoadData(reservedDataUrlIndex[0], reservedDataUrlIndex[1], reservedDataUrlIndex[2], reservedDataUrlIndex[3], reservedDataUrlIndex[4], reservedDataUrlIndex[5], reservedDataUrlIndex[6], reservedDataUrlIndex[7], reservedDataUrlIndex[8], reservedDataUrlIndex[9]);
                        }else if(quickCommonLoadReserved){
                            quickCommonLoadReserved = false;
                            QuickLoadCommonTexture(reservedQuickCommonUrlIndex);
                        }else if(quickSkyboxLoadReserved){
                            quickSkyboxLoadReserved = false;
                            QuickLoadSkyboxTexture(reservedQuickSkyBoxUrlIndex);
                        }else if(quickSpSkyboxLoadReserved){
                            quickSpSkyboxLoadReserved = false;
                            QuickLoadSpSkyboxTexture(reservedQuickSpSkyBoxUrlIndex);
                        }else if(commonLoadReserved){
                            commonLoadReserved = false;
                            LoadCommonTexture(reservedCommonUrlIndex[0], reservedCommonUrlIndex[1], reservedCommonUrlIndex[2], reservedCommonUrlIndex[3], reservedCommonUrlIndex[4], reservedCommonUrlIndex[5], reservedCommonUrlIndex[6], reservedCommonUrlIndex[7], reservedCommonUrlIndex[8], reservedCommonUrlIndex[9], reservedCommonUrlIndex[10], reservedCommonUrlIndex[11], reservedCommonUrlIndex[12], reservedCommonUrlIndex[13], reservedCommonUrlIndex[14], reservedCommonUrlIndex[15], reservedCommonUrlIndex[16], reservedCommonUrlIndex[17], reservedCommonUrlIndex[18], reservedCommonUrlIndex[19]);
                        }else if(skyboxLoadReserved){
                            skyboxLoadReserved = false;
                            LoadSkyboxTexture(reservedSkyboxUrlIndex[0], reservedSkyboxUrlIndex[1], reservedSkyboxUrlIndex[2], reservedSkyboxUrlIndex[3], reservedSkyboxUrlIndex[4], reservedSkyboxUrlIndex[5]);
                        }else if(spSkyboxLoadReserved){
                            spSkyboxLoadReserved = false;
                            LoadSpSkyboxTexture(reservedSpSkyboxUrlIndex[0], reservedSpSkyboxUrlIndex[1], reservedSpSkyboxUrlIndex[2], reservedSpSkyboxUrlIndex[3], reservedSpSkyboxUrlIndex[4], reservedSpSkyboxUrlIndex[5]);
                        }else if(BGMLoadReserved){
                            BGMLoadReserved = false;
                            LoadBGM(reservedBGMUrlIndex[0], reservedBGMUrlIndex[1], reservedBGMUrlIndex[2], reservedBGMUrlIndex[3], reservedBGMUrlIndex[4], reservedBGMUrlIndex[5], reservedBGMUrlIndex[6], reservedBGMUrlIndex[7], reservedBGMUrlIndex[8], reservedBGMUrlIndex[9]);
                        }else if(multiBGMLoadReserved){
                            multiBGMLoadReserved = false;
                            LoadMultiBGM(reservedMultiBGMUrlIndex[0], reservedMultiBGMUrlIndex[1], reservedMultiBGMUrlIndex[2], reservedMultiBGMUrlIndex[3], reservedMultiBGMUrlIndex[4], reservedMultiBGMUrlIndex[5], reservedMultiBGMUrlIndex[6], reservedMultiBGMUrlIndex[7], reservedMultiBGMUrlIndex[8], reservedMultiBGMUrlIndex[9]);
                        }else if (reservedVideoPlay) //予約しておいたビデオ再生を行う
                        {
                            reservedVideoPlay = false;
                            _setupVideoType = reserveSetupVideoType;
                            multiSyncVideoPlayerSetupIndex = reserveMultiSyncVideoPlayerSetupIndex;
                            multiSyncVideoPlayerSetupPlayerIndex = reserveMultiSyncVideoPlayerSetupPlayerIndex;
                            multiSyncVideoPlayerSetupUrlPc = reserveMultiSyncVideoPlayerSetupUrlPc;
                            multiSyncVideoPlayerSetupUrlQuest = reserveMultiSyncVideoPlayerSetupUrlQuest;
                            multiSyncVideoPlayerSetupIsStreaming = reserveMultiSyncVideoPlayerSetupIsStreaming;
                            ReloadVideoPlayer();
                        }
                    }
                    break;
                case ExternalResorceLoadMode.LOAD_BGM:
                    if (overlayUI != null) overlayUI.SetInfomation(loadBGMText);
#if UNITY_EDITOR
                    mode = ExternalResorceLoadMode.UNLOADING; //EDITORではBGMの読み込みは行いません
#endif
#if !UNITY_EDITOR
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (exBGM != null && exBGM.isFinish)
                    {
                        if (exBGM.forcedStop)
                        {
                            if (exBGM != null)
                            {
                                exBGM.forcedStop = false;
                                exBGM.gameObject.SetActive(true);
                            }
                            exBGM.Reload();
                        }
                        else
                        {
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME BGM:"+ debugCounter;
                                debugCounter = 0;
                            }
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
#endif
                    break;
                case ExternalResorceLoadMode.LOAD_MULTIBGM:
                    if (overlayUI != null) overlayUI.SetInfomation(loadMultiBGMText);
#if UNITY_EDITOR
                    mode = ExternalResorceLoadMode.UNLOADING; //EDITORではBGMの読み込みは行いません
#endif
#if !UNITY_EDITOR
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (exMultiBGM[0] != null && exMultiBGM[0].isFinish)
                    {
                        if (exMultiBGM[0].forcedStop)
                        {
                            foreach (ExternalBGMManager tmp in exMultiBGM)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                }
                            }
                            exMultiBGM[0].Reload();
                        }
                        else
                        {
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME MULTIBGM:" + debugCounter;
                                debugCounter = 0;
                            }
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
#endif
                    break;
                case ExternalResorceLoadMode.LOAD_COMMON:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (commonDLT[0] != null && commonDLT[0].isFinish)
                    {
                        if (commonDLT[0].forcedStop)
                        {
                            foreach (DLTextureManager tmp in commonDLT)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                    tmp.loaded = false;
                                }
                            }
                            commonDLT[0].Reload();
                        }
                        else
                        {
                            if (isDuplicateCommonTexture)
                            {
                                if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                                if (textureDuplicater != null && textureDuplicater != null && commonRenderTexture[0] != null && commonTexture[0] != null && !commonDLT[0].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[0], commonTexture[0]);
                                mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX1;
                            }
                            else
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME COMMON:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            } 
                        }
                    }
                    else
                    {
                        //ロード中
                        if (overlayUI != null)
                        {
                            int tmp_counter = 0;
                            foreach (DLTextureManager tmp in commonDLT)
                            {
                                tmp_counter++;
                                if (tmp != null && !tmp.loaded)
                                {
                                    break;
                                }
                            }
                            overlayUI.SetInfomation(loadCommonText + "　" + tmp_counter + "/" + commonDLT.Length);
                        }
                    }
                    break;
                case ExternalResorceLoadMode.QUICK_LOAD_COMMON:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (quickDuplicateTargetCommonDLT != null && quickDuplicateTargetCommonDLT.isFinish)
                    {
                        if (quickDuplicateTargetCommonDLT.forcedStop)
                        {
                            if (quickDuplicateTargetCommonDLT != null)
                            {
                                quickDuplicateTargetCommonDLT.forcedStop = false;
                                quickDuplicateTargetCommonDLT.gameObject.SetActive(true);
                                quickDuplicateTargetCommonDLT.loaded = false;
                            }
                            quickDuplicateTargetCommonDLT.Reload();
                            //quickLoadWait = (int)(1.0f / Time.deltaTime);
                            if (Time.deltaTime >= 0.03f)
                            {
                                quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                            }
                            else
                            {
                                quickLoadWaitCount = quickLoadWait;
                            }

                        }
                        else
                        {
                            if (isDuplicateCommonTexture)
                            {
                                if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                                if(quickLoadWaitCount > 0)
                                {    
                                    quickLoadWaitCount--;
                                    return;
                                }
                                /*//クイックロード時にデュプリケートはしない
                                if (textureDuplicater != null && textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[0] != null && !quickDuplicateTargetCommonDLT.IsSkiped()) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[0]);*/
                                mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON;
                                //quickLoadWait = (int)(1.0f / Time.deltaTime);
                                if (Time.deltaTime >= 0.03f)
                                {
                                    quickLoadWaitCount = quickLoadWait * quickLoadWait_LowFPSRate;
                                }
                                else
                                {
                                    quickLoadWaitCount = quickLoadWait;
                                }
                            }
                            else
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME COMMON:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            } 
                        }
                    }
                    else
                    {
                        //ロード中
                        if (overlayUI != null)
                        {
                            int tmp_counter = 0;
                            foreach (DLTextureManager tmp in commonDLT)
                            {
                                tmp_counter++;
                                if (tmp != null && !tmp.loaded)
                                {
                                    break;
                                }
                            }
                            overlayUI.SetInfomation(loadCommonText + "　" + tmp_counter + "/" + commonDLT.Length);
                        }
                    }
                    break;
                case ExternalResorceLoadMode.ONLY_LOAD_COMMON:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (commonDLT[0] != null && commonDLT[0].isFinish)
                    {
                        if (commonDLT[0].forcedStop)
                        {
                            foreach (DLTextureManager tmp in commonDLT)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                    tmp.loaded = false;
                                }
                            }
                            commonDLT[0].Reload();
                        }
                        else
                        {
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME COMMON:" + debugCounter;
                                debugCounter = 0;
                            }
                            loadedCommon = true;
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
                    break;

                case ExternalResorceLoadMode.QUICK_ONLY_LOAD_COMMON:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (quickDuplicateTargetCommonDLT != null && quickDuplicateTargetCommonDLT.isFinish)
                    {
                        if (quickDuplicateTargetCommonDLT.forcedStop)
                        {
                            if (quickDuplicateTargetCommonDLT != null)
                            {
                                quickDuplicateTargetCommonDLT.forcedStop = false;
                                quickDuplicateTargetCommonDLT.gameObject.SetActive(true);
                                quickDuplicateTargetCommonDLT.loaded = false;
                            }
                            quickDuplicateTargetCommonDLT.Reload();
                            //quickLoadWait = (int)(1.0f / Time.deltaTime);
                            if (Time.deltaTime >= 0.03f)
                            {
                                quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                            }
                            else
                            {
                                quickLoadWaitCount = quickLoadWait;
                            }

                        }
                        else
                        {
                            if(quickLoadWaitCount > 0)
                            {
                                quickLoadWaitCount--;
                                return;
                            }
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME COMMON:" + debugCounter;
                                debugCounter = 0;
                            }
                            loadedCommon = true;
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
                    break;
                case ExternalResorceLoadMode.LOAD_DATA:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    //if (DebugText != null && dataDLT[0].isFinish) DebugText.text = "debugCounter:" + debugCounter + dataDLT[0].isFinish;
                    if (isUseStringLoader)
                    {
                        if(_stringLoader != null)
                        {
                            if (_stringLoader.isFinish)
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME DATA:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            }
                        }
                    }
                    else
                    {
                        if (dataDLT[0] != null && dataDLT[0].isFinish)
                        {
                            if (dataDLT[0].forcedStop)
                            {
                                if (DebugText != null) DebugText.text += "\ndataDLT[0] forcedStop FINISH " + dataDLT[0];
                                foreach (DLTextureManager tmp in dataDLT)
                                {
                                    if (tmp != null)
                                    {
                                        tmp.forcedStop = false;
                                        tmp.gameObject.SetActive(true);
                                        tmp.loaded = false;
                                    }
                                }
                                dataDLT[0].Reload();
                            }
                            else
                            {
                                if (DebugText != null) DebugText.text += "\ndataDLT[0] FINISH";
                                bool checkFinish = true;

                                foreach (LoadExternalData tmp in loadExData)
                                {
                                    if (tmp != null && !tmp.isFinish)
                                    {
                                        checkFinish = false;
                                        break;
                                    }
                                }
                                if (!isWaitForDataLoading) checkFinish = true;
                                if (checkFinish)
                                {
                                    if (DebugText != null)
                                    {
                                        DebugText.text += "\nLOADINGTIME DATA:" + debugCounter;
                                        debugCounter = 0;
                                    }
                                    mode = ExternalResorceLoadMode.UNLOADING;
                                }
                            }
                        }
                        else
                        {
                            //ロード中
                            if (overlayUI != null)
                            {
                                int tmp_counter = 0;
                                foreach (DLTextureManager tmp in dataDLT)
                                {
                                    tmp_counter++;
                                    if (tmp != null && !tmp.loaded)
                                    {
                                        break;
                                    }
                                }
                                overlayUI.SetInfomation(loadDataText + "　" + tmp_counter + "/" + dataDLT.Length);
                            }
                        }
                    }
                    break;

                case ExternalResorceLoadMode.LOAD_SKYBOX:
                    if (overlayUI != null) overlayUI.SetInfomation(loadSkyboxText);
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (skyboxDLT[0] != null && skyboxDLT[0].isFinish)
                    {
                        if (skyboxDLT[0].forcedStop)
                        {
                            foreach (DLTextureManager tmp in skyboxDLT)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                    tmp.loaded = false;
                                }
                            }
                            skyboxDLT[0].Reload();
                        }
                        else
                        {
                            if (isDuplicateSkyBoxTexture)
                            {
                                if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                                if (textureDuplicater != null && textureDuplicater != null && skyboxRenderTexture[0] != null && skyboxTexture[0] != null && !skyboxDLT[0].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[0], skyboxTexture[0]);
                                mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_FRONT;
                            }
                            else
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            }
                        }
                    }
                    else
                    {
                        //ロード中
                        if (overlayUI != null)
                        {
                            int tmp_counter = 0;
                            foreach (DLTextureManager tmp in skyboxDLT)
                            {
                                tmp_counter++;
                                if (tmp != null && !tmp.loaded)
                                {
                                    break;
                                }
                            }
                            overlayUI.SetInfomation(loadSkyboxText + "　" + tmp_counter + "/" + skyboxDLT.Length);
                        }
                    }
                    break;
                case ExternalResorceLoadMode.QUICK_LOAD_SKYBOX:
                    if (overlayUI != null) overlayUI.SetInfomation(loadSkyboxText);
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (quickDuplicateTargetSkyBoxDLT != null && quickDuplicateTargetSkyBoxDLT.isFinish)
                    {
                        if (quickDuplicateTargetSkyBoxDLT.forcedStop)
                        {
                            quickDuplicateTargetSkyBoxDLT.forcedStop = false;
                            quickDuplicateTargetSkyBoxDLT.gameObject.SetActive(true);
                            quickDuplicateTargetSkyBoxDLT.loaded = false;
                            quickDuplicateTargetSkyBoxDLT.Reload();
                            //quickLoadWait = (int)(1.0f / Time.deltaTime);
                            if (Time.deltaTime >= 0.03f)
                            {
                                quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                            }
                            else
                            {
                                quickLoadWaitCount = quickLoadWait;
                            }

                        }
                        else
                        {
                            if (isDuplicateSkyBoxTexture)
                            {
                                if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                                if(quickLoadWaitCount > 0)
                                {
                                    
                                    quickLoadWaitCount--;
                                    return;
                                }
                                if (textureDuplicater != null && textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[0] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                                {
                                    textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[0]);
                                } 
                                mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX;
                                //quickLoadWait = (int)(1.0f / Time.deltaTime);
                                if (Time.deltaTime >= 0.03f)
                                {
                                    quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                                }
                                else
                                {
                                    quickLoadWaitCount = quickLoadWait;
                                }

                            }
                            else
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            }
                        }
                    }
                    else
                    {
                        //ロード中
                        if (overlayUI != null)
                        {
                            int tmp_counter = 0;
                            foreach (DLTextureManager tmp in skyboxDLT)
                            {
                                tmp_counter++;
                                if (tmp != null && !tmp.loaded)
                                {
                                    break;
                                }
                            }
                            overlayUI.SetInfomation(loadSkyboxText + "　" + tmp_counter + "/" + skyboxDLT.Length);
                        }
                    }
                    break;
                case ExternalResorceLoadMode.ONLY_LOAD_SKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (skyboxDLT[0] != null && skyboxDLT[0].isFinish)
                    {
                        if (skyboxDLT[0].forcedStop)
                        {
                            foreach (DLTextureManager tmp in skyboxDLT)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                    tmp.loaded = false;
                                }
                            }
                            skyboxDLT[0].Reload();
                        }
                        else
                        {
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                debugCounter = 0;
                            }
                            loadedSkyBox = true;
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
                    break;
                case ExternalResorceLoadMode.QUICK_ONLY_LOAD_SKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (quickDuplicateTargetSkyBoxDLT != null && quickDuplicateTargetSkyBoxDLT.isFinish)
                    {
                        if (quickDuplicateTargetSkyBoxDLT.forcedStop)
                        {
                            quickDuplicateTargetSkyBoxDLT.forcedStop = false;
                            quickDuplicateTargetSkyBoxDLT.gameObject.SetActive(true);
                            quickDuplicateTargetSkyBoxDLT.loaded = false;
                            quickDuplicateTargetSkyBoxDLT.Reload();
                            //quickLoadWait = (int)(1.0f / Time.deltaTime);
                            if (Time.deltaTime >= 0.03f)
                            {
                                quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                            }
                            else
                            {
                                quickLoadWaitCount = quickLoadWait;
                            }

                        }
                        else
                        {
                            if(quickLoadWaitCount > 0)
                                {
                                    quickLoadWaitCount--;
                                    return;
                                }
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                debugCounter = 0;
                            }
                            loadedSkyBox = true;
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
                    break;
                case ExternalResorceLoadMode.SETUP_VIDEOPLAYER:
                    if (overlayUI != null) overlayUI.SetInfomation(setupVideoText);
#if UNITY_EDITOR
                    mode = ExternalResorceLoadMode.UNLOADING; //EDITORでは動画プレイヤーの読み込みを行いません
#endif
#if !UNITY_EDITOR
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (_multiSyncVideoPlayer != null && _multiSyncVideoPlayer.isFinish)
                    {
                        if (_multiSyncVideoPlayer.forcedStop)
                        {
                            if (_multiSyncVideoPlayer != null)
                            {
                                _multiSyncVideoPlayer.forcedStop = false;
                                _multiSyncVideoPlayer.gameObject.SetActive(true);
                                switch (_setupVideoType)
                                {
                                    case SetupVideoType.PLAYLIST_INDEX_PLAY:
                                        _multiSyncVideoPlayer.SetVideoAndPlayWithPlaylistIndex(multiSyncVideoPlayerSetupIndex);
                                        break;
                                    case SetupVideoType.PLAYER_INDEX_PLAY:
                                        _multiSyncVideoPlayer.SetVideoAndPlayWithPlayerIndex(multiSyncVideoPlayerSetupPlayerIndex);
                                        break;
                                    case SetupVideoType.PCURL_AND_QUESTURL_AND_ISSTREAM_PLAY:
                                        _multiSyncVideoPlayer.SetVideoAndPlay(multiSyncVideoPlayerSetupUrlPc, multiSyncVideoPlayerSetupUrlQuest, multiSyncVideoPlayerSetupIsStreaming);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME VIDEOPLAYER:" + debugCounter;
                                debugCounter = 0;
                            }
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
#endif
                    break;
                case ExternalResorceLoadMode.LOAD_SPSKYBOX:
                    if (overlayUI != null) overlayUI.SetInfomation(loadSkyboxText);
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (spSkyboxDLT[0] != null && spSkyboxDLT[0].isFinish)
                    {
                        if (spSkyboxDLT[0].forcedStop)
                        {
                            foreach (DLTextureManager tmp in spSkyboxDLT)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                    tmp.loaded = false;
                                }
                            }
                            spSkyboxDLT[0].Reload();
                        }
                        else
                        {
                            if (isDuplicateSpSkyBoxTexture)
                            {
                                if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                                if (textureDuplicater != null && textureDuplicater != null && spSkyboxRenderTexture[0] != null && spSkyboxTexture[0] != null && !spSkyboxDLT[0].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[0], spSkyboxTexture[0]);
                                mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_FRONT;
                            }
                            else
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            }
                        }
                    }
                    else
                    {
                        //ロード中
                        if (overlayUI != null)
                        {
                            int tmp_counter = 0;
                            foreach (DLTextureManager tmp in spSkyboxDLT)
                            {
                                tmp_counter++;
                                if (tmp != null && !tmp.loaded)
                                {
                                    break;
                                }
                            }
                            overlayUI.SetInfomation(loadSkyboxText + "　" + tmp_counter + "/" + spSkyboxDLT.Length);
                        }
                    }
                    break;
                case ExternalResorceLoadMode.QUICK_LOAD_SPSKYBOX:
                    if (overlayUI != null) overlayUI.SetInfomation(loadSkyboxText);
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (quickDuplicateTargetSpSkyboxDLT != null && quickDuplicateTargetSpSkyboxDLT.isFinish)
                    {
                        if (quickDuplicateTargetSpSkyboxDLT.forcedStop)
                        {
                            quickDuplicateTargetSpSkyboxDLT.forcedStop = false;
                            quickDuplicateTargetSpSkyboxDLT.gameObject.SetActive(true);
                            quickDuplicateTargetSpSkyboxDLT.loaded = false;
                            quickDuplicateTargetSpSkyboxDLT.Reload();
                            //quickLoadWait = (int)(1.0f / Time.deltaTime);
                            if (Time.deltaTime >= 0.03f)
                            {
                                quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                            }
                            else
                            {
                                quickLoadWaitCount = quickLoadWait;
                            }

                        }
                        else
                        {
                            if (isDuplicateSpSkyBoxTexture)
                            {
                                if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                                if(quickLoadWaitCount > 0)
                                {
                                    
                                    quickLoadWaitCount--;
                                    return;
                                }
                                if (textureDuplicater != null && textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && spSkyboxTexture[0] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                                {
                                    textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, spSkyboxTexture[0]);
                                } 
                                mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX;
                                //quickLoadWait = (int)(1.0f / Time.deltaTime);
                                if (Time.deltaTime >= 0.03f)
                                {
                                    quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                                }
                                else
                                {
                                    quickLoadWaitCount = quickLoadWait;
                                }

                            }
                            else
                            {
                                if (DebugText != null)
                                {
                                    DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                    debugCounter = 0;
                                }
                                mode = ExternalResorceLoadMode.UNLOADING;
                            }
                        }
                    }
                    else
                    {
                        //ロード中
                        if (overlayUI != null)
                        {
                            int tmp_counter = 0;
                            foreach (DLTextureManager tmp in spSkyboxDLT)
                            {
                                tmp_counter++;
                                if (tmp != null && !tmp.loaded)
                                {
                                    break;
                                }
                            }
                            overlayUI.SetInfomation(loadSkyboxText + "　" + tmp_counter + "/" + spSkyboxDLT.Length);
                        }
                    }
                    break;
                case ExternalResorceLoadMode.ONLY_LOAD_SPSKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (spSkyboxDLT[0] != null && spSkyboxDLT[0].isFinish)
                    {
                        if (spSkyboxDLT[0].forcedStop)
                        {
                            foreach (DLTextureManager tmp in spSkyboxDLT)
                            {
                                if (tmp != null)
                                {
                                    tmp.forcedStop = false;
                                    tmp.gameObject.SetActive(true);
                                    tmp.loaded = false;
                                }
                            }
                            spSkyboxDLT[0].Reload();
                        }
                        else
                        {
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                debugCounter = 0;
                            }
                            loadedSpSkyBox = true;
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
                    break;
                case ExternalResorceLoadMode.QUICK_ONLY_LOAD_SPSKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (quickDuplicateTargetSpSkyboxDLT != null && quickDuplicateTargetSpSkyboxDLT.isFinish)
                    {
                        if (quickDuplicateTargetSpSkyboxDLT.forcedStop)
                        {
                            quickDuplicateTargetSpSkyboxDLT.forcedStop = false;
                            quickDuplicateTargetSpSkyboxDLT.gameObject.SetActive(true);
                            quickDuplicateTargetSpSkyboxDLT.loaded = false;
                            quickDuplicateTargetSpSkyboxDLT.Reload();
                            //quickLoadWait = (int)(1.0f / Time.deltaTime);
                            if (Time.deltaTime >= 0.03f)
                            {
                                quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                            }
                            else
                            {
                                quickLoadWaitCount = quickLoadWait;
                            }

                        }
                        else
                        {
                            if(quickLoadWaitCount > 0)
                                {
                                    quickLoadWaitCount--;
                                    return;
                                }
                            if (DebugText != null)
                            {
                                DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                                debugCounter = 0;
                            }
                            loadedSpSkyBox = true;
                            mode = ExternalResorceLoadMode.UNLOADING;
                        }
                    }
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && skyboxRenderTexture[0] != null && skyboxTexture[0] != null && !skyboxDLT[0].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[0], skyboxTexture[0]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_FRONT;
                    break;
                case ExternalResorceLoadMode.DUPLICATE_SKYBOX_FRONT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && skyboxRenderTexture[1] != null && skyboxTexture[1] != null && !skyboxDLT[1].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[1], skyboxTexture[1]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_RIGHT;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SKYBOX_RIGHT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && skyboxRenderTexture[2] != null && skyboxTexture[2] != null && !skyboxDLT[2].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[2], skyboxTexture[2]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_BACK;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SKYBOX_BACK:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && skyboxRenderTexture[3] != null && skyboxTexture[3] != null && !skyboxDLT[3].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[3], skyboxTexture[3]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_LEFT;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SKYBOX_LEFT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && skyboxRenderTexture[4] != null && skyboxTexture[4] != null && !skyboxDLT[4].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[4], skyboxTexture[4]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_UP;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SKYBOX_UP:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && skyboxRenderTexture[5] != null && skyboxTexture[5] != null && !skyboxDLT[5].IsSkiped()) textureDuplicater.Duplicate(skyboxRenderTexture[5], skyboxTexture[5]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SKYBOX_DOWN;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SKYBOX_DOWN:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (DebugText != null)
                    {
                        DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                        debugCounter = 0;
                    }
                    mode = ExternalResorceLoadMode.UNLOADING;
                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 0, true);
                    else QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 0);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[0] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[0]);
                    } 
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_FRONT;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;
                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_FRONT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 1, true);
                    else QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 1);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[1] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[1]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_RIGHT;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_RIGHT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 2, true);
                    else QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 2);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[2] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[2]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_BACK;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_BACK:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 3, true);
                    else QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 3);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[3] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[3]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_LEFT;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_LEFT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 4, true);
                    else QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 4);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[4] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[4]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_UP;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_UP:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 5, true);
                    else QDC_load_success_check = quickDuplicateTargetSkyBoxDLT.ChangeTextureIndex(6, 5);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSkyBoxRender != null && skyboxTexture[5] != null && !quickDuplicateTargetSkyBoxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSkyBoxRender, skyboxTexture[5]);
                    }   
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_DOWN;
                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SKYBOX_DOWN:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (DebugText != null)
                    {
                        DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                        debugCounter = 0;
                    }
                    mode = ExternalResorceLoadMode.UNLOADING;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && spSkyboxRenderTexture[0] != null && spSkyboxTexture[0] != null && !spSkyboxDLT[0].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[0], spSkyboxTexture[0]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_FRONT;
                    break;
                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_FRONT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && spSkyboxRenderTexture[1] != null && spSkyboxTexture[1] != null && !spSkyboxDLT[1].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[1], spSkyboxTexture[1]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_RIGHT;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_RIGHT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && spSkyboxRenderTexture[2] != null && spSkyboxTexture[2] != null && !spSkyboxDLT[2].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[2], spSkyboxTexture[2]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_BACK;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_BACK:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && spSkyboxRenderTexture[3] != null && spSkyboxTexture[3] != null && !spSkyboxDLT[3].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[3], spSkyboxTexture[3]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_LEFT;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_LEFT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && spSkyboxRenderTexture[4] != null && spSkyboxTexture[4] != null && !spSkyboxDLT[4].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[4], spSkyboxTexture[4]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_UP;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_UP:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && spSkyboxRenderTexture[5] != null && spSkyboxTexture[5] != null && !spSkyboxDLT[5].IsSkiped()) textureDuplicater.Duplicate(spSkyboxRenderTexture[5], spSkyboxTexture[5]);
                    mode = ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_DOWN;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_SPSKYBOX_DOWN:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (DebugText != null)
                    {
                        DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                        debugCounter = 0;
                    }
                    mode = ExternalResorceLoadMode.UNLOADING;
                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 0, true);
                    else QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 0);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSpSkyBoxRender != null && spSkyboxTexture[0] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSpSkyBoxRender, spSkyboxTexture[0]);
                    } 
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_FRONT;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;
                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_FRONT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 1, true);
                    else QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 1);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSpSkyBoxRender != null && spSkyboxTexture[1] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSpSkyBoxRender, spSkyboxTexture[1]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_RIGHT;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_RIGHT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 2, true);
                    else QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 2);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSpSkyBoxRender != null && spSkyboxTexture[2] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSpSkyBoxRender, spSkyboxTexture[2]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_BACK;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_BACK:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 3, true);
                    else QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 3);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSpSkyBoxRender != null && spSkyboxTexture[3] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSpSkyBoxRender, spSkyboxTexture[3]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_LEFT;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_LEFT:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 4, true);
                    else QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 4);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSpSkyBoxRender != null && spSkyboxTexture[4] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSpSkyBoxRender, spSkyboxTexture[4]);
                    }                     
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_UP;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_UP:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 5, true);
                    else QDC_load_success_check = quickDuplicateTargetSpSkyboxDLT.ChangeTextureIndex(6, 5);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetSpSkyBoxRender != null && spSkyboxTexture[5] != null && !quickDuplicateTargetSpSkyboxDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetSpSkyBoxRender, spSkyboxTexture[5]);
                    }   
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_DOWN;
                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_SPSKYBOX_DOWN:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (DebugText != null)
                    {
                        DebugText.text += "\nLOADINGTIME SKYBOX:" + debugCounter;
                        debugCounter = 0;
                    }
                    mode = ExternalResorceLoadMode.UNLOADING;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[0] != null && commonTexture[0] != null && !commonDLT[0].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[0], commonTexture[0]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX1;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX1:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[1] != null && commonTexture[1] != null && !commonDLT[1].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[1], commonTexture[1]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX2;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX2:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[2] != null && commonTexture[2] != null && !commonDLT[2].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[2], commonTexture[2]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX3;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX3:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[3] != null && commonTexture[3] != null && !commonDLT[3].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[3], commonTexture[3]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX4;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX4:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[4] != null && commonTexture[4] != null && !commonDLT[4].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[4], commonTexture[4]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX5;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX5:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[5] != null && commonTexture[5] != null && !commonDLT[5].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[5], commonTexture[5]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX6;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX6:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[6] != null && commonTexture[6] != null && !commonDLT[6].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[6], commonTexture[6]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX7;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX7:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[7] != null && commonTexture[7] != null && !commonDLT[7].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[7], commonTexture[7]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX8;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX8:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[8] != null && commonTexture[8] != null && !commonDLT[8].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[8], commonTexture[8]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX9;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX9:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[9] != null && commonTexture[9] != null && !commonDLT[9].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[9], commonTexture[9]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX10;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX10:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[10] != null && commonTexture[10] != null && !commonDLT[10].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[10], commonTexture[10]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX11;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX11:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[11] != null && commonTexture[11] != null && !commonDLT[11].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[11], commonTexture[11]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX12;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX12:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[12] != null && commonTexture[12] != null && !commonDLT[12].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[12], commonTexture[12]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX13;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX13:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[13] != null && commonTexture[13] != null && !commonDLT[13].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[13], commonTexture[13]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX14;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX14:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[14] != null && commonTexture[14] != null && !commonDLT[14].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[14], commonTexture[14]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX15;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX15:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[15] != null && commonTexture[15] != null && !commonDLT[15].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[15], commonTexture[15]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX16;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX16:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[16] != null && commonTexture[16] != null && !commonDLT[16].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[16], commonTexture[16]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX17;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX17:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[17] != null && commonTexture[17] != null && !commonDLT[17].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[17], commonTexture[17]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX18;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX18:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[18] != null && commonTexture[18] != null && !commonDLT[18].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[18], commonTexture[18]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX19;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX19:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (textureDuplicater != null && commonRenderTexture[19] != null && commonTexture[19] != null && !commonDLT[19].IsSkiped()) textureDuplicater.Duplicate(commonRenderTexture[19], commonTexture[19]);
                    mode = ExternalResorceLoadMode.DUPLICATE_COMMON_TX20;
                    break;

                case ExternalResorceLoadMode.DUPLICATE_COMMON_TX20:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (DebugText != null)
                    {
                        DebugText.text += "\nLOADINGTIME COMMON:" + debugCounter;
                        debugCounter = 0;
                    }
                    mode = ExternalResorceLoadMode.UNLOADING;
                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 0, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 0);

                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[0] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 0, 3);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[0]);

                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX1;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX1:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if(quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 1, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 1);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[1] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if(isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096,1024,1,3);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[1]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX2;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX2:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 2, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 2);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[2] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 2, 3);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[2]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX3;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX3:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 3, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 3);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[3] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 3, 3);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[3]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX4;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX4:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 4, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 4);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[4] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 0, 2);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[4]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX5;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX5:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 5, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 5);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[5] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 1, 2);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[5]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX6;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX6:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 6, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 6);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[6] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 2, 2);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[6]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX7;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX7:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 7, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 7);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[7] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 3, 2);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[7]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX8;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX8:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 8, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 8);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[8] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 0, 1);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[8]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX9;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX9:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 9, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 9);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[9] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 1, 1);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[9]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX10;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX10:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 10, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 10);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[10] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 2, 1);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[10]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX11;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX11:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 11, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 11);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[11] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 3, 1);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[11]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX12;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX12:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 12, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 12);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[12] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 0, 0);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[12]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX13;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX13:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 13, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 13);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[13] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 1, 0);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[13]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX14;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX14:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 14, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 14);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[14] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 2, 0);
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[14]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX15;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX15:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 15, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 15);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[15] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        if (isCommonTexture4096Mode && commonTexture4096 != null)
                        {
                            textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture4096, 1024, 3, 0);
                            isCommonTexture4096Mode = false; //読み終わったら4096モードはリセットする
                        }
                        else textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[15]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX16;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX16:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 16, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 16);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[16] != null && !quickDuplicateTargetCommonDLT.IsSkiped())
                    {
                        textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[16]);
                    }
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX17;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX17:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 17, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 17);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[17] != null && !quickDuplicateTargetCommonDLT.IsSkiped()) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[17]);
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX18;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX18:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 18, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 18);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[18] != null && !quickDuplicateTargetCommonDLT.IsSkiped()) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[18]);
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX19;
                    //quickLoadWait = (int)(1.0f / Time.deltaTime);
                    if (Time.deltaTime >= 0.03f)
                    {
                        quickLoadWaitCount = quickLoadWait*quickLoadWait_LowFPSRate;
                    }
                    else
                    {
                        quickLoadWaitCount = quickLoadWait;
                    }

                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX19:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (quickLoadWaitCount == quickLoadWait) QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 19, true);
                    else QDC_load_success_check = quickDuplicateTargetCommonDLT.ChangeTextureIndex(20, 19);
                    if (quickLoadWaitCount > 0)
                    {
                        if (!QDC_load_success_check) quickLoadWaitCount--;
                        else quickLoadWaitCount = 0;
                        return;
                    }
                    if (textureDuplicater != null && quickDuplicateTargetCommonRender != null && commonTexture[19] != null && !quickDuplicateTargetCommonDLT.IsSkiped()) textureDuplicater.Duplicate(quickDuplicateTargetCommonRender, commonTexture[19]);
                    mode = ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX20;
                    break;

                case ExternalResorceLoadMode.QUICK_DUPLICATE_COMMON_TX20:
                    if (DebugText != null) debugCounter += Time.deltaTime;
                    if (textureDuplicater != null && !textureDuplicater.isFinish) return;
                    if (DebugText != null)
                    {
                        DebugText.text += "\nLOADINGTIME COMMON:" + debugCounter;
                        debugCounter = 0;
                    }
                    mode = ExternalResorceLoadMode.UNLOADING;
                    break;

                default:
                    break;
            }
            
            /*ビデオプレイヤーの他プレイヤーからの再生リクエストを監視*/
            if (multiSyncVideoPlayerWaitingForSynchronizationTimeCount <= 0.0f)
            {
                if (multiSyncVideoPlayerSyncerPlayerId != -1)
                {
                    //SetupVideoPlayerWithPlayerIndex(_multiSyncVideoPlayer.playerManagerVideoPlaySync.GetPlayerIndexFromPlayerId(multiSyncVideoPlayerSyncerPlayerId));
                    ForceVideoPlayWithPlayerIndex(_multiSyncVideoPlayer.playerManagerVideoPlaySync.GetPlayerIndexFromPlayerId(multiSyncVideoPlayerSyncerPlayerId));
                    //if(!_multiSyncVideoPlayer.multiUserIsPauseVideo[_multiSyncVideoPlayer.playerManagerVideoPlaySync.GetPlayerIndexFromPlayerId(multiSyncVideoPlayerSyncerPlayerId)]) _multiSyncVideoPlayer.AddStartCurrentTime(multiSyncVideoPlayerWaitingForSynchronizationTime);
                    multiSyncVideoPlayerSyncerPlayerId = -1;
                }
                else
                {
                    int sendPlayerId_tmp = -1;
                    sendPlayerId_tmp = _multiSyncVideoPlayer.playerManagerVideoPlaySync.GetSendPlayerId();
                    if (sendPlayerId_tmp != -1)//再生リクエストを受信したときの処理
                    {
                        _multiSyncVideoPlayer.SyncRequestSyncerFromPlayerId(sendPlayerId_tmp);
                        multiSyncVideoPlayerSyncerPlayerId = sendPlayerId_tmp;
                        multiSyncVideoPlayerWaitingForSynchronizationTimeCount = multiSyncVideoPlayerWaitingForSynchronizationTime;
                    }
                }
            }
            else
            {
                if (overlayUI != null) overlayUI.SetInfomation(SyncingText);
                multiSyncVideoPlayerWaitingForSynchronizationTimeCount -= Time.deltaTime;
            }
        }

        public void ForceLoadSkyboxWithReserved()
        {
            if(quickSpSkyboxLoadReserved){
                quickSpSkyboxLoadReserved = false;
                QuickLoadSpSkyboxTexture(reservedQuickSpSkyBoxUrlIndex);
            }else if(skyboxLoadReserved){
                skyboxLoadReserved = false;
                LoadSkyboxTexture(reservedSkyboxUrlIndex[0], reservedSkyboxUrlIndex[1], reservedSkyboxUrlIndex[2], reservedSkyboxUrlIndex[3], reservedSkyboxUrlIndex[4], reservedSkyboxUrlIndex[5]);
            }
        }

        public bool GetIsFinish()
        {
            if (mode == ExternalResorceLoadMode.UNLOADING) return true;
            return false;
        }

        public void ForceModeUNLOADING()
        {
            mode = ExternalResorceLoadMode.UNLOADING;
        }

        public bool LoadSkyboxTexture(int frontIndex = 0, int rightIndex = 0, int backIndex = 0, int leftIndex = 0, int upIndex = 0, int downIndex = 0, bool isForce = false)
        {
            if (!ReloadSkyboxTexture()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (skyboxDLT[0] != null && skyboxDLT[0].urls[0] != null && urlList.elementList[frontIndex] != null) skyboxDLT[0].urls[0] = urlList.elementList[frontIndex];
            if (skyboxDLT[1] != null && skyboxDLT[1].urls[0] != null && urlList.elementList[rightIndex] != null) skyboxDLT[1].urls[0] = urlList.elementList[rightIndex];
            if (skyboxDLT[2] != null && skyboxDLT[2].urls[0] != null && urlList.elementList[backIndex] != null) skyboxDLT[2].urls[0] = urlList.elementList[backIndex];
            if (skyboxDLT[3] != null && skyboxDLT[3].urls[0] != null && urlList.elementList[leftIndex] != null) skyboxDLT[3].urls[0] = urlList.elementList[leftIndex];
            if (skyboxDLT[4] != null && skyboxDLT[4].urls[0] != null && urlList.elementList[upIndex] != null) skyboxDLT[4].urls[0] = urlList.elementList[upIndex];
            if (skyboxDLT[5] != null && skyboxDLT[5].urls[0] != null && urlList.elementList[downIndex] != null) skyboxDLT[5].urls[0] = urlList.elementList[downIndex];
            return true;
        }

        public bool QuickLoadSkyboxTexture(int index = 0, bool isForce = false)
        {
            if (!QuickReloadSkyboxTexture()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (quickDuplicateTargetSkyBoxDLT != null && quickDuplicateTargetSkyBoxDLT.urls[0] != null && urlList.elementList[index] != null) quickDuplicateTargetSkyBoxDLT.urls[0] = urlList.elementList[index];
            return true;
        }

        public bool ReloadSkyboxTexture()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
#if !UNITY_EDITOR && UNITY_ANDROID
            /*if (!isFitAndroidSkyboxRenderTexture)
            {
                isFitAndroidSkyboxRenderTexture = true;
                foreach (RenderTexture tmp in skyboxRenderTexture)
                {
                    tmp.width = 1082;
                }
            }*/
#endif
            mode = ExternalResorceLoadMode.LOAD_SKYBOX;
            foreach (DLTextureManager tmp in skyboxDLT)
            {
                if (tmp != null)
                {
                    tmp.forcedStop = true;
                    tmp.gameObject.SetActive(true);
                } 
            }
            return true;
        }

        public bool QuickReloadSkyboxTexture()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
#if !UNITY_EDITOR && UNITY_ANDROID
            /*if (!isFitAndroidSkyboxRenderTexture)
            {
                isFitAndroidSkyboxRenderTexture = true;
                quickDuplicateTargetSkyBoxDLT.width = 1082;
            }*/
#endif
            mode = ExternalResorceLoadMode.QUICK_LOAD_SKYBOX;
            if (quickDuplicateTargetSkyBoxDLT != null)
            {
                quickDuplicateTargetSkyBoxDLT.forcedStop = true;
                quickDuplicateTargetSkyBoxDLT.gameObject.SetActive(true);
            } 
            return true;
        }


        public bool LoadSpSkyboxTexture(int frontIndex = 0, int rightIndex = 0, int backIndex = 0, int leftIndex = 0, int upIndex = 0, int downIndex = 0, bool isForce = false)
        {
            if (!ReloadSpSkyboxTexture()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (spSkyboxDLT[0] != null && spSkyboxDLT[0].urls[0] != null && urlList.elementList[frontIndex] != null) spSkyboxDLT[0].urls[0] = urlList.elementList[frontIndex];
            if (spSkyboxDLT[1] != null && spSkyboxDLT[1].urls[0] != null && urlList.elementList[rightIndex] != null) spSkyboxDLT[1].urls[0] = urlList.elementList[rightIndex];
            if (spSkyboxDLT[2] != null && spSkyboxDLT[2].urls[0] != null && urlList.elementList[backIndex] != null) spSkyboxDLT[2].urls[0] = urlList.elementList[backIndex];
            if (spSkyboxDLT[3] != null && spSkyboxDLT[3].urls[0] != null && urlList.elementList[leftIndex] != null) spSkyboxDLT[3].urls[0] = urlList.elementList[leftIndex];
            if (spSkyboxDLT[4] != null && spSkyboxDLT[4].urls[0] != null && urlList.elementList[upIndex] != null) spSkyboxDLT[4].urls[0] = urlList.elementList[upIndex];
            if (spSkyboxDLT[5] != null && spSkyboxDLT[5].urls[0] != null && urlList.elementList[downIndex] != null) spSkyboxDLT[5].urls[0] = urlList.elementList[downIndex];
            return true;
        }

        public bool QuickLoadSpSkyboxTexture(int index = 0, bool isForce = false)
        {
            if (!QuickReloadSpSkyboxTexture()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (quickDuplicateTargetSpSkyboxDLT != null && quickDuplicateTargetSpSkyboxDLT.urls[0] != null && urlList.elementList[index] != null) quickDuplicateTargetSpSkyboxDLT.urls[0] = urlList.elementList[index];
            return true;
        }

        public bool ReloadSpSkyboxTexture()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
            mode = ExternalResorceLoadMode.LOAD_SPSKYBOX;
            foreach (DLTextureManager tmp in spSkyboxDLT)
            {
                if (tmp != null)
                {
                    tmp.forcedStop = true;
                    tmp.gameObject.SetActive(true);
                } 
            }
            return true;
        }

        public bool QuickReloadSpSkyboxTexture()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
            mode = ExternalResorceLoadMode.QUICK_LOAD_SPSKYBOX;
            if (quickDuplicateTargetSpSkyboxDLT != null)
            {
                quickDuplicateTargetSpSkyboxDLT.forcedStop = true;
                quickDuplicateTargetSpSkyboxDLT.gameObject.SetActive(true);
            } 
            return true;
        }

        public bool LoadData(int data1 = 0, int data2 = 0, int data3 = 0, int data4 = 0, int data5 = 0, int data6 = 0, int data7 = 0, int data8 = 0, int data9 = 0, int data10 = 0, bool isForce = false)
        {
            if (isUseStringLoader)
            {
                if (mode != ExternalResorceLoadMode.UNLOADING) return false;
                mode = ExternalResorceLoadMode.LOAD_DATA;
                if (!isForce && (!_startUpManager.GetIsFinish())) return false;
                StringLoaderUrlIndex = data1;
                if (_stringLoader != null) _stringLoader.Load(StringLoaderUrlIndex);
            }
            else
            {
                if (!ReloadData()) return false;
                if (!isForce && (!_startUpManager.GetIsFinish())) return false;
                if (dataDLT[0] != null && dataDLT[0].urls[0] != null && urlList.elementList[data1] != null) dataDLT[0].urls[0] = urlList.elementList[data1];
                if (dataDLT[1] != null && dataDLT[1].urls[0] != null && urlList.elementList[data2] != null) dataDLT[1].urls[0] = urlList.elementList[data2];
                if (dataDLT[2] != null && dataDLT[2].urls[0] != null && urlList.elementList[data3] != null) dataDLT[2].urls[0] = urlList.elementList[data3];
                if (dataDLT[3] != null && dataDLT[3].urls[0] != null && urlList.elementList[data4] != null) dataDLT[3].urls[0] = urlList.elementList[data4];
                if (dataDLT[4] != null && dataDLT[4].urls[0] != null && urlList.elementList[data5] != null) dataDLT[4].urls[0] = urlList.elementList[data5];
                if (dataDLT[5] != null && dataDLT[5].urls[0] != null && urlList.elementList[data6] != null) dataDLT[5].urls[0] = urlList.elementList[data6];
                if (dataDLT[6] != null && dataDLT[6].urls[0] != null && urlList.elementList[data7] != null) dataDLT[6].urls[0] = urlList.elementList[data7];
                if (dataDLT[7] != null && dataDLT[7].urls[0] != null && urlList.elementList[data8] != null) dataDLT[7].urls[0] = urlList.elementList[data8];
                if (dataDLT[8] != null && dataDLT[8].urls[0] != null && urlList.elementList[data9] != null) dataDLT[8].urls[0] = urlList.elementList[data9];
                if (dataDLT[9] != null && dataDLT[9].urls[0] != null && urlList.elementList[data10] != null) dataDLT[9].urls[0] = urlList.elementList[data10];
            }
            return true;
        }

        public bool LoadData(int data1 = 0, bool isForce = false)
        {
            return LoadData(data1,0,0,0,0,0,0,0,0,0, isForce);
        }

        public bool ReloadData()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
            mode = ExternalResorceLoadMode.LOAD_DATA;
            foreach (DLTextureManager tmp in dataDLT)
            {
                if (tmp != null)
                {
                    tmp.forcedStop = true;
                    tmp.gameObject.SetActive(true);
                } 
            }
            return true;
        }

        public bool LoadCommonTexture(int tx1 = 0, int tx2 = 0, int tx3 = 0, int tx4 = 0, int tx5 = 0, int tx6 = 0, int tx7 = 0, int tx8 = 0, int tx9 = 0, int tx10 = 0, int tx11 = 0, int tx12 = 0, int tx13 = 0, int tx14 = 0, int tx15 = 0, int tx16 = 0, int tx17 = 0, int tx18 = 0, int tx19 = 0, int tx20 = 0, bool isForce = false)
        {
            if (!ReloadCommonTexture()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (commonDLT[0] != null && commonDLT[0].urls[0] != null && urlList.elementList[tx1] != null) commonDLT[0].urls[0] = urlList.elementList[tx1];
            if (commonDLT[1] != null && commonDLT[1].urls[0] != null && urlList.elementList[tx2] != null) commonDLT[1].urls[0] = urlList.elementList[tx2];
            if (commonDLT[2] != null && commonDLT[2].urls[0] != null && urlList.elementList[tx3] != null) commonDLT[2].urls[0] = urlList.elementList[tx3];
            if (commonDLT[3] != null && commonDLT[3].urls[0] != null && urlList.elementList[tx4] != null) commonDLT[3].urls[0] = urlList.elementList[tx4];
            if (commonDLT[4] != null && commonDLT[4].urls[0] != null && urlList.elementList[tx5] != null) commonDLT[4].urls[0] = urlList.elementList[tx5];
            if (commonDLT[5] != null && commonDLT[5].urls[0] != null && urlList.elementList[tx6] != null) commonDLT[5].urls[0] = urlList.elementList[tx6];
            if (commonDLT[6] != null && commonDLT[6].urls[0] != null && urlList.elementList[tx7] != null) commonDLT[6].urls[0] = urlList.elementList[tx7];
            if (commonDLT[7] != null && commonDLT[7].urls[0] != null && urlList.elementList[tx8] != null) commonDLT[7].urls[0] = urlList.elementList[tx8];
            if (commonDLT[8] != null && commonDLT[8].urls[0] != null && urlList.elementList[tx9] != null) commonDLT[8].urls[0] = urlList.elementList[tx9];
            if (commonDLT[9] != null && commonDLT[9].urls[0] != null && urlList.elementList[tx10] != null) commonDLT[9].urls[0] = urlList.elementList[tx10];
            if (commonDLT[10] != null && commonDLT[10].urls[0] != null && urlList.elementList[tx11] != null) commonDLT[10].urls[0] = urlList.elementList[tx11];
            if (commonDLT[11] != null && commonDLT[11].urls[0] != null && urlList.elementList[tx12] != null) commonDLT[11].urls[0] = urlList.elementList[tx12];
            if (commonDLT[12] != null && commonDLT[12].urls[0] != null && urlList.elementList[tx13] != null) commonDLT[12].urls[0] = urlList.elementList[tx13];
            if (commonDLT[13] != null && commonDLT[13].urls[0] != null && urlList.elementList[tx14] != null) commonDLT[13].urls[0] = urlList.elementList[tx14];
            if (commonDLT[14] != null && commonDLT[14].urls[0] != null && urlList.elementList[tx15] != null) commonDLT[14].urls[0] = urlList.elementList[tx15];
            if (commonDLT[15] != null && commonDLT[15].urls[0] != null && urlList.elementList[tx16] != null) commonDLT[15].urls[0] = urlList.elementList[tx16];
            if (commonDLT[16] != null && commonDLT[16].urls[0] != null && urlList.elementList[tx17] != null) commonDLT[16].urls[0] = urlList.elementList[tx17];
            if (commonDLT[17] != null && commonDLT[17].urls[0] != null && urlList.elementList[tx18] != null) commonDLT[17].urls[0] = urlList.elementList[tx18];
            if (commonDLT[18] != null && commonDLT[18].urls[0] != null && urlList.elementList[tx19] != null) commonDLT[18].urls[0] = urlList.elementList[tx19];
            if (commonDLT[19] != null && commonDLT[19].urls[0] != null && urlList.elementList[tx20] != null) commonDLT[19].urls[0] = urlList.elementList[tx20];
            return true;
        }

        public bool QuickLoadCommonTexture(int index = 0, bool isForce = false)
        {
            if (!QuickReloadCommonTexture()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (quickDuplicateTargetCommonDLT != null && quickDuplicateTargetCommonDLT.urls[0] != null && urlList.elementList[index] != null) quickDuplicateTargetCommonDLT.urls[0] = urlList.elementList[index];
            return true;
        }

        public bool ReloadCommonTexture()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
#if !UNITY_EDITOR && UNITY_ANDROID
            /*if (!isFitAndroidCommonRenderTexture)
            {
                isFitAndroidCommonRenderTexture = true;
                foreach (RenderTexture tmp in commonRenderTexture)
                {
                    tmp.width = 1082;
                }
            }*/
#endif
            mode = ExternalResorceLoadMode.LOAD_COMMON;
            foreach (DLTextureManager tmp in commonDLT)
            {
                if (tmp != null)
                {
                    tmp.forcedStop = true;
                    tmp.gameObject.SetActive(true);
                } 
            }
            return true;
        }


        public bool QuickReloadCommonTexture()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
#if !UNITY_EDITOR && UNITY_ANDROID
            /*if (!isFitAndroidCommonRenderTexture)
            {
                isFitAndroidCommonRenderTexture = true;
                quickDuplicateTargetCommonDLT.width = 1082;
            }*/
#endif
            mode = ExternalResorceLoadMode.QUICK_LOAD_COMMON;
            if (quickDuplicateTargetCommonDLT != null)
            {
                quickDuplicateTargetCommonDLT.forcedStop = true;
                quickDuplicateTargetCommonDLT.gameObject.SetActive(true);
            } 
            return true;
        }


        public void FillCommonTexture()
        {
            if (fillTexture_Common != null) fillTexture_Common.Fill();
            if (fillTexture_Common4096 != null) fillTexture_Common4096.Fill();
        }

        public void FillSkyBoxTexture()
        {
            if(fillTexture_SkyBox != null) fillTexture_SkyBox.Fill();
        }

        public void FillSpSkyBoxTexture()
        {
            if(fillTexture_SpSkyBox != null) fillTexture_SpSkyBox.Fill();
        }

        public bool LoadMultiBGM(int bgm1 = 0, int bgm2 = 0, int bgm3 = 0, int bgm4 = 0, int bgm5 = 0, int bgm6 = 0, int bgm7 = 0, int bgm8 = 0, int bgm9 = 0, int bgm10 = 0, bool isForce = false)
        {
            if (!ReloadMultiBGM()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (exMultiBGM[0] != null && exMultiBGM[0].urls[0] != null && urlList.elementList[bgm1] != null) exMultiBGM[0].urls[0] = urlList.elementList[bgm1];
            if (exMultiBGM[1] != null && exMultiBGM[1].urls[0] != null && urlList.elementList[bgm2] != null) exMultiBGM[1].urls[0] = urlList.elementList[bgm2];
            if (exMultiBGM[2] != null && exMultiBGM[2].urls[0] != null && urlList.elementList[bgm3] != null) exMultiBGM[2].urls[0] = urlList.elementList[bgm3];
            if (exMultiBGM[3] != null && exMultiBGM[3].urls[0] != null && urlList.elementList[bgm4] != null) exMultiBGM[3].urls[0] = urlList.elementList[bgm4];
            if (exMultiBGM[4] != null && exMultiBGM[4].urls[0] != null && urlList.elementList[bgm5] != null) exMultiBGM[4].urls[0] = urlList.elementList[bgm5];
            if (exMultiBGM[5] != null && exMultiBGM[5].urls[0] != null && urlList.elementList[bgm6] != null) exMultiBGM[5].urls[0] = urlList.elementList[bgm6];
            if (exMultiBGM[6] != null && exMultiBGM[6].urls[0] != null && urlList.elementList[bgm7] != null) exMultiBGM[6].urls[0] = urlList.elementList[bgm7];
            if (exMultiBGM[7] != null && exMultiBGM[7].urls[0] != null && urlList.elementList[bgm8] != null) exMultiBGM[7].urls[0] = urlList.elementList[bgm8];
            if (exMultiBGM[8] != null && exMultiBGM[8].urls[0] != null && urlList.elementList[bgm9] != null) exMultiBGM[8].urls[0] = urlList.elementList[bgm9];
            if (exMultiBGM[9] != null && exMultiBGM[9].urls[0] != null && urlList.elementList[bgm10] != null) exMultiBGM[9].urls[0] = urlList.elementList[bgm10];
            return true;
        }

        public bool ReloadMultiBGM()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
            mode = ExternalResorceLoadMode.LOAD_MULTIBGM;
            foreach (ExternalBGMManager tmp in exMultiBGM)
            {
                if (tmp != null)
                {
                    tmp.forcedStop = true;
                    tmp.gameObject.SetActive(true);
                } 
            }
            return true;
        }

        public void ForcedStopMultiBGM()
        {
            foreach (ExternalBGMManager tmp in exMultiBGM)
            {
                if (tmp != null)
                {
                    tmp.forcedStop = false;
                    tmp.StopLoad(false);
                }
            }
            return;
        }

        public bool LoadBGM(int bgm1 = 0, int bgm2 = 0, int bgm3 = 0, int bgm4 = 0, int bgm5 = 0, int bgm6 = 0, int bgm7 = 0, int bgm8 = 0, int bgm9 = 0, int bgm10 = 0, bool isForce = false)
        {
            if (!ReloadBGM()) return false;
            if (!isForce && (!_startUpManager.GetIsFinish())) return false;
            if (exBGM.urls[0] != null && urlList.elementList[bgm1] != null) exBGM.urls[0] = urlList.elementList[bgm1];
            if (exBGM.urls[1] != null && urlList.elementList[bgm2] != null) exBGM.urls[1] = urlList.elementList[bgm2];
            if (exBGM.urls[2] != null && urlList.elementList[bgm3] != null) exBGM.urls[2] = urlList.elementList[bgm3];
            if (exBGM.urls[3] != null && urlList.elementList[bgm4] != null) exBGM.urls[3] = urlList.elementList[bgm4];
            if (exBGM.urls[4] != null && urlList.elementList[bgm5] != null) exBGM.urls[4] = urlList.elementList[bgm5];
            if (exBGM.urls[5] != null && urlList.elementList[bgm6] != null) exBGM.urls[5] = urlList.elementList[bgm6];
            if (exBGM.urls[6] != null && urlList.elementList[bgm7] != null) exBGM.urls[6] = urlList.elementList[bgm7];
            if (exBGM.urls[7] != null && urlList.elementList[bgm8] != null) exBGM.urls[7] = urlList.elementList[bgm8];
            if (exBGM.urls[8] != null && urlList.elementList[bgm9] != null) exBGM.urls[8] = urlList.elementList[bgm9];
            if (exBGM.urls[9] != null && urlList.elementList[bgm10] != null) exBGM.urls[9] = urlList.elementList[bgm10];
           return true;
        }

        public bool ReloadBGM()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
            mode = ExternalResorceLoadMode.LOAD_BGM;
            if (exBGM != null){
                exBGM.forcedStop = true;
                exBGM.gameObject.SetActive(true);
            } 
            return true;
        }

        public void ForcedStopBGM()
        {
            if (exBGM != null)
            {
                exBGM.forcedStop = false;
                exBGM.StopLoad(false);
            }
            return;
        }

        public bool SetupVideoPlayerWithPlaylistIndex(int index, bool isForce = false)
        {
            if (!ReloadVideoPlayer() || !isForce && !_startUpManager.GetIsFinish())
            {
                reservedVideoPlay = true;
                reserveSetupVideoType = SetupVideoType.PLAYLIST_INDEX_PLAY;
                reserveMultiSyncVideoPlayerSetupIndex = index;

                /*現在の読み込みを停止*/
                if (_multiSyncVideoPlayer != null) _multiSyncVideoPlayer.forcedStop = true;
                multiSyncVideoPlayerSetupUrlPc = GetPcUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupUrlQuest = GetQuestUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupIsStreaming = GetIsStreamingFromPlayListIndex(0);
                return false;
            }
            _setupVideoType = SetupVideoType.PLAYLIST_INDEX_PLAY;
            multiSyncVideoPlayerSetupIndex = index;
            return true;
        }

        public bool SetupVideoPlayerWithPlayerIndex(int index, bool isForce = false)
        {
            if (!ReloadVideoPlayer() || !isForce && !_startUpManager.GetIsFinish())
            {
                reservedVideoPlay = true;
                reserveSetupVideoType = SetupVideoType.PLAYER_INDEX_PLAY;
                reserveMultiSyncVideoPlayerSetupPlayerIndex = index;

                /*現在の読み込みを停止*/
                if (_multiSyncVideoPlayer != null) _multiSyncVideoPlayer.forcedStop = true;
                multiSyncVideoPlayerSetupUrlPc = GetPcUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupUrlQuest = GetQuestUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupIsStreaming = GetIsStreamingFromPlayListIndex(0);
                return false;
            }
            _setupVideoType = SetupVideoType.PLAYER_INDEX_PLAY;
            multiSyncVideoPlayerSetupPlayerIndex = index;
            return true;
        }

        public bool SetupVideoPlayerWithUrl(VRCUrl PcUrl, VRCUrl QuestUrl, bool isStreaming = false, bool isForce = false)
        {
            if (!ReloadVideoPlayer() || !isForce && !_startUpManager.GetIsFinish())
            {
                reservedVideoPlay = true;
                reserveSetupVideoType = SetupVideoType.PCURL_AND_QUESTURL_AND_ISSTREAM_PLAY;
                reserveMultiSyncVideoPlayerSetupUrlPc = PcUrl;
                reserveMultiSyncVideoPlayerSetupUrlQuest = QuestUrl;
                reserveMultiSyncVideoPlayerSetupIsStreaming = isStreaming;

                /*現在の読み込みを停止*/
                if (_multiSyncVideoPlayer != null) _multiSyncVideoPlayer.forcedStop = true;
                multiSyncVideoPlayerSetupUrlPc = GetPcUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupUrlQuest = GetQuestUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupIsStreaming = GetIsStreamingFromPlayListIndex(0);
                return false;
            }
            _setupVideoType = SetupVideoType.PCURL_AND_QUESTURL_AND_ISSTREAM_PLAY;
            multiSyncVideoPlayerSetupUrlPc = PcUrl;
            multiSyncVideoPlayerSetupUrlQuest = QuestUrl;
            multiSyncVideoPlayerSetupIsStreaming = isStreaming;
            return true;
        }

        public void ForceEndVideoPlay()
        {
            reservedVideoPlay = false;
            if (_multiSyncVideoPlayer != null)
            {
                if (mode == ExternalResorceLoadMode.SETUP_VIDEOPLAYER)
                {
                    mode = ExternalResorceLoadMode.UNLOADING;
                }
                
                _multiSyncVideoPlayer.forcedStop = true; 
                multiSyncVideoPlayerSetupUrlPc = GetPcUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupUrlQuest = GetQuestUrlFromPlayListIndex(0);
                multiSyncVideoPlayerSetupIsStreaming = GetIsStreamingFromPlayListIndex(0);
                _multiSyncVideoPlayer.SetVideoAndPlay(multiSyncVideoPlayerSetupUrlPc, multiSyncVideoPlayerSetupUrlQuest, multiSyncVideoPlayerSetupIsStreaming);
                _multiSyncVideoPlayer.ForceReload();
            }
        }

        public void ForceVideoPlayWithUrl(VRCUrl PcUrl, VRCUrl QuestUrl, bool isStreaming = false)
        {
            reservedVideoPlay = false;
            if (_multiSyncVideoPlayer != null)
            {
                if (mode == ExternalResorceLoadMode.SETUP_VIDEOPLAYER)
                {
                    mode = ExternalResorceLoadMode.UNLOADING;
                }
                
                _multiSyncVideoPlayer.forcedStop = false;
                multiSyncVideoPlayerSetupUrlPc = PcUrl;
                multiSyncVideoPlayerSetupUrlQuest = QuestUrl;
                multiSyncVideoPlayerSetupIsStreaming = isStreaming;
                _multiSyncVideoPlayer.SetVideoAndPlay(multiSyncVideoPlayerSetupUrlPc, multiSyncVideoPlayerSetupUrlQuest, multiSyncVideoPlayerSetupIsStreaming, true);
                _multiSyncVideoPlayer.ForceReload();
            }
        }

        public void ForceVideoPlayWithPlaylistIndex(int index)
        {
            ForceVideoPlayWithUrl(GetPcUrlFromPlayListIndex(index), GetQuestUrlFromPlayListIndex(index), GetIsStreamingFromPlayListIndex(index));
        }

        public void ForceVideoPlayWithPlayerIndex(int index)
        {
            ForceVideoPlayWithUrl(_multiSyncVideoPlayer._multiSyncVideoPlayerSyncer[index].GetMultiUserUrlsPc(), _multiSyncVideoPlayer._multiSyncVideoPlayerSyncer[index].GetMultiUserUrlsQuest(), _multiSyncVideoPlayer._multiSyncVideoPlayerSyncer[index].GetMultiUserIsStreamingVideo());
        }

        public bool ReloadVideoPlayer()
        {
            if (mode != ExternalResorceLoadMode.UNLOADING) return false;
            mode = ExternalResorceLoadMode.SETUP_VIDEOPLAYER;
            if (_multiSyncVideoPlayer != null) _multiSyncVideoPlayer.forcedStop = true;
            return true;
        }

        public void ForcedStopVideoPlayer()
        {
            if (_multiSyncVideoPlayer != null)
            {
                _multiSyncVideoPlayer.forcedStop = false;
                _multiSyncVideoPlayer.StopLoad();
            }
            return;
        }

        public void SyncMyVideoWithPlayerId(int playerId)
        {
            _multiSyncVideoPlayer.SyncMyVideoWithPlayerId(playerId);
        }

        public void PrecisionSyncMyVideoWithPlayerId(int playerId)
        {
            _multiSyncVideoPlayer.PrecisionSyncMyVideoWithPlayerId(playerId);
        }

        public void SyncMyVideoPauseWithPlayerId(int playerId)
        {
            _multiSyncVideoPlayer.SyncMyVideoPauseWithPlayerId(playerId);
        }

        public void SyncMyVideoTimeWithPlayerId(int playerId)
        {
            _multiSyncVideoPlayer.SyncMyVideoTimeWithPlayerId(playerId);
        }

        public void VideoPause()
        {
            if(!_multiSyncVideoPlayer.isPause) _multiSyncVideoPlayer.Pause(true);
            else _multiSyncVideoPlayer.ReleasePause(true);
        }

        public void VideoMoveTime_plus30()
        {
            _multiSyncVideoPlayer.MoveTime_plus30();
        }

        public void VideoMoveTime_minus30()
        {
            _multiSyncVideoPlayer.MoveTime_minus30();
        }

        public void VideoMoveTime_plus10()
        {
            _multiSyncVideoPlayer.MoveTime_plus10();
        }

        public void VideoMoveTime_minus10()
        {
            _multiSyncVideoPlayer.MoveTime_minus10();
        }

        public VRCUrl GetQuestUrlFromPlayListIndex(int index)
        {
            return _multiSyncVideoPlayer.GetQuestUrlFromPlayListIndex(index);
        }

        public VRCUrl GetPcUrlFromPlayListIndex(int index)
        {
            return _multiSyncVideoPlayer.GetPcUrlFromPlayListIndex(index);
        }

        public bool GetIsStreamingFromPlayListIndex(int index)
        {
            return _multiSyncVideoPlayer.GetIsStreamingFromPlayListIndex(index);
        }
        
        public string GetTitleWithPlaylistIndex(int index)
        {
            return _multiSyncVideoPlayer.GetTitleWithPlaylistIndex(index);
        }

        public void ImageDownloadFromUrlIndex(int index)
        {
            if(_imageDownloader != null)
            {
                _imageDownloader.LoadFromUrlIndex(index);
            }
        }

        public bool CheckVideoPlayListLicense(int playListIndex)
        {
            if (_multiSyncVideoPlayer == null) return false;
            return _multiSyncVideoPlayer.CheckPlayListLicense(playListIndex);
        }

        public void ForceResetLoadMode(bool isSafe = true)
        {
            ForceModeUNLOADING();
            if(!isSafe)
            {
                dataLoadReserved = false;
                skyboxLoadReserved = false;
                spSkyboxLoadReserved = false;
                commonLoadReserved = false;
            }

            quickSkyboxLoadReserved = false;
            quickSpSkyboxLoadReserved = false;
            quickCommonLoadReserved = false;
            BGMLoadReserved = false;
            multiBGMLoadReserved = false;
            reservedVideoPlay = false;
            ForcedStopVideoPlayer();
            ForcedStopBGM();
            ForcedStopMultiBGM();
        }
    }
}
