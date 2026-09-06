using SolaceWeather.Core;
using Xunit;

namespace SolaceWeather.Core.Tests;

public class WalkingPathTests
{
    [Fact]
    public void WalksAroundAnObstacleWithoutDiagonalCornerCutting()
    {
        var wall = new HashSet<WalkTile> { new(2, 0), new(2, 1), new(2, 2) };
        var path = WalkingPath.Find(new(0, 1), new(4, 1), 5, 5, p => !wall.Contains(p));
        Assert.NotNull(path);
        Assert.Equal(new WalkTile(0, 1), path[0]);
        Assert.Equal(new WalkTile(4, 1), path[^1]);
        Assert.DoesNotContain(path, p => wall.Contains(p));
        for (int i = 1; i < path.Count; i++)
            Assert.Equal(1, Math.Abs(path[i].X - path[i - 1].X) + Math.Abs(path[i].Y - path[i - 1].Y));
    }

    [Fact]
    public void UnreachableOrBlockedOrOffMapGoalsHaveNoPath()
    {
        Assert.Null(WalkingPath.Find(new(0, 0), new(2, 2), 3, 3, p => p.X != 1));
        Assert.Null(WalkingPath.Find(new(0, 0), new(2, 2), 3, 3, p => p != new WalkTile(2, 2)));
        Assert.Null(WalkingPath.Find(new(0, 0), new(-1, 2), 3, 3, _ => true));
    }

    [Fact]
    public void LargeMapCoordinatesDoNotWrapAndSearchIsBounded()
    {
        Assert.NotNull(WalkingPath.Find(new(130, 130), new(135, 130), 150, 150, _ => true));
        Assert.Null(WalkingPath.Find(new(0, 0), new(100, 100), 150, 150, _ => true, 4));
    }

    [Fact]
    public void AlreadyAtDestinationNeedsNoMovement()
    {
        var path = WalkingPath.Find(new(1, 1), new(1, 1), 3, 3, _ => true);
        Assert.NotNull(path);
        Assert.Single(path);
    }
}
