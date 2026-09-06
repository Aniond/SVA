namespace AbigailModern.Visuals;

internal static class NightPolicy
{
    public static int SkyHeight(int height,int shift)=>Math.Clamp(Math.Min(height-596+Math.Min(shift,shift/2),height-612+2*shift),0,Math.Max(0,height));
    public static float Fade(float minutes)
    {
        if (!float.IsFinite(minutes)) return 0;
        float t=Math.Clamp((minutes-1200)/180,0,1);
        return t*t*(3-2*t);
    }
    public static float Moon(float minutes, bool outdoors, bool rain, bool snow, bool storm, float strength) =>
        !outdoors || storm || !float.IsFinite(strength) ? 0 :
        Fade(minutes)*Math.Clamp(strength,0,1)*.12f*(rain?.25f:snow?.5f:1);
    public static float Stars(float minutes, bool outdoors, bool obscured, float strength) =>
        !outdoors || obscured || !float.IsFinite(strength) ? 0 : Fade(minutes)*Math.Clamp(strength,0,1);
}
