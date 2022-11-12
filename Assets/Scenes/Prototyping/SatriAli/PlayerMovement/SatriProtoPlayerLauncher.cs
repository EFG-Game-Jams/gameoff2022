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

        replayable = GetComponent<Replayable>();
        replayFire = replayable.GetEventList("Launcher.Fire");
        replayDryFire = replayable.GetEventList("Launcher.DryFire");
        replayChargeShot = replayable.GetEventList("Launcher.ChargeShot");

        generatedShots = maxGeneratedShots;
        reloadTimer = 0;
        regenTimer = shotRegenTime;
        chargeTimer = -1;
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
        if (generatedShots < maxGeneratedShots)
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
        if (reloadTimer > 0)
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
        if (replayable.Mode == ReplaySystem.ReplayMode.Record)
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
        if (chargeTimer >= 0)
            chargeTimer += Time.fixedDeltaTime;
    }

    private void TryFire()
    {
        Debug.Assert(replayable.Mode == ReplaySystem.ReplayMode.Record);
        if (ShotLoaded)
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

    private void LateUpdate()
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

        previewImpact.SetActive(hit);
        if (hit)
            previewImpact.transform.position = hitInfo.position;
    }

    private void DoFire(in RocketInfo info)
    {
        var rocket = Instantiate(rocketPrefab);
        rocket.Configure(player, info.origin, info.velocity);
        activeRockets.Add(rocket);

        --generatedShots;
        reloadTimer = launcherReloadTime;
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
