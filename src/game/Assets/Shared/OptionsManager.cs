using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class OptionsManager : MonoBehaviour
{
    private const string PersistentDataName = "data_options";
    private const float SaveTimeout = 5;

    public GameOptions Options { get; private set; }    

    private static OptionsManager instance;
    private float nextScheduledSave = 0;

    public static OptionsManager GetOrCreate()
    {
        if (instance == null)
        {
            GameObject holder = new GameObject("OptionsManager");
            DontDestroyOnLoad(holder);

            instance = holder.AddComponent<OptionsManager>();
            instance.Load();
        }
        return instance;
    }

    private void Load()
    {
        Options ??= new();
        PersistentDataUtil.TryLoad(PersistentDataName, Options, e => Debug.LogWarning($"OptionsManager.Load exception : {e.Message}"));
    }

    public void Save()
    {
        Options.Validate();
        if (nextScheduledSave <= 0)
            StartCoroutine(CoScheduledSave());
        nextScheduledSave = Time.realtimeSinceStartup + SaveTimeout;
    }

    public void ResetToDefaults()
    {
        Options = new();
        PersistentDataUtil.TryDelete(PersistentDataName, e => Debug.LogWarning($"OptionsManager.ResetToDefaults exception : {e.Message}"));
    }

    private IEnumerator CoScheduledSave()
    {
        do
            yield return null;
        while (Time.realtimeSinceStartup < nextScheduledSave);

        nextScheduledSave = 0;
        Options.Validate();
        PersistentDataUtil.TrySave(PersistentDataName, Options, e => Debug.LogWarning($"OptionsManager.Save exception : {e.Message}"));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInitialiseOnLoad()
    {
        GetOrCreate();
        Debug.Assert(instance != null);
    }
}
