using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    public UnityEvent<GameObject, double> onTriggerEnter;

    [SerializeField] bool oneShot = true;
    [SerializeField] bool hasTriggered = false;

    public bool HasTriggered => hasTriggered;

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    public void OnEnter(GameObject go, double fixedTime)
    {
        if (oneShot && hasTriggered)
            return;

        hasTriggered = true;
        onTriggerEnter?.Invoke(go, fixedTime);
    }
}
