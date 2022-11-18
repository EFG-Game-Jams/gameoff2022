using System;
using UnityEngine;

public class SatriProtoRocket : MonoBehaviour
{
    public struct ImpactInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public float time;
        public Collider collider;
    }

    [Header("References")]
    [SerializeField] MeshRenderer projectileMesh;
    [SerializeField] AudioSource flightSfx;
    [SerializeField] ParticleSystem[] flightVfx;
    [SerializeField] GameObject impactEffectPrefab;

    [Header("Projectile behaviour")]
    [SerializeField] LayerMask impactLayers;
    [SerializeField] float gravityScale;

    private SatriProtoPlayer player;
    private ProjectileStateInitial initialState;
    private ProjectileState currentState;

    public void Configure(SatriProtoPlayer player, Vector3 position, Vector3 velocity)
    {
        this.player = player;

        initialState = new ProjectileStateInitial
        {
            position = position,
            velocity = velocity,
            acceleration = Physics.gravity * gravityScale,
        };

        currentState = initialState.AsState;

        UpdateTransform(position, velocity);
    }

    public bool Advance(float deltaTime)
    {
        ProjectileState prevState = currentState;
        currentState = ProjectileUtils.GetStateAt(initialState, prevState.time + deltaTime);

        Vector3 diff = currentState.position - prevState.position;
        float dist = diff.magnitude;
        Vector3 dir = diff / dist;

        if (Physics.Raycast(prevState.position, dir, out RaycastHit hitInfo, dist, impactLayers))
        {
            UpdateTransform(prevState.position + dir * hitInfo.distance, currentState.velocity);
            Detonate(hitInfo);
            return false;
        }
        else
        {
            UpdateTransform(currentState.position, currentState.velocity);
        }

        return true;
    }

    private void UpdateTransform(Vector3 position, Vector3 velocity)
    {
        transform.position = position;
        transform.LookAt(position + velocity, Vector3.up);
    }

    private void Detonate(RaycastHit hitInfo)
    {
        projectileMesh.enabled = false;
        flightSfx.Stop();

        foreach (var vfx in flightVfx)
            vfx.Stop();

        RocketButton button = RocketButton.FromCollider(hitInfo.collider);
        if (button != null)
        {
            if (button.ShouldConsumeRocket)
                Instantiate(impactEffectPrefab, transform.position, impactEffectPrefab.transform.rotation);
            button.OnRocketImpact();
        }
        else
        {
            Instantiate(impactEffectPrefab, transform.position, impactEffectPrefab.transform.rotation);
            player.OnRocketDetonated(transform.position);
        }

        Destroy(gameObject, .5f); // give the particle system time to finish
    }

    public bool FindImpact(Vector3 origin, Vector3 velocity, float timeMax, float timeStep, out ImpactInfo impact)
    {
        impact = default;

        ProjectileStateInitial psi = new ProjectileStateInitial
        {
            position = origin,
            velocity = velocity,
            acceleration = Physics.gravity * gravityScale
        };

        Vector3 prevPosition = origin;
        int steps = Mathf.FloorToInt(timeMax / timeStep);
        for (int i = 1; i <= steps; ++i)
        {
            float time = i * timeStep;
            Vector3 position = ProjectileUtils.GetPositionAt(psi, time);
            Vector3 diff = position - prevPosition;
            float dist = diff.magnitude;
            Vector3 dir = diff / dist;

            if (Physics.Raycast(prevPosition, dir, out RaycastHit hitInfo, dist, impactLayers))
            {
                impact.position = hitInfo.point;
                impact.normal = hitInfo.normal;
                impact.time = (time - timeStep) + (timeStep * (hitInfo.distance / dist));
                impact.collider = hitInfo.collider;
                return true;
            }

            prevPosition = position;
        }
        return false;
    }

    public Vector3 GetPositionAt(Vector3 origin, Vector3 velocity, float time)
    {
        return ProjectileUtils.GetPositionAt(new ProjectileStateInitial { position = origin, velocity = velocity, acceleration = Physics.gravity * gravityScale }, time);
    }
}
