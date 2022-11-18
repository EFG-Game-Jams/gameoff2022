using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InversePerspectiveScale : MonoBehaviour
{
    [SerializeField] Camera referenceCamera;
    [SerializeField] float referenceScale;
    [SerializeField] float referenceScaleDistance;
    [SerializeField] float maxScale;

    void LateUpdate()
    {
        float distance = Vector3.Distance(referenceCamera.transform.position, transform.position);
        float scaleUnclamped = referenceScale * distance;
        float scale = Mathf.Clamp(scaleUnclamped, referenceScale, maxScale);
        transform.localScale = Vector3.one * scale;
    }
}
