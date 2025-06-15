using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MakingGrabbableChecklist : MonoBehaviour
{
    public bool completed1 = false;
    public bool completed2 = false;
    public bool completed3 = false;
}

#if UNITY_EDITOR
[CustomEditor(typeof(MakingGrabbableChecklist))]
public class MakingGrabbableChecklistEditor : Editor
{
    private MakingGrabbableChecklist targetGrabbable => target as MakingGrabbableChecklist;

    private bool completedAll => targetGrabbable.completed1 && targetGrabbable.completed2 && targetGrabbable.completed3;
    private bool areYouSure = false;

    public override void OnInspectorGUI()
    {
        targetGrabbable.completed1 = GUILayout.Toggle(targetGrabbable.completed1, "Adjust collider and/or mesh rotation");

        GUILayout.Space(5.0f);

        targetGrabbable.completed2 = GUILayout.Toggle(targetGrabbable.completed2, "Make a pose for the left hand");
        GUILayout.Label("Remember to assign the pose to both hand grab and distance grab interactables!");

        GUILayout.Space(5.0f);

        targetGrabbable.completed3 = GUILayout.Toggle(targetGrabbable.completed3, "Make a pose for the right hand");
        GUILayout.Label("Remember to assign the pose to both hand grab and distance grab interactables!");

        GUILayout.Space(5.0f);

        if (areYouSure)
        {
            GUILayout.Label("Are you sure you want to delete this component?\nNot all of the checklist items are completed!");
        }

        GUILayout.Label("Finished the whole checklist? Delete this component!");
        if (GUILayout.Button("All done!"))
        {
            if (!completedAll && !areYouSure)
                areYouSure = true;
            else
            {
                Undo.SetCurrentGroupName("Finished making grabbable checklist");
                Undo.RecordObject(targetGrabbable.gameObject, "");
                Undo.RegisterCompleteObjectUndo(targetGrabbable.gameObject, "");

                DestroyImmediate(targetGrabbable);
            }
        }
    }
}
#endif
