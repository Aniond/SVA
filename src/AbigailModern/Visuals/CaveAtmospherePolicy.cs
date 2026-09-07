namespace AbigailModern.Visuals;

/// <summary>Pure limits and deterministic cosmetic timing; never uses the game's random generator.</summary>
internal static class CaveAtmospherePolicy
{
    internal const int ContactLimit=24, WetLimit=12, ParticleLimit=8;
    internal static bool WetAllowed(bool mine, int area) => !mine || area!=80 && area!=121;
    internal static float Strength(float value) => float.IsFinite(value)?Math.Clamp(value,0,.35f):0;
    internal static uint Hash(int x,int y)
    {
        unchecked { uint h=(uint)x*374761393u+(uint)y*668265263u+0x9e3779b9u; h=(h^(h>>13))*1274126177u; return h^(h>>16); }
    }
    internal static double Phase(double seconds,int x,int y) => (Math.Max(0,seconds)+(Hash(x,y)%10000)/1000d)%9d;
    internal static double DripCooldown(int x,int y) => 8+(Hash(x,y)%10001)/1000d;
}
