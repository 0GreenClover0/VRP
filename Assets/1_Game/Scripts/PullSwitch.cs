using Oculus.Interaction;
using UnityEngine;

public class PullSwitch : MonoBehaviour
{
    [SerializeField] private InteractableGroupView interactableGroupView;
    [SerializeField] private MaterialPropertyBlockEditor bulbMaterial;
    [SerializeField] private GameObject grabbableObject;
    [SerializeField] private Transform desiredPosition;
    [SerializeField] private Transform yPositionActivateFlashTreshold;

    private float flashActive = 0.0f;
    private MaterialPropertyColor defaultMaterialColor;
    private MaterialPropertyColor activatedMaterialColor;

    private const float flashTime = 5.0f;
    private const float returningToDefaultSpeed = 0.1f;

    private void Awake()
    {
        defaultMaterialColor = new MaterialPropertyColor
        {
            name = "_Color",
            value = Color.white
        };

        activatedMaterialColor = new MaterialPropertyColor
        {
            name = "_Color",
            value = Color.yellow
        };
    }

    private void Update()
    {
        // If nothing is grabbing the string, slowly move up to default position.
        float distance = Vector3.Distance(grabbableObject.transform.position, desiredPosition.position);
        if (interactableGroupView.SelectingInteractorsCount == 0 && distance > 0.01f)
        {
            grabbableObject.transform.position = Vector3.MoveTowards(grabbableObject.transform.position, desiredPosition.position, Time.deltaTime * returningToDefaultSpeed);
        }

        if (flashActive <= 0.0f && interactableGroupView.SelectingInteractorsCount != 0 && grabbableObject.transform.position.y < yPositionActivateFlashTreshold.transform.position.y)
        {
            bulbMaterial.ColorProperties = new System.Collections.Generic.List<MaterialPropertyColor>() { activatedMaterialColor };
            flashActive = flashTime;
        }

        if (flashActive <= 0.0f)
        {
            bulbMaterial.ColorProperties = new System.Collections.Generic.List<MaterialPropertyColor>() { defaultMaterialColor };
        }

        flashActive -= Time.deltaTime;
    }
}
