
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DuplicateRenderTexture : UdonSharpBehaviour
    {
        [Header("RenderTextureを投影したメッシュの撮影に使うカメラ")] public Camera cam;
        [Header("RenderTextureを投影するメッシュのマテリアル(UnlitShader推奨)")] public Material sourceMeshMaterial;
        private Texture2D _targetTexture;
        private bool isStarted = false; //他スクリプトから指示があるまでは読み込みを開始しない(LoadExternalDataのisStartedとはフラグの立て方が逆であることに注意)
        public bool isFinish = true;
        public int chipSize = 1024;
        public int sectionX = 0;
        public int sectionY = 0;

        void OnPostRender()
        {
            if (!isStarted) return;
            if (!isFinish)
            {
                if (_targetTexture != null) _targetTexture.ReadPixels(cam.pixelRect, sectionX*chipSize, sectionY * chipSize, false);
                if (_targetTexture != null) _targetTexture.Apply(false);
            }
            isStarted = false;
            isFinish = true;
            this.gameObject.SetActive(false);
        }

        public void Duplicate(RenderTexture sourceTexture, Texture2D targetTexture, int _chipSize=1024, int _sectionX=0, int _sectionY=0)
        {
            if (!isFinish) return;
            sourceMeshMaterial.mainTexture = sourceTexture;
            _targetTexture = targetTexture;
            chipSize = _chipSize;
            sectionX = _sectionX;
            sectionY = _sectionY;
            isStarted = true;
            isFinish = false;
            this.gameObject.SetActive(true);
        }
    }
}
