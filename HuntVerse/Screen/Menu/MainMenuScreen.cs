using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{

    public class MainMenuScreen : MonoBehaviour
    {
        [Header("HUDS")]
        [SerializeField] GameObject mainHud;
        [SerializeField] GameObject characterSelectHud;
        [SerializeField] Button enterVillageButton;
        private async void Awake()
        {
            await UniTask.WaitUntil(() => AudioHelper.Shared);
            AudioHelper.Shared.PlayBgm(AudioKeyConst.GetSfxKey(AudioType.BGM_MAIN));
            enterVillageButton.onClick.AddListener(() => EnterVillage().Forget());
            OnViewMainHud();
        }
        private async UniTask EnterVillage()
        {
            await SceneLoadHelper.Shared.LoadSceneSingleMode(ResourceKeyConst.Ks_Village);
        }
        public void OnViewCharacterSelectHud()
        {
            if (mainHud.activeSelf) mainHud.SetActive(false);
            if (!characterSelectHud.activeSelf) characterSelectHud.SetActive(true);
        }

        public void OnViewMainHud()
        {
            if (characterSelectHud.activeSelf) characterSelectHud.SetActive(false);
            if (!mainHud.activeSelf) mainHud.SetActive(true);
        }
        public void EnterChracterSelect()
        {
            if (UIButtonClickCount.SelectedOnce)
            {
                OnViewCharacterSelectHud();
            }

        }
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit(); 
#endif
        }

        private void OnDestroy()
        {
            enterVillageButton.onClick.RemoveAllListeners();
        }
    }
}
