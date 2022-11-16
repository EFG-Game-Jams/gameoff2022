using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public PlayerData playerData;
    
    [Header("Speed")]
    public TMP_Text speedText;
    public Image speedCircle;

    [Header("Rockets")] 
    public TMP_Text rocketCount;
    public Image rocketRegenCircle;

    [Header("Weapon")] 
    public Sprite rocketLauncherEmtpy;
    public Sprite rocketLauncherLoaded;
    public Image rocketReloadCircle;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        speedText.text = playerData.speed.ToString("0.0") + " m/s";

        float speedCircleFillAmount =
            Mathf.Clamp01(playerData.speed / playerData.maxSpeed);
        speedCircle.fillAmount = speedCircleFillAmount;

        rocketCount.text = playerData.rocketReadyCount.ToString();
        float rocketsRegenFillAmount =
            Mathf.Clamp01(playerData.rocketRegenTime);
        rocketRegenCircle.fillAmount = 1 - rocketsRegenFillAmount;
        
        float rockerReloadFillAmount =
            Mathf.Clamp01(playerData.rocketReloadTime);
        rocketRegenCircle.fillAmount = 1 - rockerReloadFillAmount;

    }
}
