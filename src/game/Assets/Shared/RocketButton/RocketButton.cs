using UnityEngine;
using UnityEngine.Events;

public class RocketButton : MonoBehaviour
{
    [SerializeField] string title;
    [SerializeField] bool alwaysDisplayTitle;
    [SerializeField] bool doNotConsumeRocket;
    [SerializeField] public UnityEvent onTrigger;

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
    }
    public void OnHoverExit()
    {
        mesh.sharedMaterial = materialIdle;
        text.gameObject.SetActive(alwaysDisplayTitle);
    }
    public void OnRocketImpact()
    {
        onTrigger?.Invoke();
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
