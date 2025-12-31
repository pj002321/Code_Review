using Cysharp.Threading.Tasks;
using Hunt.Common;
using Hunt.Login;
using Hunt.Net;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hunt
{
    public class LoginScreen : MonoBehaviour
    {
        #region Field
        [SerializeField] private TMP_InputField org_idInput, org_pwInput;
        [SerializeField] private TMP_InputField new_idInput, new_pwInput, new_pwDupInput;

        [Header("LOGIN")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button pwVisButton;
        [SerializeField] private GameObject caplockVis;
        [SerializeField] private TextMeshProUGUI loginVaildText;

        [Header("CREATE")]
        [SerializeField] private Button createConfirmButton;  
        [SerializeField] private GameObject createPanel;
        [SerializeField] private TextMeshProUGUI createVaildText;
        [SerializeField] private GameObject createCaplockVis;
        [SerializeField] private Button new_pwVisButton;
        [SerializeField] private Button id_DupButton;

        [Header("ANIMATION")]
        [SerializeField] private Animator animator;

        private class InputContext
        {
            public TMP_InputField IdField;
            public TMP_InputField PwField;
            public TextMeshProUGUI VaildText;
            public GameObject Capslock;
        }

        private InputContext GetCurrentContext()
        {
            if (createPanel.activeSelf)
            {
                return new InputContext
                {
                    IdField = new_idInput,
                    PwField = new_pwInput,
                    VaildText = createVaildText,
                    Capslock = createCaplockVis
                };
            }

            return new InputContext
            {
                IdField = org_idInput,
                PwField = org_pwInput,
                VaildText = loginVaildText,
                Capslock = caplockVis
            };
        }

        private bool isPasswordVisible = false;
        private bool isCreatePasswordVisible = false;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);
        private const int VK_CAPITAL = 0x14;
        #endregion
        #region Life
        private void OnEnable()
        {
            LoginService.OnLoginResponse += HandleNotiLoginResponse;
            LoginService.OnCreateAccountResponse += HandleNotiCreateAccountResponse;
            LoginService.OnConfirmIdResponse += HandleNotiConfirmIdResponse;
        }

        private void Start()
        {

            confirmButton.onClick.AddListener(ReqAuthVaild);
            pwVisButton.onClick.AddListener(() => TogglePasswordVisibility(false));

            createConfirmButton.onClick.AddListener(ReqCreateAuthVaild);
            new_pwVisButton.onClick.AddListener(() => TogglePasswordVisibility(true));

            id_DupButton.onClick.AddListener(ReqIdDuplicate);

            org_idInput.onSubmit.AddListener(OnIdSubmit);
            org_pwInput.onSubmit.AddListener(OnPwSubmit);

            new_idInput.onSubmit.AddListener(OnIdSubmit);
            new_pwInput.onSubmit.AddListener(OnPwSubmit);

            org_idInput.Select();
            org_idInput.ActivateInputField();

            createVaildText.text = "";
            loginVaildText.text = "";
            org_pwInput.contentType = TMP_InputField.ContentType.Password;
            new_pwInput.contentType = TMP_InputField.ContentType.Password;
            new_pwDupInput.contentType = TMP_InputField.ContentType.Password;

            if (!SystemBoot.Shared.LoginServerConnected)
            {
                ShowNotificationText(loginVaildText, NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SERVER_CON_FAIL), NotiConst.COLOR_WARNNING );
            }
            else
            {
                ShowNotificationText(loginVaildText, NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SERVER_CON_SUCCESS), NotiConst.COLOR_SUCCESS);
            }
        }

        private void Update()
        {
            HandleKeyInput();
        }
        private void OnDisable()
        {
            LoginService.OnLoginResponse -= HandleNotiLoginResponse;
            LoginService.OnCreateAccountResponse -= HandleNotiCreateAccountResponse;
            LoginService.OnConfirmIdResponse -= HandleNotiConfirmIdResponse;
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(ReqAuthVaild);
            createConfirmButton.onClick.RemoveListener(ReqCreateAuthVaild);
            id_DupButton.onClick.RemoveListener(ReqIdDuplicate);
            org_idInput.onSubmit.RemoveListener(OnIdSubmit);
            org_pwInput.onSubmit.RemoveListener(OnPwSubmit);
            new_idInput.onSubmit.RemoveListener(OnIdSubmit);
            new_pwInput.onSubmit.RemoveListener(OnPwSubmit);
            LoginService.OnLoginResponse -= HandleNotiLoginResponse;
            LoginService.OnCreateAccountResponse -= HandleNotiCreateAccountResponse;
            LoginService.OnConfirmIdResponse -= HandleNotiConfirmIdResponse;
        }
        #endregion
        #region INPUT

        private void HandleKeyInput()
        {
            var key = Keyboard.current;
            if (key == null) return;

            var context = GetCurrentContext();
            //context.VaildText?.gameObject.SetActive(false);

            if (key.tabKey.wasPressedThisFrame && context.IdField.isFocused)
            {
                context.PwField?.Select();
                context.PwField?.ActivateInputField();
            }

            bool isCapsLockOn = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
            context.Capslock?.SetActive(isCapsLockOn);
        }

        private void TogglePasswordVisibility(bool isCreatePanel)
        {
            if (isCreatePanel)
            {
                isCreatePasswordVisible = !isCreatePasswordVisible;
                new_pwInput.contentType = isCreatePasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                new_pwDupInput.contentType = new_pwInput.contentType;

                new_pwInput.ForceLabelUpdate();
                new_pwDupInput.ForceLabelUpdate();
            }
            else
            {
                isPasswordVisible = !isPasswordVisible;
                org_pwInput.contentType = isPasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                org_pwInput.ForceLabelUpdate();
            }
        }
        private void OnIdSubmit(string _)
        {
            var context = GetCurrentContext();
            context.PwField.Select();
            context.PwField.ActivateInputField();
        }
        private void OnPwSubmit(string _)
        {
            if (createPanel.activeSelf)
            {
                ReqCreateAuthVaild();
            }
            else
            {
                ReqAuthVaild();
            }
        }
        #endregion
        #region REQUEST

        /// <summary> Request Server : Duplicate ID </summary>
        private void ReqIdDuplicate()
        {
            var id = new_idInput.text;
            if (string.IsNullOrEmpty(id))
            {
                ShowNotificationText(
                    createVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_INPUT),
                    NotiConst.COLOR_WARNNING
                );
                return;
            }

            $"[LogInScreen] 아이디 중복확인 요청 시도: ID={id}".DLog();
            GameSession.Shared?.LoginService.ReqIdDuplicate(id);
        }

        /// <summary> Request Server : Vaild Auth </summary>
        private void ReqAuthVaild()
        {
            var (id, pw) = VaildateAndReturnResult(org_idInput, org_pwInput, loginVaildText);
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw)) return;
            
            $"[LogInScreen] 로그인 요청 시도: ID={id}".DLog();
            GameSession.Shared?.LoginService.ReqAuthVaild(id, pw);
        }

        /// <summary> Request Server : Create Auth </summary>
        private void ReqCreateAuthVaild()
        {
            var (id, pw) = VaildateAndReturnResult(new_idInput, new_pwInput, createVaildText);
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                return;
            }

            if (!IsVaildSyncPassWord())
            {
                $"[LogInScreen] 비밀번호가 일치하지 않습니다.".DLog();
                return;
            }

            $"[LogInScreen] 계정 생성 요청 시도: ID={id}".DLog();
            GameSession.Shared?.LoginService.ReqCreateAuthVaild(id, pw);
        }
        /// <summary> 로그인 응답 처리 </summary>
        private void HandleNotiLoginResponse(ErrorType t)
        {
            
            switch (t)
            {
                case Common.ErrorType.ErrNon:
                    animator.SetBool(AniKeyConst.k_bValid, true);
                    ShowNotificationText(
                    loginVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_VAILD),
                    NotiConst.COLOR_SUCCESS);
                    $"[LogInScreen] HandleNotiLoginResponse 로그인 성공: {t}".DLog();
                    
                    SceneLoadHelper.Shared?.LoadSceneSingleMode(ResourceKeyConst.Ks_Mainmenu, false);

                    break;
                case Common.ErrorType.ErrAccountNotExist:
                    animator.SetTrigger(AniKeyConst.k_tFail);
                    animator.SetBool(AniKeyConst.k_bValid, false);
                    ShowNotificationText(
                    loginVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.ACCOUNT_NOT_EXIST),
                    NotiConst.COLOR_WARNNING);
                   
                    $"[LogInScreen] HandleNotiLoginResponse 계정 정보 존재 하지 않음: {t}".DError();
                    break;
                case Common.ErrorType.ErrDupLogin:
                    animator.SetTrigger(AniKeyConst.k_tFail);
                    animator.SetBool(AniKeyConst.k_bValid, false);
                    ShowNotificationText(
                    loginVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_LOGIN),
                    NotiConst.COLOR_WARNNING);
                    break;
            }
        }

        /// <summary> 계정 생성 응답 처리 </summary>
        private void HandleNotiCreateAccountResponse(ErrorType t)
        {
            $"[LogInScreen] HandleNotiCreateAccountResponse 호출: {t}".DLog();
            switch (t)
            {
                case Common.ErrorType.ErrNon:
                    ShowNotificationText(
                    createVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_CREATE_ACCOUNT),
                    NotiConst.COLOR_SUCCESS);
                    break;
                case Common.ErrorType.ErrDupId:
                    ShowNotificationText(
                    createVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_ID),
                    NotiConst.COLOR_WARNNING);
                    break;
            }
        }

        /// <summary> 아이디 중복확인 응답 처리 </summary>
        private void HandleNotiConfirmIdResponse(ErrorType t, bool isDup)
        {
            $"[LogInScreen] HandleNotiConfirmIdResponse 호출: {t}, IsDup: {isDup}".DLog();
            if (t == Common.ErrorType.ErrNon)
            {
                if (isDup)
                {
                    ShowNotificationText(
                    createVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_ID),
                    NotiConst.COLOR_WARNNING);
                }
                else
                {
                    ShowNotificationText(
                    createVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_ID_EXIST),
                    NotiConst.COLOR_SUCCESS);
                }
            }
        }

        #endregion
        #region VAILD
        private (string, string) VaildateAndReturnResult(
            TMP_InputField idField,
            TMP_InputField pwField,
            TextMeshProUGUI resultText )
        {
            if (!IsValid(idField.text, pwField.text, resultText))
            {
                return default;
            }

            return (idField.text, pwField.text);
        }

        private bool IsValid(string id, string pw, TextMeshProUGUI vaildText)
        {
            char[] invalidChars = { '-', '#', ' ' };

            bool isValid = !id.IsNullOrEmpty()
                           && !pw.IsNullOrEmpty()
                           && id.IndexOfAny(invalidChars) == -1
                           && pw.IndexOfAny(invalidChars) == -1;

            vaildText.gameObject.SetActive(true);

            if (isValid)
            {
                vaildText.color = NotiConst.COLOR_SUCCESS;
               
                $"Field Value Is Valid {true}".DLog();
            }
            else
            {
                ShowNotificationText(
                vaildText,
                NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_INPUT),
                NotiConst.COLOR_WARNNING);
                animator.SetTrigger(AniKeyConst.k_tFail);
                $"Field Value Is Valid {false}".DError();
            }
            animator.SetBool(AniKeyConst.k_bValid, isValid);
            return isValid;
        }

        private bool IsVaildSyncPassWord()
        {
            var vaild = new_pwInput.text == new_pwDupInput.text ? true : false;
            if (!vaild)
                ShowNotificationText(
                createVaildText,
                NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_PW),
                NotiConst.COLOR_WARNNING);

            return vaild;
        }

        #endregion
        #region Effect
        private Coroutine currentFadeCoroutine;
        private void ShowNotificationText(TextMeshProUGUI textUI, string message, Color color)
        {
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            $"[LoginScreen] Noti Msg : {message}".DLog();
            currentFadeCoroutine = StartCoroutine(UIEffect.CO_FadeText(textUI, message, color));
        }

        #endregion
    }
}