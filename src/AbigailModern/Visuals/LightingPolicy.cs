namespace AbigailModern.Visuals;

internal static class LightingPolicy
{
    public static float Exposure(float minutes, bool outdoors)
    {
        if (!outdoors) return .8f;
        if (!float.IsFinite(minutes)) return 0;
        float clock = ((minutes % 1440) + 1440) % 1440;
        if (clock < 360) return 1;
        if (clock < 480) return 1 - Smooth((clock - 360) / 120);
        return Smooth((clock - 1020) / 180);
    }

    public static float Flicker(double seconds, float x, float y, bool enabled)
    {
        if (!enabled || !double.IsFinite(seconds) || !float.IsFinite(x) || !float.IsFinite(y)) return 1;
        double phase = x * .017 + y * .031;
        return 1 + (float)(.035 * Math.Sin(seconds * 2.1 + phase) + .02 * Math.Sin(seconds * 3.7 + phase * .71));
    }

    public static float Halo(float normalizedRadius)
    {
        if (!float.IsFinite(normalizedRadius)) return 0;
        float edge = 1 - Math.Clamp(normalizedRadius, 0, 1);
        return edge * edge * edge * (1 + 3 * (1 - edge));
    }

    private static float Smooth(float value)
    {
        float t = Math.Clamp(value, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
