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
}
