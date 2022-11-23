using System.Runtime.InteropServices;

public static class WebFunctions
{
    [DllImport("__Internal")]
    public static extern void RedirectToItchAuthorizationPage(string leaderboardBaseUrl);

    [DllImport("__Internal")]
    public static extern bool GetLeaderboardsDisabled();

    [DllImport("__Internal")]
    public static extern void PersistLeaderboardsEnabled(bool enabled);

    [DllImport("__Internal")]
    public static extern string GetLeaderboardSessionGuid();

    [DllImport("__Internal")]
    public static extern void PersistLeaderboardSessionGuid(string guid);

    [DllImport("__Internal")]
    public static extern string ExtractSessionGuidFromFragment();
}