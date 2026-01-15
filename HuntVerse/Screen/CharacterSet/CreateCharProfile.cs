using Cysharp.Threading.Tasks;
using Hunt.Common;
using Hunt.Game;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class CreateCharProfile : MonoBehaviour
    {
        [Header("CREATWINDOW")]
        [SerializeField] private GameObject createWindow;
        [SerializeField] private Button createButton;
        [SerializeField] private Button dupNickButton;
        [SerializeField] private TextMeshProUGUI createcharVaildText;
        [SerializeField] private TMP_InputField nickNameField;

        [Header("SETUP")]
        private ClassType classType;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        public Image PortraitImage => portraitImage;
        public ClassType ProfessionType => classType;
        public string characterName => BindKeyConst.GetProfessionMatchName(classType);
        public List<StatInfo> stats = new List<StatInfo>();
        public string charStory;

        private bool isNicknameChecked = false;
        private string checkedNickname = string.Empty;

        /// <summary>
        /// professionType이 변경될 때 UI를 업데이트합니다.
        /// </summary>
        public void UpdateCharacterNameUI()
        {
            if (characterNameText != null)
            {
                characterNameText.text = ConvertToVerticalText(characterName);
            }
        }

        /// <summary>
        /// 텍스트를 세로로 표시하기 위해 각 글자 사이에 줄바꿈을 추가합니다.
        /// </summary>
        private string ConvertToVerticalText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            return string.Join("\n", text.ToCharArray());
        }

        public void SetProfession(ClassType profession)
        {
            classType = profession;
        }
        private void OnEnable()
        {
            dupNickButton.onClick.AddListener(() => ReqNickNameDuplicate());
            createButton.onClick.AddListener(() => ReqCreateChar());
            nickNameField.onValueChanged.AddListener(OnNicknameChanged);
            LoginService.OnConfirmNameResponse += HandleNotiConfirmNameResponse;
            LoginService.OnCreateCharResponse += HandleNotiCreateCharResponse;
            createWindow.SetActive(false);
        }
        private void OnDisable()
        {
            dupNickButton.onClick.RemoveListener(() => ReqNickNameDuplicate());
            createButton.onClick.RemoveListener(() => ReqCreateChar());
            nickNameField.onValueChanged.RemoveListener(OnNicknameChanged);
            LoginService.OnConfirmNameResponse -= HandleNotiConfirmNameResponse;
            LoginService.OnCreateCharResponse -= HandleNotiCreateCharResponse;
            
        }
        public void OnClickCreateCharacter()
        {
            CharacterSetupController.Shared.OnCreateNewCharacter(this.classType);
        }

        private void OnNicknameChanged(string newNickname)
        {
            if (checkedNickname != newNickname)
            {
                isNicknameChecked = false;
                checkedNickname = string.Empty;
            }
        }

        /// <summary> 닉네임 중복 체크 요청 </summary>
        private void ReqNickNameDuplicate()
        {
            var nickName = nickNameField.text;
            if (!IsValid(nickName, createcharVaildText))
            {
                return;
            }
            GameSession.Shared?.LoginService.ReqNicknameDuplicate(nickName);
        }
        // CreateCharProfile.cs 수정
        private void ReqCreateChar()
        {
            var nickName = nickNameField.text;

            if (!IsValid(nickName, createcharVaildText))
            {
                return;
            }

            if (!isNicknameChecked || checkedNickname != nickName)
            {
                ShowNotificationText(
                    createcharVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_REQ),
                    NotiConst.COLOR_WARNNING);
                return;
            }

            uint worldId = GetCurrentWorldId();
            uint classType = (uint)BindKeyConst.GetJobIdByClassType(this.classType);
            
            $"[CreateCharProfile] 캐릭터 생성 요청 - Nickname: {nickName}, WorldId: {worldId}, ClassType: {classType}".DLog();

            if (worldId == 0)
            {
                ShowNotificationText(
                    createcharVaildText,
                    "월드를 먼저 선택해주세요!",
                    NotiConst.COLOR_WARNNING);
                return;
            }

            GameSession.Shared?.LoginService.ReqCreateChar(nickName, worldId, classType);
        }
        private uint GetCurrentWorldId()
        {
            if (GameSession.Shared == null)
            {
                this.DError("❌ GameSession.Shared가 null입니다!");
                return 1; // 임시: 그라시아
            }
            
            uint worldId = GameSession.Shared.CurrentSelectedWorldId;
            
            $"[CreateCharProfile] 현재 worldId: {worldId}".DLog();

            if (worldId == 0)
            {
                this.DError("⚠️ 선택된 월드가 없습니다! 먼저 월드를 클릭해주세요.");
                return 1; // 임시: 그라시아
            }

            return worldId;
        }
        private bool IsValid(string nickName, TextMeshProUGUI vaildText)
        {
            char[] invalidChars = { '-', '#', ' ' };

            bool isValid = !nickName.IsNullOrEmpty() && nickName.IndexOfAny(invalidChars) == -1;

            vaildText.gameObject.SetActive(true);

            if (!isValid)
            {
                ShowNotificationText(
                           vaildText,
                           NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_INPUT),
                           NotiConst.COLOR_WARNNING);
                $"Field Value Is Valid {false}".DError();
                return false;
            }

            return isValid;
        }

        private void HandleNotiConfirmNameResponse(ErrorType t, bool isDup)
        {
            if (t != Common.ErrorType.ErrNon)
            {
                ShowNotificationText(
                    createcharVaildText,
                    "서버 오류가 발생했습니다.",
                    NotiConst.COLOR_WARNNING);
                isNicknameChecked = false;
                checkedNickname = string.Empty;
                return;
            }

            if (isDup)
            {
                ShowNotificationText(
                    createcharVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_NICK),
                    NotiConst.COLOR_WARNNING);
                isNicknameChecked = false;
                checkedNickname = string.Empty;
            }
            else
            {
                ShowNotificationText(
                    createcharVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_DUP_NICK),
                    NotiConst.COLOR_SUCCESS);
                isNicknameChecked = true;
                checkedNickname = nickNameField.text;
            }
        }

        private void HandleNotiCreateCharResponse(ErrorType t, Hunt.Login.SimpleCharacterInfo charInfo)
        {
            if (t == Common.ErrorType.ErrNon)
            {
                if (charInfo != null)
                {
                    GameSession.Shared?.AddCharacterInfo(charInfo);
                    var charModel = CharModel.FromCharacterInfo(charInfo);
                    CharacterSetupController.Shared?.OnRecvNewCharacter(charModel);
                }
                
                isNicknameChecked = false;
                checkedNickname = string.Empty;
                nickNameField.text = string.Empty;
            }
            else
            {
                ShowNotificationText(
                    createcharVaildText,
                    "캐릭터 생성에 실패했습니다.",
                    NotiConst.COLOR_WARNNING);
                $"캐릭터 생성 실패: {t}".DError();
            }
        }
        #region Effect
        private Coroutine currentFadeCoroutine;
        private void ShowNotificationText(TextMeshProUGUI textUI, string message, Color color)
        {
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);

            currentFadeCoroutine = StartCoroutine(CO_FadeText(textUI, message, color));
        }

        private IEnumerator CO_FadeText(TextMeshProUGUI textUI, string message, Color color)
        {
            textUI.text = message;
            textUI.color = color;
            textUI.gameObject.SetActive(true);

            // Fade In
            float a = 0f;
            while (a < 1f)
            {
                a += Time.deltaTime * 3f;
                textUI.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            yield return new WaitForSeconds(2f);

            while (a > 0f)
            {
                a -= Time.deltaTime * 3f;
                textUI.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            textUI.gameObject.SetActive(false);
        }
        #endregion
    }
}
