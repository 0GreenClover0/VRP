using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

public enum BlinkingType
{
    Generator,
    Spotlight,
    Flash,
    Misc
}

public class BlinkingData
{
    public BlinkingType type;
    public bool on;
    public List<MeshRenderer> renderers;
    public List<Color> baseEmissionColors;
}

public class StoryController : MonoBehaviour
{
    [Header("Changing this requires reloading the game")]
    public bool scriptedSequence = true;

    [Header("Set manual blinking")]
    public bool generatorBlinking;
    public bool spotlightBlinking;
    public bool flashBlinking;
    public bool miscBlinking;

    [Space]
    public float blinkBrightness = 3.0f;
    public float blinkSpeed = 3.0f;
    
    [Header("Blinking renderers")]
    public List<MeshRenderer> generatorRenderers;
    public List<MeshRenderer> spotlightRenderers;
    public List<MeshRenderer> flashRenderers;
    public List<MeshRenderer> miscRenderers;
    
    private BlinkingData generator;
    private BlinkingData spotlight;
    private BlinkingData flash;
    private BlinkingData misc;
    
    private List<BlinkingData> blinkers;
    
    [Header("Voicelines")]
    public List<AudioClip> voiceLines;
    
    [HideInInspector] public float logoAlpha = 0.0f;
    
    [Space]
    [Header("Progression-related variables")]
    public MeshRenderer logo;
    public GeneratorPower generatorPower;
    public float easyPowerMultiplier = 4;
    public float normalPowerMultiplier = 2;
    public float easyPowerDecrease = 0.15f;
    public float normalPowerDecrease = 0.5f;
    public GameObject spotlightPenguin;
    public FilteredTransformer spotlightFilteredTransformer;

    [Space] public ShipSpawner shipSpawner;
    [Header("Scripted ship transforms (PER LEVEL!)")]
    public Transform scriptedShipTransform1;
    public Transform scriptedShipTransform2;
    public Transform scriptedShipTransform3;
    public Transform pirates1;
    public Transform pirates2;
    
    // ---------------------------------
    
    private int currentStage = 1;
    
    private AudioSource audioSource;
    private UnityAction onStoryStageChanged;
    private float logoTimer = 0.0f;
    private float logoNextStep = 7.0f;
    private float firstPenguinVoicelineTimer = 0.0f;
    private bool firstPenguinVoicelinePlayed = false;
    private bool firstChargedGenerator = false;
    private bool firstStage3GeneratorGrabbed = false;
    private bool firstStage4SpotlightGrabbed = false;
    private FilteredRotationTransformer generatorRotationTransformer;
    private MeshRenderer scriptedShipMesh1;
    private MeshRenderer scriptedShipMesh2;
    private MeshRenderer scriptedShipMesh3;
    private bool startedControllingShip1;
    
    void StartScriptedSequence()
    {
        SetStoryStage(1);
    }

    public void SetStoryStage(int newStage)
    {
        if (!scriptedSequence)
        {
            LevelController.Instance.IsDuringScriptedSequence = false;
            Debug.LogWarning("Scripted sequence is disabled and setting story steps has no effect.");
            return;
        }

        currentStage = newStage;
        onStoryStageChanged.Invoke();
    }

    void InitializeBlinkingData()
    {
        generator = new BlinkingData { renderers = generatorRenderers, type = BlinkingType.Generator, baseEmissionColors = new List<Color>() };
        spotlight = new BlinkingData { renderers = spotlightRenderers, type = BlinkingType.Spotlight, baseEmissionColors = new List<Color>() };
        flash = new BlinkingData { renderers = flashRenderers, type = BlinkingType.Flash, baseEmissionColors = new List<Color>() };
        misc = new BlinkingData { renderers = miscRenderers, type = BlinkingType.Misc, baseEmissionColors = new List<Color>() };
        
        blinkers = new List<BlinkingData> { generator, spotlight, flash, misc };

        foreach (var b in blinkers)
        {
            foreach (var m in b.renderers)
            {
                Material mat = m.material;
                mat.EnableKeyword("_EMISSION");
                Color emissionColor = mat.GetColor("_EmissionColor");
                b.baseEmissionColors.Add(emissionColor);
            }
        }
    }
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        onStoryStageChanged += EvaluateScriptedSequence;
        generatorRotationTransformer = generatorPower.gameObject.GetComponent<FilteredRotationTransformer>();
        generatorRotationTransformer.onGrab += OnFirstStage3GeneratorGrab;
        spotlightFilteredTransformer.onGrab += OnFirstStage4SpotlightGrab;
        InitializeBlinkingData();
        StartScriptedSequence();
    }

    private void Update()
    {
        BackgroundChecks();
        UpdateBlinkingMaterials();
    }

    void BackgroundChecks()
    {
        switch (currentStage)
        {
            case 1:
                logoTimer += Time.deltaTime;

                Color color = Color.white;
                color.a = logoAlpha;
                logo.material.SetColor("_BaseColor", color);
            
                if (logoTimer >= logoNextStep)
                {
                    SetStoryStage(2);
                }
                break;
            
            case 2:
                firstPenguinVoicelineTimer += Time.deltaTime;
            
                if (firstPenguinVoicelineTimer >= 3.0f && !firstPenguinVoicelinePlayed)
                {
                    PlayVoiceLine(0);
                    generatorBlinking = true;
                    SetStoryStage(3);
                    firstPenguinVoicelinePlayed = true;
                }
                break;
            
            case 3:
                WaitForPoweredGenerator();
                break;
        }
    }

    private void UpdateBlinkingMaterials()
    {
        generator.on = generatorBlinking;
        flash.on = flashBlinking;
        spotlight.on = spotlightBlinking;
        misc.on = miscBlinking;
        
        float t = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        float emissionAddition = Mathf.Lerp(0.0f, blinkBrightness, t);
        
        foreach (var b in blinkers)
        {
            for (int i = 0; i < b.renderers.Count; i++)
            {
                if (b.renderers.Count == 0)
                {
                    return;
                }
                
                Color baseEmission = b.baseEmissionColors[i];
                Material mat = b.renderers[i].material;
                // mat.EnableKeyword("_EMISSION");
                float newEmission = baseEmission.maxColorComponent + emissionAddition;
                
                if (b.on)
                {
                    mat.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 1.0f, 1.0f) * newEmission);
                }
                else
                {
                    mat.SetColor("_EmissionColor", baseEmission);
                }
            }
        }
    }
    
    private void EvaluateScriptedSequence()
    {
        switch (currentStage)
        {
            case 1:
                ShowLogo();
                SetEasyGenerator();
                LockSpotlightAndGenerator();
                break;
            
            case 2:
                UnlockGenerator();
                SpawnFirstPenguinWithAnimation();
                break;
            
            case 3:
                break;
            
            case 4:
                SpawnScriptedShip(1);
                UnlockSpotlight();
                StartSpotlightBlinking();
                break;
            
            case 5:
                StartShip1Blinking();
                break;
            
            case 6:
                StopMiscBlinking();
                break;
        }
    }
    
    // --- Story stages ---

    void LockSpotlightAndGenerator()
    {
        // spotlightFilteredTransformer.gameObject.GetComponent<Grabbable>().enabled = false;
        // generatorRotationTransformer.gameObject.GetComponent<Grabbable>().enabled = false;

        spotlightFilteredTransformer.gameObject.GetComponent<Grabbable>().MaxGrabPoints = 0;
        generatorRotationTransformer.gameObject.GetComponent<Grabbable>().MaxGrabPoints = 0;
    }

    void UnlockSpotlight()
    {
        spotlightFilteredTransformer.gameObject.GetComponent<Grabbable>().MaxGrabPoints = 2;
    }

    void UnlockGenerator()
    {
        generatorRotationTransformer.gameObject.GetComponent<Grabbable>().MaxGrabPoints = 1;
    }
    
    void ShowLogo()
    {
        Animator animator = GetComponent<Animator>();
        animator.SetTrigger("AnimateLogo"); 
    }
    
    void SetEasyGenerator()
    {
        generatorPower.powerMultiplier = easyPowerMultiplier;
        generatorPower.powerDecrease = easyPowerDecrease;
    }

    void SetNormalGenerator()
    {
        generatorPower.powerMultiplier = normalPowerMultiplier;
        generatorPower.powerDecrease = normalPowerDecrease;
    }

    void OnFirstStage3GeneratorGrab()
    {
        if (!firstStage3GeneratorGrabbed)
        {
            HideSpotlightPenguinShowPullSwitchPenguin();
            generatorBlinking = false;
            firstStage3GeneratorGrabbed = true;
        }
    }

    void OnFirstStage4SpotlightGrab()
    {
        // Stop spotlight blinking
        if (!firstStage4SpotlightGrabbed)
        {
            blinkBrightness *= 10.0f;
            spotlightBlinking = false;
            firstStage4SpotlightGrabbed = true;
            SetStoryStage(5);
        }
    }
    
    void WaitForPoweredGenerator()
    {
        if (generatorPower.GetCurrentBarValue() >= 70.0f && !firstChargedGenerator)
        {
            PlayVoiceLine(0);
            SetStoryStage(4);
            firstChargedGenerator = true;
        }
    }

    void SpawnScriptedShip(int num)
    {
        if (num < 1 || num > 3)
        {
            Debug.LogError("Wrong ship number in scripted sequence.");
            return;
        }
        
        switch (num)
        {
            case 1:
                var ship = shipSpawner.SpawnShipAtPosition(ShipType.FoodBig, scriptedShipTransform1.position, true);
                scriptedShipMesh1 = ship.rendererReference;
                ship.onShipStateChanged += ControlShip1;
                break;
            
            case 2:

                break;
            
            case 3:

                break;
        }
    }

    void PlayVoiceLine(int id)
    {
        audioSource.clip = voiceLines[id];
        audioSource.time = 0.0f;
        audioSource.Play();
    }
    
    void SpawnFirstPenguinWithAnimation()
    {
        spotlightPenguin.SetActive(true);
        spotlightPenguin.GetComponent<Animator>().SetTrigger("Climb");
    }

    void HideSpotlightPenguinShowPullSwitchPenguin()
    {
        spotlightPenguin.GetComponent<Animator>().SetTrigger("Hide");
        // TODO: Show PullSwitch penguin!!!
    }

    void StartSpotlightBlinking()
    {
        blinkBrightness /= 10.0f;
        spotlightBlinking = true;
    }

    void StartShip1Blinking()
    {
        StartMiscBlinking(true, new List<MeshRenderer>() {scriptedShipMesh1});
    }

    void ControlShip1(BehavioralState newState)
    {
        if (newState == BehavioralState.Control && !startedControllingShip1)
        {
            startedControllingShip1 = true;
            SetStoryStage(6);
        }
    }

    void StartMiscBlinking(bool allowBlinking, List<MeshRenderer> renderersList)
    {
        BlinkingData b = new BlinkingData
        {
            baseEmissionColors = new List<Color>(),
            renderers = renderersList,
            type = BlinkingType.Misc,
            on = allowBlinking
        };
        
        foreach (var m in b.renderers)
        {
            Material mat = m.material;
            mat.EnableKeyword("_EMISSION");
            Color emissionColor = mat.GetColor("_EmissionColor");
            b.baseEmissionColors.Add(emissionColor);
        }

        blinkBrightness *= 10.0f;

        misc = b;
        blinkers[3] = misc;
        miscBlinking = allowBlinking;
    }

    void StopMiscBlinking()
    {
        blinkBrightness /= 10.0f;
        miscBlinking = false;
    }
    
    void EnableUnscriptedShipSpawner()
    {
        
    }
}
