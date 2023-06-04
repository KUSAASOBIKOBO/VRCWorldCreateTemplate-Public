
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using KUSAASOBIKOBO;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBOPHONE
{
    public enum NotificationMethod
    {
        SILENT, //携帯画面の表示だけで受信を伝えます
        VIBRATION, //携帯画面の表示をしつつ、VRコントローラ―をバイブレーションさせて伝えます
        SOUND, //携帯画面の表示をしつつ、音で伝えます。近くに携帯がない時は聞こえません
        WHEREVER_SOUND //携帯画面の表示をしつつ、音で伝えます。音はどこにいても聞こえます。この設定中は操作音もどこにいても聞こえます（ピックアップ中止か操作できないので影響はない）
    }

    public enum Status
    {
        STANDBY, //受信待機中
        PENDING, //電話を受信した状態
        CONSIDERING_CALLER, //相手の受信を待機中
        CONSIDERING_RECEIVER, //受信者が電話の受信を受けるか拒否するか選択中
        CALLING //通話中
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Phone : UdonSharpBehaviour
    {
        [Header("開始時に閉じた状態で設置するか（チェックを入れると閉じた状態で設置）")] public bool isClosedPhone = true; //開閉状態（持ち運び可能な状態か）のフラグです。
        [Header("通知方法（初期設定）")] public NotificationMethod notificationMode = NotificationMethod.SOUND;
        [Header("ボイス音量（初期設定）※相手と0距離扱いの音量になるので注意"), Range(1.0f, 10.0f)] public float VoiceGain = 5.0f;
        [Header("操作中移動できないようにする")] public bool isStopWhenUse = true;
        [Header("特定エリアを圏外にする")] public bool isSetOutSideRange = false;
        [Header("圏外エリア（ボイス拡張機能を持ったコライダー内は圏外にしてください）")] public ColliderHitGimmick[] outsideRangeCollider;
        [Header("連打で転送するか")] public bool isTeleportPhoneConsecutiveClicks = false;
        [Header("着信時転送するか")] public bool isTeleportPhoneWhenGetCall = true;
        [Header("着信時muteの時も転送するか")] public bool isTeleportPhoneAlwaysGetCall = false;
        [Header("電話の転送に必要な連続USE回数(最大255まで)")] public byte teleportPhoneConsecutiveUseNum = 5;
        [Header("連続USE判定フレーム数")] public int teleportPhoneConsecutiveFrameNum = 60;

        [Header("通話中以外のボイス音量（ワールドのデフォルト設定）"), Range(1.0f, 10.0f)] public float DefaultVoiceGain = 10.0f;
        [Header("通話中以外のボイス減衰開始距離（ワールドのデフォルト設定）")] public float DefaultVoiceDistanceNear = 0.0f;
        [Header("通話中以外のボイス減衰終了距離（ワールドのデフォルト設定）")] public float DefaultVoiceDistanceFar = 25.0f;
        [Header("歩行速度（ワールドのデフォルト設定）")] public float defaultWalkSpeed = 2.0f;
        [Header("走行速度（ワールドのデフォルト設定）")] public float defaultRunSpeed = 4.0f;
        [Header("カニ歩き速度（ワールドのデフォルト設定）")] public float defaultStrafeSpeed = 2.0f;
        [Header("ジャンプ力（ワールドのデフォルト設定）")] public float defaultJumpPower = 2.0f;
        [Header("移動入力スティック判定の押し込み量"), Range(0.2f, 1.0f)] public float inputDecisionPower = 0.8f;

        [Header("着信音SE(ループ再生)")] public AudioClip sound1;
        [Header("呼び出し中SE(ループ再生)")] public AudioClip sound2;
        [Header("決定音SE(非ループ再生)")] public AudioClip sound3;
        [Header("切断音SE(非ループ再生)")] public AudioClip sound4;
        [Header("入力音SE(非停止、PlayOneShot再生)")] public AudioClip sound5;
        [Header("開閉音SE(非停止、PlayOneShot再生)")] public AudioClip sound6;
        [Header("転送音SE(非停止、PlayOneShot再生)")] public AudioClip sound7;
        [Header("エラーSE(PlayOneShot再生)")] public AudioClip sound8;

        [Header("開いたPhoneの見た目のMesh")] public GameObject MeshOpen;
        [Header("閉じたPhoneの見た目のMesh")] public GameObject MeshClose;
        [Header("閉じたPhoneの見た目のMesh")] public GameObject backCameraDisplay;
        [Header("表面UIRoot")] public GameObject UIRoot; //isClosedPhone中はOFF
        [Header("背面UIRoot")] public GameObject backDisplayUIRoot; //isClosedPhone中はOFF
        [Header("文字表示用UI")] public Text displayText;
        [Header("切断時文字表示用UI")] public Text displayTextHungUp;
        [Header("相手の顔表示用UI")] public GameObject displayCamera;
        [Header("相手の顔表示用カメラ")] public GameObject faceCamera;
        [Header("SE再生用AudioSource")] public AudioSource _audioSource;
        [Header("通話リクエストプレイヤーマネージャー(isToMeOnly=true, isIgnoreSaveReceivePlayerIndex=true)")] public PlayerManager2 callerPlayerManager;
        [Header("切断リクエストプレイヤーマネージャー(isToMeOnly=true, isIgnoreSaveReceivePlayerIndex=true)")] public PlayerManager2 hungUpPlayerManager;

        private int topicPlayerId = -1; //通信を受信した相手を保存

        private Status phoneStatus = Status.STANDBY; //リクエスト取得処理を一時中断（中断中も受信した分はリクエストリストにスタックされます）

        private float hungUpInfoTimer = 0; //deltaTime減算で時間を計測します

        private float hungUpInfoTimerMax = 3; //3秒間表示

        private bool isShowHungUpInfoUI = false; //切断時UIを表示中か

        private bool isGriping = false; //pickup中か


        private bool isHorizontalInputLock = false;//水平入力ロック

        private bool isVerticalInputLock = false;//垂直入力ロック

        //TODO:ジャンプ入力に何か割り当てる場合はコメントアウトを外す
        //private bool isJumpInputLock = false;//ジャンプ入力ロック

        private bool isUseInputLock = false;//USE入力ロック

        private byte horizontalInputValue = 0; //1:左　2:右　値を受け取って処理したら0に書き換える

        private byte verticalInputValue = 0; //1:上　2:下　値を受け取って処理したら0に書き換える

        private byte cursor = 0; //カーソル（STANDBY中のみ更新されてdisplayNameListのindexを記録します）

        private byte page = 0; //ページ（STANDBY中のみ更新されてdisplayNameListのindexを記録します）

        private float VoiceDistanceNear = 800000.0f;

        private float VoiceDistanceFar = 900000.0f;

        private byte teleportPhoneConsecutiveUseCount = 0; //転送に必要な連続USE回数カウント

        private int teleportPhoneConsecutiveFrameCount = 0; //転送に必要な連続USEの連続とみなすフレーム数のカウント

        //音量調整機能をつける場合は有効化して割り当てる
        //private float maxVoiceGain = 15.0f; //ボイス最大音量

        private bool outSideRangeCheckOneFrameBefore = false;

        private float cameraVerticalOffsetRate = 0.0f; //カメラの上下位置オフセット

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        public override void OnPlayerJoined(VRCPlayerApi player) //プレイヤーリストの描画を更新
        {
            if (phoneStatus == Status.STANDBY) ShowUISelectTargetPlayer(); //最初にユーザー選択画面を描画
        }

        public override void OnPlayerLeft(VRCPlayerApi player) //プレイヤーリストの描画を更新
        {
            if (phoneStatus == Status.STANDBY) ShowUISelectTargetPlayer(); //最初にユーザー選択画面を描画
        }

        void Start()
        {
            if (isClosedPhone) //閉じる
            {
                if (MeshOpen != null) MeshOpen.SetActive(false);
                if (UIRoot != null) UIRoot.SetActive(false);
                if (MeshClose != null) MeshClose.SetActive(true);
                if (backDisplayUIRoot != null) backDisplayUIRoot.SetActive(true);
            }
            else //開く
            {
                if (MeshOpen != null) MeshOpen.SetActive(true);
                if (UIRoot != null) UIRoot.SetActive(true);
                if (MeshClose != null) MeshClose.SetActive(false);
                if (backDisplayUIRoot != null) backDisplayUIRoot.SetActive(false);
            }
        }

        void Update()
        {
            if (callerPlayerManager == null) return;

            /*連続USEで転送する処理のリセット*/
            if (!isGriping && isTeleportPhoneConsecutiveClicks)
            {
                if (teleportPhoneConsecutiveFrameCount > 0)
                {
                    teleportPhoneConsecutiveFrameCount--;
                }
                if (teleportPhoneConsecutiveUseCount != 0 && teleportPhoneConsecutiveFrameCount <= 0) teleportPhoneConsecutiveUseCount = 0;
            }

            /*TODO:デスクトップはUSEとpickupが同時に行われるため、移動制限が開閉状態と逆にかかる可能性がある。デスクトップだけはUPDATEで監視して移動制限を処理したほうがよさそう*/

            /*圏外設定*/
            if (isSetOutSideRange)
            {
                bool outSideRangeCheck_tmp = false;
                foreach (ColliderHitGimmick tmp in outsideRangeCollider)
                {
                    if (tmp != null && tmp.GetIsStayLocalPlayer())//圏外
                    {
                        outSideRangeCheck_tmp = true;
                    }
                }
                if (outSideRangeCheck_tmp)
                {
                    if(topicPlayerId >= 0) //誰かとつながっている場合は強制切断
                    {
                        HungUp();
                    }
                    if (!outSideRangeCheckOneFrameBefore)
                    {
                        if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\n\n\n\n\n                    圏外"; /*テキストをセット*/
                        ShowUIHungUpInfo(); /*インフォメーションを表示*/
                        /*エラーSEを再生（PlayOneShot） 圏外に入ったときサウンドを再生したいならコメントを解除*/
                        //if (_audioSource != null && sound8 != null) _audioSource.PlayOneShot(sound8);
                    }
                }
                else
                {
                    if (outSideRangeCheckOneFrameBefore)
                    {
                        hungUpInfoTimer = 0; //圏外になった状態から圏外を抜けたときは速やかに切断時UIを解除する/*決定SEを再生（非ループ）　圏外に入ったときサウンドを再生したいならコメントを解除*/
                        /*if (_audioSource != null && sound3 != null)
                        {
                            _audioSource.Stop();
                            _audioSource.clip = sound3;
                            _audioSource.loop = false;
                            _audioSource.Play();
                        }*/
                    }
                }
                outSideRangeCheckOneFrameBefore = outSideRangeCheck_tmp;
                if (outSideRangeCheck_tmp) return; //切断時UIを使って表示しますが、圏外エリア立ち入り中は切断時UIの終了カウントを進めません
            }

            /*切断時UIを一定時間後に非表示にする処理　この処理中はすべての受信を止める*/
            if (isShowHungUpInfoUI)
            {
                hungUpInfoTimer -= Time.deltaTime;
                if (hungUpInfoTimer <= 0)
                {
                    HideUIHungUpInfo(); /*hungUpInfoUIを非表示にする*/
                }
                return;
            }

            int sendPlayerId_tmp = -1;
            int hungUpPlayerId_tmp = -1;

            /*通話リクエスト受信確認処理*/
            if (phoneStatus == Status.STANDBY || phoneStatus == Status.CONSIDERING_CALLER || phoneStatus == Status.CALLING) //STANDBY中とCONSIDERING_CALLER、CALLING中以外はスタックして電話が終わるのを待つ
            {
                sendPlayerId_tmp = callerPlayerManager.GetSendPlayerId();
            }
            
            /*切断リクエスト受信確認・切断処理*/
            hungUpPlayerId_tmp = hungUpPlayerManager.GetSendPlayerId();
            if (hungUpPlayerId_tmp != -1 && VRCPlayerApi.GetPlayerById(hungUpPlayerId_tmp) != null) //切断リクエストが来たときの処理
            {
                if (topicPlayerId == hungUpPlayerId_tmp) //切断リクエストがトピックプレイヤーだった場合
                {
                    HungUp();
                    return;
                }
                else if(sendPlayerId_tmp == hungUpPlayerId_tmp) //同フレームで通話リクエストと切断リクエストを受信している場合
                {
                    /*受信を打ち消す*/
                    sendPlayerId_tmp = -1;
                    hungUpPlayerId_tmp = -1;
                }
                /*トピックプレイヤー以外からの切断リクエストは無視します。*/
                //TODO:圏外中や通話中に保留で受けたリクエストについて受信リクエストと切断リクエストが同フレームで存在するようになり、現状は切断リクエストが無視されて通話受信リクエストのみが残ってしまう。この位置でそれを検知して打ち消すようにしたい。この問題は同フレームで受信しない限り発生しない
            }

            if (phoneStatus == Status.STANDBY) /*スタンバイステータス*/
            {
                if (sendPlayerId_tmp != -1 && VRCPlayerApi.GetPlayerById(sendPlayerId_tmp) != null) //着信
                {
                    if (DebugText != null) DebugText.text += "\n着信";
                    phoneStatus = Status.PENDING; //リクエストの受信をペンディングする
                    topicPlayerId = sendPlayerId_tmp; //受信中プレイヤーを記録する
                    switch (notificationMode) //通知方法別に通知を開始
                    {
                        case NotificationMethod.SILENT:
                            if (isTeleportPhoneWhenGetCall && isTeleportPhoneAlwaysGetCall) TeleportPhone();
                            //PendingUI表示のみなのでなにもしない
                            break;
                        case NotificationMethod.SOUND:
                            /*着信音SEを再生（ループ）*/
                            if(_audioSource != null && sound1 != null)
                            {
                                if (isTeleportPhoneWhenGetCall) TeleportPhone();
                                _audioSource.Stop();
                                _audioSource.clip = sound1;
                                _audioSource.loop = true;
                                _audioSource.Play();
                            }
                            break;
                        case NotificationMethod.WHEREVER_SOUND:
                            /*着信音SEを再生（ループ）*/
                            if (_audioSource != null && sound1 != null)
                            {
                                if (isTeleportPhoneWhenGetCall) TeleportPhone();
                                _audioSource.Stop();
                                _audioSource.clip = sound1;
                                _audioSource.loop = true;
                                _audioSource.Play();
                            }
                            break;
                        case NotificationMethod.VIBRATION:
                            if (isTeleportPhoneWhenGetCall) TeleportPhone();
                            /*コントローラを振動させる*/
                            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, 1, 1);
                            break;
                        default:
                            //どれかに分類されるはずなのでこの処理は通りません。
                            break;
                    }
                    ShowUIPending(); //着信ありを表示する
                }
                else if (isGriping && !isClosedPhone) //受信していなくてかつpickupしていて開いた状態のときは送信先選択が可能
                {
                    /*プレイヤー選択処理*/
                    int tmp_playerNum = callerPlayerManager.GetPlayerNum();
                    if (cursor >= tmp_playerNum) //プレイヤーが抜けたりなどで選択していたプレイヤーの位置が空欄になってしまった場合今いる一番indexの大きいプレイヤーにカーソルを合わせる
                    {
                        cursor = (byte)(tmp_playerNum - 1);
                        page = (byte)(cursor / (byte)10);
                        ShowUISelectTargetPlayer(); //displayに描画
                    }

                    if (verticalInputValue == 1) //カーソルを上に移動する処理
                    {
                        if (cursor > 0) cursor--;
                        page = (byte)(cursor / (byte)10);
                        ShowUISelectTargetPlayer(); //displayに描画
                    }
                    else if (verticalInputValue == 2) //カーソルを下に移動する処理
                    {
                        if (cursor < tmp_playerNum - 1) cursor++; //cursorはindexなので要素数-1が最大値です。
                        page = (byte)(cursor / (byte)10);
                        ShowUISelectTargetPlayer(); //displayに描画
                    }
                    verticalInputValue = 0; //入力を処理したら次のフレームでは処理しないように0にしておく
                    if (horizontalInputValue == 1) //戻る
                    {
                        /*通知方法変更処理*/
                        switch (notificationMode) //通知方法別に通知を開始
                        {
                            case NotificationMethod.SILENT:
                                notificationMode = NotificationMethod.SOUND;
                                _audioSource.spatialBlend = 1.0f;
                                if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\n\nサウンドモード\nに変更しました\n\n着信音を鳴らして\n通知します";
                                ShowUIHungUpInfo(); /*インフォメーションを表示*/
                                /*決定SEを再生（非ループ）*/
                                if (_audioSource != null && sound3 != null)
                                {
                                    _audioSource.Stop();
                                    _audioSource.clip = sound3;
                                    _audioSource.loop = false;
                                    _audioSource.Play();
                                }
                                break;
                            case NotificationMethod.SOUND:
                                notificationMode = NotificationMethod.WHEREVER_SOUND;
                                _audioSource.spatialBlend = 0.0f;
                                if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\n距離無視サウンドモード\nに変更しました\n\n離れた場所にいても\n着信音を鳴らして\n通知します";
                                ShowUIHungUpInfo(); /*インフォメーションを表示*/
                                /*決定SEを再生（非ループ）*/
                                if (_audioSource != null && sound3 != null)
                                {
                                    _audioSource.Stop();
                                    _audioSource.clip = sound3;
                                    _audioSource.loop = false;
                                    _audioSource.Play();
                                }
                                break;
                            case NotificationMethod.WHEREVER_SOUND:
                                notificationMode = NotificationMethod.VIBRATION;
                                _audioSource.spatialBlend = 1.0f;
                                if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\nバイブレーションモード\nに変更しました\n\n\n\nVRコントローラの振動で\n通知します";
                                ShowUIHungUpInfo(); /*インフォメーションを表示*/
                                /*決定SEを再生（非ループ）*/
                                if (_audioSource != null && sound3 != null)
                                {
                                    _audioSource.Stop();
                                    _audioSource.clip = sound3;
                                    _audioSource.loop = false;
                                    _audioSource.Play();
                                }
                                break;
                            case NotificationMethod.VIBRATION:
                                notificationMode = NotificationMethod.SILENT;
                                _audioSource.spatialBlend = 1.0f;
                                if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\nサイレントモード\nに変更しました\n\n画面の着信表示のみで\n通知します";
                                ShowUIHungUpInfo(); /*インフォメーションを表示*/
                                /*決定SEを再生（非ループ）*/
                                if (_audioSource != null && sound3 != null)
                                {
                                    _audioSource.Stop();
                                    _audioSource.clip = sound3;
                                    _audioSource.loop = false;
                                    _audioSource.Play();
                                }
                                break;
                            default:
                                //どれかに分類されるはずなのでこの処理は通りません。
                                break;
                        }
                    }
                    else if (horizontalInputValue == 2) //決定
                    {
                        /*電話をかける処理*/
                        int tmp_playerId = callerPlayerManager.GetPlayerIdFromIndex((int)cursor);
                        if (tmp_playerId != Networking.LocalPlayer.playerId && tmp_playerId >= 0)
                        {
                            bool result = callerPlayerManager.SendRequest(tmp_playerId);
                            if (result)
                            {
                                topicPlayerId = tmp_playerId;
                                phoneStatus = Status.CONSIDERING_CALLER;
                                ShowUISending(); //送信中UIを表示
                                /*呼び出し中SEを再生（ループ）*/
                                if (_audioSource != null && sound2 != null)
                                {
                                    _audioSource.Stop();
                                    _audioSource.clip = sound2;
                                    _audioSource.loop = true;
                                    _audioSource.Play();
                                }
                            }
                            else
                            {
                                /*電話をかけるのが失敗たことをフィードバック：呼び出すことができませんでした*/
                                if (DebugText != null) DebugText.text += "\nERROR:呼び出すことができませんでした";
                                if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\n\n\n\n\n呼び出すことができません\nでした"; /*テキストをセット*/
                                ShowUIHungUpInfo(); /*インフォメーションを表示*/

                                /*エラーSEを再生（PlayOneShot）*/
                                if (_audioSource != null && sound8 != null) _audioSource.PlayOneShot(sound8);
                            }
                        }
                        else
                        {
                            /*電話をかけるのが失敗たことをフィードバック：その相手に電話をかけることはできません*/
                            if (DebugText != null) DebugText.text += "\nERROR:その相手に電話をかけることはできません";
                            if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\n\n\n\n\nその相手に電話をかけること\nはできません"; /*テキストをセット*/
                            ShowUIHungUpInfo(); /*インフォメーションを表示*/

                            /*エラーSEを再生（PlayOneShot）*/
                            if (_audioSource != null && sound8 != null) _audioSource.PlayOneShot(sound8);

                            //MEMO:自分で自分に電話をかけようとしたときの処理。機能を追加したい場合ここに追加することも可能だが、displayListは全員共通の順序になるため自分が下のほうにいる可能性があることに注意。使いやすさの面ではよくないので実装していない。
                        }
                    }
                    horizontalInputValue = 0; //入力を処理したら次のフレームでは処理しないように0にしておく
                }
            }
            else if (phoneStatus == Status.PENDING)/*呼び出し中ステータス*/
            {
                /*呼び出し通知処理（Status.STANDBYからの切り替え時に呼ぶのでUPDATEでは処理しない）*/
                if (isGriping)
                {
                    phoneStatus = Status.CONSIDERING_RECEIVER;
                    ShowUIReceived(); //応答するかどうかのUIを表示
                    /*決定SEを再生（非ループ）*/
                    if (_audioSource != null && sound3 != null)
                    {
                        _audioSource.Stop();
                        _audioSource.clip = sound3;
                        _audioSource.loop = false;
                        _audioSource.Play();
                    }
                }else if(notificationMode == NotificationMethod.VIBRATION)
                {
                    Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.25f, 1, 1);
                }
            }
            else if (phoneStatus == Status.CONSIDERING_CALLER)/*電話をかけた人の待機中ステータス*/
            {
                VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
                if (sendPlayerId_tmp != -1 && VRCPlayerApi.GetPlayerById(sendPlayerId_tmp) != null) //通話リクエストが来たときの処理
                {
                    if (topicPlayerId == sendPlayerId_tmp) //通話リクエストが現在送信中の相手だった場合
                    {
                        phoneStatus = Status.CALLING; //通話開始
                        ShowUICalling(); //通話中UI表示
                        /*通話相手の音声を拡張する処理*/
                        topicPlayer_tmp.SetVoiceGain(VoiceGain);
                        topicPlayer_tmp.SetVoiceDistanceNear(VoiceDistanceNear);
                        topicPlayer_tmp.SetVoiceDistanceFar(VoiceDistanceFar);
                        /*決定SEを再生（非ループ）*/
                        if (_audioSource != null && sound3 != null)
                        {
                            _audioSource.Stop();
                            _audioSource.clip = sound3;
                            _audioSource.loop = false;
                            _audioSource.Play();
                        }
                    }
                    else
                    {
                        /*通話送信中に誰かから通話リクエストがあった場合自動で切断リクエストを送りキックする*/
                        hungUpPlayerManager.SendRequest(sendPlayerId_tmp);
                    }
                }

                /*相手が存在するか確認*/
                if (topicPlayer_tmp == null) //通話中のプレイヤーが消滅したとき
                {
                    HungUp(); //切断する
                }

                /*切断入力判定処理*/
                if (isGriping && !isClosedPhone) //受信していなくてかつpickupしていて開いた状態のときは送信先選択が可能
                {
                    if (horizontalInputValue == 1) //切断
                    {
                        HungUp(); //切断する
                    }
                    //CONSIDERING_CALLERで決定の入力は処理しない

                    horizontalInputValue = 0; //入力を処理したら次のフレームでは処理しないように0にしておく
                }
            }
            else if (phoneStatus == Status.CONSIDERING_RECEIVER)/*電話を受けた人の待機中ステータス*/
            {
                /*通話相手に追従してカメラ表示する処理*/
                TrackingCamera();

                /*切断・応答入力判定処理*/
                if (isGriping && !isClosedPhone) //受信していなくてかつpickupしていて開いた状態のときは送信先選択が可能
                {
                    if (horizontalInputValue == 1) //切断
                    {
                        HungUp(); //切断する
                    }
                    else if (horizontalInputValue == 2) //応答
                    {
                        /*応答する処理*/
                        if (topicPlayerId != Networking.LocalPlayer.playerId && topicPlayerId >= 0)
                        {
                            bool result = callerPlayerManager.SendRequest(topicPlayerId);
                            if (result)
                            {
                                phoneStatus = Status.CALLING; //通話開始
                                ShowUICalling(); //通話中UI表示
                                /*通話相手の音声を拡張する処理*/
                                VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
                                topicPlayer_tmp.SetVoiceGain(VoiceGain);
                                topicPlayer_tmp.SetVoiceDistanceNear(VoiceDistanceNear);
                                topicPlayer_tmp.SetVoiceDistanceFar(VoiceDistanceFar);
                                /*決定SEを再生（非ループ）*/
                                if (_audioSource != null && sound3 != null)
                                {
                                    _audioSource.Stop();
                                    _audioSource.clip = sound3;
                                    _audioSource.loop = false;
                                    _audioSource.Play();
                                }
                            }
                            else
                            {
                                /*電話をかけるのが失敗たことをフィードバック：応答することができませんでした*/
                                HungUp(); //切断する
                            }
                        }
                        else
                        {
                            /*電話をかけるのが失敗たことをフィードバック：不正な相手からの通話リクエスト*/
                            HungUp(); //切断する
                        }
                    }
                    horizontalInputValue = 0; //入力を処理したら次のフレームでは処理しないように0にしておく
                }
            }
            else if (phoneStatus == Status.CALLING)/*電話を受けた人の待機中ステータス*/
            {
                /*相手が存在するか確認*/
                VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
                if (topicPlayer_tmp == null) //通話中のプレイヤーが消滅したとき
                {
                    HungUp(); //切断する
                }

                /*通話中の受信処理*/
                if (topicPlayerId == sendPlayerId_tmp)
                {
                    //現在通話中の相手からの受信の場合無視（受信時の応答として送信したリクエストが初回送信として伝わって電話が成立した場合CALLINGステータス中に再度同じ人からリクエストが来ることがある）
                }
                else if (sendPlayerId_tmp != -1 && VRCPlayerApi.GetPlayerById(sendPlayerId_tmp) != null)
                {
                    /*通話が開始したら受信したリクエストをすべてキック*/
                    hungUpPlayerManager.SendRequest(sendPlayerId_tmp);
                }

                /*通話相手に追従してカメラ表示する処理*/
                TrackingCamera();

                /*入力判定処理*/
                if (isGriping && !isClosedPhone) //受信していなくてかつpickupしていて開いた状態のときは送信先選択が可能
                {
                    /*音量調整処理（あまり効果的ではなかったためオミット）*/
                    /*if (verticalInputValue == 1) //音量を上げる
                    {
                        if (VoiceGain < maxVoiceGain)
                        {
                            VoiceGain++;
                            topicPlayer_tmp.SetVoiceGain(VoiceGain);
                        }
                        else
                        {
                            ボイス音量がこれ以上上げられないことをフィードバック
                                            }
                    }
                    else if (verticalInputValue == 2) //音量を下げる
                    {
                        if (VoiceGain > 1)
                        {
                            VoiceGain--; //1以下にはならない
                            topicPlayer_tmp.SetVoiceGain(VoiceGain);
                        }
                        else
                        {
                            ボイス音量がこれ以上下げられないことをフィードバック
                                            }
                    }*/

                    /*カメラ位置調整処理*/
                    if (verticalInputValue == 1) //カメラ位置を上げる
                    {
                        cameraVerticalOffsetRate += 0.02f;
                    }
                    else if (verticalInputValue == 2) //カメラ位置を下げる
                    {
                        cameraVerticalOffsetRate -= 0.02f;
                    }
                    verticalInputValue = 0; //入力を処理したら次のフレームでは処理しないように0にしておく

                    /*切断判定処理とカメラ位置調整リセット*/
                    if (horizontalInputValue == 1) //切断
                    {
                        HungUp(); //切断する
                    }
                    else if (horizontalInputValue == 2) //カメラ位置リセット
                    {
                        cameraVerticalOffsetRate = 0.0f;
                    }
                    horizontalInputValue = 0; //入力を処理したら次のフレームでは処理しないように0にしておく
                }
            }
        }

        /*UI表示処理*/
        private void ShowUIHungUpInfo() //切断時インフォメーションUI表示処理（この表示は一定時間で消える）
        {
            isShowHungUpInfoUI = true;
            hungUpInfoTimer = hungUpInfoTimerMax; //一定時間後に消すタイマーをセット
            if (displayTextHungUp != null) displayTextHungUp.gameObject.SetActive(true);
            displayText.gameObject.SetActive(false);
        }

        private void HideUIHungUpInfo() //切断時インフォメーションを非表示にする
        {
            hungUpInfoTimer = 0;
            isShowHungUpInfoUI = false;
            if (displayTextHungUp != null) displayTextHungUp.gameObject.SetActive(false);
            displayText.gameObject.SetActive(true);
        }

        private void ShowUISelectTargetPlayer() //通話相手選択UI表示処理
        {
            if (displayText == null || displayCamera == null || faceCamera == null || backCameraDisplay == null) return;
            displayText.gameObject.SetActive(true);
            displayCamera.SetActive(false);
            faceCamera.SetActive(false);
            backCameraDisplay.SetActive(false);
            displayText.text = "離れた相手と通話ができます\n通話相手を選択してください\n";
            string[] tmpList = callerPlayerManager.GetDisplayNameList();
            int counter = 0;

            for (int i = page * 10; i < tmpList.Length; i++)
            {
                displayText.text += "\n";
                if (cursor == i)
                {
                    displayText.text += "▶";
                }
                else
                {
                    displayText.text += "　";
                }
                if (tmpList[i] != null) displayText.text += tmpList[i];
                counter++;
                if (counter >= 10) break;
            }

            int tmp_plyaerNum = callerPlayerManager.GetPlayerNum();
            if ((tmp_plyaerNum - page * 10) > 10)
            {
                displayText.text += "\n　　　　▼";
            }
            else
            {
                displayText.text += "\n";
            }
            displayText.text += "\n\n移動スティック入力\n▼▲選択　▶決定　◀着信音\n";
        }

        private void ShowUISending() //通話送信中UI表示処理
        {
            if (displayText == null || displayCamera == null || faceCamera == null || backCameraDisplay == null) return;
            displayText.gameObject.SetActive(true);
            displayCamera.SetActive(false);
            faceCamera.SetActive(false);
            backCameraDisplay.SetActive(false);
            VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
            if (topicPlayer_tmp != null) displayText.text = "\n\n\n\n\n\n\n" + topicPlayer_tmp.displayName + "\n      さんを呼び出し中です\n\n\n\n\n\n移動スティック入力\n◀切断\n";
        }

        private void ShowUIPending() //ペンディング中表示処理（着信あり）
        {
            if (displayText == null || displayCamera == null || faceCamera == null || backCameraDisplay == null) return;
            displayText.gameObject.SetActive(true);
            displayCamera.SetActive(false);
            faceCamera.SetActive(false);
            displayText.text = "\n\n\n\n\n\n\n\n                  着信あり\n\n\n\n\n\n\n\n掴む\n相手を確認";
        }

        private void ShowUIReceived() //受信時選択UI表示処理
        {
            if (displayText == null || displayCamera == null || faceCamera == null || backCameraDisplay == null) return;
            displayText.gameObject.SetActive(true);
            displayCamera.SetActive(true);
            faceCamera.SetActive(true);
            backCameraDisplay.SetActive(true);
            VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
            if (topicPlayer_tmp != null) displayText.text = "\n" + topicPlayer_tmp.displayName + "\nさんから電話です\n\n\n\n\n\n\n\n\n\n\n\n\n移動スティック入力\n▶電話に出る　◀電話を切断\n";

        }

        private void ShowUICalling() //通話中UI表示
        {
            if (displayText == null || displayCamera == null || faceCamera == null || backCameraDisplay == null) return;
            displayText.gameObject.SetActive(true);
            displayCamera.SetActive(true);
            faceCamera.SetActive(true);
            backCameraDisplay.SetActive(true);
            VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
            if (topicPlayer_tmp != null) displayText.text = "\n" + topicPlayer_tmp.displayName + "\nさんと通話中です\n\n\n\n\n\n\n\n\n\n\n\n\n移動スティック入力\n▼▲カメラ位置上下　◀切断\n▶カメラ位置リセット";
        }

        /*複数個所から呼ばれる処理*/
        private void HungUp() //切断処理
        {
            if (DebugText != null) DebugText.text += "切断";
            if (topicPlayerId != -1)
            {
                if (DebugText != null) DebugText.text += "\nplayerId:" + topicPlayerId + "に切断を送信しました";
                hungUpPlayerManager.SendRequest(topicPlayerId);
                VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
                if (topicPlayer_tmp != null)
                {
                    topicPlayer_tmp.SetVoiceGain(DefaultVoiceGain);
                    topicPlayer_tmp.SetVoiceDistanceNear(DefaultVoiceDistanceNear);
                    topicPlayer_tmp.SetVoiceDistanceFar(DefaultVoiceDistanceFar);
                }
            }
            topicPlayerId = -1;
            phoneStatus = Status.STANDBY;
            /*切断SEを再生（非ループ）*/
            if (_audioSource != null && sound4 != null)
            {
                _audioSource.Stop();
                _audioSource.clip = sound4;
                _audioSource.loop = false;
                _audioSource.Play();
            }

            ShowUISelectTargetPlayer(); //displayに描画
            if (displayTextHungUp != null) displayTextHungUp.text = "\n\n\n\n\n\n\n\n            切断されました"; /*切断時テキストをセット*/
            ShowUIHungUpInfo(); /*切断インフォメーションを表示*/
        }

        public void TrackingCamera() //カメラでトピックプレイヤーをトラッキングする処理
        {
            if (faceCamera == null) return;
            VRCPlayerApi topicPlayer_tmp = VRCPlayerApi.GetPlayerById(topicPlayerId);
            if (topicPlayer_tmp == null) return;
            Vector3 playerPos_tmp = topicPlayer_tmp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Quaternion playerRot_tmp = topicPlayer_tmp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            faceCamera.transform.rotation = playerRot_tmp;
            faceCamera.transform.position = new Vector3(playerPos_tmp.x, playerPos_tmp.y, playerPos_tmp.z);
            faceCamera.transform.position += faceCamera.transform.forward;
            faceCamera.transform.position += faceCamera.transform.up * cameraVerticalOffsetRate;
            faceCamera.transform.rotation *= Quaternion.Euler(0.0f, 180.0f, 0.0f);
        }

        /*インプットに対する処理*/
        public override void OnPickup()
        {
            isGriping = true;
            if (isStopWhenUse && !isClosedPhone)
            {
                Networking.LocalPlayer.SetWalkSpeed(0);
                Networking.LocalPlayer.SetRunSpeed(0);
                Networking.LocalPlayer.SetStrafeSpeed(0);
                Networking.LocalPlayer.SetJumpImpulse(0);
            }
        }

        public override void OnDrop()
        {
            isGriping = false;
            if (isStopWhenUse)
            {
                Networking.LocalPlayer.SetWalkSpeed(defaultWalkSpeed);
                Networking.LocalPlayer.SetRunSpeed(defaultRunSpeed);
                Networking.LocalPlayer.SetStrafeSpeed(defaultStrafeSpeed);
                Networking.LocalPlayer.SetJumpImpulse(defaultJumpPower);
            }
        }

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args) //左スティックの左右
        {
            if (!isGriping || isClosedPhone) return;
            if (value <= 0.1 && value >= -0.1) //HorizontalInputLockを解除
            {
                horizontalInputValue = 0;
                isHorizontalInputLock = false;
            }
            if (isHorizontalInputLock) return;
            if (value < -1*inputDecisionPower) //左を入力
            {
                horizontalInputValue = 1;
                isHorizontalInputLock = true;
                /*入力SEを再生（PlayOneShot）*/
                if (_audioSource != null && sound5 != null) _audioSource.PlayOneShot(sound5);
            }
            else if (value > inputDecisionPower) //右を入力
            {
                horizontalInputValue = 2;
                isHorizontalInputLock = true;
                /*入力SEを再生（PlayOneShot）*/
                if (_audioSource != null && sound5 != null) _audioSource.PlayOneShot(sound5);
            }
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args) //左スティックの上下
        {
            if (!isGriping || isClosedPhone) return;
            if (value <= 0.1 && value >= -0.1) //VerticalInputLockを解除
            {
                verticalInputValue = 0;
                isVerticalInputLock = false;
            }
            if (isVerticalInputLock) return;
            if (value < -1*inputDecisionPower)
            {
                verticalInputValue = 2;
                isVerticalInputLock = true;
                /*入力SEを再生（PlayOneShot）*/
                if (_audioSource != null && sound5 != null) _audioSource.PlayOneShot(sound5);
            }
            else if (value > inputDecisionPower)
            {
                verticalInputValue = 1;
                isVerticalInputLock = true;
                /*入力SEを再生（PlayOneShot）*/
                if (_audioSource != null && sound5 != null) _audioSource.PlayOneShot(sound5);
            }
        }

        /*オミットした実装ジャンプ入力*/
        /*public override void InputJump(bool value, UdonInputEventArgs args) //ジャンプ
        {
            if (!isGriping) return;
            *//*開閉SEを再生（PlayOneShot）*//*
            if (_audioSource != null && sound6 != null) _audioSource.PlayOneShot(sound6);
            if (!value) isJumpInputLock = false;
            if (isJumpInputLock) return;
            if (value)
            {
                isJumpInputLock = true;
                //ジャンプ入力時の実装をここに書く
            }
        }*/

        public override void Interact() //Useで開閉
        {
            /*開閉SEを再生（PlayOneShot）*/
            if (_audioSource != null && sound6 != null) _audioSource.PlayOneShot(sound6);
            isClosedPhone = !isClosedPhone;
            if (isClosedPhone) //閉じる
            {
                if (MeshOpen != null) MeshOpen.SetActive(false);
                if (UIRoot != null) UIRoot.SetActive(false);
                if (MeshClose != null) MeshClose.SetActive(true);
                if (backDisplayUIRoot != null) backDisplayUIRoot.SetActive(true);
            }
            else //開く
            {
                if (MeshOpen != null) MeshOpen.SetActive(true);
                if (UIRoot != null) UIRoot.SetActive(true);
                if (MeshClose != null) MeshClose.SetActive(false);
                if (backDisplayUIRoot != null) backDisplayUIRoot.SetActive(false);
            }   
        }

        public override void InputUse(bool value, UdonInputEventArgs args) //Use入力
        {
            if(!isTeleportPhoneConsecutiveClicks) return;
            if (isGriping) return; //pickupしていないとき
            if (!value) isUseInputLock = false;
            if (isUseInputLock) return;
            if (value)
            {
                isUseInputLock = true;
                teleportPhoneConsecutiveUseCount++;
                teleportPhoneConsecutiveFrameCount = teleportPhoneConsecutiveFrameNum;
                if (teleportPhoneConsecutiveUseCount >= teleportPhoneConsecutiveUseNum)
                {
                    TeleportPhone();
                }
            }
        }

        public void TeleportPhone() //目の前に転送
        {
            Vector3 playerPos_tmp = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Quaternion playerRot_tmp = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            this.gameObject.transform.rotation = playerRot_tmp;
            this.gameObject.transform.position = new Vector3(playerPos_tmp.x, playerPos_tmp.y, playerPos_tmp.z);
            this.gameObject.transform.position += this.gameObject.transform.forward / 1.5f;
            this.gameObject.transform.rotation *= Quaternion.Euler(40.0f, 180.0f, 0.0f);
            teleportPhoneConsecutiveUseCount = 0;
            teleportPhoneConsecutiveFrameCount = 0;
            /*転送時SEを再生（PlayOneShot）*/
            if (_audioSource != null && sound7 != null) _audioSource.PlayOneShot(sound7);
        }

        public void ChangeNotificationMode_SILENT() //通知方法をsilentに変更
        {
            notificationMode = NotificationMethod.SILENT;
            /*決定SEを再生（非ループ）*/
            if (_audioSource != null && sound3 != null)
            {
                _audioSource.Stop();
                _audioSource.clip = sound3;
                _audioSource.loop = false;
                _audioSource.Play();
            }
        }

        public void ChangeNotificationMode_SOUND() //通知方法をサウンドに変更
        {
            notificationMode = NotificationMethod.SOUND;
            /*決定SEを再生（非ループ）*/
            if (_audioSource != null && sound3 != null)
            {
                _audioSource.Stop();
                _audioSource.clip = sound3;
                _audioSource.loop = false;
                _audioSource.Play();
            }
        }

        public void ChangeNotificationMode_SWITCH() //通知方法を切り替え
        {
            if(notificationMode != NotificationMethod.SILENT)
            {
                ChangeNotificationMode_SOUND();
            }
            else
            {
                ChangeNotificationMode_SILENT();
            }
        }
    }
}
