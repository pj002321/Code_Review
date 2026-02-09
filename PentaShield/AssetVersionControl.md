# 에셋 버전 관리 및 검증 (Asset Version Control & Verification)

본 문서는 `PentaShield` 프로젝트의 Addressable 에셋 버전 관리, 다운로드, 무결성 검증 시스템에 대해 설명합니다.

## 1. 시스템 개요

이 시스템은 Firebase Storage를 백엔드로 사용하여 에셋 번들을 배포하고, 클라이언트에서 이를 안전하게 업데이트하는 구조를 갖추고 있습니다.

### 주요 기능
*   **버전 관리**: 파일 목록의 해시를 기반으로 에셋 버전 변경을 감지합니다.
*   **무결성 검증**: MD5 해시를 통해 다운로드된 파일의 손상 여부를 확인합니다.
*   **호환성 체크**: 앱 버전과 에셋 카탈로그 버전을 비교하여 강제 업데이트를 유도합니다.

---

## 2. 주요 클래스 및 역할

| 클래스 | 경로 | 역할 |
| :--- | :--- | :--- |
| **BootLoader** | `Boot/BootLoader.cs` | 게임 진입점. 앱 버전 체크, 점검 모드 확인, 다운로드 시작 등을 관리합니다. |
| **AddressableDownloadManager** | `Addressables/AddressableDownloadManager.cs` | 파일 다운로드, 로컬 캐시 관리, 무결성 검증(MD5)을 수행합니다. |
| **AddressableSystemManager** | `Addressables/AddressableSystemManager.cs` | Addressable 시스템 초기화, 카탈로그 업데이트, 경로 리맵핑을 담당합니다. |
| **AddressableFirebaseUploader** | `Addressables/AddressabpeFirebaseUploader.cs` | (Editor) 빌드된 에셋을 Firebase Storage로 업로드하는 툴입니다. |

---

## 3. 상세 프로세스

### 3.1 초기 진입 및 검사 (BootLoader)

게임 시작 시 `BootLoader.EntryGame()`에서 다음 순서로 검사를 수행합니다.

1.  **점검 모드 확인 (`CheckMaintenanceFlag`)**
    *   Firebase Realtime Database의 `maintenance/is_on` 값을 확인합니다.
    *   `true`일 경우 점검 팝업을 띄우고 진입을 차단합니다.

2.  **카탈로그 업데이트 확인 (`CheckCatalogVersion`)**
    *   Addressables 시스템을 통해 원격 카탈로그의 업데이트가 있는지 확인합니다.

```csharp
// BootLoader.cs

private async UniTask<bool> CheckMaintenanceFlag()
{
    // ... 초기화 대기 ...
    const string path = "maintenance/is_on";
    var value = await rtDb.GetValueAsync(path);
    // ...
    return result;
}

private async UniTask<bool> CheckCatalogVersion()
{
    // ...
    var checkHandle = Addressables.CheckForCatalogUpdates(false);
    var catalogsToUpdate = await checkHandle.ToUniTask();
    
    bool hasUpdate = catalogsToUpdate != null && catalogsToUpdate.Count > 0;
    
    Addressables.Release(checkHandle);
    return hasUpdate;
}
```

### 3.2 버전 관리 및 다운로드 (AddressableDownloadManager)

`StartDownload()` 호출 시 다음과 같이 동작합니다.

#### A. 버전 감지 (Version Detection)
*   Firebase Storage에서 파일 목록을 가져와 **버전 해시**를 생성합니다.
    *   `GenerateVersionHash`: 파일 이름들을 정렬하여 조합한 문자열의 MD5 해시를 생성.
*   **캐시 초기화**:
    *   로컬에 저장된 이전 버전(`version.txt`)과 비교하여 다를 경우, **로컬 캐시를 전체 삭제**합니다 (`ClearLocalCache`).
    *   이는 파일 간 의존성 문제나 구 버전 파일 잔재로 인한 오류를 방지하기 위함입니다.

```csharp
// AddressableDownloadManager.cs

private string GenerateVersionHash(List<FirebaseStorageItem> items)
{
    var fileNames = items
        .Where(item => !item.name.EndsWith("/"))
        .Select(item => item.name)
        .OrderBy(name => name)
        .ToList();

    string combinedNames = string.Join("|", fileNames);
    using (var md5 = MD5.Create())
    {
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(combinedNames));
        return Convert.ToBase64String(hash);
    }
}

public void ClearLocalCache()
{
    if (Directory.Exists(persistentDataPath))
    {
        Directory.Delete(persistentDataPath, true);
        Directory.CreateDirectory(persistentDataPath);
        verifiedFiles.Clear();
        DeleteVersionFile();
    }
}
```

#### B. 다운로드 대상 선정
*   각 파일에 대해 다음 조건을 확인하여 다운로드 목록에 추가합니다.
    *   로컬에 파일이 없음 via `File.Exists`
    *   로컬 파일의 MD5 해시가 서버와 다름 (검증 옵션 활성화 시)

### 3.3 무결성 검증 (Integrity Check)

파일 다운로드가 완료되면 즉시 무결성을 검증합니다.

*   **검증 로직 (`ValidateFileIntegrity`)**:
    1.  다운로드된 파일의 MD5 해시를 계산합니다 (`CalculateMD5`).
    2.  Firebase Storage 메타데이터의 `md5Hash`와 비교합니다.
    3.  **불일치 시**: 해당 파일을 삭제하고 예외를 발생시킵니다. (다운로드 실패 처리)
    4.  **일치 시**: `verifiedFiles` 목록에 추가하고 다운로드를 완료합니다.

```csharp
// AddressableDownloadManager.cs

private void ValidateFileIntegrity(FirebaseStorageItem fileItem, string localPath)
{
    string downloadedHash = CalculateMD5(localPath);
    if (downloadedHash != fileItem.md5Hash)
    {
        File.Delete(localPath);
        throw new Exception($"파일 무결성 검증 실패: {fileItem.name}");
    }

    verifiedFiles.Add(fileItem.name);
}

private string CalculateMD5(string filePath)
{
    using (var md5 = MD5.Create())
    using (var stream = File.OpenRead(filePath))
    {
        byte[] hash = md5.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }
}
```

### 3.4 앱 호환성 체크 (App Compatibility)

다운로드 완료 후, `BootLoader`는 앱 버전과 에셋 버전을 비교합니다.

*   **카탈로그 버전 확인**: 다운로드된 `catalog_X.Y.Z.json` 파일명에서 버전을 파싱합니다.
*   **앱 버전 확인**: `Application.version`을 확인합니다.
*   **비교 로직**:
    *   카탈로그 버전이 앱 버전보다 **상위(Major/Minor)**일 경우 업데이트가 필요한 것으로 간주합니다.
    *   이 경우 게임 진입을 막고 스토어 업데이트를 안내합니다.

```csharp
// BootLoader.cs
private bool IsCatalogNewer(string catalogVersion, string appVersion)
{
    // ... 버전 파싱 로직 ...
    if (c.major != a.major) return c.major > a.major;
    if (c.minor != a.minor) return c.minor > a.minor;
    return c.patch > a.patch;
}
```

---

## 4. 데이터 흐름 요약

1.  **App Start** -> `BootLoader` Init
2.  **Check Maintenance** -> Firebase DB
3.  **Download Manager** Start
    *   Fetch File List -> Generate Version Hash
    *   Check Local Cache -> Clear if Version Mismatch
    *   Download Files -> **Verify MD5**
4.  **Compatibility Check**
    *   Catalog Version vs App Version
    *   If Catalog > App -> Block Entry
5.  **Addressable Init**
    *   `AddressableSystemManager` Loads Local Catalog
    *   Map `ab/` paths to `PersistentDataPath`
6.  **Game Entry**

## 5. 에디터 업로드 (AddressableFirebaseUploader)

*   **빌드**: `AddressableAssetSettings.BuildPlayerContent()`를 호출하여 번들을 빌드합니다.
*   **초기화**: Firebase Storage의 타겟 경로를 비웁니다 (Clean Upload).
*   **업로드**: 빌드된 파일들을 Firebase Storage에 업로드합니다.

```csharp
// AddressableFirebaseUploader.cs

private async void StartUploadWithCleanup(List<string> filesToUpload)
{
    // ...
    AddLog("🗑️ Firebase Storage 경로 정리 중...");
    await ClearFirebaseStoragePath();
    
    uploadStatus = "업로드 준비 중...";
    await uploaderService.StartUpload(filesToUpload);
    // ...
}
```
