using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Transform buoyMesh;
    public Vector3 lightOffset;
    void Update()
    {
        transform.LookAt(Camera.main.transform.position, Vector3.up);
        transform.position = buoyMesh.position + lightOffset;
    }
}
