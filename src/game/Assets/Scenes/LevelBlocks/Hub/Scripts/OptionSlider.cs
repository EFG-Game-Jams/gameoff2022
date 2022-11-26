using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OptionSlider : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] float min;
    [SerializeField] float max;
    [SerializeField] float displayScale = 1f;
    [SerializeField] bool displayRound;
    [SerializeField] string displaySuffix;

    [Header("References")]
    [SerializeField] TextMeshProUGUI textValue;
    [SerializeField] Image imageOutline;
    [SerializeField] Image imageFill;
    [SerializeField] BoxCollider triggerCollider;
    [SerializeField] RocketButtonBase triggerButton;

    public UnityEvent<float> onChanged;

    public void SetValue(float value)
    {
        value = Mathf.Clamp(value, min, max);
        float mu = (value - min) / (max - min);
        imageFill.fillAmount = mu;
        SetValueText(value);
    }
    private void SetValueText(float value)
    {
        float scaled = value * displayScale;
        string text = displayRound ? Mathf.RoundToInt(scaled).ToString() : scaled.ToString();
        textValue.text = text + displaySuffix;
    }

    private void OnHoverEnter()
    {
        imageOutline.color = Color.HSVToRGB(0, 0, 1f);
        imageFill.color = Color.HSVToRGB(0, 0, 1f);
    }
    private void OnHoverExit()
    {
        imageOutline.color = Color.HSVToRGB(0, 0, .75f);
        imageFill.color = Color.HSVToRGB(0, 0, .75f);
    }
    private void OnTrigger(Vector3 lpos)
    {
        float radius = triggerCollider.size.x;
        float mu = Mathf.Clamp01(.5f + lpos.x / radius);
        float value = Mathf.Lerp(min, max, mu);
        SetValue(value);
        onChanged?.Invoke(value);
    }

    private void OnValidate()
    {
        OnHoverExit();

        //RectTransform t = imageFill.transform as RectTransform;
        //triggerCollider.size = new Vector3(t.rect.width, t.rect.height, .01f);
    }

    private void Start()
    {
        triggerButton.onHoverEnter.AddListener(OnHoverEnter);
        triggerButton.onHoverExit.AddListener(OnHoverExit);
        triggerButton.onTrigger.AddListener(OnTrigger);

        StartCoroutine(CoUpdateColiderSize());
    }

    IEnumerator CoUpdateColiderSize()
    {
        yield return null;

        RectTransform t = imageFill.transform as RectTransform;
        triggerCollider.size = new Vector3(t.rect.width, t.rect.height, .01f);
    }
}
