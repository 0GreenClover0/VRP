using System;
using Oculus.Interaction;
using UnityEngine;

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

    private float _lastAngle;
    private float _currentAngle;
    private float _proposedAngle;

    public void BeginTransform()
    {
        _underlyingTransformer.BeginTransform();
        _underlyingTargetTransform.localRotation = _targetTransform.localRotation;
        _underlyingTargetTransform.position = _targetTransform.position;
        _lastAngle = GetLocalAngle();
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
        float delta = Mathf.DeltaAngle(_lastAngle, _currentAngle);
        _proposedAngle = _lastAngle + delta;
        
        Debug.Log("CurrentAngle: " + GetLocalAngle() + ", LastAngle: " + _lastAngle + ", Delta: " + delta + ", ProposedAngle: " + _proposedAngle);
        
        // Allow only forward (positive) rotation, exclude some off jumps in rotation resulting in odd backward turns sometimes
        if (_proposedAngle > _lastAngle)
        {
            // Cancel backward rotation
            _currentAngle = _lastAngle;
            _proposedAngle = _lastAngle;
            _underlyingTargetTransform.localRotation = Quaternion.AngleAxis(_lastAngle, _rotationAxis.normalized);
            _underlyingTargetTransform.position = _targetTransform.position;
        }
        else
        {
            _lastAngle = _proposedAngle;
        }

        Quaternion currentRot = _targetTransform.localRotation;
        Quaternion targetRot = Quaternion.AngleAxis(_lastAngle, _rotationAxis.normalized);
        _targetTransform.localRotation = Quaternion.Slerp(currentRot, targetRot, _filterStrength);
        _targetTransform.position = Vector3.Lerp(_targetTransform.position, _underlyingTargetTransform.position, _filterStrength);
    }

    private float GetLocalAngle()
    {
        Vector3 localEuler = _underlyingTargetTransform.localRotation.eulerAngles;
        return Vector3.Dot(localEuler, _rotationAxis.normalized);
    }
}
