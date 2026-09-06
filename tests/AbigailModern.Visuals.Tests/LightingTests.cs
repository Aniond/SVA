using AbigailModern.Visuals;

public sealed class LightingTests
{
    [Fact]
    public void OutdoorHalosFadeInAtDuskAndRemainVisibleAfterMidnight()
    {
        Assert.Equal(0, LightingPolicy.Exposure(720, true));
        Assert.InRange(LightingPolicy.Exposure(1110, true), .1f, .9f);
        Assert.Equal(1, LightingPolicy.Exposure(1320, true));
        Assert.Equal(1, LightingPolicy.Exposure(1500, true));
        Assert.Equal(1, LightingPolicy.Exposure(60, true));
        Assert.Equal(0, LightingPolicy.Exposure(480, true));
    }

    [Fact]
    public void InteriorLightExposureDoesNotDependOnOutdoorTime()
    {
        Assert.True(LightingPolicy.Exposure(720, false) > 0);
        Assert.Equal(LightingPolicy.Exposure(720, false), LightingPolicy.Exposure(1380, false));
    }

    [Fact]
    public void FireFlickerIsBoundedSmoothAndStableWhenRedrawn()
    {
        for (int i = 0; i < 1000; i++)
        {
            double time = i / 60d;
            float value = LightingPolicy.Flicker(time, 210, 900, true);
            Assert.InRange(value, .94f, 1.06f);
            Assert.InRange(Math.Abs(value - LightingPolicy.Flicker(time + 1 / 60d, 210, 900, true)), 0, .01f);
            Assert.Equal(value, LightingPolicy.Flicker(time, 210, 900, true));
        }
        Assert.Equal(1, LightingPolicy.Flicker(4, 210, 900, false));
        Assert.NotEqual(LightingPolicy.Flicker(4, 210, 900, true), LightingPolicy.Flicker(4, 810, 900, true));
    }

    [Fact]
    public void HaloHasNoHardEdgeAndFallsAwayFromCenter()
    {
        Assert.Equal(1, LightingPolicy.Halo(0));
        Assert.True(LightingPolicy.Halo(.2f) > LightingPolicy.Halo(.6f));
        Assert.InRange(LightingPolicy.Halo(.99f), 0, .001f);
        Assert.Equal(0, LightingPolicy.Halo(1));
        Assert.Equal(0, LightingPolicy.Halo(2));
    }
}
