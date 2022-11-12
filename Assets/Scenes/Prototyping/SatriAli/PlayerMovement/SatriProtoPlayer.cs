using Replay;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SatriProtoPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] RigidbodyInterpolation positionMode;

    [Header("Rockets")]
    [SerializeField] float rocketImpulseMax;
    [SerializeField] float rocketRadiusMin;
    [SerializeField] float rocketRadiusMax;

    private SatriProtoPlayerMovement movement;
    private SatriProtoPlayerCollision collision;

    private Replayable replayable;
    private ReplayStream.Writer replayWriterPosition;
    private ReplayStream.Writer replayWriterAim;
    private ReplayStream.Reader replayReaderPosition;
    private ReplayStream.Reader replayReaderAim;

    private SatriProtoPlayerMovement.ControlState controlStateMove;

    private float cameraHeading;
    private float cameraPitch;
    private Vector3 prevPosition;
    private Vector3 position;
    private Vector3 velocity;

    public Vector3 Velocity => velocity;

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
        if (replayable.Mode == ReplaySystem.ReplayMode.Playback)
            return; // ignore during playback

        Vector3 diff = position - impactPosition;
        float dist = diff.magnitude;
        if (dist >= rocketRadiusMax)
            return;

        float rocketStrength = 1f - Mathf.Clamp01((dist - rocketRadiusMin) / (rocketRadiusMax - rocketRadiusMin));
        float rocketImpulse = rocketStrength * rocketImpulseMax;
        Vector3 dir = diff / dist;
        Vector3 impulse = dir * rocketImpulse;

        velocity += impulse;
    }

    private void Start()
    {
        movement = GetComponent<SatriProtoPlayerMovement>();
        collision = GetComponent<SatriProtoPlayerCollision>();

        replayable = GetComponent<Replay.Replayable>();
        if (replayable.Mode == ReplaySystem.ReplayMode.Record)
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
        velocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (replayable.Mode == ReplaySystem.ReplayMode.Record)
        {
            ApplyAim(cameraHeading, cameraPitch);

            Vector3 newVelocity = movement.CalculateVelocity(velocity, controlStateMove, collision.IsGrounded, deltaTime);

            Vector3 displacement = newVelocity * deltaTime;
            Vector3 newPosition = position + displacement;

            collision.ApplyCollisionResponse(position, ref newPosition, ref newVelocity, deltaTime);

            prevPosition = position;
            position = newPosition;
            velocity = newVelocity;            

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

    private void OnGUI()
    {
        GUILayout.Label($"H speed: {Vector3.Scale(velocity, new Vector3(1, 0, 1)).magnitude}");
        GUILayout.Label($"V speed: {velocity.y}");
    }
}
