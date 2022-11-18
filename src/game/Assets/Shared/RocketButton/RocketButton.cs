using UnityEngine;
using UnityEngine.Events;

public class RocketButton : MonoBehaviour
{
    [SerializeField] Material materialIdle;
    [SerializeField] Material materialHover;
    [SerializeField] MeshRenderer mesh;

    public UnityEvent onTrigger;

    public static RocketButton FromCollider(Collider collider)
    {
        Transform t = collider.transform;
        if (t.parent == null)
            return null;
        return t.parent.GetComponent<RocketButton>();
    }

    public void OnHoverEnter()
    {
        mesh.sharedMaterial = materialHover;
    }
    public void OnHoverExit()
    {
        mesh.sharedMaterial = materialIdle;
    }
    public void OnRocketImpact()
    {
        onTrigger?.Invoke();
    }
}
