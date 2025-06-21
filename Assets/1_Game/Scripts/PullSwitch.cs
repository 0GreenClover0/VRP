using System.Linq;
using Oculus.Interaction;
using UnityEngine;

class FakeEnvironmentLightData
{
    public Color fogColor;
    public float ambientIntensity;
    public float skyExposure;
    public float reflectionIntensity;
}

public class PullSwitch : MonoBehaviour
{
    [SerializeField] private InteractableGroupView interactableGroupView;
    [SerializeField] private MaterialPropertyBlockEditor bulbMaterial;
    [SerializeField] private GameObject grabbableObject;
    [SerializeField] private Transform ropeDesiredPosition;
    [SerializeField] private Transform yPositionActivateFlashTreshold;
    [SerializeField] private AudioClip flashSound;
    [SerializeField] private SnapInteractable lightbulbSocket;

    private float flashActiveVisual = -0.1f;
    private AudioSource audioSource;
    private MaterialPropertyColor defaultMaterialColor;
    private MaterialPropertyColor activatedMaterialColor;

    [Space] [Header("Normal light data")]
    [SerializeField] private Color normalFogColor = new Color(0.0f, 0.0f, 0.02f, 1.0f);
    [SerializeField] private float normalAmbientIntensity = 0.3f;
    [SerializeField] private float normalSkyExposure = 1.0f;
    [SerializeField] private float normalReflectionIntensity = 0.275f;
    
    [Space] [Header("Flash light data")]
    [SerializeField] private Color flashFogColor = new Color(0.7f, 0.7f, 1.0f, 1.0f);
    [SerializeField] private float flashAmbientIntensity = 4.0f;
    [SerializeField] private float flashSkyExposure = 3.0f;
    [SerializeField] private float flashReflectionIntensity = 1.0f;
    
    [Space] [Header("Flash curves")]
    [SerializeField] private AnimationCurve lightUp;
    [SerializeField] private AnimationCurve lightDown;
    
    private FakeEnvironmentLightData normalLightData = new FakeEnvironmentLightData();   // LERP A
    private FakeEnvironmentLightData flashLightData = new FakeEnvironmentLightData();    // LERP B

    private const float flashTime = 5.0f;
    private const float returningToDefaultSpeed = 0.1f;

    private bool isBulbInSocket;
    private SnapInteractor currentBulb;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = flashSound;
        
        defaultMaterialColor = new MaterialPropertyColor
        {
            name = "_BaseColor",
            value = Color.white
        };

        activatedMaterialColor = new MaterialPropertyColor
        {
            name = "_BaseColor",
            value = new Color(0.4f, 0.4f, 0.4f, 1.0f)
        };

        normalLightData.fogColor = normalFogColor;
        normalLightData.ambientIntensity = normalAmbientIntensity;
        normalLightData.skyExposure = normalSkyExposure;
        normalLightData.reflectionIntensity = normalReflectionIntensity;

        flashLightData.fogColor = flashFogColor;
        flashLightData.ambientIntensity = flashAmbientIntensity;
        flashLightData.skyExposure = flashSkyExposure;
        flashLightData.reflectionIntensity = flashReflectionIntensity;
    }

    private void Update()
    {
        isBulbInSocket = lightbulbSocket.SelectingInteractors.Count > 0;
        
        if (isBulbInSocket)
        {
            // We always have only one interactor
            currentBulb = lightbulbSocket.SelectingInteractors.First();
        }
        else
        {
            currentBulb = null;
        }
        
        // If nothing is grabbing the string, slowly move up to default position.
        float distance = Vector3.Distance(grabbableObject.transform.position, ropeDesiredPosition.position);
        if (interactableGroupView.SelectingInteractorsCount == 0 && distance > 0.01f)
        {
            grabbableObject.transform.position = Vector3.MoveTowards(grabbableObject.transform.position, ropeDesiredPosition.position, Time.deltaTime * returningToDefaultSpeed);
        }

        if (isBulbInSocket && interactableGroupView.SelectingInteractorsCount != 0 && grabbableObject.transform.position.y < yPositionActivateFlashTreshold.transform.position.y)
        {
            ActivateFlash();
            bulbMaterial.ColorProperties = new System.Collections.Generic.List<MaterialPropertyColor>() { activatedMaterialColor };
        }

        if (flashActiveVisual < 0.0f)
        {
            bulbMaterial.ColorProperties = new System.Collections.Generic.List<MaterialPropertyColor>() { defaultMaterialColor };
        }

        if (flashActiveVisual >= 0.0f && flashActiveVisual <= flashTime)
        {
            float flashCounter01 = AK.MapRangeClamped(flashActiveVisual, 0.0f, flashTime, 0.0f, 1.0f);
            float x = Mathf.Lerp(flashTime, 0.0f, flashCounter01);
            float y = lightDown.Evaluate(x);
            LerpFakeLighting(y);
        }

        flashActiveVisual -= Time.deltaTime / flashTime;
        flashActiveVisual = Mathf.Clamp(flashActiveVisual, -1.0f, float.MaxValue);
    }

    [ContextMenu("Activate flash")]
    private void ActivateFlash()
    {
        flashActiveVisual = flashTime;
        Player.Instance.ActivateFlash();

        if (!audioSource.isPlaying)
        {
            audioSource.Play();   
        }
        
        EjectLightbulb();
    }

    // t: 0-1
    void LerpFakeLighting(float t)
    {
        Color currentFogColor = Color.Lerp(normalLightData.fogColor, flashLightData.fogColor, t);
        float currentAmbientIntensity = Mathf.Lerp(normalLightData.ambientIntensity, flashLightData.ambientIntensity, t);
        float currentSkyExposure = Mathf.Lerp(normalLightData.skyExposure, flashLightData.skyExposure, t);
        float currentReflectionIntensity = Mathf.Lerp(normalLightData.reflectionIntensity, flashLightData.reflectionIntensity, t);
        
        RenderSettings.fogColor = currentFogColor;
        RenderSettings.ambientIntensity = currentAmbientIntensity;
        RenderSettings.skybox.SetFloat("_Exposure", currentSkyExposure);
        RenderSettings.reflectionIntensity = currentReflectionIntensity;
    }
    
    void EjectLightbulb()
    {
        Debug.Log(currentBulb);
        currentBulb.enabled = false;
    }
}
