
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FillTheTextureWithColor : UdonSharpBehaviour
    {
        [Header("ダミーカメラ")] public Camera cam;
        public Texture2D[] targetTexture;
        public Color[] fillColor;
        private bool isStarted = false; //他スクリプトから指示があるまでは読み込みを開始しない(LoadExternalDataのisStartedとはフラグの立て方が逆であることに注意)
        public bool isFinish = true;

        void OnPostRender()
        {
            if (!isStarted) return;
            if (!isFinish)
            {
                if(targetTexture.Length != fillColor.Length) return;
                int dataNum = targetTexture.Length;
                for(int i=0;i<dataNum;i++)
                {
                    if (targetTexture[i] != null)
                    {
                        targetTexture[i].ReadPixels(cam.pixelRect, 0, 0, false);
                        /*int width = targetTexture[i].width;
                        int height = targetTexture[i].height;
                        for(int y=0;y<height;y++)
                        {
                            for(int x=0;x<width;x++)
                            {
                                targetTexture[i].SetPixel(x, y, fillColor[i]);
                                Debug.Log("targetTexture[i].SetPixel(x = "+x+", y = "+y+", fillColor[i]);");
                            }
                        }*/
                        targetTexture[i].Apply(false);
                    } 
                }
            }
            isStarted = false;
            isFinish = true;
            this.gameObject.SetActive(false);
        }
        public void Fill()
        {
            if (!isFinish) return;
            cam.backgroundColor = fillColor[0];
            isStarted = true;
            isFinish = false;
            this.gameObject.SetActive(true);

            
        }
    }
}
