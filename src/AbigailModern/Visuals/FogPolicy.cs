namespace AbigailModern.Visuals;

internal static class FogPolicy
{
    public static float Density(bool outdoors, float minutes, string season, bool rain, bool lightning, bool greenRain, float strength)
    {
        if (!outdoors || !float.IsFinite(strength)) return 0;
        strength = Math.Clamp(strength, 0, 1);
        minutes = float.IsFinite(minutes) ? minutes : 720;
        float morning = Math.Clamp((630 - minutes) / 210, 0, 1);
        float evening = Math.Clamp((minutes - 1020) / 180, 0, 1);
        float seasonal = season switch { "fall" => 1.2f, "winter" => .8f, "summer" => .65f, _ => 1f };
        float density = (.012f + .05f * Math.Max(morning, evening) + (rain ? .035f : 0) + (lightning ? .015f : 0) + (greenRain ? .015f : 0)) * seasonal;
        return Math.Min(.14f, density) * strength;
    }

    public static double Wrap(double value, double period)
    {
        if (!double.IsFinite(value) || !double.IsFinite(period) || period <= 0) return 0;
        return (value % period + period) % period;
    }

    public static double Offset(double camera, double seconds, double speed, double period)
    {
        if (!double.IsFinite(seconds)) seconds = 0;
        if (!double.IsFinite(speed)) speed = 0;
        // Reduce time before multiplication so even a pathological clock cannot overflow.
        double drift = speed == 0 ? 0 : Wrap(seconds, period / Math.Abs(speed)) * speed;
        return Wrap(Wrap(camera, period) - drift, period);
    }
}
