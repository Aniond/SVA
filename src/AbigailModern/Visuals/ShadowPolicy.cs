namespace AbigailModern.Visuals;

internal static class ShadowPolicy
{
    public static (float X,float Y,float Length,float Opacity) Solar(float minutes)
    {
        if (!float.IsFinite(minutes) || minutes <= 360 || minutes >= 1200) return default;
        float phase = (minutes - 360) / 840 * MathF.PI;
        float x = -MathF.Cos(phase);
        const float y = .42f;
        float norm = MathF.Sqrt(x*x+y*y);
        float sun = MathF.Sin(phase);
        return (x/norm,y/norm,.3f+1.7f*x*x,sun*sun);
    }
    public static float Opacity(float minutes,float strength,bool outdoors,bool rain,bool snow,bool storm)
    {
        if (!outdoors || storm || !float.IsFinite(strength)) return 0;
        return Solar(minutes).Opacity*Math.Clamp(strength,0,1)*.32f*(rain?.16f:snow?.38f:1);
    }
    public static int CasterLimit(int requested) => Math.Clamp(requested,0,256);
    public static float ProjectedLength(float height,float multiplier) =>
        !float.IsFinite(height) || !float.IsFinite(multiplier) || height <= 0 || multiplier <= 0
            ? 0 : (float)Math.Clamp((double)height*multiplier,0,512);
}
