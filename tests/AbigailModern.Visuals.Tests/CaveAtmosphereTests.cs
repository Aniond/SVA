using AbigailModern.Visuals;
public class CaveAtmosphereTests
{
    [Theory]
    [InlineData(80)]
    [InlineData(121)]
    public void LavaAndSkullAreasDoNotGetWetDecoration(int area) => Assert.False(CaveAtmospherePolicy.WetAllowed(true,area));
    [Theory]
    [InlineData(0)]
    [InlineData(40)]
    public void TemperateAndFrostCanUseValidatedWater(int area) => Assert.True(CaveAtmospherePolicy.WetAllowed(true,area));
    [Fact]
    public void CosmeticPhaseIsStableBoundedAndIndependentOfNativeRandom()
    {
        double a=CaveAtmospherePolicy.Phase(12.25,4,9);
        var unrelated=new Random(7);for(int i=0;i<100;i++)unrelated.Next();
        Assert.Equal(a,CaveAtmospherePolicy.Phase(12.25,4,9));
        Assert.InRange(a,0,9);Assert.NotEqual(a,CaveAtmospherePolicy.Phase(12.25,5,9));
        Assert.Equal(a,CaveAtmospherePolicy.Phase(21.25,4,9),8);
    }
    [Fact]
    public void StrengthCannotBecomeOpaqueOrInvalid()
    {
        Assert.Equal(0,CaveAtmospherePolicy.Strength(float.NaN));Assert.Equal(0,CaveAtmospherePolicy.Strength(-1));Assert.Equal(.35f,CaveAtmospherePolicy.Strength(99));
        Assert.Equal(24,CaveAtmospherePolicy.ContactLimit);Assert.Equal(12,CaveAtmospherePolicy.WetLimit);Assert.Equal(8,CaveAtmospherePolicy.ParticleLimit);
    }
}

public class CaveDripTimingTests
{
    [Fact]
    public void LocalizedDripsHaveBoundedDeterministicCooldown()
    {
        for(int y=-20;y<20;y++)for(int x=-20;x<20;x++)
        {
            double seconds=AbigailModern.Visuals.CaveAtmospherePolicy.DripCooldown(x,y);
            Assert.InRange(seconds,8,18);Assert.Equal(seconds,AbigailModern.Visuals.CaveAtmospherePolicy.DripCooldown(x,y));
        }
    }
}
