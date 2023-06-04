
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    public enum ExDataLoadMode
    {
        VERTICAL,
        HORIZONTAL
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LoadExternalData : UdonSharpBehaviour
    {
        public Camera cam;
        public Texture2D targetTexture;
        private bool isStarted = true; //他スクリプトから指示があるまでは読み込みを開始しない
        public bool isFinish = true;

        [Header("ロードモード")] public ExDataLoadMode mode = ExDataLoadMode.HORIZONTAL;
        [Header("全て処理するのにかかる時間の倍率")] public float exeTimeRate = 1;
        [Header("最大入力文字数(Default:41)")] public int numberOfLetterLimit = 41;
        [Header("Android端末用ビット補正タイミング1(Default:10)")] public int androidFixBit1 = 10;
        [Header("Android端末用ビット補正タイミング2(Default:33)")] public int androidFixBit2 = 33;
        [Header("PC用スキップ文字位置1(Default:9)")] public int skipCharacterPos1 = 9;
        [Header("PC用スキップ文字位置2(Default:30)")] public int skipCharacterPos2 = 30;
        [Header("PC用スキップ文字位置3(Default:30)")] public int skipCharacterPos3 = 30;

        private int frameRate = 60;

        private int currentExeSection = 0;

        private int sectionSize = 1;

        [Header("受信データ(1078件(index0~1077)まで)")] public string[] data;
        [Header("受信したデータ数")] public int dataNum = 0;

        [Header("デバッグテキスト出力用UIText")] public Text DebugText;

        void Update()
        {
            if(isStarted && !isFinish)
            {
                //if (DebugText != null) DebugText.text += "GetPix() Started:"+(currentExeSection * sectionSize) +"-"+ ((currentExeSection + 1) * sectionSize) + "\n";
                GetPix(currentExeSection * sectionSize, ((currentExeSection + 1) * sectionSize));
                //if (DebugText != null) DebugText.text += "GetPix() Finished\n";
                currentExeSection++;
                if (currentExeSection > frameRate)
                {
                    FinishLoad();
                }
            }
        }

        void GetPix(int startIndex,int endIndex)
        {
            int xMax = (int)cam.pixelRect.xMax;
            int yMax = (int)cam.pixelRect.yMax;
            for (int i= startIndex; i < endIndex; i++)
            {
                if (i >= data.Length) break;
                char[] tmp_char = new char[xMax / 24];
                int countA = 0;
                string tmp_string = "";
                int countB = 0;

                int j_offset = 0;
                for (int k = 0; k < 24; k++)
                {
                    Color tmp_color2;
                    if(mode == ExDataLoadMode.HORIZONTAL) tmp_color2 = targetTexture.GetPixel(j_offset, i);
                    else tmp_color2 = targetTexture.GetPixel(i, j_offset);
                    j_offset++;
                    if (tmp_color2.r < 0.3f)
                    {
                        break;
                    }
                }
                for (int k = 0; k < 24; k++)
                {
                    Color tmp_color2;
                    if(mode == ExDataLoadMode.HORIZONTAL) tmp_color2 = targetTexture.GetPixel(j_offset, i);
                    else tmp_color2 = targetTexture.GetPixel(i, j_offset);
                    j_offset++;
                    if (tmp_color2.r < 0.3f)
                    {
                        break;
                    }
                }

                for (int j = j_offset; j <= xMax; j++)
                {
                    Color tmp_color;
                    if(mode == ExDataLoadMode.HORIZONTAL) tmp_color = targetTexture.GetPixel(j, i);
                    else tmp_color = targetTexture.GetPixel(i, j);
                    if (tmp_color.r < 0.3f)
                    {
                        tmp_string += "1";
                    }
                    else
                    {
                        tmp_string += "0";
                    }
                    if (countB >= 23)
                    {
                        int tmp_int = Convert.ToInt32(tmp_string,2);
                        if (tmp_int == 10 || tmp_int == 0)
                        {
                            break;
                        }
                        else
                        {
                            tmp_char[countA] = (char)tmp_int;
                            countB = 0;
                            tmp_string = "";
                            countA++;
                            if (countA >= numberOfLetterLimit) break; //読み込み文字数制限(indexは0からなので制限数になったら即break)
#if !UNITY_EDITOR && UNITY_ANDROID
                            //Android端末(Quest2)の読み込み時にbit誤差が出るためbit補正する
                            if(mode == ExDataLoadMode.HORIZONTAL && countA == androidFixBit1) j++;
                            if(mode == ExDataLoadMode.HORIZONTAL && countA == androidFixBit2) j++;
#endif
#if UNITY_EDITOR || !UNITY_ANDROID
                            if(mode == ExDataLoadMode.HORIZONTAL && countA == skipCharacterPos1) j+=24;
                            if(mode == ExDataLoadMode.HORIZONTAL && countA == skipCharacterPos2) j+=24;
                            if(mode == ExDataLoadMode.HORIZONTAL && countA == skipCharacterPos3) j+=24;
#endif
                        }
                    }
                    else
                    {
                        countB++;
                    }
                }
#if UNITY_EDITOR || !UNITY_ANDROID
                if (i > 0) //Android端末(Quest2)の読み込み時にbit誤差が出るため1要素目は空にしている。そのためi==0は無視する
                {
                    data[i - 1] = new string(tmp_char);
                    data[i - 1] = data[i - 1].Split('\0')[0];
                    if (data[i - 1] == "")
                    {
                        FinishLoad();
                        break;
                    }
                    Debug.Log("GetData:" + data[i - 1]);
                }
#endif
#if !UNITY_EDITOR && UNITY_ANDROID
                    data[i] = new string(tmp_char);
                    data[i] = data[i].Split('\0')[0];
                    if (data[i] == "")
                    {
                        FinishLoad();
                        break;
                    }
                    char tmpChar = (char)16;
                    string tmpStr = tmpChar.ToString();
                    data[i] = data[i].Replace(tmpStr, "");
                    Debug.Log("GetData:" + data[i]);
#endif
            }


            // 画素値を取得する
            /*var color = targetTexture.GetPixel(capturePointX, capturePointY);
            int intValue = Convert.ToInt32(String.Format("{0:D8}", Convert.ToInt32(Convert.ToString((int)(color.r * 255.0f), 2))) + String.Format("{0:D8}", Convert.ToInt32(Convert.ToString((int)(color.g * 255.0f), 2))) + String.Format("{0:D8}", Convert.ToInt32(Convert.ToString((int)(color.b * 255.0f), 2))), 2);
            if (intValue >= 65535) return;
            char charValue = (char)intValue;

            Debug.Log("int" + intValue);
            Debug.Log("char" + charValue);
            Debug.Log("char:" + charValue.ToString() + " r:" +(color.r*255.0f).ToString() + " g:" + (color.g * 255.0f).ToString() + " b:" + (color.b * 255.0f).ToString());
            */
        }

        void OnPostRender()
        {
            //if (DebugText != null) DebugText.text += "OnPostRender()\n";
            if (!isStarted)
            {
                targetTexture.ReadPixels(cam.pixelRect, 0, 0, false);
                //if (DebugText != null) DebugText.text += "ReadPixels() Finished\n";
                targetTexture.Apply(false);
                //if (DebugText != null) DebugText.text += "Apply() Finished\n";
            }
            //if (DebugText != null) DebugText.text += "targetTexture.ReadPixels()\n";
            isStarted = true;
            //this.gameObject.SetActive(false);
        }

        public void StartLoad()
        {
            /*char encordCheckChar = 'あ';
            int encordCheckInt = (int)encordCheckChar;
            string tmp_string = Convert.ToString(encordCheckInt, 2);
            int tmp_int = Convert.ToInt32(tmp_string, 2);
            char tmp_char = (char)tmp_int;

            if (DebugText != null) DebugText.text += "Default charValue is " + encordCheckChar + "\n";
            if (DebugText != null) DebugText.text += "Default IntValue is " + encordCheckInt + "\n";
            if (DebugText != null) DebugText.text += "Default ConvertValue is " + tmp_string + "\n";
            if (DebugText != null) DebugText.text += "Default ConvertIntValue is " + tmp_int + "\n";
            if (DebugText != null) DebugText.text += "Default ConvertCharValue is " + tmp_char + "\n";
            return;*/
            //if (DebugText != null) DebugText.text += "StartLoad()\n";
            isStarted = false;
            isFinish = false;
            frameRate = (int)((1f / Time.deltaTime) * exeTimeRate);
            currentExeSection = 0;
            sectionSize = data.Length / frameRate;
            if ((float)data.Length % (float)frameRate != 0f) sectionSize++;
            if (sectionSize < 1) sectionSize = 1;
            this.gameObject.SetActive(true);
        }

        public void FinishLoad()
        {
            isFinish = true;
            dataNum = 0;
            if (DebugText != null) DebugText.text += "\n<Loaded Data>\n";
            foreach (string tmp in data)
            {
                if (tmp != "")
                {
                    dataNum++;
                    if (DebugText != null) DebugText.text += "["+tmp+ "],";
                }
            }
            if (DebugText != null) DebugText.text += "\ndataNum:" + dataNum + "\n";
            this.gameObject.SetActive(false);
        }

        public void ClearData()
        {
            for (int i = 0;i < data.Length; i++)
            {
                data[i] = "";
            }
        }
    }
}
