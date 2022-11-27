using System.Collections;
using UnityEngine;

public class GrowOnFocus : MonoBehaviour
{
    [SerializeField]
    private Vector3 finalMultiplier = new Vector3(1.2f, 1.2f, 1.0f);
    [SerializeField]
    private AnimationCurve animationCurve;

    bool hasFocus;
    Vector3 initialScale, currentScale, finalScale;

    Coroutine animationCoroutine = null;

    // Start is called before the first frame update
    public void Start()
    {
        initialScale = transform.localScale;
        currentScale = initialScale;
        finalScale = Vector3.Scale(initialScale, finalMultiplier);

        // Debug.Log($"Initial scale {initialScale}");
        // Debug.Log($"Final scale {finalScale}");
    }

    public void OnFocus()
    {
        Animate(true);
    }

    public void OnBlur()
    {
        Animate(false);
    }

    public void Animate(bool hasFocus)
    {
        this.hasFocus = hasFocus;
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(GrowAnimation());
    }

    private IEnumerator GrowAnimation()
    {
        float elapsed = 0.0f;
        while (elapsed < animationCurve[animationCurve.length - 1].time)
        {
            elapsed += Time.deltaTime;

            if (hasFocus)
            {
                currentScale = Vector3.Lerp(currentScale, finalScale, animationCurve.Evaluate(elapsed));
            }
            else
            {
                currentScale = Vector3.Lerp(currentScale, initialScale, animationCurve.Evaluate(elapsed));
            }
            transform.localScale = currentScale;

            // Debug.Log(transform.localScale);

            yield return new WaitForEndOfFrame();
        }

        if (hasFocus)
        {
            currentScale = finalScale;
        }
        else
        {
            currentScale = initialScale;
        }
        transform.localScale = currentScale;

        animationCoroutine = null;
        yield break;
    }

}
