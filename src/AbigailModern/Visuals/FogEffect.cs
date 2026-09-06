using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AbigailModern.Visuals;

internal sealed class FogEffect : IDisposable
{
    private Texture2D? noise;
    private const int TextureSize = 129;

    public int Draw(SpriteBatch batch, VisualFrame frame, VisualSettings settings)
    {
        if (!settings.Enabled || !settings.FogEnabled || frame.Viewport.Width <= 0 || frame.Viewport.Height <= 0) return 0;
        float density = FogPolicy.Density(frame.Outdoors, frame.Minutes, frame.Season, frame.Raining, frame.Lightning, frame.GreenRain, settings.FogStrength);
        if (density <= 0) return 0;
        EnsureTexture(batch.GraphicsDevice);
        int calls = 0;
        // Adaptive tile size caps each layer at at most 6 x 6 quads, even on huge viewports.
        float size = Math.Max(1024f, Math.Max(frame.Viewport.Width, frame.Viewport.Height) / 4f);
        for (int layer = 0; layer < 2; layer++)
        {
            float tile = size * (layer == 0 ? 1 : 1.43f);
            float xOffset = (float)FogPolicy.Offset(frame.Viewport.X, frame.Seconds, layer == 0 ? 5 : -3, tile);
            float yOffset = (float)FogPolicy.Offset(frame.Viewport.Y, frame.Seconds, layer == 0 ? 1.7 : 2.6, tile);
            Color tint = (frame.GreenRain ? new Color(172, 194, 175) : new Color(184, 200, 209)) * (density * .5f);
            for (float y = -yOffset; y < frame.Viewport.Height; y += tile)
            for (float x = -xOffset; x < frame.Viewport.Width; x += tile)
            {
                batch.Draw(noise!, new Vector2(x, y), null, tint, 0, Vector2.Zero, tile / TextureSize, SpriteEffects.None, 1f);
                calls++;
            }
        }
        return calls;
    }

    private void EnsureTexture(GraphicsDevice device)
    {
        if (noise is not null && !noise.IsDisposed && noise.GraphicsDevice == device) return;
        noise?.Dispose();
        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        for (int x = 0; x < TextureSize; x++)
        {
            double value = 0, weight = 0;
            for (int octave = 0; octave < 3; octave++)
            {
                int cells = 2 << octave;
                double px = x / (double)(TextureSize - 1) * cells;
                double py = y / (double)(TextureSize - 1) * cells;
                int ix = (int)Math.Floor(px), iy = (int)Math.Floor(py);
                double tx = px - ix, ty = py - iy;
                tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                double a = Hash(ix % cells, iy % cells, octave);
                double b = Hash((ix + 1) % cells, iy % cells, octave);
                double c = Hash(ix % cells, (iy + 1) % cells, octave);
                double d = Hash((ix + 1) % cells, (iy + 1) % cells, octave);
                double w = 1.0 / (1 << octave);
                value += ((a + (b - a) * tx) * (1 - ty) + (c + (d - c) * tx) * ty) * w;
                weight += w;
            }
            float alpha = (float)Math.Pow(value / weight, 1.7);
            // Premultiply all channels; both texture and tint obey AlphaBlend semantics.
            pixels[y * TextureSize + x] = Color.White * alpha;
        }
        noise = new Texture2D(device, TextureSize, TextureSize);
        noise.SetData(pixels);
    }

    private static double Hash(int x, int y, int octave)
    {
        uint n = unchecked((uint)(x * 374761393 + y * 668265263 + octave * 1274126177 + 917));
        n = unchecked((n ^ (n >> 13)) * 1274126177);
        return (n ^ (n >> 16)) / (double)uint.MaxValue;
    }

    public void Dispose() { noise?.Dispose(); noise = null; }
}
