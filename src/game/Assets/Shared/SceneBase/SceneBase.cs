using Replay;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(int.MinValue)]
public class SceneBase : MonoBehaviour
{
    // we want stuff to be accessible even within Awake callbacks, so make sure everything is serialised
    [SerializeField] ReplaySystem replaySystem;
    [SerializeField] AudioMixer audioMixer;

    // public accessors
    public ReplaySystem ReplaySystem => replaySystem;

    // scene-switch helper
    private static System.Action onSceneChangeAction;

    // active scene info
    public static string ActiveSceneName => SceneManager.GetActiveScene().name;
    public static SceneBase Current { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InnitialiseOnLoad()
    {
        onSceneChangeAction = null;
        Current = null;
    }

    public static void SwitchScene(int sceneBuildIndex, System.Action onSwitched = null)
    {
        onSceneChangeAction = onSwitched;
        SceneManager.LoadScene(sceneBuildIndex);
    }
    public static void SwitchScene(string sceneName, System.Action onSwitched = null)
    {
        onSceneChangeAction = onSwitched;
        SceneManager.LoadScene(sceneName);
    }
    public static void ReloadScene(System.Action onSwitched = null)
    {
        SwitchScene(SceneManager.GetActiveScene().buildIndex, onSwitched);
    }

    private void Awake()
    {
        Current = this;
        onSceneChangeAction?.Invoke();
        onSceneChangeAction = null;
    }

    private void OnDestroy()
    {
        Current = null;
    }

    private float VolumeToAttenuation(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        return Mathf.Log10(volume) * 20f;
    }

    private void Update()
    {
        // apply audio settings
        GameOptions options = OptionsManager.GetOrCreate().Options;
        audioMixer.SetFloat("VolumeMaster", VolumeToAttenuation(options.volumeMaster));
        audioMixer.SetFloat("VolumeEffects", VolumeToAttenuation(options.volumeEffects));
        audioMixer.SetFloat("VolumeMusic", VolumeToAttenuation(options.volumeMusic));
    }
}
