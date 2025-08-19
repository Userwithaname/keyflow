using UnityEngine;

public static class Misc {
    public static float ValidateIfNaN(float input, float fallback) {
        if (float.IsNaN(input))
            Debug.LogWarning("Input was is NaN, using fallback value {fallback}");
        return float.IsNaN(input) ? fallback : input;
    }
}
