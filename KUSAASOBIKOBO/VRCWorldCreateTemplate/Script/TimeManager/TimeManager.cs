
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using System;
using UnityEngine.UI;
using UnityEngine.Rendering;


namespace KUSAASOBIKOBO
{
    public enum Weather
    {
        NONE,
        SUNNY_DAY,
        CLOUDY_DAY,
        RAINY_DAY,
        SNOWY_DAY
    }

    public enum TimeOfSky
    {
        NONE,
        SUNRISE,
        MORNING,
        DAYTIME,
        TWILIGHT,
        NIGHT
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TimeManager : UdonSharpBehaviour
    {
        public DateTime now;
        public DateTime localTime;
        public DateTime jstCache;
        private TimeZoneInfo jstZoneInfo = System.TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        public TimeOfSky nowTimeOfSky = TimeOfSky.NONE;
        public Weather nowWeather = Weather.NONE;
        private TimeOfSky reflectTimeOfSky = TimeOfSky.NONE;
        private Weather reflectWeather = Weather.NONE;
        public Material currentSkybox;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color currentColor;
        public bool isChangeSourceColor = true;

        public GameObject[] rainParticles;
        public GameObject[] snowParticles;

        public ExternalResourceLoadManager _externalResourceLoadManager;

        public bool isUseUrlIndexSunriseSkybox = false;
        public bool isUseUrlIndexMorningSkybox = false;
        public bool isUseUrlIndexDaytimeSkybox = false;
        public bool isUseUrlIndexTwilightSkybox = false;
        public bool isUseUrlIndexNightSkybox = false;
        public bool isUseUrlIndexCloudyDaytimeSkybox = false;
        public bool isUseUrlIndexCloudyNightSkybox = false;

        public bool isQuickLoad = false;
        public bool isStandardLoad = true;

        public int sunriseSkyboxUrlIndexQuickLoad;
        public int morningSkyboxUrlIndexQuickLoad;
        public int daytimeSkyboxUrlIndexQuickLoad;
        public int twilightSkyboxUrlIndexQuickLoad;
        public int nightSkyboxUrlIndexQuickLoad;
        public int cloudyDaytimeSkyboxUrlIndexQuickLoad;
        public int cloudyNightSkyboxUrlIndexQuickLoad;

        public int[] sunriseSkyboxUrlIndex = new int[6];
        public int[] morningSkyboxUrlIndex = new int[6];
        public int[] daytimeSkyboxUrlIndex = new int[6];
        public int[] twilightSkyboxUrlIndex = new int[6];
        public int[] nightSkyboxUrlIndex = new int[6];
        public int[] cloudyDaytimeSkyboxUrlIndex = new int[6];
        public int[] cloudyNightSkyboxUrlIndex = new int[6];

        public Material sunriseSkybox;
        public Material morningSkybox;
        public Material daytimeSkybox;
        public Material twilightSkybox;
        public Material nightSkybox;
        public Material cloudyDaytimeSkybox;
        public Material cloudyNightSkybox;

        [Header("ライトカラー"), ColorUsage(false, true)] public Color sunriseColor;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color morningColor;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color daytimeColor;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color twilightColor;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color nightColor;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color cloudyDaytimeColor;
        [Header("ライトカラー"), ColorUsage(false, true)] public Color cloudyNightColor;

        //TODO:時間別のlightingColorも設定できるようにする

        public float updateInterval = 1.0f;
        private float updateIntervalCount = 0.0f;
        public bool isGetEveryFrame = true;
        public JstAnimationSync[] _jstAnimationSync;
        public bool isAnimationSyncRegularly = true;
        public int animationSyncIntervalSecond = 60;
        private int animationSyncInterval_count = 0;
        public string dateTextFormat = "yyyy/MM/dd (ddd)";
        public string timeTextFormat = "HH:mm:ss";
        public string yearStringOffset = "20";
        public Text[] jstDateText;
        public Text[] jstTimeText;
        public Text[] localDateText;
        public Text[] localTimeText;
        public Text[] localTimeAlarmText;
        public GameObject[] jstClockLongHand;
        public GameObject[] jstClockShortHand;
        public GameObject[] localClockLongHand;
        public GameObject[] localClockShortHand;
        public GameObject[] timeLimitObject;
        public string[] timeLimit;
        public LoadExternalData externalData;
        public int localTimeAlarmHour = 9;
        public int localTimeAlarmMinute = 30;
        public bool isAlarm = false;
        public AudioSource AlarmAudioSource;//Alarmの音はAudioSourceに最初からセットしておく

        void Start()
        {
            /* 年間の天気情報を取得（デバッグ用）
            DateTime debug_dateTIme = GetJst();
            for (int i=0; i< 365; i++)
            {
                GetJPTownWeather(debug_dateTIme.AddDays(i));
            }
            */
        }

        void Update()
        {
            if(isGetEveryFrame) GetTime();

            if(updateIntervalCount <= 0)
            {
                if(!isGetEveryFrame) GetTime();
                updateIntervalCount = updateInterval;

                //処理
                DateTime jst_tmp = GetJst();
                if(jstDateText.Length >= 1)
                {
                    string jstDateString = jst_tmp.ToString(dateTextFormat);
                    foreach(Text tmp in jstDateText)
                    {
                        if(tmp != null && tmp.gameObject.activeInHierarchy) tmp.text = jstDateString;
                    }
                }

                if(jstTimeText.Length >= 1)
                {
                    string jstTimeString = jst_tmp.ToString(timeTextFormat);
                    foreach(Text tmp in jstTimeText)
                    {
                        if(tmp != null && tmp.gameObject.activeInHierarchy) tmp.text = jstTimeString;
                    }
                }

                if(localDateText.Length >= 1)
                {
                    string localDateString = localTime.ToString(dateTextFormat);
                    foreach(Text tmp in localDateText)
                    {
                        if(tmp != null && tmp.gameObject.activeInHierarchy) tmp.text = localDateString;
                    }
                }

                if(localTimeText.Length >= 1)
                {
                    string localTimeString = localTime.ToString(timeTextFormat);
                    foreach(Text tmp in localTimeText)
                    {
                        if(tmp != null && tmp.gameObject.activeInHierarchy) tmp.text = localTimeString;
                    }
                }

                if (jstClockShortHand.Length >= 1)
                {
                    foreach (GameObject tmp in jstClockShortHand)
                    {
                        if (tmp != null && tmp.activeInHierarchy)
                        {
                            Quaternion rot_tmp = tmp.transform.localRotation;
                            rot_tmp.eulerAngles = new Vector3(rot_tmp.eulerAngles.x, ((720.0f*(float)jst_tmp.Hour)/24.0f) + (((360.0f/12.0f) * (float)jst_tmp.Minute) / 60.0f), rot_tmp.eulerAngles.z);
                            tmp.transform.localRotation = rot_tmp;
                        }
                    }
                }

                if (jstClockLongHand.Length >= 1)
                {
                    foreach (GameObject tmp in jstClockLongHand)
                    {
                        if (tmp != null && tmp.activeInHierarchy)
                        {
                            Quaternion rot_tmp = tmp.transform.localRotation;
                            rot_tmp.eulerAngles = new Vector3(rot_tmp.eulerAngles.x, (360.0f * (float)jst_tmp.Minute) / 60.0f, rot_tmp.eulerAngles.z);
                            tmp.transform.localRotation = rot_tmp;
                        }
                    }
                }

                if (localClockShortHand.Length >= 1)
                {
                    foreach (GameObject tmp in localClockShortHand)
                    {
                        if (tmp != null && tmp.activeInHierarchy)
                        {
                            Quaternion rot_tmp = tmp.transform.localRotation;
                            rot_tmp.eulerAngles = new Vector3(rot_tmp.eulerAngles.x, ((720.0f * (float)localTime.Hour) / 24.0f) + (((360.0f / 12.0f) * (float)localTime.Minute) / 60.0f), rot_tmp.eulerAngles.z);
                            tmp.transform.localRotation = rot_tmp;
                        }
                    }
                }

                if (localClockLongHand.Length >= 1)
                {
                    foreach (GameObject tmp in localClockLongHand)
                    {
                        if (tmp != null && tmp.activeInHierarchy)
                        {
                            Quaternion rot_tmp = tmp.transform.localRotation;
                            rot_tmp.eulerAngles = new Vector3(rot_tmp.eulerAngles.x, (360.0f * (float)localTime.Minute) / 60.0f, rot_tmp.eulerAngles.z);
                            tmp.transform.localRotation = rot_tmp;
                        }
                    }
                }

                if (timeLimitObject.Length <= timeLimit.Length)
                {
                    //if(externalData.DebugText != null) externalData.DebugText.text += "\nStart:LimitObject";
                    for(int i=0; i < timeLimitObject.Length; i++)
                    {
                        if(timeLimitObject[i] != null)
                        {
                            string[] limitObjectStartAndEnd = timeLimit[i].Split(',');
                            //if(externalData.DebugText != null) externalData.DebugText.text += "\nSplitStep Finish";
                            int startIntValue_tmp = 0;
                            int endIntValue_tmp = 0;
                            int startIntValue2_tmp = 0;
                            int endIntValue2_tmp = 0;
                            int jstIntValue_tmp = 0;
                            int jstIntValue2_tmp = 0;
                            TimeSpan oneDay_tmp = TimeSpan.Parse("1");
                            TimeSpan zero_tmp = TimeSpan.Parse("00:00:00");
                            if(limitObjectStartAndEnd.Length >= 4 && limitObjectStartAndEnd[0] == "2" || limitObjectStartAndEnd[0] == "3")
                            {
                                startIntValue_tmp = Int32.Parse(limitObjectStartAndEnd[1]);
                                endIntValue_tmp = Int32.Parse(limitObjectStartAndEnd[3]); 
                            }else if(limitObjectStartAndEnd.Length >= 5 && limitObjectStartAndEnd[0] == "4")
                            {
                                startIntValue_tmp = Int32.Parse(limitObjectStartAndEnd[1]);
                                endIntValue_tmp = Int32.Parse(limitObjectStartAndEnd[4]);
                                startIntValue2_tmp = Int32.Parse(limitObjectStartAndEnd[2]);
                                endIntValue2_tmp = Int32.Parse(limitObjectStartAndEnd[5]);    
                            }

                            if(limitObjectStartAndEnd.Length >= 1 && limitObjectStartAndEnd[0] == "2") jstIntValue_tmp = (int)jst_tmp.DayOfWeek;
                            else if(limitObjectStartAndEnd.Length >= 1 && limitObjectStartAndEnd[0] == "3") jstIntValue_tmp = jst_tmp.Day;
                            else if(limitObjectStartAndEnd.Length >= 1 && limitObjectStartAndEnd[0] == "4"){
                                jstIntValue_tmp = jst_tmp.Month;
                                jstIntValue2_tmp = jst_tmp.Day;
                            }
                            //if(externalData.DebugText != null) externalData.DebugText.text += "\nGetValueStep Finish";
                            /*
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nlimitObjectStartAndEnd.Length:"+limitObjectStartAndEnd.Length;
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nlimitObjectStartAndEnd[0]:"+limitObjectStartAndEnd[0];
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nyearStringOffset + limitObjectStartAndEnd[1]:"+ yearStringOffset + limitObjectStartAndEnd[1] + "|";
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nyearStringOffset + limitObjectStartAndEnd[2]:"+ yearStringOffset + limitObjectStartAndEnd[2] + "|";
                            char[] chararray_tmp;
                            chararray_tmp = limitObjectStartAndEnd[1].ToCharArray(0, 17);
                            char[] chararray2_tmp;
                            chararray2_tmp = limitObjectStartAndEnd[2].ToCharArray(0, 17);

                            foreach(char tmp in chararray_tmp)
                            {
                                if(externalData.DebugText != null) externalData.DebugText.text += "\nchararray_tmp:"+ tmp + "(" + (int)tmp + ")";
                            }

                            foreach(char tmp in chararray2_tmp)
                            {
                                if(externalData.DebugText != null) externalData.DebugText.text += "\nchararray2_tmp:"+ tmp + "(" + (int)tmp + ")";
                            }

                            if(externalData.DebugText != null) externalData.DebugText.text += "\nyearStringOffset + limitObjectStartAndEnd[1].Length:"+ limitObjectStartAndEnd[1].Length;
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nyearStringOffset + limitObjectStartAndEnd[2].Length:"+ limitObjectStartAndEnd[2].Length;
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nDateTime.Parse(yearStringOffset + limitObjectStartAndEnd[1]):"+DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[1]);
                            if(externalData.DebugText != null) externalData.DebugText.text += "\nDateTime.Parse(yearStringOffset + limitObjectStartAndEnd[2]):"+DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[2]);
                            if(externalData.DebugText != null) externalData.DebugText.text += "\njst_tmp.CompareTo(DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[1]))"+jst_tmp.CompareTo(DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[1]));
                            if(externalData.DebugText != null) externalData.DebugText.text += "\njst_tmp.CompareTo(DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[2]))"+jst_tmp.CompareTo(DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[2]));
                            if(externalData.DebugText != null) externalData.DebugText.text += "\njst_tmp:"+jst_tmp;
                            */
                            if (
                                limitObjectStartAndEnd.Length >= 3
                                &&
                                    limitObjectStartAndEnd[0] == "0"//Direct 0,SDateTime,EDateTime
                                &&
                                    jst_tmp.CompareTo(DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[1])) >= 0 
                                &&
                                    jst_tmp.CompareTo(DateTime.Parse(yearStringOffset + limitObjectStartAndEnd[2])) <= 0
                                )
                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\nDirect Finish";
                                if(!timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(true);
                            }
                            else if (
                                    limitObjectStartAndEnd.Length >= 3
                                &&
                                    limitObjectStartAndEnd[0] == "1"//Dayly 1,STime,ETime
                                &&
                                    (
                                        ((TimeSpan.Parse(limitObjectStartAndEnd[1]) <= TimeSpan.Parse(limitObjectStartAndEnd[2])) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[1]) && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[2]))
                                        ||
                                        ((TimeSpan.Parse(limitObjectStartAndEnd[1]) > TimeSpan.Parse(limitObjectStartAndEnd[2])) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[1]) && jst_tmp.TimeOfDay <= oneDay_tmp)
                                        ||
                                        ((TimeSpan.Parse(limitObjectStartAndEnd[1]) > TimeSpan.Parse(limitObjectStartAndEnd[2])) && jst_tmp.TimeOfDay >= zero_tmp && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[2]))
                                    )
                                )
                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\nDayly Finish";
                                if(!timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(true);
                            }
                            else if (
                                    limitObjectStartAndEnd.Length >= 5
                                &&
                                    limitObjectStartAndEnd[0] == "2"//Weekly 2,SWeekNum,STime,EWeekNum,ETime (0:Sunday, 6:Fryday)
                                &&
                                    (
                                        ((startIntValue_tmp <= endIntValue_tmp) && jstIntValue_tmp >= startIntValue_tmp && jstIntValue_tmp <= endIntValue_tmp)
                                        ||
                                        ((startIntValue_tmp > endIntValue_tmp) && ((jstIntValue_tmp >= startIntValue_tmp && jstIntValue_tmp <= 7) || (jstIntValue_tmp >= 0 && jstIntValue_tmp <= endIntValue_tmp)))
                                    )
                                &&
                                    (
                                        ((jstIntValue_tmp != endIntValue_tmp) && (jstIntValue_tmp != startIntValue_tmp))
                                        ||
                                        ((startIntValue_tmp == endIntValue_tmp) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[2]) && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[4]))
                                        ||
                                        ((startIntValue_tmp == jstIntValue_tmp) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[2]) && jst_tmp.TimeOfDay <= oneDay_tmp)
                                        ||
                                        ((endIntValue_tmp == jstIntValue_tmp) && jst_tmp.TimeOfDay >= zero_tmp && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[4]))
                                    )
                                )
                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\nWeekly Finish";
                                if(!timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(true);
                            }
                            else if (
                                limitObjectStartAndEnd.Length >= 5
                                &&
                                limitObjectStartAndEnd[0] == "3"//Monthly 3,SDay,STime,EDay,ETime
                                &&
                                    (
                                        ((startIntValue_tmp <= endIntValue_tmp) && jstIntValue_tmp >= startIntValue_tmp && jstIntValue_tmp <= endIntValue_tmp)
                                        ||
                                        ((startIntValue_tmp > endIntValue_tmp) && ((jstIntValue_tmp >= startIntValue_tmp && jstIntValue_tmp <= 31) || (jstIntValue_tmp >= 1 && jstIntValue_tmp <= endIntValue_tmp)))
                                    )
                                &&
                                    (
                                        ((jstIntValue_tmp != endIntValue_tmp) && (jstIntValue_tmp != startIntValue_tmp))
                                        ||
                                        ((startIntValue_tmp == endIntValue_tmp) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[2]) && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[4]))
                                        ||
                                        ((startIntValue_tmp == jstIntValue_tmp) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[2]) && jst_tmp.TimeOfDay <= oneDay_tmp)
                                        ||
                                        ((endIntValue_tmp == jstIntValue_tmp) && jst_tmp.TimeOfDay >= zero_tmp && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[4]))
                                    )
                                )
                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\nMonthly Finish";
                                if(!timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(true);
                            }
                            else if (
                                limitObjectStartAndEnd.Length >= 7
                                &&
                                limitObjectStartAndEnd[0] == "4" //Yearly 4,SMonth,SDay,STime,EMonth,EDay,ETime
                                &&
                                    (
                                        ((startIntValue_tmp <= endIntValue_tmp) && jstIntValue_tmp >= startIntValue_tmp && jstIntValue_tmp <= endIntValue_tmp)
                                        ||
                                        ((startIntValue_tmp > endIntValue_tmp) && ((jstIntValue_tmp >= startIntValue_tmp && jstIntValue_tmp <= 12) || (jstIntValue_tmp >= 1 && jstIntValue_tmp <= endIntValue_tmp)))
                                    )
                                &&
                                    (
                                        ((jstIntValue_tmp != endIntValue_tmp) && (jstIntValue_tmp != startIntValue_tmp))
                                        ||
                                        ((startIntValue_tmp == endIntValue_tmp) && jstIntValue2_tmp >= startIntValue2_tmp && jstIntValue2_tmp <= endIntValue2_tmp)
                                        ||
                                        ((startIntValue_tmp == jstIntValue_tmp) /*&& jstIntValue2_tmp >= jstIntValue2_tmp*/ && jstIntValue2_tmp <= 31)//TODO:ちょっと怪しいので調査する
                                        ||
                                        ((endIntValue_tmp == jstIntValue_tmp) && jstIntValue2_tmp >= 1 && jstIntValue2_tmp <= endIntValue2_tmp)
                                    )
                                &&
                                    (
                                        ((jstIntValue2_tmp != endIntValue2_tmp) && (jstIntValue2_tmp != startIntValue2_tmp))
                                        ||
                                        ((startIntValue2_tmp == endIntValue2_tmp && startIntValue_tmp == endIntValue_tmp) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[3]) && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[6]))
                                        ||
                                        ((startIntValue2_tmp == jstIntValue2_tmp && startIntValue_tmp == jstIntValue_tmp) && jst_tmp.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[3]) && jst_tmp.TimeOfDay <= oneDay_tmp)
                                        ||
                                        ((endIntValue2_tmp == jstIntValue2_tmp && endIntValue_tmp == jstIntValue_tmp) && jst_tmp.TimeOfDay >= zero_tmp && jst_tmp.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[6]))
                                    )
                                )

                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\nYearly Finish";
                                if(!timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(true);
                            }
                            else if (
                                    limitObjectStartAndEnd.Length >= 3
                                &&
                                    limitObjectStartAndEnd[0] == "5"//Dayly(LocalTime) 1,STime,ETime
                                &&
                                    (
                                        ((TimeSpan.Parse(limitObjectStartAndEnd[1]) <= TimeSpan.Parse(limitObjectStartAndEnd[2])) && localTime.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[1]) && localTime.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[2]))
                                        ||
                                        ((TimeSpan.Parse(limitObjectStartAndEnd[1]) > TimeSpan.Parse(limitObjectStartAndEnd[2])) && localTime.TimeOfDay >= TimeSpan.Parse(limitObjectStartAndEnd[1]) && localTime.TimeOfDay <= oneDay_tmp)
                                        ||
                                        ((TimeSpan.Parse(limitObjectStartAndEnd[1]) > TimeSpan.Parse(limitObjectStartAndEnd[2])) && localTime.TimeOfDay >= zero_tmp && localTime.TimeOfDay <= TimeSpan.Parse(limitObjectStartAndEnd[2]))
                                    )
                                )
                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\nDayly(LocalTime) Finish";
                                if(!timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(true);
                            }
                            else //期間対象外
                            {
                                //if(externalData.DebugText != null) externalData.DebugText.text += "\n期間対象外 Finish";
                                if(timeLimitObject[i].activeSelf) timeLimitObject[i].SetActive(false);
                            }
                        }
                    }
                }

                if (isAlarm && localTime.Hour == localTimeAlarmHour && localTime.Minute == localTimeAlarmMinute && localTime.Second == 0) //Alarmチェック1秒以下の音は何度もなるので注意
                {
                    if(AlarmAudioSource != null)
                    {
                        if (!AlarmAudioSource.isPlaying) AlarmAudioSource.Play();
                    }
                }

                if (jst_tmp.Minute == 0 && jst_tmp.Second <= 2) //一時間ごとの更新チェック(処理落ちマージン2秒、複数回実行あり)
                {
                    SetSkyMySelf(jst_tmp);

                    if(jst_tmp.Second == 0 && jst_tmp.Hour == 0){ //0時更新
                        foreach(JstAnimationSync tmp in _jstAnimationSync)
                        {
                            if(tmp != null && tmp.gameObject.activeInHierarchy) tmp.SyncJst();
                        }
                    }
                }
                if(isAnimationSyncRegularly)
                {
                    if(animationSyncInterval_count == 0){
                        animationSyncInterval_count = animationSyncIntervalSecond;
                        foreach(JstAnimationSync tmp in _jstAnimationSync)
                        {
                            if(tmp != null && tmp.gameObject.activeInHierarchy) tmp.SyncJst();
                        } 

                    }else{
                        animationSyncInterval_count--;
                    }
                }
            }
            else
            {
                updateIntervalCount -= Time.deltaTime;
            }
        } 

        public void SetTimeLimitWithExternalData()
        {
            if(externalData == null) return;
            //if(externalData.DebugText != null) externalData.DebugText.text += "\nexternalData.data.Length" + externalData.data.Length;
            timeLimit = new string[timeLimitObject.Length];
            //if(externalData.DebugText != null) externalData.DebugText.text += "\ntimeLimit.Length" + timeLimit.Length;
            for(int i=0; i<timeLimit.Length; i++)
            {
                timeLimit[i] = externalData.data[i];
                //if(externalData.DebugText != null) externalData.DebugText.text += "\ntimeLimit["+i+"]:" + timeLimit[i];
            }
            //if(externalData.DebugText != null) externalData.DebugText.text += "\nSetTimeLimitWithExternalData Finish";
        }

        public void GetTime()
        {
            /*どこからアクセスされてもnowはutcで持つ*/
            now = Networking.GetNetworkDateTime();

            /*ローカルタイムも別途保存しておく*/
            localTime = now.ToLocalTime();
        }

        public DateTime GetJst()
        {
            if (now == null)
            {
                jstCache = System.TimeZoneInfo.ConvertTimeFromUtc(Networking.GetNetworkDateTime(), jstZoneInfo);
                return jstCache;
            }
            jstCache = System.TimeZoneInfo.ConvertTimeFromUtc(now, jstZoneInfo);
            return jstCache;
        }

        public bool IsNowFromMonth_Day(Vector2 monthDay)
        {
            int tmp_month = jstCache.Month;
            int tmp_day = jstCache.Day;
            if (tmp_month == monthDay.x && tmp_day == monthDay.y) return true;
            return false;
        }

        public int GetMonth()
        {
            return jstCache.Month;      
        }

        public int GetDay()
        {
            return jstCache.Day;
        }

        Weather GetJPTownWeather(DateTime jst) //JPTOWNの天候を取得します。このメソッドで天気を管理している場合すべてのワールドで天気が連動します
        {
            
            // if (
            //     jst.CompareTo(DateTime.Parse(StartDateTimeString[index])) >= 0 
            //     &&
            //     jst.CompareTo(DateTime.Parse(EndDateTimeString[index])) <= 0
            //     )
            // {
            // }
            bool rainySeason = false;
            bool winterSeason = false;

            float day_jst = (float)jst.Day;
            float month_jst = (float)jst.Month;

            if(month_jst == 6.0f || month_jst == 9.0f)//雨季（6月と9月は雨の当選確率があがる）
            {
                rainySeason = true;
            } 
            else if(month_jst == 11.0f || month_jst == 12.0f || month_jst == 1.0f || month_jst == 2.0f)//雪が降る時期（クリスマスの時期は雪が降ると嬉しいので実際より早めに雪が降るように設定。11月、12月、1月、2月は雪も抽選する）
            {
                winterSeason = true;
            } 
            int weatherBaseValue = (int)(((day_jst / month_jst) * 125.0f) / (month_jst / 3.0f)) % 7;
            //1日中同じ天気で0時に更新される時間によって天気が変化することはないため取得はログイン時と0時のみでよい
            
            Weather result = Weather.SUNNY_DAY;

            if(weatherBaseValue == 0) //０なら確定で雨
            {
                result = Weather.RAINY_DAY;
            }
            else if(weatherBaseValue == 1)//1はwinterSeasonなら雪、rainySeasonなら雨、それ以外はくもりになる。
            {
                if(winterSeason)
                {
                    result = Weather.SNOWY_DAY;
                }
                else if(rainySeason) 
                {
                    result = Weather.RAINY_DAY;
                }
                else
                {
                    result = Weather.CLOUDY_DAY;
                }
            }
            //Debug.Log("" + month_jst + "/" + day_jst + " weatherBaseValue:" + weatherBaseValue + " GetJPTownWeather:" + result);
            return result;
        }

        TimeOfSky GetTimeOfSkyJst(DateTime jst) //日本時間基準で時間による空模様を取得します
        {
            int month_jst = jst.Month;
            int hour_jst = jst.Hour;

            TimeOfSky result = TimeOfSky.NIGHT;
            //夏と冬の2パターンあるが、1時間ごとにちょうどの時間でチェックすればよい
            if(month_jst >= 4 && month_jst<=9)//4,5,6,7,8,9月は、夏至（4:30, 19:00）なので5:00-6:00を日の出、6:00-12:00を朝、12:00-17:00を昼、17:00-18:00を夕暮れ、18:00-5:00を夜とする
            {
                if(hour_jst > 5 && hour_jst <= 6)//5:00-6:00を日の出
                {
                    result = TimeOfSky.SUNRISE;
                }
                else if(hour_jst > 6 && hour_jst <= 12)//6:00-12:00を朝
                {
                    result = TimeOfSky.MORNING;
                }
                else if(hour_jst > 12 && hour_jst <= 17)//12:00-17:00を昼
                {
                    result = TimeOfSky.DAYTIME;
                }
                else if(hour_jst > 17 && hour_jst <= 18)//17:00-18:00を夕暮れ
                {
                    result = TimeOfSky.TWILIGHT;
                }
                else if(hour_jst > 18 && hour_jst <= 23 || hour_jst > 0 && hour_jst <= 5)//18:00-5:00を夜
                {
                    result = TimeOfSky.NIGHT;
                }
            }
            else //10,11,12,1,2,3月は、冬至（6:30, 16:30）なので7:00-8:00を日の出、8:00-11:00を朝、11:00-16:00を昼、16:00-17:00を夕暮れ、17:00-7:00を夜とする
            {
                if(hour_jst > 7 && hour_jst <= 8)//7:00-8:00を日の出
                {
                    result = TimeOfSky.SUNRISE;
                }
                else if(hour_jst > 8 && hour_jst <= 11)//8:00-11:00を朝
                {
                    result = TimeOfSky.MORNING;
                }
                else if(hour_jst > 11 && hour_jst <= 16)//11:00-16:00を昼
                {
                    result = TimeOfSky.DAYTIME;
                }
                else if(hour_jst > 16 && hour_jst <= 17)//16:00-17:00を夕暮れ
                {
                    result = TimeOfSky.TWILIGHT;
                }
                else if(hour_jst > 17 && hour_jst <= 23 || hour_jst > 0 && hour_jst <= 7)//17:00-7:00を夜
                {
                    result = TimeOfSky.NIGHT;
                }
            }

            return result;
        }

        private void SetSkyMySelf(DateTime jst)
        {
            if(RenderSettings.skybox != currentSkybox) return; //TimeManagerの管理外マテリアルが設定されているときは書き換えを見送り           
            //if(isChangeSourceColor && RenderSettings.ambientLight != currentColor) return; //TimeManagerの管理外ソースカラーが設定されているときは書き換えを見送り
            SetSky(jst);
        }

        public void SetSky(DateTime jst, bool isForce = false) //CLOUDY_DAY,RAINY_DAYは優先それ以外はTimeOfSkyにもとづいて設定する
        {
            int month_tmp = jst.Month;
            int day_tmp = jst.Day;
            int hour_tmp = jst.Hour;
            nowWeather = GetJPTownWeather(jst);
            nowTimeOfSky = GetTimeOfSkyJst(jst);

            if(nowTimeOfSky != reflectTimeOfSky  || nowWeather != reflectWeather || isForce)
            {
                /*空を変える*/
                if (_externalResourceLoadManager != null)
                {
                    if (isQuickLoad)
                    {
                        if(nowTimeOfSky == TimeOfSky.SUNRISE && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexSunriseSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = sunriseSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;
                            }
                            currentSkybox = sunriseSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = sunriseColor;
                        }

                        if(nowTimeOfSky == TimeOfSky.MORNING && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexMorningSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = morningSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;

                            }
                            currentSkybox = morningSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = morningColor;
                        }

                        if(nowTimeOfSky == TimeOfSky.DAYTIME && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexDaytimeSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = daytimeSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;
                            }
                            currentSkybox = daytimeSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = daytimeColor;
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.TWILIGHT && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexTwilightSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = twilightSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;
                            }
                            currentSkybox = twilightSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = twilightColor;
                        }

                        if(nowTimeOfSky == TimeOfSky.NIGHT && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexNightSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = nightSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;
                            }
                            currentSkybox = nightSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = nightColor;    
                        }
                        
                        if(nowTimeOfSky != TimeOfSky.NIGHT && (nowWeather == Weather.CLOUDY_DAY || nowWeather == Weather.RAINY_DAY))
                        {
                            if(isUseUrlIndexCloudyDaytimeSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = cloudyDaytimeSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;
                            }
                            currentSkybox = cloudyDaytimeSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = cloudyDaytimeColor;    
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.NIGHT && (nowWeather == Weather.CLOUDY_DAY || nowWeather == Weather.RAINY_DAY))
                        {
                            if(isUseUrlIndexCloudyNightSkybox)
                            {
                                _externalResourceLoadManager.reservedQuickSkyBoxUrlIndex = cloudyNightSkyboxUrlIndexQuickLoad;
                                _externalResourceLoadManager.quickSkyboxLoadReserved = true;
                            }
                            currentSkybox = cloudyNightSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = cloudyNightColor;    
                        }  
                    }
                    if (isStandardLoad)
                    {
                        if(nowTimeOfSky == TimeOfSky.SUNRISE && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexSunriseSkybox)
                            {
                                for(int i = 0; i < sunriseSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = sunriseSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = sunriseSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = sunriseColor;    
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.MORNING && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexMorningSkybox)
                            {
                                for(int i = 0; i < morningSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = morningSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = morningSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = morningColor;    
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.DAYTIME && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexDaytimeSkybox)
                            {
                                for(int i = 0; i < daytimeSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = daytimeSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = daytimeSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = daytimeColor;   
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.TWILIGHT && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexTwilightSkybox)
                            {
                                for(int i = 0; i < twilightSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = twilightSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = twilightSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = twilightColor;
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.NIGHT && nowWeather != Weather.CLOUDY_DAY && nowWeather != Weather.RAINY_DAY)
                        {
                            if(isUseUrlIndexNightSkybox)
                            {
                                for(int i = 0; i < nightSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = nightSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = nightSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = nightColor;    
                        }
                        
                        if(nowTimeOfSky != TimeOfSky.NIGHT && (nowWeather == Weather.CLOUDY_DAY || nowWeather == Weather.RAINY_DAY))
                        {
                            if(isUseUrlIndexCloudyDaytimeSkybox)
                            {
                                for(int i = 0; i < cloudyDaytimeSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = cloudyDaytimeSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = cloudyDaytimeSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = cloudyDaytimeColor;    
                        }
                        
                        if(nowTimeOfSky == TimeOfSky.NIGHT && (nowWeather == Weather.CLOUDY_DAY || nowWeather == Weather.RAINY_DAY))
                        {
                             if(isUseUrlIndexCloudyNightSkybox)
                            {
                                for(int i = 0; i < cloudyNightSkyboxUrlIndex.Length; i++)
                                {
                                   _externalResourceLoadManager.reservedSkyboxUrlIndex[i] = cloudyNightSkyboxUrlIndex[i];
                                }
                                _externalResourceLoadManager.skyboxLoadReserved = true;
                            }
                            currentSkybox = cloudyNightSkybox;
                            /*if(!isChangeSourceColor)*/ currentColor = cloudyNightColor;   
                        }
                    }
                }

                if(nowWeather == Weather.RAINY_DAY)
                {
                    foreach(GameObject tmp in rainParticles)
                    {
                        if(tmp != null)
                        {
                            tmp.SetActive(true);
                        } 
                    }
                }
                else
                {
                    foreach(GameObject tmp in rainParticles)
                    {
                        if(tmp != null)
                        {
                            tmp.SetActive(false);
                        } 
                    }
                }

                if(nowWeather == Weather.SNOWY_DAY)
                {
                    foreach(GameObject tmp in snowParticles)
                    {
                        if(tmp != null)
                        {
                            tmp.SetActive(true);
                        } 
                    }
                }
                else
                {
                    foreach(GameObject tmp in snowParticles)
                    {
                        if(tmp != null)
                        {
                            tmp.SetActive(false);
                        } 
                    }
                }

                if(currentSkybox != null) RenderSettings.skybox = currentSkybox;
                if(isChangeSourceColor) RenderSettings.ambientLight = currentColor;
                reflectWeather = nowWeather;
                reflectTimeOfSky = nowTimeOfSky;
            }
        }
        
        public void IncrementAlarmHour()
        {
            if (localTimeAlarmHour >= 23) localTimeAlarmHour = 0;
            else localTimeAlarmHour++;
            RefreshAlarmText();
        }

        public void DecrementAlarmHour()
        {
            if (localTimeAlarmHour <= 0) localTimeAlarmHour = 23;
            else localTimeAlarmHour--;
            RefreshAlarmText();
        }

        public void IncrementAlarmMinute()
        {
            if (localTimeAlarmMinute >= 59)
            {
                localTimeAlarmMinute = 0;
                IncrementAlarmHour();
            }
            else localTimeAlarmMinute++;
            RefreshAlarmText();
        }

        public void DecrementAlarmMinute()
        {
            if (localTimeAlarmMinute <= 0)
            {
                localTimeAlarmMinute = 59;
                DecrementAlarmHour();
            }
            else localTimeAlarmMinute--;
            RefreshAlarmText();
        }

        public void AlarmON()
        {
            isAlarm = true;
        }

        public void AlarmOFF()
        {
            isAlarm = false;
        }

        public void RefreshAlarmText()
        {
            if (localTimeAlarmText.Length >= 1)
            {
                string localTimeString = localTimeAlarmHour.ToString("00") + ":" + localTimeAlarmMinute.ToString("00");
                foreach (Text tmp in localTimeAlarmText)
                {
                    if (tmp != null && tmp.gameObject.activeInHierarchy) tmp.text = localTimeString;
                }
            }
        }
    }
}
