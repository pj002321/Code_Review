using Cysharp.Threading.Tasks;
using Hunt.Login;
using Hunt.Net;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace Hunt
{
    public class GameSession : MonoBehaviourSingleton<GameSession>
    {
        [SerializeField] private string loginServerIp;
        [SerializeField] private int loginServerPort; 
        private UInt64 loginServerKey;
        private NetworkManager networkManager;
        private string gameServerIp;
        private int gameServerPort;
        private UInt64 gameServerKey;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private LoginService loginService;
        public LoginService LoginService => loginService;
        protected override bool DontDestroy => base.DontDestroy;
        #region Life
        protected override void Awake()
        {
            base.Awake();
        }
        private void Start()
        {
            InitializeSession();
        }

        private void InitializeSession()
        {
            networkManager = NetworkManager.Shared;
            if (networkManager == null) return;

            loginService = new LoginService(networkManager);

            isInitialized = true;
            $"[GameSession] Session Initialized".DLog();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region Network Connect

        /// <summary> 로그인서버 연결 </summary>
        public async UniTask<bool> ConnectionToLoginServer()
        {
            if (!isInitialized)
            {
                $"[GameSession] 초기화 대기 중 ...".DLog();
                float elapsed = 0f;
                while (!isInitialized && elapsed < 1f)
                {
                    await UniTask.Delay(10);
                    elapsed += 0.01f;
                }

                if (!isInitialized)
                {
                    "[GameSession] 초기화 실패!".DError();
                    return false;
                }
            }

            if (networkManager == null) return false;
            "[GameSession] 로그인서버 연결 시도".DLog();

            if (networkManager.IsExistConnection(loginServerKey))
            {
                networkManager.StopNet(loginServerKey);
            }
            bool connected = false;
            await UniTask.RunOnThreadPool(() =>
            {
                connected = networkManager.ConnLoginServerSync(
                    (e, msg) => { $"[GameSession] 로그인 서버 연결 끊김 : {msg}".DLog(); },
                    () => { $"[GameSession] 로그인 서버 연결 성공".DLog(); },
                    (e) => { $"[GameSession] 로그인 서버 연결 실패:{e.Message}".DLog(); }
                    );
            });
            if (connected)
            {
                networkManager.StartLoginServer();
            }
            return connected;
        }
        /// <summary> 로그인서버 연결해제 </summary>
        public async UniTask DisConnectionToLoginServer()
        {
            $"[GameSession] 로그인 서버 연결을 해제.".DLog();
            await UniTask.RunOnThreadPool(() =>
            {
                networkManager?.DisConnLoginServer();
            });
        }

        private bool hasGameServerInfo = false;
        public void SetGameServerInfo(LoginAns loginans)
        {
            $"[GameSession] 게임 서버 정보 저장 : {gameServerIp} : {gameServerPort}".DLog();
        }
        /// <summary> 게임서버 연결 </summary>
        public async UniTask<bool> ConnectionToGameServer()
        {
            if (!hasGameServerInfo)
            {
                $"[GameSession] 게임 서버 정보가 설정되지 않음.".DError();
                return false;
            }
            if (networkManager == null)
            {
                $"[GameSession] NetworkManager is null".DError();
                return false;
            }

            $"[GameSession] 게임 서버 연결 시도: {gameServerIp} : {gameServerPort}".DLog();

            if (networkManager.IsExistConnection(gameServerKey))
            {
                $"[GameSession] 기존 게임 서버 연결 해제".DLog();
                networkManager.StopNet(gameServerKey);
            }

            bool connected = false;
            await UniTask.RunOnThreadPool(() =>
            {
                var netModule = networkManager.MakeNetModule(
                    NetModule.ServiceType.Game,
                    (error, msg) => { $"[GameSession] 게임 서버 연결 끊김: {error}, {msg}".DLog(); },
                    () => { $"[GameSession] 게임 서버 연결 성공".DLog(); },
                    (e) => { $"[GameSession] 게임 서버 연결 실패 : {e.Message}".DLog(); }
                );

                connected = netModule.SyncConn(gameServerIp, gameServerPort);

                if (connected)
                {
                    networkManager.InsertNetModule(gameServerKey, netModule);
                }
            });

            return connected;
        }
        /// <summary> 게임서버 연결해제 </summary>
        public async UniTask DisConnectionToGameServer()
        {
            $"[GameSession] 게임 서버 연결 해제".DLog();
            await UniTask.RunOnThreadPool(() =>
            {
                if (networkManager != null && networkManager.IsExistConnection(gameServerKey))
                {
                    networkManager.StopNet(gameServerKey);
                }
            });
        }

        #endregion

        #region Bind
        public List<SimpleCharacterInfo> CharacterInfos { get; protected set; }
        public SimpleCharacterInfo SelectedCharacter { get; protected set; }
        // Login
        public void SetCharacterList(List<SimpleCharacterInfo> characters)
        {
            CharacterInfos = new List<SimpleCharacterInfo>(characters);
            $"[GameSession] 캐릭터 리스트 저장 : {characters.Count}개".DLog();
        }
        public void SelectCharacter(SimpleCharacterInfo character)
        {
            SelectedCharacter = character;
            $"[GameSession] 선택된 캐릭터 : 이름->{character.Name} , 직업->{character.ClassType}".DLog();
        }

        public void SelectCharacterById(ulong charId)
        {
            SelectedCharacter = CharacterInfos?.Find(c => c.CharId == charId);
            if (SelectedCharacter != null)
            {
                $"[GameSession] 캐릭터 선택 : {SelectedCharacter.Name}".DLog();
            }
        }
        #endregion

        #region Dev
        // Dev
        public CharacterModel SelectedCharacterModel { get; protected set; }
        public void SelectCharacterModel(CharacterModel model)
        {
            SelectedCharacterModel = model;
            $"[GameSession] 선택된 캐릭터 (Model): {model.name} (ClassType: {model.classtype})".DLog();
        }
        #endregion

    }
}
