using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public enum GrabbingHand
{
    None,
    Left,
    Right,
    Both
}

public class AdditionalGrabbableLogic : MonoBehaviour
{
    private const OVRInput.RawButton spotlightLeftButton = OVRInput.RawButton.LIndexTrigger;
    private const OVRInput.RawButton spotlightRightButton = OVRInput.RawButton.RIndexTrigger;
    private const OVRInput.RawButton spotlightAnyButton = OVRInput.RawButton.LIndexTrigger | OVRInput.RawButton.RIndexTrigger;
    
    // Components
    private FilteredTransformer filteredTransformer;
    private GrabFreeTransformer freeTransformer;
    private Grabbable grabbable;
    public SpotlightController spotlightController;
    public HandGrabInteractable leftInteractable;
    public HandGrabInteractable rightInteractable;
    public float oneHandFilterValue = 0.02f;
    public float twoHandsFilterValue = 0.08f;
    [HideInInspector] public GrabbingHand holdingHand;
    
    private int grabbersNum; // How many hands are grabbing the spotlight now?
    
    private void Awake()
    {
        filteredTransformer = GetComponent<FilteredTransformer>();
        grabbable = GetComponent<Grabbable>();
        freeTransformer = GetComponent<GrabFreeTransformer>();
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
    }

    void SwitchOneTwoHands()
    {
        switch (grabbersNum)
        {
            case 0:
            case 1:
                filteredTransformer._filterStrength = oneHandFilterValue;
                break;
            
            case 2:
                filteredTransformer._filterStrength = twoHandsFilterValue;
                break;
        }
    }

    void TurnDownSpotlight()
    {
        if ((OVRInput.Get(spotlightLeftButton) && holdingHand == GrabbingHand.Left)
            || (OVRInput.Get(spotlightRightButton) && holdingHand == GrabbingHand.Right)
            || (OVRInput.Get(spotlightAnyButton) && holdingHand == GrabbingHand.Both))
        {
            spotlightController.generatorPowerScript.spotlightTurnedDown = true;
        }
        else
        {
            spotlightController.generatorPowerScript.spotlightTurnedDown = false;
        }
    }
    
    private void Update()
    {
        EvaluateGrabbersNum();
        SwitchOneTwoHands();
        TurnDownSpotlight();
    }
}
