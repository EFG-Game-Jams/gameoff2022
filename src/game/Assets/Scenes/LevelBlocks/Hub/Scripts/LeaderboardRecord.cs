using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardRecord : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textRank;
    [SerializeField] TextMeshProUGUI textPlayer;
    [SerializeField] TextMeshProUGUI textTime;

    public LeaderboardListEntry Record { get; private set; }

    public void SetRecord(LeaderboardListEntry record, int rank, bool isLocalPlayer)
    {
        Record = record;

        textRank.text = rank.ToString();
        textPlayer.text = record.playerName;

        System.TimeSpan timeSpan = System.TimeSpan.FromMilliseconds(record.timeInMilliseconds);
        textTime.text = string.Format("<mspace=.05>{0:D2}:{1:D2}.{2:D3}</mspace>", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);

        Color color = (isLocalPlayer ? new Color(1f, .5f, .125f) : Color.white);
        textRank.color = color;
        textPlayer.color = color;
        textTime.color = color;
    }
}
