using UnityEngine;

[System.Serializable]
public struct ProjectileStateInitial
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;

    public ProjectileState AsState => new ProjectileState { position = position, velocity = velocity, time = 0f };
}

[System.Serializable]
public struct ProjectileState
{
    public Vector3 position;
    public Vector3 velocity;
    public float time;
}

public static class ProjectileUtils
{
    public static Vector3 GetDisplacementAt(in ProjectileStateInitial p, float time)
    {
        // dp = v0 * t + 0.5 * a * t²
        return (p.velocity + .5f * p.acceleration * time) * time;
    }
    public static Vector3 GetPositionAt(in ProjectileStateInitial p, float time)
    {
        return p.position + GetDisplacementAt(p, time);
    }
    public static Vector3 GetVelocityAt(in ProjectileStateInitial p, float time)
    {
        // v = v0 + at
        return p.velocity + p.acceleration * time;
    }
    public static ProjectileState GetStateAt(in ProjectileStateInitial p, float time)
    {
        return new ProjectileState
        {
            position = GetPositionAt(p, time),
            velocity = GetVelocityAt(p, time),
            time = time,
        };
    }
}