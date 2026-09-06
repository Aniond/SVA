using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AbigailModern.Visuals;

/// <summary>Cosmetic overlays; the game retains ownership of all native lighting.</summary>
internal sealed class LightingEffect : IDisposable
{
    private const int TextureSize = 128;
    private Texture2D? halo;

    public int Draw(SpriteBatch batch, VisualFrame frame, VisualSettings settings)
    {
        if (!settings.Enabled || !settings.LightingEnabled || !float.IsFinite(settings.LightingStrength)
            || settings.LightingStrength <= 0 || settings.MaxLights <= 0) return 0;
        float exposure = LightingPolicy.Exposure(frame.Minutes, frame.Outdoors);
        if (exposure <= 0 || frame.Lights.Count == 0) return 0;

        int calls = 0;
        int budget = Math.Min(settings.MaxLights, 128);
        foreach (SceneLight light in frame.Lights)
        {
            if (calls >= budget) break;
            if (!float.IsFinite(light.Position.X) || !float.IsFinite(light.Position.Y)
                || !float.IsFinite(light.Radius) || light.Radius <= 0
                || !float.IsFinite(light.Strength) || light.Strength <= 0 || light.Color.A == 0) continue;
            // Native maps may contain enormous light sources; cosmetic bloom stays local.
            float radius = Math.Clamp(light.Radius * .65f, 12, 240);
            float x = light.Position.X - frame.Viewport.X;
            float y = light.Position.Y - frame.Viewport.Y;
            if (x + radius < 0 || y + radius < 0 || x - radius > frame.Viewport.Width || y - radius > frame.Viewport.Height) continue;
            float opacity = .16f * Math.Clamp(settings.LightingStrength, 0, 1) * exposure
                * Math.Clamp(light.Strength, 0, 1) * (light.Color.A / 255f)
                * LightingPolicy.Flicker(frame.Seconds, light.Position.X, light.Position.Y, light.Flicker);
            if (opacity < .001f) continue;
            EnsureTexture(batch.GraphicsDevice);
            // SceneLight.Color is already the display hue, converted by the snapshot reader.
            Color tint = new(light.Color.R, light.Color.G, light.Color.B, (byte)255);
            batch.Draw(halo!, new Vector2(x, y), null, tint * opacity, 0,
                new Vector2(TextureSize / 2f), radius * 2 / TextureSize, SpriteEffects.None, 1);
            calls++;
        }
        return calls;
    }

    private void EnsureTexture(GraphicsDevice device)
    {
        if (halo is { IsDisposed: false } && ReferenceEquals(halo.GraphicsDevice, device)) return;
        halo?.Dispose();
        halo = new Texture2D(device, TextureSize, TextureSize);
        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        for (int x = 0; x < TextureSize; x++)
        {
            float dx = (x + .5f - TextureSize / 2f) / (TextureSize / 2f);
            float dy = (y + .5f - TextureSize / 2f) / (TextureSize / 2f);
            pixels[y * TextureSize + x] = Color.White * LightingPolicy.Halo(MathF.Sqrt(dx * dx + dy * dy));
        }
        halo.SetData(pixels);
    }

    public void Dispose()
    {
        halo?.Dispose();
        halo = null;
    }
}
