
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections;
using System;


namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BakeExternalDataTexture : UdonSharpBehaviour
    {
        public Texture2D targetTexture;
        public string[] data;

        void Start()
        {
            SetPix();
        }
        void SetPix()
        {
            int x_index = 0;
            int y_index = 0;
            foreach (string tmp in data)
            {
                char[] charArray;
                charArray = tmp.ToCharArray();
                foreach(char tmpchar in charArray)
                {
                    int tmp_int = (int)tmpchar;
                    Debug.Log("tmpchar:" + tmpchar);
                    char[] charArray2;
                    string tmp_string = Convert.ToString(tmp_int, 2);
                    Debug.Log("tmpstring:"+ tmp_string);
                    if(tmp_string.Length < 24)
                    {
                        int addzeroNum = 24 - tmp_string.Length;
                        for(int i = 0; i < addzeroNum; i++)
                        {
                            tmp_string = "0" + tmp_string;
                        }
                    }
                    Debug.Log("tmpstringFix:" + tmp_string);
                    charArray2 = tmp_string.ToCharArray();
                    foreach (char tmpchar2 in charArray2)
                    {
                        if(tmpchar2=='1') targetTexture.SetPixel(x_index, y_index,Color.black);
                        else targetTexture.SetPixel(x_index, y_index, Color.white);
                        x_index++;
                    }
                }

                /*終端文字として改行を挿入*/
                char[] charArray3;
                string tmp_string2 = "1010";
                Debug.Log("tmpstring:" + tmp_string2);
                if (tmp_string2.Length < 24)
                {
                    int addzeroNum = 24 - tmp_string2.Length;
                    for (int i = 0; i < addzeroNum; i++)
                    {
                        tmp_string2 = "0" + tmp_string2;
                    }
                }
                Debug.Log("tmpstringFix:" + tmp_string2);
                charArray3 = tmp_string2.ToCharArray();
                foreach (char tmpchar2 in charArray3)
                {
                    if (tmpchar2 == '1') targetTexture.SetPixel(x_index, y_index, Color.black);
                    else targetTexture.SetPixel(x_index, y_index, Color.white);
                    x_index++;
                }
                /*ここまで*/

                x_index = 0;
                y_index++;
            }
            targetTexture.Apply();

        }
    }
}
