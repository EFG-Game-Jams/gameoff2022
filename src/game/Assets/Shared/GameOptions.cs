using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameOptions
{
    public float fieldOfView = 75;

    public float volumeMaster = .5f;
    public float volumeEffects = 1f;
    public float volumeMusic = .05f;

    public void Validate()
    {
        fieldOfView = Mathf.Clamp(fieldOfView, 60f, 110f);
        volumeMaster = Mathf.Clamp01(volumeMaster);
        volumeEffects = Mathf.Clamp01(volumeEffects);
        volumeMusic = Mathf.Clamp01(volumeMusic);
    }
}
