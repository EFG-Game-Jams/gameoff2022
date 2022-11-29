using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]
public class PlayerData : ScriptableObject {

    [Header("Player")]
    // Shows the speed of player (can be the magnitude)
    public float speed;
    // Max speed which player can reach. Used to scale the speed on UI
    public float maxSpeed;
    // Altitude of player
    public float altitude;

    [Header("Rockets")]
    // How many rockets player has ready
    public int rocketReadyCount;
    // Timer showing the regeneration of rockets. Goes from 0(start of reload) to 1(finished) and idle is 0
    public float rocketRegenTime;

    [Header("Launcher")]
    // Timer showing the reloading of ready rockets. Goes from 0(start of reload) to 1(finished) and idle is 0
    public float rocketReloadTime;
    // Rocket loaded
    public bool isRocketLoaded;
    // Shot charge state [0; 1]
    public float shotCharge;

    [Header("Level number")]
    // Level number
    public string levelNumberText;
    
    [Header("Timer")]
    // Timer
    public string levelTimerText;

}
