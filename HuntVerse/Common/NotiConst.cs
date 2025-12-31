using UnityEngine;

namespace Hunt
{
    public enum AUTH_NOTI_TYPE
    {
        SERVER_CON_SUCCESS, SERVER_CON_FAIL,
        DUP_PW, FAIL_INPUT, DUP_ID, ACCOUNT_NOT_EXIST, DUP_LOGIN, DUP_NICK, DUP_REQ,
        SUCCESS_VAILD, SUCCESS_ID_EXIST, SUCCESS_DUP_NICK, SUCCESS_CREATE_ACCOUNT
    }
    public enum NPCNotiType
    {
        None = 0,
        Sell = 1,
        Quest = 2,

    }

    public static class NotiConst
    {
        public static readonly Color COLOR_WARNNING = Hex("CC8E8E");
        public static readonly Color COLOR_SUCCESS = Hex("83DB4E");
        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out var color);
            return color;
        }

        public static string GetAuthNotiMsg(AUTH_NOTI_TYPE type)
        {
            return type switch
            {
                AUTH_NOTI_TYPE.DUP_PW => "비밀번호가 틀렸습니다. 다시 한 번 확인해 주세요.",
                AUTH_NOTI_TYPE.FAIL_INPUT => "특수문자(#, -, 공백)는 입력할 수 없습니다.",
                AUTH_NOTI_TYPE.DUP_ID => "이미 존재 하는 아이디입니다.",
                AUTH_NOTI_TYPE.DUP_LOGIN => "이미 로그인 중입니다.",
                AUTH_NOTI_TYPE.DUP_NICK => "이미 존재 하는 이름입니다.",
                AUTH_NOTI_TYPE.DUP_REQ => "중복 확인이 안되었습니다.",
                AUTH_NOTI_TYPE.SUCCESS_DUP_NICK => "사용가능한 닉네임입니다.",
                AUTH_NOTI_TYPE.ACCOUNT_NOT_EXIST => "해당 계정을 찾을 수 없습니다.",
                AUTH_NOTI_TYPE.SUCCESS_VAILD => "환영합니다, 헌터님.",
                AUTH_NOTI_TYPE.SUCCESS_ID_EXIST => "사용가능한 아이디입니다.",
                AUTH_NOTI_TYPE.SUCCESS_CREATE_ACCOUNT => "계정이 생성되었습니다.",
                AUTH_NOTI_TYPE.SERVER_CON_FAIL => "서버에 접속실패했습니다.",
                AUTH_NOTI_TYPE.SERVER_CON_SUCCESS => "서버에 접속성공했습니다.",
                _ => string.Empty
            };
        }
    }

    public static class NotiNpcConst
    {
        public static string GetIconKeyNpcNotiType(NPCNotiType t)
        {
            return t switch
            {
                NPCNotiType.None => "normal",
                _ => string.Empty
            };
        }
    }
}
