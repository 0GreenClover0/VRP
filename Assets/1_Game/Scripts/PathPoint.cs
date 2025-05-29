using UnityEngine;

// NOTE: This class exists to only draw a gizmo, because if we do that in the parent the GameObject cannot be selected by clicking.
//       https://discussions.unity.com/t/how-to-pick-gizmos/446512/4
//       https://discussions.unity.com/t/how-to-pick-ondrawgizmos-gizmo/22157
public class PathPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.58f, 0.2f, 0.66f, 0.75f);
        Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
    }
}
