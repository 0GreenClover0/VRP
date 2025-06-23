using System.Collections;
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

    [Space]
    public GameManager gameManagerRef;
    
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
    public List<AudioClip> emergentVoiceLines; // if this doesnt fire, check if StoryController is not disabled in runtime
    public List<AudioClip> gameOverVoiceLines; // if this doesnt fire, check if StoryController is not disabled in runtime
    
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
    public Animator pullSwitchAnimator;

    [Space] public ShipSpawner shipSpawner;
    [Header("Scripted ship transforms (PER LEVEL!)")]
    public Transform scriptedShipTransform1;
    public Transform scriptedShipTransform2;
    public Transform scriptedShipTransform3;
    public Transform pirates1;
    public Transform pirates2;
    
    // ---------------------------------
    
    public int currentStage { get; private set; } = 1 ;
    
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
    private float secondScriptedShipTimer = 0.0f;
    private float thirdScriptedShipTimer = 0.0f;
    private bool secondScriptedShipSpawned = false;
    private bool thirdScriptedShipSpawned = false;
    private int conditionCounter = 0;
    private float finishScriptedSequenceTimer = 0.0f;
    private bool triggeredFinishingScriptedSequence = false;
    
    void StartScriptedSequence()
    {
        SetStoryStage(1);
    }

    public void SetStoryStage(int newStage)
    {
        if (!scriptedSequence)
        {
            LevelController.Instance.IsDuringScriptedSequence = false;
            // Debug.LogWarning("Scripted sequence is disabled and setting story steps has no effect.");
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
    
    // Awake is too early, because we need to get LevelController (it's null on Awake)
    private void Start()
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
                    PlayVoiceLine(5);
                }
                break;
            
            case 2:
                firstPenguinVoicelineTimer += Time.deltaTime;
            
                if (firstPenguinVoicelineTimer >= 8.0f && !firstPenguinVoicelinePlayed)
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
            
            case 7:
                secondScriptedShipTimer += Time.deltaTime;
            
                if (secondScriptedShipTimer >= 5.0f && !secondScriptedShipSpawned)
                {
                    PlayVoiceLine(2);
                    SpawnScriptedShip(2);
                    secondScriptedShipSpawned = true;
                }
                break;
            
            case 8:
                thirdScriptedShipTimer += Time.deltaTime;
            
                if (thirdScriptedShipTimer >= 5.0f && !thirdScriptedShipSpawned)
                {
                    PlayVoiceLine(3);
                    SpawnScriptedShip(3);
                    SpawnScriptedShip(4);
                    SpawnScriptedShip(5);
                    thirdScriptedShipSpawned = true;
                }

                if (conditionCounter >= 3)
                {
                    finishScriptedSequenceTimer += Time.deltaTime;

                    if (!triggeredFinishingScriptedSequence && finishScriptedSequenceTimer >= 5.0f)
                    {
                        SetStoryStage(9);
                        triggeredFinishingScriptedSequence = true;
                    }
                }
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
                    break;
                }
                
                Color baseEmission = b.baseEmissionColors[i];
                if (b.renderers[i] == null || b.renderers.Count == 0 || b.renderers[i].material == null)
                {
                    continue;
                }
                
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
                DisablePullSwitchPenguin();
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
            
            case 9:
                FinishScriptedSequence();
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

    void DisablePullSwitchPenguin()
    {
        foreach (var mesh in pullSwitchAnimator.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            mesh.enabled = false;
        }
        
        // pullSwitchAnimator.gameObject.SetActive(false);
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
    
    void FinishScriptedSequence()
    {
        SetNormalGenerator();
        PlayVoiceLine(4);
        scriptedSequence = false;
        LevelController.Instance.IsDuringScriptedSequence = false;
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
            HideSpotlightPenguinShowPullSwitchPenguin();
            PlayVoiceLine(1);
            SetStoryStage(4);
            firstChargedGenerator = true;
        }
    }
    
    void SpawnScriptedShip(int num)
    {
        if (num < 1 || num > 5)
        {
            Debug.LogError("Wrong ship number in scripted sequence.");
            return;
        }
        
        switch (num)
        {
            case 1:
                var ship1 = shipSpawner.SpawnShipAtPosition(ShipType.FoodBig, scriptedShipTransform1.position, true);
                scriptedShipMesh1 = ship1.rendererReference;
                ship1.onShipStateChanged += OnShip1StateChange;
                break;
            
            case 2:
                var ship2 = shipSpawner.SpawnShipAtPosition(ShipType.WoodBig, scriptedShipTransform2.position);
                scriptedShipMesh2 = ship2.rendererReference;
                ship2.onShipStateChanged += OnShip2StateChange;
                break;
            
            case 3:
                var ship3 = shipSpawner.SpawnShipAtPosition(ShipType.FoodBig, scriptedShipTransform3.position);
                scriptedShipMesh3 = ship3.rendererReference;
                ship3.onShipStateChanged += OnShip3StateChange;
                break;
            
            // Pirates
            case 4:
                var ship4 = shipSpawner.SpawnShipAtPosition(ShipType.Pirates, pirates1.position);
                ship4.onShipStateChanged += OnShip4StateChange;
                break;
            
            case 5:
                var ship5 = shipSpawner.SpawnShipAtPosition(ShipType.Pirates, pirates2.position);
                ship5.onShipStateChanged += OnShip5StateChange;
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
        StartCoroutine(PullSwitchPenguinEnter());
    }

    IEnumerator PullSwitchPenguinEnter()
    {
        yield return new WaitForSeconds(3.5f);
        pullSwitchAnimator.GetComponent<Animator>().SetTrigger("Enter");
        yield return new WaitForSeconds(0.5f);
        foreach (var mesh in pullSwitchAnimator.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            mesh.enabled = true;
        }
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

    void OnShip1StateChange(BehavioralState newState)
    {
        if (newState == BehavioralState.Control && !startedControllingShip1)
        {
            startedControllingShip1 = true;
            SetStoryStage(6);
        }

        if (newState == BehavioralState.InPort)
        {
            SetStoryStage(7);
        }
    }

    void OnShip2StateChange(BehavioralState newState)
    {
        if (newState == BehavioralState.InPort)
        {
            SetStoryStage(8);
        }
    }
    
    void OnShip3StateChange(BehavioralState newState)
    {
        if (newState == BehavioralState.InPort)
        {
            conditionCounter++;
        }
    }
    
    void OnShip4StateChange(BehavioralState newState)
    {
        // TODO: VERY IMPORTANT! This should take into account guiding pirates safely
        // From one entrance to another, without destroying them
        if (newState == BehavioralState.Destroyed)
        {
            conditionCounter++;
        }
    }
    
    void OnShip5StateChange(BehavioralState newState)
    {
        // TODO: VERY IMPORTANT! This should take into account guiding pirates safely
        // From one entrance to another, without destroying them
        if (newState == BehavioralState.Destroyed)
        {
            conditionCounter++;
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
}
