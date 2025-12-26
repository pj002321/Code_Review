using Cysharp.Threading.Tasks;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 메인 메뉴 화면 관리
    /// - 버튼 동작 설정
    /// - 상점 구매 처리
    /// - 화폐 UI 업데이트
    /// - 패널 관리
    /// </summary>
    public partial class MainMenuScreen : MonoBehaviour
    {
        private const float SOUND_SETTINGS_DELAY = 0.1f;
        private const float DEFAULT_VOLUME = 1f;
        private const string BGM_PREF_KEY = "BGM";
        private const string SFX_PREF_KEY = "SFX";

        private void Awake()
        {
            HandleButtonSfx();
            HandleSellBehaviour();
            HandleSoundSettingsAsync().Forget();
        }

        private void Update()
        {
            ActiveStatePannels();
        }

        private void OnDestroy()
        {
            if (shopConfirmUI != null)
            {
                shopConfirmUI.onPurchaseSuccess -= OnPurchaseConfirm;
            }
        }

        private async UniTaskVoid HandleSoundSettingsAsync()
        {
            await UniTask.WaitUntil(() => AudioManager.Shared != null);
            await UniTask.Delay(System.TimeSpan.FromSeconds(SOUND_SETTINGS_DELAY));
            HandleVolumeSettings();
        }

        /// <summary> 화폐 텍스트 업데이트 </summary>
        public void UpdateCurrencyTexts()
        {
            SetEliText();
            SetStoneText();
        }
    }
}
