using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatriProtoPlayerCollision : MonoBehaviour
{
    [SerializeField] private float sphereHigh = .5f;
    [SerializeField] private float sphereLow = -.5f;
    [SerializeField] private float sphereRadius = .5f;
    [SerializeField] private float depenetrationBias = .001f;
    [SerializeField] private float restitution = 0f;
    [SerializeField] private float friction = .1f;
    [SerializeField] private int maxIterations = 10;
    [SerializeField] private int warnIterations = 5;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private LayerMask triggerMask;

    private RaycastHit[] cachedHitResultArray;

    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        cachedHitResultArray = new RaycastHit[64];
    }

    public void ApplyCollisionResponse(Vector3 prevPosition, ref Vector3 newPosition, ref Vector3 newVelocity, float deltaTime)
    {
        IsGrounded = false;

        Vector3 pOffset1 = transform.up * sphereHigh;
        Vector3 pOffset2 = transform.up * sphereLow;

        int responseIterations = 0;
        for (int i = 0; i < maxIterations; ++i)
        {
            if (newPosition == prevPosition)
                break;

            Vector3 displacement = newPosition - prevPosition;
            float distance = displacement.magnitude;
            Vector3 direction = displacement / distance;

            // start the cast "behind" us to avoid clipping through geometry we're already touching
            Vector3 castOffset = prevPosition + direction * -depenetrationBias;
            float castDistance = distance + depenetrationBias;

            Vector3 p1 = castOffset + pOffset1;
            Vector3 p2 = castOffset + pOffset2;

            if (!Physics.CapsuleCast(p1, p2, sphereRadius, direction, out RaycastHit hitInfo, castDistance, collisionMask, QueryTriggerInteraction.Ignore))
                break;
            ++responseIterations;

            // depenetration            
            float penetrationDistance = castDistance - hitInfo.distance;
            float penetrationDepth = -Vector3.Dot(direction, hitInfo.normal) * penetrationDistance;
            Vector3 depenetrationOffset = hitInfo.normal * (penetrationDepth + depenetrationBias);

            // restitution
            Vector3 normalVelocity = Vector3.Project(newVelocity, hitInfo.normal);
            Vector3 restitutionResponse = normalVelocity * -(1f + restitution);
            
            // friction
            Vector3 tangentVelocity = Vector3.ProjectOnPlane(newVelocity, hitInfo.normal);
            //Vector3 frictionResponse = tangentVelocity * -(friction * deltaTime);
            float frictionMagnitude = Mathf.Min(friction * deltaTime, tangentVelocity.magnitude);
            Vector3 frictionResponse = tangentVelocity.normalized * -frictionMagnitude;

            // apply response
            newPosition += depenetrationOffset;
            newVelocity += restitutionResponse;
            newVelocity += frictionResponse;

            // we consider we're grounded if we hit something with a normal at most 45° from vertical
            const float cos45 = 0.7071f;
            IsGrounded = IsGrounded || Vector3.Dot(hitInfo.normal, Vector3.up) > cos45;
        }

        if (responseIterations >= warnIterations)
            Debug.LogWarning($"PlayerCollision detected {responseIterations} penetrations, this could mean it's colliding with something it shouldn't...");

        // triggers
        {
            Vector3 displacement = newPosition - prevPosition;
            float distance = displacement.magnitude;
            Vector3 direction = displacement / distance;

            // start the cast "behind" us to avoid clipping through geometry we're already touching
            Vector3 castOffset = prevPosition + direction * -depenetrationBias;
            float castDistance = distance + depenetrationBias;

            Vector3 p1 = castOffset + pOffset1;
            Vector3 p2 = castOffset + pOffset2;

            int triggerCount = Physics.CapsuleCastNonAlloc(p1, p2, sphereRadius, direction, cachedHitResultArray, castDistance, triggerMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < triggerCount; ++i)
            {
                RaycastHit hit = cachedHitResultArray[i];
                PlayerTrigger trigger = hit.collider.GetComponent<PlayerTrigger>();
                Debug.Assert(trigger != null);

                float hitDistance = hit.distance - depenetrationBias; // time begins at the start position, so compensate for backtracking
                float hitDistanceRatio = Mathf.Clamp01(hitDistance / distance);
                float hitTime = hitDistanceRatio * deltaTime;
                double time = Time.fixedTimeAsDouble + hitTime;
                trigger.OnEnter(gameObject, time);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * sphereHigh, sphereRadius);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * sphereLow, sphereRadius);
    }

    internal void UpdateInReplay(Vector3 prevPosition, Vector3 position, Vector3 velocity, float deltaTime)
    {
        ApplyCollisionResponse(prevPosition, ref position, ref velocity, deltaTime);
    }
}
