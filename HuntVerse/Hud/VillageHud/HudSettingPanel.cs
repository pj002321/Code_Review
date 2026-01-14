using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class HudSettingPanel : MonoBehaviour
    {
        [Header("PROFILE")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Image playerClassIconImage;
        [SerializeField] private RawImage portraitRawImg;

        private GameObject portraitModel;
        private GameObject portraitCam;
        private ClassType playerClassType;

        private void OnEnable()
        {
            SetCharacter().Forget();
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        private async UniTask SetCharacter()
        {
            var selectedChar = GameSession.Shared?.SelectedCharacter;
            var selectedModel = GameSession.Shared?.SelectedCharacterModel;
            var classType = BindKeyConst.GetClassTypeByJobId(selectedChar.ClassType);
            if (selectedChar != null)
            {
                
                await UpdateCharInfo(selectedChar.Name, selectedChar.Level, classType);
            }
            else
            {
                this.DError("캐릭터 정보를 찾을 수 없습니다.");
                await UpdateCharInfo("Hunt", 13, ClassType.Archer);
            }
        }

        private async UniTask UpdateCharInfo(string name, ulong level, ClassType classType)
        {
            if (playerNameText!= null) 
                playerNameText.text = name;
            if (playerLevelText != null) 
                playerLevelText.text = $"Lv. {level}";

            playerClassType = classType;

            var key = BindKeyConst.GetIconKeyByProfession(playerClassType);
            if (playerClassIconImage != null || !string.IsNullOrEmpty(key))
            {
                var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(key);
                playerClassIconImage.sprite = sprite;
            }
            await LoadPortrait(playerClassType);
        }

        private async UniTask LoadPortrait(ClassType classType)
        {
            if (playerClassIconImage == null || AbLoader.Shared == null)
            {
                this.DError("ClassIconImage is NULL.");
                return;
            }

            var modelKey = BindKeyConst.GetModelKeyByProfession(classType);
            var camKey = ResourceKeyConst.Kp_Portrait_Cam;
            if (string.IsNullOrEmpty(modelKey) || string.IsNullOrEmpty(camKey))
            {
                this.DError($"포트레이트 키를 찾을 수 없습니다: Model={modelKey}, Cam={camKey}");
                return;
            }
            
            Release();

            portraitCam = await AbLoader.Shared.LoadInstantiateAsync(camKey);
            portraitModel = await AbLoader.Shared.LoadInstantiateAsync(modelKey);

            if (portraitCam == null || portraitModel == null)
            {
                $"포트레이트 스폰 실패: Model={portraitModel != null}, Cam={portraitCam != null}".DError();
                return;
            }

            portraitModel.transform.SetParent(transform);
            portraitCam.transform.SetParent(portraitModel.transform);
            portraitCam.transform.localPosition = new Vector3(0, 0, -10);
            portraitModel.transform.localPosition = new Vector3(0, 200, 0);
            portraitModel.transform.localRotation = Quaternion.identity;

            int rtLayer = LayerMask.NameToLayer("RT");
            if (rtLayer == -1)
            {
                this.DError($"RT 레이어를 찾을 수 없습니다");
            }
            else
            {
                SetLayerRecursive(portraitModel.transform, rtLayer);
            }

            var animator = portraitModel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.CrossFade(AniKeyConst.k_cDancing, 0.1f);
            }

        }

        private void SetLayerRecursive(Transform parent, int layer)
        {
            parent.gameObject.layer = layer;
            foreach (Transform child in parent)
            {
                SetLayerRecursive(child, layer);
            }
        }

        private void Release()
        {
            if(portraitModel != null)
            {
                var modelKey = BindKeyConst.GetModelKeyByProfession(playerClassType);
                if(!string.IsNullOrEmpty(modelKey) )
                {
                    AbLoader.Shared?.ReleaseAsset(modelKey);
                }
                Destroy(portraitModel);
                portraitModel = null;
            }

            if(portraitCam != null)
            {
                var camkey = ResourceKeyConst.Kp_Portrait_Cam;
                if (!string.IsNullOrEmpty(camkey))
                {
                    AbLoader.Shared?.ReleaseAsset(camkey);
                }
                Destroy(portraitCam);
                portraitCam = null;
            }
        }

        public void OpenSkillPanel()
        {

        }

        public void OpenInventoryPanel()
        {

        }
    }
}
