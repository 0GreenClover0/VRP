using System;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class AdditionalGrabbableLogic : MonoBehaviour
{
    // Components
    private FilteredTransformer filteredTransformer;
    private GrabFreeTransformer freeTransformer;
    private Grabbable grabbable;
    public HandGrabInteractable leftInteractable;
    public HandGrabInteractable rightInteractable;
    public float oneHandFilterValue = 0.02f;
    public float twoHandsFilterValue = 0.08f;
    
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
        
        if (leftInteractable.Interactors.Any(i => i.IsGrabbing))
            grabbersNum++;

        if (rightInteractable.Interactors.Any(i => i.IsGrabbing))
            grabbersNum++;
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
    
    private void Update()
    {
        EvaluateGrabbersNum();
        SwitchOneTwoHands();
    }
}
