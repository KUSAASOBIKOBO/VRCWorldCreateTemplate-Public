
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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MultiAreaBathManager : UdonSharpBehaviour
    {
        public MultiAreaManager _multiAreaManager;
        public float completionTime = 300.0f;
        public bool isComplete = false;

        public GameObject bathSurface;
        public float bathSurfaceHeightMax = 1.0f;
        public float bathSurfaceHeightMin = 0.0f;


        private float updateIntervalSecondCount = 0;
        public float updateIntervalSecond = 60.0f;

        public AudioSource SESpeaker;
        public AudioClip finishSound;

        public GameObject puttingWaterSpeaker;
        public GameObject completeParticle;

        void OnEnable()
        {
            isComplete = false;
            Check();
        }

        void Update()
        {
            if (_multiAreaManager == null) return;
            if (isComplete) return;

            if (updateIntervalSecondCount > 0)
            {
                updateIntervalSecondCount -= Time.deltaTime;

                if (updateIntervalSecondCount <= 0)
                {
                    updateIntervalSecondCount = updateIntervalSecond;
                    Check(true);
                }
            }
            else
            {
                updateIntervalSecondCount = updateIntervalSecond;
                Check();
            }
        }

        void Check(bool isUpdateCall = false)
        {
            if (_multiAreaManager == null) return;
            if (bathSurface == null) return;
            float elpsedTime = _multiAreaManager.DateTimeSaveElapsedTime();
            if(elpsedTime >= completionTime)
            {
                bathSurface.transform.localPosition = new Vector3(bathSurface.transform.localPosition.x, bathSurfaceHeightMax, bathSurface.transform.localPosition.z);
                if(isUpdateCall)
                {
                    if (SESpeaker != null && finishSound != null && !isComplete)
                    {
                        SESpeaker.PlayOneShot(finishSound);
                    }
                }
                if (puttingWaterSpeaker != null && puttingWaterSpeaker.activeSelf)
                {
                    puttingWaterSpeaker.SetActive(false);
                }

                if (completeParticle != null && !completeParticle.activeSelf)
                {
                    completeParticle.SetActive(true);
                }
                isComplete = true;
            }
            else
            {
                isComplete = false;
                float rate = elpsedTime / completionTime;
                float y_tmp = (bathSurfaceHeightMax - bathSurfaceHeightMin) * rate + bathSurfaceHeightMin;
                bathSurface.transform.localPosition = new Vector3(bathSurface.transform.localPosition.x, y_tmp, bathSurface.transform.localPosition.z);
                if(puttingWaterSpeaker != null && !puttingWaterSpeaker.activeSelf)
                {
                    puttingWaterSpeaker.SetActive(true);
                }

                if (completeParticle != null && completeParticle.activeSelf)
                {
                    completeParticle.SetActive(false);
                }
            }
        }
    }
}
