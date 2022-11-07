using Replay;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(int.MinValue)]
public class SceneBase : MonoBehaviour
{
    // we want stuff to be accessible even within Awake callbacks, so make sure everything is serialised
    [SerializeField] ReplaySystem replaySystem;
    public ReplaySystem ReplaySystem => replaySystem;

    // scene-switch helper
    private static System.Action onSceneChangeAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ClearSceneChangeAction() => onSceneChangeAction = null;

    public static void SwitchScene(int sceneBuildIndex, System.Action onSwitched = null)
    {
        onSceneChangeAction = onSwitched;
        SceneManager.LoadScene(sceneBuildIndex);
    }
    public static void ReloadScene(System.Action onSwitched = null)
    {
        SwitchScene(SceneManager.GetActiveScene().buildIndex, onSwitched);
    }

    private void Awake()
    {
        onSceneChangeAction?.Invoke();
        onSceneChangeAction = null;
    }
}
