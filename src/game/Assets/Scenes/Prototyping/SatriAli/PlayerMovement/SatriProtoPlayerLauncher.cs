using Replay;
using Replay.StreamExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SatriProtoPlayerLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform muzzle;
    [SerializeField] LineRenderer previewLine;
    [SerializeField] GameObject previewImpact;
    [SerializeField] SatriProtoRocket rocketPrefab;

    [SerializeField] AudioSource sfxDryFire;
    [SerializeField] AudioSource sfxFire;
    [SerializeField] AudioSource sfxReload;

    [Header("Launcher config")]
    [SerializeField] int maxGeneratedShots;
    [SerializeField] float launcherReloadTime; 
    [SerializeField] float shotRegenTime;

    [Header("Shot config")]
    [SerializeField] bool inheritVelocity;
    [SerializeField] bool chargeShots;
    [SerializeField] float chargeShotTime;
    [SerializeField] float chargeShotDelay;
    [SerializeField] float muzzleVelocityMin;
    [SerializeField] float muzzleVelocityMax;

    [Header("Trajectory preview")]
    [SerializeField] int trajectoryPreviewSegments;
    [SerializeField] float trajectoryPreviewTime;

    private SatriProtoPlayer player;
    private Replayable replayable;
    private ReplayEventList replayFire;
    private ReplayEventList replayDryFire;
    private ReplayEventList replayChargeShot;

    private bool shouldBeginCharge;
    private bool shouldTryFire;

    private int generatedShots;
    private float reloadTimer;
    private float regenTimer;
    private float chargeTimer;

    private RocketButtonBase hoveredRocketButton;
    private LayerMask uiLayer;

    private bool isEnabled = true;
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value)
                return;
            isEnabled = value;
            if (isEnabled)
            {
                sfxReload.Play();
                generatedShots = maxGeneratedShots;
            }
            else
            {
                generatedShots = 0;
            }
            regenTimer = shotRegenTime;
            reloadTimer = 0;
            chargeTimer = -1;
        }
    }

    private struct RocketInfo : IStreamable
    {
        public Vector3 origin;
        public Vector3 velocity;

        public void WriteToStream(Stream s)
        {
            s.WriteVector3(origin);
            s.WriteVector3(velocity);
        }
        public void ReadFromStream(Stream s)
        {
            origin = s.ReadVector3();
            velocity = s.ReadVector3();
        }
    }

    List<SatriProtoRocket> activeRockets = new();

    private bool Reloading => (generatedShots > 0 && reloadTimer > 0);
    private bool ShotLoaded => (generatedShots > 0 && !Reloading);

    private Vector3 ChargedMuzzleVelocity
    {
        get
        {
            float speed = chargeShots
                ? Mathf.Lerp(muzzleVelocityMin, muzzleVelocityMax, (chargeTimer - chargeShotDelay) / chargeShotTime)
                : muzzleVelocityMax;

            Vector3 velocity = muzzle.forward * speed;
            if (inheritVelocity)
                velocity += player.Velocity;

            return velocity;
        }
    }

    private void Awake()
    {
        player = GetComponent<SatriProtoPlayer>();

        generatedShots = maxGeneratedShots;
        reloadTimer = 0;
        regenTimer = shotRegenTime;
        chargeTimer = -1;

        uiLayer = LayerMask.GetMask("UI");
    }

    private void Start()
    {
        replayable = GetComponent<Replayable>();
        replayFire = replayable.GetEventList("Launcher.Fire");
        replayDryFire = replayable.GetEventList("Launcher.DryFire");
        replayChargeShot = replayable.GetEventList("Launcher.ChargeShot");
    }

    private void OnInputFire(InputValue value)
    {
        if (chargeShots)
        {
            shouldBeginCharge = value.isPressed;
            shouldTryFire = !value.isPressed;
        }
        else
        {
            shouldTryFire = value.isPressed;
        }
    }

    private void FixedUpdate()
    {
        // generate
        if (isEnabled && generatedShots < maxGeneratedShots)
        {
            regenTimer -= Time.fixedDeltaTime;
            if (regenTimer <= 0f)
            {
                ++generatedShots;
                regenTimer = shotRegenTime;

                if (generatedShots == 1 && reloadTimer == 0)
                    sfxReload.Play();
            }
        }

        // reload
        if (isEnabled && reloadTimer > 0)
        {
            reloadTimer = Mathf.Max(0f, reloadTimer - Time.fixedDeltaTime);
            if (reloadTimer == 0 && generatedShots > 0)
                sfxReload.Play();
        }

        // simulate rockets
        // we do this explicitly rather than relying on the rocket's own event functions because the first fixedupdate
        // would probably only run on the rocket at the first fixedupdate, after the first update, after it's spawned
        for (int i = activeRockets.Count - 1; i >= 0; --i)
        {
            SatriProtoRocket rocket = activeRockets[i];
            if (!rocket.Advance(Time.fixedDeltaTime))
                activeRockets.RemoveAt(i);
        }

        // fire
        if (!isEnabled)
        {
            shouldBeginCharge = false;
            shouldTryFire = false;
        }
        else if (replayable.ShouldRecord)
        {
            if (shouldBeginCharge)
            {
                replayChargeShot.Write();
                DoBeginCharge();
            }
            shouldBeginCharge = false;

            if (shouldTryFire)
                TryFire();
            shouldTryFire = false;
        }
        else
        {
            while (replayDryFire.TryRead())
                DoDryFire();
            while (replayChargeShot.TryRead())
                DoBeginCharge();
            while (replayFire.TryRead(out RocketInfo info))
                DoFire(info);
        }

        // charge shot
        if (isEnabled && chargeTimer >= 0)
            chargeTimer += Time.fixedDeltaTime;

        // UI
        var uiData = player.uiData;
        uiData.rocketReadyCount = generatedShots;
        uiData.rocketRegenTime = isEnabled && generatedShots < maxGeneratedShots ? Mathf.Clamp01(1f - regenTimer / shotRegenTime) : 0f;
        uiData.rocketReloadTime = reloadTimer <= 0 ? 0f : Mathf.Clamp01(1f - reloadTimer / launcherReloadTime);
        uiData.isRocketLoaded = ShotLoaded;
    }

    private void TryFire()
    {
        Debug.Assert(replayable.ShouldRecord);
        if (hoveredRocketButton != null && !hoveredRocketButton.ShouldConsumeRocket)
        {
            Debug.Assert(replayable.Mode == ReplaySystem.ReplayMode.None);
            hoveredRocketButton.OnTrigger(Vector3.zero);
            chargeTimer = -1;
        }
        else if (ShotLoaded)
        {
            RocketInfo info = new RocketInfo { origin = muzzle.position, velocity = ChargedMuzzleVelocity };
            replayFire.Write(info);
            DoFire(info);
        }
        else
        {
            replayDryFire.Write();
            DoDryFire();
        }
    }

    private RocketButtonBase GetHoveredUi(out RaycastHit hitInfo)
    {
        const float MaxDistance = 2f;
        if (Physics.Raycast(muzzle.position, muzzle.forward, out hitInfo, MaxDistance, uiLayer, QueryTriggerInteraction.Collide))
            return RocketButtonBase.FromCollider(hitInfo.collider);
        return null;
    }

    private void UpdateRocketButton(RocketButtonBase hitRocketButton)
    {
        if (hoveredRocketButton != hitRocketButton)
        {
            if (hoveredRocketButton != null)
                hoveredRocketButton.OnHoverExit();
            hoveredRocketButton = hitRocketButton;
            if (hitRocketButton != null)
                hoveredRocketButton.OnHoverEnter();
        }
    }

    private void LateUpdate()
    {
        RocketButtonBase uiButton = GetHoveredUi(out RaycastHit uiHit);
        if (uiButton != null)
        {
            UpdateRocketButton(uiButton);
            previewLine.gameObject.SetActive(false);
            previewImpact.SetActive(true);
            previewImpact.transform.position = uiHit.point;
            previewImpact.transform.LookAt(uiHit.point + uiHit.normal, Vector3.up);
        }
        else
        {
            previewLine.gameObject.SetActive(isEnabled);
            previewImpact.SetActive(isEnabled);

            if (isEnabled)
            {
                const float maxTime = 10f;
                Vector3 origin = muzzle.position;
                Vector3 velocity = ChargedMuzzleVelocity;

                bool hit = rocketPrefab.FindImpact(origin, velocity, maxTime, Time.fixedDeltaTime, out SatriProtoRocket.ImpactInfo hitInfo);

                float previewTimeEnd = (hit ? hitInfo.time : trajectoryPreviewTime);
                float previewTimeStep = previewTimeEnd / trajectoryPreviewSegments;
                Vector3 previewToMuzzle = muzzle.position - previewLine.transform.position;
                Vector3 previewToMuzzleStep = previewToMuzzle / trajectoryPreviewSegments;

                if (previewLine.positionCount != trajectoryPreviewSegments + 1)
                    previewLine.positionCount = trajectoryPreviewSegments + 1;

                for (int i = 0; i <= trajectoryPreviewSegments; ++i)
                {
                    float time = i * previewTimeStep;
                    Vector3 offset = previewToMuzzleStep * -(trajectoryPreviewSegments - i);
                    previewLine.SetPosition(i, rocketPrefab.GetPositionAt(origin, velocity, time) + offset);
                }

                RocketButtonBase hitRocketButton = null;
                if (hit)
                {
                    hitRocketButton = RocketButtonBase.FromCollider(hitInfo.collider);
                    if (hitRocketButton == null)
                    {
                        previewImpact.SetActive(true);
                        previewImpact.transform.position = hitInfo.position;
                        previewImpact.transform.LookAt(hitInfo.position + hitInfo.normal, transform.forward);
                    }
                    else
                    {
                        previewImpact.SetActive(false);
                    }
                }
                else
                {
                    previewImpact.SetActive(false);
                }

                if (hoveredRocketButton != hitRocketButton)
                {
                    if (hoveredRocketButton != null)
                        hoveredRocketButton.OnHoverExit();
                    hoveredRocketButton = hitRocketButton;
                    if (hitRocketButton != null)
                        hoveredRocketButton.OnHoverEnter();
                }
            }
        }
    }

    private void DoFire(in RocketInfo info)
    {
        var rocket = Instantiate(rocketPrefab);
        rocket.Configure(player, info.origin, info.velocity);
        activeRockets.Add(rocket);

        if (hoveredRocketButton == null || hoveredRocketButton.ShouldConsumeRocket)
        {
            --generatedShots;
            reloadTimer = launcherReloadTime;
        }

        chargeTimer = -1;

        sfxFire.Play();
    }
    private void DoDryFire()
    {
        chargeTimer = -1;

        sfxDryFire.PlayOneShot(sfxDryFire.clip);
    }
    private void DoBeginCharge()
    {
        chargeTimer = 0;
    }
}
