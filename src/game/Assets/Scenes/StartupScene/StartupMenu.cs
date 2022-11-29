using System.Collections;
using UnityEngine;

public class StartupMenu : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup menu;

    [SerializeField]
    private CanvasGroup errorMenu;

    [SerializeField]
    private AnimationCurve animationCurve;

    [SerializeField]
    private string editorSessionSecret = string.Empty;

    private Coroutine onlineCoroutine = null;

    // Start is called before the first frame update
    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;

        if (!Application.isEditor && !WebFunctions.HasLocalStorage())
        {
            StartCoroutine(AnimateMenuIn(errorMenu));
        }
        else
        {
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
                        StartCoroutine(AnimateMenuIn(menu));
                    }
                }
                else
                {
                    GoOffline();
                }
            }));
        }
    }

    public void GoOnline()
    {
        if (onlineCoroutine != null)
        {
            return;
        }

        var client = LeaderboardClient.GetClient();
        if (Application.isEditor)
        {
            if (string.IsNullOrWhiteSpace(editorSessionSecret))
            {
                GoOffline();
                return;
            }

            onlineCoroutine = StartCoroutine(client.ConnectAsEditor(editorSessionSecret, (_) => GotoNextScene()));
            return;
        }
        else
        {
            onlineCoroutine = StartCoroutine(client.Connect((_) => GotoNextScene()));
        }
    }

    public void GoOffline()
    {
        if (onlineCoroutine != null)
        {
            StopCoroutine(onlineCoroutine);
        }

        var client = LeaderboardClient.GetClient();
        client.DisableOnlineLeaderboard();
        GotoNextScene();
    }

    private void GotoNextScene() => SceneBase.SwitchScene("HubScene");

    private IEnumerator AnimateMenuIn(CanvasGroup group)
    {
        float elapsed = 0f;
        while (group.alpha < animationCurve[animationCurve.length - 1].time)
        {
            group.alpha = animationCurve.Evaluate(elapsed);
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        group.enabled = true;
        group.alpha = 1f;
        group.blocksRaycasts = true;
        yield break;
    }
}
