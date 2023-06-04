
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using System;

namespace KUSAASOBIKOBO
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BeverageList : UdonSharpBehaviour
    {
        public string originalCocktailName = "オリジナルカクテル";
        public string[] beverageNameList;
        public Color[] beverageColorList;
        public Vector3 beverageFoamColorDifference = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 beverageTopColorDifference = new Vector3(15.0f, 15.0f, 15.0f);
        public Vector3 beverageRimColorDifference = new Vector3(30.0f, 30.0f, 30.0f);

        public BeverageShaker[] _beverageShaker;
        public ParticleSystem[] shakerParticle;

        public BeverageGlass[] _beverageBottle;
        public ParticleSystem[] bottleParticle;


        public string[] recipe;
        /*レシピの記載方法
            [完成する飲み物のindex],[材料のindex]:[割合(100分率)],[材料のindex]:[割合(100分率)],[材料のindex]:[割合(100分率)],[材料のindex]:[割合(100分率)]
            例：13,1:50,2:50
            ※スペースは入れないでください（誤表記修正機能実装済み）
            ※最後にカンマは入れないでください（誤表記修正機能実装済み）
            ※割合の合計値は100になるようにしてください
            ※同一の組み合わせで割合がallowableLimit範囲内のレシピが複数ある場合indexが小さいレシピが必ず優先されます（近いほうを選ぶわけではありません）
            ※全ドリンクに対して1種100%のレシピを作っておくと単体でカクテルを作った場合に正しく名前が表示されます
        */

        public void SetBeverageColor(int index, Material mat)
        {
            if(index > beverageColorList.Length) return;
            if(index < 0) return;
            mat.SetColor("_Colour", beverageColorList[index]);
            mat.SetColor("_TopColor", new Color(beverageColorList[index].r + beverageTopColorDifference.x/255.0f,beverageColorList[index].g + beverageTopColorDifference.y/255.0f,beverageColorList[index].b + beverageTopColorDifference.z/255.0f,beverageColorList[index].a));
            mat.SetColor("_FoamColor", new Color(beverageColorList[index].r + beverageFoamColorDifference.x/255.0f,beverageColorList[index].g + beverageFoamColorDifference.y/255.0f,beverageColorList[index].b + beverageFoamColorDifference.z/255.0f,beverageColorList[index].a));
            mat.SetColor("_RimColor", new Color(beverageColorList[index].r + beverageRimColorDifference.x/255.0f,beverageColorList[index].g + beverageRimColorDifference.y/255.0f,beverageColorList[index].b + beverageRimColorDifference.z/255.0f,beverageColorList[index].a));
        }

        public void SetCocktailColor(int index, Material mat)//indexはシェイカーのindex
        {
            if(index > _beverageShaker.Length) return;
            if(index < 0) return;
            if(_beverageShaker[index]._beverageGlass == null) return;
            if(_beverageShaker[index]._beverageGlass.index != -1){
                 SetBeverageColor(_beverageShaker[index]._beverageGlass.index, mat);
            }
            else
            {
                _beverageShaker[index].SetOriginalColor();
                mat.SetColor("_Colour", _beverageShaker[index]._beverageGlass.color);
                mat.SetColor("_TopColor", new Color(_beverageShaker[index]._beverageGlass.color.r + beverageTopColorDifference.x/255.0f,_beverageShaker[index]._beverageGlass.color.g + beverageTopColorDifference.y/255.0f,_beverageShaker[index]._beverageGlass.color.b + beverageTopColorDifference.z/255.0f,_beverageShaker[index]._beverageGlass.color.a));
                mat.SetColor("_FoamColor", new Color(_beverageShaker[index]._beverageGlass.color.r + beverageFoamColorDifference.x/255.0f,_beverageShaker[index]._beverageGlass.color.g + beverageFoamColorDifference.y/255.0f,_beverageShaker[index]._beverageGlass.color.b + beverageFoamColorDifference.z/255.0f,_beverageShaker[index]._beverageGlass.color.a));
                mat.SetColor("_RimColor", new Color(_beverageShaker[index]._beverageGlass.color.r + beverageRimColorDifference.x/255.0f,_beverageShaker[index]._beverageGlass.color.g + beverageRimColorDifference.y/255.0f,_beverageShaker[index]._beverageGlass.color.b + beverageRimColorDifference.z/255.0f,_beverageShaker[index]._beverageGlass.color.a));
            }
        }

        public void SetBeverageName(int index, Text _text)
        {
            if(index > beverageNameList.Length) return;
            if(index < 0)
            {
                _text.text = originalCocktailName;
            }
            else
            {
                _text.text = beverageNameList[index];
            }
        }

        public int CheckRecipe(BeverageShaker shaker)
        {
            int repertory = beverageColorList.Length;
            if(repertory != beverageNameList.Length) return -1;
            if(repertory != shaker.distribution.Length) return -1;
            int recipeNum = recipe.Length;
            int ingredientsNum = 0;
            float totalDistribution = 0.0f;
            foreach(float distributionTmp in shaker.distribution)
            {
                totalDistribution += distributionTmp;
                if(distributionTmp != 0.0f) ingredientsNum++;
            }

            if(ingredientsNum == 0) return -1;

            for(int i=0; i < recipeNum; i++)
            {
                bool result = true;
                //レシピを解析
                if(recipe[i].Length != 0)
                {
                    //誤表記を修正
                    //最後にカンマを入れる誤表記を修正
                    char lastChar = recipe[i][recipe[i].Length-1];
                    if(lastChar == ',') recipe[i].Remove(recipe[i].Length-1);

                    //ありそうな誤表記を修正
                    recipe[i].Replace(" ","");
                    recipe[i].Replace("　","");
                    recipe[i].Replace("%","");
                    recipe[i].Replace("％","");
                    recipe[i].Replace(";",":");

                    string[] tmp =  recipe[i].Split(',');
                    if(ingredientsNum == (tmp.Length-1)) //素材の数が同じならより詳細にチェック
                    {
                        for(int j = 1;j < tmp.Length; j++) //tmp[0]は完成したもののindexなのでスキップ
                        {
                            string[] tmp2 =  tmp[j].Split(':');
                            int ingredientsIndex;
                            int ingredientsDistribution;
                            if(tmp2.Length != 2)
                            {
                                result = false;
                                break;//エラー表記があるならレシピが無効
                            }
                            Debug.Log("tmp2[0]:"+tmp2[0]);
                            Debug.Log("tmp2[1]:"+tmp2[1]);
                            Debug.Log("tmp[0]:"+tmp[0]);
                            ingredientsIndex = Int32.Parse(tmp2[0]);
                            ingredientsDistribution = Int32.Parse(tmp2[1]);
                            
                            Debug.Log("shaker.distribution[ingredientsIndex]:"+shaker.distribution[ingredientsIndex]);
                            Debug.Log("Math.Abs(ingredientsDistribution - ((shaker.distribution[ingredientsIndex]/totalDistribution)*100)):"+Math.Abs(ingredientsDistribution - ((shaker.distribution[ingredientsIndex]/totalDistribution)*100)));
                            if(shaker.distribution[ingredientsIndex] == 0.0f && ingredientsDistribution != 0)
                            {
                                result = false;
                                break;//対象の素材が入っていないならこのレシピではない
                            } 
                            if(Math.Abs(ingredientsDistribution - ((shaker.distribution[ingredientsIndex]/totalDistribution)*100)) > shaker.allowableLimit)
                            {
                                result = false;
                                break;//対象の素材の正しい割合と比較した差が許容値を超えてしまったらこのレシピではない
                            }
                        }
                        if(result) return Int32.Parse(tmp[0]);//素材数が同一で且つレシピの素材がすべて許容値以内の割合で入っているならレシピのindexを返して処理を終える
                    }else result = false;
                }else result = false;
            }
            return -1;//すべてのレシピを探索し終えたが見つからなかった
        }
    }
}