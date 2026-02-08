using penta;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using penta;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class UserDataManager : MonoBehaviourSingleton<UserDataManager>
{
    public UserData Data { get; private set; } = null;

    // 저장 관리
    private Queue<SaveRequest> saveQueue = new Queue<SaveRequest>();
    private bool isSaving = false;
    private bool isProcessingQueue = false;    

    // 자동저장
    private float autoSaveTimer = 0f;
    private const float AUTO_SAVE_INTERVAL = 600f;  // 10분 = 600초
    private bool enableAutoSave = true;

    /// <summary> Firestore 사용자 문서 실시간 반영용 (콘솔/다른 기기 수정 시 Eli·Stone 등 즉시 반영) </summary>
    private ListenerRegistration _userDocListener;

    public List<StageData> StageDatas => Data?.StageDatas;
    public ItemData ItemData => Data?.Item;
    public int GlobalItemCount => Data.GlobalItems;

    public bool IsInitialized { get; private set; } = false;
    public event Action<UserData> OnDataUpdated;

    protected override void Awake()
    {
        base.Awake();
    }
    private async void Start()
    {
        "[UserDataManager] Start() 시작".Log();

        "[UserDataManager] Firebase 초기화 대기 중...".Log();
        await UniTask.WaitUntil(() => PentaFirebase.Shared != null && PentaFirebase.Shared.IsInitialized == true);
        "[UserDataManager] Firebase 초기화 완료".Log();

        "[UserDataManager] PFireAuth 초기화 대기 중...".Log();
        await UniTask.WaitUntil(() => PentaFirebase.Shared.PAuth != null && PentaFirebase.Shared.PAuth.IsInitialized == true);
        "[UserDataManager] PFireAuth 초기화 완료".Log();

        if (LoadUserData() == false)
        {   // UserData가 로드되지 않은 경우 새로 생성
            "[UserDataManager] 로컬 파일 없음 - 새 익명 유저 생성".Log();
            Data = UserData.CreateNewAnonymousUserData();
            await SaveCritical();   // 생성 직후 저장 (이후 로그인시 유지가 되도록)
        }
        else
        {
            $"[UserDataManager] 로컬 파일 로드 성공 - ID: {Data?.Id}".Log();
        }

        string msg = $"[UserDataManager] 초기화 완료 - User ID : {Data.Id}, Auth IsLoggedIn : {PentaFirebase.Shared.PAuth.IsLoggedIn}";
        msg.Log();
        IsInitialized = true;
        NotifyDataUpdated();
        if (!IsAnonymouseUser())
            StartUserDocListener();
    }

    private void Update()
    {
        if (!enableAutoSave || !IsInitialized) return;

        autoSaveTimer += Time.deltaTime;

        if (autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            autoSaveTimer = 0f;
            SaveAuto();
        }
    }


    protected override void OnDestroy()
    {
        StopUserDocListener();
        base.OnDestroy();
    }


    private void OnApplicationPause(bool pause)
    {
        if (pause && Data != null)
        {
            Data.SaveDataToLocalFile().Forget();
        }
    }
    private void OnApplicationQuit()
    {
        // 앱 종료 시 Queue에 남은 작업 모두 처리
        $"[UserDataManager] 앱 종료 - 남은 Queue 작업: {saveQueue.Count}개".EWarning();

        // Queue를 모두 비우고 Critical 저장
        saveQueue.Clear();
        SaveDataSynchronously();
    }



    #region Debug
#if UNITY_EDITOR
    [Button("SaveTest")]
    private void SaveTest()
    {
        if (Data == null)
        {
            "UserData is null".DError();
            return;
        }
        _ = Data.SaveDataToLocalFile();
    }

#endif
    #endregion

    public void NotifyDataUpdated()
    {
        if (Data == null) return;
        OnDataUpdated?.Invoke(Data);
    }

    public void ClearData()
    {
        "[UserDataManager] Clearing user data...".Log();
        StopUserDocListener();
        Data = UserData.CreateNewAnonymousUserData();
        if (Data != null)
        {
            Data.Name = "PentaHero"; // 익명 유저의 기본 이름 설정
            Data.SaveDataToLocalFile().Forget(); // 파일로 저장
        }
        
        // UI가 즉시 업데이트되도록 이벤트 발행
        OnDataUpdated?.Invoke(Data);
        
        "[UserDataManager] Local user data has been cleared and reset to anonymous user.".Log();
    }

    /// <summary>
    /// 사용자 계정 및 데이터를 완전히 삭제합니다.
    /// Firebase에서 사용자 문서를 삭제하고 랭킹에서도 제거한 후 로컬 데이터를 초기화합니다.
    /// </summary>
    /// <param name="userId">삭제할 사용자 ID</param>
    /// <returns>성공 여부</returns>
    public async UniTask<bool> DeleteUserAccount(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            "[UserDataManager] Cannot delete account - User ID is null or empty.".EError();
            return false;
        }

        try
        {
            "[UserDataManager] Starting account deletion process...".Log();

            // 1. Firebase Firestore에서 사용자 문서 삭제
            bool firestoreDeleted = await PentaFirebase.Shared.PfireStore.DeleteDocumentAsync("users", userId);
            if (firestoreDeleted)
            {
                $"[UserDataManager] Firestore data deleted for user: {userId}".Log();
            }
            else
            {
                $"[UserDataManager] Failed to delete Firestore data for user: {userId}".DWarning();
            }

            // 2. 랭킹에서 사용자 제거
            bool rankingDeleted = await PentaFirebase.Shared.PfireStore.RemoveUserFromRankingsAsync(userId);
            if (rankingDeleted)
            {
                $"[UserDataManager] User {userId} removed from rankings".Log();
            }
            else
            {
                $"[UserDataManager] Failed to remove user {userId} from rankings".DWarning();
            }

            // 3. 로컬 데이터 초기화
            ClearData();
            
            "[UserDataManager] Account deletion completed successfully.".Log();
            return true;
        }
        catch (Exception e)
        {
            $"[UserDataManager] Account deletion failed: {e.Message}".EError();
            return false;
        }
    }

    /// <summary>
    /// 로그인 성공 후 호출될 데이터 마이그레이션 및 동기화 메서드.
    /// </summary>
    /// <param name="firebaseUser">로그인에 성공한 Firebase 사용자</param>
    public async UniTask<bool> SyncWithFirebase(FirebaseUser firebaseUser)
    {
        if (firebaseUser == null)
        {
            "Firebase user is null. Cannot sync.".DError();
            return false;
        }

        // 다른 계정으로 로그인하는 경우를 대비해, 먼저 로컬 데이터를 초기화합니다.
        // 이렇게 하면 다른 계정 정보가 남는 것을 방지할 수 있습니다.
        if (Data != null && Data.Id != firebaseUser.UserId)
        {
            $"[UserDataManager] Different user detected (기존: {Data.Id} -> 새로운: {firebaseUser.UserId}). Clearing local data before sync.".Log();
            ClearData(); // ClearData()에서 이미 새로운 익명 데이터를 생성하고 저장하므로 LoadUserData() 불필요
        }
        
        if (Data == null)
        {
            "[UserDataManager] No local data after clear/load, creating new.".Log();
            Data = UserData.CreateNewAnonymousUserData();
        }

        var pFireStore = PentaFirebase.Shared.PfireStore;
        DocumentReference userDocRef = pFireStore.GetUserDocumentReference(firebaseUser.UserId);
        DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync();

        bool isLocalDataAnonymous = IsAnonymouseUser(); // Guid로 생성된 ID인지 확인

        if (snapshot.Exists)
        {
            // 시나리오 1: 서버에 데이터가 있음 (기존 유저)
            "Server data found.".ELog();
            UserData serverData = snapshot.ConvertTo<UserData>();

            // 로컬 데이터가 익명이거나 다른 유저의 데이터였을 경우, 서버 데이터로 완전히 덮어쓴다.
            if (isLocalDataAnonymous || Data.Id != serverData.Id)
            {
                "Overwriting local data with server data.".Log();
                this.Data = serverData;
            }
        }
        else
        {
            // 시나리오 2: 서버에 데이터가 없음 (신규 유저)
            "No server data found.".ELog();
            if (isLocalDataAnonymous)
            {
                // 로컬에 있는 임시 데이터를 서버 계정에 귀속(마이그레이션)시킨다.
                "Migrating local anonymous data to Firebase account.".Log();
                this.Data.Id = firebaseUser.UserId; // ID를 영구 Firebase UID로 교체
                this.Data.Name = firebaseUser.DisplayName; // 이름도 구글 계정 이름으로 업데이트
            }
        }
        
        if (string.IsNullOrWhiteSpace(Data.Name))
        {
            Data.Name = string.IsNullOrWhiteSpace(firebaseUser.DisplayName)
                ? "PentaHero"
                : firebaseUser.DisplayName;
            NotifyDataUpdated();
        }

        await FirebaseSaveUserData();   // 파이어베이스에 업로드 (내부에서 로컬 저장 진행)

        StartUserDocListener();
        $"Sync complete. Final User ID: {Data.Id}".ELog();
        return true;
    }

    public async UniTask UpdateUserDataAsync()
    {
        if (IsAnonymouseUser() == true)
        {
            await Data.SaveDataToLocalFile();
        }
        else
        {
            // Firebase 저장은 Critical로 처리
            await SaveCritical("Firebase 동기화");
        }
    }

    public async UniTask<bool> FirebaseSaveUserData()
    {

        // 임시 유저(google 연동안된 유저)의 데이터는 firebase에 저장하지 않음
        if (IsAnonymouseUser())
        {
            "Cannot save to Firebase with a temporary anonymous ID. Please log in and sync first.".EWarning();
            return false;
        }

        await UniTask.WaitUntil(() => PentaFirebase.Shared.IsInitialized);

        Data.LastUpdate = DateTime.UtcNow;   // 저장 직전 시간 업데이트

        await Data.SaveDataToLocalFile();     // 업로드 직전 유저 데이터 로컬에 저장

        bool success = await PentaFirebase.Shared.PfireStore.SetDocumentAsync("users", Data.Id, Data);

        if (success)
        {
            NotifyDataUpdated();
        }

        if (success && File.Exists(PentaConst.SaveTodoUploadFilePath))
        {   // 업로드를 성공했으며 기존의 Todo 파일이 존재한다면 Todo파일 제거
            try
            {
                File.Delete(PentaConst.SaveTodoUploadFilePath);
                $"Todo upload file deleted after successful upload.".ELog();
            }
            catch (Exception e)
            {
                $"Failed to delete Todo upload file: {e.Message}".EError();
            }
        }

        return success;
    }

    public async UniTask<bool> IsAnonymouseUserAsync()
    {
        await UniTask.WaitUntil(() => IsInitialized == true);
        return Data.Id.StartsWith(PentaConst.PrefixUserId) == true ? true : false;
    }

    public bool IsAnonymouseUser()
    {
#if UNITY_EDITOR
        return false;
#else        
        return Data.Id.StartsWith(PentaConst.PrefixUserId) == true ? true : false;
#endif
    }



    public string GetUserDataJson()
    {
        if (Data == null)
        {
            "UserData is null, cannot get JSON.".EError();
            return string.Empty;
        }
        return JsonConvert.SerializeObject(Data);
    }


    private bool LoadUserData()
    {
        Data = UserData.LoadLatestUserData();
        return Data != null;
    }

    private void StartUserDocListener()
    {
        StopUserDocListener();
        if (Data == null || IsAnonymouseUser() || PentaFirebase.Shared?.PfireStore == null) return;
        _userDocListener = PentaFirebase.Shared.PfireStore.ListenToDocument(PFireStore.UserCollection, Data.Id, OnUserDocSnapshot);
    }

    private void StopUserDocListener()
    {
        if (_userDocListener != null)
        {
            _userDocListener.Stop();
            _userDocListener = null;
        }
    }

    private void OnUserDocSnapshot(DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists) return;
        var serverData = snapshot.ConvertTo<UserData>();
        if (serverData == null) return;
        UniTask.Void(async () =>
        {
            await UniTask.SwitchToMainThread();
            if (Data == null) return;
            Data = serverData;
            NotifyDataUpdated();
        });
    }

    /// <summary>
    /// 앱 종료 시 동기적으로 저장 (완료 보장)
    /// </summary>
    private void SaveDataSynchronously()
    {
        if (Data == null) return;

        try
        {
            // UniTask를 동기적으로 대기
            Data.SaveDataToLocalFile().GetAwaiter().GetResult();
            "[UserDataManager] 동기 저장 완료".Log();
        }
        catch (Exception e)
        {
#if !UNITY_EDITOR
            $"[UserDataManager] 동기 저장 실패: {e.Message}".DError();
#endif
        }
    }

    #region Save

    /// <summary>
    /// 저장 Queue를 순차적으로 처리합니다.
    /// </summary>
    private async UniTaskVoid ProcessSaveQueue()
    {
        if (isProcessingQueue)
        {
            "[UserDataManager] Queue 처리가 이미 진행 중입니다.".DWarning();
            return;
        }

        isProcessingQueue = true;
        $"[UserDataManager] 📦 Queue 처리 시작 (대기: {saveQueue.Count}개)".Log();

        try
        {
            while (saveQueue.Count > 0)
            {
                SaveRequest request = saveQueue.Dequeue();

                $"[UserDataManager] Queue 처리 중: {request.Reason} (Priority: {request.Priority}, 남은 작업: {saveQueue.Count}개)".Log();

                // 저장 실행
                isSaving = true;
                bool success = await Data.SaveDataToLocalFile(
                    onBackupFailed: (errorMsg) =>
                    {
                        $"[UserDataManager] Queue 백업 실패 ({request.Reason}): {errorMsg}".DError();
                    }
                );
                isSaving = false;

                if (success)
                {
                    $"[UserDataManager] ✅ Queue 저장 완료: {request.Reason}".Log();
                }
                else
                {
                    $"[UserDataManager] ❌ Queue 저장 실패: {request.Reason}".DError();

                    // 실패 시 재시도? (선택사항)
                    // 여기서는 일단 로그만 남기고 다음 작업 진행
                }

                // 다음 저장 전 짧은 대기 (과부하 방지)
                if (saveQueue.Count > 0)
                {
                    await UniTask.Delay(100);  // 100ms 대기
                }
            }

            $"[UserDataManager] 📦 Queue 처리 완료".Log();
        }
        catch (Exception e)
        {
            $"[UserDataManager] Queue 처리 중 오류 발생: {e.Message}".DError();
        }
        finally
        {
            isProcessingQueue = false;
        }
    }       // ProcessSaveQueue()


    /// <summary>
    /// Critical 저장: 결제, 스테이지 클리어 등 중요한 작업 시 즉시 저장합니다.
    /// Queue를 우회하고, 진행 중인 저장이 있으면 완료를 기다립니다.
    /// </summary>
    /// <param name="reason">저장 사유 (디버그용)</param>
    /// <returns>저장 성공 여부</returns>
    public async UniTask<bool> SaveCritical(string reason = "Critical")
    {
        $"[UserDataManager] 🔴 Critical 저장 요청: {reason}".Log();

        // 진행 중인 저장이 있으면 완료 대기
        if (isSaving)
        {
            $"[UserDataManager] 진행 중인 저장 대기 중... (사유: {reason})".DWarning();
            await UniTask.WaitUntil(() => isSaving == false);
            $"[UserDataManager] 대기 완료, Critical 저장 시작".Log();
        }

        // 즉시 저장 (동기적으로)
        isSaving = true;

        try
        {
            bool success = await Data.SaveDataToLocalFile(
            onBackupFailed: (errorMsg) =>
            {
                $"[UserDataManager] Critical 백업 실패: {errorMsg}".DError();
            });

            if (success)
            {
                $"[UserDataManager] ✅ Critical 저장 완료: {reason}".ELog();
            }
            else
            {
                $"[UserDataManager] ❌ Critical 저장 실패: {reason}".DError();
            }

            return success;
        }
        finally
        {
            isSaving = false;
        }
    }       // SaveCritical()

    /// <summary>
    /// Important 저장: 일반적인 중요 작업 시 Queue에 추가합니다.
    /// Queue에 추가 후 즉시 순차 처리됩니다.
    /// </summary>
    /// <param name="reason">저장 사유 (디버그용)</param>
    public void SaveImportant(string reason = "Important")
    {
        $"[UserDataManager] 🟡 Important 저장 요청: {reason}".Log();

        // 새 요청 추가
        SaveRequest request = new SaveRequest
        {
            Priority = E_SavePriority.Important,
            Timestamp = DateTime.UtcNow,
            Reason = reason
        };

        saveQueue.Enqueue(request);
        $"[UserDataManager] Important 저장 Queue 추가 (대기 중: {saveQueue.Count}개)".Log();

        // Queue 처리 시작 (이미 처리 중이면 무시)
        if (!isProcessingQueue)
        {
            ProcessSaveQueue().Forget();
        }
    }

    /// <summary>
    /// Important 저장 (비동기): 서버에 즉시 저장이 필요한 경우 사용합니다.
    /// 출석 체크 등 서버 검증이 필요한 작업에 사용합니다.
    /// </summary>
    /// <param name="reason">저장 사유 (디버그용)</param>
    /// <returns>저장 성공 여부</returns>
    public async UniTask<bool> SaveImportantAsync(string reason = "Important")
    {
        $"[UserDataManager] 🟡 Important 저장 요청 (Async): {reason}".Log();

        if (IsAnonymouseUser())
        {
            $"[UserDataManager] Anonymous user - saving to local only".DWarning();
            await Data.SaveDataToLocalFile();
            return true;
        }

        // 진행 중인 저장이 있으면 완료 대기
        if (isSaving)
        {
            $"[UserDataManager] 진행 중인 저장 대기 중... (사유: {reason})".DWarning();
            await UniTask.WaitUntil(() => isSaving == false);
        }

        isSaving = true;

        try
        {
            // 로컬 저장
            await Data.SaveDataToLocalFile();

            // Firebase 저장
            if (PentaFirebase.Shared?.PfireStore != null && PentaFirebase.Shared.IsInitialized)
            {
                Data.LastUpdate = DateTime.UtcNow;
                bool firebaseSuccess = await PentaFirebase.Shared.PfireStore.SetDocumentAsync("users", Data.Id, Data);
                
                if (firebaseSuccess)
                {
                    $"[UserDataManager] ✅ Important 저장 완료 (서버): {reason}".Log();
                    NotifyDataUpdated();
                    return true;
                }
                else
                {
                    $"[UserDataManager] ❌ Firebase 저장 실패: {reason}".DError();
                    return false;
                }
            }
            else
            {
                $"[UserDataManager] Firebase not available - local save only".DWarning();
                return true;
            }
        }
        catch (Exception e)
        {
            $"[UserDataManager] ❌ Important 저장 중 예외 발생: {e.Message}".DError();
            return false;
        }
        finally
        {
            isSaving = false;
        }
    }

    /// <summary>
    /// Auto 저장: 10분 타이머에서 자동으로 호출됩니다.
    /// Queue에 대기 중인 작업이 있으면 스킵합니다 (불필요한 저장 방지).
    /// </summary>
    private void SaveAuto()
    {
        $"[UserDataManager] ⚪ Auto 저장 타이머 {AUTO_SAVE_INTERVAL}sec".Log();

        // Queue에 이미 대기 중인 작업이 있으면 스킵
        if (saveQueue.Count > 0)
        {
            $"[UserDataManager] Queue에 {saveQueue.Count}개 작업 대기 중 - Auto 저장 스킵".Log();
            return;
        }

        // 현재 저장 중이면 스킵
        if (isSaving)
        {
            "[UserDataManager] 저장 진행 중 - Auto 저장 스킵".Log();
            return;
        }

        // Auto 저장 요청 추가
        SaveRequest request = new SaveRequest
        {
            Priority = E_SavePriority.Auto,
            Timestamp = DateTime.UtcNow,
            Reason = $"{AUTO_SAVE_INTERVAL}sec 자동 저장"
        };

        saveQueue.Enqueue(request);
        $"[UserDataManager] Auto 저장 Queue 추가".Log();

        // Queue 처리 시작
        if (!isProcessingQueue)
        {
            ProcessSaveQueue().Forget();
        }
    }       // SaveAuto()


    #endregion








}       // ClassEnd
