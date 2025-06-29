using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Windows;

public class GeneratorPower : MonoBehaviour
{
    [HideInInspector]
    public float rotationDelta;

    [Tooltip("Seconds interval of how often is rotationDelta added to currentGeneratorPower")]
    [Range(0.1f, 10.0f)]
    public float chargeTimer = 1.0f;

    [Tooltip("Multiplier of the value added to currentGeneratorPower each interval")]
    [Range(0.1f, 10.0f)]
    public float powerMultiplier = 1.5f;

    [Tooltip("How much power is taken away each interval when not charged")]
    [Range(0.1f, 10.0f)]
    public float powerDecrease = 1.0f;

    //////////////////////////////////
    [Space]
    [Header("Visualization")]
    [Space]
    //////////////////////////////////

    [Tooltip("Time in seconds that is needed to interpolate the bar from 0 to 100%")]
    public float speed = 1.0f;

    public float minRotationDelta = 3.0f;

    public Transform barTransform;

    [Space]

    public Transform mechanism1;
    public Transform mechanism2;
    public Transform mechanism3;
    public Transform mechanism4;
    public Transform mechanism5;
    public Transform mechanism6;

    [HideInInspector] public bool spotlightTurnedDown = false;
    
    // 0-100
    [HideInInspector] public float currentGeneratorPower;
    private float currentTime = 0.0f;
    private float barValue = 0.0f;
    private float t = 0.0f;
    private float lastA = 0.0f, lastB = 0.0f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        barValue = GetCurrentGeneratorPower();
    }

    void OnTimer()
    {
        if (Mathf.Abs(rotationDelta) > minRotationDelta)
        {
            currentGeneratorPower += Mathf.Abs(rotationDelta) * powerMultiplier;   
        }

        if (rotationDelta < Mathf.Abs(0.001f) && !spotlightTurnedDown)
        {
            currentGeneratorPower -= powerDecrease;
        }

        currentGeneratorPower = GetCurrentGeneratorPower();
        t = 0.0f;
        lastA = barValue;
        lastB = currentGeneratorPower;

        if ((!audioSource.isPlaying || audioSource.time > Random.Range(0.05f, 0.1f)) && Mathf.Abs(rotationDelta) > minRotationDelta)
        {
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.Play();
        }
    }

    // In range 0-100%, not 0-1
    float LerpPerTick()
    {
        return 100.0f / (speed * (1.0f / Time.fixedDeltaTime));
    }

    // 0-1
    float LerpPerTick01()
    {
        return LerpPerTick() * 0.01f;
    }

    private void FixedUpdate()
    {
        currentTime += Time.fixedDeltaTime;
        if (currentTime >= chargeTimer)
        {
            OnTimer();
            currentTime = 0.0f;
        }

        t += LerpPerTick01();
        barValue = Mathf.Lerp(lastA, lastB, t);
        barValue = Mathf.Clamp(barValue, 0, 100.0f);
        //Debug.Log("Current Generator Value: " + currentGeneratorPower + ", Bar Value: " + barValue + ", Rotation Delta: " + rotationDelta);

        SetBarVisuals();
        SetMechanismVisuals();
    }

    public float GetCurrentGeneratorPower()
    {
        return Mathf.Clamp(currentGeneratorPower, 0.0f, 100.0f);
    }

    public float GetCurrentBarValue()
    {
        return Mathf.Clamp(barValue, 0.0f, 100.0f);
    }

    void SetBarVisuals()
    {
        Vector3 scale = barTransform.localScale;
        scale.x = AK.MapRangeClamped(barValue, 0.0f, 100.0f, 0.0f, 0.04f);
        barTransform.localScale = scale;
    }

    void SetMechanismVisuals()
    {
        Vector3 v1 = mechanism1.transform.localEulerAngles;
        Vector3 v2 = mechanism2.transform.localEulerAngles;
        Vector3 v3 = mechanism3.transform.localEulerAngles;
        Vector3 v4 = mechanism4.transform.localEulerAngles;
        Vector3 v5 = mechanism4.transform.localEulerAngles;
        Vector3 v6 = mechanism4.transform.localEulerAngles;

        v1.z += rotationDelta;
        v2.z -= rotationDelta;
        v3.z += rotationDelta;
        v4.z -= rotationDelta;
        v5.z += rotationDelta;
        v6.z += rotationDelta;

        mechanism1.transform.localEulerAngles = v1;
        mechanism2.transform.localEulerAngles = v2;
        mechanism3.transform.localEulerAngles = v3;
        mechanism4.transform.localEulerAngles = v4;
        mechanism5.transform.localEulerAngles = v5;
        mechanism6.transform.localEulerAngles = v6;
    }
}
