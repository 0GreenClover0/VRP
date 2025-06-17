using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    public StoryController storyController;

    [Space]
    [Header("Global looping sounds")]
    
    public AudioClip windSound;
	
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        PlayWindSound();
    }

    void PlayWindSound()
    {
        audioSource.clip = windSound;
        audioSource.Play();
    }
}
