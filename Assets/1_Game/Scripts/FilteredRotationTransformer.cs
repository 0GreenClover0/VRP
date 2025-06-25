using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

public class FilteredRotationTransformer : MonoBehaviour, ITransformer
{
    [SerializeField]
    private OneGrabRotateTransformer _underlyingTransformer;

    [SerializeField]
    private Transform _targetTransform;

    [SerializeField]
    private Transform _underlyingTargetTransform;

    [SerializeField, Range(0f, 1f)]
    private float _filterStrength = 0.05f;

    [SerializeField]
    private Vector3 _rotationAxis = Vector3.forward;

    private Transform leftController;
    private Transform rightController;

    public UnityAction onGrab;
    
    private float _lastAngle;
    private float _currentAngle;
    private float _proposedAngle;

    private void Start()
    {
        leftController = GameManager.Instance.player.GetComponent<OVRCameraRig>().leftControllerAnchor;
        rightController = GameManager.Instance.player.GetComponent<OVRCameraRig>().rightControllerAnchor;
    }

    public void BeginTransform()
    {
        _underlyingTransformer.BeginTransform();
        // _underlyingTargetTransform.localRotation = _targetTransform.localRotation;
        // _underlyingTargetTransform.position = _targetTransform.position;
        // _lastAngle = GetLocalAngle();
        onGrab.Invoke();
    }

    public void EndTransform()
    {
        _underlyingTransformer.EndTransform();
    }

    public void Initialize(IGrabbable grabbable)
    {
        _underlyingTransformer.Initialize(grabbable);
    }
    
    // TODO:
    // Add bool for one side rotation
    // Fix odd minor jump forward (direction ok)
    public void UpdateTransform()
    {
        _underlyingTransformer.UpdateTransform();
        
        // Compute rotation delta
        _currentAngle = GetLocalAngle();
        
        // Debug.Log("CurrentAngle: " + GetLocalAngle() + ", LastAngle: " + _lastAngle + ", Delta: " + delta + ", ProposedAngle: " + _proposedAngle);
        
        
        Quaternion currentRot = _targetTransform.localRotation;
        Quaternion targetRot = Quaternion.AngleAxis(_currentAngle, _rotationAxis.normalized);
        _targetTransform.localRotation = Quaternion.Slerp(currentRot, targetRot, _filterStrength);
        _targetTransform.position = Vector3.Lerp(_targetTransform.position, _underlyingTargetTransform.position, _filterStrength);
    }

    private float GetLocalAngle()
    {
        Vector3 localEuler = _underlyingTargetTransform.localRotation.eulerAngles;
        return Vector3.Dot(localEuler, _rotationAxis.normalized);
    }
}
