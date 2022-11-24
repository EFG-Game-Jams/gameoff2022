using UnityEngine;
using UnityEngine.Events;

public class RocketButton : RocketButtonBase
{
    [Space]

    [SerializeField] string title;
    [SerializeField] bool alwaysDisplayTitle;

    [Space]

    [SerializeField] Material materialIdle;
    [SerializeField] Material materialHover;
    [SerializeField] MeshRenderer mesh;
    [SerializeField] TMPro.TextMeshPro text;

    public override void OnHoverEnter()
    {
        mesh.sharedMaterial = materialHover;
        text.gameObject.SetActive(true);
        base.OnHoverEnter();
    }
    public override void OnHoverExit()
    {
        mesh.sharedMaterial = materialIdle;
        text.gameObject.SetActive(alwaysDisplayTitle);
        base.OnHoverExit();
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
