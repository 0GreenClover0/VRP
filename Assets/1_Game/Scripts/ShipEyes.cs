using UnityEngine;

public class ShipEyes : MonoBehaviour
{
    [HideInInspector] public bool seeObstacle = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IceBound iceBound))
        {
            seeObstacle = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IceBound iceBound))
        {
            seeObstacle = false;
        }
    }
}
