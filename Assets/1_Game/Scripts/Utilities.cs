using Oculus.Interaction;
using UnityEngine;

public static class Utilities
{
    public static Vector3 Convert2DTo3D(Vector2 vector)
    {
        return new Vector3(vector.x, 0.0f, vector.y);
    }

    public static Vector2 Convert3DTo2D(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    public static float EaseOutQuart(float x)
    {
        return 1.0f - Mathf.Pow(1.0f - x, 4.0f);
    }

    public static GameManager GetGameManager()
    {
        return GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }
    
    public static void SilenceRigidbody(Rigidbody r, bool silence)
    {
        if (silence)
        {
            // r.isKinematic = true;
            r.interpolation = RigidbodyInterpolation.None;
            r.useGravity = false;
        }
        else
        {
            r.interpolation = RigidbodyInterpolation.Interpolate;
            r.useGravity = true;
            // r.isKinematic = false;
        }
    }
}
