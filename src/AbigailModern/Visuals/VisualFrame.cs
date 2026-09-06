using Microsoft.Xna.Framework;

namespace AbigailModern.Visuals;

internal sealed class VisualFrame
{
    public Rectangle Viewport { get; init; }
    public float Minutes { get; init; }
    public double Seconds { get; init; }
    public bool Outdoors { get; init; }
    public bool Raining { get; init; }
    public bool Snowing { get; init; }
    public bool Lightning { get; init; }
    public bool GreenRain { get; init; }
    public int SkyHeight { get; init; }
    public string Season { get; init; } = "spring";
    public IReadOnlyList<SceneLight> Lights { get; init; } = Array.Empty<SceneLight>();
    public IReadOnlyList<ShadowCaster> Casters { get; init; } = Array.Empty<ShadowCaster>();
}

internal sealed record SceneLight(Vector2 Position, float Radius, Color Color, bool Flicker, float Strength = 1);
internal enum CasterKind { Tree, Building }
internal sealed record ShadowCaster(Vector2 Foot, float Width, float Height, CasterKind Kind);
