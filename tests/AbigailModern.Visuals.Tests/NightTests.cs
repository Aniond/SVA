using AbigailModern.Visuals;
namespace AbigailModern.Visuals.Tests;

public class NightTests
{
    [Theory]
    [InlineData(720,0,108)]
    [InlineData(720,-20,68)]
    [InlineData(720,20,134)]
    [InlineData(400,0,0)]
    public void SkyEndsAboveAllNativeMountainLayers(int height,int shift,int expected)
        =>Assert.Equal(expected,NightPolicy.SkyHeight(height,shift));
    [Fact] public void NightFadesInSmoothlyAndSurvivesAfterMidnight()
    {
        Assert.Equal(0, NightPolicy.Fade(1200));
        Assert.InRange(NightPolicy.Fade(1290), .49f, .51f);
        Assert.Equal(1, NightPolicy.Fade(1380));
        Assert.Equal(1, NightPolicy.Fade(1560));
        Assert.Equal(0, NightPolicy.Fade(float.NaN));
    }
    [Fact] public void MoonlightRespectsWeatherAndInterior()
    {
        float clear=NightPolicy.Moon(1440,true,false,false,false,1);
        Assert.InRange(clear,.05f,.15f);
        Assert.InRange(NightPolicy.Moon(1440,true,true,false,false,1),0,clear*.5f);
        Assert.Equal(0,NightPolicy.Moon(1440,false,false,false,false,1));
        Assert.Equal(0,NightPolicy.Moon(1440,true,false,false,true,1));
        Assert.Equal(0,NightPolicy.Moon(1440,true,false,false,false,float.NaN));
    }
    [Fact] public void StarsNeedClearOutdoorNight()
    {
        Assert.True(NightPolicy.Stars(1440,true,false,.5f)>0);
        Assert.Equal(0,NightPolicy.Stars(720,true,false,1));
        Assert.Equal(0,NightPolicy.Stars(1440,true,true,1));
        Assert.Equal(0,NightPolicy.Stars(1440,false,false,1));
        Assert.Equal(0,NightPolicy.Stars(1440,true,false,0));
    }
}
