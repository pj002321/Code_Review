using Cysharp.Threading.Tasks;
using UnityEngine;

namespace chaos
{
    public partial class MainMenuScreen : MonoBehaviour
    {
        #region Constants
        private const float BGM_DELAY = 0.1f;
        private const float SOUND_SETTINGS_DELAY = 0.1f;
        private const float DEFAULT_VOLUME = 1f;
        private const string BGM_PREF_KEY = "BGM";
        private const string SFX_PREF_KEY = "SFX";
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            HandleButtonSfx();
            HandleSellBehaviour();
            HandleSoundSettingsAsync().Forget();
        }

        public void Update()
        {
            ActiveStatePannels();
        }

        private void OnEnable()
        {
            if (ShouldPlayBGM())
            {
                PlayBGMAsync().Forget();
            }

            UpdateCurrencyTexts();
        }

        private void OnDestroy()
        {
            if (shopConfirmUI != null)
            {
                shopConfirmUI.onPurchaseSuccess -= OnPurchaseConfirm;
            }
        }
        #endregion

        #region Audio
        private bool ShouldPlayBGM()
        {
            return gameObject.activeInHierarchy && 
                   AudioHelper.GetCurrentBGM() != AudioConst.MAIN_MENU_BGM;
        }

        private async UniTaskVoid PlayBGMAsync()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(BGM_DELAY));
            AudioHelper.PlayBGM(AudioConst.MAIN_MENU_BGM, DEFAULT_VOLUME);
        }

        private async UniTaskVoid HandleSoundSettingsAsync()
        {
            await UniTask.WaitUntil(() => AudioManager.Shared != null);
            await UniTask.Delay(System.TimeSpan.FromSeconds(SOUND_SETTINGS_DELAY));
            HandleVolumeSettings();
        }
        #endregion

        #region Currency UI
        private void UpdateCurrencyTexts()
        {
            SetEliText();
            SetStoneText();
        }
        #endregion
    }
}
