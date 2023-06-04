
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    public enum StartUpStep
    {
        NONE_STEP,
        LOAD_DATA_STEP,
        ANALYZE_DATA_STEP,
        LOAD_BGM_STEP,
        LOAD_SKYBOX_STEP,
        LOAD_COMMON_STEP,
        LOAD_MULTIBGM_STEP,
        SETUP_VIDEOPLAYER_STEP,
        WARP_STEP,
        SYNCWAIT_STEP,
        FINISH_STEP
    }
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StartUpManager : UdonSharpBehaviour
    {
        StartUpStep step = StartUpStep.NONE_STEP;
        public ExternalResourceLoadManager ExResourceLoadManager;
        public TimeManager _timeManager;
        public MultiAreaManager _multiAreaManager;
        public OverlayUIManager overlayUI;
        public float fadeoutCount = 0.0f;
        public float fadeoutCountMax = 3.0f; //フェードアウト待機時間
        public float showTitleInterval = 5.0f; //タイトル表示時間
        public string loadingText = "NowLoading..."; //ロード中に出る文字
        public int showTitleImageIndex = 0; 
        public bool showTitleImageDone = false; 

        public int[] dataUrlIndex = new int[10];
        public int[] skyboxUrlIndex = new int[6];
        public int[] commonUrlIndex = new int[20];
        public int[] BGMUrlIndex = new int[10];
        public int[] multiBGMUrlIndex = new int[10];
        public int multiSyncVideoPlayerSetupIndex = 0;//ビデオプレイヤーの初期読み込みプレイリストインデックス

        public bool isQuickLoadSkyBox;
        public bool isQuickLoadCommon;

        public bool isFillColorBeforeLoad = false;

        public GameObject entryPoint;

        bool isSpawning = false;//スポーン中フラグ

        public bool isShowTitleImageOneTime = true;

        public bool isRespawnToReLoad = true;


        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        //TimeManager

        //MultiAreaManager

        //DisplayEffectManager

        void Start()
        {
            //LoadAll();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;
            LoadAll();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer && isRespawnToReLoad)
            {
                if (ExResourceLoadManager != null)
                {
                    /*現在ロード予約されている読み込みを取り消し*/
                    ExResourceLoadManager.ForceResetLoadMode(false);
                }
                LoadAll();
            }
        }

        void Update()
        {
            if (overlayUI != null)
            {
                if (overlayUI.isFadeOutFinishedTitleDisplay() && !showTitleImageDone)
                {
                    showTitleImageDone = true;
                    if(overlayUI._imageDownloadSafe != null && showTitleImageIndex == overlayUI._imageDownloadSafe.urlIndex) overlayUI.SetImage(showTitleImageIndex, 3.0f, true, true);
                    else overlayUI.SetImage(showTitleImageIndex, 3.0f, true, false);
                }

                if (isSpawning)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            if (overlayUI.isFadeOutFinishedBlackDisplay())
                            {
                                overlayUI.TitleFadeOut();
                                Networking.LocalPlayer.TeleportTo(entryPoint.transform.position, entryPoint.transform.rotation);
                                //teleport先にOverlayUIを即座にTrackingさせる
                                if (overlayUI != null)
                                {
                                    overlayUI.Tracking();
                                }

                                //フェードイン
                                overlayUI.BlackFadeIn();
                                Networking.LocalPlayer.Immobilize(false);
                                isSpawning = false;
                            }
                        }
                    }
                }
            }
            else //overlayUIが設定されていない（通知を表示させたくない場合）の処理
            {
                if (isSpawning)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            Networking.LocalPlayer.Immobilize(false);
                            isSpawning = false;
                        }
                    }
                }
            }

            switch (step)
            {
                case StartUpStep.LOAD_DATA_STEP:
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        ExResourceLoadManager.LoadData(dataUrlIndex[0], dataUrlIndex[1], dataUrlIndex[2], dataUrlIndex[3], dataUrlIndex[4], dataUrlIndex[5], dataUrlIndex[6], dataUrlIndex[7], dataUrlIndex[8], dataUrlIndex[9], true);
                        step = StartUpStep.ANALYZE_DATA_STEP;
                        if (DebugText != null) DebugText.text += "\nStartUpStep.LOAD_DATA_STEP FINISH";
                        //Debug.Log("StartUpStep.LOAD_DATA_STEP FINISH");
                    }
                    break;
                case StartUpStep.ANALYZE_DATA_STEP:
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        //ここでデータを解析して読み込みURLIndexなどのリストを得る
                        step = StartUpStep.LOAD_BGM_STEP;
                        if(_timeManager != null) _timeManager.SetTimeLimitWithExternalData();
                        if (_multiAreaManager != null)
                        {
                            _multiAreaManager.SetExternalDefaultAreaSettingData();
                            _multiAreaManager.SetExternalDefaultActiveStateData();
                            _multiAreaManager.SetExternalMaterialTilingData();
                            _multiAreaManager.SetExternalRegisteredUsers();
                            if (_multiAreaManager._seamlessAreaManager != null)
                            {
                                _multiAreaManager._seamlessAreaManager.UpdateDoorplate();
                                _multiAreaManager._seamlessAreaManager.SetMatrialTiling();
                            }
                        }
                        int spawnArea = -1;
                        if (_multiAreaManager != null)
                        {
                            for (int i = 0; i < _multiAreaManager.isSpawnArea.Length; i++)
                            {
                                if (_multiAreaManager.isSpawnArea[i])
                                {
                                    spawnArea = i;
                                    break;
                                }
                            }
                        }
                        if (spawnArea >= 0)
                        {
                            if (_multiAreaManager != null)
                            {
                                if(_multiAreaManager.GetIsGetAreaState(spawnArea))
                                {
                                    step = StartUpStep.FINISH_STEP;
                                    _multiAreaManager.ReserveSpawn(spawnArea);
                                }
                                else
                                {
                                    step = StartUpStep.SYNCWAIT_STEP;
                                }
                            }
                        }
                        if (DebugText != null) DebugText.text += "\nStartUpStep.ANALYZE_DATA_STEP FINISH";
                        //Debug.Log("StartUpStep.ANALYZE_DATA_STEP FINISH");
                    }
                    break;
                case StartUpStep.LOAD_BGM_STEP:
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        ExResourceLoadManager.LoadBGM(BGMUrlIndex[0], BGMUrlIndex[1], BGMUrlIndex[2], BGMUrlIndex[3], BGMUrlIndex[4], BGMUrlIndex[5], BGMUrlIndex[6], BGMUrlIndex[7], BGMUrlIndex[8], BGMUrlIndex[9], true);
                        step = StartUpStep.LOAD_COMMON_STEP;
                        if (DebugText != null) DebugText.text += "\nStartUpStep.LOAD_BGM_STEP FINISH";
                        //Debug.Log("StartUpStep.LOAD_BGM_STEP FINISH");
                    }
                    break;
                case StartUpStep.LOAD_COMMON_STEP:
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        if(isQuickLoadCommon)  ExResourceLoadManager.QuickLoadCommonTexture(commonUrlIndex[0], true);
                        else ExResourceLoadManager.LoadCommonTexture(commonUrlIndex[0], commonUrlIndex[1], commonUrlIndex[2], commonUrlIndex[3], commonUrlIndex[4], commonUrlIndex[5], commonUrlIndex[6], commonUrlIndex[7], commonUrlIndex[8], commonUrlIndex[9], commonUrlIndex[10], commonUrlIndex[11], commonUrlIndex[12], commonUrlIndex[13], commonUrlIndex[14], commonUrlIndex[15], commonUrlIndex[16], commonUrlIndex[17], commonUrlIndex[18], commonUrlIndex[19], true);
                        step = StartUpStep.LOAD_SKYBOX_STEP;
                        if (DebugText != null) DebugText.text += "\nStartUpStep.LOAD_COMMON_STEP FINISH";
                        //Debug.Log("StartUpStep.LOAD_COMMON_STEP FINISH");
                    }
                    break;
                case StartUpStep.LOAD_SKYBOX_STEP:
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        if(_timeManager != null)
                        {
                            RenderSettings.skybox = _timeManager.currentSkybox;
                            RenderSettings.ambientLight = _timeManager.currentColor;
                            _timeManager.SetSky(_timeManager.GetJst(), true);
                            ExResourceLoadManager.ForceLoadSkyboxWithReserved();
                            step = StartUpStep.LOAD_MULTIBGM_STEP;
                        }
                        else
                        {
                            if(isQuickLoadSkyBox) ExResourceLoadManager.QuickLoadSkyboxTexture(skyboxUrlIndex[0], true);
                            else ExResourceLoadManager.LoadSkyboxTexture(skyboxUrlIndex[0], skyboxUrlIndex[1], skyboxUrlIndex[2], skyboxUrlIndex[3], skyboxUrlIndex[4], skyboxUrlIndex[5], true);
                            step = StartUpStep.LOAD_MULTIBGM_STEP;
                            if (DebugText != null) DebugText.text += "\nStartUpStep.LOAD_SKYBOX_STEP FINISH";
                            //Debug.Log("StartUpStep.LOAD_SKYBOX_STEP FINISH");
                        }
                    }
                    break;
                case StartUpStep.LOAD_MULTIBGM_STEP:
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        ExResourceLoadManager.LoadMultiBGM(multiBGMUrlIndex[0], multiBGMUrlIndex[1], multiBGMUrlIndex[2], multiBGMUrlIndex[3], multiBGMUrlIndex[4], multiBGMUrlIndex[5], multiBGMUrlIndex[6], multiBGMUrlIndex[7], multiBGMUrlIndex[8], multiBGMUrlIndex[9], true);
                        step = StartUpStep.WARP_STEP;
                        if (DebugText != null) DebugText.text += "\nStartUpStep.LOAD_MULTIBGM_STEP FINISH";
                        //Debug.Log("StartUpStep.LOAD_MULTIBGM_STEP FINISH");
                    }
                    break;
                /*case StartUpStep.SETUP_VIDEOPLAYER_STEP:
                    step = StartUpStep.FINISH_STEP; //動画再生は待たずにスタートアップを終える
                    if (ExResourceLoadManager.GetIsFinish())
                    {
                        ExResourceLoadManager.SetupVideoPlayerWithPlaylistIndex(multiSyncVideoPlayerSetupIndex, true);
                        if (DebugText != null) DebugText.text += "\nStartUpStep.SETUP_VIDEOPLAYER_STEP FINISH";
                        Debug.Log("StartUpStep.SETUP_VIDEOPLAYER_STEP FINISH");
                    }
                    break;*/
                case StartUpStep.WARP_STEP:
                    isSpawning = true;
                    //最低フェード時間分は待つ処理
                    fadeoutCount = fadeoutCountMax;

                    /*int spawnArea = -1;
                    if(_multiAreaManager != null)
                    {
                        for(int i=0; i < _multiAreaManager.isSpawnArea.Length; i++)
                        {
                            if (_multiAreaManager.isSpawnArea[i])
                            {
                                spawnArea = i;
                                break;
                            }
                        }
                    }
                    if(spawnArea < 0)
                    {
                        if(entryPoint != null)
                        {
                            isSpawning = true;
                            //最低フェード時間分は待つ処理
                            fadeoutCount = fadeoutCountMax;
                        }
                    }
                    else
                    {
                        if (_multiAreaManager != null)
                        {
                            _multiAreaManager.ReserveSpawn(spawnArea);
                        }
                    }*/
                    step = StartUpStep.FINISH_STEP;
                    break;
                case StartUpStep.SYNCWAIT_STEP:
                    int spawnArea_wait = -1;
                    if (_multiAreaManager != null)
                    {
                        for (int i = 0; i < _multiAreaManager.isSpawnArea.Length; i++)
                        {
                            if (_multiAreaManager.isSpawnArea[i])
                            {
                                spawnArea_wait = i;
                                break;
                            }
                        }
                    }
                    if (spawnArea_wait >= 0)
                    {
                        if (_multiAreaManager != null)
                        {
                            if (_multiAreaManager.GetIsGetAreaState(spawnArea_wait))
                            {
                                step = StartUpStep.FINISH_STEP;
                                _multiAreaManager.ReserveSpawn(spawnArea_wait);
                            }
                        }
                    }
                    break;
            }
        }

        void LoadAll()
        {
            if(isFillColorBeforeLoad)
            {
                if(ExResourceLoadManager != null)
                {
                    ExResourceLoadManager.FillCommonTexture();
                    ExResourceLoadManager.FillSkyBoxTexture();
                }
            }
            isSpawning = false;
            Networking.LocalPlayer.Immobilize(true);
            //フェードアウト
            if (overlayUI != null)
            {
                overlayUI.BlackFadeOut();
                overlayUI.SetTitle(loadingText, showTitleInterval);
                if(!isShowTitleImageOneTime) showTitleImageDone = false;//初回のみ画像を出す
            }
            fadeoutCount = fadeoutCountMax;
            step = StartUpStep.LOAD_DATA_STEP;
        }

        public bool GetIsFinish()
        {
            if (step == StartUpStep.FINISH_STEP) return true;
            return false;
        }
    }
}
