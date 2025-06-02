using System;
using UnityEngine;

public struct FloaterSettings
{
    public FloaterSettings(float sinkRate, float sideRotationStrength, float forwardRotationStrength, float sideFloatersOffset, float forwardFloatersOffset)
    {
        sink_rate = sinkRate;
        side_rotation_strength = sideRotationStrength;
        forward_rotation_strength = forwardRotationStrength;
        side_floaters_offset = sideFloatersOffset;
        forward_floaters_offset = forwardFloatersOffset;
    }

    float sink_rate;
    float side_rotation_strength;
    float forward_rotation_strength;
    float side_floaters_offset;
    float forward_floaters_offset;
}

public class Floater : MonoBehaviour
{
    [SerializeField] private float sink;
    [SerializeField] private float sideFloatersOffset;
    [SerializeField] private float sideRotationStrength;
    [SerializeField] private float forwardRotationStrength;
    [SerializeField] private float forwardFloatersOffset;
    
    private void Awake()
    {
        
    }

    private void Update()
    {
        // Vector3 position = transform.position;
        // Vector2 position2D = new Vector2(position.x, position.z);
        // Vector2 movementDirection = new Vector2(transform.forward.normalized.x, transform.forward.normalized.z);
        // Vector2 perpendicularToMovementDirection = new Vector2(movementDirection.y, -movementDirection.x);
        //
        // float heightToTheLeft = water.GetWaveHeight(position2D + perpendicularToMovementDirection * sideFloatersOffset);
        // float heightToTheRight = water.GetWaveHeight(position2D - perpendicularToMovementDirection * sideFloatersOffset);
        // float height = water.GetWaveHeight(position2D) - sink;
        // float heightAtFront = water.GetWaveHeight(position2D + movementDirection * forwardFloatersOffset);
        // float heightAtBack = water.GetWaveHeight(position2D - movementDirection * forwardFloatersOffset);
        //
        // // Set new position with updated Y (water height)
        // transform.position = new Vector3(position2D.x, height, position2D.y);
        //
        // // Calculate pitch (rotation around X) and roll (rotation around Z)
        // float rotationValue = (heightAtFront - heightAtBack) * forwardRotationStrength;
        // Quaternion pitchRotation = Quaternion.AngleAxis(rotationValue, transform.right);
        //
        // float sideRotationValue = (heightToTheLeft - heightToTheRight) * sideRotationStrength;
        // Quaternion rollRotation = Quaternion.AngleAxis(sideRotationValue, transform.forward);
        //
        // // Combine rotations
        // Quaternion finalRotation = rollRotation * pitchRotation;
        // Vector3 euler = finalRotation.eulerAngles;
        // Vector3 currentEuler = transform.eulerAngles;
        //
        // transform.eulerAngles = new Vector3(euler.x, currentEuler.y, euler.z);
    }
}
