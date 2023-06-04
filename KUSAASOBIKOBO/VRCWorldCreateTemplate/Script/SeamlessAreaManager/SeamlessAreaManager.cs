
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    /**/
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SeamlessAreaManager : UdonSharpBehaviour
    {
        [Header("このエリアの初期エントリーポイント")] public Transform myEntryPoint;
        [Header("初期エントリーポイント位置の名前")] public string myEntryPointName = "始まりの場所";

        [Header("エントリーポイント")] public Transform[] entryPoints;

        [Header("エントリーポイントから出てきた際のオフセット位置")] public Vector3 spawnPointOffset = new Vector3(0.0f, 0.0f, -2.0f);

        [Header("インフォメーションを出すオフセット位置")] public Vector3 infomationOffset = new Vector3(0.0f, 0.0f, -2.0f);

        [Header("ワープ判定になる距離")] public float warpDeterminationDistance = 1.0f;

        [Header("インフォメーションを出す判定になる距離")] public float infomationDeterminationDistance = 3.0f;

        [Header("オープンエリアのインフォメーションを出す判定になる距離")] public float openInfomationDeterminationDistance = 7.0f;

        [Header("表札表示判定距離")] public float showDoorplateDeterminationDistance = 10.0f;

        [Header("近づいたときインフォメーションを出すか")] public bool[] isShowInfomation; //近づいたときパスワード入力用テンキーを表示させ入力済みの場合はこのbool値をfalseにすると表示されなくなります

        [Header("全てのエントリーポイントを処理するのにかかる時間の倍率")] public float exeTimeRate = 1;
        
        [Header("マルチエリアマネージャー")] public MultiAreaManager _multiAreaManager;

        [Header("テクスチャURLIndex(クイックロード)")] public int quickCommonUrlIndex;

        [Header("ExternalResourceLoadManager")] public ExternalResourceLoadManager _externalResourceLoadManager;

        [Header("ExternalMaterialManager")] public ExternalMaterialManager _externalMaterialManager;

        [Header("OverlayUIManager")] public OverlayUIManager overlayUI;

        [Header("パスワード入力ボード")] public GameObject infomationWindow;

        [Header("ホストウィンドウ")] public GameObject infomationWindowHost;

        [Header("テキストインフォメーションウィンドウ")] public GameObject infomationWindowTextInfomation;

        [Header("インフォメーションオープン")] public GameObject infomationOpen;

        [Header("パスワード入力ボードの入力内容表示テキスト欄")] public Text inputValueFeedback;

        [Header("エリアインフォメーションテキスト")] public Text areaInfomationText;
        [Header("パスワード入力ボードのパスワード表示欄")] public Text answerText;
        [Header("パスワード入力ボードのゲスト表示テキスト")] public string guestText = "ゲスト";
        [Header("パスワード入力ボードの間違っていた時の表示")] public string ngAnswerText = "パスワードが違います";

        private int frameRate = 60;

        private int currentExeSection = 0;

        private int sectionSize = 1;

        private bool separateExeFinish = true;

        private int showIndomationWindowTopicEntryPointIndex = 0;

        private int inputValue = 0;

        bool isWarpWithinArea = false;//エリア内ワープ
        int warpWithinAreaIndex = -1;//エリア内ワープ先インデックス(-1など不当たりでentryPoint[0]にワープ)
        public AudioSource sEAudioSource;//SEのAudioSource
        public AudioClip warpWithinAreaSE;//エリア内ワープのSE


        public float fadeoutCount = 0.0f;
        public float fadeoutCountMax = 3.0f; //フェードアウト待機時間

        [Header("テレポート先入力内容表示テキスト欄")] public Text InputWarpWithinAreaIndexValueFeedback;
        [Header("テレポート先エリア名表示欄")] public Text WarpWithinAreaNameFeedback;
        [Header("テレポート先エリア名候補表示欄")] public Text WarpWithinAreaList;
        bool isWarpWithinAreaListUpdated = false;//エリア内ワープ
        private int InputWarpWithinAreaIndexValue = 0;
        public bool isIgnoreIndex0Spawn = false;

        public GameObject[] eventAreaInfomation;
        public int[] eventAreaInfomationIndex;



        [Header("マテリアルのタイリング")] public Vector2[] materialTiling = {new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f) };

        void Start()
        {
            UpdateDoorplate();
        }

        void Update() 
        {
            Vector3 myPosition = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            float topicDistance = 10.0f;
            GameObject canvas_tmp = null;
            if (separateExeFinish)
            {
                frameRate = (int)((1f / Time.deltaTime)* exeTimeRate);
                currentExeSection = 0;
                sectionSize = entryPoints.Length / frameRate;
                if ((float)entryPoints.Length % (float)frameRate != 0f) sectionSize++;
                if (sectionSize < 1) sectionSize = 1;
                separateExeFinish = false;
            }
            for (int i = currentExeSection * sectionSize;i < ((currentExeSection+1) * sectionSize);i++ )
            {
                if (i >= entryPoints.Length) break;
                if (entryPoints[i] != null && entryPoints[i].gameObject.activeSelf/*非アクティブなentryPointを無視*/)
                {

                    //EventInfomationを表示
                    if (_multiAreaManager != null && eventAreaInfomationIndex.Length == eventAreaInfomation.Length)
                    {
                        for (int j = 0; j < eventAreaInfomationIndex.Length; j++)
                        {
                            if (i == eventAreaInfomationIndex[j])
                            {
                                if (_multiAreaManager.GetIsOpen(i))
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

                    Vector3 epTmp = entryPoints[i].position;

                    //高さ(Y軸)を無視
                    epTmp =  new Vector3(epTmp.x, myPosition.y, epTmp.z);

                    topicDistance = Vector3.Distance(epTmp, myPosition);
                    canvas_tmp = entryPoints[i].parent.gameObject.transform.parent.gameObject;
                    if (topicDistance <= warpDeterminationDistance)　//隣接する家と同時に判定する範囲を指定しないこと
                    {
                        if(_externalResourceLoadManager != null)
                        {
                            if (_externalResourceLoadManager._startUpManager.GetIsFinish())
                            {
                                //Debug.Log("Spawn:"+i+"Room!!");
                                if(_multiAreaManager != null) _multiAreaManager.ReserveSpawn(i);
                            }
                        }
                    }                 

                    //MultiAreaManagerに問い合わせてisOpenかどうか確認
                    //isLockならisOpenの状態でインフォメーションを出し分け
                    //isOpenでないならロック解除用インフォメーションを出す
                    //isOpenならロック用インフォメーションを出す
                    //isLockではないなら出さない
                    if (topicDistance <= infomationDeterminationDistance) //隣接する家と同時に判定する範囲を指定しないこと
                    {
                        if (_externalResourceLoadManager != null)
                        {
                            if (_externalResourceLoadManager._startUpManager.GetIsFinish())
                            {
                                if (_multiAreaManager != null)
                                {
                                    if (!_multiAreaManager.IsAreaOpen(i))
                                    {
                                        if (_multiAreaManager.areaInfomationText[i] == "")
                                        {
                                            if (infomationWindow != null)
                                            {
                                                showIndomationWindowTopicEntryPointIndex = i;
                                                Vector3 epTmp2 = entryPoints[i].localPosition;
                                                Quaternion epTmp3 = entryPoints[i].rotation;

                                                Vector3 epTmp4 = new Vector3(epTmp2.x + infomationOffset.x, epTmp2.y + infomationOffset.y, epTmp2.z + infomationOffset.z);
                                                entryPoints[i].localPosition = epTmp4;
                                                epTmp4 = entryPoints[i].position;
                                                infomationWindow.transform.position = new Vector3(epTmp4.x, myPosition.y, epTmp4.z);
                                                entryPoints[i].localPosition = epTmp2;
                                                epTmp3.eulerAngles = new Vector3(epTmp3.eulerAngles.x, epTmp3.eulerAngles.y - 180.0f, epTmp3.eulerAngles.z);
                                                infomationWindow.transform.rotation = epTmp3;
                                                if (!infomationWindow.activeSelf) infomationWindow.SetActive(true);
                                            }
                                        }
                                        else
                                        {
                                            if (infomationWindowTextInfomation != null)
                                            {
                                                showIndomationWindowTopicEntryPointIndex = i;
                                                Vector3 epTmp2 = entryPoints[i].localPosition;
                                                Quaternion epTmp3 = entryPoints[i].rotation;

                                                Vector3 epTmp4 = new Vector3(epTmp2.x + infomationOffset.x, epTmp2.y + infomationOffset.y, epTmp2.z + infomationOffset.z);
                                                entryPoints[i].localPosition = epTmp4;
                                                epTmp4 = entryPoints[i].position;
                                                infomationWindowTextInfomation.transform.position = new Vector3(epTmp4.x, myPosition.y, epTmp4.z);
                                                entryPoints[i].localPosition = epTmp2;
                                                epTmp3.eulerAngles = new Vector3(epTmp3.eulerAngles.x, epTmp3.eulerAngles.y - 180.0f, epTmp3.eulerAngles.z);
                                                infomationWindowTextInfomation.transform.rotation = epTmp3;

                                                if(areaInfomationText != null) areaInfomationText.text = _multiAreaManager.areaInfomationText[i];
                                                if (!infomationWindowTextInfomation.activeSelf) infomationWindowTextInfomation.SetActive(true);
                                            }
                                        } 
                                    }
                                    else if(!_multiAreaManager.GetIsOpen(i))
                                    {
                                        if (_multiAreaManager.areaInfomationText[i] == "")
                                        {
                                            if (infomationWindowHost != null)
                                            {
                                                showIndomationWindowTopicEntryPointIndex = i;
                                                Vector3 epTmp2 = entryPoints[i].localPosition;
                                                Quaternion epTmp3 = entryPoints[i].rotation;

                                                Vector3 epTmp4 = new Vector3(epTmp2.x + infomationOffset.x, epTmp2.y + infomationOffset.y, epTmp2.z + infomationOffset.z);
                                                entryPoints[i].localPosition = epTmp4;
                                                epTmp4 = entryPoints[i].position;
                                                infomationWindowHost.transform.position = new Vector3(epTmp4.x, myPosition.y, epTmp4.z);
                                                entryPoints[i].localPosition = epTmp2;
                                                epTmp3.eulerAngles = new Vector3(epTmp3.eulerAngles.x, epTmp3.eulerAngles.y - 180.0f, epTmp3.eulerAngles.z);
                                                infomationWindowHost.transform.rotation = epTmp3;

                                                if (answerText != null)
                                                {
                                                    int password_tmp = _multiAreaManager.GetPassword(i);
                                                    if (password_tmp >= 0) answerText.text = password_tmp.ToString();
                                                }
                                                if (!infomationWindowHost.activeSelf) infomationWindowHost.SetActive(true);
                                            }
                                        }
                                        else
                                        {
                                            if (infomationWindowTextInfomation != null)
                                            {
                                                showIndomationWindowTopicEntryPointIndex = i;
                                                Vector3 epTmp2 = entryPoints[i].localPosition;
                                                Quaternion epTmp3 = entryPoints[i].rotation;

                                                Vector3 epTmp4 = new Vector3(epTmp2.x + infomationOffset.x, epTmp2.y + infomationOffset.y, epTmp2.z + infomationOffset.z);
                                                entryPoints[i].localPosition = epTmp4;
                                                epTmp4 = entryPoints[i].position;
                                                infomationWindowTextInfomation.transform.position = new Vector3(epTmp4.x, myPosition.y, epTmp4.z);
                                                entryPoints[i].localPosition = epTmp2;
                                                epTmp3.eulerAngles = new Vector3(epTmp3.eulerAngles.x, epTmp3.eulerAngles.y - 180.0f, epTmp3.eulerAngles.z);
                                                infomationWindowTextInfomation.transform.rotation = epTmp3;

                                                if (areaInfomationText != null) areaInfomationText.text = _multiAreaManager.areaInfomationText[i];
                                                if (!infomationWindowTextInfomation.activeSelf) infomationWindowTextInfomation.SetActive(true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (infomationWindow != null)
                                        {
                                            if (infomationWindow.activeSelf) infomationWindow.SetActive(false);
                                            infomationWindow.transform.position = new Vector3(0.0f, -90.0f, 0.0f);
                                        }
                                        if (infomationWindowHost != null)
                                        {
                                            if (infomationWindowHost.activeSelf) infomationWindowHost.SetActive(false);
                                            infomationWindowHost.transform.position = new Vector3(0.0f, -90.0f, 0.0f);
                                        }
                                        if (infomationWindowTextInfomation != null)
                                        {
                                            if (infomationWindowTextInfomation.activeSelf) infomationWindowTextInfomation.SetActive(false);
                                            infomationWindowTextInfomation.transform.position = new Vector3(0.0f, -90.0f, 0.0f);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (_multiAreaManager != null)
                    {
                        if(topicDistance <= openInfomationDeterminationDistance)
                        {
                            if (_multiAreaManager.GetIsOpen(i))
                            {
                                if (infomationOpen != null)
                                {
                                    showIndomationWindowTopicEntryPointIndex = i;
                                    Vector3 epTmp2 = entryPoints[i].localPosition;
                                    Quaternion epTmp3 = entryPoints[i].rotation;

                                    Vector3 epTmp4 = new Vector3(epTmp2.x + infomationOffset.x, epTmp2.y + infomationOffset.y, epTmp2.z + infomationOffset.z);
                                    entryPoints[i].localPosition = epTmp4;
                                    epTmp4 = entryPoints[i].position;
                                    infomationOpen.transform.position = new Vector3(epTmp4.x, infomationOpen.transform.position.y, epTmp4.z);
                                    entryPoints[i].localPosition = epTmp2;
                                    epTmp3.eulerAngles = new Vector3(epTmp3.eulerAngles.x + 90.0f, epTmp3.eulerAngles.y - 180.0f, epTmp3.eulerAngles.z);
                                    infomationOpen.transform.rotation = epTmp3;
                                    if (!infomationOpen.activeSelf) infomationOpen.SetActive(true);
                                }
                            }
                            else
                            {
                                if (infomationOpen != null)
                                {
                                    if (infomationOpen.activeSelf) infomationOpen.SetActive(false);
                                    infomationOpen.transform.position = new Vector3(0.0f, infomationOpen.transform.position.y, 0.0f);
                                }
                            }
                        }
                    }
                    
                    if (topicDistance <= showDoorplateDeterminationDistance)
                    {
                        if (canvas_tmp != null && !canvas_tmp.activeSelf) canvas_tmp.SetActive(true);
                    }
                    else
                    {
                        if (canvas_tmp != null && canvas_tmp.activeSelf) canvas_tmp.SetActive(false);
                    }
                }
            }
            currentExeSection++;
            if (currentExeSection > frameRate) separateExeFinish = true; //端数があるかもしれないので1回多く回す

            //シームレスエリア内のワープ処理
            if (overlayUI != null)
            {
                if (isWarpWithinArea)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            if (overlayUI.isFadeOutFinishedBlackDisplay())
                            {
                                //ワープ
                                if (entryPoints.Length > warpWithinAreaIndex && warpWithinAreaIndex >= 0)
                                {
                                    Vector3 spawnPointPosition = entryPoints[warpWithinAreaIndex].localPosition;
                                    Quaternion spawnPointRotation = entryPoints[warpWithinAreaIndex].rotation;

                                    Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + spawnPointOffset.x, spawnPointPosition.y + spawnPointOffset.y, spawnPointPosition.z + spawnPointOffset.z);

                                    entryPoints[warpWithinAreaIndex].localPosition = spawnPointPosition2;

                                    spawnPointPosition2 = entryPoints[warpWithinAreaIndex].position;

                                    entryPoints[warpWithinAreaIndex].localPosition = spawnPointPosition;

                                    Networking.LocalPlayer.TeleportTo(spawnPointPosition2, Quaternion.Euler(spawnPointRotation.eulerAngles.x, spawnPointRotation.eulerAngles.y + 180.0f, spawnPointRotation.eulerAngles.z));

                                    //Networking.LocalPlayer.TeleportTo(entryPoints[warpWithinAreaIndex].position, entryPoints[warpWithinAreaIndex].rotation);
                                }
                                else if(warpWithinAreaIndex == -1)
                                {
                                    Vector3 spawnPointPosition = myEntryPoint.localPosition;
                                    Quaternion spawnPointRotation = myEntryPoint.rotation;

                                    Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + spawnPointOffset.x, spawnPointPosition.y + spawnPointOffset.y, spawnPointPosition.z + spawnPointOffset.z);

                                    myEntryPoint.localPosition = spawnPointPosition2;

                                    spawnPointPosition2 = myEntryPoint.position;

                                    myEntryPoint.localPosition = spawnPointPosition;

                                    Networking.LocalPlayer.TeleportTo(spawnPointPosition2, Quaternion.Euler(spawnPointRotation.eulerAngles.x, spawnPointRotation.eulerAngles.y + 180.0f, spawnPointRotation.eulerAngles.z));
                                    //Networking.LocalPlayer.TeleportTo(myEntryPoint.position, myEntryPoint.rotation);
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
            else
            {
                if (isWarpWithinArea)
                {
                    if (fadeoutCount > 0)
                    {
                        fadeoutCount -= Time.deltaTime;
                        if (fadeoutCount <= 0)
                        {
                            fadeoutCount = 0;
                            //ワープ
                            if (entryPoints.Length > warpWithinAreaIndex && warpWithinAreaIndex >= 0)
                            {
                                Vector3 spawnPointPosition = entryPoints[warpWithinAreaIndex].localPosition;
                                Quaternion spawnPointRotation = entryPoints[warpWithinAreaIndex].rotation;

                                Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + spawnPointOffset.x, spawnPointPosition.y + spawnPointOffset.y, spawnPointPosition.z + spawnPointOffset.z);

                                entryPoints[warpWithinAreaIndex].localPosition = spawnPointPosition2;

                                spawnPointPosition2 = entryPoints[warpWithinAreaIndex].position;

                                entryPoints[warpWithinAreaIndex].localPosition = spawnPointPosition;

                                Networking.LocalPlayer.TeleportTo(spawnPointPosition2, Quaternion.Euler(spawnPointRotation.eulerAngles.x, spawnPointRotation.eulerAngles.y + 180.0f, spawnPointRotation.eulerAngles.z));

                                //Networking.LocalPlayer.TeleportTo(entryPoints[warpWithinAreaIndex].position, entryPoints[warpWithinAreaIndex].rotation);
                            }
                            else if (warpWithinAreaIndex == -1)
                            {
                                Vector3 spawnPointPosition = myEntryPoint.localPosition;
                                Quaternion spawnPointRotation = myEntryPoint.rotation;

                                Vector3 spawnPointPosition2 = new Vector3(spawnPointPosition.x + spawnPointOffset.x, spawnPointPosition.y + spawnPointOffset.y, spawnPointPosition.z + spawnPointOffset.z);

                                myEntryPoint.localPosition = spawnPointPosition2;

                                spawnPointPosition2 = myEntryPoint.position;

                                myEntryPoint.localPosition = spawnPointPosition;

                                Networking.LocalPlayer.TeleportTo(spawnPointPosition2, Quaternion.Euler(spawnPointRotation.eulerAngles.x, spawnPointRotation.eulerAngles.y + 180.0f, spawnPointRotation.eulerAngles.z));
                                //Networking.LocalPlayer.TeleportTo(myEntryPoint.position, myEntryPoint.rotation);
                            }

                            //ロックを解除
                            Networking.LocalPlayer.Immobilize(false);
                            isWarpWithinArea = false;
                        }
                    }
                }
            }
            ShowWarpWithinAreaList();
        }

        public void UpdateDoorplate() //表札の表示を更新
        {
            int index = 0;
            foreach (Transform tmp in entryPoints)
            {
                if (tmp != null)
                {
                    Text tmp_text = tmp.parent.gameObject.GetComponent<Text>();
                    if (tmp_text != null)
                    {
                        //すべての表札を非表示にする
                        GameObject canvas_tmp = tmp_text.gameObject.transform.parent.gameObject;
                        if (canvas_tmp != null) canvas_tmp.SetActive(false);

                        //MultiAreaManagerから表札の記載内容が取得できなかった場合インデックスを表記する
                        string areaName_tmp = "";
                        if (_multiAreaManager != null)
                        {
                            areaName_tmp = _multiAreaManager.GetAreaName(index);
                        }
                        if(areaName_tmp == "")
                        {
                            tmp_text.fontSize = 300;
                            tmp_text.text = index.ToString();
                        }
                        else
                        {
                            int areaNameLength_tmp = areaName_tmp.Length;
                            int fontSize_tmp = 300;
                            if (areaNameLength_tmp <= 5)
                            {
                                fontSize_tmp = 300;
                            }else if(areaNameLength_tmp <= 6)
                            {
                                fontSize_tmp = 250;
                            }
                            else if (areaNameLength_tmp <= 7)
                            {
                                fontSize_tmp = 220;
                            }
                            else if (areaNameLength_tmp <= 20)
                            {
                                fontSize_tmp = 200;
                            }
                            else
                            {
                                fontSize_tmp = 100;
                            }
                            tmp_text.fontSize = fontSize_tmp; //TODO:文字数によって変える
                            tmp_text.text = areaName_tmp;
                        }

                        //文字が反転している場合は戻す
                        Transform tmp_transform;
                        if (tmp_text.gameObject.transform.lossyScale.x < 0)
                        {
                            tmp_transform = tmp.parent;
                            tmp.parent = null;
                            tmp_text.gameObject.transform.localScale = new Vector3(-tmp_text.gameObject.transform.localScale.x, tmp_text.gameObject.transform.localScale.y, tmp_text.gameObject.transform.localScale.z);
                            tmp.parent = tmp_transform;
                        }
                        if (tmp_text.gameObject.transform.lossyScale.z < 0)
                        {
                            //Debug.Log("reverse Z: index = " + index);
                            //エントリーポイントの反転を直す
                            Quaternion rot_tmp = tmp.localRotation;
                            //Debug.Log("reverse Z: rot_tmp = " + rot_tmp.eulerAngles);
                            rot_tmp.eulerAngles = new Vector3(rot_tmp.eulerAngles.x, 0.0f, rot_tmp.eulerAngles.z);
                            tmp.localRotation = rot_tmp;

                            tmp_transform = tmp.parent;
                            tmp.parent = null;
                            tmp_text.gameObject.transform.localScale = new Vector3(-tmp_text.gameObject.transform.localScale.x, tmp_text.gameObject.transform.localScale.y, tmp_text.gameObject.transform.localScale.z);
                            tmp.parent = tmp_transform;
                        }
                    }
                }
                index++;
            }
        }

        public void LoadQuickCommonTexture()
        {
            if (_externalResourceLoadManager != null)
            {
                SetMatrialTiling();
                //_externalResourceLoadManager.FillCommonTexture();
                _externalResourceLoadManager.isCommonTexture4096Mode = false;
                _externalResourceLoadManager.reservedQuickCommonUrlIndex = quickCommonUrlIndex;
                _externalResourceLoadManager.quickCommonLoadReserved = true;
            }
        }
        public void SetMatrialTiling()
        {
            if (_externalMaterialManager != null)
            {
                for (int i = 1; i <= materialTiling.Length; i++)
                {
                    _externalMaterialManager.ChangeTiling(i, materialTiling[i - 1]);
                }
            }
        }

        public void InputPassword_1()
        {
            if (inputValue == 0) inputValue = 1;
            else inputValue = inputValue * 10 + 1;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_2()
        {
            if (inputValue == 0) inputValue = 2;
            else inputValue = inputValue * 10 + 2;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_3()
        {
            if (inputValue == 0) inputValue = 3;
            else inputValue = inputValue * 10 + 3;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_4()
        {
            if (inputValue == 0) inputValue = 4;
            else inputValue = inputValue * 10 + 4;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_5()
        {
            if (inputValue == 0) inputValue = 5;
            else inputValue = inputValue * 10 + 5;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_6()
        {
            if (inputValue == 0) inputValue = 6;
            else inputValue = inputValue * 10 + 6;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_7()
        {
            if (inputValue == 0) inputValue = 7;
            else inputValue = inputValue * 10 + 7;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_8()
        {
            if (inputValue == 0) inputValue = 8;
            else inputValue = inputValue * 10 + 8;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_9()
        {
            if (inputValue == 0) inputValue = 9;
            else inputValue = inputValue * 10 + 9;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPassword_0()
        {
            if (inputValue == 0) return;
            else inputValue = inputValue * 10;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }
        public void InputPasswordClear()
        {
            inputValue = 0;
            if (inputValueFeedback != null) inputValueFeedback.text = inputValue.ToString();
        }

        public void CheckPassword()
        {
            if (_multiAreaManager != null && !_multiAreaManager.GetIsOpen(showIndomationWindowTopicEntryPointIndex))
            {
                bool result = _multiAreaManager.CheckPassword(showIndomationWindowTopicEntryPointIndex, inputValue);
                if (inputValueFeedback != null)
                {
                    if(result)
                    {
                        if (infomationWindow != null) infomationWindow.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                        inputValueFeedback.text = "";
                    }
                    else
                    {
                        inputValueFeedback.text = ngAnswerText;
                    }
                }
            }
            InputPasswordClear();
        }

        public void Warp(int index = -1, bool noTargetToCancel = true)
        {
            if (isWarpWithinArea) return;
            //Debug.Log("Warp Start");
            if(noTargetToCancel)
            {
                if (index >= entryPoints.Length || index < -1) return;
            }
            if (index == 0 && isIgnoreIndex0Spawn) return;
            isWarpWithinArea = true;
            warpWithinAreaIndex = index;
            //フェードアウト
            if (overlayUI != null)
            {
                overlayUI.BlackFadeOut();
            }
            fadeoutCount = fadeoutCountMax;
            Networking.LocalPlayer.Immobilize(true);
            //Debug.Log("Warp End");
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
            warpWithinAreaIndex = InputWarpWithinAreaIndexValue;

            if (WarpWithinAreaNameFeedback != null && _multiAreaManager != null)
            {
                if (warpWithinAreaIndex < entryPoints.Length || warpWithinAreaIndex >= 0)
                {
                    WarpWithinAreaNameFeedback.text = _multiAreaManager.GetAreaName(warpWithinAreaIndex);
                }
            }
        }

        public void WarpMyEntryPointInputWarpWithinAreaIndex()
        {
            SetMyEntryPointInputWarpWithinAreaIndex();
            Warp();
        }

        public void SetWarpWithinAreaNameFeedback()
        {
            warpWithinAreaIndex = InputWarpWithinAreaIndexValue;

            //ワープボードに反映
            if (WarpWithinAreaNameFeedback != null && _multiAreaManager != null)
            {
                if (warpWithinAreaIndex < entryPoints.Length || warpWithinAreaIndex >= 0)
                {
                    WarpWithinAreaNameFeedback.text = _multiAreaManager.GetAreaName(warpWithinAreaIndex);
                }
            }
            InputWarpWithinAreaIndexClear();
        }

        public void SetMyEntryPointInputWarpWithinAreaIndex()
        {
            warpWithinAreaIndex = -1;
            //ワープボードに反映
            if (WarpWithinAreaNameFeedback != null)
            {
                WarpWithinAreaNameFeedback.text = myEntryPointName;
            }
        }

        public void ShowWarpWithinAreaList()
        {
            if (WarpWithinAreaList == null || _multiAreaManager == null) return;
            if (!WarpWithinAreaList.gameObject.activeInHierarchy)
            {
                isWarpWithinAreaListUpdated = false;
                return;
            }
            if (isWarpWithinAreaListUpdated) return;
            isWarpWithinAreaListUpdated = true;
            WarpWithinAreaList.text = "";
            int length = _multiAreaManager._multiAreaSyncer.Length;
            for (int i = 0; i < length; i++)
            {
                if(_multiAreaManager.GetIsOpen(i))
                {
                    WarpWithinAreaList.text += "" + i + ":" + _multiAreaManager.GetAreaName(i) + "[OPEN]\n";
                }
                else if (_multiAreaManager.GetAreaState(i) != "")
                {
                    WarpWithinAreaList.text += "" + i + ":" + _multiAreaManager.GetAreaName(i) + "\n";
                }
            }
        }

        //SendCustomEvent呼び出し用
        public void Warp() {
            //ワープSEを再生
            if (sEAudioSource != null && warpWithinAreaSE != null)
            {
                sEAudioSource.PlayOneShot(warpWithinAreaSE);
            }

            Warp(warpWithinAreaIndex); 
        }
        public void Warp_EventArea0() { if(eventAreaInfomationIndex.Length >= 1) Warp(eventAreaInfomationIndex[0]); }
        public void Warp_EventArea1() { if (eventAreaInfomationIndex.Length >= 2) Warp(eventAreaInfomationIndex[1]); }
        public void Warp_EventArea2() { if (eventAreaInfomationIndex.Length >= 3) Warp(eventAreaInfomationIndex[2]); }
        public void Warp_EventArea3() { if (eventAreaInfomationIndex.Length >= 4) Warp(eventAreaInfomationIndex[3]); }
        public void Warp_EventArea4() { if (eventAreaInfomationIndex.Length >= 5) Warp(eventAreaInfomationIndex[4]); }
        public void Warp_EventArea5() { if (eventAreaInfomationIndex.Length >= 6) Warp(eventAreaInfomationIndex[5]); }
        public void Warp_EventArea6() { if (eventAreaInfomationIndex.Length >= 7) Warp(eventAreaInfomationIndex[6]); }
        public void Warp_EventArea7() { if (eventAreaInfomationIndex.Length >= 8) Warp(eventAreaInfomationIndex[7]); }
        public void Warp_EventArea8() { if (eventAreaInfomationIndex.Length >= 9) Warp(eventAreaInfomationIndex[8]); }
        public void Warp_EventArea9() { if (eventAreaInfomationIndex.Length >= 10) Warp(eventAreaInfomationIndex[9]); }
    }
}
