using UnityEngine;

public class FootstepSounds : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] AudioSource[] sources;

    [Header("Audio clips")]
    [SerializeField] AudioClip[] clipsSoft;
    [SerializeField] AudioClip[] clipsHard;

    [Header("Behaviour")]
    [SerializeField] float minDistanceBetweenSteps;
    [SerializeField] float minTimeBetweenSteps;
    [SerializeField] float downwardSpeedHardnessBias;
    [SerializeField] AnimationCurve speedToSoftHard;
    [SerializeField] AnimationCurve hardnessToVolume;

    private int nextSourceIndex;
    private Vector3 prevPosition;
    private float distanceSinceLastStep;
    private float timeSinceLastStep;
    private Vector3 approximateVelocity;

    private void Start()
    {
        prevPosition = transform.position;
    }

    private void Update()
    {
        Vector3 position = transform.position;
        Vector3 displacement = position - prevPosition;
        float distance = displacement.magnitude;
        prevPosition = position;

        distanceSinceLastStep += distance;
        timeSinceLastStep += Time.deltaTime;
        approximateVelocity = displacement / Time.deltaTime;

        if (IsGrounded() && distanceSinceLastStep >= minDistanceBetweenSteps && timeSinceLastStep >= minTimeBetweenSteps)
        {
            PlayFootstep();
            distanceSinceLastStep = 0;
            timeSinceLastStep = 0;
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * .01f, -Vector3.up, .1f, ~0, QueryTriggerInteraction.Ignore);
    }

    private void PlayFootstep()
    {
        float speed = approximateVelocity.magnitude;

        float hardness = Mathf.Clamp01(-approximateVelocity.y * downwardSpeedHardnessBias);
        hardness = Mathf.Clamp01(hardness + speedToSoftHard.Evaluate(speed));
        bool useHard = (Random.Range(0f, 1f) <= hardness);
        float volumeScale = Mathf.Clamp01(hardnessToVolume.Evaluate(hardness) + Random.Range(-.1f, .1f));

        AudioClip[] clips = (useHard ? clipsHard : clipsSoft);
        AudioClip clip = clips[Random.Range(0, clips.Length)];

        AudioSource source = sources[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sources.Length;

        source.PlayOneShot(clip, volumeScale);
    }
}
