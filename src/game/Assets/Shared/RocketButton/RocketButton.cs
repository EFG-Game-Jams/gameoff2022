using UnityEngine;
using UnityEngine.Events;

public class RocketButton : MonoBehaviour
{
    [SerializeField] string title;
    [SerializeField] bool alwaysDisplayTitle;
    [SerializeField] bool doNotConsumeRocket;

    [Space]

    [SerializeField] public UnityEvent<Vector3> onTrigger;
    [SerializeField] public UnityEvent onHoverEnter;
    [SerializeField] public UnityEvent onHoverExit;

    [Space]

    [SerializeField] Material materialIdle;
    [SerializeField] Material materialHover;
    [SerializeField] MeshRenderer mesh;
    [SerializeField] TMPro.TextMeshPro text;

    public bool ShouldConsumeRocket => !doNotConsumeRocket;

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
        text.gameObject.SetActive(true);
        onHoverEnter?.Invoke();
    }
    public void OnHoverExit()
    {
        mesh.sharedMaterial = materialIdle;
        text.gameObject.SetActive(alwaysDisplayTitle);
        onHoverExit?.Invoke();
    }
    public void OnRocketImpact(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        onTrigger?.Invoke(localPosition);
    }

    private void OnValidate()
    {
        if (text != null)
            text.text = title;
    }

    private void Awake()
    {
        text.gameObject.SetActive(alwaysDisplayTitle);
    }
}
