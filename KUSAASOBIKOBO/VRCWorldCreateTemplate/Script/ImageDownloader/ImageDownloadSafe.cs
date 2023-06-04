
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using System;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Image;
using VRC.SDK3.Components;


namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ImageDownloadSafe : UdonSharpBehaviour
    {
        public Material _material;
        public Material[] _subMaterial;
        public VRCUrl _url;
        private VRCImageDownloader _imageDownloader;
        public TextureInfo _textureInfo;
        private bool instantiated = false;
        private bool loading = false;
        private bool isSyncer = false;
        public VRCUrlInputField inputFIeld;
        public Text infomationText;
        public string errorText = "読み込みに失敗しました";
        public string successText = "読み込みが完了しました";
        public string loadingText = "ロード中です";
        private float wateAutoReloadTIme = 15.0f;
        private float wateAutoReloadTimeCount = -1;
        private string lastLoadUrl;
        public RawImage rawImage;
        private IVRCImageDownload result;
        public float rawImageMaxWidth = 2048.0f;

        /*クエストで画像が反転するバグがあったときの処理の名残*/
        //public bool isQuestReversalRawImage = false; //一つの対象に対して1回だけ行えばよい処理です。例えば1つのRawImageに対して複数のImageDownloadSafeから画像を書き換える場合このフラグはそのうちの1つに入っていれば良いのです
        //public bool isQuestReversalMaterial = true;

        public VRCUrlList urlList;
        public int urlIndex;

        public bool isEnableToLoad = false;
        public bool isEnableToSetFromUrlIndex = false;

        void Start()
        {
            QuestReversal();
        }

        void Update()
        {
            if(wateAutoReloadTimeCount > 0)
            {
                wateAutoReloadTimeCount -= Time.deltaTime;
                if(wateAutoReloadTimeCount <= 0)
                {
                    if(_url.ToString() != lastLoadUrl)
                    {
                        isSyncer = true;
                        ReLoad();
                    }
                }
            }
        }

        void OnEnable()
        {
            SyncRequestOwner();
            if(isEnableToLoad) wateAutoReloadTimeCount = wateAutoReloadTIme;
            if (isEnableToSetFromUrlIndex) LoadFromUrlIndex();
        }

        void OnDestroy()
        {
            if(instantiated) _imageDownloader.Dispose();
        }

        public override void OnImageLoadSuccess(IVRCImageDownload _result)
        {
            Debug.Log("ImageDownloadSafe.OnImageLoadSuccess");
            loading = false;
            lastLoadUrl = _url.ToString();
            result = _result;
            foreach(Material tmp in _subMaterial)
            {
                if (tmp != null) tmp.SetTexture("_MainTex", _material.GetTexture("_MainTex"));
            }
            
            SetRawImageTexture();
            if(infomationText != null)
            {
                infomationText.text = successText;
            }
        }

        public override void OnImageLoadError(IVRCImageDownload _result)
        {
            Debug.Log("ImageDownloadSafe.OnImageLoadError");
            loading = false;
            if (infomationText != null)
            {
                infomationText.text = errorText;
            }
        }

        void QuestReversal()
        {
            return;//不具合が解消し不要な処理になりました。
            /*
#if !UNITY_EDITOR && UNITY_ANDROID
            if (isQuestReversalMaterial)
            {
                Vector2 tmp = _material.GetTextureScale("_MainTex");
                tmp.y = tmp.y * -1;
                _material.SetTextureScale("_MainTex", tmp);

                //if (_subMaterial != null)
                //{
                //    Vector2 tmp2 = _subMaterial.GetTextureScale("_MainTex");
                //    tmp2.y = tmp2.y * -1;
                //    _subMaterial.SetTextureScale("_MainTex", tmp2);
                //}
            }

            if(rawImage != null && isQuestReversalRawImage)
            {
                rawImage.rectTransform.localScale = new Vector3(rawImage.rectTransform.localScale.x, rawImage.rectTransform.localScale.y * -1, rawImage.rectTransform.localScale.z);
            }
#endif
    */
        }

        void SetRawImageTexture()
        {
            if(rawImage != null)
            {
                rawImage.texture = _material.GetTexture("_MainTex");
                float width_tmp = Convert.ToSingle(result.Result.width);
                float height_tmp = Convert.ToSingle(result.Result.height);
                float tmp = rawImageMaxWidth/ width_tmp;
                rawImage.rectTransform.sizeDelta = new Vector2(width_tmp*tmp, height_tmp*tmp);
            }
        }

        public void LoadFromInputField()
        {
            if(inputFIeld != null)
            {
                if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                _url = inputFIeld.GetUrl();
                SyncRequestOwner();
                isSyncer = true;
                ReLoad();
            }
        }

        public void LoadFromUrlIndex(int index)
        {
            //Debug.Log("LoadFromUrlIndex:START");
            if(urlList != null && urlList.elementList.Length > index)
            {
                //Debug.Log("LoadFromUrlIndex:SetURL");
                _url = urlList.elementList[index];
                isSyncer = true;
                ReLoad();
            }
        }

        public void LoadFromUrlIndex() { LoadFromUrlIndex(urlIndex); }

            void SyncRequestOwner()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Sync");
        }

        void Sync()
        {
            RequestSerialization();
        }

        void ReNew()
        {
            if(instantiated)
            {
                _imageDownloader.Dispose();
                instantiated = false;
                ReNew();
            }
            else
            {
                _imageDownloader = new VRCImageDownloader();
                instantiated = true;
            }
        }

        public void ReLoad()
        {
            if (loading)
            {
                if(infomationText != null)
                {
                    infomationText.text = loadingText;
                }
            }
            else
            {
                if (_url.ToString() == "" && lastLoadUrl == "") return;
                loading = true;
                ReNew();
                _imageDownloader.DownloadImage(_url, _material, this.GetComponent<UdonBehaviour>(), _textureInfo);
                if(isSyncer)
                {
                    isSyncer = false;
                }
                else
                {
                    if(_url.ToString() == lastLoadUrl)
                    {
                        wateAutoReloadTimeCount = wateAutoReloadTIme;
                    }
                }
            }
        }
    }
}
