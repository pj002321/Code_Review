using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Hunt
{

    public class HudCharInventoryPanel : MonoBehaviour
    {
        [SerializeField] private RawImage portrait;
        private GameObject portraitCam;
        private GameObject portraitModel;
        private ClassType playerClassType;
        private void OnEnable()
        {
            UpdateInvenPanel().Forget();
        }

        private async UniTask UpdateInvenPanel()
        {
            var myChar = GameSession.Shared?.SelectedCharacter;
            var myCharModel = GameSession.Shared?.SelectedCharacterModel;
            if (myChar != null)
            {
                var classType = BindKeyConst.GetClassTypeByJobId(myChar.ClassType);
                playerClassType = classType;
                await LoadPortrait(playerClassType);
            }

        }

        private async UniTask LoadPortrait(ClassType classType)
        {
            try
            {

                var myChar = GameSession.Shared?.SelectedCharacter;
                var modelKey = BindKeyConst.GetModelKeyByProfession(classType);
                var camKey = ResourceKeyConst.Kp_Portrait_Cam;
                Release();
                portraitModel = await AbLoader.Shared.LoadInstantiateAsync(modelKey);
                portraitCam = await AbLoader.Shared.LoadInstantiateAsync(camKey);

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

                this.DLog("Portrait 로딩 완료.");
            }
            catch(Exception e)
            {
                this.DError($"Portrait 로딩 실패 : {e.Message}");
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