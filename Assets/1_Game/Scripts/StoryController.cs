using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
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

    [Space] [Header("Progression-related variables")]
    [HideInInspector] public float logoAlpha = 0.0f;
    public MeshRenderer logo;
    
    // ---------------------------------
    
    private int currentStage = 1;
    
    private AudioSource audioSource;
    private UnityAction onStoryStageChanged;
    private float logoTimer = 0.0f;
    private float logoNextStep = 6.0f;
    
    void StartScriptedSequence()
    {
        SetStoryStage(1);
    }

    public void SetStoryStage(int newStage)
    {
        if (!scriptedSequence)
        {
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
        if (currentStage == 1)
        {
            logoTimer += Time.deltaTime;

            Color color = Color.white;
            color.a = logoAlpha;
            logo.material.SetColor("_BaseColor", color);
            
            if (logoTimer >= logoNextStep)
            {
                SetStoryStage(2);
            }
        }
    }

    private void UpdateBlinkingMaterials()
    {
        generator.on = generatorBlinking;
        flash.on = flashBlinking;
        spotlight.on = spotlightBlinking;
        
        float t = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        float emissionAddition = Mathf.Lerp(0.0f, blinkBrightness, t);
        
        foreach (var b in blinkers)
        {
            for (int i = 0; i < b.renderers.Count; i++)
            {
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
                break;
            
            case 2:
                
                break;
        }
    }
    
    // --- Story stages ---

    void ShowLogo()
    {
        Animator animator = GetComponent<Animator>();
        animator.SetTrigger("AnimateLogo"); 
    }

    void SetEasyGenerator()
    {
        
    }

    void SetNormalGenerator()
    {
        
    }

    void SpawnScriptedShip(int number)
    {
        if (number < 1 || number > 3)
        {
            Debug.LogError("Wrong ship number in scripted sequence.");
            return;
        }

        switch (number)
        {
            case 1:

                break;
            
            case 2:

                break;
            
            case 3:

                break;
        }
    }

    void PlayVoiceLine(int num)
    {
        audioSource.clip = voiceLines[num];
        audioSource.time = 0.0f;
        audioSource.Play();
    }
    
    void SpawnFirstPenguinWithAnimation()
    {
        
    }

    void HideSpotlightPenguinShowPullSwitchPenguin()
    {
        
    }

    void EnableUnscriptedShipSpawner()
    {
        
    }
    
    void SetMiscBlinkingData(BlinkingData blinkingData)
    {
        misc = blinkingData;
    }
}
