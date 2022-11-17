using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SatriProtoTriggerArea : MonoBehaviour
{
    public UnityEvent<GameObject, double> onTriggerEnter;

    private bool hasTriggered;

    public void OnEnter(GameObject go, double fixedTime)
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
        onTriggerEnter?.Invoke(go, fixedTime);
    }
}
