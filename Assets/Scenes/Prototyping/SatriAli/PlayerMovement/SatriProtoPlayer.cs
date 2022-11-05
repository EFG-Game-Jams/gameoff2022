using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SatriProtoPlayer : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] RigidbodyInterpolation positionMode;

    private SatriProtoPlayerMovement movement;
    private SatriProtoPlayerCollision collision;

    private SatriProtoPlayerMovement.ControlState controlStateMove;
    private float cameraPitch;
    private Vector3 prevPosition;
    private Vector3 position;
    private Vector3 velocity;

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

        transform.Rotate(Vector3.up, delta.x);

        cameraPitch -= delta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0, 0);
    }

    private void Start()
    {
        movement = GetComponent<SatriProtoPlayerMovement>();
        collision = GetComponent<SatriProtoPlayerCollision>();

        position = transform.position;
        velocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.Locked;

        //Time.fixedDeltaTime = .01f;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        Vector3 newVelocity = movement.CalculateVelocity(velocity, controlStateMove, collision.IsGrounded, deltaTime);

        Vector3 displacement = newVelocity * deltaTime; // (velocity + newVelocity) * (.5f * deltaTime);
        Vector3 newPosition = position + displacement;

        collision.ApplyCollisionResponse(position, ref newPosition, ref newVelocity, deltaTime);

        prevPosition = position;
        position = newPosition;
        velocity = newVelocity;
    }

    private void Update()
    {
        float interpolationTime = (float)(Time.timeAsDouble - Time.fixedTimeAsDouble); ;
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

        //Debug.Log($"Speed: {velocity.magnitude}");
        //Debug.Log($"H speed: {Vector3.Scale(velocity, new Vector3(1, 0, 1)).magnitude}");
    }

    private void OnGUI()
    {
        GUILayout.Label($"H speed: {Vector3.Scale(velocity, new Vector3(1, 0, 1)).magnitude}");
        GUILayout.Label($"V speed: {velocity.y}");
    }
}
