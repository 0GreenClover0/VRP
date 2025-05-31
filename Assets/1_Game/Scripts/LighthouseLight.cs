using UnityEngine;

public class LighthouseLight : MonoBehaviour
{
    public Ship controlledShip = null;
    public bool isEnabled = true;

    public Vector2 GetPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }
}
