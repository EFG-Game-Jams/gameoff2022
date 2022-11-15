using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]
public class PlayerData : ScriptableObject {
    public string objectName = "PlayerData";
    public float speed;
    public float maxSpeed;
    public int shotsReady;
    public float reloadTime;
    public float shotsRecharchingTime;
}
