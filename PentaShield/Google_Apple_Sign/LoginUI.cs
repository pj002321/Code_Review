using chaos;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

namespace PentaShield
{
    /// <summary>
    /// 로그인 UI 관리
    /// - Google/Apple 로그인 처리
    /// - 로그아웃 처리
    /// - 계정 삭제 처리
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        public AuthInfoUI authInfoUI = null;
        public Button googleLoginBtn = null;
        public Button appleLoginBtn = null;
        public Button logoutBtn = null;
        public Button accountBtn = null;

        [Header("Name Edit")]
        [SerializeField] private NameEditPanel nameEditPanel;

        [Header("Noti")]
        [SerializeField] private GameObject signedNoti;
        [SerializeField] private GameObject logoutNoti;
        [SerializeField] private GameObject accountNoti;
        [SerializeField] private GameObject logoutOverlay;

        private void Awake()
        {
            googleLoginBtn.onClick.AddListener(() => _ = LogIn("Google"));
            appleLoginBtn.onClick.AddListener(() => _ = LogIn("Apple"));
            logoutBtn.onClick.AddListener(LogOut);
            accountBtn.onClick.AddListener(AccountDelete);
        }

        /// <summary> Google/Apple 로그인 처리 </summary>
        private async UniTask LogIn(string provider)
        {
            await UniTask.WaitUntil(() =>
                PentaFirebase.Shared != null &&
                PentaFirebase.Shared.PAuth != null &&
                PentaFirebase.Shared.PAuth.IsInitialized);

            if (PentaFirebase.Shared.PAuth.IsLoggedIn && 
                PentaFirebase.Shared.PAuth.CurrentUser != null &&
                !PentaFirebase.Shared.PAuth.CurrentUser.IsAnonymous)
            {
                await UniTask.WaitUntil(() =>
                    UserDataManager.Shared != null &&
                    UserDataManager.Shared.IsInitialized);

                UserDataManager.Shared.ClearData();

                if (authInfoUI != null)
                {
                    await authInfoUI.UpdateIdText("");
                    authInfoUI.UpdateUserNameText("");
                }
            }

            FirebaseUser user = null;

            if (provider == "Google")
            {
                user = await PentaFirebase.Shared.PAuth.SignInWithGoogleAsync();
            }
            else if (provider == "Apple")
            {
                user = await PentaFirebase.Shared.PAuth.SignInWithAppleAsync();
            }

            if (user == null)
            {
                $"[LoginUI] {provider} sign-in failed or was cancelled".DError();
                return;
            }

            _ = ShowNotification(signedNoti);
            UpdateLogoutOverlay();

            await UniTask.WaitUntil(() =>
                UserDataManager.Shared != null &&
                UserDataManager.Shared.IsInitialized);

            bool isSuccess = await UserDataManager.Shared.SyncWithFirebase(user);

            if (authInfoUI != null && isSuccess)
            {
                string nationCode = RegionConstHelper.GetNationCode(Application.systemLanguage);
                await authInfoUI.UpdateUserNationImage(nationCode);
            }
        }

        /// <summary> 로그아웃 처리 </summary>
        private void LogOut()
        {
            PentaFirebase.Shared.PAuth.SignOut();
            if (UserDataManager.Shared != null)
            {
                UserDataManager.Shared.ClearData();
            }
            _ = ShowNotification(logoutNoti);
            UpdateLogoutOverlay();
        }

        /// <summary> 계정 삭제 처리 </summary>
        private async void AccountDelete()
        {
            if (PentaFirebase.Shared?.PAuth == null || !PentaFirebase.Shared.PAuth.IsLoggedIn)
            {
                $"[LoginUI] No user is logged in".DWarnning();
                return;
            }

            var currentUser = PentaFirebase.Shared.PAuth.CurrentUser;
            if (currentUser == null || currentUser.IsAnonymous)
            {
                $"[LoginUI] Cannot delete anonymous or null user".DWarnning();
                return;
            }

            string userId = currentUser.UserId;

            await UniTask.WaitUntil(() =>
                UserDataManager.Shared != null &&
                UserDataManager.Shared.IsInitialized);

            bool dataDeleted = await UserDataManager.Shared.DeleteUserAccount(userId);
            if (!dataDeleted)
            {
                $"[LoginUI] Failed to delete user data from Firestore".DError();
            }

            bool authDeleted = await PentaFirebase.Shared.PAuth.AccountDelete();
            if (authDeleted)
            {
                _ = ShowNotification(accountNoti);
                UpdateLogoutOverlay();
            }
            else
            {
                $"[LoginUI] Failed to delete account from Firebase Auth".DError();
            }
        }

        /// <summary> 알림 표시 </summary>
        private async UniTask ShowNotification(GameObject notificationObject)
        {
            if (notificationObject == null) return;

            CanvasGroup canvasGroup = notificationObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notificationObject.AddComponent<CanvasGroup>();
            }

            notificationObject.SetActive(true);
            canvasGroup.alpha = 1f;

            await UniTask.Delay(2000);

            const float fadeDuration = 0.5f;
            var elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsedTime / fadeDuration);
                await UniTask.Yield();
            }

            canvasGroup.alpha = 0f;
            notificationObject.SetActive(false);
        }

        /// <summary> 로그아웃 오버레이 업데이트 </summary>
        private void UpdateLogoutOverlay()
        {
            if (logoutOverlay == null) return;

            bool isLoggedIn = PentaFirebase.Shared?.PAuth != null && 
                              PentaFirebase.Shared.PAuth.IsLoggedIn &&
                              PentaFirebase.Shared.PAuth.CurrentUser != null &&
                              !PentaFirebase.Shared.PAuth.CurrentUser.IsAnonymous;

            logoutOverlay.SetActive(!isLoggedIn);
        }
    }
}
