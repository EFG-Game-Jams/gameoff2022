using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PlayerTrajectoryVisualiser : MonoBehaviour
{
    private const float MaxSpeed = 30f;
    private const float JumpSpeed = 5f;
    private const float MaxSimulationTime = 10f;

    [Range(0f, MaxSpeed)]
    [SerializeField] float speed;

    [Range(0f, 90f)]
    [SerializeField] float elevation;

    [SerializeField] bool jump;
    
    [Header("References")]
    [SerializeField] Mesh endpointMesh;

    private bool dirty;
    private List<Vector3> positions = new();

    private void OnValidate()
    {
        dirty = true;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            dirty = true;
            transform.hasChanged = false;
        }
        if (dirty)
        {
            Simulate();
            dirty = false;
        }
    }

    private Vector3 CalculateInitialVelocity()
    {
        Vector3 dir = transform.TransformDirection(Quaternion.Euler(-elevation, 0, 0) * Vector3.forward);
        Vector3 vel = dir * speed;
        if (jump)
            vel.y += JumpSpeed;
        return vel;
    }

    private void Simulate()
    {
        positions.Clear();

        Vector3 pos = transform.position;
        Vector3 vel = CalculateInitialVelocity();

        positions.Add(pos);

        int layerMask = LayerMask.GetMask("LevelStatic");
        float t = 0f;
        while (t < MaxSimulationTime)
        {
            t += Time.fixedDeltaTime;

            Vector3 prevPos = pos;
            pos = prevPos + vel * Time.fixedDeltaTime;
            positions.Add(pos);

            if (Physics.CapsuleCast(prevPos + Vector3.up * .5f, prevPos - Vector3.up * .5f, .5f, vel.normalized, (pos - prevPos).magnitude, layerMask))
                break;

            vel += Physics.gravity * Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (positions.Count == 0)
            return;

        Vector3 offset = Vector3.up;

        Gizmos.color = Color.black;
        Vector3 p0 = positions[0];
        for (int i=1; i<positions.Count; ++i)
        {
            Vector3 p1 = positions[i];
            Gizmos.DrawLine(p0 + offset, p1 + offset);
            Gizmos.DrawLine(p0 - offset, p1 - offset);
            p0 = p1;
        }

        Gizmos.DrawMesh(endpointMesh, p0);
    }
}
