using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.ComponentModel.Design;

namespace Hunt
{
    public class CharInfoField : MonoBehaviour
    {
        private bool isCreated = false;
        private CharModel currentModel;

        [SerializeField] private GameObject createPannel;
        [SerializeField] private GameObject userInfoPannel;

        [SerializeField] private Image professionIcon;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI savePointText;
        [SerializeField] private Button selectButton;
        private Animator animator;
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClickField);
            }
        }

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnClickField);
            }
        }
        public ulong Level
        {
            get => ulong.Parse(levelText.text);
            set => levelText.text = value.ToString();

        }
        public string Name
        {
            get => nameText.text;
            set => nameText.text = value;
        }
        public string SavePoint
        {
            get => savePointText.text;
            set => savePointText.text = value;
        }

        /// <summary>
        /// 현재 필드에 바인딩된 캐릭터 모델을 반환합니다.
        /// </summary>
        public CharModel CurrentModel => currentModel;

        /// <summary>
        /// 현재 필드에 캐릭터 데이터가 존재하는지 확인합니다.
        /// </summary>
        public bool HasCharacterData => currentModel != null && currentModel.IsCreated;

        /// <summary>
        /// 현재 필드에 직업 아이콘이 로드되어 있는지 확인합니다.
        /// </summary>
        public bool HasProfessionIcon => professionIcon != null && professionIcon.enabled && professionIcon.sprite != null;

        public void InitField(bool iscreated)
        {
            isCreated = iscreated;

            createPannel.SetActive(!isCreated);
            userInfoPannel.SetActive(isCreated);
        }


        public void SetLevelFieldValue(ulong level) => Level = level;
        public void SetNameFieldValue(string name) => Name = name;
        public void SetSavePointFieldValie(string savepoint) => SavePoint = savepoint;
        public async void Bind(CharModel model)
        {
            currentModel = model;
            var created = model?.IsCreated == true;
            InitField(created);

            if (!created)
            {
                SetLevelFieldValue(0);
                SetNameFieldValue(string.Empty);
                SetSavePointFieldValie(string.Empty);
                if (professionIcon != null)
                {
                    professionIcon.sprite = null;
                    professionIcon.enabled = false;
                }
                return;
            }

            SetLevelFieldValue(model.level);
            SetNameFieldValue(model.name);
            SetSavePointFieldValie(BindKeyConst.GetMapNameByMapId(model.mapId));

            if (professionIcon != null)
            {
                await LoadProfessionIcon(model.classtype);
            }
        }

        private async UniTask LoadProfessionIcon(ClassType profession)
        {
            if (AbLoader.Shared == null)
            {
                "🖼️ [CharacterInfoField] AbLoader.Shared is null".DError();
                return;
            }

            string iconKey = BindKeyConst.GetIconKeyByProfession(profession);
            if (string.IsNullOrEmpty(iconKey))
            {
                professionIcon.enabled = false;
                return;
            }

            try
            {
                var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(iconKey);
                if (sprite != null)
                {
                    professionIcon.sprite = sprite;
                    professionIcon.enabled = true;
                    $"🖼️ [CharacterInfoField] Icon loaded: {iconKey}".DLog();
                }
                else
                {
                    professionIcon.enabled = false;
                    $"🖼️ [CharacterInfoField] Failed to load icon: {iconKey}".DError();
                }
            }
            catch (System.Exception ex)
            {
                $"🖼️ [CharacterInfoField] Error loading icon: {ex.Message}".DError();
                professionIcon.enabled = false;
            }
        }

        public void OnClickField()
        {
            CharacterSetupController.Shared.OnSelectCharacterField(this);
        }
        public void HightlightField(bool active)
        {
            if (animator == null) return;
            animator.SetBool(AniKeyConst.k_bSelect, active);
        }


    }
}