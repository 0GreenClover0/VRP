using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

public class AdditionalLightbulbLogic : MonoBehaviour
{
    [HideInInspector] public GrabbingHand holdingHand;
    
    private int grabbersNum; // How many hands are grabbing the spotlight now?
    public HandGrabInteractable leftInteractable;
    public HandGrabInteractable rightInteractable;
    public UnityAction onTakeBulbFromPenguin;
    private Rigidbody rigidbody;
    private bool firstGrab = true;
    
    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    void EvaluateGrabbersNum()
    {
        grabbersNum = 0;

        holdingHand = GrabbingHand.None;
        
        if (leftInteractable.Interactors.Any(i => i.IsGrabbing))
        {
            grabbersNum++;
            holdingHand = GrabbingHand.Left;
        }

        if (rightInteractable.Interactors.Any(i => i.IsGrabbing))
        {
            grabbersNum++;
            if (holdingHand == GrabbingHand.Left)
            {
                holdingHand = GrabbingHand.Both;
            }
            else
            {
                holdingHand = GrabbingHand.Right;
            }
        }
        
        Debug.Log(grabbersNum);
    }

    void UnfreezeRigidbody()
    {
        if (grabbersNum > 0 && firstGrab && (rigidbody.isKinematic || !rigidbody.useGravity))
        {
            Utilities.SilenceRigidbody(rigidbody, false);
            transform.SetParent(null);
            firstGrab = false;
            onTakeBulbFromPenguin.Invoke();
        }
    }
    
    private void Update()
    {
        EvaluateGrabbersNum();
        UnfreezeRigidbody();
    }
}
