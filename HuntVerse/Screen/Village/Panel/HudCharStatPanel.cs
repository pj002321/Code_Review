using Cysharp.Threading.Tasks;
using Hunt.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public enum CharStatType
    {
        
        HP,MP,      // ü��,����
        STR,        // ��
        INT,        // ����
        PATK,MATK,  // ���� �� ���� ���ݷ�
        CRIT,       // ũ��Ƽ��
        ASPD,MSPD,  // ���� �ӵ�, �̵� �ӵ�
        LUK,        // ��
        DEF,        // ����
        EVA,        // ȸ��
    }

    public class HudCharStatPanel : MonoBehaviour
    {
        [SerializeField] public CharStatType charStatType;
        [SerializeField] private TextMeshProUGUI charNameText;
        [SerializeField] private RawImage charIconImg;
        private GameObject portraitModel;
        private GameObject portraitCam;
        private ClassType playerClassType;

        private void OnEnable()
        {
            UpdateStatPanel().Forget();
        }
        private async UniTask UpdateStatPanel()
        {
            var myChar = GameSession.Shared?.SelectedCharacter;
            var myCharModel = GameSession.Shared?.SelectedCharacterModel;
            if (myChar != null)
            {
                var classType = BindKeyConst.GetClassTypeByJobId(myChar.ClassType);
                playerClassType = classType;
                await LoadPortrait(playerClassType);
                SetName(myChar);
            }

        }

        private void SetName(SimpleCharacterInfo userInfo)
        {
            charNameText.text = userInfo.Name;
        }

        private async UniTask LoadPortrait(ClassType classType)
        {
            if (AbLoader.Shared == null)
            {
                this.DError("AbLoader is NULL.");
                return;
            }

            var modelKey = BindKeyConst.GetModelKeyByProfession(classType);
            var camKey = ResourceKeyConst.Kp_Portrait_Cam;
            if (string.IsNullOrEmpty(modelKey) || string.IsNullOrEmpty(camKey))
            {
                this.DError($"Portrait 키를 찾을 수 없습니다: Model={modelKey}, Cam={camKey}");
                return;
            }

            Release();

            portraitCam = await AbLoader.Shared.LoadInstantiateAsync(camKey);
            portraitModel = await AbLoader.Shared.LoadInstantiateAsync(modelKey);

            if (portraitCam == null || portraitModel == null)
            {
                $"Portrait 스폰 실패: Model={portraitModel != null}, Cam={portraitCam != null}".DError();
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
                this.DError($"RT 레이어 없음.");
            }
            SetupPortraitLayerAndCamera(portraitModel.transform, rtLayer);

            var animator = portraitModel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.CrossFade(AniKeyConst.k_cDancing, 0.1f);
            }

        }

        private void SetupPortraitLayerAndCamera(Transform parent, int layer)
        {
            parent.gameObject.layer = layer;
            foreach (Transform child in parent)
            {
                SetupPortraitLayerAndCamera(child, layer);
            }

            if (parent == portraitModel.transform && portraitCam != null)
            {
                var cam = portraitCam.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0, 0, 0, 0);

                    this.DLog($"Portrait Camera 설정 완료.");
                }
            }
        }
        private void Release()
        {
            if (portraitModel != null)
            {
                var modelKey = BindKeyConst.GetModelKeyByProfession(playerClassType);
                if (!string.IsNullOrEmpty(modelKey))
                {
                    AbLoader.Shared?.ReleaseAsset(modelKey);
                }
                Destroy(portraitModel);
                portraitModel = null;
            }

            if (portraitCam != null)
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
    }
}
