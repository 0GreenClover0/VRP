using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject player;
    public StoryController storyController;

    [SerializeField] private GameOverScreen gameOver;

    [Space]
    [Header("Global looping sounds")]

    public AudioClip windSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        PlayWindSound();

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("GameManager instance already existed. Destroying the old one.");
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    private void PlayWindSound()
    {
        audioSource.clip = windSound;
        audioSource.Play();
    }

    public void GameOver()
    {
        gameOver.Appear();
    }
}
