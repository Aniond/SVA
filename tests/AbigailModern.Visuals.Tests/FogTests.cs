using AbigailModern.Visuals;

public class FogTests
{
    [Fact] public void IndoorsAndDisabledStrengthHaveNoFog()
    {
        Assert.Equal(0, FogPolicy.Density(false, 420, "fall", true, true, false, 1));
        Assert.Equal(0, FogPolicy.Density(true, 420, "fall", true, true, false, 0));
    }
    [Fact] public void DawnAndRainFavorFogWithoutWhitewash()
    {
        float noon = FogPolicy.Density(true, 720, "spring", false, false, false, 1);
        Assert.True(FogPolicy.Density(true, 420, "spring", false, false, false, 1) > noon);
        Assert.True(FogPolicy.Density(true, 720, "spring", true, false, false, 1) > noon);
        Assert.InRange(FogPolicy.Density(true, 420, "fall", true, true, true, 1), 0, .14f);
    }
    [Fact] public void SeasonsAndStrengthAffectDensity()
    {
        float summer = FogPolicy.Density(true, 420, "summer", false, false, false, 1);
        float fall = FogPolicy.Density(true, 420, "fall", false, false, false, 1);
        Assert.True(fall > summer);
        Assert.Equal(fall * .5f, FogPolicy.Density(true, 420, "fall", false, false, false, .5f));
    }
    [Fact] public void WrappingAndDriftRemainContinuousAcrossTileEdges()
    {
        Assert.Equal(1023, FogPolicy.Wrap(-1, 1024));
        Assert.Equal(0, FogPolicy.Wrap(1024, 1024));
        Assert.Equal(FogPolicy.Offset(400, 12, 3, 1024), FogPolicy.Offset(1424, 12, 3, 1024));
        Assert.Equal(1, FogPolicy.Offset(401, 12, 3, 1024) - FogPolicy.Offset(400, 12, 3, 1024), 5);
    }
    [Fact] public void InvalidInputsStayFiniteAndBounded()
    {
        Assert.Equal(0, FogPolicy.Wrap(double.NaN, 1024));
        Assert.Equal(0, FogPolicy.Wrap(2, 0));
        Assert.True(double.IsFinite(FogPolicy.Offset(int.MaxValue, double.PositiveInfinity, 4, 1024)));
        Assert.InRange(FogPolicy.Density(true, float.NaN, "unknown", true, true, true, float.PositiveInfinity), 0, .14f);
    }
}
