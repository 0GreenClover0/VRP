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

    void Start()
    {
        leverTransform = transform;
        leverZ = leverTransform.eulerAngles.z;
        previousZ = leverZ;
        generatorPower = GetComponent<GeneratorPower>();
        rotateTransformer = GetComponent<OneGrabRotateTransformer>();
    }

    void Update()
    {
        leverZ = leverTransform.eulerAngles.z;
        
        if (cached)
        {
            previousZ = leverZ;
        }
    
        rotationDelta = leverZ - previousZ;
        generatorPower.rotationDelta = rotationDelta;
    
        cached = !cached;
    }
}
