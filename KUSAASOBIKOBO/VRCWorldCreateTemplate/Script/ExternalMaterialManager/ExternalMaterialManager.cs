
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.Rendering;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalMaterialManager : UdonSharpBehaviour
    {
        public Material[] ExTextureMaterial1;
        public Material[] ExTextureMaterial2;
        public Material[] ExTextureMaterial3;
        public Material[] ExTextureMaterial4;
        public Material[] ExTextureMaterial5;
        public Material[] ExTextureMaterial6;
        public Material[] ExTextureMaterial7;
        public Material[] ExTextureMaterial8;
        public Material[] ExTextureMaterial9;
        public Material[] ExTextureMaterial10;
        public Material[] ExTextureMaterial11;
        public Material[] ExTextureMaterial12;
        public Material[] ExTextureMaterial13;
        public Material[] ExTextureMaterial14;
        public Material[] ExTextureMaterial15;
        public Material[] ExTextureMaterial16;
        public Material[] ExTextureMaterial17;
        public Material[] ExTextureMaterial18;
        public Material[] ExTextureMaterial19;
        public Material[] ExTextureMaterial20;

        public void ChangeTiling(int textureId, Vector2 tiling)
        {
            switch (textureId)
            {
                case 1:
                    foreach(Material tmp in ExTextureMaterial1)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 2:
                    foreach (Material tmp in ExTextureMaterial2)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 3:
                    foreach (Material tmp in ExTextureMaterial3)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 4:
                    foreach (Material tmp in ExTextureMaterial4)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 5:
                    foreach (Material tmp in ExTextureMaterial5)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 6:
                    foreach (Material tmp in ExTextureMaterial6)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 7:
                    foreach (Material tmp in ExTextureMaterial7)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 8:
                    foreach (Material tmp in ExTextureMaterial8)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 9:
                    foreach (Material tmp in ExTextureMaterial9)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 10:
                    foreach (Material tmp in ExTextureMaterial10)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 11:
                    foreach (Material tmp in ExTextureMaterial11)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 12:
                    foreach (Material tmp in ExTextureMaterial12)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 13:
                    foreach (Material tmp in ExTextureMaterial13)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 14:
                    foreach (Material tmp in ExTextureMaterial14)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 15:
                    foreach (Material tmp in ExTextureMaterial15)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 16:
                    foreach (Material tmp in ExTextureMaterial16)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 17:
                    foreach (Material tmp in ExTextureMaterial17)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 18:
                    foreach (Material tmp in ExTextureMaterial18)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 19:
                    foreach (Material tmp in ExTextureMaterial19)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
                case 20:
                    foreach (Material tmp in ExTextureMaterial20)
                    {
                        tmp.SetTextureScale("_MainTex", tiling);
                    }
                    break;
            }
        }

        public void ResetTiling(int textureId)
        {
            ChangeTiling(textureId, new Vector2(1.0f, 1.0f));
        }

        public void ResetTilingAll()
        {
            for(int i=1; i<=20; i++)
            {
                ResetTiling(i);
            }
        }
    }
}