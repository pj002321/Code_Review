using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    /// <summary>
    /// 직업 선택 버튼 컴포넌트입니다.
    /// 각 직업 버튼에 이 컴포넌트를 붙이고 Inspector에서 직업을 설정합니다.
    /// </summary>
    public class CreateCharPreviewSlot : MonoBehaviour
    {
        [SerializeField] private ClassType professionType;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        private void OnButtonClicked()
        {
            if (CharacterSetupController.Shared != null)
            {
                CharacterSetupController.Shared.OnShowCharInfo(professionType);
            }
        }
    }
}

