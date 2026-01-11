using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Networking;

namespace penta.Editor
{
    /// <summary>Addressable 빌드 및 Firebase Storage 업로드를 통합 관리하는 에디터 툴</summary>
    public class AddressableFirebaseUploader : EditorWindow
    {
        private FirebaseUploaderSettings settings = new FirebaseUploaderSettings();
        private FirebaseUploaderService uploaderService;
        
        private bool isUploading = false;
        private string uploadStatus = "";
        private int totalFiles = 0;
        private int uploadedFiles = 0;
        
        private Vector2 scrollPosition;
        private List<string> uploadLog = new List<string>();

        [MenuItem("PentaShield/Addressable Firebase Uploader")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressableFirebaseUploader>("Addressable Firebase Uploader");
            window.minSize = new Vector2(600, 700);
        }

        private void OnEnable()
        {
            settings.Load();
            
            if (string.IsNullOrEmpty(settings.AddressableBuildPath))
            {
                string foundPath = FindBuildPath();
                if (foundPath != null)
                {
                    settings.AddressableBuildPath = foundPath;
                    AddLog($"✅ Addressable 빌드 경로 자동 감지: {foundPath}");
                }
                else
                {
                    AddLog("⚠️ Addressable 빌드 경로를 자동으로 찾을 수 없습니다. 수동으로 선택해주세요.");
                }
            }

            uploaderService = new FirebaseUploaderService(settings);
            uploaderService.OnLog += AddLog;
            uploaderService.OnProgress += (uploaded, total) =>
            {
                uploadedFiles = uploaded;
                totalFiles = total;
                uploadStatus = $"업로드 중... ({uploaded}/{total})";
                Repaint();
            };
            uploaderService.OnComplete += () =>
            {
                uploadStatus = "✅ 업로드 완료!";
                EditorUtility.DisplayDialog("업로드 완료", $"총 {uploadedFiles}개 파일이 성공적으로 업로드되었습니다!", "확인");
                Repaint();
            };
            uploaderService.OnError += (error) =>
            {
                uploadStatus = "❌ 업로드 실패";
                EditorUtility.DisplayDialog("업로드 실패", $"업로드 중 오류가 발생했습니다:\n{error}", "확인");
                Repaint();
            };
        }

        private void OnDisable()
        {
            settings.Save();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            DrawFirebaseSettings();
            EditorGUILayout.Space();
            
            DrawAddressableSettings();
            EditorGUILayout.Space();
            
            DrawUploadSection();
            EditorGUILayout.Space();
            
            DrawLogSection();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>Firebase Storage 설정 UI를 그립니다</summary>
        private void DrawFirebaseSettings()
        {
            EditorGUILayout.LabelField("🔥 Firebase Storage 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.LabelField("Firebase Project ID:", EditorStyles.label);
                settings.FirebaseProjectId = EditorGUILayout.TextField(settings.FirebaseProjectId);
                
                EditorGUILayout.LabelField("Firebase API Key:", EditorStyles.label);
                settings.FirebaseApiKey = EditorGUILayout.PasswordField(settings.FirebaseApiKey);
                
                EditorGUILayout.LabelField("Storage Bucket:", EditorStyles.label);
                settings.StorageBucket = EditorGUILayout.TextField(settings.StorageBucket);
                
                EditorGUILayout.LabelField("업로드 경로 (Firebase Storage):", EditorStyles.label);
                settings.UploadPath = EditorGUILayout.TextField(settings.UploadPath);
                
                if (!settings.UploadPath.EndsWith("/") && !string.IsNullOrEmpty(settings.UploadPath))
                {
                    settings.UploadPath += "/";
                }
                
                EditorGUILayout.HelpBox($"파일들이 gs://{settings.StorageBucket}/{settings.UploadPath} 경로에 업로드됩니다.", MessageType.Info);
                
                if (GUILayout.Button("Firebase Console에서 설정 확인"))
                {
                    Application.OpenURL($"https://console.firebase.google.com/project/{settings.FirebaseProjectId}/storage");
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>Addressable 빌드 설정 UI를 그립니다</summary>
        private void DrawAddressableSettings()
        {
            EditorGUILayout.LabelField("📦 Addressable 빌드 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.LabelField("Addressable 빌드 폴더:", EditorStyles.label);
                EditorGUILayout.BeginHorizontal();
                {
                    settings.AddressableBuildPath = EditorGUILayout.TextField(settings.AddressableBuildPath);
                    if (GUILayout.Button("찾기", GUILayout.Width(50)))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("Addressable 빌드 폴더 선택", settings.AddressableBuildPath, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            settings.AddressableBuildPath = selectedPath;
                        }
                    }
                    if (GUILayout.Button("자동", GUILayout.Width(50)))
                    {
                        string foundPath = FindBuildPath();
                        if (foundPath != null)
                        {
                            settings.AddressableBuildPath = foundPath;
                            AddLog($"✅ Addressable 빌드 경로 자동 감지: {foundPath}");
                        }
                        else
                        {
                            AddLog("⚠️ Addressable 빌드 경로를 자동으로 찾을 수 없습니다.");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                settings.IncludeSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", settings.IncludeSubfolders);
                settings.OverwriteExisting = EditorGUILayout.Toggle("기존 파일 덮어쓰기", settings.OverwriteExisting);
                
                if (!string.IsNullOrEmpty(settings.AddressableBuildPath) && Directory.Exists(settings.AddressableBuildPath))
                {
                    var files = FirebaseUploaderService.GetFilesToUpload(settings.AddressableBuildPath, settings.IncludeSubfolders);
                    EditorGUILayout.HelpBox($"업로드할 파일: {files.Count}개\n경로: {settings.AddressableBuildPath}", MessageType.Info);
                    
                    if (files.Count > 0 && files.Count <= 10)
                    {
                        EditorGUILayout.LabelField("파일 목록 미리보기:", EditorStyles.miniLabel);
                        foreach (var file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            EditorGUILayout.LabelField($"  • {fileName}", EditorStyles.miniLabel);
                        }
                    }
                    else if (files.Count > 10)
                    {
                        EditorGUILayout.LabelField($"파일이 너무 많아 미리보기를 생략합니다. (총 {files.Count}개)", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("유효하지 않은 경로입니다.", MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>업로드 섹션 UI를 그립니다</summary>
        private void DrawUploadSection()
        {
            EditorGUILayout.LabelField("🔧 Addressable 관리", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUI.enabled = !isUploading;
                    
                    if (GUILayout.Button("🗑️ 캐시 삭제", GUILayout.Height(30)))
                    {
                        ClearBuildCache();
                    }
                    
                    if (GUILayout.Button("🔨 빌드 (로컬 정리 후)", GUILayout.Height(30)))
                    {
                        BuildWithCleanup();
                    }
                    
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🚀 Firebase 업로드", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUI.enabled = !isUploading && settings.IsValid();
                
                if (GUILayout.Button("🔥 Firebase Storage에 업로드 (기존 파일 삭제 후)", GUILayout.Height(40)))
                {
                    var filesToUpload = FirebaseUploaderService.GetFilesToUpload(settings.AddressableBuildPath, settings.IncludeSubfolders);
                    
                    if (EditorUtility.DisplayDialog("업로드 확인", 
                        $"Firebase Storage의 {settings.UploadPath} 경로를 비우고\n" +
                        $"총 {filesToUpload.Count}개 파일을 새로 업로드하시겠습니까?\n\n" +
                        $"대상: gs://{settings.StorageBucket}/{settings.UploadPath}", 
                        "업로드", "취소"))
                    {
                        StartUploadWithCleanup(filesToUpload);
                    }
                }
                
                GUI.enabled = true;
                
                if (isUploading)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"상태: {uploadStatus}");
                    
                    if (totalFiles > 0)
                    {
                        float progress = (float)uploadedFiles / totalFiles;
                        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{uploadedFiles}/{totalFiles} 파일 업로드됨 ({progress:P1})");
                    }
                    
                    EditorGUILayout.Space();
                    
                    if (GUILayout.Button("❌ 업로드 중단"))
                    {
                        uploaderService?.Cancel();
                        isUploading = false;
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("📁 빌드 폴더 열기"))
                    {
                        if (Directory.Exists(settings.AddressableBuildPath))
                        {
                            EditorUtility.RevealInFinder(settings.AddressableBuildPath);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>로그 섹션 UI를 그립니다</summary>
        private void DrawLogSection()
        {
            EditorGUILayout.LabelField("📝 업로드 로그", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("🗑️ 로그 지우기", GUILayout.Width(100)))
                    {
                        uploadLog.Clear();
                    }
                    
                    if (GUILayout.Button("💾 로그 저장", GUILayout.Width(100)))
                    {
                        SaveLogToFile();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                {
                    foreach (string log in uploadLog)
                    {
                        EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
                    }
                    
                    if (uploadLog.Count == 0)
                    {
                        EditorGUILayout.LabelField("로그가 없습니다.", EditorStyles.centeredGreyMiniLabel);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>Firebase Storage 경로를 비운 후 파일을 업로드합니다</summary>
        private async void StartUploadWithCleanup(List<string> filesToUpload)
        {
            if (isUploading) return;

            isUploading = true;
            uploadedFiles = 0;
            totalFiles = filesToUpload.Count;
            uploadStatus = "Firebase Storage 정리 중...";

            try
            {
                AddLog("🗑️ Firebase Storage 경로 정리 중...");
                await ClearFirebaseStoragePath();
                
                uploadStatus = "업로드 준비 중...";
                await uploaderService.StartUpload(filesToUpload);
            }
            catch (Exception e)
            {
                AddLog($"❌ 오류 발생: {e.Message}");
                EditorUtility.DisplayDialog("오류", $"업로드 중 오류가 발생했습니다:\n{e.Message}", "확인");
            }
            finally
            {
                isUploading = false;
                Repaint();
            }
        }

        /// <summary>로컬 빌드 디렉토리를 정리한 후 Addressable을 빌드합니다</summary>
        private void BuildWithCleanup()
        {
            if (EditorUtility.DisplayDialog("빌드 확인", 
                "로컬 빌드 디렉토리를 정리하고 새로 빌드하시겠습니까?", 
                "빌드", "취소"))
            {
                if (string.IsNullOrEmpty(settings.AddressableBuildPath))
                {
                    string foundPath = FindBuildPath();
                    if (foundPath != null)
                    {
                        settings.AddressableBuildPath = foundPath;
                        AddLog($"📁 빌드 경로 자동 설정: {foundPath}");
                    }
                    else
                    {
                        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                        if (addressableSettings != null)
                        {
                            var buildPath = addressableSettings.profileSettings.GetValueByName(addressableSettings.activeProfileId, "Remote.BuildPath");
                            if (!string.IsNullOrEmpty(buildPath))
                            {
                                settings.AddressableBuildPath = buildPath;
                                AddLog($"📁 Addressable 설정에서 빌드 경로 가져옴: {buildPath}");
                            }
                        }
                    }
                }

                AddLog("🗑️ 로컬 빌드 디렉토리 정리 중...");
                ClearLocalBuildDirectory();
                
                AddLog("🔨 Addressable 빌드 시작...");
                if (BuildPlayerContent())
                {
                    AddLog("✅ Addressable 빌드 완료!");
                    string foundPath = FindBuildPath();
                    if (foundPath != null)
                    {
                        settings.AddressableBuildPath = foundPath;
                        AddLog($"📁 빌드 경로: {foundPath}");
                    }
                }
                else
                {
                    AddLog("❌ Addressable 빌드 실패");
                }
            }
        }

        /// <summary>Addressable 빌드 캐시를 삭제합니다</summary>
        private void ClearBuildCache()
        {
            if (EditorUtility.DisplayDialog("캐시 삭제 확인", 
                "Addressable 빌드 캐시를 삭제하시겠습니까?", 
                "삭제", "취소"))
            {
                string[] cachePaths = {
                    "Library/com.unity.addressables",
                    "ServerData",
                    "ExportAb"
                };

                int deletedCount = 0;
                foreach (string relativePath in cachePaths)
                {
                    string fullPath = Path.Combine(Application.dataPath, "..", relativePath);
                    if (Directory.Exists(fullPath))
                    {
                        try
                        {
                            Directory.Delete(fullPath, true);
                            AddLog($"✅ 캐시 삭제: {relativePath}");
                            deletedCount++;
                        }
                        catch (Exception e)
                        {
                            AddLog($"⚠️ 캐시 삭제 실패 ({relativePath}): {e.Message}");
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    AssetDatabase.Refresh();
                    AddLog($"✅ {deletedCount}개 캐시 폴더 정리 완료");
                }
                else
                {
                    AddLog("정리할 캐시가 없습니다.");
                }
            }
        }

        /// <summary>로컬 빌드 디렉토리의 모든 파일을 삭제합니다</summary>
        private void ClearLocalBuildDirectory()
        {
            if (string.IsNullOrEmpty(settings.AddressableBuildPath) || !Directory.Exists(settings.AddressableBuildPath))
            {
                AddLog("⚠️ 빌드 디렉토리가 설정되지 않았거나 존재하지 않습니다.");
                return;
            }

            try
            {
                var files = Directory.GetFiles(settings.AddressableBuildPath, "*", SearchOption.AllDirectories);
                int deletedCount = 0;

                foreach (string file in files)
                {
                    if (!file.EndsWith(".meta"))
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                var directories = Directory.GetDirectories(settings.AddressableBuildPath, "*", SearchOption.AllDirectories);
                foreach (string dir in directories.OrderByDescending(d => d.Length))
                {
                    if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                    {
                        Directory.Delete(dir);
                    }
                }

                AddLog($"✅ 로컬 빌드 디렉토리 정리 완료 ({deletedCount}개 파일 삭제)");
            }
            catch (Exception e)
            {
                AddLog($"❌ 로컬 빌드 디렉토리 정리 실패: {e.Message}");
            }
        }

        /// <summary>Firebase Storage의 업로드 경로에 있는 모든 파일을 삭제합니다</summary>
        private async Task ClearFirebaseStoragePath()
        {
            try
            {
                string listUrl = $"https://firebasestorage.googleapis.com/v0/b/{settings.StorageBucket}/o?prefix={Uri.EscapeDataString(settings.UploadPath)}";
                if (!string.IsNullOrEmpty(settings.FirebaseApiKey))
                {
                    listUrl += $"&key={settings.FirebaseApiKey}";
                }

                using (UnityWebRequest request = UnityWebRequest.Get(listUrl))
                {
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Delay(50);
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<FirebaseStorageListResponse>(request.downloadHandler.text);
                        if (response != null && response.items != null && response.items.Length > 0)
                        {
                            AddLog($"🗑️ {response.items.Length}개 파일 삭제 중...");
                            
                            int deletedCount = 0;
                            foreach (var item in response.items)
                            {
                                string deleteUrl = $"https://firebasestorage.googleapis.com/v0/b/{settings.StorageBucket}/o/{Uri.EscapeDataString(item.name)}";
                                if (!string.IsNullOrEmpty(settings.FirebaseApiKey))
                                {
                                    deleteUrl += $"?key={settings.FirebaseApiKey}";
                                }

                                using (UnityWebRequest deleteRequest = UnityWebRequest.Delete(deleteUrl))
                                {
                                    var deleteOperation = deleteRequest.SendWebRequest();
                                    while (!deleteOperation.isDone)
                                    {
                                        await Task.Delay(50);
                                    }

                                    if (deleteRequest.result == UnityWebRequest.Result.Success)
                                    {
                                        deletedCount++;
                                    }
                                }
                            }

                            AddLog($"✅ Firebase Storage 정리 완료 ({deletedCount}/{response.items.Length}개 파일 삭제)");
                        }
                        else
                        {
                            AddLog("✅ Firebase Storage 경로가 이미 비어있습니다.");
                        }
                    }
                    else
                    {
                        AddLog($"⚠️ Firebase Storage 파일 목록 조회 실패: {request.error}");
                    }
                }
            }
            catch (Exception e)
            {
                AddLog($"⚠️ Firebase Storage 정리 중 오류: {e.Message}");
            }
        }

        /// <summary>Addressable Player Content를 빌드합니다</summary>
        private bool BuildPlayerContent()
        {
            try
            {
                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (addressableSettings == null)
                {
                    AddLog("❌ AddressableAssetSettings를 찾을 수 없습니다.");
                    return false;
                }

                AddressableAssetSettings.BuildPlayerContent();
                return true;
            }
            catch (Exception e)
            {
                AddLog($"❌ 빌드 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>Addressable 빌드 경로를 자동으로 찾습니다</summary>
        private string FindBuildPath()
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings != null)
            {
                try
                {
                    var remoteBuildPath = addressableSettings.profileSettings.GetValueByName(addressableSettings.activeProfileId, "Remote.BuildPath");
                    if (!string.IsNullOrEmpty(remoteBuildPath) && Directory.Exists(remoteBuildPath))
                    {
                        return Path.GetFullPath(remoteBuildPath);
                    }

                    var localBuildPath = addressableSettings.profileSettings.GetValueByName(addressableSettings.activeProfileId, "Local.BuildPath");
                    if (!string.IsNullOrEmpty(localBuildPath) && Directory.Exists(localBuildPath))
                    {
                        return Path.GetFullPath(localBuildPath);
                    }
                }
                catch (Exception e)
                {
                    AddLog($"⚠️ Addressable 설정에서 경로 가져오기 실패: {e.Message}");
                }
            }

            string[] possiblePaths = {
                Path.Combine(Application.dataPath, "../ServerData"),
                Path.Combine(Application.dataPath, "../ExportAb/Android"),
                Path.Combine(Application.dataPath, "../ExportAb/iOS"),
                Path.Combine(Application.dataPath, "../AddressableAssetsData"),
                Path.Combine(Application.dataPath, "../Build/AddressableAssets"),
                Path.Combine(Application.dataPath, "../Builds/AddressableAssets")
            };

            foreach (string path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            return null;
        }

        /// <summary>로그를 파일로 저장합니다</summary>
        private void SaveLogToFile()
        {
            try
            {
                string logPath = EditorUtility.SaveFilePanel("로그 저장", "", $"AddressableUpload_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt", "txt");
                
                if (!string.IsNullOrEmpty(logPath))
                {
                    File.WriteAllLines(logPath, uploadLog);
                    AddLog($"💾 로그가 저장되었습니다: {logPath}");
                    
                    EditorUtility.DisplayDialog("로그 저장 완료", $"로그가 저장되었습니다:\n{logPath}", "확인");
                }
            }
            catch (System.Exception e)
            {
                AddLog($"❌ 로그 저장 실패: {e.Message}");
            }
        }

        /// <summary>로그 메시지를 추가합니다</summary>
        private void AddLog(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            uploadLog.Add($"[{timestamp}] {message}");
            
            if (uploadLog.Count > 200) 
            {
                uploadLog.RemoveAt(0);
            }
            
            Debug.Log($"[Addressable Uploader] {message}");
            Repaint();
        }

        [Serializable]
        private class FirebaseStorageListResponse
        {
            public FirebaseStorageItem[] items;
        }

        [Serializable]
        private class FirebaseStorageItem
        {
            public string name;
            public string bucket;
            public string generation;
        }
    }
}
