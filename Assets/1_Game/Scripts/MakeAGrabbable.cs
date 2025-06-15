using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using System.Reflection;
using Oculus.Interaction.DistanceReticles;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MakeAGrabbable : MonoBehaviour
{
    public GameObject distanceGrabHoverAudioPrefab;
    public GameObject distanceGrabPullAudioPrefab;
    public GameObject basicGrabPickupAudioPrefab;
    public GameObject basicGrabReleaseAudioPrefab;

#if UNITY_EDITOR

public void MakeMeAGrabbable()
{
    Undo.SetCurrentGroupName("Make a grabbable");
    Undo.RecordObject(gameObject, "");
    Undo.RegisterCompleteObjectUndo(gameObject, "");

    gameObject.AddComponent<MakingGrabbableChecklist>();

    Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    rigidbody.mass = 0.15f;

    BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

    // Try to automatically set the size of the collider based on the size of the object
    BoxCollider sampleSize = transform.GetChild(0).gameObject.AddComponent<BoxCollider>();
    Vector3 size = sampleSize.size;
    size.x *= transform.GetChild(0).transform.lossyScale.x;
    size.y *= transform.GetChild(0).transform.lossyScale.y;
    size.z *= transform.GetChild(0).transform.lossyScale.z;
    boxCollider.size = size;
    boxCollider.center = sampleSize.center;
    DestroyImmediate(sampleSize, true);

    Grabbable grabbable = gameObject.AddComponent<Grabbable>();
    grabbable.TransferOnSecondSelection = true;

    var grabbingRules = new Oculus.Interaction.GrabAPI.GrabbingRule();
    grabbingRules[HandFinger.Thumb] = Oculus.Interaction.GrabAPI.FingerRequirement.Optional;
    grabbingRules[HandFinger.Index] = Oculus.Interaction.GrabAPI.FingerRequirement.Optional;
    grabbingRules[HandFinger.Middle] = Oculus.Interaction.GrabAPI.FingerRequirement.Required;
    grabbingRules[HandFinger.Ring] = Oculus.Interaction.GrabAPI.FingerRequirement.Required;
    grabbingRules[HandFinger.Max] = Oculus.Interaction.GrabAPI.FingerRequirement.Optional;

    GameObject handGrabLeft = new GameObject();
    handGrabLeft.transform.parent = transform;
    handGrabLeft.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
    handGrabLeft.name = "HandGrabInteractableLeft";
    Undo.RegisterCreatedObjectUndo(handGrabLeft, "");
    HandGrabInteractable handGrabInteractableLeft = handGrabLeft.AddComponent<HandGrabInteractable>();
    handGrabInteractableLeft.InjectAllHandGrabInteractable(Oculus.Interaction.Grab.GrabTypeFlags.All, rigidbody, Oculus.Interaction.GrabAPI.GrabbingRule.DefaultPinchRule, grabbingRules);

    ReticleDataMesh leftDataMesh = handGrabLeft.AddComponent<ReticleDataMesh>();
    leftDataMesh.Filter = transform.GetChild(0).GetComponent<MeshFilter>();

    handGrabLeft.AddComponent<TagSet>();

    GameObject handGrabRight = new GameObject();
    handGrabRight.transform.parent = transform;
    handGrabRight.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
    handGrabRight.name = "HandGrabInteractableRight";
    Undo.RegisterCreatedObjectUndo(handGrabRight, "");
    HandGrabInteractable handGrabInteractableRight = handGrabRight.AddComponent<HandGrabInteractable>();
    handGrabInteractableRight.InjectAllHandGrabInteractable(Oculus.Interaction.Grab.GrabTypeFlags.All, rigidbody, Oculus.Interaction.GrabAPI.GrabbingRule.DefaultPinchRule, grabbingRules);

    ReticleDataMesh rightDataMesh = handGrabRight.AddComponent<ReticleDataMesh>();
    rightDataMesh.Filter = transform.GetChild(0).GetComponent<MeshFilter>();

    handGrabRight.AddComponent<TagSet>();

    GameObject handGrab = new GameObject();
    handGrab.transform.parent = transform;
    handGrab.name = "HandGrab";
    Undo.RegisterCreatedObjectUndo(handGrab, "");

    GameObject basicGrabPickupAudio = (GameObject)PrefabUtility.InstantiatePrefab(basicGrabPickupAudioPrefab, handGrab.transform);
    AudioTrigger basicGrabPickupAudioTrigger = basicGrabPickupAudio.GetComponent<AudioTrigger>();

    GameObject basicGrabReleaseAudio = (GameObject)PrefabUtility.InstantiatePrefab(basicGrabReleaseAudioPrefab, handGrab.transform);
    AudioTrigger basicGrabReleaseAudioTrigger = basicGrabReleaseAudio.GetComponent<AudioTrigger>();

    PointableUnityEventWrapper handGrabLeftEventWrapper = handGrab.AddComponent<PointableUnityEventWrapper>();
    handGrabLeftEventWrapper.InjectPointable(handGrabInteractableLeft);

    PointableUnityEventWrapper handGrabRightEventWrapper = handGrab.AddComponent<PointableUnityEventWrapper>();
    handGrabRightEventWrapper.InjectPointable(handGrabInteractableRight);

    if (handGrabLeftEventWrapper == null || handGrabRightEventWrapper == null)
    {
        Debug.LogError("One or more of the event wrappers are null! Aborting.");
        return;
    }

    if (basicGrabPickupAudioTrigger == null || basicGrabReleaseAudioTrigger == null)
    {
        Debug.LogError("One or more of the audio triggers are null! Aborting.");
        return;
    }

    Undo.SetCurrentGroupName("Assign events");

    Undo.RecordObject(handGrabLeftEventWrapper, "");
    Undo.RecordObject(handGrabRightEventWrapper, "");

    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(handGrabLeftEventWrapper.WhenSelect, basicGrabPickupAudioTrigger.PlayAudio);
    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(handGrabRightEventWrapper.WhenSelect, basicGrabPickupAudioTrigger.PlayAudio);

    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(handGrabLeftEventWrapper.WhenUnselect, basicGrabReleaseAudioTrigger.PlayAudio);
    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(handGrabRightEventWrapper.WhenUnselect, basicGrabReleaseAudioTrigger.PlayAudio);

    DestroyImmediate(this);
}

#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(MakeAGrabbable))]
public class MakeAGrabbableEditor : Editor
{
    private MakeAGrabbable targetGrabbable => target as MakeAGrabbable;

    public override void OnInspectorGUI()
    {
        bool meshFound = targetGrabbable.transform.childCount != 0 && targetGrabbable.transform.GetChild(0).GetComponent<MeshFilter>() != null;
        if (!meshFound)
        {
            GUILayout.Label("No Mesh Filter found! A lot of components depend on the mesh filter of the object.");
            return;
        }

        GUILayout.Label("Ready to generate a product.");
        if (GUILayout.Button("Make me a grabbable"))
            targetGrabbable.MakeMeAGrabbable();
    }
}
#endif
