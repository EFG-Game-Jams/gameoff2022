using Replay;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SatriProtoPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] public PlayerData uiData;

    [Header("Movement")]
    [SerializeField] RigidbodyInterpolation positionMode;

    [Header("Rockets")]
    [SerializeField] float rocketImpulseMax;
    [SerializeField] float rocketRadiusMin;
    [SerializeField] float rocketRadiusMax;
    [SerializeField] bool rocketLimitFinalSpeed;
    [SerializeField] float rocketFinalSpeedMax;

    private SatriProtoPlayerMovement movement;
    private SatriProtoPlayerCollision collision;

    private Replayable replayable;
    private ReplayStream.Writer replayWriterPosition;
    private ReplayStream.Writer replayWriterAim;
    private ReplayStream.Reader replayReaderPosition;
    private ReplayStream.Reader replayReaderAim;

    private SatriProtoPlayerMovement.ControlState controlStateMove;

    private bool movementLocked;
    private bool aimLocked;

    private float cameraHeading;
    private float cameraPitch;
    private Vector3 prevPosition;
    private Vector3 position;
    private Vector3 velocity;

    public Vector3 Velocity => velocity;

    public struct TransformSnapshot
    {
        public float cameraHeading;
        public float cameraPitch;
        public Vector3 position;
    }

    public void Teleport(Vector3 pos, Quaternion rot)
    {
        Debug.Assert(replayable.Mode == ReplaySystem.ReplayMode.None);
        Vector3 euler = rot.eulerAngles;
        SetTransform(new TransformSnapshot { cameraHeading = euler.y, cameraPitch = euler.x, position = pos });
    }
    public void SetTransform(TransformSnapshot snapshot)
    {
        Debug.Assert(replayable.Mode == ReplaySystem.ReplayMode.None);
        cameraHeading = snapshot.cameraHeading;
        cameraPitch = snapshot.cameraPitch;

        position = snapshot.position;
        prevPosition = position;
        velocity = Vector3.zero;

        ApplyAim(cameraHeading, cameraPitch);
        transform.position = position;
    }
    public TransformSnapshot GetTransform()
    {
        return new TransformSnapshot { cameraHeading = cameraHeading, cameraPitch = cameraPitch, position = position };
    }

    public void SetLocks(bool movement, bool aim)
    {
        movementLocked = movement;
        aimLocked = aim;

        if (movementLocked)
            velocity = Vector3.zero;
    }

    private void OnInputMove(InputValue value)
    {
        controlStateMove.move = value.Get<Vector2>();
    }
    private void OnInputJump(InputValue value)
    {
        controlStateMove.jump = value.isPressed;
    }

    private void OnInputAim(InputValue value)
    {
        if (aimLocked)
            return;

        Vector2 delta = value.Get<Vector2>();

        cameraHeading = Mathf.DeltaAngle(0, cameraHeading + delta.x);

        cameraPitch -= delta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
    }

    private void ApplyAim(float heading, float pitch)
    {
        transform.localEulerAngles = new Vector3(0f, heading, 0f);
        cameraTransform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    public void OnRocketDetonated(Vector3 impactPosition)
    {
        if (replayable.ShouldPlayback)
            return; // ignore during playback

        Vector3 diff = position - impactPosition;
        float dist = diff.magnitude;
        if (dist >= rocketRadiusMax)
            return;

        float rocketStrength = 1f - Mathf.Clamp01((dist - rocketRadiusMin) / (rocketRadiusMax - rocketRadiusMin));
        float rocketImpulse = rocketStrength * rocketImpulseMax;
        Vector3 dir = diff / dist;
        Vector3 impulse = dir * rocketImpulse;

        if (rocketLimitFinalSpeed && (velocity + impulse).magnitude > rocketFinalSpeedMax)
        {
            if (velocity.magnitude < rocketFinalSpeedMax || Vector3.Dot(velocity.normalized, impulse.normalized) < 0) // only add velocity if we aren't yet at the cap
            {
                // wolfram alpha: solve m = sqrt((a + x A)^2 + (b + x B)^2 + (c + x C)^2) for x
                float m = rocketFinalSpeedMax;
                float a = velocity.x;
                float b = velocity.y;
                float c = velocity.z;
                float A = impulse.x;
                float B = impulse.y;
                float C = impulse.z;
                // ok let's go...
                float part1 = 1f / (2 * (A * A + B * B + C * C));
                float part2 = Mathf.Sqrt(
                    Mathf.Pow(2 * a * A + 2 * b * B + 2 * c * C, 2)
                    - 4 * (A * A + B * B + C * C) * (a * a + b * b + c * c - m * m)           
                    );
                float part3 = -2 * a * A - 2 * b * B - 2 * c * C;

                float ratio = part1 * (part2 + part3);
                if (ratio < 0)
                    ratio = part1 * (-part2 + part3);

                Debug.Assert(ratio >= 0);
                velocity += impulse * ratio;
            }
        }
        else
        {
            velocity += impulse;
        }
    }

    private void Awake()
    {
        movement = GetComponent<SatriProtoPlayerMovement>();
        collision = GetComponent<SatriProtoPlayerCollision>();
        replayable = GetComponent<Replay.Replayable>();
    }

    private void Start()
    {
        if (replayable.ShouldRecord)
        {
            replayWriterPosition = replayable.GetWriter("position");
            replayWriterAim = replayable.GetWriter("aim");
        }
        else
        {
            replayReaderPosition = replayable.GetReader("position");
            replayReaderAim = replayable.GetReader("aim");
        }

        position = transform.position;
        prevPosition = position;
        velocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (replayable.ShouldRecord)
        {
            ApplyAim(cameraHeading, cameraPitch);

            Vector3 newVelocity = movement.CalculateVelocity(velocity, controlStateMove, collision.IsGrounded, deltaTime);

            Vector3 displacement = newVelocity * deltaTime;
            Vector3 newPosition = position + displacement;

            collision.ApplyCollisionResponse(position, ref newPosition, ref newVelocity, deltaTime);

            prevPosition = position;
            if (!movementLocked)
            {
                position = newPosition;
                velocity = newVelocity;
            }

            replayWriterPosition.Write(position);
            replayWriterAim.Write(new Vector2(cameraHeading, cameraPitch));
        }
        else
        {
            Vector2 aim = replayReaderAim.ReadVector2();
            cameraHeading = aim.x;
            cameraPitch = aim.y;
            ApplyAim(cameraHeading, cameraPitch);

            prevPosition = position;
            position = replayReaderPosition.ReadVector3();
            velocity = (position - prevPosition) / Time.fixedDeltaTime;
        }

        uiData.speed = Vector3.Scale(velocity, new Vector3(1, 0, 1)).magnitude;
        uiData.maxSpeed = rocketFinalSpeedMax;
    }

    private void Update()
    {
        ApplyAim(cameraHeading, cameraPitch);

        float interpolationTime = (float)(Time.timeAsDouble - Time.fixedTimeAsDouble);
        switch (positionMode)
        {
            case RigidbodyInterpolation.None:
                transform.position = position;
                break;
            case RigidbodyInterpolation.Interpolate:
                transform.position = Vector3.Lerp(prevPosition, position, interpolationTime / Time.fixedDeltaTime);
                break;
            case RigidbodyInterpolation.Extrapolate:
                transform.position = transform.position = position + velocity * interpolationTime;
                break;
        }
    }

    /*private void OnGUI()
    {
        GUILayout.Label($"H speed: {Vector3.Scale(velocity, new Vector3(1, 0, 1)).magnitude}");
        GUILayout.Label($"V speed: {velocity.y}");
    }*/
}
