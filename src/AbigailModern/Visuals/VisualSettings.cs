namespace AbigailModern.Visuals;

internal sealed class VisualSettings
{
    public bool Enabled { get; set; } = true;
    public bool LightingEnabled { get; set; } = true;
    public bool ShadowsEnabled { get; set; } = true;
    public bool FogEnabled { get; set; } = true;
    public bool MoonlightEnabled { get; set; } = true;
    public bool StarsEnabled { get; set; } = true;
    public float MoonlightStrength { get; set; } = .65f;
    public float StarsStrength { get; set; } = .65f;
    public float LightingStrength { get; set; } = .65f;
    public float ShadowStrength { get; set; } = .55f;
    public float FogStrength { get; set; } = .5f;
    public int MaxLights { get; set; } = 64;
    public int MaxCasters { get; set; } = 160;

    public void Normalize()
    {
        LightingStrength=Clamp(LightingStrength,.65f);
        ShadowStrength=Clamp(ShadowStrength,.55f);
        FogStrength=Clamp(FogStrength,.5f);
        MoonlightStrength=Clamp(MoonlightStrength,.65f);
        StarsStrength=Clamp(StarsStrength,.65f);
        MaxLights=Math.Clamp(MaxLights,1,128);
        MaxCasters=Math.Clamp(MaxCasters,1,256);
    }
    private static float Clamp(float value,float fallback) => float.IsFinite(value)?Math.Clamp(value,0,1):fallback;
}
