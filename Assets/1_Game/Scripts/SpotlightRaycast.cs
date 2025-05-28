using UnityEngine;

public class SpotlightRaycast : MonoBehaviour
{
    RaycastHit hit;

    [Tooltip("If the spotlight should attract ships (false if, e.g. generator power is 0)")]
    public bool attractShips = true;

    [Space]

    public Transform waterConeTransform;
    public GameObject spotlightConeRef;

    [Range(0.01f, 5.0f)]
    public float waterConeRadius = 1.75f;

    void Update()
    {
        if (attractShips)
        {
            if (!spotlightConeRef.activeInHierarchy)
                spotlightConeRef.SetActive(true);

            ShootRaycast();
        }
        else if (waterConeTransform.gameObject.activeInHierarchy)
        {
            waterConeTransform.gameObject.SetActive(false);
            spotlightConeRef.SetActive(false);
        }
    }

    void ShootRaycast()
    {
        Vector3 scale = waterConeTransform.localScale;
        scale.x = waterConeRadius * 2.0f;
        scale.z = waterConeRadius * 2.0f;
        waterConeTransform.localScale = scale;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 500))
        {
            if (!waterConeTransform.gameObject.activeInHierarchy)
                waterConeTransform.gameObject.SetActive(true);

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 500.0f, Color.yellow);
            Debug.DrawLine(hit.point, hit.point + Vector3.up * 0.5f, Color.red);
            waterConeTransform.position = hit.point + Vector3.up * 0.05f;
        }
        else if (waterConeTransform.gameObject.activeInHierarchy)
        {
            waterConeTransform.gameObject.SetActive(false);
        }
    }
}
