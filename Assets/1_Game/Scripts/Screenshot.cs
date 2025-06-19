using UnityEngine;

public class Screenshot : MonoBehaviour
{
    [SerializeField] private string path;

    [Range(1, 5)]
    [SerializeField] private int size = 1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            path += "screenshot-";
            path += System.Guid.NewGuid().ToString() + ".png";

            ScreenCapture.CaptureScreenshot(path, size);
        }
    }
}
