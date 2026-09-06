using AbigailModern.Visuals;

public class ShadowTests
{
    [Theory]
    [InlineData(300)]
    [InlineData(360)]
    [InlineData(1200)]
    [InlineData(1600)]
    [InlineData(float.NaN)]
    public void NoSunMeansNoShadow(float minutes) => Assert.Equal(0, ShadowPolicy.Solar(minutes).Opacity);

    [Fact]
    public void ShadowsRotateContinuouslyAndLengthenNearHorizon()
    {
        var morning = ShadowPolicy.Solar(480);
        var noon = ShadowPolicy.Solar(780);
        var evening = ShadowPolicy.Solar(1080);
        Assert.True(morning.X < 0 && evening.X > 0);
        Assert.True(morning.Length > noon.Length && evening.Length > noon.Length);
        Assert.InRange(Math.Abs(ShadowPolicy.Solar(779.99f).X - ShadowPolicy.Solar(780.01f).X),0,.001f);
        Assert.True(ShadowPolicy.Solar(1199).Opacity < evening.Opacity);
    }

    [Fact]
    public void WeatherAndIndoorShadowsAreReduced()
    {
        Assert.Equal(0,ShadowPolicy.Opacity(780,1,false,false,false,false));
        Assert.Equal(0,ShadowPolicy.Opacity(780,1,true,false,false,true));
        float clear = ShadowPolicy.Opacity(780,1,true,false,false,false);
        Assert.True(clear > 0);
        Assert.InRange(ShadowPolicy.Opacity(780,1,true,true,false,false),0,clear*.3f);
        Assert.InRange(ShadowPolicy.Opacity(780,1,true,false,true,false),0,clear*.5f);
        Assert.Equal(0,ShadowPolicy.Opacity(780,float.NaN,true,false,false,false));
    }

    [Fact]
    public void InvalidAndExtremeInputsRemainBounded()
    {
        Assert.Equal(0,ShadowPolicy.CasterLimit(-2));
        Assert.Equal(256,ShadowPolicy.CasterLimit(int.MaxValue));
        Assert.Equal(0,ShadowPolicy.ProjectedLength(float.NaN,2));
        Assert.InRange(ShadowPolicy.ProjectedLength(float.MaxValue,2),0,512);
        Assert.InRange(ShadowPolicy.Opacity(780,float.MaxValue,true,false,false,false),0,.32f);
    }
}
