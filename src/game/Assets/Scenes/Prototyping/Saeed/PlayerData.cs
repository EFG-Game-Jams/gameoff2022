using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]
public class PlayerData : ScriptableObject {

    // Shows the speed of player (can be the magnitude)
    public float speed;

    // Max speed which player can reach. Used to scale the speed on UI
    public float maxSpeed;

    // How many rockets player has ready
    public int rocketReadyCount;

    // Timer showing the reloading of ready rockets. Goes from 0(start of reload) to 1(finished) and idle is 0
    public float rocketReloadTime;

    // Timer showing the regeneration of rockets. Goes from 0(start of reload) to 1(finished) and idle is 0
    public float rocketRegenTime;

    // Level number
    public int levelNumber;
    
    // Rocket loaded
    public bool isRocketLoaded;
}
