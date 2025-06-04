using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SpotlightController : MonoBehaviour
{
    RaycastHit hit;

    [FormerlySerializedAs("enableSpotlightLogic")] [Tooltip("If the spotlight should be enabled and also attract ships (false if, e.g. generator power is 0)")]
    public bool spotlightEnabled = true;
    public AnimationCurve coneIntensityOverPower;
    public AnimationCurve waterConeIntensityOverPower;
    public GeneratorPower generatorPowerScript;

    [Space]

    public Transform waterConeTransform;
    public GameObject spotlightConeRef;
    private LighthouseLight lighthouseLight;

    [Range(0.01f, 5.0f)]
    public float waterConeRadius = 1.75f;

    private float currentBarValue01;

    private void Awake()
    {
        lighthouseLight = waterConeTransform.gameObject.GetComponent<LighthouseLight>();
        spotlightEnabled = false;
    }

    private void Start()
    {
        if (generatorPowerScript == null)
        {
            Debug.LogError("In this scene, GeneratorPower script is not assigned to the Spotlight (to control spotlight intensity over generator power)");
        }
    }

    private void EnableLighthouseLight(bool enable)
    {
        lighthouseLight.enabled = enable;
        waterConeTransform.gameObject.SetActive(enable);
        spotlightConeRef.gameObject.SetActive(enable);
    }
    
    void Update()
    {
        EvaluateGeneratorPower();
        
        if (spotlightEnabled)
        {
            EnableLighthouseLight(true);
            ShootRaycast();
        }
        else
        {
            EnableLighthouseLight(false);
        }
    }

    void EvaluateGeneratorPower()
    {
        if (generatorPowerScript.GetCurrentBarValue() > 0.001f)
        {
            spotlightEnabled = true;
        }
        else
        {
            spotlightEnabled = false;
        }
        
        currentBarValue01 = generatorPowerScript.GetCurrentBarValue() * 0.01f;

        float coneIntensity = coneIntensityOverPower.Evaluate(currentBarValue01);
        float coneOpacity = AK.MapRangeClamped(coneIntensity, 0.0f, 1.0f, 0.0f, 0.3f);

        float waterConeIntensity = waterConeIntensityOverPower.Evaluate(currentBarValue01);
        float waterConeOpacity = AK.MapRangeClamped(waterConeIntensity, 0.0f, 1.0f, 0.0f, 0.3f);

        Material coneMaterial = spotlightConeRef.GetComponent<MeshRenderer>().material;
        Material waterConeMaterial = waterConeTransform.gameObject.GetComponent<MeshRenderer>().material;

        Color coneColor = coneMaterial.color;
        coneColor.a = coneOpacity;
        coneMaterial.color = coneColor;

        Color waterConeColor = waterConeMaterial.color;
        waterConeColor.a = waterConeOpacity;
        waterConeMaterial.color = waterConeColor;
    }

    void ShootRaycast()
    {
        Vector3 scale = waterConeTransform.localScale;
        scale.x = waterConeRadius * 2.0f;
        scale.z = waterConeRadius * 2.0f;
        waterConeTransform.localScale = scale;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 500))
        {
            if (!waterConeTransform.gameObject.activeInHierarchy)
                waterConeTransform.gameObject.SetActive(true);

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 500.0f, Color.yellow);
            Debug.DrawLine(hit.point, hit.point + Vector3.up * 0.5f, Color.red);
            waterConeTransform.position = hit.point + Vector3.up * 0.05f;
        }
        else if (waterConeTransform.gameObject.activeInHierarchy)
        {
            waterConeTransform.gameObject.SetActive(false);
            waterConeTransform.gameObject.GetComponent<LighthouseLight>().enabled = false;
        }
    }
}
