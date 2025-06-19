using UnityEngine;

public class AK
{
    public static float MapRangeClamped(float input, float rangeMin, float rangeEnd, float newRangeMin, float newRangeEnd)
    {
        float t = Mathf.InverseLerp(rangeMin, rangeEnd, input);
        return Mathf.Clamp(Mathf.Lerp(newRangeMin, newRangeEnd, t), newRangeMin, newRangeEnd);
    }
}
