using System;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class FilteredTransformer : MonoBehaviour, ITransformer
{
    [SerializeField]
    private GrabFreeTransformer _underlyingTransformer;
    
    [SerializeField]
    private Transform _targetTransform;

    [SerializeField]
    private Transform _underlyingTargetTransform;

    [SerializeField, Range(0f, 1f)]
    public float _filterStrength = 0.05f;

    [SerializeField] private Transform _dotProductOrigin;

    [Range(-3.0f, 3.0f)]
    public float dotProductMultiplier = -1.0f;

    [Space]
    public float yawClampA = -70f;
    public float yawClampB = 70f;
    
    public UnityAction onGrab;
    
    private GameManager gameManager;
    private Transform leftController;
    private Transform rightController;
    private Vector3 averageHandsPosition;
    private AdditionalGrabbableLogic additionalGrabbableLogic;
    
    // This is HARDCODED, because this Meta SDK is SO RETARTED that I can't even GET those values from the component...
    public float constraintA = 0.0f;
    public float constraintB = 75.0f;
    public float offset = -0.6f;
    public float dot01min = 0.385f;
    public float dot01max = 1.0f;
    public float clampAOffset = 0.0f;
    public float clampBOffset = 0.0f;
    public float earlierOffset = 0.0f;
    
    public void BeginTransform()
    {
        _underlyingTransformer.BeginTransform();
        onGrab.Invoke();
    }

    public void EndTransform()
    {
        _underlyingTransformer.EndTransform();
    }

    public void Initialize(IGrabbable grabbable)
    {
        _underlyingTransformer.Initialize(grabbable);
        gameManager = Utilities.GetGameManager();
        
        leftController = gameManager.player.GetComponent<OVRCameraRig>().leftControllerAnchor;
        rightController = gameManager.player.GetComponent<OVRCameraRig>().rightControllerAnchor;
        additionalGrabbableLogic = GetComponent<AdditionalGrabbableLogic>();
    }

    void CalculateAverageHandsPos()
    {
        if(additionalGrabbableLogic.holdingHand == GrabbingHand.Both)
        {
            averageHandsPosition = (leftController.position + rightController.position) / 2.0f;   
        }
        else if (additionalGrabbableLogic.holdingHand == GrabbingHand.Left)
        {
            averageHandsPosition = leftController.position;
        }
        else if (additionalGrabbableLogic.holdingHand == GrabbingHand.Right)
        {
            averageHandsPosition = rightController.position;
        }
    }
    
    public void UpdateTransform()
    {
        _underlyingTransformer.UpdateTransform();
            
        // _targetTransform.position = Vector3.Lerp(_targetTransform.position, _underlyingTargetTransform.position, _filterStrength);
        
        // !!! APPLYING ROTATION SLERP
        // _targetTransform.rotation = Quaternion.Slerp(_targetTransform.rotation, _underlyingTargetTransform.rotation, _filterStrength);

        CalculateAverageHandsPos();

        Vector3 referencePoint = _dotProductOrigin.position;
        // referencePoint.y = averageHandsPosition.y;
        
        Vector3 dirVector = (averageHandsPosition - referencePoint).normalized;
        Debug.DrawLine(referencePoint, referencePoint + dirVector);
        // Vector3 forward = dirVector.normalized;
        // Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        // !!! Debug.Log(Vector3.Dot(dirVector, transform.forward));

        // HARDCODED
        float limitA = constraintA;
        float limitB = -Mathf.Sin(Mathf.Deg2Rad * constraintB);
        float dotValue = Vector3.Dot(dirVector, transform.forward) + earlierOffset;
        float dot01 = AK.MapRangeClamped(dotValue, -1.0f, 1.0f, dot01min, dot01max);
        float dotSign = dotValue > 0 ? 1.0f : -1.0f;
        float y = (limitB + limitA) / 2.0f + dotProductMultiplier * Mathf.Abs(dotValue) * dot01 * dotSign;
        // Min = B, because limitB is negative
        y = Mathf.Clamp(y + offset, limitB + clampBOffset, limitA + clampAOffset);
        
        Vector3 targetForward = _underlyingTargetTransform.forward;
        
        // Debug.Log("Y: "+ y + ", dot: " + dotValue);

        // New front point for calculating yaw
        Vector3 v = rightController.position - leftController.position;
        Vector3 yawDir = Vector3.Cross(v, Vector3.up).normalized;
        
        targetForward = yawDir;
        
        targetForward.y = y;
        // targetForward.y = 0;
        if (targetForward == Vector3.zero)
        {
            return;   
        }
        
        targetForward.Normalize();
        Quaternion targetYawRotation = Quaternion.LookRotation(targetForward, Vector3.up);
        
        // Step 1: Interpolate rotation
        _targetTransform.rotation = Quaternion.Slerp(
            _targetTransform.rotation,
            targetYawRotation,
            _filterStrength
        );

        // Extract Euler and normalize Y to [-180, 180]
        Vector3 eulerRotation = _targetTransform.rotation.eulerAngles;
        float yaw = eulerRotation.y;
        if (yaw > 180f) yaw -= 360f;

        yaw = Mathf.Clamp(yaw, yawClampA, yawClampB);

        // Reconstruct the quaternion but clamped
        eulerRotation.y = yaw;
        _targetTransform.rotation = Quaternion.Euler(eulerRotation);

    }
}