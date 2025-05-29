using System;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    public List<Vector2> points = new();

    private void Awake()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            points.Add(Utilities.Convert3DTo2D(transform.GetChild(i).position));
        }
    }

    public Vector2 GetPointAt(float x)
    {
        if (points == null || points.Count < 2)
        {
            Debug.LogWarning("Not enough points to form a line.");
            return Vector2.zero;
        }

        if (x < 0.0f || x > 1.0f)
        {
            Debug.LogWarning("x out of range: " + x);
            return Vector2.zero;
        }

        float totalLength = 0f;

        // First, compute the total length of the polyline
        List<float> segmentLengths = new List<float>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            float segmentLength = Vector2.Distance(points[i], points[i + 1]);
            segmentLengths.Add(segmentLength);
            totalLength += segmentLength;
        }

        x = x * totalLength;

        // Find the segment that contains the point at distance x
        float accumulated = 0f;
        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segmentLength = segmentLengths[i];
            if (accumulated + segmentLength >= x)
            {
                float t = (x - accumulated) / segmentLength;
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                return Vector2.Lerp(a, b, t);
            }
            accumulated += segmentLength;
        }

        // If we somehow fall through, return the last point
        return points[points.Count - 1];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.58f, 0.2f, 0.66f, 0.75f);

        Vector3[] points = new Vector3[transform.childCount];

        for (int i = 0; i < transform.childCount; ++i)
        {
            points[i] = transform.GetChild(i).position;
        }

        Gizmos.DrawLineStrip(points.AsSpan(), false);
    }

    private void OnValidate()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            if (!transform.GetChild(i).TryGetComponent(out PathPoint point))
            {
                transform.GetChild(i).gameObject.AddComponent<PathPoint>();
            }
        }
    }
}
