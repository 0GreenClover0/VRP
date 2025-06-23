using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject player;
    public StoryController storyController;

    [HideInInspector] public bool warnedAboutPullSwitch = false;
    [SerializeField] private GameOverScreen gameOver;
    [SerializeField] private Appearable restartButtons;

    [Space]
    [Header("Global looping sounds")]

    public AudioClip windSound;

    private AudioSource audioSource;
    [HideInInspector] public bool firstFlashUsed = false;

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
        // restartButtons.transform.localScale = Vector3.zero;
        restartButtons.gameObject.SetActive(true);
        restartButtons.Appear();

        LevelController.Instance.GameFinished = true;

        gameOver.Appear();
    }

    public void WarnAboutPullSwitch()
    {
        if (!firstFlashUsed)
        {
            StartCoroutine(RopeReminder());
        }
    }

    IEnumerator RopeReminder()
    {
        yield return new WaitForSeconds(2);
        
        storyController.PlayEmergentVoiceline(1);
        storyController.blinkBrightness /= 5.0f;
        storyController.flashBlinking = true;
        
        yield return new WaitForSeconds(15);

        if (!firstFlashUsed)
        {
            storyController.PlayEmergentVoiceline(8);
            yield return new WaitForSeconds(30);
            storyController.blinkBrightness *= 5.0f;
            storyController.flashBlinking = false;
        }
    }
}
