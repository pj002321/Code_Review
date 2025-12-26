using Cysharp.Threading.Tasks;
using PentaShield;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace chaos
{
    /// <summary>
    /// 이름 수정 버튼
    /// - 사용자 이름 표시 및 수정
    /// - UserData 변경 시 자동 업데이트
    /// </summary>
    public class NameEditButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button editButton;
        [SerializeField] private TextMeshProUGUI nameDisplayText;
        [SerializeField] private NameEditPanel nameEditPanel;

        [Header("Settings")]
        [SerializeField] private bool autoUpdateDisplay = true;

        private void Awake()
        {
            editButton.onClick.AddListener(OnEditButtonClicked);
        }

        /// <summary> 현재 이름 표시 </summary>
        public void UpdateNameDisplay()
        {
            if (nameDisplayText == null) return;

            if (UserDataManager.Shared != null && UserDataManager.Shared.Data != null)
            {
                string currentName = UserDataManager.Shared.Data.Name;
                nameDisplayText.text = string.IsNullOrWhiteSpace(currentName) ? "이름 없음" : currentName;
            }
            else
            {
                nameDisplayText.text = "Loading...";
            }
        }

        /// <summary> 이름 수정 버튼 클릭 처리 </summary>
        private void OnEditButtonClicked()
        {
            if (nameEditPanel == null)
            {
                $"[NameEditButton] NameEditPanel is not assigned".DError();
                return;
            }

            if (UserDataManager.Shared == null || UserDataManager.Shared.Data == null)
            {
                $"[NameEditButton] UserDataManager not ready".DError();
                return;
            }

            string currentName = UserDataManager.Shared.Data.Name;
            nameEditPanel.Show(currentName, OnNameChanged, OnEditCancelled);
        }

        private void OnNameChanged(string newName)
        {
            UpdateNameDisplay();
        }

        private void OnEditCancelled()
        {
        }
    }
}
