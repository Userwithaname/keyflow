
public static class Misc {
    public static float ValidateIfNaN(float input, float fallback){
        return float.IsNaN(input)?fallback:input;
    }
}
