using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubRaceEntranceScreenInfo : HubRaceEntranceScreen
{
    [SerializeField] TMPro.TextMeshProUGUI textTitle;
    [SerializeField] TMPro.TextMeshProUGUI textTimeLast;
    [SerializeField] TMPro.TextMeshProUGUI textTimeBest;

    public void Configure(string title, string timeLast, string timeBest)
    {
        textTitle.text = title;
        textTimeLast.text = FormatMonoText(timeLast);
        textTimeBest.text = FormatMonoText(timeBest);
    }

    public void SetTitle(string s) => textTitle.text = s;
    public void SetTimeLast(string s) => textTimeLast.text = FormatMonoText(s);
    public void SetTimeBest(string s) => textTimeBest.text = FormatMonoText(s);

    /*protected override void Start()
    {
        base.Start();
    }*/
}
