using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    private AudioClip[] music;
    private int lastPlayedTrack;

    void Start()
    {
        if (FindObjectsOfType<MusicPlayer>().Length > 1)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        music = Resources.LoadAll<AudioClip>("Music");
        lastPlayedTrack = Random.Range(0, music.Length);
    }

    void Update()
    {
        if (music.Length > 0 && !audioSource.isPlaying)
        {
            lastPlayedTrack = (lastPlayedTrack + Random.Range(0, music.Length - 1)) % music.Length;
            audioSource.clip = music[lastPlayedTrack];
            audioSource.Play();
        }
    }
}
