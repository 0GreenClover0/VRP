using System;
using System.Linq;
using System.Numerics;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEditor.ShaderGraph;
using UnityEngine;
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

    private GameManager gameManager;
    private Transform leftController;
    private Transform rightController;
    private Vector3 averageHandsPosition;

    private float currentDot;
    private float previousDot;
    private float dotDelta;
    private bool cached = false;
    
    public void BeginTransform()
    {
        _underlyingTransformer.BeginTransform();
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
    }

    private void Update()
    {
        // averageHandsPosition = (leftController.position + rightController.position) / 2.0f;
        // Vector3 v = averageHandsPosition - transform.position;
        // v.x = transform.position.x;
        // v.y = transform.position.y;
        //
        // float dot = Vector3.Dot(v, Vector3.up + transform.position) - 25.0f;
        //
        //
    }

    void AccumulateDotDelta()
    {
        averageHandsPosition = (leftController.position + rightController.position) / 2.0f;
        Vector3 v = averageHandsPosition - transform.position;
        currentDot = Vector3.Dot(v, Vector3.up + transform.position) - 25.0f;;
        
        if (cached)
        {
            previousDot = currentDot;
        }
    
        dotDelta = currentDot - previousDot;
        cached = !cached;
    }
    
    public void UpdateTransform()
    {
        _underlyingTransformer.UpdateTransform();
        
        // AccumulateDotDelta();
        //
        // Vector3 transformEulerR = rightController.localEulerAngles;
        // transformEulerR.x += 100.0f * dotDelta;
        // rightController.localEulerAngles = transformEulerR;
        //
        // Vector3 transformEulerL = leftController.localEulerAngles;
        // transformEulerL.x += 100.0f * dotDelta;
        // rightController.localEulerAngles = transformEulerL;
        //
        // Debug.Log(dotDelta);


        // _targetTransform.position = Vector3.Lerp(_targetTransform.position, _underlyingTargetTransform.position, _filterStrength);
        _targetTransform.rotation = Quaternion.Slerp(_targetTransform.rotation, _underlyingTargetTransform.rotation, _filterStrength);
    }
}