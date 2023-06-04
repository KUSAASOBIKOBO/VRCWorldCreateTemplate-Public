
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Components;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LoadExternalDataFromStringDownloader : UdonSharpBehaviour
    {
        public int urlIndex;
        public string data;
        UdonBehaviour script;
        public VRCUrlList _urlList;
        public LoadExternalData[] _loadExternalData;
        public bool isFinish = true;

        void Start()
        {
            //GetData(); //Test
        }

        public void GetData()
        {
            if (_urlList == null || _urlList.elementList.Length <= urlIndex) return;
            script = (UdonBehaviour)this.gameObject.GetComponent(typeof(UdonBehaviour));
            VRC.SDK3.StringLoading.VRCStringDownloader.LoadUrl(_urlList.elementList[urlIndex], script);
        }

        public void SetToLoadExternalData()
        {
            string[] tmpStrings = data.Split('\n');
            int tmpStringsCounter = 0;
            foreach(LoadExternalData tmp in _loadExternalData)
            {
                if (tmpStringsCounter >= tmpStrings.Length) break;
                for (int i = 0;i < tmp.data.Length;i++)
                {
                    //ここで受け取ったデータを分割して格納する。改行で次の要素、改行2回で次の配列に移動
                    tmp.data[i] = tmpStrings[tmpStringsCounter];
                    tmpStringsCounter++;
                    if (tmpStringsCounter >= tmpStrings.Length) break;
                    if (tmpStrings[tmpStringsCounter] == "")
                    {
                        tmpStringsCounter++;
                        break;
                    }
                }
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            data = result.Result;
            SetToLoadExternalData();
            isFinish = true;
        }

        public void Load(int index)
        {
            if(index == 0)
            {
                isFinish = true;
                return;
            }
            else
            {
                urlIndex = index;
            }
            isFinish = false;
            GetData();

        }
    }
}