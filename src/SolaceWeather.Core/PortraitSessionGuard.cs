namespace SolaceWeather.Core;

/// <summary>Even reloading the same farmer invalidates an earlier network result.</summary>
public sealed class PortraitSessionGuard
{
    public int Current { get; private set; }
    public bool IsCurrent(int requestSession) => requestSession == Current;
    public void Reset() => Current++;
}
