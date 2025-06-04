using UnityEngine;

public class LighthouseLight : MonoBehaviour
{
    public Ship controlledShip = null;

    public Vector2 GetPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
}
