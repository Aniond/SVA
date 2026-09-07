namespace SolaceWeather.Core;

public sealed record TailoringRecoveryAward(int Health, int Energy);
public sealed class TailoringRecovery
{
    public int Day { get; set; } = -1;
    public int HealthToday { get; set; }
    public int EnergyToday { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] private double healthSeconds;
    [System.Text.Json.Serialization.JsonIgnore] private double energySeconds;
    public bool IsValid() => Day >= -1 && HealthToday is >= 0 and <= 20 && EnergyToday is >= 0 and <= 40;
    public TailoringRecoveryAward Tick(int day, bool active, bool healthEquipped, bool energyEquipped, bool recentlyHurt, double seconds)
    {
        if (!IsValid() || day < 0 || day < Day) return new(0,0);
        if (day != Day) { Day = day; HealthToday = EnergyToday = 0; healthSeconds = energySeconds = 0; }
        if (!active || recentlyHurt || seconds <= 0 || seconds > 2) { healthSeconds = energySeconds = 0; return new(0,0); }
        healthSeconds = healthEquipped && HealthToday < 20 ? healthSeconds + seconds : 0;
        energySeconds = energyEquipped && EnergyToday < 40 ? energySeconds + seconds : 0;
        int health = healthSeconds >= 60 ? 1 : 0, energy = energySeconds >= 60 ? 2 : 0;
        if (health > 0) { healthSeconds = 0; HealthToday += health; }
        if (energy > 0) { energySeconds = 0; EnergyToday += energy; }
        return new(health, energy);
    }
}
