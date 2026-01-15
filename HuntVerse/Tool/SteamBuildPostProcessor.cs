using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// 빌드가 끝난 뒤 Steam 관련 필수 파일을 실행 파일 위치로 복사한다.
/// </summary>
public static class SteamBuildPostProcessor
{
    private const string SteamAppIdFileName = "steam_appid.txt";
    private const string SteamApi64DllName = "steam_api64.dll";
    private const string SteamApiDllName = "steam_api.dll";

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        try
        {
            var buildDir = Path.GetDirectoryName(pathToBuiltProject);
            if (string.IsNullOrEmpty(buildDir))
            {
                Debug.LogWarning("[SteamBuildPostProcessor] 빌드 디렉터리를 찾을 수 없습니다.");
                return;
            }

            CopySteamAppId(buildDir);
            CopySteamDll(buildDir, SteamApi64DllName, "Plugins/x86_64");
            CopySteamDll(buildDir, SteamApiDllName, "Plugins/x86");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamBuildPostProcessor] 빌드 후 Steam 파일 복사 실패: {e}");
        }
    }

    private static void CopySteamAppId(string buildDir)
    {
        var source = Path.Combine(Application.dataPath, "StreamingAssets", SteamAppIdFileName);
        var destination = Path.Combine(buildDir, SteamAppIdFileName);

        if (!File.Exists(source))
        {
            Debug.LogWarning($"[SteamBuildPostProcessor] {source} 파일이 없어 복사하지 못했습니다.");
            return;
        }

        File.Copy(source, destination, overwrite: true);
        Debug.Log($"[SteamBuildPostProcessor] {SteamAppIdFileName} 복사 완료 -> {destination}");
    }

    private static void CopySteamDll(string buildDir, string fileName, string relativePluginPath)
    {
        var source = Path.Combine(Application.dataPath, "Plugins", relativePluginPath, fileName);
        if (!File.Exists(source))
        {
            return;
        }

        var destination = Path.Combine(buildDir, fileName);
        File.Copy(source, destination, overwrite: true);
        Debug.Log($"[SteamBuildPostProcessor] {fileName} 복사 완료 -> {destination}");
    }
}

