using UnityEngine;
using Steamworks;


public class UserAuth : MonoBehaviourSingleton<UserAuth>
{
    private bool hasLoggedInfo = false;
    protected override bool DontDestroy => base.DontDestroy;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    public void Initialize()
    {
        if (SteamManager.Initialized && !hasLoggedInfo)
        {
            LogInSteamUserInfo();
            hasLoggedInfo = true;
        }
    }

    private void LogInSteamUserInfo()
    {
        try
        {
            CSteamID steamID = SteamUser.GetSteamID();
            Debug.Log($"[SteamLogIn] : Steam ID : {steamID}");

            string personaName = SteamFriends.GetPersonaName();
            Debug.Log($"[SteamLogIn] UserName : {personaName}");
            int level = SteamUser.GetPlayerSteamLevel();
            Debug.Log($"[SteamLogIn] level : {level}");

            var isVAC = SteamUser.BIsBehindNAT();
            var isSubscribe = SteamApps.BIsSubscribed();
            Debug.Log($"[SteamLogIn] VAC : {isVAC}  | SubScribe : {isSubscribe}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SteamLogIn] Steam Info Bring Fail {e.Message}");
        }
    }
}