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

    // Timer showing the reloading of ready rockets
    public float rocketReloadTime;

    // Timer showing the regeneration of rockets
    public float rocketRegenTime;

    // Level number
    public int levelNumber;
}
