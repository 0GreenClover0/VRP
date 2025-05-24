using UnityEngine;

public class Generator : MonoBehaviour
{
    [SerializeField]
    private bool distinctDirections = false;

    private Transform leverTransform;
    private float leverZ;
    private float previousZ;
    private float rotationDelta;
    private bool cached = false;
    private GeneratorPower generatorPower;

    void Start()
    {
        leverTransform = transform;
        leverZ = leverTransform.eulerAngles.z;
        previousZ = leverZ;
        generatorPower = GetComponent<GeneratorPower>();
    }

    void Update()
    {
        leverZ = leverTransform.eulerAngles.z;

        if (cached)
        {
            previousZ = leverZ;
        }

        rotationDelta = distinctDirections ? leverZ - previousZ : Mathf.Abs(leverZ - previousZ);
        generatorPower.rotationDelta = leverZ - previousZ; // For visual presentation we shouldn't have an absolute value

        cached = !cached;
    }
}
