using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SatriProtoTriggerArea : MonoBehaviour
{
    [SerializeField] string filterTag;

    public UnityEvent<GameObject> onTriggerEnter;
    public UnityEvent<GameObject> onTriggerExit;

    private void OnTriggerEnter(Collider other)
    {
        if (filterTag.Length == 0 || other.gameObject.CompareTag(filterTag))
            onTriggerEnter?.Invoke(other.gameObject);
    }
    private void OnTriggerExit(Collider other)
    {
        if (filterTag.Length == 0 || other.gameObject.CompareTag(filterTag))
            onTriggerExit?.Invoke(other.gameObject);
    }
}
