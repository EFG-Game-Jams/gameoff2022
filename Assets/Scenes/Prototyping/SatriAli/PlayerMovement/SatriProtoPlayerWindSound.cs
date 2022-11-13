using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatriProtoPlayerWindSound : MonoBehaviour
{
    [SerializeField] SatriProtoPlayer player;
    [SerializeField] Transform listener;
    [SerializeField] AudioSource audioSource;

    [SerializeField] float maxSpeed;
    [SerializeField] Vector2 volumeRange;
    [SerializeField] float volumeChangeSpeed;
    [SerializeField] AnimationCurve speedToVolume;
    [SerializeField] Vector2 pitchRange;
    [SerializeField] float pitchChangeSpeed;
    [SerializeField] AnimationCurve speedToPitch;
    [SerializeField] float maxPan;

    private void FixedUpdate()
    {
        Vector3 velocity = player.Velocity;
        float speed = velocity.magnitude;
        float mu = speed / maxSpeed;

        float volume = Mathf.Lerp(volumeRange.x, volumeRange.y, speedToVolume.Evaluate(mu));
        float pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, speedToPitch.Evaluate(mu));

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, volume, volumeChangeSpeed * Time.fixedDeltaTime);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, pitch, pitchChangeSpeed * Time.fixedDeltaTime);

        Vector3 dir = velocity.normalized;
        Vector3 ldir = listener.InverseTransformDirection(dir);
        audioSource.panStereo = ldir.x * maxPan;
    }
}
