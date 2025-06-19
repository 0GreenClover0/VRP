using UnityEngine;
using UnityEngine.Serialization;

public class Billboard : MonoBehaviour
{
    [FormerlySerializedAs("buoyMesh")]
    public Transform transformToOffsetFrom;

    [FormerlySerializedAs("lightOffset")]
    public Vector3 offset;

    private new Camera camera;

    private void Awake()
    {
        camera = Camera.main;
    }

    private void Update()
    {
        transform.LookAt(camera.transform.position, Vector3.up);

        if (transformToOffsetFrom != null)
        {
            transform.localPosition = transformToOffsetFrom.localPosition + offset;
        }
    }
}
