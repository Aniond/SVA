namespace SolaceWeather.PlayerPortraits;

/// <summary>Retry transient capture failures only after an explicit action, never on every update.</summary>
public sealed class PortraitCaptureRecovery
{
    public bool Attempted { get; private set; }
    public bool TryCapture(Func<bool> capture, bool explicitRetry)
    {
        if (Attempted && !explicitRetry) return false;
        Attempted = true;
        try { return capture(); } catch { return false; }
    }
}
