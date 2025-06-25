using System.Collections;
using Oculus.Interaction;
using UnityEngine;

public class Generator : MonoBehaviour
{
    private Transform leverTransform;
    private float leverZ;
    private float previousZ;
    private float rotationDelta;
    private bool cached = false;
    private GeneratorPower generatorPower;
    private OneGrabRotateTransformer rotateTransformer;

    private float powerGeneratorCooldown = 10.0f;
    private bool remindedAboutGenerator = false;
    private InteractableGroupView igv;
    
    void Start()
    {
        leverTransform = transform;
        leverZ = leverTransform.eulerAngles.z;
        previousZ = leverZ;
        generatorPower = GetComponent<GeneratorPower>();
        rotateTransformer = GetComponent<OneGrabRotateTransformer>();
        igv = GetComponent<InteractableGroupView>();
    }

    void Update()
    {
        leverZ = leverTransform.eulerAngles.z;
        
        if (cached)
        {
            previousZ = leverZ;
        }
    
        rotationDelta = -(leverZ - previousZ);
        rotationDelta = Mathf.Clamp(rotationDelta, 0.0f, rotationDelta);
        generatorPower.rotationDelta = rotationDelta;
        
        if (generatorPower.GetCurrentGeneratorPower() < 15.0f && generatorPower.GetCurrentGeneratorPower() > 0.0f
            && !remindedAboutGenerator && igv.SelectingInteractorsCount <= 0)
        {
            if (GameManager.Instance.storyController != null)
            {
                GameManager.Instance.storyController.PlayEmergentVoiceline(Random.Range(6, 8));
            }

            remindedAboutGenerator = true;
            StartCoroutine(ResetRemindedAboutGenerator());
        }
    
        cached = !cached;
    }
    
    IEnumerator ResetRemindedAboutGenerator()
    {
        yield return new WaitForSeconds(powerGeneratorCooldown);
        remindedAboutGenerator = false;
    }
}
