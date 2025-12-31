using Hunt.Common;
using Hunt.Login;
using Hunt.Net;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 로그인 서버 관련 네트워크 요청/응답을 처리하는 핸들러
    /// NetworkManager를 주입받아 사용 (테스트 및 확장성 향상)
    /// </summary>
    public class LoginService
    {
        private readonly NetworkManager networkManager;
        public static event Action<ErrorType> OnLoginResponse;
        public static event Action<ErrorType> OnCreateAccountResponse;
        public static event Action<ErrorType, bool> OnConfirmIdResponse;
        public static event Action<ErrorType> OnCreateCharResponse;
        public LoginService(NetworkManager networkManager = null)
        {
            this.networkManager = networkManager ?? NetworkManager.Shared;
        }

        /// <summary> 로그인 응답 처리 </summary>
        public static void NotifyLoginResponse(ErrorType t)
        {
            $"[LoginService] 로그인 응답 수신: {t}".DLog();
            if (OnLoginResponse == null) return;
            NotifyLoginResponseAsync(t).Forget();
        }

        private static async UniTaskVoid NotifyLoginResponseAsync(ErrorType t)
        {
            await UniTask.SwitchToMainThread();
            OnLoginResponse?.Invoke(t);
        }

        /// <summary> 계정 생성 응답 처리 </summary>
        public static void NotifyCreateAccountResponse(ErrorType t)
        {
            $"[LoginService] 계정 생성 응답 수신: {t}".DLog();
            if (OnCreateAccountResponse == null)
            {
                $"[LoginService] OnCreateAccountResponse 이벤트 구독자 없음!".DError();
                return;
            }
            NotifyCreateAccountResponseAsync(t).Forget();
        }

        private static async UniTaskVoid NotifyCreateAccountResponseAsync(ErrorType t)
        {
            await UniTask.SwitchToMainThread();
            OnCreateAccountResponse?.Invoke(t);
        }

        /// <summary> 아이디 중복 확인 응답 처리 </summary>
        public static void NotifyConfirmIdResponse(ErrorType t, bool isDup)
        {
            $"[LoginService] 아이디 중복확인 응답 수신: {t}, IsDup: {isDup}".DLog();
            if (OnConfirmIdResponse == null)
            {
                $"[LoginService] OnConfirmIdResponse 이벤트 구독자 없음!".DError();
                return;
            }
            NotifyConfirmIdResponseAsync(t, isDup).Forget();
        }

        private static async UniTaskVoid NotifyConfirmIdResponseAsync(ErrorType t, bool isDup)
        {
            await UniTask.SwitchToMainThread();
            OnConfirmIdResponse?.Invoke(t, isDup);
        }

        /// <summary> 캐릭터 생성 응답 처리 </summary>
        public static void NotifyCreateCharResponse(ErrorType t)
        {
            $"[LoginService] 캐릭터 생성 응답 수신: {t}".DLog();
            if (OnCreateCharResponse == null)
            {
                $"[LoginService] OnCreateCharResponse 이벤트 구독자 없음!".DError();
                return;
            }
            NotifyCreateCharResponseAsync(t).Forget();
        }

        private static async UniTaskVoid NotifyCreateCharResponseAsync(ErrorType t)
        {
            await UniTask.SwitchToMainThread();
            OnCreateCharResponse?.Invoke(t);
        }

        public void ReqAuthVaild(string id, string pw)
        {
            var req = new LoginReq { Id = id, Pw = pw };
            $"[LoginService] 로그인 요청: ID={id} PW={pw}".DLog();
            networkManager.SendToLogin(Hunt.Common.MsgId.LoginReq, req);
        }

        public void ReqCreateAuthVaild(string id, string pw)
        {
            var req = new CreateAccountReq { Id = id, Pw = pw };
            networkManager.SendToLogin(Hunt.Common.MsgId.CreateAccountReq, req);
            $"[LoginService] 계정 생성 요청: ID={id}".DLog();
        }

        public void ReqIdDuplicate(string id)
        {
            var req = new ConfirmIdReq{ Id = id };
           networkManager.SendToLogin(Hunt.Common.MsgId.ConfirmIdReq, req);
            $"[LoginService] 아이디 중복확인 요청: ID={id}".DLog();
        }

        /// <summary>
        /// 캐릭터 생성 시 닉네임 중복 체크
        /// </summary>
        public void ReqNicknameDuplicate(string nickname)
        {
            var req = new ConfirmNameReq{ Name = nickname };
            networkManager.SendToLogin(Hunt.Common.MsgId.ConfirmNameReq, req);
            $"[LoginService] 닉네임 중복확인 요청: Nickname={nickname}".DLog();
        }
        public void ReqCreateChar(string nickname)
        {
            var req = new CreateCharReq { Name = nickname };
            networkManager.SendToLogin(Hunt.Common.MsgId.CreateCharReq, req);
            $"[LoginService] 캐릭터 생성 요청: Nickname={nickname}".DLog();
        }

    }
}
