using AbigailModern.Visuals;
namespace AbigailModern.Visuals.Tests;
public class VisualPolicyTests
{
    [Fact] public void OrdinaryWorldIsEligible() => Assert.True(VisualPolicy.Eligible(true,true,false,false,false));
    [Theory]
    [InlineData(false,true,false,false,false)] [InlineData(true,false,false,false,false)]
    [InlineData(true,true,true,false,false)] [InlineData(true,true,false,true,false)] [InlineData(true,true,false,false,true)]
    public void ProtectedScreensDoNotRender(bool ready,bool location,bool scene,bool minigame,bool screenshot)
        => Assert.False(VisualPolicy.Eligible(ready,location,scene,minigame,screenshot));
    [Fact] public void ClockInterpolationCrossesHourWithoutJump()
    {
        Assert.Equal(719.999f,VisualPolicy.Minutes(1150,7000,7000),2);
        Assert.Equal(720,VisualPolicy.Minutes(1200,0,7000));
        Assert.Equal(1560,VisualPolicy.Minutes(2600,0,7000));
    }
    [Fact] public void InvalidTimerCannotPoisonRendering() => Assert.Equal(360,VisualPolicy.Minutes(600,double.NaN,0));
    [Fact] public void ConfigurationIsBoundedWithoutReenablingDisabledEffects()
    {
        var settings=new VisualSettings {Enabled=false,FogEnabled=false,LightingStrength=float.NaN,ShadowStrength=3,FogStrength=-1,MaxLights=int.MaxValue,MaxCasters=-5};
        settings.Normalize();Assert.False(settings.Enabled);Assert.False(settings.FogEnabled);
        Assert.Equal(.65f,settings.LightingStrength);Assert.Equal(1,settings.ShadowStrength);Assert.Equal(0,settings.FogStrength);
        Assert.Equal(128,settings.MaxLights);Assert.Equal(1,settings.MaxCasters);
    }
}
