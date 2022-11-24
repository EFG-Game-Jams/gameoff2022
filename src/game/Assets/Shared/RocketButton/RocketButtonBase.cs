using UnityEngine;
using UnityEngine.Events;

public class RocketButtonBase : MonoBehaviour
{
    [SerializeField] bool doNotConsumeRocket;
    [SerializeField] public UnityEvent<Vector3> onTrigger;
    [SerializeField] public UnityEvent onHoverEnter;
    [SerializeField] public UnityEvent onHoverExit;

    public bool ShouldConsumeRocket => !doNotConsumeRocket;

    public static RocketButtonBase FromCollider(Collider collider)
    {
        Transform t = collider.transform;
        if (t.parent == null)
            return null;
        return t.parent.GetComponent<RocketButtonBase>();
    }

    public virtual void OnHoverEnter()
    {
        onHoverEnter?.Invoke();
    }
    public virtual void OnHoverExit()
    {
        onHoverExit?.Invoke();
    }
    public virtual void OnTrigger(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        onTrigger?.Invoke(localPosition);
    }
}
