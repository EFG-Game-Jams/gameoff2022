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
    [SerializeField] HubRaceEntranceScreenLeaderboardWorld screenLeft;
    [SerializeField] HubRaceEntranceScreenLeaderboardNear screenRight;
    [SerializeField] PlayerTrigger playerTriggerFloor;
    [SerializeField] PlayerTrigger playerTriggerCeiling;
    [SerializeField] AudioSource screenMoveSound;

    private Transform[] screenTransforms;
    private GameObject player;
    private float screenAnimCurrent;
    private float screenAnimDirection;

    private string SceneName => gameObject.name;
    private string LevelName => gameObject.name.ToLowerInvariant();

    private void OnValidate()
    {
        if (nameLabel.text != null)
            nameLabel.text = LevelName;
    }

    private void Start()
    {
        nameLabel.text = LevelName;

        playerTriggerFloor.onTriggerEnter.AddListener(OnPlayerEnter);
        playerTriggerCeiling.onTriggerEnter.AddListener(EnterRace);

        screenFront.SetTitle(LevelName);
        screenFront.SetTimeLast(GamemodeHub.GetRaceLastTimeString(LevelName));
        screenFront.SetTimeBest("N/A");

        screenRight.onRefreshComplete.AddListener(() =>
        {
            string bestTime = screenRight.GetLocalPlayerTime();
            screenFront.SetTimeBest(bestTime);
        });

        screenTransforms = new Transform[] { screenFront.transform, screenLeft.transform, screenRight.transform };
        UpdateScreenAnimation();
    }

    private void EnterRace(GameObject player, double fixedTime)
    {
        var snapshot = new SatriProtoPlayer.TransformSnapshot();
        snapshot.cameraHeading = transform.eulerAngles.y;
        snapshot.cameraPitch = 0;
        snapshot.position = transform.TransformPoint(new Vector3(1.5f, 6f, 1.5f));
        GamemodeHub.BeginRace(SceneName, snapshot);
    }

    private void OnPlayerEnter(GameObject player, double fixedTime)
    {
        AnimateScreens(1);

        if (this.player == null)
        {
            StartCoroutine(CoDeactivateScreensOnPlayerLeave());
        }
        this.player = player;

        screenLeft.Refresh(LevelName);
        screenRight.Refresh(LevelName);
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
        screenMoveSound.Play();

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
        screenLeft.Opacity = opacity;
        screenRight.Opacity = opacity;
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
