using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelBounds : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<GameObject, double> onPlayerOutOfBounds;

    [Header("Auto-sizing (use menu option to apply)")]
    [SerializeField] Vector3 padMin;
    [SerializeField] Vector3 padMax;

    [ContextMenu("Auto size")]
    private void SetAutoSize()
    {
        gameObject.SetActive(false);

        Collider[] allColliders = FindObjectsOfType<Collider>();
        if (allColliders.Length > 0)
        {
            Bounds bounds = allColliders[0].bounds;
            for (int i = 1; i < allColliders.Length; ++i)
                bounds.Encapsulate(allColliders[i].bounds);

            bounds.min -= padMin;
            bounds.max += padMax;

            transform.localScale = bounds.size;
            transform.position = bounds.min;
        }
        else
        {
            transform.localScale = Vector3.one;
            transform.position = Vector3.zero;
        }

        gameObject.SetActive(true);
    }

    private void Awake()
    {
        GetComponentInChildren<SatriProtoTriggerArea>().onTriggerEnter.AddListener(OnPlayerOutOfBounds);
    }
    private void OnPlayerOutOfBounds(GameObject gameObject, double fixedTime)
    {
        onPlayerOutOfBounds?.Invoke(gameObject, fixedTime);
    }
}
