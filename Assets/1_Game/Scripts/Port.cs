using System.Collections.Generic;
using UnityEngine;

public class Port : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }

        if (other.attachedRigidbody.TryGetComponent(out Ship ship))
        {
            ship.Stop();
        }
    }
}
