
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    public enum OverlayAnimationStatus
    {
        ANIM_IDLE,
        INANIM_STARTED,
        INANIM_FINISHED,
        OUTANIM_STARTED,
        OUTANIM_FINISHED
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OverlayUIManager : UdonSharpBehaviour
    {
        [Header("トラッキングモード")] public int trackingMode= 2;

        [Header("アニメーター")] public Animator anim;

        [Header("インフォメーション表示時間")] public float infomationDisplayInterval = 5.0f;
        [Header("タイトル表示時間")] public float titleDisplayInterval = 3.0f;
        public Text infomationText;
        public Text titleText;
        public Text titleShadowText;
        public GameObject image;
        public ImageDownloadSafe _imageDownloadSafe;
        public Texture2D fallbackTexture;


        private float infomationDisplayIntervalCounter = 0.0f;
        private float titleDisplayIntervalCounter = 0.0f;
        private OverlayAnimationStatus infomationDisplayStatus = OverlayAnimationStatus.ANIM_IDLE;
        private OverlayAnimationStatus titleDisplayStatus = OverlayAnimationStatus.ANIM_IDLE;
        private OverlayAnimationStatus blackDisplayStatus = OverlayAnimationStatus.ANIM_IDLE;

        public GameObject ScreenObject;

        public void Tracking()
        {
            /*ヘッドに追従する処理*/
            VRCPlayerApi topicPlayer_tmp = Networking.LocalPlayer;
            if (topicPlayer_tmp == null) return;
            Vector3 playerPos_tmp = topicPlayer_tmp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Quaternion playerRot_tmp = topicPlayer_tmp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            if(trackingMode == 1)
            {
                playerRot_tmp.eulerAngles = new Vector3(playerRot_tmp.eulerAngles.x, playerRot_tmp.eulerAngles.y, 0.0f);
            }
            else if(trackingMode == 2)
            {
                playerRot_tmp.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
            }

            this.gameObject.transform.rotation = playerRot_tmp;
            this.gameObject.transform.position = playerPos_tmp;
        }

        public void SetTrackingOffSet()
        {
            VRCPlayerApi topicPlayer_tmp = Networking.LocalPlayer;
            if (topicPlayer_tmp == null) return;
            Vector3 playerPos_tmp = topicPlayer_tmp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Quaternion playerRot_tmp = topicPlayer_tmp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            if (trackingMode == 1 || trackingMode == 2)
            {
                playerRot_tmp.eulerAngles = new Vector3(playerRot_tmp.eulerAngles.x, playerRot_tmp.eulerAngles.y, 0.0f);
            }
            this.gameObject.transform.rotation = playerRot_tmp;
            this.gameObject.transform.position = playerPos_tmp;
        }

        void Update()
        {
            /*使用していない時は非アクティブにする*/
            if(blackDisplayStatus == OverlayAnimationStatus.INANIM_FINISHED && titleDisplayStatus == OverlayAnimationStatus.OUTANIM_FINISHED && infomationDisplayStatus == OverlayAnimationStatus.OUTANIM_FINISHED)
            {
                if(ScreenObject != null && ScreenObject.activeSelf) ScreenObject.SetActive(false);
            }
            else
            {
                if (ScreenObject != null && !ScreenObject.activeSelf) ScreenObject.SetActive(true);
            }

            Tracking();

            if (anim == null) return;

            /*タイトルの自動フェードアウト処理*/
            if (titleDisplayStatus == OverlayAnimationStatus.INANIM_FINISHED && titleDisplayIntervalCounter > 0)
            {
                titleDisplayIntervalCounter -= Time.deltaTime;
                if(titleDisplayIntervalCounter <= 0)
                {
                    TitleFadeOut();
                }
            }
            /*インフォメーションの自動スライドアウト処理*/
            if (infomationDisplayStatus == OverlayAnimationStatus.INANIM_FINISHED && infomationDisplayIntervalCounter > 0)
            {
                infomationDisplayIntervalCounter -= Time.deltaTime;
                if (infomationDisplayIntervalCounter <= 0)
                {
                    infomationDisplayIntervalCounter = 0;
                    anim.SetBool("infomation", false);
                }
            }
        }

        public void BlackFadeIn()
        {
            if (trackingMode == 2) SetTrackingOffSet();
            this.gameObject.SetActive(true);
            anim.SetBool("black", true);
        }

        public void BlackFadeOut()
        {
            this.gameObject.SetActive(true);
            anim.SetBool("black", false);
        }

        public void SetTitle(string displayText, bool isForceStatusChange = false)
        {
            if (trackingMode == 2) SetTrackingOffSet();
            this.gameObject.SetActive(true);
            titleText.text = displayText;
            titleShadowText.text = displayText;
            if(image != null) image.SetActive(false);
            anim.SetBool("title", true);
            titleDisplayIntervalCounter = titleDisplayInterval;
            if (isForceStatusChange) TitleFadeInStart();
        }

        public void SetTitle(string displayText, float _interval, bool isForceStatusChange = false)
        {
            SetTitle(displayText, isForceStatusChange);
            titleDisplayIntervalCounter = _interval;
        }

        public void TitleFadeOut()
        {
            titleDisplayIntervalCounter = 0;
            anim.SetBool("title", false);
        }

        public void SetImage(int urlIndex, bool isForceStatusChange = false, bool isWithOutImageDownload = false)
        {
            if (_imageDownloadSafe == null || image == null) return;
            if (trackingMode == 2) SetTrackingOffSet();
            this.gameObject.SetActive(true);
            titleText.text = "";
            titleShadowText.text = "";
            image.SetActive(true);
            if (!isWithOutImageDownload)
            {
                if (fallbackTexture != null && _imageDownloadSafe.rawImage != null) _imageDownloadSafe.rawImage.texture = fallbackTexture;
                _imageDownloadSafe.LoadFromUrlIndex(urlIndex);
            }
            anim.SetBool("title", true);
            titleDisplayIntervalCounter = titleDisplayInterval;
            if (isForceStatusChange) TitleFadeInStart();
        }

        public void SetImage(int urlIndex, float _interval, bool isForceStatusChange = false, bool isWithOutImageDownload = false)
        {
            SetImage(urlIndex, isForceStatusChange, isWithOutImageDownload);
            titleDisplayIntervalCounter = _interval;
        }

        public void ImageFadeOut()
        {
            TitleFadeOut();
        }

        public void SetInfomation(string displayText)
        {
            if (trackingMode == 2) SetTrackingOffSet();
            this.gameObject.SetActive(true);
            infomationText.text = displayText;
            anim.SetBool("infomation", true);
            infomationDisplayIntervalCounter = infomationDisplayInterval;
        }


        public void BlackFadeInStart()
        {
            blackDisplayStatus = OverlayAnimationStatus.INANIM_STARTED;
            //Debug.Log("CallAnimationmethod");
        }
        public void BlackFadeInFinish()
        {
            blackDisplayStatus = OverlayAnimationStatus.INANIM_FINISHED;
            //Debug.Log("CallAnimationmethod");
        }
        public void BlackFadeOutStart()
        {
            blackDisplayStatus = OverlayAnimationStatus.OUTANIM_STARTED;
            //Debug.Log("CallAnimationmethod");
        }
        public void BlackFadeOutFinish()
        {
            blackDisplayStatus = OverlayAnimationStatus.OUTANIM_FINISHED;
            //Debug.Log("CallAnimationmethod");
        }

        public void TitleFadeInStart()
        {
            titleDisplayStatus = OverlayAnimationStatus.INANIM_STARTED;
            //Debug.Log("CallAnimationmethod:TitleFadeInStart");
        }
        public void TitleFadeInFinish()
        {
            titleDisplayStatus = OverlayAnimationStatus.INANIM_FINISHED;
            //Debug.Log("CallAnimationmethod:TitleFadeInFinish");
        }
        public void TitleFadeOutStart()
        {
            titleDisplayStatus = OverlayAnimationStatus.OUTANIM_STARTED;
            //Debug.Log("CallAnimationmethod:TitleFadeOutStart");
        }
        public void TitleFadeOutFinish()
        {
            titleDisplayStatus = OverlayAnimationStatus.OUTANIM_FINISHED;
            //Debug.Log("CallAnimationmethod:TitleFadeOutFinish");
        }

        public void InfomationSlideInStart()
        {
            infomationDisplayStatus = OverlayAnimationStatus.INANIM_STARTED;
            //Debug.Log("CallAnimationmethod");
        }
        public void InfomationSlideInFinish()
        {
            infomationDisplayStatus = OverlayAnimationStatus.INANIM_FINISHED;
            //Debug.Log("CallAnimationmethod");
            infomationDisplayIntervalCounter = infomationDisplayInterval;
        }
        public void InfomationSlideOutStart()
        {
            infomationDisplayStatus = OverlayAnimationStatus.OUTANIM_FINISHED;
            //Debug.Log("CallAnimationmethod");
        }
        public void InfomationSlideOutFinish()
        {
            infomationDisplayStatus = OverlayAnimationStatus.OUTANIM_FINISHED;
            //Debug.Log("CallAnimationmethod");
        }

        public bool isFadeInFinishedBlackDisplay()
        {
            if(blackDisplayStatus == OverlayAnimationStatus.INANIM_FINISHED) return true;
            return false;
        }

        public bool isFadeOutFinishedBlackDisplay()
        {
            if (blackDisplayStatus == OverlayAnimationStatus.OUTANIM_FINISHED) return true;
            return false;
        }

        public bool isFadeInFinishedTitleDisplay()
        {
            if (titleDisplayStatus == OverlayAnimationStatus.INANIM_FINISHED) return true;
            return false;
        }

        public bool isFadeOutFinishedTitleDisplay()
        {
            if (titleDisplayStatus == OverlayAnimationStatus.OUTANIM_FINISHED) return true;
            return false;
        }
    }
}
