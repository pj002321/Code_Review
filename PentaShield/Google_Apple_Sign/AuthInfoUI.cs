using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using chaos;
using UnityEngine.UI;

namespace PentaShield
{
    /// <summary>
    /// 인증 정보 UI 관리
    /// - 사용자 ID/이름 표시
    /// - 국가 이미지 표시
    /// - UserData 변경 시 자동 업데이트
    /// </summary>
    public class AuthInfoUI : MonoBehaviour
    {
        public TextMeshProUGUI userIdText = null;
        public TextMeshProUGUI userNameText = null;
        public Image userNationImg;

        /// <summary> 사용자 ID 텍스트 업데이트 </summary>
        public async UniTask UpdateIdText(string idText = "")
        {
            if (PentaFirebase.Shared?.PAuth?.IsInitialized != true ||
                UserDataManager.Shared == null ||
                UserDataManager.Shared.Data == null)
            {
                if (userIdText != null)
                {
                    userIdText.text = "Connecting...";
                }
            }

            if (string.IsNullOrEmpty(idText))
            {
                await UniTask.WaitUntil(() =>
                    PentaFirebase.Shared?.PAuth?.IsInitialized == true &&
                    UserDataManager.Shared != null &&
                    UserDataManager.Shared.Data != null);

                idText = UserDataManager.Shared.Data.Id;
            }

            if (userIdText != null)
            {
                userIdText.text = $"ID : {idText}";
            }
        }

        /// <summary> 국가 이미지 업데이트 </summary>
        public async UniTask UpdateUserNationImage(string nation)
        {
            string key = nation switch
            {
                "KOR" => PentaConst.KSIconKorea,
                "USA" => PentaConst.KSIconEng,
                "JAP" => PentaConst.KSIconJap,
                "CHI" => PentaConst.KSIconChi,
                _ => PentaConst.KSIconKorea
            };

            userNationImg.sprite = await AbHelper.Shared.LoadAssetAsync<Sprite>(key);
        }

        /// <summary> 사용자 이름 텍스트 업데이트 </summary>
        public void UpdateUserNameText(string nameText)
        {
            if (userNameText == null) return;

            string displayName = string.IsNullOrWhiteSpace(nameText) ? "Unknown" : nameText;
            userNameText.text = $"Name : {displayName}";
        }

        /// <summary> 모든 텍스트 업데이트 </summary>
        public void UpdateAllText(string idText, string userNameText)
        {
            _ = UpdateIdText(idText);
            UpdateUserNameText(userNameText);
        }

        /// <summary> UserData 변경 시 UI 업데이트 </summary>
        private void HandleUserDataUpdated(UserData userData)
        {
            if (userData == null) return;

            UpdateUserNameText(userData.Name);
            _ = UpdateIdText(userData.Id);

            string nationCode = userData.Region != null
                ? RegionConstHelper.GetNationCode(userData.Region)
                : RegionConstHelper.GetNationCode(Application.systemLanguage);
            _ = UpdateUserNationImage(nationCode);
        }
    }
}
