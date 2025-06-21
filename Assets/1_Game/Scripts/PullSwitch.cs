using System.Linq;
using Oculus.Interaction;
using UnityEngine;

public class PullSwitch : MonoBehaviour
{
    [SerializeField] private InteractableGroupView interactableGroupView;
    [SerializeField] private Light pointLight;
    [SerializeField] private MaterialPropertyBlockEditor bulbMaterial;
    [SerializeField] private GameObject grabbableObject;
    [SerializeField] private Transform ropeDesiredPosition;
    [SerializeField] private Transform yPositionActivateFlashTreshold;
    [SerializeField] private AudioClip flashSound;
    [SerializeField] private SnapInteractable lightbulbSocket;

    private float flashActiveVisual = 0.0f;
    private AudioSource audioSource;
    private MaterialPropertyColor defaultMaterialColor;
    private MaterialPropertyColor activatedMaterialColor;

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
            value = Color.yellow
        };
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

        if (isBulbInSocket && flashActiveVisual <= 0.0f && interactableGroupView.SelectingInteractorsCount != 0 && grabbableObject.transform.position.y < yPositionActivateFlashTreshold.transform.position.y)
        {
            ActivateFlash();
            bulbMaterial.ColorProperties = new System.Collections.Generic.List<MaterialPropertyColor>() { activatedMaterialColor };
        }

        if (flashActiveVisual <= 0.0f)
        {
            bulbMaterial.ColorProperties = new System.Collections.Generic.List<MaterialPropertyColor>() { defaultMaterialColor };
        }

        if (flashActiveVisual >= 3.0f && flashActiveVisual <= 5.0f)
        {
            if (!pointLight.enabled)
            {
                pointLight.enabled = true;
            }

            float t = Mathf.InverseLerp(5.0f, 3.0f, flashActiveVisual); // t goes from 0.0 to 1.0
            float ramp = Mathf.Pow(t, 1.5f); // Faster ramp-up
            float fade = Mathf.Pow(1.0f - t, 1.2f); // Slower fade-out
            float shaped = ramp * fade * 4.0f; // Bell-like pulse shape
            pointLight.intensity = shaped * 100.0f;
        }
        else
        {
            pointLight.intensity = 0.0f;

            if (pointLight.enabled)
            {
                pointLight.enabled = false;
            }
        }

        flashActiveVisual -= Time.deltaTime;
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

    void EjectLightbulb()
    {
        Debug.Log(currentBulb);
        currentBulb.enabled = false;
    }
}
