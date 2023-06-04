
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace KUSAASOBIKOBO
{
    /**/
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] //同期要素はないが、このスクリプトがアタッチされているオブジェクトのIsOwnerを見て初期化するかどうかを決めているためSyncMode.Noneにはしない
    public class MultiAreaManager : UdonSharpBehaviour
    {
        //currentAreaId
        //MultiUserCurrentAreaId[80]
        //Room別VideoPlayerUrl
        //Room別VideoPlayer再生位置
        //Room別ギミックステータス

        public GameObject body;
        public MultiAreaSyncer[] _multiAreaSyncer;//_seamlessAreaManagerのentryPoints依存の要素数

        public int currentResidentIndex = -1;

        public string[] residentsName = new string[1000];
        public Vector3[] residentsBirthday = new Vector3[1000];

        public int currentAreaIndex = -1;
        private int[] beforeAreaIndex = new int[1] {-1};
        [Header("パスワードの桁数")] public short passwordDigits = 5;

        //public Vector3[] physicalAddress = new Vector3[500]; //_seamlessAreaManagerのentryPoints依存の要素数　TODO:ここでnewせずにStartで要素数0の時だけnewするようにする。外部データで入力する場合もあるためその際も0ならentryPoint依存でnewする
        //[UdonSynced(UdonSyncMode.None)] private bool[] isOpen = new bool[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        private bool[] lastIsOpen = new bool[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        private bool[] isAreaOwner = new bool[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        public bool[] isSpawnArea = new bool[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        //public bool[] isLock = new bool[500]; //_seamlessAreaManagerのentryPoints依存の要素数 defaultAreaSettingに統合
        public GameObject[] objects = new GameObject[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        public string[] defaultAreaSetting = new string[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        public string[] defaultActiveState = new string[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        public string[] materialTiling = new string[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        public string[] areaInfomationText = new string[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        //[UdonSynced(UdonSyncMode.None)] private int[] areaPassword = new int[500]; //_seamlessAreaManagerのentryPoints依存の要素数
        public GameObject[] birthDayObjects;

        public GameObject[] entryPoints;
        public Vector3[] spawnPointOffset; //entryPointsと同要素数必要
        public int[] entryPointsLinkAreaIndex; //entryPointsと同要素数必要
        public bool[] ignoreWarp; //entryPointsと同要素数必要
        [Header("ワープ判定になる距離")] public float warpDeterminationDistance = 1.0f;

        bool isSpawnReserved = false;//スポーン予約中フラグ
        bool isSpawning = false;//スポーン中フラグ
        bool isWarpWithinArea = false;//エリア内ワープ
        bool isResetDone = false;//転送前のリセットエリアに送ったか
        int warpWithinAreaIndex = -1;//エリア内ワープ先インデックス(-1など不当たりでentryPoint[0]にワープ)
        public GameObject[] warpWithinAreaPoint;//エリア内ワープのスポーン位置ゲームオブジェクト

        int entryPointsIndex = 0;
        int beforeAreaIndexSpawning = -1; //スポーンをリザーブした時いたエリアのindex

        public float fadeoutCount = 0.0f;
        public float fadeoutCountMax = 3.0f; //フェードアウト待機時間

        public ColliderHitGimmick _colliderHitGimmick;
        public OverlayUIManager overlayUI;
        public SeamlessAreaManager _seamlessAreaManager; //-1指定のエリアに相当するシームレスエリア
        public ExternalMaterialManager _externalMaterialManager;
        public AudioSource SESpeaker;
        public AudioClip changeIsOpenSound;
        public AudioClip changeSourceColorSound;
        //public AudioClip[] enterAreaSound;
        public AudioClip warpSeamlessAreaWithIndexValueSE;//ナンバー入力によるシームレスエリアへのワープのSE

        //[UdonSynced(UdonSyncMode.None)] private string[] areaState = new string[500]; //bit値を保存してフォーマットの順序でリストアします。[SourceColor, SpSkyBoxUrlId,Phys,Video等]
        //最初にエリアに入った人が生成します。初期スポーン地点はマルチエリア管轄外の位置にする必要があります。
        //そこで読み込み時間分を待機してください。このステータスが空文字の場合その人が最初の入場者とする仕様です。
        private bool[][] activeStateLocalFlag = new bool[500][]; //AreaStateのactiveState部を書き換えるローカルを優先するフラグです。原則activeStateLocalの値を入れるときに立てていこうグローバルの該当インデックスの値を無視します
        private bool[][] activeStateLocal = new bool[500][]; //AreaStateのactiveState部を書き換えるローカル値を保存します

        private bool[][] areaSettingLocalFlag = new bool[500][]; //AreaStateのareaSetting部を書き換えるローカルを優先するフラグです。原則areaSettingLocalの値を入れるときに立てていこうグローバルの該当インデックスの値を無視します
        private string[][] areaSettingLocal = new string[500][]; //AreaStateのactiveState部を書き換えるローカル値を保存します

        private string lastSyncObjectStatus = "";

        public Vector2 placementFormat; //この値を元に配置を決めます(シーン内の配置位置が始点)
        public Vector3 sizeOfArea; //この値を元に配置を決めます(最大ルームサイズ)

        public LoadExternalData externalRegisteredUsersData;
        public LoadExternalData externalDefaultAreaSettingData;
        public LoadExternalData externalDefaultActiveStateData;
        public LoadExternalData externalMaterialTilingData;

        private bool reserveSync = false;

        //private bool isGetData = false;

        private int lastDay = 0;

        public Transform resetPos;

        [Header("テレポート先入力内容表示テキスト欄")] public Text InputWarpWithinAreaIndexValueFeedback;
        [Header("テレポート先エリア名表示欄")] public Text WarpWithinAreaNameFeedback;
        [Header("テレポート先エリア名候補表示欄")] public Text WarpWithinAreaList;
        bool isWarpWithinAreaListUpdated = false;//エリア内ワープ
        private int InputWarpWithinAreaIndexValue = 0;
        public bool isIgnoreIndex0Spawn = false;

        public GameObject[] eventAreaInfomation;
        public int[] eventAreaInfomationIndex;

        public bool isUseMultiAreaBGMMain = false;

        public bool isFastLoad = false;
/*
        public BoolSyncer isOpenSyncer;

        public IntSyncer areaPasswordSyncer;

        public StringSyncer areaStateSyncer;*/

        /*public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SyncOwner");
        }*/

        /*public void SyncOwner()
        {
            RequestSerialization();
        }*/

        //TODO:処理中にリスポーンされたらすべてを解除しUIもFADEINさせる←解除するとStartUpManagerを邪魔するのでしない
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if(player == Networking.LocalPlayer)
            {
                //if (overlayUI != null) overlayUI.BlackFadeIn();
                isSpawnReserved = false;
                isSpawning = false;
                fadeoutCount = 0;
            }

            if(_seamlessAreaManager != null)
            {
                _seamlessAreaManager.gameObject.SetActive(true);

                /*//シームレスエリアのテクスチャをロードする(仮)　TODO:リスポーンマネージャーにまとめる(StartUpManager再呼び出しでもいいかも)
                _seamlessAreaManager.LoadQuickCommonTexture();
                if (_colliderHitGimmick != null && _colliderHitGimmick._timeManager != null)
                {
                    RenderSettings.skybox = _colliderHitGimmick._timeManager.currentSkybox;
                    RenderSettings.ambientLight = _colliderHitGimmick._timeManager.currentColor;
                    _colliderHitGimmick._timeManager.SetSky(_colliderHitGimmick._timeManager.GetJst());
                }*/

                body.SetActive(false);
            }  
        }

        void Update()
        {
            if (body != null && !body.activeSelf) return;

            //エリア読み込み中に同期を要求されていた場合エリア読み込み終了後に同期を行う
            if (!isSpawnReserved && !isSpawning && reserveSync)
            {
                reserveSync = false;
                Sync();
            }

            if (overlayUI != null)
            {
                if (isSpawnReserved)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            if (overlayUI.isFadeOutFinishedBlackDisplay())
                            {
                                if (!isResetDone)
                                {
                                    isResetDone = true;
                                    Networking.LocalPlayer.TeleportTo(resetPos.position, resetPos.rotation);
                                    //OverlayUIを直ちにトラッキング
                                    overlayUI.Tracking();
                                    fadeoutCount = fadeoutCountMax;
                                }
                                else
                                {
                                    fadeoutCount = 0;
                                    Spawn();
                                }
                            }
                        }
                    }
                }
                else if (isSpawning)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            if (_colliderHitGimmick != null || _colliderHitGimmick.targetExternalResourceLoadManager != null || _colliderHitGimmick.targetExternalResourceLoadManager.GetIsFinish())
                            {
                                //フェードイン
                                overlayUI.BlackFadeIn();
                                Networking.LocalPlayer.Immobilize(false);
                                isSpawning = false;
                                if (currentAreaIndex <= -1) //シームレスエリアに移動した時エリアマネージャーは自身を停止して処理負荷を下げる
                                {
                                    body.SetActive(false);
                                }
                            }
                        }
                    }
                }
                else if (isWarpWithinArea)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            if (overlayUI.isFadeOutFinishedBlackDisplay())
                            {
                                //ワープ(エリア内ワープはローカルポジション加算等はしないためそのまま値を使用)
                                if(warpWithinAreaPoint.Length > warpWithinAreaIndex && warpWithinAreaIndex >= 0)
                                {
                                    Networking.LocalPlayer.TeleportTo(warpWithinAreaPoint[warpWithinAreaIndex].transform.position, warpWithinAreaPoint[warpWithinAreaIndex].transform.rotation);
                                }
                                else
                                {
                                    if(entryPoints.Length >= 1)
                                    {
                                        Networking.LocalPlayer.TeleportTo(entryPoints[0].transform.position, entryPoints[0].transform.rotation);
                                    }
                                }

                                //OverlayUIを直ちにトラッキング
                                overlayUI.Tracking();

                                //フェードイン
                                overlayUI.BlackFadeIn();

                                //ロックを解除
                                Networking.LocalPlayer.Immobilize(false);
                                isWarpWithinArea = false;
                            }
                        }
                    }
                }
            }
            else //overlayUIが設定されていない（通知を表示させたくない場合）の処理
            {
                if (isSpawnReserved)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            if (!isResetDone)
                            {
                                isResetDone = true;
                                Networking.LocalPlayer.TeleportTo(resetPos.position, resetPos.rotation);
                                fadeoutCount = fadeoutCountMax;
                            }
                            else
                            {
                                fadeoutCount = 0;
                                Spawn();
                            }
                        }
                    }
                }
                else if (isSpawning)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            if (_colliderHitGimmick != null || _colliderHitGimmick.targetExternalResourceLoadManager != null || _colliderHitGimmick.targetExternalResourceLoadManager.GetIsFinish())
                            {
                                Networking.LocalPlayer.Immobilize(false);
                                isSpawning = false;
                                if (currentAreaIndex <= -1) //シームレスエリアに移動した時エリアマネージャーは自身を停止して処理負荷を下げる
                                {
                                    body.SetActive(false);
                                }
                            }
                        }
                    }
                }
                else if (isWarpWithinArea)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            //ワープ(エリア内ワープはローカルポジション加算等はしないためそのまま値を使用)
                            if (warpWithinAreaPoint.Length > warpWithinAreaIndex && warpWithinAreaIndex >= 0)
                            {
                                Networking.LocalPlayer.TeleportTo(warpWithinAreaPoint[warpWithinAreaIndex].transform.position, warpWithinAreaPoint[warpWithinAreaIndex].transform.rotation);
                            }
                            else
                            {
                                if (entryPoints.Length >= 1)
                                {
                                    Networking.LocalPlayer.TeleportTo(entryPoints[0].transform.position, entryPoints[0].transform.rotation);
                                }
                            }

                            //ロックを解除
                            Networking.LocalPlayer.Immobilize(false);
                            isWarpWithinArea = false;
                        }
                    }
                }
            }

            if(currentAreaIndex >= 0)
            {
                Vector3 myPosition = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                float topicDistance = 10.0f;
                for (int i = 0; i < entryPoints.Length; i++)
                {
                    if (entryPoints[i] != null && (ignoreWarp.Length <= i || (ignoreWarp.Length > i && !ignoreWarp[i] )))
                    {
                        Vector3 epTmp = entryPoints[i].transform.position;

                        //高さ(Y軸)を無視
                        epTmp = new Vector3(epTmp.x, myPosition.y, epTmp.z);
                        topicDistance = Vector3.Distance(epTmp, myPosition);
                        if (topicDistance <= warpDeterminationDistance) //隣接する家と同時に判定する範囲を指定しないこと
                        {
                            ReserveSpawn(entryPointsLinkAreaIndex[i]);
                            break;
                        }
                    }
                }

                //日が変わったらカレントエリアのbirthdayをチェック
                if (_colliderHitGimmick != null && _colliderHitGimmick._timeManager != null)
                {
                    int nowDate_tmp = _colliderHitGimmick._timeManager.GetDay();
                    if (nowDate_tmp != lastDay)
                    {
                        if(IsBirthDay(currentAreaIndex))
                        {
                            if (isSpawnReserved)
                            {
                                reserveSync = true;
                                return; //スポーン予約中は別のスポーン処理を無効化
                            }
                            if (isSpawning)
                            {
                                reserveSync = true;
                                return; //スポーン処理中は別のスポーン処理を無効化
                            }
                            Sync();
                        }
                    }
                    lastDay = nowDate_tmp;
                }
                ShowWarpWithinAreaList();
            }
        }

        public void Spawn()
        {
            //Debug.Log("CHGExecuteCheck:Spawn() areaIndex = "+currentAreaIndex);
            if (!isSpawnReserved) return; //スポーン処理予約されていない場合行わない
            if (isSpawning) return; //スポーン処理中は別のスポーン処理を無効化
            if (isWarpWithinArea) return;

            if (currentAreaIndex >= 0)
            {
                if (_multiAreaSyncer.Length <= currentAreaIndex) return;
                if (isAreaOwner.Length <= currentAreaIndex) return;
                if (materialTiling.Length <= currentAreaIndex) return;

                if (!GetIsOpen(currentAreaIndex) && !isAreaOwner[currentAreaIndex]) return;
                if (materialTiling[currentAreaIndex] == "") materialTiling[currentAreaIndex] = "1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1";              

                //マテリアルのタイリングを調整(tiling情報はステータスには含めずエリア内での操作でグローバルに変更はできない)
                if (_externalMaterialManager != null)
                {
                    string[] materialTilingStringtmp = materialTiling[currentAreaIndex].Split(',');
                    if (materialTilingStringtmp.Length != 40)
                    {
                        materialTiling[currentAreaIndex] = "1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1";
                        materialTilingStringtmp = materialTiling[currentAreaIndex].Split(',');
                    }
                    int materialTilingStringIndex = 0;
                    for (int i = 1; i <= 20; i++)
                    {
                        _externalMaterialManager.ChangeTiling(i, new Vector2(float.Parse(materialTilingStringtmp[materialTilingStringIndex]), float.Parse(materialTilingStringtmp[materialTilingStringIndex + 1])));
                        materialTilingStringIndex += 2;
                    }
                }

                if (_colliderHitGimmick != null)//コライダーヒットによるテクスチャ読み込みを無効化
                {
                    _colliderHitGimmick.isEntered = true;
                }

                //プレイヤーをentryPointsからspawnPointOffset離れた位置にテレポートさせる
                if (entryPoints.Length > entryPointsIndex && spawnPointOffset.Length > entryPointsIndex && entryPointsLinkAreaIndex.Length > entryPointsIndex)
                {
                    Vector3 spawnPointPosition = entryPoints[entryPointsIndex].transform.localPosition;
                    Quaternion spawnPointRotation = entryPoints[entryPointsIndex].transform.rotation;

                    Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + spawnPointOffset[entryPointsIndex].x, spawnPointPosition.y + spawnPointOffset[entryPointsIndex].y, spawnPointPosition.z + spawnPointOffset[entryPointsIndex].z);

                    entryPoints[entryPointsIndex].transform.localPosition = spawnPointPosition2;

                    spawnPointPosition2 = entryPoints[entryPointsIndex].transform.position;

                    entryPoints[entryPointsIndex].transform.localPosition = spawnPointPosition;

                    entryPointsLinkAreaIndex[entryPointsIndex] = beforeAreaIndex[beforeAreaIndex.Length - 1];

                    Networking.LocalPlayer.TeleportTo(spawnPointPosition2, spawnPointRotation);

                    if (_colliderHitGimmick != null)//（テクスチャ読み込み開始。ワープ時にOnPlayerTriggerEnterが発火しないことがある不具合の対策）
                    {
                        _colliderHitGimmick.ExecuteAllPlayer(Networking.LocalPlayer);
                        _colliderHitGimmick.Execute();

                        string[] areaStatus_Tmp = GetAreaState(currentAreaIndex).Split('|');
                        if (areaStatus_Tmp.Length >= 2)
                        {
                            string[] areaSetting_tmp = areaStatus_Tmp[0].Split(',');

                            /*もし空の背景が「TimeManagerを使用」だった場合、ExeCuteでは読まれないので追加で処理する*/
                            //ライティングカラーセッティング
                            int r = -1;
                            int g = -1;
                            int b = -1;

                            if (areaSettingLocalFlag[currentAreaIndex][4] && areaSettingLocalFlag[currentAreaIndex][5] && areaSettingLocalFlag[currentAreaIndex][6])
                            {
                                r = Convert.ToInt32(areaSettingLocal[currentAreaIndex][4], 10);
                                g = Convert.ToInt32(areaSettingLocal[currentAreaIndex][5], 10);
                                b = Convert.ToInt32(areaSettingLocal[currentAreaIndex][6], 10);
                            }
                            else
                            {
                                r = Convert.ToInt32(areaSetting_tmp[4], 10);
                                g = Convert.ToInt32(areaSetting_tmp[5], 10);
                                b = Convert.ToInt32(areaSetting_tmp[6], 10);
                            }

                            if (r < 0 || g < 0 || b < 0) //-1が入っている場合は外気ソースカラーをそのまま使うという意味になる
                            {
                                _colliderHitGimmick.isUseLightingControl = true; //ソースカラー制御を一時的に有効にする
                                _colliderHitGimmick.ResetLighting();
                                _colliderHitGimmick.ReloadSky();
                                _colliderHitGimmick.isUseLightingControl = true; //ソースカラー制御を無効にする
                            }
                        }
                    }
                }
                //teleport先にOverlayUIを即座にTrackingさせる
                if (overlayUI != null)
                {
                    overlayUI.Tracking();
                }

                if (_seamlessAreaManager != null) _seamlessAreaManager.gameObject.SetActive(false);
            }
            else
            {
                if (_colliderHitGimmick != null)//TODO:不具合回避のための対応。厳密調査：なぜかSeamlessAreaManagerに戻るときに元いたエリアのCHGをもう一度Enterしてしまう問題があり、Executeさせないための処理
                {
                    _colliderHitGimmick.isEntered = true;
                }
                if (_seamlessAreaManager != null) _seamlessAreaManager.gameObject.SetActive(true);

                //プレイヤーをentryPointsからspawnPointOffset離れた位置にテレポートさせる
                if (_seamlessAreaManager != null) //beforeAreaIndexSpawningのentryPoint+offsetの位置にteleportします
                {
                    _seamlessAreaManager.gameObject.SetActive(true);

                    //シームレスエリアのテクスチャをロードする
                    _seamlessAreaManager.LoadQuickCommonTexture();

                    if (_seamlessAreaManager.entryPoints.Length > beforeAreaIndexSpawning)
                    {
                        if(currentAreaIndex == -3)//シームレスエリアのマイエントリーポイントに移動
                        {
                            Vector3 spawnPointPosition = _seamlessAreaManager.myEntryPoint.transform.localPosition;
                            Quaternion spawnPointRotation = _seamlessAreaManager.myEntryPoint.transform.rotation;

                            Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + _seamlessAreaManager.spawnPointOffset.x, spawnPointPosition.y + _seamlessAreaManager.spawnPointOffset.y, spawnPointPosition.z + _seamlessAreaManager.spawnPointOffset.z);

                            _seamlessAreaManager.myEntryPoint.transform.localPosition = spawnPointPosition2;

                            spawnPointPosition2 = _seamlessAreaManager.myEntryPoint.transform.position;

                            _seamlessAreaManager.myEntryPoint.transform.localPosition = spawnPointPosition;

                            Networking.LocalPlayer.TeleportTo(spawnPointPosition2, spawnPointRotation);
                        }
                        else
                        {
                            Vector3 spawnPointPosition = _seamlessAreaManager.entryPoints[beforeAreaIndexSpawning].transform.localPosition;
                            Quaternion spawnPointRotation = _seamlessAreaManager.entryPoints[beforeAreaIndexSpawning].transform.rotation;

                            Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + _seamlessAreaManager.spawnPointOffset.x, spawnPointPosition.y + _seamlessAreaManager.spawnPointOffset.y, spawnPointPosition.z + _seamlessAreaManager.spawnPointOffset.z);

                            _seamlessAreaManager.entryPoints[beforeAreaIndexSpawning].transform.localPosition = spawnPointPosition2;

                            spawnPointPosition2 = _seamlessAreaManager.entryPoints[beforeAreaIndexSpawning].transform.position;

                            _seamlessAreaManager.entryPoints[beforeAreaIndexSpawning].transform.localPosition = spawnPointPosition;

                            if (currentAreaIndex == -2)//teleport移動定数
                            {
                                Networking.LocalPlayer.TeleportTo(spawnPointPosition2, Quaternion.Euler(spawnPointRotation.eulerAngles.x, spawnPointRotation.eulerAngles.y + 180.0f, spawnPointRotation.eulerAngles.z));
                            }
                            else
                            {
                                Networking.LocalPlayer.TeleportTo(spawnPointPosition2, spawnPointRotation);
                            }
                        }  
                    }
                    //teleport先にOverlayUIを即座にTrackingさせる
                    if (overlayUI != null)
                    {
                        overlayUI.Tracking();
                    }

                    if (_colliderHitGimmick != null && _colliderHitGimmick._timeManager != null)
                    {
                        RenderSettings.skybox = _colliderHitGimmick._timeManager.currentSkybox;
                        RenderSettings.ambientLight = _colliderHitGimmick._timeManager.currentColor;
                        _colliderHitGimmick._timeManager.isChangeSourceColor = true;
                        _colliderHitGimmick._timeManager.SetSky(_colliderHitGimmick._timeManager.GetJst(), true);

                        //コライダーに保存されたデフォルトが古くてこっちで書き換えられてしまうのであらかじめ最新化しておく
                        _colliderHitGimmick.defaultAmbientMode = RenderSettings.ambientMode;
                        _colliderHitGimmick.defaultColor = _colliderHitGimmick._timeManager.currentColor;
                    }
                }
            }

            isSpawnReserved = false;
            isSpawning = true;

            //最低フェード時間分は待つ処理
            fadeoutCount = fadeoutCountMax;
        }

        public void ReserveSpawn(int _areaIndex, int _entryPointsIndex = 0, bool _ignoreWarp = false, bool isSaveBeforeAreaIndex = false)
        {
            if (isSpawnReserved) return; //スポーン予約中は別のスポーン処理を無効化
            if (isSpawning) return; //スポーン処理中は別のスポーン処理を無効化
            if (isWarpWithinArea) return;

            if (_areaIndex >= 0)
            {
                //Debug.Log("ReserveSpawn:_areaIndex = "+ _areaIndex);
                if (_multiAreaSyncer.Length <= _areaIndex) return;
                if (isAreaOwner.Length <= _areaIndex) return;
                if (defaultAreaSetting.Length <= _areaIndex) return;
                if (defaultActiveState.Length <= _areaIndex) return;

                if (!GetIsOpen(_areaIndex) && !isAreaOwner[_areaIndex]) return;
                if (defaultAreaSetting[_areaIndex] == "") return;
                if (defaultActiveState[_areaIndex] == "") return;

                /*そのエリアにインスタンスが建ってから初めて入ったプレイヤーならステータスを更新*/
                if (_multiAreaSyncer.Length > currentAreaIndex && GetAreaState(_areaIndex) == "")
                {
                    SetAreaState((defaultAreaSetting[_areaIndex] + "|" + defaultActiveState[_areaIndex]), _areaIndex);
                    lastSyncObjectStatus = GetAreaState(_areaIndex);
                    /*if(isGetData || Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
                    {
                        SetAreaState((defaultAreaSetting[_areaIndex] + "|" + defaultActiveState[_areaIndex]), _areaIndex);
                        lastSyncObjectStatus = GetAreaState(_areaIndex);
                    }
                    else
                    {
                        SetAreaState((defaultAreaSetting[_areaIndex] + "|" + defaultActiveState[_areaIndex]), _areaIndex);
                        lastSyncObjectStatus = GetAreaState(_areaIndex);
                    }*/
                }
                if(activeStateLocalFlag[_areaIndex] == null) activeStateLocalFlag[_areaIndex] = new bool[defaultActiveState[_areaIndex].Length];
                if (activeStateLocal[_areaIndex] == null) activeStateLocal[_areaIndex] = new bool[defaultActiveState[_areaIndex].Length];
                int defaultActiveStateSplitNumTmp = defaultAreaSetting[_areaIndex].Split(',').Length;
                if(defaultActiveStateSplitNumTmp != 0)
                {
                    if (areaSettingLocalFlag[_areaIndex] == null) areaSettingLocalFlag[_areaIndex] = new bool[defaultActiveStateSplitNumTmp];
                    if (areaSettingLocal[_areaIndex] == null) areaSettingLocal[_areaIndex] = new string[defaultActiveStateSplitNumTmp];
                }

                /*
                 例えば
                エリア100
                5x5配置でサイズ113としたとき
                0,0,0から始まりy=0は24まで
                25は0,y_size*1,0になる
                つまり最初の0,0,0は0,y_size*0,0のこと
                この0は

                int y_num = (_areaIndex / ((int)x_num * (int)z_num));

                によってあらわされる
                エリア113はy_num = 4になる。

                int planePos_tmp = _areaIndex - (y_num * x_num * z_num );

                によって平面座標の特定ができて

                z_pos = (float)((planePos_tmp / x_num) * z_size);
                x_pos = (float)((planePos_tmp - ((planePos_tmp / x_num) * x_num)) * x_size);
                 */
                //マルチエリアをインデックスに応じた座標に配置
                int x_num = (int)placementFormat.x;
                int z_num = (int)placementFormat.y;
                int x_size = (int)sizeOfArea.x;
                int y_size = (int)sizeOfArea.y;
                int z_size = (int)sizeOfArea.z;

                float x_pos = 0.0f;
                float y_pos = 0.0f;
                float z_pos = 0.0f;

                int y_num = (_areaIndex / (x_num * z_num));
                int planePos_tmp = _areaIndex - (y_num * x_num * z_num);
                y_pos = (float)((y_num + 1) * y_size);
                z_pos = (float)((planePos_tmp / x_num) * z_size);
                x_pos = (float)((planePos_tmp - ((planePos_tmp / x_num) * x_num)) * x_size);


                Networking.LocalPlayer.Immobilize(true); //エリアを転送するときは先にプレイヤーの動きを止めておく（エリアからエリアに移動する際に重力による下加速が発生するのを防ぐ）

                this.gameObject.transform.position = new Vector3(x_pos, y_pos, z_pos);
   
                string[] tmp = GetAreaState(_areaIndex).Split('|');
                if (tmp.Length < 2) return;
                string[] areaSetting_tmp = tmp[0].Split(',');

                /*事前にquickCommonをロード開始する*/
                if(_colliderHitGimmick != null)
                {
                    if(_colliderHitGimmick.targetExternalResourceLoadManager != null)
                    {
                        /*現在ロード予約されている読み込みを取り消し*/
                        if(isFastLoad) _colliderHitGimmick.targetExternalResourceLoadManager.ForceResetLoadMode();
                    }

                    if ((areaSetting_tmp.Length > 3 && areaSetting_tmp[2] != "0") || (areaSettingLocalFlag[_areaIndex][2] && areaSettingLocal[_areaIndex][2] != "0"))
                    {
                        if (areaSettingLocalFlag[_areaIndex][3])
                        {
                            if (areaSettingLocal[_areaIndex][3] == "1")//4096テクスチャ利用(ここで発火判定はしない)
                            {
                                _colliderHitGimmick.isCommonTexture4096 = true;
                            }
                            else
                            {
                                _colliderHitGimmick.isCommonTexture4096 = false;
                            }
                        }
                        else
                        {
                            if (areaSetting_tmp.Length > 4 && areaSetting_tmp[3] == "1")//4096テクスチャ利用(ここで発火判定はしない)
                            {
                                _colliderHitGimmick.isCommonTexture4096 = true;
                            }
                            else
                            {
                                _colliderHitGimmick.isCommonTexture4096 = false;
                            }
                        }
                        _colliderHitGimmick.isCommonLoad = true;
                        _colliderHitGimmick.isCommonLoadQuick = true;
                        _colliderHitGimmick.isCommonLoadBeforeResetTexture = false;
                        if (areaSettingLocalFlag[_areaIndex][2])
                        {
                            _colliderHitGimmick.quickCommonUrlIndex = Convert.ToInt32(areaSettingLocal[_areaIndex][2], 10);
                        }
                        else
                        {
                            if (areaSetting_tmp.Length > 3) _colliderHitGimmick.quickCommonUrlIndex = Convert.ToInt32(areaSetting_tmp[2], 10);
                        }
                        _colliderHitGimmick.CommonLoadColliderIn();
                    }
                }
            }
            else
            {
                if (_colliderHitGimmick.targetExternalResourceLoadManager != null)
                {
                    /*現在ロード予約されている読み込みを取り消し*/
                    if (isFastLoad) _colliderHitGimmick.targetExternalResourceLoadManager.ForceResetLoadMode();
                }
                /*事前にquickCommonをロード開始する*/
                if (_seamlessAreaManager != null)_seamlessAreaManager.LoadQuickCommonTexture();
            }

            if (isSaveBeforeAreaIndex && beforeAreaIndex[beforeAreaIndex.Length - 1] != currentAreaIndex)
            {
                int length_tmp = beforeAreaIndex.Length;
                int[] beforeAreaIndex_backup = new int[beforeAreaIndex.Length];
                for(int i = 0; i < beforeAreaIndex.Length; i++)
                {
                    beforeAreaIndex_backup[i] = beforeAreaIndex[i];
                }
                beforeAreaIndex = new int[beforeAreaIndex.Length + 1];
                for (int i = 0; i < beforeAreaIndex_backup.Length; i++)
                {
                    beforeAreaIndex[i] = beforeAreaIndex_backup[i];
                }
                beforeAreaIndex[beforeAreaIndex.Length - 1] = currentAreaIndex;
            }
            entryPointsIndex = _entryPointsIndex;
            if(ignoreWarp.Length > _entryPointsIndex)
            {
                ignoreWarp[_entryPointsIndex] = _ignoreWarp;
            }

            beforeAreaIndexSpawning = currentAreaIndex;
            currentAreaIndex = _areaIndex;
            isSpawnReserved = true;
            isResetDone = false;
            Networking.LocalPlayer.Immobilize(true);

            ReflectStatus(true, true);

            //フェードアウト
            if (overlayUI != null)
            {  
                overlayUI.BlackFadeOut();
            }
            fadeoutCount = fadeoutCountMax;

            //TODO:入室時の音

            body.SetActive(true);
        }
/*
        public override void OnDeserialization()
        {
            isGetData = true;
            if(currentAreaIndex >= 0 && GetIsOpen(currentAreaIndex) != lastIsOpen[currentAreaIndex])
            {
              if(SESpeaker != null && changeIsOpenSound != null)
                {
                    SESpeaker.PlayOneShot(changeIsOpenSound);
                }
            }
            SetLastIsOpen();
            if (isSpawnReserved)
            {
                reserveSync = true;
                return; //スポーン予約中は別のスポーン処理を無効化
            }
            if (isSpawning)
            {
                reserveSync = true;
                return; //スポーン処理中は別のスポーン処理を無効化
            }
            Sync();
        }*/

        public void DeserializationSyncer()
        {
            if (currentAreaIndex >= 0 && GetIsOpen(currentAreaIndex) != lastIsOpen[currentAreaIndex])
            {
                if (SESpeaker != null && changeIsOpenSound != null)
                {
                    SESpeaker.PlayOneShot(changeIsOpenSound);
                }
            }
            SetLastIsOpen();
            if (isSpawnReserved)
            {
                reserveSync = true;
                return; //スポーン予約中は別のスポーン処理を無効化
            }
            if (isSpawning)
            {
                reserveSync = true;
                return; //スポーン処理中は別のスポーン処理を無効化
            }
            Sync();
        }

        private void SetLastIsOpen()
        {
            int length = lastIsOpen.Length;
            for(int i=0; i < length; i++)
            {
                lastIsOpen[i] = GetIsOpen(i);
            }
        }

        public void Sync()
        {
            if (currentAreaIndex < 0) return;

            if (GetAreaState(currentAreaIndex) != lastSyncObjectStatus)
            {
                ReflectStatus();
                lastSyncObjectStatus = GetAreaState(currentAreaIndex);
            }
        }

        public void SetExternalDefaultAreaSettingData()
        {
            if (externalDefaultAreaSettingData == null) return;
            bool isSetIsOpen = false;
            bool isSetPassword = false;
            bool isSetInfomationText = false;

            if (defaultAreaSetting.Length == _multiAreaSyncer.Length)
            {
                if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) isSetIsOpen = true;
            }
            if(defaultAreaSetting.Length == _multiAreaSyncer.Length)
            {
                if (Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) isSetPassword = true;
            }
            if (defaultAreaSetting.Length == areaInfomationText.Length)
            {
                isSetInfomationText = true;
            }

            for (int i = 0; i < defaultAreaSetting.Length; i++)
            {
                if (i >= externalDefaultAreaSettingData.data.Length) break;
                defaultAreaSetting[i] = externalDefaultAreaSettingData.data[i];
                string[] tmp = defaultAreaSetting[i].Split(',');
                if (isSetIsOpen)
                {
                    if (tmp.Length > 1)
                    {
                        if (tmp[1] == "1")
                        {
                            SetIsOpen(true, i, true); 
                            if (lastIsOpen.Length == _multiAreaSyncer.Length) lastIsOpen[i] = true;
                        }
                        else
                        {
                            SetIsOpen(false, i, true);
                            if (lastIsOpen.Length == _multiAreaSyncer.Length) lastIsOpen[i] = false;
                        }

                    }
                }
                if(isSetInfomationText)
                {
                    if (tmp.Length > 23)
                    {
                        areaInfomationText[i] = tmp[23].Replace(@"\n", "\n");
                    }
                }
                if (isSetPassword)
                {
                    int pass_tmp = 0;
                    pass_tmp = ((int)(Random.value * Mathf.Pow(10, (float)passwordDigits)));
                    while (pass_tmp == 0) pass_tmp = ((int)(Random.value * Mathf.Pow(10, (float)passwordDigits)));//0の場合再抽選
                    SetAreaPassword(pass_tmp, i, true);
                }
            }
            externalDefaultAreaSettingData.ClearData();
        }

        public void SetExternalDefaultActiveStateData()
        {
            if (externalDefaultActiveStateData == null) return;
            for (int i = 0; i < defaultActiveState.Length; i++)
            {
                if (i >= externalDefaultActiveStateData.data.Length) break;
                defaultActiveState[i] = externalDefaultActiveStateData.data[i];
            }
            externalDefaultActiveStateData.ClearData();
        }

        public void SetExternalMaterialTilingData()
        {
            if (externalMaterialTilingData == null) return;
            for (int i = 0; i < materialTiling.Length; i++)
            {
                if (i >= externalMaterialTilingData.data.Length) break;
                materialTiling[i] = externalMaterialTilingData.data[i];
            }
            externalMaterialTilingData.ClearData();
        }

        public void SetExternalRegisteredUsers()
        {
            if (externalRegisteredUsersData == null) return;
            bool isSetBirthDay = false;
            if (residentsName.Length == residentsBirthday.Length)
            {
                isSetBirthDay = true;
            }

            for (int i = 0; i < residentsName.Length; i++)
            {
                if (i >= externalRegisteredUsersData.data.Length) break;
                string[] tmp = externalRegisteredUsersData.data[i].Split(',');
                int tmp2 = 0;
                if (tmp.Length >= 1) residentsName[i] = tmp[0];
                if (tmp.Length >= 2)
                {
                    tmp2 = Convert.ToInt32(tmp[1], 10);
                    if(tmp2 < isAreaOwner.Length)
                    {
                        if(residentsName[i] == Networking.LocalPlayer.displayName) isAreaOwner[tmp2] = true;
                    }
                }
                if (tmp.Length >= 3)
                {
                    if (tmp2 < isSpawnArea.Length)
                    {
                        if (tmp[2] == "1" && residentsName[i] == Networking.LocalPlayer.displayName) isSpawnArea[tmp2] = true;
                        //else isSpawnArea[tmp2] = false;
                    }
                }
                if (tmp.Length >= 4 && isSetBirthDay && tmp[2] == "1")
                {
                    string[] tmp3 = tmp[3].Split('/');
                    if(tmp3.Length >= 2)
                    {
                        
                        residentsBirthday[i].x = (float)Convert.ToDouble(tmp3[0]);
                        residentsBirthday[i].y = (float)Convert.ToDouble(tmp3[1]);
                        residentsBirthday[i].z = (float)Convert.ToDouble(tmp2);
                    }
                    
                }
            }
            externalRegisteredUsersData.ClearData();
        }

        public void AreaStateChangeGlobalOnlyAreaOwner(int[] index, bool isConstTrue, int[] constFalseIndex)
        {
            if (!isAreaOwner[currentAreaIndex]) return;
            AreaStateChangeGlobal(index, isConstTrue, constFalseIndex);
        }

        public void AreaStateChangeGlobal(int[] index, bool isConstTrue, int[] constFalseIndex)
        {
            if (currentAreaIndex < 0) return;
            if (GetAreaState(currentAreaIndex) == "") return;

            string[] tmp = GetAreaState(currentAreaIndex).Split('|');
            if (tmp.Length < 2) return;
            char[] tmp2 = tmp[1].ToCharArray();

            //更新
            foreach(int index_tmp in constFalseIndex)
            {
                if (tmp2.Length <= index_tmp) return;
                if (tmp2[index_tmp] == '0' || tmp2[index_tmp] == '1') tmp2[index_tmp] = '0';
                else return;
            }

            foreach (int index_tmp in index)
            {
                if (tmp2.Length <= index_tmp) return;
                if (tmp2[index_tmp] == '0')
                {
                    tmp2[index_tmp] = '1';
                }
                else if (tmp2[index_tmp] == '1')
                {
                    if (!isConstTrue) tmp2[index_tmp] = '0';
                }
                else return;
            }
            string tmp3 = tmp[0] + "|" + new string(tmp2);
            SetAreaState(tmp3, currentAreaIndex);
            ReflectStatus();
        }

        public void AreaStateChangeLocal(int[] index, bool isConstTrue, int[] constFalseIndex)
        {
            if (currentAreaIndex < 0) return;
            if (activeStateLocal[currentAreaIndex] == null || activeStateLocalFlag[currentAreaIndex] == null) return;

            foreach(int tmp in constFalseIndex)
            {
                if (activeStateLocal[currentAreaIndex].Length <= tmp) return;
                if (activeStateLocalFlag[currentAreaIndex].Length <= tmp) return;
                activeStateLocal[currentAreaIndex][tmp] = false;
                activeStateLocalFlag[currentAreaIndex][tmp] = true;
            }

            foreach (int tmp in index)
            {
                if (activeStateLocal[currentAreaIndex].Length <= tmp) return;
                if (!isConstTrue)
                {
                    activeStateLocal[currentAreaIndex][tmp] = !activeStateLocal[currentAreaIndex][tmp];
                    activeStateLocalFlag[currentAreaIndex][tmp] = true;
                }
                else
                {
                    activeStateLocal[currentAreaIndex][tmp] = true;
                    activeStateLocalFlag[currentAreaIndex][tmp] = true;
                }
            }
            ReflectStatus();
        }

        public void AreaLightingChangeGlobal(int r, int g, int b)
        {
            if (currentAreaIndex < 0) return;
            if (GetAreaState(currentAreaIndex) == "") return;

            string[] tmp = GetAreaState(currentAreaIndex).Split('|');
            if (tmp.Length < 2) return;
            string[] areaSetting_tmp = tmp[0].Split(',');

            //更新
            areaSetting_tmp[4] = r.ToString();
            areaSetting_tmp[5] = g.ToString();
            areaSetting_tmp[6] = b.ToString();
            string tmp3 = "";
            foreach (string tmp2 in areaSetting_tmp) tmp3 += tmp2 + ",";
            tmp3 = tmp3.Remove(tmp3.Length - 1);
            tmp3 += "|" + tmp[1];
            SetAreaState(tmp3, currentAreaIndex);
            if (SESpeaker != null && changeSourceColorSound != null)
            {
                SESpeaker.PlayOneShot(changeSourceColorSound);
            }
            ReflectStatus();
        }

        public void AreaLightingChangeLocal(float r, float g, float b)
        {
            if (currentAreaIndex < 0) return;
            if (areaSettingLocal[currentAreaIndex] == null) return;
            if (areaSettingLocalFlag[currentAreaIndex] == null) return;

            areaSettingLocal[currentAreaIndex][4] = r.ToString();
            areaSettingLocal[currentAreaIndex][5] = g.ToString();
            areaSettingLocal[currentAreaIndex][6] = b.ToString();

            areaSettingLocalFlag[currentAreaIndex][4] = true;
            areaSettingLocalFlag[currentAreaIndex][5] = true;
            areaSettingLocalFlag[currentAreaIndex][6] = true;

            if (SESpeaker != null && changeSourceColorSound != null)
            {
                SESpeaker.PlayOneShot(changeSourceColorSound);
            }
            ReflectStatus();
        }

        public void ReflectStatus(bool isMute = false, bool isNotExe = false)//objectにAudioSourceがついている場合原則そのサウンドをStopしてPlayする。isMuteがついているときは再生しない
        {
            if (currentAreaIndex < 0) return;

            string[] tmp = GetAreaState(currentAreaIndex).Split('|');
            if (tmp.Length < 2) return;
            string[] areaSetting_tmp = tmp[0].Split(',');
            char[] activeState_tmp = tmp[1].ToCharArray();

            //areaSetting部を反映

            //設定値フォーマット
            //areaSetting_tmp[0] name 
            //areaSetting_tmp[1] isOpen
            //areaSetting_tmp[2] txUrlIndex //初回のみEnterで発火。それ以降は別途に呼び出す必要あり
            //areaSetting_tmp[3] TxIs4095 //初回のみEnterで発火。それ以降は別途に呼び出す必要あり
            //areaSetting_tmp[4] r
            //areaSetting_tmp[5] g
            //areaSetting_tmp[6] b
            //areaSetting_tmp[7] spSkyboxUrlIndex
            //areaSetting_tmp[8] 有効化フラグ(0か1)
            // areaSetting_tmp[9] 歩く速度
            //areaSetting_tmp[10] 走る速度
            //areaSetting_tmp[11] カニ歩き速度
            //areaSetting_tmp[12] ジャンプ力
            //areaSetting_tmp[13] ジャンプ回数
            //areaSetting_tmp[14] 無限ジャンプフラグ(0か1)
            //areaSetting_tmp[15] 重力の強さ
            //areaSetting_tmp[16] デフォルトでビデオ再生するか？
            //areaSetting_tmp[17] pcUrlIndex
            //areaSetting_tmp[18] questUrlIndex
            //areaSetting_tmp[19] isStreaming
            //areaSetting_tmp[20] bgmUrlIndex
            //areaSetting_tmp[21] birthDayBgmUrlIndex
            //areaSetting_tmp[22] birthDayObjectIndex
            //areaSetting_tmp[23] areaInfomationText
            //areaSetting_tmp[24] AlphaTxetureUrlIndex
            //areaSetting_tmp[25] posterUrlIndex1
            //areaSetting_tmp[26] posterMaxWidth1
            //areaSetting_tmp[27] posterUrlIndex2
            //areaSetting_tmp[28] posterMaxWidth2
            //areaSetting_tmp[29] posterUrlIndex3
            //areaSetting_tmp[30] posterMaxWidth3
            //areaSetting_tmp[31] posterUrlIndex4
            //areaSetting_tmp[32] posterMaxWidth4
            //areaSetting_tmp[33] posterUrlIndex5
            //areaSetting_tmp[34] posterMaxWidth5
            //areaSetting_tmp[35] posterUrlIndex6
            //areaSetting_tmp[36] posterMaxWidth6
            //areaSetting_tmp[37] BGM2
            //areaSetting_tmp[38] PlayListStartIndex
            //areaSetting_tmp[39] PlayListEndIndex

            int parameterNum = 40;
            //Debug.Log("areaSetting_tmp.Length = "+ areaSetting_tmp.Length + ", areaSettingLocalFlag[currentAreaIndex].Length" + areaSettingLocalFlag[currentAreaIndex].Length);
            if (areaSetting_tmp.Length >= parameterNum && areaSettingLocalFlag[currentAreaIndex] != null && areaSettingLocalFlag[currentAreaIndex].Length >= parameterNum && areaSettingLocal[currentAreaIndex] != null && areaSettingLocal[currentAreaIndex].Length >= parameterNum)
            {
                if (_colliderHitGimmick != null)
                {
                    bool isBirthday_tmp = IsBirthDay(currentAreaIndex);

                    //オーバーレイでエリア名を表示
                    _colliderHitGimmick.isShowOverlayText = true;
                    _colliderHitGimmick.overlayTitle = areaSetting_tmp[0];

                    bool isChanged = false;
                    //テクスチャロード関連 
                    if (areaSettingLocalFlag[currentAreaIndex][2])
                    {
                        _colliderHitGimmick.quickCommonUrlIndex = Convert.ToInt32(areaSettingLocal[currentAreaIndex][2], 10);
                    }
                    else
                    {
                        _colliderHitGimmick.quickCommonUrlIndex = Convert.ToInt32(areaSetting_tmp[2], 10);
                    }

                    if (areaSettingLocalFlag[currentAreaIndex][3])
                    {
                        if (areaSettingLocal[currentAreaIndex][3] == "1")//4096テクスチャ利用(ここで発火判定はしない)
                        {
                            _colliderHitGimmick.isCommonTexture4096 = true;
                        }
                        else
                        {
                            _colliderHitGimmick.isCommonTexture4096 = false;
                        }
                    }
                    else
                    {
                        if (areaSetting_tmp[3] == "1")//4096テクスチャ利用(ここで発火判定はしない)
                        {
                            _colliderHitGimmick.isCommonTexture4096 = true;
                        }
                        else
                        {
                            _colliderHitGimmick.isCommonTexture4096 = false;
                        }
                    }      

                    if (_colliderHitGimmick.quickCommonUrlIndex != 0)
                    {
                        if (!_colliderHitGimmick.isCommonLoad)
                        {
                            _colliderHitGimmick.isCommonLoad = true;
                            isChanged = true;
                        }
                        _colliderHitGimmick.isCommonLoadQuick = true;
                        _colliderHitGimmick.isCommonLoadBeforeResetTexture = false;


                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.CommonLoadColliderIn();
                        }
                    }
                    else
                    {
                        _colliderHitGimmick.isCommonLoad = false;
                    }

                    //ライティングカラーセッティング
                    isChanged = false;
                    int r = -1;
                    int g = -1;
                    int b = -1;

                    if (areaSettingLocalFlag[currentAreaIndex][4] && areaSettingLocalFlag[currentAreaIndex][5] && areaSettingLocalFlag[currentAreaIndex][6])
                    {
                        r = Convert.ToInt32(areaSettingLocal[currentAreaIndex][4], 10);
                        g = Convert.ToInt32(areaSettingLocal[currentAreaIndex][5], 10);
                        b = Convert.ToInt32(areaSettingLocal[currentAreaIndex][6], 10);
                    }
                    else
                    {
                        r = Convert.ToInt32(areaSetting_tmp[4], 10);
                        g = Convert.ToInt32(areaSetting_tmp[5], 10);
                        b = Convert.ToInt32(areaSetting_tmp[6], 10);
                    }

                    if (r >= 0 && g >= 0 && b >= 0) //-1が入っている場合は外気ソースカラーをそのまま使うという意味になる
                    {
                        if (!_colliderHitGimmick.isUseLightingControl)
                        {
                            _colliderHitGimmick.isUseLightingControl = true;
                            isChanged = true;
                        }
                        if (_colliderHitGimmick != null && _colliderHitGimmick._timeManager != null)
                        {
                            _colliderHitGimmick.defaultAmbientMode = RenderSettings.ambientMode;
                            _colliderHitGimmick.defaultColor = _colliderHitGimmick._timeManager.currentColor;
                        }
                        _colliderHitGimmick.isSaveStartUpLightingParameter = false;
                        float r_per = (float)r / 255.0f;
                        float g_per = (float)g / 255.0f;
                        float b_per = (float)b / 255.0f;

                        if (_colliderHitGimmick.changeColor.r != r_per || _colliderHitGimmick.changeColor.g != g_per || _colliderHitGimmick.changeColor.b != b_per)
                        {
                            _colliderHitGimmick.changeColor = new Color(r_per, g_per, b_per);
                            isChanged = true;
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.ChangeLightingColor();
                        }
                    }
                    else
                    {
                        if (_colliderHitGimmick.isUseLightingControl)
                        {
                            _colliderHitGimmick.isUseLightingControl = false; //ソースカラー制御を無効にする
                            isChanged = true;
                        }
                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.isUseLightingControl = true; //ソースカラー制御を一時的に有効にする
                            _colliderHitGimmick.ResetLighting();
                            _colliderHitGimmick.ReloadSky();
                            /*if (_colliderHitGimmick._timeManager != null)
                            {
                                RenderSettings.skybox = _colliderHitGimmick._timeManager.currentSkybox;
                                RenderSettings.ambientLight = _colliderHitGimmick._timeManager.currentColor;
                                _colliderHitGimmick._timeManager.SetSky(_colliderHitGimmick._timeManager.GetJst());
                            }*/
                            _colliderHitGimmick.isUseLightingControl = false; //ソースカラー制御を無効にする
                        }
                    }

                    //SPスカイボックスセッティング
                    isChanged = false;
                    if(areaSetting_tmp[7] != "0" || (areaSettingLocalFlag[currentAreaIndex][7] && areaSettingLocal[currentAreaIndex][7] != "0"))
                    {
                        if (!_colliderHitGimmick.isChangeSkyBox)
                        {
                            _colliderHitGimmick.isChangeSkyBox = true;
                            isChanged = true;
                        }

                        if (!_colliderHitGimmick.isSpSkyboxLoad)
                        {
                            _colliderHitGimmick.isSpSkyboxLoad = true;
                            _colliderHitGimmick.isSpSkyboxLoadQuick = true;
                            isChanged = true;
                        }
                        if(areaSettingLocalFlag[currentAreaIndex][7])
                        {
                            int quickSpSkyBoxUrlIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][7],10);
                            if (_colliderHitGimmick.quickSpSkyBoxUrlIndex != quickSpSkyBoxUrlIndex_tmp)
                            {
                                _colliderHitGimmick.quickSpSkyBoxUrlIndex = quickSpSkyBoxUrlIndex_tmp;
                                isChanged = true;
                            }
                        }
                        else
                        {
                            int quickSpSkyBoxUrlIndex_tmp2 = Convert.ToInt32(areaSetting_tmp[7], 10);
                            if (_colliderHitGimmick.quickSpSkyBoxUrlIndex != quickSpSkyBoxUrlIndex_tmp2)
                            {
                                _colliderHitGimmick.quickSpSkyBoxUrlIndex = quickSpSkyBoxUrlIndex_tmp2;
                                isChanged = true;
                            }
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.SpSkyboxLoadColliderIn();
                            _colliderHitGimmick.ChangeSkyBox();
                        }
                    }
                    else
                    {
                        if(_colliderHitGimmick.isSpSkyboxLoad)
                        {
                            _colliderHitGimmick.isSpSkyboxLoad = false;
                            isChanged = true;
                        }

                        if (_colliderHitGimmick.isChangeSkyBox)
                        {
                            _colliderHitGimmick.isChangeSkyBox = false;
                            isChanged = true;
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.isSpSkyboxLoad = true;
                            _colliderHitGimmick.ResetSkyBox();
                            _colliderHitGimmick.ReloadSky();
                            _colliderHitGimmick.isSpSkyboxLoad = false;
                        }
                    }

                    //身体能力変化セッティング
                    isChanged = false;
                    float f_tmp = 0.0f;
                    int i_tmp = 0;
                    bool phy_resetFlag = false;

                    //areaSetting_tmp[8] 有効化フラグ(0か1)
                    if (areaSettingLocalFlag[currentAreaIndex][8])
                    {  
                        if (areaSettingLocal[currentAreaIndex][8] == "1")
                        {
                            if (!_colliderHitGimmick.isPlayerPhysicalParameter)
                            {
                                _colliderHitGimmick.isPlayerPhysicalParameter = true;
                                isChanged = true;
                            }
                        }
                        else if (areaSettingLocal[currentAreaIndex][8] == "0")
                        {
                            if (_colliderHitGimmick.isPlayerPhysicalParameter)
                            {
                                _colliderHitGimmick.isPlayerPhysicalParameter = false;
                                phy_resetFlag = true;
                                isChanged = true;
                            }
                        }
                    }
                    else
                    {
                        if (areaSetting_tmp[8] == "1")
                        {
                            if (!_colliderHitGimmick.isPlayerPhysicalParameter)
                            {
                                _colliderHitGimmick.isPlayerPhysicalParameter = true;
                                isChanged = true;
                            }
                        }
                        else if (areaSetting_tmp[8] == "0")
                        {
                            if (_colliderHitGimmick.isPlayerPhysicalParameter)
                            {
                                _colliderHitGimmick.isPlayerPhysicalParameter = false;
                                phy_resetFlag = true;
                                isChanged = true;
                            }
                        }
                    }
                    if(_colliderHitGimmick.isPlayerPhysicalParameter) //有効化中のみ身体能力変化値を適用
                    {
                        // areaSetting_tmp[9] 歩く速度
                        if (areaSettingLocalFlag[currentAreaIndex][9]) f_tmp = (float)Convert.ToDouble(areaSettingLocal[currentAreaIndex][9]);
                        else f_tmp = (float)Convert.ToDouble(areaSetting_tmp[9]);
                        if (f_tmp != _colliderHitGimmick.walkSpeed)
                        {
                            _colliderHitGimmick.walkSpeed = f_tmp;
                            isChanged = true;
                        }
                        //areaSetting_tmp[10] 走る速度
                        if (areaSettingLocalFlag[currentAreaIndex][10]) f_tmp = (float)Convert.ToDouble(areaSettingLocal[currentAreaIndex][10]);
                        else f_tmp = (float)Convert.ToDouble(areaSetting_tmp[10]);
                        if (f_tmp != _colliderHitGimmick.runSpeed)
                        {
                            _colliderHitGimmick.runSpeed = f_tmp;
                            isChanged = true;
                        }
                        //areaSetting_tmp[11] カニ歩き速度
                        if (areaSettingLocalFlag[currentAreaIndex][11]) f_tmp = (float)Convert.ToDouble(areaSettingLocal[currentAreaIndex][11]);
                        else f_tmp = (float)Convert.ToDouble(areaSetting_tmp[11]);
                        if (f_tmp != _colliderHitGimmick.strafeSpeed)
                        {
                            _colliderHitGimmick.strafeSpeed = f_tmp;
                            isChanged = true;
                        }
                        //areaSetting_tmp[12] ジャンプ力
                        if (areaSettingLocalFlag[currentAreaIndex][12]) f_tmp = (float)Convert.ToDouble(areaSettingLocal[currentAreaIndex][12]);
                        else f_tmp = (float)Convert.ToDouble(areaSetting_tmp[12]);
                        if (f_tmp != _colliderHitGimmick.jumpPower)
                        {
                            _colliderHitGimmick.jumpPower = f_tmp;
                            isChanged = true;
                        }
                        //areaSetting_tmp[13] ジャンプ回数
                        if (areaSettingLocalFlag[currentAreaIndex][13]) i_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][13], 10);
                        else i_tmp = Convert.ToInt32(areaSetting_tmp[13], 10);
                        if (i_tmp != _colliderHitGimmick.maxJumpNum)
                        {
                            _colliderHitGimmick.maxJumpNum = i_tmp;
                            isChanged = true;
                        }
                        //areaSetting_tmp[14] 無限ジャンプフラグ(0か1)
                        if (areaSettingLocalFlag[currentAreaIndex][14])
                        {
                            if (areaSettingLocal[currentAreaIndex][14] == "1")
                            {
                                if (!_colliderHitGimmick.infinityJump)
                                {
                                    _colliderHitGimmick.infinityJump = true;
                                    isChanged = true;
                                }
                            }
                            else if (areaSettingLocal[currentAreaIndex][14] == "0")
                            {
                                if (_colliderHitGimmick.infinityJump)
                                {
                                    _colliderHitGimmick.infinityJump = false;
                                    isChanged = true;
                                }
                            }
                        }
                        else
                        {
                            if (areaSetting_tmp[14] == "1")
                            {
                                if (!_colliderHitGimmick.infinityJump)
                                {
                                    _colliderHitGimmick.infinityJump = true;
                                    isChanged = true;
                                }
                            }
                            else if (areaSetting_tmp[14] == "0")
                            {
                                if (_colliderHitGimmick.infinityJump)
                                {
                                    _colliderHitGimmick.infinityJump = false;
                                    isChanged = true;
                                }
                            }
                        }
                        //areaSetting_tmp[15] 重力の強さ
                        if (areaSettingLocalFlag[currentAreaIndex][12]) f_tmp = (float)Convert.ToDouble(areaSettingLocal[currentAreaIndex][15]);
                        else f_tmp = (float)Convert.ToDouble(areaSetting_tmp[15]);
                        if (f_tmp != _colliderHitGimmick.gravityStrength)
                        {
                            _colliderHitGimmick.gravityStrength = f_tmp;
                            isChanged = true;
                        }
                    }

                    //身体能力変化を反映
                    if (!isNotExe && isChanged)
                    {
                        if (phy_resetFlag)
                        {
                            _colliderHitGimmick.isPlayerPhysicalParameter = true;
                            _colliderHitGimmick.ResetPlayerPhysicalParameter();
                            _colliderHitGimmick.isPlayerPhysicalParameter = false;
                        }
                        else
                        {
                            _colliderHitGimmick.ChangePlayerPhysicalParameter();
                        }
                    }
                    
                    //ビデオセッティング
                    if(!isBirthday_tmp)
                    {
                        isChanged = false;
                        bool video_resetFlag = false;

                        //areaSetting_tmp[16] デフォルトでビデオ再生するか？
                        if (areaSettingLocalFlag[currentAreaIndex][16])
                        {
                            if (areaSettingLocal[currentAreaIndex][16] == "1")
                            {
                                if (!_colliderHitGimmick.isVideoPlayerPlayControl)
                                {
                                    _colliderHitGimmick.isVideoPlayerPlayControl = true;
                                    isChanged = true;
                                }
                            }
                            else if (areaSettingLocal[currentAreaIndex][16] == "0")
                            {
                                if (_colliderHitGimmick.isVideoPlayerPlayControl)
                                {
                                    _colliderHitGimmick.isVideoPlayerPlayControl = false;
                                    video_resetFlag = true;
                                    isChanged = true;
                                }
                            }
                        }
                        else
                        {
                            if (areaSetting_tmp[16] == "1")
                            {
                                if (!_colliderHitGimmick.isVideoPlayerPlayControl)
                                {
                                    _colliderHitGimmick.isVideoPlayerPlayControl = true;
                                    isChanged = true;
                                }
                            }
                            else if (areaSetting_tmp[16] == "0")
                            {
                                if (_colliderHitGimmick.isVideoPlayerPlayControl)
                                {
                                    _colliderHitGimmick.isVideoPlayerPlayControl = false;
                                    video_resetFlag = true;
                                    isChanged = true;
                                }
                            }
                        }
                        if (_colliderHitGimmick.isVideoPlayerPlayControl)
                        {
                            //areaSetting_tmp[17] pcUrlIndex
                            if (areaSettingLocalFlag[currentAreaIndex][17]) i_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][17], 10);
                            else i_tmp = Convert.ToInt32(areaSetting_tmp[17], 10);
                            if (i_tmp != _colliderHitGimmick.defaultUrlPcIndex)
                            {
                                _colliderHitGimmick.defaultUrlPcIndex = i_tmp;
                                isChanged = true;
                            }
                            //areaSetting_tmp[18] questUrlIndex
                            if (areaSettingLocalFlag[currentAreaIndex][18]) i_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][18], 10);
                            else i_tmp = Convert.ToInt32(areaSetting_tmp[18], 10);
                            if (i_tmp != _colliderHitGimmick.defaultUrlQuestIndex)
                            {
                                _colliderHitGimmick.defaultUrlQuestIndex = i_tmp;
                                isChanged = true;
                            }
                            //areaSetting_tmp[19] isStreaming
                            if (areaSettingLocalFlag[currentAreaIndex][19])
                            {
                                if (areaSettingLocal[currentAreaIndex][19] == "1")
                                {
                                    if (!_colliderHitGimmick.defaultIsStreaming)
                                    {
                                        _colliderHitGimmick.defaultIsStreaming = true;
                                        isChanged = true;
                                    }
                                }
                                else if (areaSettingLocal[currentAreaIndex][19] == "0")
                                {
                                    if (_colliderHitGimmick.defaultIsStreaming)
                                    {
                                        _colliderHitGimmick.defaultIsStreaming = false;
                                        isChanged = true;
                                    }
                                }
                            }
                            else
                            {
                                if (areaSetting_tmp[19] == "1")
                                {
                                    if (!_colliderHitGimmick.defaultIsStreaming)
                                    {
                                        _colliderHitGimmick.defaultIsStreaming = true;
                                        isChanged = true;
                                    }
                                }
                                else if (areaSetting_tmp[19] == "0")
                                {
                                    if (_colliderHitGimmick.defaultIsStreaming)
                                    {
                                        _colliderHitGimmick.defaultIsStreaming = false;
                                        isChanged = true;
                                    }
                                }
                            }

                            _colliderHitGimmick.SetDefaultVideoUrl();
                        }

                        if (!isNotExe && isChanged)
                        {
                            if (video_resetFlag)
                            {
                                _colliderHitGimmick.isVideoPlayerPlayControl = true;
                                _colliderHitGimmick.VideoColliderOut();
                                _colliderHitGimmick.isVideoPlayerPlayControl = false;
                            }
                            else
                            {
                                _colliderHitGimmick.VideoColliderIn();
                            }
                        }
                    }

                    //BGMセッティング
                    isChanged = false;
                    int b_index = 20;
                    if (isBirthday_tmp && !isUseMultiAreaBGMMain) b_index = 21;
                    //areaSetting_tmp[20] bgmUrlIndex
                    //areaSetting_tmp[21] birthDayBgmUrlIndex
                    //Debug.Log("areaSetting_tmp[b_index]:" + areaSetting_tmp[b_index]);
                    if (areaSetting_tmp[b_index] != "-1" || (areaSettingLocalFlag[currentAreaIndex][b_index] && areaSettingLocal[currentAreaIndex][b_index] != "-1"))
                    {
                        if (!_colliderHitGimmick.isBGMLoad)
                        {
                            _colliderHitGimmick.isBGMLoad = true;
                            isChanged = true;
                        }
                        if (areaSettingLocalFlag[currentAreaIndex][b_index])
                        {
                            int bgmIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][b_index], 10);
                            if (_colliderHitGimmick.BGMUrlIndex[0] != bgmIndex_tmp)
                            {
                                for(int i= 0; i < _colliderHitGimmick.BGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.BGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.BGMUrlIndex[0] = bgmIndex_tmp;
                                isChanged = true;
                            }
                        }
                        else
                        {
                            int bgmIndex_tmp2 = Convert.ToInt32(areaSetting_tmp[b_index], 10);
                            if (_colliderHitGimmick.BGMUrlIndex[0] != bgmIndex_tmp2)
                            {
                                for (int i = 0; i < _colliderHitGimmick.BGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.BGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.BGMUrlIndex[0] = bgmIndex_tmp2;
                                isChanged = true;
                            }
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.BGMLoadColliderIn();
                        }
                    }
                    else if (areaSetting_tmp[b_index] != "0" || (areaSettingLocalFlag[currentAreaIndex][b_index] && areaSettingLocal[currentAreaIndex][b_index] != "0"))
                    {
                        if (!_colliderHitGimmick.isBGMLoad)
                        {
                            _colliderHitGimmick.isBGMLoad = true;
                            isChanged = true;
                        }
                        if (areaSettingLocalFlag[currentAreaIndex][b_index])
                        {
                            int bgmIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][b_index], 10);
                            if (_colliderHitGimmick.BGMUrlIndex[0] != bgmIndex_tmp)
                            {
                                for (int i = 0; i < _colliderHitGimmick.BGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.BGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.BGMUrlIndex[0] = bgmIndex_tmp;
                                isChanged = true;
                            }
                        }
                        else
                        {
                            int bgmIndex_tmp2 = Convert.ToInt32(areaSetting_tmp[b_index], 10);
                            if (_colliderHitGimmick.BGMUrlIndex[0] != bgmIndex_tmp2)
                            {
                                for (int i = 0; i < _colliderHitGimmick.BGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.BGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.BGMUrlIndex[0] = bgmIndex_tmp2;
                                isChanged = true;
                            }
                        }

                        if (!isNotExe && isChanged)
                        {
                            if(_colliderHitGimmick.targetExternalResourceLoadManager != null && _colliderHitGimmick.targetExternalResourceLoadManager.exBGM != null) _colliderHitGimmick.targetExternalResourceLoadManager.exBGM.StopLoad();
                            _colliderHitGimmick.BGMLoadColliderIn();
                        }
                    }
                    else
                    {
                        if (_colliderHitGimmick.isBGMLoad)
                        {
                            _colliderHitGimmick.isBGMLoad = false;
                            isChanged = true;
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.isBGMLoad = true;
                            _colliderHitGimmick.BGMLoadColliderOut();
                            _colliderHitGimmick.isBGMLoad = false;
                        }
                    }

                    //誕生日オブジェクトの有効化
                    //areaSetting_tmp[22] birthDayObjectIndex ※birthDayObjectにローカルはありません
                    if(isBirthday_tmp)
                    {
                        int birthDayObjectIndex = Convert.ToInt32(areaSetting_tmp[22], 10);
                        for (int k = 0; k < birthDayObjects.Length; k++)
                        {
                            if(k != birthDayObjectIndex) birthDayObjects[k].SetActive(false);
                        }

                        if (birthDayObjectIndex >= 0 && birthDayObjects.Length > birthDayObjectIndex)
                        {
                            birthDayObjects[birthDayObjectIndex].SetActive(true);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < birthDayObjects.Length; k++)
                        {
                            birthDayObjects[k].SetActive(false);
                        }
                    }


                    //areaSetting_tmp[24] AlphaTxetureUrlIndex
                    isChanged = false;
                    if(_colliderHitGimmick.targetExternalResourceLoadManager._imageDownloader != null)
                    {
                        if (areaSetting_tmp[24] != "0" || (areaSettingLocalFlag[currentAreaIndex][24] && areaSettingLocal[currentAreaIndex][24] != "0"))
                        {
                            int alphaTxetureIndex_tmp;
                            if(areaSettingLocalFlag[currentAreaIndex][24]) alphaTxetureIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][24], 10);
                            else alphaTxetureIndex_tmp = Convert.ToInt32(areaSetting_tmp[24], 10);
                            if (_colliderHitGimmick.targetExternalResourceLoadManager._imageDownloader.urlIndex != alphaTxetureIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.targetExternalResourceLoadManager._imageDownloader.urlIndex = alphaTxetureIndex_tmp;
                            }

                            if (isChanged)//コライダーのExeで発火しないためchangeしていたら必ず呼ぶ
                            {
                                _colliderHitGimmick.targetExternalResourceLoadManager._imageDownloader.LoadFromUrlIndex();
                            }
                        }
                    }


                    //areaSetting_tmp[25] posterUrlIndex1　～　/areaSetting_tmp[36] posterMaxWidth6
                    isChanged = false;
                    for (int i = 25; i < 37; i+=2)
                    {
                        if (areaSetting_tmp[i] != "0" || (areaSettingLocalFlag[currentAreaIndex][i] && areaSettingLocal[currentAreaIndex][i] != "0"))
                        {
                            int posterIndex_tmp;
                            int posterMaxWidth_tmp;
                            if (areaSettingLocalFlag[currentAreaIndex][i]) posterIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][i], 10);
                            else posterIndex_tmp = Convert.ToInt32(areaSetting_tmp[i], 10);

                            if (areaSettingLocalFlag[currentAreaIndex][i + 1]) posterMaxWidth_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][i + 1], 10);
                            else posterMaxWidth_tmp = Convert.ToInt32(areaSetting_tmp[i + 1], 10);

                            if (i == 25 && _colliderHitGimmick.poster.Length > 1 && _colliderHitGimmick.poster[0].urlIndex != posterIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.SetPoster(0, posterIndex_tmp, posterMaxWidth_tmp);
                            }
                            else if (i == 27 && _colliderHitGimmick.poster.Length > 2 && _colliderHitGimmick.poster[1].urlIndex != posterIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.SetPoster(1, posterIndex_tmp, posterMaxWidth_tmp);
                            }
                            else if (i == 29 && _colliderHitGimmick.poster.Length > 3 && _colliderHitGimmick.poster[2].urlIndex != posterIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.SetPoster(2, posterIndex_tmp, posterMaxWidth_tmp);
                            }
                            else if (i == 31 && _colliderHitGimmick.poster.Length > 4 && _colliderHitGimmick.poster[3].urlIndex != posterIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.SetPoster(3, posterIndex_tmp, posterMaxWidth_tmp);
                            }
                            else if (i == 33 && _colliderHitGimmick.poster.Length > 5 && _colliderHitGimmick.poster[4].urlIndex != posterIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.SetPoster(4, posterIndex_tmp, posterMaxWidth_tmp);
                            }
                            else if (i == 35 && _colliderHitGimmick.poster.Length > 6 && _colliderHitGimmick.poster[5].urlIndex != posterIndex_tmp)
                            {
                                isChanged = true;
                                _colliderHitGimmick.SetPoster(5, posterIndex_tmp, posterMaxWidth_tmp);
                            }
                        }
                    }

                    if (isChanged)
                    {
                        _colliderHitGimmick.isLoadPoster = true;
                    }
                    else
                    {
                        _colliderHitGimmick.isLoadPoster = false;
                    }

                    if (!isNotExe && isChanged)
                    {
                        _colliderHitGimmick.LoadPosterColliderIn();
                    }

                    //BGM2セッティング
                    isChanged = false;
                    b_index = 37;
                    if (isBirthday_tmp && isUseMultiAreaBGMMain) b_index = 21;
                    //areaSetting_tmp[37] BGM2
                    if (areaSetting_tmp[b_index] != "-1" || (areaSettingLocalFlag[currentAreaIndex][b_index] && areaSettingLocal[currentAreaIndex][b_index] != "-1"))
                    {
                        if (!_colliderHitGimmick.isMultiBGMLoad)
                        {
                            _colliderHitGimmick.isMultiBGMLoad = true;
                            isChanged = true;
                        }
                        if (areaSettingLocalFlag[currentAreaIndex][b_index])
                        {
                            int multibgmIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][b_index], 10);
                            if (_colliderHitGimmick.multiBGMUrlIndex[0] != multibgmIndex_tmp)
                            {
                                for (int i = 0; i < _colliderHitGimmick.multiBGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.multiBGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.multiBGMUrlIndex[0] = multibgmIndex_tmp;
                                isChanged = true;
                            }
                        }
                        else
                        {
                            int multibgmIndex_tmp2 = Convert.ToInt32(areaSetting_tmp[b_index], 10);
                            if (_colliderHitGimmick.multiBGMUrlIndex[0] != multibgmIndex_tmp2)
                            {
                                for (int i = 0; i < _colliderHitGimmick.multiBGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.multiBGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.multiBGMUrlIndex[0] = multibgmIndex_tmp2;
                                isChanged = true;
                            }
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.MultiBGMLoadColliderIn();
                        }
                    }
                    else if (areaSetting_tmp[b_index] != "0" || (areaSettingLocalFlag[currentAreaIndex][b_index] && areaSettingLocal[currentAreaIndex][b_index] != "0"))
                    {
                        if (!_colliderHitGimmick.isMultiBGMLoad)
                        {
                            _colliderHitGimmick.isMultiBGMLoad = true;
                            isChanged = true;
                        }
                        if (areaSettingLocalFlag[currentAreaIndex][b_index])
                        {
                            int multibgmIndex_tmp = Convert.ToInt32(areaSettingLocal[currentAreaIndex][b_index], 10);
                            if (_colliderHitGimmick.multiBGMUrlIndex[0] != multibgmIndex_tmp)
                            {
                                for (int i = 0; i < _colliderHitGimmick.multiBGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.multiBGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.multiBGMUrlIndex[0] = multibgmIndex_tmp;
                                isChanged = true;
                            }
                        }
                        else
                        {
                            int multibgmIndex_tmp2 = Convert.ToInt32(areaSetting_tmp[b_index], 10);
                            if (_colliderHitGimmick.multiBGMUrlIndex[0] != multibgmIndex_tmp2)
                            {
                                for (int i = 0; i < _colliderHitGimmick.multiBGMUrlIndex.Length; i++)
                                {
                                    _colliderHitGimmick.multiBGMUrlIndex[i] = 0;
                                }
                                _colliderHitGimmick.multiBGMUrlIndex[0] = multibgmIndex_tmp2;
                                isChanged = true;
                            }
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.MultiBGMForceStop();
                            _colliderHitGimmick.MultiBGMLoadColliderIn();
                        }
                    }
                    else
                    {
                        if (_colliderHitGimmick.isBGMLoad)
                        {
                            _colliderHitGimmick.isMultiBGMLoad = false;
                            isChanged = true;
                        }

                        if (!isNotExe && isChanged)
                        {
                            _colliderHitGimmick.isMultiBGMLoad = true;
                            _colliderHitGimmick.MultiBGMLoadColliderOut();
                            _colliderHitGimmick.isMultiBGMLoad = false;
                        }
                    }

                    //プレイリストセッティング
                    isChanged = false;
                    //areaSetting_tmp[38] PlayListStartIndex
                    //areaSetting_tmp[39] PlayListEndIndex
                    int playListStartIndex = 0;
                    int playListEndIndex = 0;
                    if (areaSettingLocalFlag[currentAreaIndex][38] && areaSettingLocalFlag[currentAreaIndex][39])
                    {
                        playListStartIndex = Convert.ToInt32(areaSettingLocal[currentAreaIndex][38], 10);
                        playListEndIndex = Convert.ToInt32(areaSettingLocal[currentAreaIndex][39], 10);
                    }
                    else
                    {
                        playListStartIndex = Convert.ToInt32(areaSetting_tmp[38], 10);
                        playListEndIndex = Convert.ToInt32(areaSetting_tmp[39], 10);
                    }

                    //0は非表示プレイリストなのでセットできず、インデックスなので0以下も指定できないのでその場合は1に変更
                    if(playListStartIndex <= 0)
                    {
                        playListStartIndex = 1;
                    }

                    if (playListEndIndex <= 0)
                    {
                        playListStartIndex = 1;
                    }

                    if (_colliderHitGimmick.categoryPageRangeMin != playListStartIndex || _colliderHitGimmick.categoryPageRangeMax != playListEndIndex)
                    {
                        _colliderHitGimmick.categoryPageRangeMin = playListStartIndex;
                        _colliderHitGimmick.categoryPageRangeMax = playListEndIndex;
                        isChanged = true;
                    }

                    if (!isNotExe && isChanged)
                    {
                        if(_colliderHitGimmick != null && _colliderHitGimmick.targetExternalResourceLoadManager != null && _colliderHitGimmick.targetExternalResourceLoadManager._multiSyncVideoPlayer != null)
                        {
                            _colliderHitGimmick.targetExternalResourceLoadManager._multiSyncVideoPlayer.categoryPageRangeMin = _colliderHitGimmick.categoryPageRangeMin;
                            _colliderHitGimmick.targetExternalResourceLoadManager._multiSyncVideoPlayer.categoryPageRangeMax = _colliderHitGimmick.categoryPageRangeMax;
                            _colliderHitGimmick.targetExternalResourceLoadManager._multiSyncVideoPlayer.categoryPageIndex = _colliderHitGimmick.categoryPageRangeMin;
                            _colliderHitGimmick.targetExternalResourceLoadManager._multiSyncVideoPlayer.SetCategoryPage();
                        } 
                    }
                }
            }

            //activeState部を反映
            for (int i = 0; i < objects.Length; i++)
            {
                if(activeStateLocalFlag[currentAreaIndex] != null && activeStateLocal[currentAreaIndex] != null && activeStateLocalFlag[currentAreaIndex][i])
                {
                    if (objects[i] != null)
                    {
                       /* if (activeStateLocal[currentAreaIndex].Length >= i)
                        {
                            if (objects[i].activeSelf) objects[i].SetActive(false);
                        }*/

                        if (activeStateLocal[currentAreaIndex][i])
                        {
                            if (!objects[i].activeSelf)
                            {
                                objects[i].SetActive(true);
                                if(!isMute)
                                {
                                    AudioSource objectAudio = objects[i].GetComponent<AudioSource>();
                                    if(objectAudio != null)
                                    {
                                        objectAudio.Stop();
                                        objectAudio.Play();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (objects[i].activeSelf) objects[i].SetActive(false);
                        }
                    }
                }
                else
                {
                    if (objects[i] != null)
                    {
                        /*if (activeState_tmp.Length >= i)
                        {
                            if (objects[i].activeSelf) objects[i].SetActive(false);
                        }*/

                        if (activeState_tmp[i] == '1')
                        {
                            if (!objects[i].activeSelf)
                            {
                                objects[i].SetActive(true);
                                if (!isMute)
                                {
                                    AudioSource objectAudio = objects[i].GetComponent<AudioSource>();
                                    if (objectAudio != null)
                                    {
                                        objectAudio.Stop();
                                        objectAudio.Play();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (objects[i].activeSelf) objects[i].SetActive(false);
                        }
                    }
                }    
            }

            //EventAreaInfomationの表示
            CheckAndShowEventAreaInfomation();
        }

        public bool IsBirthDay(int index)
        {
            if (residentsName.Length != residentsBirthday.Length) return false;
            int regIndex = 0;

            foreach(Vector3 tmp in residentsBirthday)
            {
                if((int)tmp.z == index)
                {
                    if (_colliderHitGimmick != null && _colliderHitGimmick._timeManager != null)
                    {
                        bool isBirthday_tmp = _colliderHitGimmick._timeManager.IsNowFromMonth_Day(new Vector2(residentsBirthday[regIndex].x, residentsBirthday[regIndex].y));
                        if (isBirthday_tmp) return true;
                    }
                    if (residentsName[regIndex] == "") break;
                }
                regIndex++;
            }
            
            return false;
        }

        public string GetAreaName(int index)
        {
            if (index < 0) return "";
            if (index >= defaultAreaSetting.Length) return "";
            string[] tmp = defaultAreaSetting[index].Split(',');
            if (tmp.Length <= 0) return "";
            return tmp[0].Replace(@"\n", "\n");
        }

        public int GetPassword(int index)
        {
            if (index < 0) return -1;
            if (isAreaOwner[index]) return GetAreaPassword(index);
            return -1;
        }

        public bool CheckPassword(int index, int inputValue)
        {
            if (index < 0) return false;
            if (!GetIsGetAreaPassword(index)) return false;
            if (inputValue == 0) return false;//0の場合は再抽選しているためパスワードになりえない。バグって初期値になっている場合は0なためそういう時は正解にならないようにする。
            if (inputValue == GetAreaPassword(index))
            {
                isAreaOwner[index] = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OpenCurrentArea()
        {
            if (currentAreaIndex < 0) return;
            if (!GetIsGetIsOpen(currentAreaIndex)) return;
            //if (!isAreaOwner[currentAreaIndex]) return;
            SetIsOpen(true, currentAreaIndex);
            if (lastIsOpen.Length == _multiAreaSyncer.Length) lastIsOpen[currentAreaIndex] = true;
            if (SESpeaker != null && changeIsOpenSound != null)
            {
                SESpeaker.PlayOneShot(changeIsOpenSound);
            }
        }

        public void CloseCurrentArea()
        {
            if (currentAreaIndex < 0) return;
            if (!GetIsGetIsOpen(currentAreaIndex)) return;
            //if (!isAreaOwner[currentAreaIndex]) return;
            SetIsOpen(false, currentAreaIndex);
            if (lastIsOpen.Length == _multiAreaSyncer.Length) lastIsOpen[currentAreaIndex] = false;
            if (SESpeaker != null && changeIsOpenSound != null)
            {
                SESpeaker.PlayOneShot(changeIsOpenSound);
            }
        }

        public void SwitchCurrentAreaOpenStatus()
        {
            if (currentAreaIndex < 0) return;
            if(GetIsOpen(currentAreaIndex))
            {
                CloseCurrentArea();
            }
            else
            {
                OpenCurrentArea();
            }
        }


        public void SwitchCurrentAreaOpenStatusOnlyAreaOwner()
        {
            if (!isAreaOwner[currentAreaIndex]) return;
            SwitchCurrentAreaOpenStatus();
        }

        public bool IsAreaOpen(int index)
        {
            if (GetIsOpen(index) || isAreaOwner[index]) return true;
            return false;
        }

        public void CheckAndShowEventAreaInfomation()
        {
            //EventInfomationを表示
            for(int i = 0; i < _multiAreaSyncer.Length; i++)
            {
                if (eventAreaInfomationIndex.Length == eventAreaInfomation.Length)
                {
                    for (int j = 0; j < eventAreaInfomationIndex.Length; j++)
                    {
                        if (i == eventAreaInfomationIndex[j])
                        {
                            if (GetIsOpen(i))
                            {
                                if (eventAreaInfomation[j] != null && !eventAreaInfomation[j].activeSelf) eventAreaInfomation[j].SetActive(true);
                            }
                            else
                            {
                                if (eventAreaInfomation[j] != null && eventAreaInfomation[j].activeSelf) eventAreaInfomation[j].SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        public void InputWarpWithinAreaIndex_1()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 1;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 1;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_2()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 2;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 2;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_3()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 3;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 3;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_4()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 4;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 4;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_5()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 5;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 5;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_6()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 6;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 6;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_7()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 7;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 7;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_8()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 8;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 8;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_9()
        {
            if (InputWarpWithinAreaIndexValue == 0) InputWarpWithinAreaIndexValue = 9;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10 + 9;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndex_0()
        {
            if (InputWarpWithinAreaIndexValue == 0) return;
            else InputWarpWithinAreaIndexValue = InputWarpWithinAreaIndexValue * 10;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }
        public void InputWarpWithinAreaIndexClear()
        {
            InputWarpWithinAreaIndexValue = 0;
            if (InputWarpWithinAreaIndexValueFeedback != null) InputWarpWithinAreaIndexValueFeedback.text = InputWarpWithinAreaIndexValue.ToString();
            ExploreWarpWithinAreaIndex();
        }

        public void ExploreWarpWithinAreaIndex()
        {
            if (WarpWithinAreaNameFeedback != null)
            {
                if (InputWarpWithinAreaIndexValue < entryPoints.Length || InputWarpWithinAreaIndexValue >= 0)
                {
                    WarpWithinAreaNameFeedback.text = GetAreaName(InputWarpWithinAreaIndexValue);
                }
            }
        }

        public void WarpMyEntryPointInputWarpWithinAreaIndex()
        {
            if(_seamlessAreaManager != null && WarpWithinAreaNameFeedback != null) WarpWithinAreaNameFeedback.text = _seamlessAreaManager.myEntryPointName;
            //ワープSEを再生
            if (SESpeaker != null && warpSeamlessAreaWithIndexValueSE != null)
            {
                SESpeaker.PlayOneShot(warpSeamlessAreaWithIndexValueSE);
            }
            ReserveSpawn(-3);
        }
        public void WarpToSeamlessArea(int index)
        {
            if (index == 0 && isIgnoreIndex0Spawn) return;
            ReserveSpawn(-2);
            beforeAreaIndexSpawning = index;
        }
        public void WarpToSeamlessArea()
        {
            //ワープSEを再生
            if (SESpeaker != null && warpSeamlessAreaWithIndexValueSE != null)
            {
                SESpeaker.PlayOneShot(warpSeamlessAreaWithIndexValueSE);
            }

            WarpToSeamlessArea(InputWarpWithinAreaIndexValue);
        }

        public void Warp_EventArea0() { if (eventAreaInfomationIndex.Length >= 1) WarpToSeamlessArea(eventAreaInfomationIndex[0]); }
        public void Warp_EventArea1() { if (eventAreaInfomationIndex.Length >= 2) WarpToSeamlessArea(eventAreaInfomationIndex[1]); }
        public void Warp_EventArea2() { if (eventAreaInfomationIndex.Length >= 3) WarpToSeamlessArea(eventAreaInfomationIndex[2]); }
        public void Warp_EventArea3() { if (eventAreaInfomationIndex.Length >= 4) WarpToSeamlessArea(eventAreaInfomationIndex[3]); }
        public void Warp_EventArea4() { if (eventAreaInfomationIndex.Length >= 5) WarpToSeamlessArea(eventAreaInfomationIndex[4]); }
        public void Warp_EventArea5() { if (eventAreaInfomationIndex.Length >= 6) WarpToSeamlessArea(eventAreaInfomationIndex[5]); }
        public void Warp_EventArea6() { if (eventAreaInfomationIndex.Length >= 7) WarpToSeamlessArea(eventAreaInfomationIndex[6]); }
        public void Warp_EventArea7() { if (eventAreaInfomationIndex.Length >= 8) WarpToSeamlessArea(eventAreaInfomationIndex[7]); }
        public void Warp_EventArea8() { if (eventAreaInfomationIndex.Length >= 9) WarpToSeamlessArea(eventAreaInfomationIndex[8]); }
        public void Warp_EventArea9() { if (eventAreaInfomationIndex.Length >= 10) WarpToSeamlessArea(eventAreaInfomationIndex[9]); }

        public void ShowWarpWithinAreaList()
        {
            if (WarpWithinAreaList == null) return;
            if (!WarpWithinAreaList.gameObject.activeInHierarchy)
            {
                isWarpWithinAreaListUpdated = false;
                return;
            }
            if (isWarpWithinAreaListUpdated) return;
            isWarpWithinAreaListUpdated = true;
            WarpWithinAreaList.text = "";
            int length = _multiAreaSyncer.Length;
            for (int i = 0; i < length; i++)
            {
                if (GetIsOpen(i))
                {
                    WarpWithinAreaList.text += "" + i + ":" + GetAreaName(i) + "[OPEN]\n";
                }
                else if (GetAreaState(i) != "")
                {
                    WarpWithinAreaList.text += "" + i + ":" + GetAreaName(i) + "\n";
                }
            }
        }

        public void Warp(int index = -1)
        {
            if (isSpawnReserved) return; //スポーン処理予約されていない場合行わない
            if (isSpawning) return; //スポーン処理中は別のスポーン処理を無効化
            if (isWarpWithinArea) return;
            isWarpWithinArea = true;
            warpWithinAreaIndex = index;
            //フェードアウト
            if (overlayUI != null)
            {
                overlayUI.BlackFadeOut();
            }
            fadeoutCount = fadeoutCountMax;
            Networking.LocalPlayer.Immobilize(true);
        }

        //SendCustomEvent呼び出し用(マルチエリアの場合決め打ちの移動が多いため個別を用意)
        public void Warp() { Warp(warpWithinAreaIndex); }
        public void Warp0() { Warp(0); }
        public void Warp1() { Warp(1); }
        public void Warp2() { Warp(2); }
        public void Warp3() { Warp(3); }
        public void Warp4() { Warp(4); }
        public void Warp5() { Warp(5); }
        public void Warp6() { Warp(6); }
        public void Warp7() { Warp(7); }
        public void Warp8() { Warp(8); }
        public void Warp9() { Warp(9); }
        public void Warp10() { Warp(10); }
        public void Warp11() { Warp(11); }
        public void Warp12() { Warp(12); }
        public void Warp13() { Warp(13); }
        public void Warp14() { Warp(14); }
        public void Warp15() { Warp(15); }
        public void Warp16() { Warp(16); }
        public void Warp17() { Warp(17); }
        public void Warp18() { Warp(18); }
        public void Warp19() { Warp(19); }
        public void Warp20() { Warp(20); }
        public void Warp21() { Warp(21); }
        public void Warp22() { Warp(22); }
        public void Warp23() { Warp(23); }
        public void Warp24() { Warp(24); }
        public void Warp25() { Warp(25); }
        public void Warp26() { Warp(26); }
        public void Warp27() { Warp(27); }
        public void Warp28() { Warp(28); }
        public void Warp29() { Warp(29); }
        public void Warp30() { Warp(30); }
        public void Warp31() { Warp(31); }
        public void Warp32() { Warp(32); }
        public void Warp33() { Warp(33); }
        public void Warp34() { Warp(34); }
        public void Warp35() { Warp(35); }
        public void Warp36() { Warp(36); }
        public void Warp37() { Warp(37); }
        public void Warp38() { Warp(38); }
        public void Warp39() { Warp(39); }
        public void Warp40() { Warp(40); }
        public void Warp41() { Warp(41); }
        public void Warp42() { Warp(42); }
        public void Warp43() { Warp(43); }
        public void Warp44() { Warp(44); }
        public void Warp45() { Warp(45); }
        public void Warp46() { Warp(46); }
        public void Warp47() { Warp(47); }
        public void Warp48() { Warp(48); }
        public void Warp49() { Warp(49); }
        public void Warp50() { Warp(50); }
        public void Warp51() { Warp(51); }
        public void Warp52() { Warp(52); }
        public void Warp53() { Warp(53); }
        public void Warp54() { Warp(54); }
        public void Warp55() { Warp(55); }
        public void Warp56() { Warp(56); }
        public void Warp57() { Warp(57); }
        public void Warp58() { Warp(58); }
        public void Warp59() { Warp(59); }
        public void Warp60() { Warp(60); }
        public void Warp61() { Warp(61); }
        public void Warp62() { Warp(62); }
        public void Warp63() { Warp(63); }
        public void Warp64() { Warp(64); }
        public void Warp65() { Warp(65); }
        public void Warp66() { Warp(66); }
        public void Warp67() { Warp(67); }
        public void Warp68() { Warp(68); }
        public void Warp69() { Warp(69); }
        public void Warp70() { Warp(70); }
        public void Warp71() { Warp(71); }
        public void Warp72() { Warp(72); }
        public void Warp73() { Warp(73); }
        public void Warp74() { Warp(74); }
        public void Warp75() { Warp(75); }
        public void Warp76() { Warp(76); }
        public void Warp77() { Warp(77); }
        public void Warp78() { Warp(78); }
        public void Warp79() { Warp(79); }
        public void Warp80() { Warp(80); }
        public void Warp81() { Warp(81); }
        public void Warp82() { Warp(82); }
        public void Warp83() { Warp(83); }
        public void Warp84() { Warp(84); }
        public void Warp85() { Warp(85); }
        public void Warp86() { Warp(86); }
        public void Warp87() { Warp(87); }
        public void Warp88() { Warp(88); }
        public void Warp89() { Warp(89); }
        public void Warp90() { Warp(90); }
        public void Warp91() { Warp(91); }
        public void Warp92() { Warp(92); }
        public void Warp93() { Warp(93); }
        public void Warp94() { Warp(94); }
        public void Warp95() { Warp(95); }
        public void Warp96() { Warp(96); }
        public void Warp97() { Warp(97); }
        public void Warp98() { Warp(98); }
        public void Warp99() { Warp(99); }
        public void Warp100() { Warp(100); }

        public bool GetIsOpen(int index)
        {
            if(index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) return _multiAreaSyncer[index].GetIsOpen();
                else return false;
            }
            return false;
        }

        private void SetIsOpen(bool value, int index, bool isForce = false)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) _multiAreaSyncer[index].SetIsOpen(value, isForce);
            }
        }

        public bool GetIsGetIsOpen(int index)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) return _multiAreaSyncer[index].GetIsGetIsOpen();
                else return false;
            }
            return false;
        }

        public string GetAreaState(int index)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) return _multiAreaSyncer[index].GetAreaState();
                else return "";
            }
            return "";
        }

        private void SetAreaState(string value, int index, bool isForce = false)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) _multiAreaSyncer[index].SetAreaState(value, isForce);
            }
        }

        public bool GetIsGetAreaState(int index)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) return _multiAreaSyncer[index].GetIsGetAreaState();
                else return false;
            }
            return false;
        }

        private int GetAreaPassword(int index)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) return _multiAreaSyncer[index].GetAreaPassword();
                else return -1;
            }
            return -1;
        }

        private void SetAreaPassword(int value, int index, bool isForce = false)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) _multiAreaSyncer[index].SetAreaPassword(value, isForce);
            }
        }

        public bool GetIsGetAreaPassword(int index)
        {
            if (index >= 0 && index < _multiAreaSyncer.Length)
            {
                if (_multiAreaSyncer[index] != null) return _multiAreaSyncer[index].GetIsGetAreaPassword();
                else return false;
            }
            return false;
        }

        /*public void ReplicationIsOpen()
        {
            if(isOpenSyncer != null)
            {
                isOpen = isOpenSyncer.Get();
            }
        }

        public void ReplicationAreaPassword()
        {
            if (areaPasswordSyncer != null)
            {
                areaPassword = areaPasswordSyncer.Get();
            }
        }

        public void OverWriteIsOpen()
        {
            if (isOpenSyncer != null)
            {
                isOpenSyncer.Set(isOpen);
            }
        }

        public void OverWriteAreaPassword()
        {
            if (areaPasswordSyncer != null)
            {
                areaPasswordSyncer.Set(areaPassword);
            }
        }

        public void ReplicationAreaState()
        {
            if (areaStateSyncer != null)
            {
                areaState = areaStateSyncer.Get();
            }
        }*/
    }
}
