using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubSceneRaceEntrance : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] float screenIdleHeight;
    [SerializeField] float screenActiveHeight;
    [SerializeField] float screenAnimationTime;        

    [Header("References")]
    [SerializeField] TMPro.TextMeshPro nameLabel;
    [SerializeField] HubRaceEntranceScreenInfo screenFront;
    [SerializeField] Transform screenLeft;
    [SerializeField] Transform screenRight;
    [SerializeField] PlayerTrigger playerTriggerFloor;

    private Transform[] screenTransforms;
    private GameObject player;
    private float screenAnimCurrent;
    [SerializeField] private float screenAnimDirection;

    private void OnValidate()
    {
        if (nameLabel.text != null)
            nameLabel.text = gameObject.name;
    }

    private void Start()
    {
        nameLabel.text = gameObject.name;

        playerTriggerFloor.onTriggerEnter.AddListener(OnPlayerEnter);

        screenFront.Configure(gameObject.name, "N/A", "N/A");

        screenTransforms = new Transform[] { screenFront.transform, screenLeft, screenRight };
        UpdateScreenAnimation();
    }

    private void OnPlayerEnter(GameObject player, double fixedTime)
    {
        AnimateScreens(1);

        if (this.player == null)
        {
            //Debug.Log("StartCoroutine(CoDeactivateScreensOnPlayerLeave())");
            StartCoroutine(CoDeactivateScreensOnPlayerLeave());
        }
        this.player = player;
    }

    private void AnimateScreens(float direction)
    {
        bool isRunning = screenAnimDirection != 0;
        screenAnimDirection = direction;
        if (!isRunning)
            StartCoroutine(CoAnimateScreens());
    }
    private IEnumerator CoAnimateScreens()
    {
        UpdateScreenAnimation();

        while (screenAnimCurrent >= 0f && screenAnimCurrent <= 1f)
        {
            yield return null;
            screenAnimCurrent += Time.deltaTime * screenAnimDirection / screenAnimationTime;
            UpdateScreenAnimation();
        }

        screenAnimCurrent = Mathf.Clamp01(screenAnimCurrent);
        screenAnimDirection = 0f;
    }
    private void UpdateScreenAnimation()
    {
        float mu = Mathf.Clamp01(screenAnimCurrent);
        mu = mu < 0.5f ? 2.0f * mu * mu : -1.0f + (4.0f - 2.0f * mu) * mu;
        float y = Mathf.Lerp(screenIdleHeight, screenActiveHeight, mu);

        foreach (var screen in screenTransforms)
        {
            Vector3 pos = screen.localPosition;
            pos.y = y;
            screen.localPosition = pos;
        }

        float opacity = Mathf.Pow(Mathf.Clamp01(screenAnimCurrent), 4);
        screenFront.Opacity = opacity;
    }

    private IEnumerator CoDeactivateScreensOnPlayerLeave()
    {
        var waitAnimation = new WaitForSeconds(screenAnimationTime);
        yield return waitAnimation;

        Vector3 center = transform.TransformPoint(new Vector3(1.5f, 1.1f, 1.5f));
        float distance = 0f;
        do
        {
            yield return null;
            distance = Vector3.Distance(center, player.transform.position);
            //Debug.Log(distance);
        }
        while (player != null && distance < .5f);

        AnimateScreens(-1);
        yield return waitAnimation;

        player = null;
        playerTriggerFloor.ResetTrigger();
    }
}
