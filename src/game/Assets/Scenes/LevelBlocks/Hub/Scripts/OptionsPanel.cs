using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OptionsPanel : MonoBehaviour
{
    [SerializeField] OptionSlider fieldOfView;
    [SerializeField] OptionSlider volumeMaster;
    [SerializeField] OptionSlider volumeEffects;
    [SerializeField] OptionSlider volumeMusic;
    [SerializeField] RocketButtonBase buttonResetDefaults;
    [SerializeField] RocketButtonBase buttonGoOnline;
    [SerializeField] TextMeshProUGUI textOnlineStatus;

    void Start()
    {
        OptionsManager optionsManager = OptionsManager.GetOrCreate();

        volumeMaster.onChanged.AddListener(value =>
        {
            optionsManager.Options.volumeMaster = value;
            optionsManager.Save();
        });
        volumeEffects.onChanged.AddListener(value =>
        {
            optionsManager.Options.volumeEffects = value;
            optionsManager.Save();
        });
        volumeMusic.onChanged.AddListener(value =>
        {
            optionsManager.Options.volumeMusic = value;
            optionsManager.Save();
        });
        fieldOfView.onChanged.AddListener(value =>
        {
            optionsManager.Options.fieldOfView = value;
            optionsManager.Save();
            FindObjectOfType<SatriProtoPlayer>().RefreshFieldOfView();
        });

        volumeMaster.SetValue(optionsManager.Options.volumeMaster);
        volumeEffects.SetValue(optionsManager.Options.volumeEffects);
        volumeMusic.SetValue(optionsManager.Options.volumeMusic);
        fieldOfView.SetValue(optionsManager.Options.fieldOfView);

        buttonResetDefaults.onTrigger.AddListener(v =>
        {
            OptionsManager.GetOrCreate().ResetToDefaults();
            volumeMaster.SetValue(optionsManager.Options.volumeMaster);
            volumeEffects.SetValue(optionsManager.Options.volumeEffects);
            volumeMusic.SetValue(optionsManager.Options.volumeMusic);
            fieldOfView.SetValue(optionsManager.Options.fieldOfView);
            FindObjectOfType<SatriProtoPlayer>().RefreshFieldOfView();
        });

        LeaderboardClient client = LeaderboardClient.GetClient();

        bool clientStateSwitch = client.IsServerHealthy && client.IsLeaderboardDisabledByUser;
        buttonGoOnline.gameObject.SetActive(clientStateSwitch);
        textOnlineStatus.gameObject.SetActive(!clientStateSwitch);

        buttonGoOnline.onTrigger.AddListener(v =>
        {
            client.ResetLeaderboardEnabledUserChoice();
            LeaderboardClient.DiscardExistingClient();
            SceneBase.SwitchScene("StartupScene");
        });

        StartCoroutine(CoUpdateOnlineStatus());
    }

    IEnumerator CoUpdateOnlineStatus()
    {
        if (!textOnlineStatus.gameObject.activeSelf)
            yield break;

        LeaderboardClient client = LeaderboardClient.GetClient();

        textOnlineStatus.text = $"Leaderboard Status : Connecting...";
        yield return new WaitForSeconds(10);

        string onlineStatus = null;
        if (!client.IsServerHealthy)
            onlineStatus = "Offline";
        else if (client.IsOffline)
            onlineStatus = "Connection Error";
        else
            onlineStatus = "Online";
        textOnlineStatus.text = $"Leaderboard Status : {onlineStatus}";
    }
}
