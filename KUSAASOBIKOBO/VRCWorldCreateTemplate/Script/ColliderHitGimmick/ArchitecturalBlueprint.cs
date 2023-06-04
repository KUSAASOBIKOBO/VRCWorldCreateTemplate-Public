
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KUSAASOBIKOBO
{
    /*
     * ＜説明＞
     * ゲームオブジェクトのtransformを変更し、指定の位置に指定の回転角で指定のスケールにしてルートを指定の親につけて建築します。
     * 外部スクリプトから発火するモジュールのスクリプトです。
     */
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ArchitecturalBlueprint : UdonSharpBehaviour
    {
        [Header("建築時に要素をActiveにする")] public bool isForceActive = false;
        [Header("OnEnable時に建築する")] public bool isSelfArchitect = false;
        [Header("OnDisable時に解体する")] public bool isSelfDismantling = false;
        [Header("OnEnable時に要素を非Activeにする")] public bool isReset = false;

        [Header("建築対象のルート(ここで指定したオブジェクトの親を変更します)")] public GameObject objectRoot;
        [Header("建築対象の親")] public Transform parentTransform;

        [Header("建築オブジェクトリスト(トランスフォームリストと同一要素数である必要があります)")] public GameObjectList architectObjectList;
        [Header("建築トランスフォームリスト(オブジェクトリストと同一要素数である必要があります)")] public TransformList architectTransformList;

        [Header("建築リストを直接指定する(trueの場合直接指定が優先されます)")] public bool isDirectInputArchitectList = false;
        [Header("直接指定建築オブジェクトリスト")] public GameObject[] DirectArchitectObjectList;
        [Header("直接指定建築トランスフォームリスト")] public Transform[] DirectArchitectTransformList;

        private Vector3[] originalPosition;
        private Quaternion[] originalRotation;
        private Vector3[] originalLocalScale;

        void OnEnable()
        {
            if (isReset)
            {
                if (isDirectInputArchitectList)
                {
                    for (int i = 0; i < DirectArchitectObjectList.Length; i++)
                    {
                        if (DirectArchitectObjectList[i] != null) DirectArchitectObjectList[i].SetActive(false);
                    }
                }
                else
                {
                    for (int i = 0; i < architectObjectList.elementList.Length; i++)
                    {
                        if (architectObjectList.elementList[i] != null) architectObjectList.elementList[i].SetActive(false);
                    }
                }
            }
            if (isSelfArchitect) Architect();
        }

        void OnDisable()
        {
            if(isSelfDismantling) Dismantling();
        }

        public void Architect()
        {
            //if(OriginalArchitectTransformList != null && OriginalArchitectTransformList.Length == 0) OriginalArchitectTransformList = new Transform[DirectArchitectTransformList.Length];
            

            if (isDirectInputArchitectList)
            {
                if (originalPosition == null || originalPosition.Length == 0) originalPosition = new Vector3[DirectArchitectTransformList.Length];
                if (originalRotation == null || originalRotation.Length == 0) originalRotation = new Quaternion[DirectArchitectTransformList.Length];
                if (originalLocalScale == null || originalLocalScale.Length == 0) originalLocalScale = new Vector3[DirectArchitectTransformList.Length];

                if (DirectArchitectObjectList.Length != DirectArchitectTransformList.Length) return;

                if (objectRoot != null && parentTransform != null) objectRoot.transform.parent = parentTransform;

                for (int i = 0; i < DirectArchitectObjectList.Length; i++)
                {
                    if (DirectArchitectObjectList[i] != null && DirectArchitectTransformList[i] != null)
                    {
                        if(isForceActive) DirectArchitectObjectList[i].SetActive(true);

                        originalPosition[i] = DirectArchitectObjectList[i].transform.position;
                        originalRotation[i] = DirectArchitectObjectList[i].transform.rotation;
                        originalLocalScale[i] = DirectArchitectObjectList[i].transform.localScale;

                        DirectArchitectObjectList[i].transform.position = DirectArchitectTransformList[i].position;
                        DirectArchitectObjectList[i].transform.rotation = DirectArchitectTransformList[i].rotation;
                        DirectArchitectObjectList[i].transform.localScale = DirectArchitectTransformList[i].localScale;
                    }
                }
            }
            else
            {
                if (originalPosition == null || originalPosition.Length == 0) originalPosition = new Vector3[architectTransformList.elementList.Length];
                if (originalRotation == null || originalRotation.Length == 0) originalRotation = new Quaternion[architectTransformList.elementList.Length];
                if (originalLocalScale == null || originalLocalScale.Length == 0) originalLocalScale = new Vector3[architectTransformList.elementList.Length];

                if (architectObjectList == null || architectTransformList == null) return;
                if (architectObjectList.elementList.Length != architectTransformList.elementList.Length) return;

                if (objectRoot != null && parentTransform != null) objectRoot.transform.parent = parentTransform;

                for (int i = 0; i < architectObjectList.elementList.Length; i++)
                {
                    if (architectObjectList.elementList[i] != null && architectTransformList.elementList[i] != null)
                    {
                        if(isForceActive) architectObjectList.elementList[i].SetActive(true);

                        originalPosition[i] = architectObjectList.elementList[i].transform.position;
                        originalRotation[i] = architectObjectList.elementList[i].transform.rotation;
                        originalLocalScale[i] = architectObjectList.elementList[i].transform.localScale;

                        architectObjectList.elementList[i].transform.position = architectTransformList.elementList[i].position;
                        architectObjectList.elementList[i].transform.rotation = architectTransformList.elementList[i].rotation;
                        architectObjectList.elementList[i].transform.localScale = architectTransformList.elementList[i].localScale;
                    }
                }
            }
        }

        public void Dismantling()
        {
            if (originalPosition == null || originalRotation == null || originalLocalScale == null || originalPosition.Length == 0 || originalRotation.Length == 0 || originalLocalScale.Length == 0) return;

            if (isDirectInputArchitectList)
            {
                if (DirectArchitectObjectList.Length != originalPosition.Length) return;

                if (objectRoot != null && parentTransform != null) objectRoot.transform.parent = parentTransform;

                for (int i = 0; i < DirectArchitectObjectList.Length; i++)
                {
                    if (DirectArchitectObjectList[i] != null && DirectArchitectTransformList[i] != null)
                    {
                        DirectArchitectObjectList[i].transform.position = originalPosition[i];
                        DirectArchitectObjectList[i].transform.rotation = originalRotation[i];
                        DirectArchitectObjectList[i].transform.localScale = originalLocalScale[i];
                    }
                }
            }
            else
            {
                if (architectObjectList == null || architectTransformList == null) return;
                if (architectObjectList.elementList.Length != originalPosition.Length) return;

                if (objectRoot != null && parentTransform != null) objectRoot.transform.parent = parentTransform;

                for (int i = 0; i < architectObjectList.elementList.Length; i++)
                {
                    if (architectObjectList.elementList[i] != null && architectTransformList.elementList[i] != null)
                    {
                        architectObjectList.elementList[i].transform.position = originalPosition[i];
                        architectObjectList.elementList[i].transform.rotation = originalRotation[i];
                        architectObjectList.elementList[i].transform.localScale = originalLocalScale[i];
                    }
                }
            }
        }
    }
}
