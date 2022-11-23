using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubRaceEntranceScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;

    public float Opacity
    {
        get => canvasGroup.alpha;
        set
        {
            canvasGroup.gameObject.SetActive(value > 0);
            canvasGroup.alpha = value;
        }
    }

    protected virtual void Start()
    {
        Opacity = 0;
    }

    protected string FormatMonoText(string text)
    {
        return $"<mspace=.1>{text}</mspace>";
    }
}
