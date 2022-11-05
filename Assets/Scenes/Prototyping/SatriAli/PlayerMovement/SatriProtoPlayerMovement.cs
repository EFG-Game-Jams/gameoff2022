using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatriProtoPlayerMovement : MonoBehaviour
{
    public struct ControlState
    {
        public Vector2 move;
        public bool jump;
    }

    [Header("Horizontal")]
    [SerializeField] Vector2 maxSpeed;
    [SerializeField] Vector2 maxAcceleration;
    [SerializeField] Vector2 airControlScale;
    [SerializeField] AnimationCurve accelerationCurve;

    [Header("Vertical")]
    [SerializeField] float gravityScale = 1f;
    [SerializeField] float jumpVelocity = 5f;
    [SerializeField] float jumpStopStrength = 3f;

    private bool jumping;
    private float jumpInitialVelocity;

    public float Gravity => Physics.gravity.y * gravityScale;

    public Vector3 CalculateVelocity(Vector3 velocity, in ControlState controlState, bool isGrounded, float deltaTime)
    {
        Vector3 newVelocity = velocity;
        newVelocity += CalculateMoveImpulse(velocity, controlState, isGrounded, deltaTime);
        newVelocity += CalculateJumpAndGravityImpulse(newVelocity, controlState, isGrounded, deltaTime);        
        return newVelocity;
    }

    private Vector3 CalculateMoveImpulse(Vector3 worldVelocity, in ControlState controlState, bool isGrounded, float deltaTime)
    {
        Vector3 localVelocity3 = transform.InverseTransformVector(worldVelocity);

        Vector2 localVelocity = new Vector2(localVelocity3.x, localVelocity3.z);

        Vector2 targetVelocity = Vector2.Scale(controlState.move, maxSpeed);
        Vector2 maxControlAcceleration = (isGrounded ? maxAcceleration : Vector2.Scale(maxAcceleration, airControlScale));

        bool allowSlowdownX = isGrounded || controlState.move.y != 0; // ground slowdown and air turning if holding W/S
        bool allowSlowdownY = isGrounded; // ground slowdown only

        Vector2 localImpulse;
        localImpulse.x = CalculateImpulse(localVelocity.x, targetVelocity.x, maxSpeed.x, maxControlAcceleration.x, deltaTime, allowSlowdownX);
        localImpulse.y = CalculateImpulse(localVelocity.y, targetVelocity.y, maxSpeed.y, maxControlAcceleration.y, deltaTime, allowSlowdownY);

        Vector3 localImpulse3 = new Vector3(localImpulse.x, 0, localImpulse.y);
        return transform.TransformVector(localImpulse3);
    }
    private float CalculateImpulse(float currentVelocity, float targetVelocity, float maxSpeed, float maxAcceleration, float deltaTime, bool allowSlowdown)
    {
        // Mathf.Sign returns positive for zero
        // here we want -1 / 0 / +1 so we'll use System.Math.Sign
        int cSign = Math.Sign(currentVelocity);
        int tSign = Math.Sign(targetVelocity);
        int aSign = Math.Sign(targetVelocity - currentVelocity);

        // if we're not allowed to slow down, don't do anything when target velocity is zero
        if (!allowSlowdown && tSign == 0)
            return 0f;

        // if we're not allowed to slow down, don't do anything if we're already at or above target velocity
        if (!allowSlowdown && cSign == tSign && Mathf.Abs(currentVelocity) >= Mathf.Abs(targetVelocity))
            return 0f;

        // figure out the maximum velocity we could accelerate to        
        float maxVelocity = maxSpeed * aSign;
        // and the maximum acceleration we can apply
        float accelerationCurveTime = Mathf.Clamp01(currentVelocity / maxVelocity); // this will be zero if we're trying to change direction
        float maxAbsAcceleration = maxAcceleration * accelerationCurve.Evaluate(accelerationCurveTime);

        // calculate correct impulse to avoid overshooting
        float impulseAbs = Mathf.Min(maxAbsAcceleration * deltaTime, Mathf.Abs(targetVelocity - currentVelocity));
        return impulseAbs * aSign;
    }

    private Vector3 CalculateJumpAndGravityImpulse(Vector3 currentVelocity, in ControlState controlState, bool isGrounded, float deltaTime)
    {
        /*
        // end jump?
        if (!controlState.jump)
        {
            if (isGrounded)
                jumpTime = 0f;
            else
                jumpTime = jumpMaxSustainTime;
            jumpInitialVelocity = 0f;
            return Vector3.zero;
        }

        // start jump?
        if (isGrounded && controlState.jump && jumpTime == 0f)
        {
            Debug.Log("Start jump");
            jumpTime = deltaTime;
            jumpInitialVelocity = currentVelocity.y;
            return new Vector3(0f, jumpVelocity, 0f);
        }

        // sustain
        jumpTime += deltaTime;
        if (jumpTime < jumpMaxSustainTime)
        {
            Debug.Log("Sustain jump");
            return new Vector3(0f, jumpInitialVelocity + jumpVelocity - currentVelocity.y, 0f);
        }

        // beyond max sustain time
        return Vector3.zero;
        */

        if (isGrounded && controlState.jump && !jumping)
        {
            // start jump
            // add jump impulse, no gravity this frame
            jumping = true;
            jumpInitialVelocity = currentVelocity.y;
            return new Vector3(0f, jumpVelocity, 0f);
        }

        if (jumping && currentVelocity.y > jumpInitialVelocity + jumpVelocity)
        {
            // something else added upwards velocity, cancel the jump so we don't apply stopping gravity when the player releases the jump
            jumping = false;
            jumpInitialVelocity = 0f;
        }

        if (jumping && !controlState.jump)
        {
            // jump released
            if (currentVelocity.y > jumpInitialVelocity)
            {
                // stopping gravity
                return new Vector3(0f, Gravity * jumpStopStrength * deltaTime, 0f);
            }
            else
            {
                // jump finished, freefall
                jumping = false;
                jumpInitialVelocity = 0f;
            }
        }

        // standard gravity
        return new Vector3(0f, Gravity * deltaTime, 0f);
    }
}
