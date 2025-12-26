using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Database;
using UnityEngine;

namespace penta
{
    /// <summary>
    /// Firebase 초기화 및 관리 (주요 로직)
    /// - Firebase 앱 초기화
    /// - Firestore, Auth, Realtime Database 초기화
    /// </summary>
    public class PentaFirebase : MonoBehaviourSingleton<PentaFirebase>
    {
        public PFireStore PfireStore { get; private set; } = null;
        public PFireAuth PAuth { get; private set; } = null;
        public PRealTimeDb PRealTimeDb { get; private set; } = null;

        private FirebaseApp app = null;

        public bool IsInitialized { get; private set; } = false;

        protected override void Awake()
        {
            base.Awake();
            FirebaseInit().Forget();
        }

        protected override void OnDestroy()
        {
            PAuth?.Dispose();
            PRealTimeDb?.Dispose();
            base.OnDestroy();
        }

        /// <summary> Firebase 초기화 </summary>
        private async UniTask FirebaseInit()
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                PfireStore = new PFireStore(FirebaseFirestore.DefaultInstance);
                PAuth = new PFireAuth(FirebaseAuth.DefaultInstance);

                InitializeRealtimeDatabase();
            }

            await UniTask.WaitUntil(() =>
                PAuth != null && PAuth.IsInitialized &&
                PfireStore != null && PfireStore.IsInitialized);

            if (PRealTimeDb != null)
            {
                await UniTask.WaitUntil(() => PRealTimeDb.IsInitialized);
            }

            IsInitialized = true;
        }

        /// <summary> Realtime Database 초기화 </summary>
        private void InitializeRealtimeDatabase()
        {
            try
            {
                var config = FirebaseConfig.Load();
                if (!config.IsDatabaseUrlValid())
                {
                    PRealTimeDb = null;
                    return;
                }

                string normalizedUrl = config.GetNormalizedDatabaseUrl();
                var dbInstance = FirebaseDatabase.GetInstance(app, normalizedUrl);
                PRealTimeDb = new PRealTimeDb(dbInstance.RootReference);
            }
            catch (System.Exception e)
            {
                PRealTimeDb = null;
            }
        }
    }
}
