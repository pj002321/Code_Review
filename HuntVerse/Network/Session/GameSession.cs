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
        [SerializeField] private string loginServerIp = "127.0.0.1";
        [SerializeField] private int loginServerPort = 9000;
        private UInt64 loginServerKey;

        private NetworkManager networkManager;
        private string gameServerIp;
        private int gameServerPort;
        private UInt64 gameServerKey;
        public uint CurrentSelectedWorldId { get; private set; }

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
            this.DLog("Session Initialized");
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
                this.DLog("초기화 대기 중 ...");
                float elapsed = 0f;
                while (!isInitialized && elapsed < 1f)
                {
                    await UniTask.Delay(10);
                    elapsed += 0.01f;
                }

                if (!isInitialized)
                {
                    this.DError("초기화 실패!");
                    return false;
                }
            }

            if (networkManager == null) return false;
            this.DLog("로그인서버 연결 시도");

            if (networkManager.IsExistConnection(loginServerKey))
            {
                networkManager.StopNet(loginServerKey);
            }
            bool connected = false;
            await UniTask.RunOnThreadPool(() =>
            {
                connected = networkManager.ConnLoginServerSync(
                    (e, msg) => { this.DLog($"로그인 서버 연결 끊김 : {msg}"); },
                    () => { this.DLog("로그인 서버 연결 성공"); },
                    (e) => { this.DLog($"로그인 서버 연결 실패:{e.Message}"); }
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
            this.DLog("로그인 서버 연결을 해제.");
            await UniTask.RunOnThreadPool(() =>
            {
                networkManager?.DisConnLoginServer();
            });
        }

        private bool hasGameServerInfo = false;
        public void SetGameServerInfo(LoginAns loginans)
        {
            this.DLog($"게임 서버 정보 저장 : {gameServerIp} : {gameServerPort}");
        }
        /// <summary> 게임서버 연결 </summary>
        public async UniTask<bool> ConnectionToGameServer()
        {
            if (!hasGameServerInfo)
            {
                this.DError("게임 서버 정보가 설정되지 않음.");
                return false;
            }
            if (networkManager == null)
            {
                this.DError("NetworkManager is null");
                return false;
            }

            this.DLog($"게임 서버 연결 시도: {gameServerIp} : {gameServerPort}");

            if (networkManager.IsExistConnection(gameServerKey))
            {
                this.DLog("기존 게임 서버 연결 해제");
                networkManager.StopNet(gameServerKey);
            }

            bool connected = false;
            await UniTask.RunOnThreadPool(() =>
            {
                var netModule = networkManager.MakeNetModule(
                    NetModule.ServiceType.Game,
                    (error, msg) => { this.DLog($"게임 서버 연결 끊김: {error}, {msg}"); },
                    () => { this.DLog("게임 서버 연결 성공"); },
                    (e) => { this.DLog($"게임 서버 연결 실패 : {e.Message}"); }
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
            this.DLog("게임 서버 연결 해제");
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
        public WorldListRequest CachedWorldList { get; private set; }
        public Dictionary<string, List<CharModel>> CachedCharactersByWorld { get; private set; } = new Dictionary<string, List<CharModel>>();
        // Login
        public void SetCharacterList(List<SimpleCharacterInfo> characters)
        {
            CharacterInfos = new List<SimpleCharacterInfo>(characters);
            this.DLog($"캐릭터 리스트 저장 : {characters.Count}개");
        }

        public void AddCharacterInfo(SimpleCharacterInfo character)
        {
            if (CharacterInfos == null)
            {
                CharacterInfos = new List<SimpleCharacterInfo>();
            }
            CharacterInfos.Add(character);
            this.DLog($"캐릭터 추가: {character.Name} (CharId: {character.CharId})");
        }

        public void SelectCharacter(SimpleCharacterInfo character)
        {
            SelectedCharacter = character;
            this.DLog($"선택된 캐릭터 : 이름->{character.Name} , 직업->{character.ClassType}");
        }

        public void SelectCharacterById(ulong charId)
        {
            SelectedCharacter = CharacterInfos?.Find(c => c.CharId == charId);
            if (SelectedCharacter != null)
            {
                this.DLog($"캐릭터 선택 : {SelectedCharacter.Name}");
            }
        }
        public void SetSelectedWorld(uint worldId)
        {
            CurrentSelectedWorldId = worldId;
            this.DLog($"✅ 선택된 월드 ID 설정됨: {worldId}");
        }
        
        public void SetWorldList(WorldListRequest worldList)
        {
            CachedWorldList = worldList;
            this.DLog($"월드 리스트 캐싱: {worldList?.channels?.Count ?? 0}개");
        }
        #endregion

        #region Dev
        // Dev
        public CharModel SelectedCharacterModel { get; protected set; }
        public void SelectCharacterModel(CharModel model)
        {
            SelectedCharacterModel = model;
            this.DLog($"선택된 캐릭터 (Model): {model.name} (ClassType: {model.classtype})");
        }
        #endregion

    }
}
