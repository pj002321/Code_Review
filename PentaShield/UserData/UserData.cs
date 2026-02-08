using penta;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using Newtonsoft.Json;
using penta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

[FirestoreData]
[Serializable]
public class UserData
{
    [FirestoreProperty]
    [JsonProperty("Id")] public string Id { get; set; }

    [FirestoreProperty]
    [JsonProperty("Name")] public string Name { get; set; }

    [FirestoreProperty]
    [JsonProperty("Level")] public int Level { get; set; }

    [FirestoreProperty]
    [JsonProperty("StageDatas")] public List<StageData> StageDatas { get; set; }

    [FirestoreProperty]
    [JsonProperty("GlobalItems")] public int GlobalItems { get; set; } = 0;

    [FirestoreProperty]
    [JsonProperty("Region")] public RegionConst Region { get; set; } = 0;

    [FirestoreProperty]
    [JsonProperty("Item")] public ItemData Item { get; set; }

    [FirestoreProperty]
    [JsonProperty("LastUpdate")] public DateTime LastUpdate { get; set; }

    // [Cache] TODO : 재화는 임시로 이렇게 해두었지만 정밀한 관리가 필요하다면 Class로 매핑해서 관리하는게 좋아보임
    [FirestoreProperty]
    [JsonProperty("Stone")] public int Stone { get; set; }

    [FirestoreProperty]
    [JsonProperty("Eli")] public int Eli { get; set; }

    [FirestoreProperty]
    [JsonProperty("DailyMission")] public DailyRewardData DailyMission { get; set; }

    [FirestoreProperty]
    [JsonProperty("HighestWave")] public int HighestWave { get; set; }


    /// <summary>
    /// 백업 실패 시 호출될 콜백 델리게이트
    /// </summary>
    public delegate void OnBackupFailedCallback(string errorMessage);


    public UserData()
    {
        StageDatas = new List<StageData>();
        Item = new ItemData();
        DailyMission = new DailyRewardData();
    }


    /// <summary>
    /// 유저 데이터를 로컬 파일에 저장 저장 전 기존 파일을 백업
    /// </summary>
    /// <param name="onBackupFailed">백업 실패 시 호출될 콜백 (선택사항)</param>
    /// <returns>저장 성공 여부</returns>
    public async UniTask<bool> SaveDataToLocalFile(
        OnBackupFailedCallback onBackupFailed = null)
    {
        this.LastUpdate = DateTime.UtcNow;

        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        byte[] encryptedData = EncryptionHelper.Encrypt(json);

        string mainFilePath = PentaConst.SaveDataFilePath;
        string backupFilePath = PentaConst.SaveBackupFilePath;

        // ===== 기존 메인 파일을 백업 파일로 복사 =====
        if (File.Exists(mainFilePath))
        {
            try
            {
                // 기존 백업 파일이 있으면 삭제 (덮어쓰기 위해)
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                    $"[UserData] 기존 백업 파일 삭제 완료".Log();
                }
                
                File.Copy(mainFilePath, backupFilePath, overwrite: true);

                $"[UserData] 백업 파일 생성 완료\n경로: {backupFilePath}".Log();
            }
            catch (Exception e)
            {
                // 백업 실패 시 저장 중단
                string errorMsg = $"백업 파일 생성 실패: {e.Message}";
                $"[UserData] {errorMsg}".DError();
                $"[UserData] 데이터 손실 방지를 위해 저장을 중단합니다.".DWarning();

                // 콜백 호출 (제공된 경우에만)
                onBackupFailed?.Invoke(errorMsg);

                return false;  // 저장 중단
            }
        }
        else
        {
            "[UserData] 기존 파일이 없어 백업을 건너뜁니다 (신규 저장).".Log();
        }

        // ===== 새 데이터를 메인 파일에 저장 =====
        try
        {
            await File.WriteAllBytesAsync(mainFilePath, encryptedData);
            $"[UserData] 유저 데이터 저장 완료\n경로: {mainFilePath}\nLastUpdate: {this.LastUpdate:yyyy-MM-dd HH:mm:ss}".ELog();

            return true;  // 저장 성공
        }
        catch (Exception e)
        {
            $"[UserData] 파일 저장 실패: {e.Message}".DError();
            $"[UserData] 백업 파일({backupFilePath})이 유지되어 있으므로 다음 실행 시 복구 가능합니다.".EWarning();

            return false;  // 저장 실패
        }
    }

    /// <summary>
    /// 최신 유저 데이터를 로드 (메인 파일 → 백업 파일 순서)
    /// </summary>
    /// <returns>로드된 UserData, 실패 시 null</returns>
    public static UserData LoadLatestUserData()
    {
        return LoadFromLocalFileWithBackup();        
    }

    public static UserData CreateNewAnonymousUserData()
    {
        UserData newUser = new UserData
        {
            Id = $"{PentaConst.PrefixUserId}" + Guid.NewGuid().ToString(),
            Name = "PentaHero",
            GlobalItems = 10,
            Region = RegionConstHelper.GetRegionName(Application.systemLanguage),
        };
        return newUser;
    }

    public bool AddStageData(StageData data)
    {
        if(data == null) { return false; }

        StageData targetData = null;

        // 기존 데이터 탐색
        foreach (StageData checkData in StageDatas)
        {
            if(checkData.StageName == data.StageName)
            {
                targetData = checkData;
                break;
            }
        }

        
        if(targetData == null)
        {   // if : 기존데이터가 존재하지않음 (새로운 스테이지 정보는 무조건 추가)
            $"[UserData] : 기존에 존재하지 않으며 새로운 스테이지 데이터이므로 데이터를 추가합니다\n{data.StageName}\nRound : {data.Round}\nScore : {data.Score}".Log();
            StageDatas.Add(data);
            return true;
        }        
        else
        {   // else : 기존데이터가 존재함 (라운드 및 스코어 비교 후 조건에 맞으면 추가)
            if (targetData.Round < data.Round)
            {
                $"[UserData] : 기존에 존재하던 데이터보다 더 높은 라운드이기때문에 데이터를 추가합니다\n{data.StageName}\n기존 : {targetData.Round}\n새데이터 : {data.Round}".Log();
                StageDatas.Remove(targetData);
                StageDatas.Add(data);
                return true;
            }
            else if (targetData.Round == data.Round)
            {
                if (targetData.Score < data.Score)
                {   // 라운드 동일 && 스코어가 기존보다 큼 (기존과 체인지)
                    $"[UserData] : 기존과 라운드는 동일하지만 점수가 더 높아서 데이터를 추가합니다.\n{data.StageName}\n기존 Round : {targetData.Round}\n새데이터 Round : {data.Round}\n기존 Score : {targetData.Score}\n새데이터 Score : {data.Score}".Log();
                    StageDatas.Remove(targetData);
                    StageDatas.Add(data);
                    return true;
                }
                else
                {   // 라운드 동일 && 스코어가 기존보다 같거나 작음 (추가 안함)
                    $"[UserData] : 기존데이터와 라운드가 동일하지만 스코어가 기존보다 낮기때문에 스테이지 데이터를 추가하지 않습니다.".Log();
                    return false;
                }
            }
            "[UserData] : 모든 조건에 맞지않아 데이터를 추가하지 못했습니다.".DError();
            return false;
        }
    }       // AddStageData()

    /// <summary>
    /// 로컬 파일에서 유저 데이터를 로드합니다. 메인 파일 실패 시 백업 파일을 시도합니다.
    /// </summary>
    /// <returns>로드된 UserData, 실패 시 null</returns>
    public static UserData LoadFromLocalFileWithBackup()
    {
        string mainFilePath = PentaConst.SaveDataFilePath;
        string backupFilePath = PentaConst.SaveBackupFilePath;

        // ===== 메인 파일 로드 시도 =====
        if (File.Exists(mainFilePath))
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(mainFilePath);
                string json = EncryptionHelper.Decrypt(encryptedData);

                // Decrypt 실패 시 null 반환됨
                if (string.IsNullOrEmpty(json))
                {
                    "[UserData] 메인 파일 복호화 실패 - 백업 파일 시도".DWarning();
                }
                else
                {
                    UserData userData = JsonConvert.DeserializeObject<UserData>(json);

                    if (userData != null)
                    {
                        $"[UserData] ✅ 메인 파일 로드 성공\n경로: {mainFilePath}\nLastUpdate: {userData.LastUpdate:yyyy-MM-dd HH:mm:ss}".ELog();
                        return userData;
                    }
                    else
                    {
                        "[UserData] 메인 파일 역직렬화 실패 - 백업 파일 시도".DWarning();
                    }
                }
            }
            catch (Exception e)
            {
                $"[UserData] 메인 파일 로드 실패: {e.Message}\n백업 파일 로드를 시도합니다.".DWarning();
            }
        }
        else
        {
            "[UserData] 메인 파일이 존재하지 않습니다. 백업 파일을 확인합니다.".Log();
        }

        // ===== 백업 파일 로드 시도 =====
        if (File.Exists(backupFilePath))
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(backupFilePath);
                string json = EncryptionHelper.Decrypt(encryptedData);

                if (string.IsNullOrEmpty(json))
                {
                    "[UserData] 백업 파일 복호화 실패".DError();
                }
                else
                {
                    UserData userData = JsonConvert.DeserializeObject<UserData>(json);

                    if (userData != null)
                    {
                        $"[UserData] ⚠️ 백업 파일에서 데이터 복구 성공!\n경로: {backupFilePath}\nLastUpdate: {userData.LastUpdate:yyyy-MM-dd HH:mm:ss}".EWarning();

                        // 백업에서 복구했으므로 메인 파일로 다시 복사하여 정상화
                        try
                        {
                            File.Copy(backupFilePath, mainFilePath, overwrite: true);
                            "[UserData] 백업 파일을 메인 파일로 복원 완료".Log();
                        }
                        catch (Exception copyEx)
                        {
                            $"[UserData] 백업 파일 복원 실패 (데이터는 로드됨): {copyEx.Message}".DWarning();
                        }

                        return userData;
                    }
                    else
                    {
                        "[UserData] 백업 파일 역직렬화 실패".DError();
                    }
                }
            }
            catch (Exception e)
            {
                $"[UserData] 백업 파일 로드 실패: {e.Message}".DError();
            }
        }
        else
        {
            "[UserData] 백업 파일도 존재하지 않습니다.".Log();
        }

        // ===== 3단계: 모든 로드 실패 =====
        "[UserData] ❌ 로드 가능한 파일이 없습니다. 신규 데이터를 생성해야 합니다.".EWarning();
        return null;
    }       // LoadFromLocalFileWithBackup()


    /// <summary>
    /// 일반 Save 파일과 TODO 파일 중 최신 데이터를 반환 (제거 에정)
    /// </summary>
    /// <returns>최신 UserData, 둘 다 없으면 null</returns>
    public static UserData GetLatestUserDataFromFiles()
    {
        UserData normalData = null;
        UserData todoData = null;

        // 일반 Save 파일 로드
        if (File.Exists(PentaConst.SaveDataFilePath))
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(PentaConst.SaveDataFilePath);
                string normalJson = EncryptionHelper.Decrypt(encryptedData);
                normalData = JsonConvert.DeserializeObject<UserData>(normalJson);

                if (normalData != null)
                {
                    $"[GetLatestUserData] 일반 파일 로드 성공 (LastUpdate: {normalData.LastUpdate})".Log();
                }
            }
            catch (Exception e)
            {
                $"[GetLatestUserData] 일반 파일 읽기 실패: {e.Message}".DError();
            }
        }
        else
        {
            "[GetLatestUserData] 일반 Save 파일이 존재하지 않습니다.".Log();
        }

        if (File.Exists(PentaConst.SaveTodoUploadFilePath))
        {
            try
            {                
                byte[] encryptedData = File.ReadAllBytes(PentaConst.SaveTodoUploadFilePath);
                string todoJson = EncryptionHelper.Decrypt(encryptedData);
                todoData = JsonConvert.DeserializeObject<UserData>(todoJson);

                if (todoData != null)
                {
                    $"[GetLatestUserData] TODO 파일 로드 성공 (LastUpdate: {todoData.LastUpdate})".Log();
                }
            }
            catch (Exception e)
            {
                $"[GetLatestUserData] TODO 파일 읽기 실패: {e.Message}".DError();
            }
        }
        else
        {
            "[GetLatestUserData] TODO 파일이 존재하지 않습니다.".Log();
        }

        //최신 데이터 선택
        // 둘 다 없는 경우
        if (normalData == null && todoData == null)
        {
            "[GetLatestUserData] 로드 가능한 파일이 없습니다.".EWarning();
            return null;
        }

        // 일반 파일만 있는 경우
        if (normalData != null && todoData == null)
        {
            "[GetLatestUserData] 일반 파일 반환".ELog();
            return normalData;
        }

        // TODO 파일만 있는 경우
        if (normalData == null && todoData != null)
        {
            "[GetLatestUserData] TODO 파일 반환".ELog();
            return todoData;
        }

        // 둘 다 있는 경우 - LastUpdate 비교
        if (normalData.LastUpdate >= todoData.LastUpdate)
        {
            $"[GetLatestUserData] 일반 파일이 더 최신입니다. (일반: {normalData.LastUpdate}, TODO: {todoData.LastUpdate})".ELog();
            return normalData;
        }
        else
        {
            $"[GetLatestUserData] TODO 파일이 더 최신입니다. (일반: {normalData.LastUpdate}, TODO: {todoData.LastUpdate})".ELog();
            return todoData;
        }
    }       // GetLatestUserDataFromFiles()


}       // ClassEnd