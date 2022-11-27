using System.Collections;
using UnityEngine;

public class StartupMenu : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup menu;

    [SerializeField]
    private AnimationCurve animationCurve;

    [SerializeField]
    private string editorSessionSecret = string.Empty;

    // Start is called before the first frame update
    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;

        var client = LeaderboardClient.GetClient();

        StartCoroutine(client.CheckServerHealth((healthy) =>
        {
            if (healthy)
            {
                if (client.IsLeaderboardEnabledByUser)
                {
                    GoOnline();
                }
                else if (client.IsLeaderboardDisabledByUser)
                {
                    GoOffline();
                }
                else
                {
                    StartCoroutine(AnimateMenuIn());
                }
            }
            else
            {
                GoOffline();
            }
        }));
    }

    public void GoOnline()
    {
        var client = LeaderboardClient.GetClient();
        if (Application.isEditor)
        {
            if (string.IsNullOrWhiteSpace(editorSessionSecret))
            {
                GoOffline();
                return;
            }

            StartCoroutine(client.ConnectAsEditor(editorSessionSecret, (_) => GotoNextScene()));
            return;
        }
        else
        {
            StartCoroutine(client.Connect((_) => GotoNextScene()));
        }
    }

    public void GoOffline()
    {
        var client = LeaderboardClient.GetClient();
        client.DisableOnlineLeaderboard();
        GotoNextScene();
    }

    private void GotoNextScene() => SceneBase.SwitchScene("HubScene");

    private IEnumerator AnimateMenuIn()
    {
        float elapsed = 0f;
        while (menu.alpha < animationCurve[animationCurve.length - 1].time)
        {
            menu.alpha = animationCurve.Evaluate(elapsed);
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        menu.enabled = true;
        menu.alpha = 1f;
        yield break;
    }
}
