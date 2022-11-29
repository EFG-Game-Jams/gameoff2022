using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public PlayerData playerData;
    
    [Header("Player")]
    public TMP_Text speedText;
    public Image speedCircle;
    public RawImage altitudeImage;

    [Header("Rockets")] 
    public TMP_Text rocketCount;
    public Image rocketRegenCircle;

    [Header("Weapon")] 
    public Sprite rocketLauncherEmtpy;
    public Sprite rocketLauncherLoaded;
    public Image rocketReloadCircle;
    public Image rocketLauncher;
    public Image rocketLauncherCharge;

    [Header("LevelNumber")] 
    public TMP_Text levelNumberText;
    public GameObject levelNumberHolder;
    
    [Header("LevelTimer")] 
    public TMP_Text levelTimerText;
    public GameObject levelTimerHolder;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // Speed indicator calculation
        speedText.text = $"<mspace=18>{playerData.speed.ToString("0.0") + " m/s"}</mspace>";;
        float speedCircleFillAmount =
            Mathf.Clamp01(playerData.speed / playerData.maxSpeed);
        speedCircle.fillAmount = speedCircleFillAmount;

        // Altitude image UV offset
        Rect altUvRect = altitudeImage.uvRect;
        altUvRect.y = altUvRect.height * .5f;
        altUvRect.y += playerData.altitude * .1f;
        altitudeImage.uvRect = altUvRect;

        // Rocket count indicator calculation
        rocketCount.text = playerData.rocketReadyCount.ToString();
        float rocketsRegenFillAmount =
            Mathf.Clamp01(playerData.rocketRegenTime);
        rocketRegenCircle.fillAmount = rocketsRegenFillAmount;
        
        // Rocket launcher indicator calculation
        float rockerReloadFillAmount =
            Mathf.Clamp01(playerData.rocketReloadTime);
        rocketReloadCircle.fillAmount = rockerReloadFillAmount;
        rocketLauncher.sprite = playerData.isRocketLoaded ? rocketLauncherLoaded : rocketLauncherEmtpy;

        // Shot charge
        rocketLauncherCharge.fillAmount = Mathf.Clamp01(playerData.shotCharge);
        
        // Level number
        levelNumberHolder.SetActive(!string.IsNullOrEmpty(playerData.levelNumberText));
        levelNumberText.text = playerData.levelNumberText;

        // Level timer
        levelTimerHolder.SetActive(!string.IsNullOrEmpty(playerData.levelTimerText));
        levelTimerText.text = $"<mspace=30>{playerData.levelTimerText}</mspace>";
    }
}
