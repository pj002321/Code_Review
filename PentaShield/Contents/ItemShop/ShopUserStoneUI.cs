using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// 상점 화면의 Stone 재화 UI (주요 로직)
    /// - Stone 재화 표시 및 업데이트
    /// </summary>
    public class ShopUserStoneUI : ShopGameMoneyUIBase
    {
        [SerializeField] private TextMeshProUGUI textComponent;

        private void Awake()
        {
            Func<int> getter = () => UserDataManager.Shared.Data.Stone;
            Action<int> setter = (setValue) => { UserDataManager.Shared.Data.Stone += setValue; };

            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }
            Initalize(getter, setter, textComponent);
        }

        private void OnDestroy()
        {
            if (UserDataManager.Shared != null)
            {
                UserDataManager.Shared.OnDataUpdated -= OnDataChanged;
            }
        }

        /// <summary> UI 초기화 및 이벤트 구독 </summary>
        public void Initialize()
        {
            UpdateText().Forget();
            UserDataManager.Shared.OnDataUpdated += OnDataChanged;
        }

        private void OnDataChanged(UserData data)
        {
            UpdateText().Forget();
        }
    }
}
