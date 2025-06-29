using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject player;
    public StoryController storyController;

    [HideInInspector] public bool warnedAboutPullSwitch = false;
    [SerializeField] private GameOverScreen gameOver;
    [SerializeField] private Appearable restartButtons;
    public PullSwitch pullSwitch;

    [Space]
    [Header("Global looping sounds")]

    public AudioClip windSound;

    private bool showedControllerPrompt = false;
    private AudioSource audioSource;
    private AdditionalGrabbableLogic additionalGrabbableLogic;
    [HideInInspector] public bool firstFlashUsed = false;

    public List<string> gameSceneNames = new List<string>();

    private void Awake()
    {
        gameSceneNames = new List<string>() {"Random1", "Random2"};

        audioSource = GetComponent<AudioSource>();

        if (storyController != null)
        {
            additionalGrabbableLogic = storyController.spotlightFilteredTransformer.gameObject.GetComponent<AdditionalGrabbableLogic>();
        }

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

    private float leftButtonsTimer = 0.0f;
    private float rightButtonsTimer = 0.0f;

    private void Update()
    {
        if (OVRInput.Get(OVRInput.RawButton.X) && OVRInput.Get(OVRInput.RawButton.Y))
        {
            leftButtonsTimer += Time.deltaTime;

            if (leftButtonsTimer > 5.0f)
            {
                leftButtonsTimer = -999.0f;

                SceneManager.LoadSceneAsync("Menu");
            }
        }
        else
        {
            leftButtonsTimer = 0.0f;
        }

        if (OVRInput.Get(OVRInput.RawButton.A) && OVRInput.Get(OVRInput.RawButton.B))
        {
            rightButtonsTimer += Time.deltaTime;

            if (rightButtonsTimer > 5.0f)
            {
                rightButtonsTimer = -999.0f;

                string randomLevel = gameSceneNames[UnityEngine.Random.Range(0, Instance.gameSceneNames.Count)];
                SceneManager.LoadSceneAsync(randomLevel);
            }
        }
        else
        {
            rightButtonsTimer = 0.0f;
        }

        DimSpotlightTick();
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

    public void ScheduleShowingControllerPrompt()
    {
        if (!showedControllerPrompt && !LevelController.Instance.IsDuringScriptedSequence && !LevelController.Instance.GameFinished)
        {
            if (additionalGrabbableLogic.holdingHand == GrabbingHand.None ||
                additionalGrabbableLogic.spotlightController.generatorPowerScript.GetCurrentGeneratorPower() < 30)
            {
                storyController.spotlightFilteredTransformer.onGrab += OnGrab;
                return;
            }
            StartCoroutine(ShowDimSpotlightHint(60)); // !   
        }
    }

    private void OnGrab()
    {
        StartCoroutine(ShowDimSpotlightHint(20));
    }

    IEnumerator RopeReminder()
    {
        yield return new WaitForSeconds(2);
        
        storyController.PlayEmergentVoiceline(1);
        storyController.blinkBrightness = 5.5f;
        storyController.flashBlinking = true;
        
        yield return new WaitForSeconds(15);

        if (!firstFlashUsed)
        {
            storyController.PlayEmergentVoiceline(8);
            yield return new WaitForSeconds(30);
            storyController.blinkBrightness = 8.0f;
            storyController.flashBlinking = false;
        }
    }

    void DimSpotlightTick()
    {
        if (LevelController.Instance.IsDuringScriptedSequence || LevelController.Instance.GameFinished)
        {
            return;
        }
        
        Color color = Color.white;
        color.a = storyController.logoAlpha;
        storyController.controllerPrompt.material.SetColor("_BaseColor", color);
    }
    
    IEnumerator ShowDimSpotlightHint(int time)
    {
        yield return new WaitForSeconds(time);

        if (additionalGrabbableLogic.holdingHand == GrabbingHand.None)
        {
            StartCoroutine(ShowDimSpotlightHint(5));
            yield break;
        }
        
        if (!showedControllerPrompt)
        {
            Animator animator = storyController.GetComponent<Animator>();
            animator.SetTrigger("AnimatePromptController");
            showedControllerPrompt = true;
        }
    }
}
