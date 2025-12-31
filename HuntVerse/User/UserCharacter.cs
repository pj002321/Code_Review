using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Hunt
{

    public class UserCharacter : MonoBehaviour
    {
        private UserCharLoco characterAction;
        private GameObject model;

        private bool isSetupComplete = false;
        public bool IsSetupComplete => isSetupComplete;
        private void Start()
        {
            characterAction = GetComponent<UserCharLoco>();
            if (characterAction != null)
            {
                characterAction.enabled = false;
            }

            var myChar = GameSession.Shared?.SelectedCharacter;

            string modelKey;
            Vector3 spawnpos = Vector3.zero;

            if (myChar != null)
            {
                modelKey = BindKeyConst.GetModelKeyByProfession((ClassType)myChar.ClassType);
                $"[UserCharacter] 캐릭터 스폰 {myChar}: {myChar.Name} (Lv.{myChar.Level}, ClassType:{myChar.ClassType}".DLog();

            }
            else if (GameSession.Shared.SelectedCharacterModel != null)
            {
                var model = GameSession.Shared.SelectedCharacterModel;
                modelKey = BindKeyConst.GetModelKeyByProfession(model.classtype);
                $"[UserCharacter] 캐릭터 스폰 (CharacterModel/Dev): {model.name}".DLog();
            }
            else
            {
                modelKey = BindKeyConst.GetModelKeyByProfession(ClassType.Archer);
                $"[UserCharacter] ⚠ 선택된 캐릭터 없음".DError();

            }

            SetUp(modelKey,spawnpos).Forget();

        }
        private async UniTask SetUp(string modelKey, Vector3 spawnPos)
        {
            try
            {
                if(AbLoader.Shared==null)
                {
                    $"Abloader not set".DError();
                }
                var go = await AbLoader.Shared.LoadAssetAsync<GameObject>(modelKey);
                if (go == null)
                {
                    $"Abloader Error : {modelKey}".DError();
                }
                model = Instantiate<GameObject>(go);
                model.transform.SetParent(transform);
                model.transform.position = Vector3.zero;
                model.transform.rotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.transform.position = spawnPos;

                if (characterAction != null)
                {
                    characterAction.enabled = true;
                    characterAction.Initialize(model);
                }

                isSetupComplete = true;

            }
            catch(Exception e) 
            {
                $"User Character Setup Fail! {e.Message}".DError();
            }


        }

    }

}