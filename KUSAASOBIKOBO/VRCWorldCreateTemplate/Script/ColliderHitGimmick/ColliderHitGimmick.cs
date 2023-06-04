
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;

namespace KUSAASOBIKOBO
{
    /*
     * ＜説明＞
     * プレイヤーがコライダー内に入ったり出たりした発生させたい事象について一括で設定できるスクリプトです。
     * LocalPlayerの状態でisStayLocalPlayerの値が変化するため、外部スクリプトからコライダー内にいるかどうかの情報を取得することも可能です。
     * これにより、複数のスクリプトでOnPlayerTriggerEnterやOnPlayerTriggerStayがチェックされることを防ぎ負荷を軽減させることができます。
     * BoxColliderのないGameObjectに追加することで外部スクリプトのみで発火するようにできます。
     */
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ColliderHitGimmick : UdonSharpBehaviour
    {
        /*
         * ＜注意＞
         * Holo移動対策等でインタラクトによる強制実行を実装したい場合はInteractGimmickなどの外部スクリプトを追加してExecute()を直接呼んでください。
         * isStayLocalPlayerを見て発火させるスクリプトと併用する場合も同一です。
         */

        /*ボイス音量コントロールやコライダー内のプレイヤー特定に使うため基本的に必須*/
        [Header("PlayerDatabase")] public PlayerDatabase _playerDataBase;

        /*ビデオプレイヤーを含め外部リソースを扱う場合必須*/
        [Header("ExternalResourceLoadManager")] public ExternalResourceLoadManager targetExternalResourceLoadManager;

        /*マテリアル変更等で使用する場合必要*/
        [Header("TimeManager")] public TimeManager _timeManager;

        /*オーバーレイマネージャコントロールで必要*/
        [Header("OverlayUIManager")] public OverlayUIManager _overlayUIManager;

        /*監視モード*/
        [Header("コライダー内にいるとき継続的にステータスを監視（有効化推奨）")] public bool isContinuousExecute = true;

        /*対象ユーザ限定化オプション(非対称ユーザオプションは無効化されます)*/
        [Header("効果対象ユーザーをdisplayNameで限定する")] public bool isLimitedUserEffectRange = false;
        [Header("効果対象ユーザーのdisplayNameリスト")] public string[] LimitedUsers;

        /*非対象ユーザ限定化オプション(対称ユーザオプションが入っている場合無効です)*/
        [Header("除外ユーザーをdisplayNameで限定する")] public bool isLimitedIgnoreUserEffectRange = false;
        [Header("除外ユーザーのdisplayNameリスト")] public string[] LimitedIgnoreUsers;

        /*外部スクリプトからの制御についてのオプション*/
        [Header("外部スクリプトから直接発火した場合もユーザー限定を有効化(非有効化推奨)")] public bool isUseLimitedUserControlWhenExtendScriptExecute = false;

        /*外部スクリプトから監視可能項目*/
        private bool isStayLocalPlayer;//LocalPlayerが入ったときにtrueになり、LocalPlayerが出たりこのオブジェクトが非アクティブになったときfalseになります。
        public bool GetIsStayLocalPlayer() { return isStayLocalPlayer; }

        //private VRCPlayerApi[] players = new VRCPlayerApi[80];//VRCPlayerApi.GetPlayers(players);の格納先です。
        public VRCPlayerApi[] GetPlayers() { return _playerDataBase.players; }
        //public void SetPlayers(VRCPlayerApi[] otherScriptPlayers) { players = otherScriptPlayers; }
        //[Header("外部スクリプトからPlayersを書き換える（自力で取得をしない）")] public bool isOverwritePlayers = false;

        /*特定空間内に入ったときと出たときにSEを鳴らす*/
        [Header("コライダーに入ったときSEを再生するか")] public bool isSoundSEWhenColliderIn = false;
        [Header("コライダーにから出たときSEを再生するか")] public bool isSoundSEWhenColliderOut = false;
        [Header("自分以外のプレイヤーがコライダーに入った場合もSEを再生するか")] public bool isGlobalSoundSE = false;
        [Header("SEを再生するAudioSource（複数指定可能）")] public AudioSource[] playSEAudioSources; //店外と店内の距離が離れている施設で2箇所から再生させたい場合
        [Header("入ったときに鳴らすSE")] public AudioClip ColliderInSE;
        [Header("出たときに鳴らすSE")] public AudioClip ColliderOutSE;

        /*SourceColorChanger 特定空間内のAmbientColorを変更（主にリアルタイムライトによる明るさ調整）*/
        [Header("コライダー内のライティングをAmbientColorで制御するか")] public bool isUseLightingControl = false;
        [Header("初期化時にdefaultAmbientModeとdefaultColorを保存")] public bool isSaveStartUpLightingParameter = false;
        [Header("コライダー外で使っているAmbientMode")] public AmbientMode defaultAmbientMode;
        [Header("コライダー外で使っているワールドライトカラー"), ColorUsage(false, true)] public Color defaultColor;
        [Header("コライダー内だけのAmbientMode(カラー適用はFlat時)")] public AmbientMode changeAmbientMode = AmbientMode.Flat;
        [Header("コライダー内だけのワールドライトカラー"), ColorUsage(false, true)] public Color changeColor;

        /*特定空間内のスカイボックスを変更する*/
        [Header("コライダー内のスカイボックスを変更するか")] public bool isChangeSkyBox = false;
        [Header("初期化時にdefaultSkyBoxを保存")] public bool isSaveDefaultSkyBox = false;
        [Header("TimeManagerからdefaultSkyBoxを取得(優先)")] public bool isUseTimeManagerSkybox = false;
        [Header("コライダー外で使っているSkyBox")] public Material defaultSkybox;
        [Header("コライダー内だけのSkyBox")] public Material skybox;

        /*特定空間内のボイス拡張設定を変更する*/
        [Header("コライダー内のプレイヤーのボイスを拡張するか")] public bool isChangeVoiceSetting = false;
        [Header("コライダー内のプレイヤーどうしは拡張を無効にするか")] public bool isChangeVoiceSettingIgnoreInsidePlayer = false;

        [Header("コライダー外に出たときのボイス音量")] public int defaultVoiceGain = 10;
        [Header("コライダー外に出たときのボイス減衰開始距離")] public int defaultVoiceDistanceNear = 0;
        [Header("コライダー外に出たときのボイス減衰終了距離")] public int defaultVoiceDistanceFar = 25;
        [Header("コライダー外に出たときのボイス原点半径")] public int defaultVoiceVolumetricRadius = 0;
        [Header("コライダー外に出たときのボイスローパスフィルター")] public bool defaultVoiceLowpass = true;

        [Header("コライダー内にいるときのボイス音量")] public int VoiceGain = 10;
        [Header("コライダー内にいるときのボイス減衰開始距離")] public int VoiceDistanceNear = 10;
        [Header("コライダー内にいるときのボイス減衰終了距離")] public int VoiceDistanceFar = 25;
        [Header("コライダー内にいるときのボイス原点半径")] public int VoiceVolumetricRadius = 0;
        [Header("コライダー内にいるときのボイスローパスフィルター")] public bool VoiceLowpass = false;

        /*特定空間内だけプレイヤーの移動速度、落下速度、ジャンプ力、ジャンプ回数などを変更する*/
        [Header("コライダー内にいるときプレイヤーの身体能力を変更するか")] public bool isPlayerPhysicalParameter = false;
        [Header("初期化時に身体能力を保存")] public bool isSaveDefaultPlayerPhysicalParameter = false;
        [Header("ジャンプマネージャー")] public JumpManager _jumpManager;

        [Header("コライダー内にいるときの歩行速度")] public float walkSpeed = 2.0f;
        [Header("コライダー内にいるときの走行速度")] public float runSpeed = 4.0f;
        [Header("コライダー内にいるときのカニ歩き速度")] public float strafeSpeed = 2.0f;
        [Header("コライダー内にいるときのジャンプ力")] public float jumpPower = 1.0f;
        [Header("コライダー内にいるときのジャンプ可能回数(無限ジャンプ時は無視)")] public int maxJumpNum = 1;
        [Header("コライダー内にいるとき無限にジャンプできるようにする")] public bool infinityJump = true;
        [Header("コライダー内にいるときの重力の強さ")] public float gravityStrength = 1.0f;

        [Header("コライダー外にいるときの歩行速度")] public float defaultWalkSpeed = 2.0f;
        [Header("コライダー外にいるときの走行速度")] public float defaultRunSpeed = 4.0f;
        [Header("コライダー外にいるときのカニ歩き速度")] public float defaultStrafeSpeed = 2.0f;
        [Header("コライダー外にいるときのジャンプ力")] public float defaultJumpPower = 2.0f;
        [Header("コライダー外にいるときのジャンプ可能回数(無限ジャンプ時は無視)")] public int defaultMaxJumpNum = 1;
        [Header("コライダー外にいるとき無限にジャンプできるようにする)")] public bool defaultInfinityJump = false;
        [Header("コライダー外にいるときの重力の強さ")] public float defaultGravityStrength = 1.0f;

        /*特定空間内のだけAudioSourceのVolumeを変更する*/
        [Header("コライダー内にいるとき共通音量のAudioSourceの音量を変更するか")] public bool isUseAudioSourceVolumeControl = false;
        [Header("初期化時に0番目の要素の音量をdefaultAudioSourceVolumeを保存")] public bool isSaveDefaultAudioSourceVolume = false;
        [Header("共通音量のAudioSource")] public AudioSource[] targetAudioSources;
        [Header("コライダー外にいるときの音量"), Range(0f, 1f)] public float defaultAudioSourceVolume = 1.0f;
        [Header("コライダー内にいるときの音量"), Range(0f, 1f)] public float audioSourceVolume = 0.0f;

        /*特定空間内のだけ指定のUrlのVideoPlayerを再生する*/
        [Header("コライダー内にいるとき特定のVideoを再生するか")] public bool isVideoPlayerPlayControl = false;
        [Header("コライダー外に出たときVideoを停止するか")] public bool isVideoPlayerStopControl = false;
        [Header("プレイヤーから同期(isMonitoringPlayer必須)")] public bool isVideoPlayerSync = false;
        [Header("デフォルトでビデオ再生する")] public bool isDefaultVideoPlay = false;

        [Header("再生するURL(Pc)")] public VRCUrl urlPc;
        [Header("再生するURL(Quest)")] public VRCUrl urlQuest;
        [Header("再生するURLはストリーミングか")] public bool isStreaming;

        [Header("デフォルト再生UrlPcIndex")] public int defaultUrlPcIndex = 0;
        [Header("デフォルト再生UrlQuestIndex")] public int defaultUrlQuestIndex = 0;
        [Header("デフォルト再生Urlはストリーミングか")] public bool defaultIsStreaming = false;
        [Header("URLリスト")] public VRCUrlList urlList;


        private int globalPlayVideoTargetIndex = 0; //GlobalPlayVideo呼んだ時に再生されるビデオIndexです
        [Header("ビデオ検索入力フィードバックText")] public Text globalPlayVideoTargetIndexFeedback;
        [Header("ビデオ検索決定時ビデオタイトル表示Text")] public Text globalPlayVideoTargetTitleFeedback;
        [Header("動画セレクタからの再生開始実行時入力内容をクリアするか")] public bool isClearToPlayVideoTagetIndex = false;

        [Header("透過ディスプレイコントロールスライダー")] public Slider transparentDisplaySlider;
        [Header("ボリュームコントロールスライダー")] public Slider VideoVolumeSlider;
        [Header("プレイリスト表示用Text")] public Text playListViewWithCategoryPage;
        [Header("プレイリストタイトル表示用Text")] public Text playListTitleViewWithCategoryPage;
        [Header("プレイリストindex最小値（初期値）")] public int categoryPageRangeMin = 1;
        [Header("プレイリストindex最大値")] public int categoryPageRangeMax = 1;

        [Header("VideoのPCUrlInputField")] public VRC.SDK3.Components.VRCUrlInputField pcUrlInputField;
        [Header("VideoのQuestUrlInputField")] public VRC.SDK3.Components.VRCUrlInputField questUrlInputField;
        [Header("VideoのPCUrlInputFieldに出る薄い文字")] public Text pcUrlInputFieldFeedback;
        [Header("VideoのQuestUrlInputFieldに出る薄い文字")] public Text questUrlInputFieldFeedback;
        private VRCUrl emptyUrl = new VRCUrl("");
        [Header("Questに別のUrlを再生するかのトグル")] public Toggle isSeparateQuestUrlToggle;
        [Header("ストリーミング動画かのトグル")] public Toggle isStreamingToggle;
        //private bool reserveGlobalSync = false;
        //private int reserveGlobalSyncCounter = 0;
        [Header("再生位置微調整の調整値")] public Text globalPlayVideoOffsetFeedback;

        /*その他外部リソース読み込み系(targetExternalResourceLoadManager必須)*/
        [Header("コライダー内に入ったときにデータをロードする")] public bool isDataLoad = false;
        [Header("コライダー内に入ったときに読み込むdataUrlIndex")] public int[] dataUrlIndex = new int[10];



        [Header("コライダー内に入ったときにスカイボックスをロードする")] public bool isSkyboxLoad = false;
        [Header("クイックスカイボックスロードするか")] public bool isSkyboxLoadQuick = false;
        [Header("個別テクスチャロードするか")] public bool isSkyboxLoadStandard = true;
        [Header("スカイボックスロード前にテクスチャをリセットするか")] public bool isSkyboxLoadBeforeResetTexture = false;
        [Header("コライダー内に入ったときに読み込むskyboxUrlIndex")] public int[] skyboxUrlIndex = new int[6];
        [Header("コライダー内に入ったときに読み込むquickSkyBoxUrlIndex")] public int quickSkyBoxUrlIndex = 0;


        [Header("コライダー内に入ったときにSPスカイボックスをロードする")] public bool isSpSkyboxLoad = false;
        [Header("クイックSPスカイボックスロードするか")] public bool isSpSkyboxLoadQuick = false;
        [Header("個別テクスチャロードするか")] public bool isSpSkyboxLoadStandard = true;
        [Header("SPスカイボックスロード前にテクスチャをリセットするか")] public bool isSpSkyboxLoadBeforeResetTexture = false;
        [Header("コライダー内に入ったときに読み込むspSkyboxUrlIndex")] public int[] spSkyboxUrlIndex = new int[6];
        [Header("コライダー内に入ったときに読み込むquickSpSkyboxUrlIndex")] public int quickSpSkyBoxUrlIndex = 0;


        [Header("コライダー内に入ったときにテクスチャをロードする")] public bool isCommonLoad = false;
        [Header("クイックテクスチャをロードするか")] public bool isCommonLoadQuick = false;
        [Header("個別テクスチャロードするか")] public bool isCommonLoadStandard = true;
        [Header("テクスチャロード前にテクスチャをリセットするか")] public bool isCommonLoadBeforeResetTexture = false;
        [Header("4096テクスチャ4x4か(クイック限定)")] public bool isCommonTexture4096 = false;
        [Header("書き込み先4096テクスチャ")] public Texture2D commonTexture4096;
        [Header("コライダー内に入ったときに読み込むcommonUrlIndex")] public int[] commonUrlIndex = new int[20];
        [Header("コライダー内に入ったときに読み込むquickCommonUrlIndex")] public int quickCommonUrlIndex = 0;


        [Header("コライダー内に入ったときにBGMをロードする")] public bool isBGMLoad = false;
        [Header("コライダー内に入ったときに読み込むBGMUrlIndex")] public int[] BGMUrlIndex = new int[10];


        [Header("コライダー内に入ったときに空間BGMをロードする")] public bool isMultiBGMLoad = false;
        [Header("コライダー内に入ったときに読み込むmultiBGMUrlIndex")] public int[] multiBGMUrlIndex = new int[10];

        /*オーバーレイUI操作*/
        [Header("コライダー内に入ったときにタイトルを表示する")] public bool isShowOverlayText = false;
        [Header("コライダー内に入ったときに表示するタイトル")] public string overlayTitle = "";
        [Header("コライダー内に入ったときにタイトルを表示する時間")] public float overlayTitleInterval = 3.0f;

        /*ポスター(ImageDownloadSafe)読み込み*/
        [Header("コライダー内に入ったときにポスターを読み込む")] public bool isLoadPoster = false;
        [Header("コライダー内に入ったときに表示するポスターのURLIndex")] public int[] posterUrlId;
        [Header("コライダー内に入ったときに表示するポスターの最大横幅")] public float[] posterMaxWidth;
        [Header("ポスター(ImageDownloadSafe)")] public ImageDownloadSafe[] poster;


        /*特定空間内だけGameObjectのアクティブ状態を変更する（共通のObjectListで最大5グループ設定可能。進捗に応じて特定空間のオブジェクトを増やしたり内容を変えたりしたい場合に複数使う）*/
        [Header("コライダー内にいるときGameObjectのアクティブ状態を変更するか(group1)")] public bool isUseGameObjectChangeActiveGroup1 = false;
        [Header("コライダー内にいるときGameObjectのアクティブ状態を変更するか(group2)")] public bool isUseGameObjectChangeActiveGroup2 = false;
        [Header("コライダー内にいるときGameObjectのアクティブ状態を変更するか(group3)")] public bool isUseGameObjectChangeActiveGroup3 = false;
        [Header("コライダー内にいるときGameObjectのアクティブ状態を変更するか(group4)")] public bool isUseGameObjectChangeActiveGroup4 = false;
        [Header("コライダー内にいるときGameObjectのアクティブ状態を変更するか(group5)")] public bool isUseGameObjectChangeActiveGroup5 = false;
        [Header("コライダー内にいるとき対象以外のすべてのオブジェクトリストのGameObjectを非アクティブにする")] public bool isUnActiveNotTargetWhenInCollider = false;//共通のオブジェクトリストでこのスクリプトを入れ子にして使う場合はfalseにしてください。SitからExitした際もEnterが発火するため入れ子にしたGameObjectがfalseになりその実行順は制御できません。
        [Header("コライダーを出たときアクティブ対象を非アクティブにする")] public bool isUnActiveActiveTargetsWhenExit = false;//同一のObjectListを共有するColliderHitGimmickがある場合非アクティブが優先される可能性があります。
        [Header("コライダーを出たとき非アクティブ対象をアクティブにする")] public bool isActiveUnActiveTargetsWhenExit = false;//同一のObjectListを共有するColliderHitGimmickがある場合非アクティブが優先される可能性があります。
        [Header("自分以外のプレイヤーがコライダーに入った場合もアクティブ状態を変更する")] public bool isGameObjectChangeActiveGlobal = false;//人が入るとスポットライトが付くとか他の人に見せたいもののアクティブ状態を変更している場合true
        /*＜isGameObjectChangeActiveGlobalについて＞
         * このオプションをisUnActiveActiveTargetsWhenExit、isActiveUnActiveTargetsWhenExitと併用して有効にする場合isContinuousExecuteがtrueである必要があります。（falseの場合、終了時処理が優先されるようになります）
         * コライダー内にいる人の数だけ処理が呼ばれます。大量にコライダー内に人が入ることを意図している場合は使用しないでください。
         * isUnActiveNotTargetWhenInColliderがtrueの場合誰かがコライダーに入るごとにオブジェクトが非アクティブになる可能性があります。（同一フレーム上ではあるので内部的なことで影響は軽微）
         * isUnActiveActiveTargetsWhenExit、isActiveUnActiveTargetsWhenExitがtrueの場合誰かがコライダーから出るごとに一瞬オブジェクトが非アクティブになります。(次のStayが別のプレイヤーによって呼ばれるまで発生)
         */

        [Header("オブジェクトリスト")] public GameObjectList changeActiveObjectList;
        [Header("コライダー内にいるときアクティブにするGameObject(group1)")] public GameObject[] activeTargetGameObjectGroup1;
        [Header("コライダー内にいるとき非アクティブにするGameObject(group1)")] public GameObject[] unActiveTargetGameObjectGroup1;
        [Header("コライダー内にいるときアクティブにするGameObject(group2)")] public GameObject[] activeTargetGameObjectGroup2;
        [Header("コライダー内にいるとき非アクティブにするGameObject(group2)")] public GameObject[] unActiveTargetGameObjectGroup2;
        [Header("コライダー内にいるときアクティブにするGameObject(group3)")] public GameObject[] activeTargetGameObjectGroup3;
        [Header("コライダー内にいるとき非アクティブにするGameObject(group3)")] public GameObject[] unActiveTargetGameObjectGroup3;
        [Header("コライダー内にいるときアクティブにするGameObject(group4)")] public GameObject[] activeTargetGameObjectGroup4;
        [Header("コライダー内にいるとき非アクティブにするGameObject(group4)")] public GameObject[] unActiveTargetGameObjectGroup4;
        [Header("コライダー内にいるときアクティブにするGameObject(group5)")] public GameObject[] activeTargetGameObjectGroup5;
        [Header("コライダー内にいるとき非アクティブにするGameObject(group5)")] public GameObject[] unActiveTargetGameObjectGroup5;

        /*特定空間内のプレイヤーを監視する*/
        [Header("コライダー内にいるプレイヤーを監視する")] public bool isMonitoringPlayer = false;
        [NonSerialized] public bool[] playerListWithinRange = new bool[80] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }; //この配列のindexがPlayerのindexになります。
        private float remonitoringTimeCounter = 0;
        private float remonitoringTime = 3;
        private int myIndex = -1;


        /*特定空間に入ったときにターゲットのGameObjectのtransformを変更する。（建築）
        * ※特定空間を出た際もtransformは維持されます。
        * ※特定空間を出た際に親も元に戻しません。
        */
        [Header("コライダー内に入ったときGameObjectのtransformを変更(建築)するか")] public bool isArchitect = false;
        [Header("コライダー内を抜けたとき建築していた物を解体するか（元の位置に戻すか）")] public bool isDismantling = false;
        [Header("建築設計図")] public ArchitecturalBlueprint _architecturalBlueprint;

        [Header("別のCHG")] public ColliderHitGimmick _otherColliderHitGimmick;
        [Header("コライダーを出たとき別のCHGのビデオプレイヤー再読み込み")] public bool isPlayOtherCHGVideo = false;
        [Header("別のCHGに入ったときリストから対象を除外")] public bool isIgnorePlayerListInOtherCHG = false;


        private bool isPrecisionSyncSendReserve = false;

        public bool isEntered = false; //sitによるEnter再発火防止

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        void Update()
        {
            /*
            if (DebugText != null && _playerDataBase != null && isMonitoringPlayer)
            {
                DebugText.text = "PlayerList";
                for(int i = 0; i < playerListWithinRange.Length; i++)
                {
                    if(playerListWithinRange[i])
                    {
                        DebugText.text += "\n" + _playerDataBase.displayNameList[i]; 
                    }
                }
            }
            */

            /*精密同期のSendタイミングを調整*/
            if(isPrecisionSyncSendReserve){
                if(_timeManager != null){
                    if(_timeManager.now.Second%10 == 0){
                        if (isVideoPlayerPlayControl && targetExternalResourceLoadManager != null)
                        {
                            if (isMonitoringPlayer)
                            {
                                if (targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
                                {
                                    urlPc = targetExternalResourceLoadManager._multiSyncVideoPlayer.currentUrlPc;
                                    urlQuest = targetExternalResourceLoadManager._multiSyncVideoPlayer.currentUrlQuest;
                                    isStreaming = !targetExternalResourceLoadManager._multiSyncVideoPlayer.isContinuous;
                                    //targetExternalResourceLoadManager.ForceVideoPlayWithUrl(urlPc, urlQuest, isStreaming);

                                    for (int i = 0; i < playerListWithinRange.Length; i++)
                                    {
                                        if (playerListWithinRange[i])//自分も精密同期タイミングで開始したいのでmyIndexを除外しない
                                        {
                                            targetExternalResourceLoadManager.PrecisionSyncMyVideoWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                                        } 
                                    }
                                }
                            }
                        }
                        isPrecisionSyncSendReserve = false;
                    }
                }
            }

            /*if(reserveGlobalSync){
                if(reserveGlobalSyncCounter > 0)
                {
                    reserveGlobalSyncCounter--;
                }else{
                    reserveGlobalSync = false;
                    GlobalSyncOtherPlayerOnly();
                }
            }*/
        }

        public int GetNumberOfPlayerInCollider()
        {
            int result = 0;
            if (isMonitoringPlayer)
            {
                for (int i = 0; i < playerListWithinRange.Length; i++)
                {
                    if (playerListWithinRange[i])
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        public void StartMonitoring()
        {
            if (isMonitoringPlayer )
            {
                remonitoringTimeCounter = remonitoringTime;
            }
        }

        public void ResetMonitoringPlayer()
        {
            if (isMonitoringPlayer)
            {
                for(int i=0; i < playerListWithinRange.Length; i++) playerListWithinRange[i] = false;
            }
        }

        public void ResetPlayerListWithinRange()
        {
            for (int i = 0; i < playerListWithinRange.Length; i++)
            {
                playerListWithinRange[i] = false;
            }
            remonitoringTimeCounter = 0;
        }  

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (isStayLocalPlayer)
            {
                ResetPlayerListWithinRange();
                StartMonitoring();
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (isStayLocalPlayer)
            {
                ResetPlayerListWithinRange();
                StartMonitoring();
            }
        }

        void Start()
        {
            SetDefault();
        }

        public void SetDefault()
        {
            /*発火対象PlayerをdisplayNameで絞る*/
            if (!isAuthorizedUser(Networking.LocalPlayer)) return;

            if (isUseLightingControl && isSaveStartUpLightingParameter) //このオブジェクトが初めてアクティブになったときにdefaultAmbientModeとdefaultColorを保存します。
            {
                defaultAmbientMode = RenderSettings.ambientMode;
                defaultColor = RenderSettings.ambientLight;
            }

            if (isChangeSkyBox && isSaveDefaultSkyBox) //このオブジェクトが初めてアクティブになったときにdefaultSkyBoxを保存します。
            {
                defaultSkybox = RenderSettings.skybox;
            }

            if (isPlayerPhysicalParameter && isSaveDefaultPlayerPhysicalParameter) //このオブジェクトが初めてアクティブになったときにプレイヤーの身体能力を保存します。
            {
                defaultWalkSpeed = Networking.LocalPlayer.GetWalkSpeed();
                defaultRunSpeed = Networking.LocalPlayer.GetRunSpeed();
                defaultStrafeSpeed = Networking.LocalPlayer.GetStrafeSpeed();
                defaultJumpPower = Networking.LocalPlayer.GetJumpImpulse();
                if (_jumpManager != null)
                {
                    defaultMaxJumpNum = _jumpManager.maxJumpNum;
                    defaultInfinityJump = _jumpManager.infinityJump;
                }
                defaultGravityStrength = Networking.LocalPlayer.GetGravityStrength();
            }

            if (isUseAudioSourceVolumeControl && isSaveDefaultAudioSourceVolume) //このオブジェクトが初めてアクティブになったときにAudioSourceの0番目の要素の音量を保存します。
            {
                if (targetAudioSources != null && targetAudioSources[0] != null) defaultAudioSourceVolume = targetAudioSources[0].volume;
            }

            SetDefaultVideoUrl();
        }

        public void SetDefaultVideoUrl()
        {
            if (isDefaultVideoPlay && urlList != null && urlList.elementList[defaultUrlPcIndex] != null && urlList.elementList[defaultUrlQuestIndex] != null)
            {
                urlPc = urlList.elementList[defaultUrlPcIndex];
                urlQuest = urlList.elementList[defaultUrlQuestIndex];
                isStreaming = defaultIsStreaming;
            }
        }


        private void OnEnable()
        {
            StartMonitoring();
        }

        private void OnDisable()//非アクティブ化時の処理
        {
            /*発火対象PlayerをdisplayNameで絞る*/
            if (!isAuthorizedUser(Networking.LocalPlayer)) return;
            if (!isStayLocalPlayer) return;
            isEntered = false;

            ResetStatus();
            ResetAllPlayer(null, true); //nullで呼ぶと全プレイヤー対象に処理されます
            ResetPlayerListWithinRange();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)//リスポーン時処理
        {
            if (player != Networking.LocalPlayer) return; //自身のリスポーン時のみ有効
            if (!isStayLocalPlayer) return;
            isEntered = false;

            /*発火対象PlayerをdisplayNameで絞る*/
            if (!isAuthorizedUser(player)) return;

            ResetStatus();
            ResetAllPlayer(null, true); //nullで呼ぶと全プレイヤー対象に処理されます
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            /*特定空間内のプレイヤーを監視する*/
            MonitoringPlayerEnter(player);

            /*発火対象PlayerをdisplayNameで絞る*/
            if (!isAuthorizedUser(Networking.LocalPlayer)) return;

            if (isContinuousExecute) return;
            /*全てのプレイヤーの中の誰かがコライダー内に入ったの実行内容*/
            ExecuteAllPlayer(player);

            /*以降自分がコライダー内に入ったときのみの実行内容*/
            if (player != Networking.LocalPlayer) return;
            if (isEntered) return;
            isEntered = true;
            SetDefault(); //コライダーに入る直前の値をdefaultに保存
            Execute();
        }

        public override void OnPlayerTriggerStay(VRCPlayerApi player)
        {
            /*特定空間内のプレイヤーを監視する(ワールド入退出があった場合10秒間は毎フレームモニタリングする)*/
            if(remonitoringTimeCounter > 0)
            {
                MonitoringPlayerEnter(player);
            }
            if (remonitoringTimeCounter > 0) remonitoringTimeCounter -= Time.deltaTime;

            /*発火対象PlayerをdisplayNameで絞る*/
            if (!isAuthorizedUser(Networking.LocalPlayer)) return;

            if (!isContinuousExecute) return;
            /*全てのプレイヤーの中の誰かがコライダー内にいるときの実行内容*/
            ExecuteAllPlayer(player);
            /*以降自分がコライダー内にいるときのみの実行内容*/
            if (player != Networking.LocalPlayer) return;
            //if (remonitoringTimeCounter > 0) remonitoringTimeCounter -= Time.deltaTime;
            Execute();
        }

        public void ExitCollider(VRCPlayerApi player)
        {
            /*特定空間内のプレイヤーを監視する*/
            MonitoringPlayerExit(player);

            /*発火対象PlayerをdisplayNameで絞る*/
            if (!isAuthorizedUser(Networking.LocalPlayer)) return;
            isEntered = false;

            /*全てのプレイヤーの中の誰かがコライダー内にいるときの実行内容*/
            ResetAllPlayer(player, false);
            if (_otherColliderHitGimmick != null)
            {
                if (_otherColliderHitGimmick.isIgnorePlayerListInOtherCHG)
                {
                    _otherColliderHitGimmick.StartMonitoring();
                }
            }
            /*以降自分がコライダー外に出た時のみの実行内容*/
            if (player != Networking.LocalPlayer) return;
            ResetStatus();

            if (_otherColliderHitGimmick != null)
            {
                if (isPlayOtherCHGVideo)
                {
                    _otherColliderHitGimmick.VideoColliderIn();
                }
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            ExitCollider(player);
        }

        public void MonitoringPlayerEnter(VRCPlayerApi player)
        {
            /*特定空間内のプレイヤーを監視する*/
            if (isMonitoringPlayer && _playerDataBase != null)
            {
                int index_tmp = _playerDataBase.GetPlayerIndexFromPlayerId(player.playerId);
                if(index_tmp != -1)
                {
                    if(_otherColliderHitGimmick == null || !isIgnorePlayerListInOtherCHG) playerListWithinRange[index_tmp] = true;
                    else 
                    {
                        if (_otherColliderHitGimmick != null)
                        {
                            if (isIgnorePlayerListInOtherCHG)
                            {
                                if(!_otherColliderHitGimmick.playerListWithinRange[index_tmp]) playerListWithinRange[index_tmp] = true;
                                else playerListWithinRange[index_tmp] = false;
                            }
                            else playerListWithinRange[index_tmp] = true;

                            if (_otherColliderHitGimmick.isIgnorePlayerListInOtherCHG)
                            {
                                _otherColliderHitGimmick.playerListWithinRange[index_tmp] = false;
                            }
                        }
                    }
                    if(player.playerId == Networking.LocalPlayer.playerId) myIndex = index_tmp;
                }
            }
        }

        public void MonitoringPlayerExit(VRCPlayerApi player)
        {
            /*特定空間内のプレイヤーを監視する*/
            if (isMonitoringPlayer && _playerDataBase != null)
            {
                int index_tmp = _playerDataBase.GetPlayerIndexFromPlayerId(player.playerId);
                if (index_tmp != -1)
                {
                    playerListWithinRange[index_tmp] = false;
                }
            }
        }

        public bool isAuthorizedUser(VRCPlayerApi player)//特定のdisplayNameのユーザーに絞って実行
        {
            if(!isLimitedUserEffectRange && !isLimitedIgnoreUserEffectRange)  return true; //設定されていなければ対象

            bool result = false;

            if (isLimitedUserEffectRange)
            {
                result = false;

                foreach (string tmp in LimitedUsers)
                {
                    if (tmp == player.displayName)
                    {
                        result = true;
                        break;
                    }
                }
            }
            else if (isLimitedIgnoreUserEffectRange)
            {
                result = true;

                foreach (string tmp in LimitedIgnoreUsers)
                {
                    if (tmp == player.displayName)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        public void Execute()
        {
            //Debug.Log("CHG:Execute"+overlayTitle);
            if (isUseLimitedUserControlWhenExtendScriptExecute && !isAuthorizedUser(Networking.LocalPlayer)) return;

            isStayLocalPlayer = true;
            ChangeLightingColor();
            ChangeSkyBox();
            ChangePlayerPhysicalParameter();
            ChangeAudioSourceVolumeControl();
            Architect();
            if (!isGameObjectChangeActiveGlobal)
            {
                UnActiveAllObjcetList();
                ChangeGameObjectChangeActiveGroup1();
                ChangeGameObjectChangeActiveGroup2();
                ChangeGameObjectChangeActiveGroup3();
                ChangeGameObjectChangeActiveGroup4();
                ChangeGameObjectChangeActiveGroup5();
            }
            VideoColliderIn();
            DataLoadColliderIn();
            CommonLoadColliderIn();
            SkyboxLoadColliderIn();
            SpSkyboxLoadColliderIn();
            BGMLoadColliderIn();
            MultiBGMLoadColliderIn();
            ShowOverlayTitleColliderIn();
            LoadPosterColliderIn();
        }

        public void ExecuteAllPlayer(VRCPlayerApi player)
        {
            if (isUseLimitedUserControlWhenExtendScriptExecute && !isAuthorizedUser(Networking.LocalPlayer)) return;

            if (!this.gameObject.activeSelf) return; //非アクティブ時は実行しない

            ChangeVoiceSetting(player);

            if (isGlobalSoundSE || player == Networking.LocalPlayer)
            {
                SoundSEColliderIn();
            }

            if (isGameObjectChangeActiveGlobal)
            {
                UnActiveAllObjcetList();
                ChangeGameObjectChangeActiveGroup1();
                ChangeGameObjectChangeActiveGroup2();
                ChangeGameObjectChangeActiveGroup3();
                ChangeGameObjectChangeActiveGroup4();
                ChangeGameObjectChangeActiveGroup5();
            }
        }

        public void ResetAllPlayer(VRCPlayerApi player, bool allFlag) //null呼びで全プレイヤーに処理する
        {
            if (isUseLimitedUserControlWhenExtendScriptExecute && !isAuthorizedUser(Networking.LocalPlayer)) return;

            if (!allFlag) ResetVoiceSetting(player);
            else ResetVoiceSettingAll();

            if (isGlobalSoundSE || player == Networking.LocalPlayer)
            {
                SoundSEColliderOut();
            }

            if (isGameObjectChangeActiveGlobal)
            {
                ResetGameObjectChangeActiveGroup1();
                ResetGameObjectChangeActiveGroup2();
                ResetGameObjectChangeActiveGroup3();
                ResetGameObjectChangeActiveGroup4();
                ResetGameObjectChangeActiveGroup5();
            }
        }

        public void ResetStatus()
        {
            if (isUseLimitedUserControlWhenExtendScriptExecute && !isAuthorizedUser(Networking.LocalPlayer)) return;
            ResetMonitoringPlayer();
            isStayLocalPlayer = false;
            ResetLighting();
            ResetSkyBox();
            ReloadSky();
            ResetPlayerPhysicalParameter();
            ResetAudioSourceVolumeControl();
            Dismantling();
            if (!isGameObjectChangeActiveGlobal)
            {
                ResetGameObjectChangeActiveGroup1();
                ResetGameObjectChangeActiveGroup2();
                ResetGameObjectChangeActiveGroup3();
                ResetGameObjectChangeActiveGroup4();
                ResetGameObjectChangeActiveGroup5();
            }
            VideoColliderOut();
            DataLoadColliderOut();
            CommonLoadColliderOut();
            SkyboxLoadColliderOut();
            SpSkyboxLoadColliderOut();
            BGMLoadColliderOut();
            MultiBGMLoadColliderOut();
        }
        public void VideoColliderIn()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                //reserveGlobalSync = false;
                if(targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
                {
                    if(VideoVolumeSlider != null)
                    {
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.volumeSlider = VideoVolumeSlider;
                        VideoSetVolume();
                    }

                    if(transparentDisplaySlider != null)
                    {
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.transparentDisplaySlider = transparentDisplaySlider;
                        VideoSetTransparentDisplayMaterialValue();
                    }

                    if(playListTitleViewWithCategoryPage != null)
                    {
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.playListTitleViewWithCategoryPage = playListTitleViewWithCategoryPage;
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.categoryPageRangeMin = categoryPageRangeMin;
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.categoryPageRangeMax = categoryPageRangeMax;
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.categoryPageIndex = categoryPageRangeMin;
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.SetCategoryPage();
                    }

                    if(playListViewWithCategoryPage != null)
                    {
                        targetExternalResourceLoadManager._multiSyncVideoPlayer.playListViewWithCategoryPage = playListViewWithCategoryPage;
                    }
                    targetExternalResourceLoadManager._multiSyncVideoPlayer.SetCategoryPage();
                }
                bool check_tmp = false;
                if (isVideoPlayerSync)
                {
                    if (isMonitoringPlayer)
                    {
                        for (int i = 0; i < playerListWithinRange.Length; i++)
                        {
                            if (playerListWithinRange[i] && i != myIndex)
                            {
                                targetExternalResourceLoadManager.SetupVideoPlayerWithPlayerIndex(i);
                                check_tmp = true;
                                break;
                            }
                        }
                    }
                }
                if(!check_tmp) targetExternalResourceLoadManager.SetupVideoPlayerWithUrl(urlPc, urlQuest, isStreaming);
            }
        }

        private void DataLoadColliderIn()
        {
            if (!isDataLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                for(int i = 0; i < dataUrlIndex.Length; i++)
                {
                    targetExternalResourceLoadManager.reservedDataUrlIndex[i] = dataUrlIndex[i];
                }
                targetExternalResourceLoadManager.dataLoadReserved = true;
            }
        }

        private void DataLoadColliderOut()
        {
            if (!isDataLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                targetExternalResourceLoadManager.dataLoadReserved = false;
            }
        }

        public void CommonLoadColliderIn()
        {
            if (!isCommonLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                if(isCommonLoadBeforeResetTexture) targetExternalResourceLoadManager.FillCommonTexture();

                if (isCommonLoadQuick)
                {
                    if(isCommonTexture4096 && commonTexture4096 != null)
                    {
                        targetExternalResourceLoadManager.isCommonTexture4096Mode = true;
                        targetExternalResourceLoadManager.commonTexture4096 = commonTexture4096;
                    }
                    else
                    {
                        targetExternalResourceLoadManager.isCommonTexture4096Mode = false;
                    }

                    targetExternalResourceLoadManager.reservedQuickCommonUrlIndex = quickCommonUrlIndex;
                    targetExternalResourceLoadManager.quickCommonLoadReserved = true;
                } 

                if (isCommonLoadStandard)
                {
                    for(int i = 0; i < commonUrlIndex.Length; i++)
                    {
                        targetExternalResourceLoadManager.reservedCommonUrlIndex[i] = commonUrlIndex[i];
                    }
                    targetExternalResourceLoadManager.commonLoadReserved = true;
                } 
            }
        }

        private void CommonLoadColliderOut()
        {
            if (!isCommonLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                targetExternalResourceLoadManager.commonLoadReserved = false;
            }
        }

        private void SkyboxLoadColliderIn()
        {
            if (!isSkyboxLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                if(isSkyboxLoadBeforeResetTexture) targetExternalResourceLoadManager.FillSkyBoxTexture();

                if (isSkyboxLoadQuick)
                {
                    targetExternalResourceLoadManager.reservedQuickSkyBoxUrlIndex = quickSkyBoxUrlIndex;
                    targetExternalResourceLoadManager.quickSkyboxLoadReserved = true;
                } 

                if (isSkyboxLoadStandard)
                {
                    for(int i = 0; i < skyboxUrlIndex.Length; i++)
                    {
                        targetExternalResourceLoadManager.reservedSkyboxUrlIndex[i] = skyboxUrlIndex[i];
                    }
                    targetExternalResourceLoadManager.skyboxLoadReserved = true;
                }
                
            }
        }

        private void SkyboxLoadColliderOut()
        {
            if (!isSkyboxLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isSkyboxLoadQuick) targetExternalResourceLoadManager.quickSkyboxLoadReserved = false;
                if (isSkyboxLoadStandard) targetExternalResourceLoadManager.skyboxLoadReserved = false;
            }
        }

        public void SpSkyboxLoadColliderIn()
        {
            if (!isSpSkyboxLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                if(isSpSkyboxLoadBeforeResetTexture) targetExternalResourceLoadManager.FillSpSkyBoxTexture();

                if (isSpSkyboxLoadQuick)
                {
                    targetExternalResourceLoadManager.reservedQuickSpSkyBoxUrlIndex = quickSpSkyBoxUrlIndex;
                    targetExternalResourceLoadManager.quickSpSkyboxLoadReserved = true;
                } 

                if (isSpSkyboxLoadStandard)
                {
                    for(int i = 0; i < spSkyboxUrlIndex.Length; i++)
                    {
                        targetExternalResourceLoadManager.reservedSpSkyboxUrlIndex[i] = spSkyboxUrlIndex[i];
                    }
                    targetExternalResourceLoadManager.spSkyboxLoadReserved = true;
                }
                
            }
        }
        
        private void SpSkyboxLoadColliderOut()
        {
            if (!isSpSkyboxLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isSpSkyboxLoadQuick) targetExternalResourceLoadManager.quickSpSkyboxLoadReserved = false;
                if (isSpSkyboxLoadStandard) targetExternalResourceLoadManager.spSkyboxLoadReserved = false;
            }
        }
/*
        private void QuickCommonLoadColliderIn()
        {
            if (!isCommonLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                for(int i = 0; i < commonUrlIndex.Length; i++)
                {
                    targetExternalResourceLoadManager.reservedCommonUrlIndex[i] = commonUrlIndex[i];
                }
                targetExternalResourceLoadManager.quickCommonLoadReserved = true;
            }
        }

        private void QuickCommonLoadColliderOut()
        {
            if (!isCommonLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                targetExternalResourceLoadManager.quickCommonLoadReserved = false;
            }
        }

        private void QuickSkyboxLoadColliderIn()
        {
            if (!isSkyboxLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                for(int i = 0; i < skyboxUrlIndex.Length; i++)
                {
                    targetExternalResourceLoadManager.reservedSkyboxUrlIndex[i] = skyboxUrlIndex[i];
                }
                targetExternalResourceLoadManager.quickSkyboxLoadReserved = true;
            }
        }

        private void QuickSkyboxLoadColliderOut()
        {
            if (!isSkyboxLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                targetExternalResourceLoadManager.quickSkyboxLoadReserved = false;
            }
        }
*/
        public void BGMLoadColliderIn()
        {
            if (!isBGMLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                for(int i = 0; i < BGMUrlIndex.Length; i++)
                {
                    targetExternalResourceLoadManager.reservedBGMUrlIndex[i] = BGMUrlIndex[i];
                }
                targetExternalResourceLoadManager.BGMLoadReserved = true;
            }
        }

        public void BGMLoadColliderOut()
        {
            if (!isBGMLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                if(targetExternalResourceLoadManager.BGMLoadReserved) targetExternalResourceLoadManager.BGMLoadReserved = false;
                else
                {
                    for(int i = 0; i < BGMUrlIndex.Length; i++)
                    {
                        targetExternalResourceLoadManager.reservedBGMUrlIndex[i] = 0;
                        targetExternalResourceLoadManager.BGMLoadReserved = true;
                    }
                }
            }
        }

        public void MultiBGMLoadColliderIn()
        {
            if (!isMultiBGMLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                for(int i = 0; i < multiBGMUrlIndex.Length; i++)
                {
                    targetExternalResourceLoadManager.reservedMultiBGMUrlIndex[i] = multiBGMUrlIndex[i];
                }
                targetExternalResourceLoadManager.multiBGMLoadReserved = true;
            }
        }

        public void MultiBGMLoadColliderOut()
        {
            if (!isMultiBGMLoad) return;
            if (targetExternalResourceLoadManager != null)
            {
                for (int i = 0; i < multiBGMUrlIndex.Length; i++)
                {
                    targetExternalResourceLoadManager.reservedMultiBGMUrlIndex[i] = 0;
                }
                targetExternalResourceLoadManager.multiBGMLoadReserved = true;
            }
        }

        public void MultiBGMForceStop()
        {
            if (targetExternalResourceLoadManager != null)
            {
                for (int i = 0; i < targetExternalResourceLoadManager.exMultiBGM.Length; i++)
                {
                    targetExternalResourceLoadManager.exMultiBGM[i].StopLoad();
                }
            }
        }

        private void ShowOverlayTitleColliderIn()
        {
            if (!isShowOverlayText) return;
            if (_overlayUIManager != null)
            {
                _overlayUIManager.SetTitle(overlayTitle, overlayTitleInterval);
            }
        }

        public void LoadPosterColliderIn()
        {
            if (!isLoadPoster) return;
            if (poster.Length >= posterUrlId.Length && posterUrlId.Length == posterMaxWidth.Length)
            {
                int length_tmp = posterUrlId.Length;

                for(int i = 0; i < length_tmp; i++)
                {
                    if (poster[i] != null)
                    {
                        poster[i].urlIndex = posterUrlId[i];
                        poster[i].rawImageMaxWidth = posterMaxWidth[i];
                        poster[i].LoadFromUrlIndex();
                    }
                }
            }
        }


        public void SetPoster(int _index, int _urlId, float _maxWidth)
        {
            if (poster.Length > _index && posterUrlId.Length > _index && posterMaxWidth.Length > _index)
            {
                if (poster[_index] != null)
                {
                    posterUrlId[_index] = _urlId;
                    posterMaxWidth[_index] = _maxWidth;
                }
            }
        }

        public void GlobalPlayVideoWithPlayListIndex(int index)
        {
            GlobalPlayVideoWithUrl(targetExternalResourceLoadManager.GetPcUrlFromPlayListIndex(index), targetExternalResourceLoadManager.GetQuestUrlFromPlayListIndex(index), targetExternalResourceLoadManager.GetIsStreamingFromPlayListIndex(index));
        }

        public void InputGlobalPlayVideoIndex_1(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 1;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+1;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_2(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 2;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+2;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_3(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 3;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+3;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_4(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 4;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+4;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_5(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 5;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+5;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_6(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 6;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+6;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_7(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 7;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+7;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_8(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 8;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+8;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }
        public void InputGlobalPlayVideoIndex_9(){
            if(globalPlayVideoTargetIndex == 0) globalPlayVideoTargetIndex = 9;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10+9;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }        
        public void InputGlobalPlayVideoIndex_0(){
            if(globalPlayVideoTargetIndex == 0) return;
            else globalPlayVideoTargetIndex = globalPlayVideoTargetIndex*10;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }          
        public void InputGlobalPlayVideoIndexClear(){
            globalPlayVideoTargetIndex = 0;
            SetGlobalPlayVideoTargetTitleFeedback();
            if (globalPlayVideoTargetIndexFeedback != null) globalPlayVideoTargetIndexFeedback.text = globalPlayVideoTargetIndex.ToString();
        }

        public void SetGlobalPlayVideoTargetTitleFeedback()
        {
            if (targetExternalResourceLoadManager != null && globalPlayVideoTargetTitleFeedback != null) globalPlayVideoTargetTitleFeedback.text = targetExternalResourceLoadManager.GetTitleWithPlaylistIndex(globalPlayVideoTargetIndex);
        }

        public void GlobalPlayVideoWithTargetIndex(){
            if (targetExternalResourceLoadManager != null && !targetExternalResourceLoadManager.CheckVideoPlayListLicense(globalPlayVideoTargetIndex))
            {
                globalPlayVideoTargetTitleFeedback.text = "License Error!";
                return;//license切れ
            }
            GlobalPlayVideoWithPlayListIndex(globalPlayVideoTargetIndex);
            if(isClearToPlayVideoTagetIndex)
            {
                InputGlobalPlayVideoIndexClear();
            } 
        }

        public void GlobalPlayVideoWithUrl(VRCUrl PcUrl, VRCUrl QuestUrl, bool _isStreaming = false)
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isMonitoringPlayer)
                {
                    urlPc = PcUrl;
                    urlQuest = QuestUrl;
                    isStreaming = _isStreaming;
                    targetExternalResourceLoadManager.ForceVideoPlayWithUrl(urlPc, urlQuest, _isStreaming);
                    /*reserveGlobalSync = true;
                    reserveGlobalSyncCounter = (int)targetExternalResourceLoadManager.multiSyncVideoPlayerWaitingForSynchronizationTime;*/
                    for (int i = 0; i < playerListWithinRange.Length; i++)
                    {
                        if (playerListWithinRange[i] && i != myIndex)
                        {
                            targetExternalResourceLoadManager.SyncMyVideoWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                        }
                    }
                }
            }
        }

        public void GlobalSyncOtherPlayerOnly()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isMonitoringPlayer)
                {
                    for (int i = 0; i < playerListWithinRange.Length; i++)
                    {
                        if (playerListWithinRange[i] && i != myIndex)
                        {
                            targetExternalResourceLoadManager.SyncMyVideoWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                        }
                    }
                }
            }
        }

        public void GlobalPlayVideoWithUrlInputField()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isMonitoringPlayer)
                {
                    if(pcUrlInputField == null || questUrlInputField == null || isSeparateQuestUrlToggle == null ||isStreamingToggle == null) return;
                    urlPc = pcUrlInputField.GetUrl();
                    if(isSeparateQuestUrlToggle.isOn) urlQuest = questUrlInputField.GetUrl();
                    else urlQuest = pcUrlInputField.GetUrl();
                    isStreaming = isStreamingToggle.isOn;
                    targetExternalResourceLoadManager.ForceVideoPlayWithUrl(urlPc, urlQuest, isStreaming);

                    pcUrlInputField.SetUrl(emptyUrl);
                    questUrlInputField.SetUrl(emptyUrl);
                    if(pcUrlInputFieldFeedback != null) pcUrlInputFieldFeedback.text = "再生中…(新たに入力することは可能です)";
                    if(questUrlInputFieldFeedback != null && isSeparateQuestUrlToggle.isOn) questUrlInputFieldFeedback.text = "再生中…(新たに入力することは可能です)";
                    for (int i = 0; i < playerListWithinRange.Length; i++)
                    {
                        if (playerListWithinRange[i] && i != myIndex)
                        {
                            targetExternalResourceLoadManager.SyncMyVideoWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                        }
                    }
                }
            }
        }

        public void VideoToggleQuestInputField()
        {
            if(questUrlInputField == null) return;
            if(isSeparateQuestUrlToggle.isOn)
            {
                questUrlInputField.gameObject.SetActive(true);
            }
            else
            {
                questUrlInputField.gameObject.SetActive(false);
            }
        }

        public void GlobalPlayVideoResync()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isMonitoringPlayer)
                {
                    if (targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
                    {
                        urlPc = targetExternalResourceLoadManager._multiSyncVideoPlayer.currentUrlPc;
                        urlQuest = targetExternalResourceLoadManager._multiSyncVideoPlayer.currentUrlQuest;
                        isStreaming = !targetExternalResourceLoadManager._multiSyncVideoPlayer.isContinuous;
                        targetExternalResourceLoadManager.ForceVideoPlayWithUrl(urlPc, urlQuest, isStreaming);

                        for (int i = 0; i < playerListWithinRange.Length; i++)
                        {
                            if (playerListWithinRange[i] && i != myIndex)
                            {
                                targetExternalResourceLoadManager.SyncMyVideoWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                            }
                        }
                    }
                }
            }
        }

        public void PrecisionSynchronizationVideo()
        {
            if (!isVideoPlayerPlayControl) return;
            isPrecisionSyncSendReserve = true;
        }

        public void GlobalPauseVideo()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isMonitoringPlayer)
                {
                    targetExternalResourceLoadManager.VideoPause();
                    for (int i = 0; i < playerListWithinRange.Length; i++)
                    {
                        if (playerListWithinRange[i])
                        {
                            targetExternalResourceLoadManager.SyncMyVideoPauseWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                        }
                    }
                }
            }
        }

        public void GlobalVideoTimeSync()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                if (isMonitoringPlayer)
                {
                    for (int i = 0; i < playerListWithinRange.Length; i++)
                    {
                        if (playerListWithinRange[i] && i != myIndex)
                        {
                            targetExternalResourceLoadManager.SyncMyVideoTimeWithPlayerId(_playerDataBase.GetPlayerIdFromIndex(i));
                        }
                    }
                }
            }
        }

        public void VideoMoveTimeReset()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTimeReset();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }

        }

        public void VideoMoveTime_plus1()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTime_plus1();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }
        }

        public void VideoMoveTime_minus1()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTime_minus1();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }
        }

        public void VideoMoveTime_plus0_1()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTime_plus0_1();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }
        }

        public void VideoMoveTime_minus0_1()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTime_minus0_1();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }
        }

        public void VideoMoveTime_plus0_0_1()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTime_plus0_0_1();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }
        }

        public void VideoMoveTime_minus0_0_1()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.MoveTime_minus0_0_1();
                //if(globalPlayVideoOffsetFeedback != null) globalPlayVideoOffsetFeedback.text = targetExternalResourceLoadManager._multiSyncVideoPlayer.offsetCounter.ToString("F2");
            }
        }

        public void VideoMoveTime_plus30()
        {
            targetExternalResourceLoadManager.VideoMoveTime_plus30();
            GlobalVideoTimeSync();
        }

        public void VideoMoveTime_minus30()
        {
            targetExternalResourceLoadManager.VideoMoveTime_minus30();
            GlobalVideoTimeSync();
        }

        public void VideoMoveTime_plus10()
        {
            targetExternalResourceLoadManager.VideoMoveTime_plus10();
            GlobalVideoTimeSync();
        }

        public void VideoMoveTime_minus10()
        {
            targetExternalResourceLoadManager.VideoMoveTime_minus10();
            GlobalVideoTimeSync();
        }

        public void VideoSetTransparentDisplayMaterialValue()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.SetTransparentDisplayMaterialValue();
            }
        }

        public void VideoSetVolume()
        {
           if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.SetVolume();
            } 
        }

        public void VideoSetCategoryPage()
        {
           if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.SetCategoryPage();
            } 
        }

        public void VideoNextCategoryPage()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.NextCategoryPage();
            }
        }

        public void VideoBeforeCategoryPage()
        {
            if (!isVideoPlayerPlayControl) return;
            if (targetExternalResourceLoadManager != null && targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
            {
                targetExternalResourceLoadManager._multiSyncVideoPlayer.BeforeCategoryPage();
            } 
        }

        public void ResyncVideo()
        {
            VideoColliderIn();
        }

        public void VideoColliderOut()
        {
            if (!isVideoPlayerStopControl) return;
            if (targetExternalResourceLoadManager != null)
            {
                targetExternalResourceLoadManager.ForceEndVideoPlay();
            }
        }

        private void SoundSEColliderIn()
        {
            if (!isSoundSEWhenColliderIn) return;
            if (playSEAudioSources != null && ColliderInSE != null)
            {
                foreach(AudioSource tmp in playSEAudioSources)
                {
                    if (tmp != null) tmp.PlayOneShot(ColliderInSE);
                }
            }
        }

        private void SoundSEColliderOut()
        {
            if (!isSoundSEWhenColliderOut) return;
            if (playSEAudioSources != null && ColliderOutSE != null)
            {
                foreach (AudioSource tmp in playSEAudioSources)
                {
                    if (tmp != null) tmp.PlayOneShot(ColliderOutSE);
                }
            }
        }

        public void ResetLighting()
        {
            if (!isUseLightingControl) return;
            RenderSettings.ambientMode = defaultAmbientMode;

            if(isUseTimeManagerSkybox)
            {
                //ReloadSky()で処理
                if (_timeManager != null)
                {
                    _timeManager.isChangeSourceColor = true;
                }
            }
            else
            {
                RenderSettings.ambientLight = defaultColor;
            }

        }

        public void ChangeLightingColor()
        {
            if (!isUseLightingControl) return;
            RenderSettings.ambientMode = changeAmbientMode;
            RenderSettings.ambientLight = changeColor;
            if (_timeManager != null)
            {
                _timeManager.isChangeSourceColor = false;
            }
        }

        public void ResetSkyBox()
        {
            if (!isChangeSkyBox) return;
            if(isUseTimeManagerSkybox)
            {
                //ReloadSky()で処理
            }
            else
            {
                if (defaultSkybox != null) RenderSettings.skybox = defaultSkybox;
            }
        }

        public void ReloadSky()
        {
            if (!isChangeSkyBox && !isUseLightingControl) return;
            if(isUseTimeManagerSkybox)
            {
                if (_timeManager != null && _timeManager.currentSkybox != null ){
                    RenderSettings.skybox = _timeManager.currentSkybox;
                    RenderSettings.ambientLight = _timeManager.currentColor;
                    _timeManager.SetSky(_timeManager.GetJst());
                } 
                else
                {
                    if (isChangeSkyBox)
                    {
                        if (defaultSkybox != null) RenderSettings.skybox = defaultSkybox;
                    }

                    if (isUseLightingControl)
                    {
                        RenderSettings.ambientLight = defaultColor;
                    }
                } 
            }
        }

        public void ChangeSkyBox()
        {
            if (!isChangeSkyBox) return;
            if(skybox != null) RenderSettings.skybox = skybox;
        }

        private void ResetVoiceSetting(VRCPlayerApi player)
        {
            if (!isChangeVoiceSetting) return;
            player.SetVoiceGain(defaultVoiceGain);
            player.SetVoiceDistanceNear(defaultVoiceDistanceNear);
            player.SetVoiceDistanceFar(defaultVoiceDistanceFar);
            player.SetVoiceVolumetricRadius(defaultVoiceVolumetricRadius);
            player.SetVoiceLowpass(defaultVoiceLowpass);
            /*
            if(DebugText != null && player != Networking.LocalPlayer)
            {
                DebugText.text += "" + player.displayName + ":RESET";
                string[] debugTextLines = DebugText.text.Split('\n');
                if(debugTextLines.Length >= 30)
                {
                    DebugText.text = debugTextLines[debugTextLines.Length - 30];
                    for (int i = (debugTextLines.Length - 30) + 1; i < debugTextLines.Length; i++)
                    {
                        DebugText.text += debugTextLines[i] + "\n";
                    }
                }
            }
            */
        }

        public void ResetVoiceSettingAll()
        {
            if (isUseLimitedUserControlWhenExtendScriptExecute && !isAuthorizedUser(Networking.LocalPlayer)) return;

            if (!isChangeVoiceSetting) return;
            /*if (!isOverwritePlayers)
            {
                for (int i = 0; i < players.Length; i++) players[i] = null;
                VRCPlayerApi.GetPlayers(players);
            }*/
            if (_playerDataBase == null) return;
            if (_playerDataBase.players == null) return;
            foreach (VRCPlayerApi tmp in _playerDataBase.players)
            {
                if (tmp == null) continue;
                ResetVoiceSetting(tmp);
            }
        }

        private void ChangeVoiceSetting(VRCPlayerApi player)
        {
            if (!isChangeVoiceSetting) return;
            if(isChangeVoiceSettingIgnoreInsidePlayer && playerListWithinRange[myIndex])
            {
                if (isMonitoringPlayer && _playerDataBase != null)
                {
                    int topicPlayerId_tmp = _playerDataBase.GetPlayerIndexFromPlayerId(player.playerId);
                    if (playerListWithinRange[topicPlayerId_tmp])
                    {
                        ResetVoiceSetting(player);
                        return;
                    }
                }
            }
            player.SetVoiceGain(VoiceGain);
            player.SetVoiceDistanceNear(VoiceDistanceNear);
            player.SetVoiceDistanceFar(VoiceDistanceFar);
            player.SetVoiceVolumetricRadius(VoiceVolumetricRadius);
            player.SetVoiceLowpass(VoiceLowpass);
            /*
            if (DebugText != null && player != Networking.LocalPlayer)
            {
                DebugText.text += "" + player.displayName + ":VoiceGain:" + VoiceGain + "\n";
                DebugText.text += "" + player.displayName + ":VoiceDistanceNear:" + VoiceDistanceNear + "\n";
                DebugText.text += "" + player.displayName + ":VoiceDistanceFar:" + VoiceDistanceFar + "\n";
                DebugText.text += "" + player.displayName + ":VoiceVolumetricRadius:" + VoiceVolumetricRadius + "\n";
                DebugText.text += "" + player.displayName + ":VoiceLowpass:" + VoiceLowpass + "\n";
                string[] debugTextLines = DebugText.text.Split('\n');
                if (debugTextLines.Length > 30)
                {
                    DebugText.text = debugTextLines[debugTextLines.Length - 30];
                    for (int i = (debugTextLines.Length - 30) + 1; i < debugTextLines.Length; i++)
                    {
                        DebugText.text += debugTextLines[i] + "\n";
                    }
                }
            }
            */
        }

        public void ResetPlayerPhysicalParameter()
        {
            if (!isPlayerPhysicalParameter) return;
            Networking.LocalPlayer.SetWalkSpeed(defaultWalkSpeed);
            Networking.LocalPlayer.SetRunSpeed(defaultRunSpeed);
            Networking.LocalPlayer.SetStrafeSpeed(defaultStrafeSpeed);
            Networking.LocalPlayer.SetJumpImpulse(defaultJumpPower);
            Networking.LocalPlayer.SetGravityStrength(defaultGravityStrength);
            if (_jumpManager != null)
            {
                _jumpManager.maxJumpNum = defaultMaxJumpNum;
                _jumpManager.infinityJump = defaultInfinityJump;
            }
        }

        public void ChangePlayerPhysicalParameter()
        {
            if (!isPlayerPhysicalParameter) return;
            Networking.LocalPlayer.SetWalkSpeed(walkSpeed);
            Networking.LocalPlayer.SetRunSpeed(runSpeed);
            Networking.LocalPlayer.SetStrafeSpeed(strafeSpeed);
            Networking.LocalPlayer.SetJumpImpulse(jumpPower);
            Networking.LocalPlayer.SetGravityStrength(gravityStrength);
            if (_jumpManager != null)
            {
                _jumpManager.maxJumpNum = maxJumpNum;
                _jumpManager.infinityJump = infinityJump;
            }
        }

        private void ResetAudioSourceVolumeControl()
        {
            if (!isUseAudioSourceVolumeControl) return;
            if (targetAudioSources != null)
            {
                foreach(AudioSource tmp in targetAudioSources)
                {
                    if (tmp != null) tmp.volume = defaultAudioSourceVolume;
                }
            }
        }

        private void ChangeAudioSourceVolumeControl()
        {
            if (!isUseAudioSourceVolumeControl) return;
            if (targetAudioSources != null)
            {
                foreach (AudioSource tmp in targetAudioSources)
                {
                    if (tmp != null) tmp.volume = audioSourceVolume;
                }
            }
        }

        private void UnActiveAllObjcetList() //オブジェクトリストの内容を非アクティブ化にする(isUseGameObjectChangeActiveGroupのいずれかがtrueの場合チェックし、isUnActiveNotTargetWhenInColliderが有効ならば実行する)
        {
            if (!isUseGameObjectChangeActiveGroup1 && !isUseGameObjectChangeActiveGroup2 && !isUseGameObjectChangeActiveGroup3 && !isUseGameObjectChangeActiveGroup4 && !isUseGameObjectChangeActiveGroup5) return;

            if (isUnActiveNotTargetWhenInCollider && changeActiveObjectList != null && changeActiveObjectList.elementList != null)
            {
                foreach (GameObject tmp in changeActiveObjectList.elementList) //オブジェクトリストのGameObjectをすべて非アクティブにする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }
        }

        private void ResetGameObjectChangeActiveGroup1()
        {
            if (!isUseGameObjectChangeActiveGroup1) return;

            if (isUnActiveActiveTargetsWhenExit && activeTargetGameObjectGroup1 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup1) //アクティブ化対象オブジェクトを非アクティブにする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (isActiveUnActiveTargetsWhenExit && unActiveTargetGameObjectGroup1 != null) //非アクティブ化対象オブジェクトをアクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup1)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ChangeGameObjectChangeActiveGroup1()
        {
            if (!isUseGameObjectChangeActiveGroup1) return;

            if (unActiveTargetGameObjectGroup1 != null) //非アクティブ化対象オブジェクトを非アクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup1)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (activeTargetGameObjectGroup1 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup1) //アクティブ化対象オブジェクトをアクティブ化にする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ResetGameObjectChangeActiveGroup2()
        {
            if (!isUseGameObjectChangeActiveGroup2) return;

            if (isUnActiveActiveTargetsWhenExit && activeTargetGameObjectGroup2 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup2) //アクティブ化対象オブジェクトを非アクティブにする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (isActiveUnActiveTargetsWhenExit && unActiveTargetGameObjectGroup2 != null) //非アクティブ化対象オブジェクトをアクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup2)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ChangeGameObjectChangeActiveGroup2()
        {
            if (!isUseGameObjectChangeActiveGroup2) return;

            if (unActiveTargetGameObjectGroup2 != null) //非アクティブ化対象オブジェクトを非アクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup2)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (activeTargetGameObjectGroup2 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup2) //アクティブ化対象オブジェクトをアクティブ化にする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ResetGameObjectChangeActiveGroup3()
        {
            if (!isUseGameObjectChangeActiveGroup3) return;

            if (isUnActiveActiveTargetsWhenExit && activeTargetGameObjectGroup3 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup3) //アクティブ化対象オブジェクトを非アクティブにする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (isActiveUnActiveTargetsWhenExit && unActiveTargetGameObjectGroup3 != null) //非アクティブ化対象オブジェクトをアクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup3)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ChangeGameObjectChangeActiveGroup3()
        {
            if (!isUseGameObjectChangeActiveGroup3) return;

            if (unActiveTargetGameObjectGroup3 != null) //非アクティブ化対象オブジェクトを非アクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup3)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (activeTargetGameObjectGroup3 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup3) //アクティブ化対象オブジェクトをアクティブ化にする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ResetGameObjectChangeActiveGroup4()
        {
            if (!isUseGameObjectChangeActiveGroup4) return;

            if (isUnActiveActiveTargetsWhenExit && activeTargetGameObjectGroup4 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup4) //アクティブ化対象オブジェクトを非アクティブにする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (isActiveUnActiveTargetsWhenExit && unActiveTargetGameObjectGroup4 != null) //非アクティブ化対象オブジェクトをアクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup4)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ChangeGameObjectChangeActiveGroup4()
        {
            if (!isUseGameObjectChangeActiveGroup4) return;

            if (unActiveTargetGameObjectGroup4 != null) //非アクティブ化対象オブジェクトを非アクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup4)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (activeTargetGameObjectGroup4 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup4) //アクティブ化対象オブジェクトをアクティブ化にする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ResetGameObjectChangeActiveGroup5()
        {
            if (!isUseGameObjectChangeActiveGroup5) return;

            if (isUnActiveActiveTargetsWhenExit && activeTargetGameObjectGroup5 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup5) //アクティブ化対象オブジェクトを非アクティブにする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (isActiveUnActiveTargetsWhenExit && unActiveTargetGameObjectGroup5 != null) //非アクティブ化対象オブジェクトをアクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup5)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void ChangeGameObjectChangeActiveGroup5()
        {
            if (!isUseGameObjectChangeActiveGroup5) return;

            if (unActiveTargetGameObjectGroup5 != null) //非アクティブ化対象オブジェクトを非アクティブ化にする
            {
                foreach (GameObject tmp in unActiveTargetGameObjectGroup5)
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(false);
                    }
                }
            }

            if (activeTargetGameObjectGroup5 != null)
            {
                foreach (GameObject tmp in activeTargetGameObjectGroup5) //アクティブ化対象オブジェクトをアクティブ化にする
                {
                    if (tmp != null)
                    {
                        tmp.SetActive(true);
                    }
                }
            }
        }

        private void Architect()
        {
            if (!isArchitect) return;
            if (_architecturalBlueprint != null) _architecturalBlueprint.Architect();
        }

        private void Dismantling()
        {
            if (!isArchitect) return; //建築していない場合は使えない
            if (!isDismantling) return;
            if (_architecturalBlueprint != null) _architecturalBlueprint.Dismantling();
        }

        public void SetQuickLoadParameter()
        {
            SetQuickLoadWait();
        }

        public void SetQuickLoadWait()
        {
            if(questUrlInputField != null) targetExternalResourceLoadManager.quickLoadWait = int.Parse(questUrlInputField.GetUrl().Get());
        }
    }
}
