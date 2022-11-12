using Replay;
using Replay.StreamExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SatriProtoPlayerLauncher : MonoBehaviour
{
    [SerializeField] Transform muzzle;
    [SerializeField] LineRenderer previewLine;
    [SerializeField] GameObject previewImpact;
    [SerializeField] SatriProtoRocket rocketPrefab;

    [SerializeField] AudioSource sfxDryFire;
    [SerializeField] AudioSource sfxFire;
    [SerializeField] AudioSource sfxReload;

    [SerializeField] int maxGeneratedShots;
    [SerializeField] float launcherReloadTime; 
    [SerializeField] float shotRegenTime;
    [SerializeField] float muzzleVelocity;

    private Replayable replayable;
    private ReplayEventList replayFire;
    private ReplayEventList replayDryFire;

    private bool shouldTryFire;
    private int generatedShots;
    private float reloadTimer;
    private float regenTimer;

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

    private void Awake()
    {
        replayable = GetComponent<Replayable>();
        replayFire = replayable.GetEventList("Launcher.Fire");
        replayDryFire = replayable.GetEventList("Launcher.DryFire");

        generatedShots = maxGeneratedShots;
        reloadTimer = 0;
        regenTimer = shotRegenTime;
    }

    private void OnInputFire()
    {
        shouldTryFire = true;
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
            if (shouldTryFire)
                TryFire();
            shouldTryFire = false;
        }
        else
        {
            while (replayDryFire.TryRead())
                DoDryFire();
            while (replayFire.TryRead(out RocketInfo info))
                DoFire(info);
        }
    }

    private void TryFire()
    {
        Debug.Assert(replayable.Mode == ReplaySystem.ReplayMode.Record);
        if (ShotLoaded)
        {
            RocketInfo info = new RocketInfo { origin = muzzle.position, velocity = muzzle.forward * muzzleVelocity };
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
        //bool hit = Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hitInfo, 100, collisionLayers);
        bool hit = rocketPrefab.FindImpact(muzzle.position, muzzle.forward * muzzleVelocity, maxTime, Time.fixedDeltaTime, out SatriProtoRocket.ImpactInfo hitInfo);

        previewLine.positionCount = 2;
        previewLine.SetPosition(0, previewLine.transform.position);

        if (hit)
        {
            previewLine.SetPosition(1, hitInfo.position);
            previewImpact.transform.position = hitInfo.position;
        }
        else
        {
            previewLine.SetPosition(1, muzzle.position + muzzle.forward * 10 * muzzleVelocity);
        }

        previewImpact.SetActive(hit);
    }

    private void DoFire(in RocketInfo info)
    {
        --generatedShots;
        reloadTimer = launcherReloadTime;

        sfxFire.Play();

        var rocket = Instantiate(rocketPrefab);
        rocket.Configure(info.origin, info.velocity);
        activeRockets.Add(rocket);
    }
    private void DoDryFire()
    {
        sfxDryFire.PlayOneShot(sfxDryFire.clip);
    }
}
