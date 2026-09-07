using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;

namespace SolaceWeather.Tailoring;

/// <summary>Only generated fabric shading enters the native silhouette; masks and sleeve markers stay authored.</summary>
internal static class FabricConverter
{
    internal static (byte[] Png, string Color) Convert(byte[] donor, Texture2D template, string slot)
    {
        var (w, h) = PortraitImageHeader.Read(donor);
        using var stream = new MemoryStream(donor);
        using var image = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
        if (image.Width != w || image.Height != h) throw new InvalidDataException("Donor dimensions changed.");
        var pixels = new Color[w * h]; image.GetData(pixels);
        var fabric = new Color[64];
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
        {
            long r = 0, g = 0, b = 0, n = 0;
            for (int iy = y * h / 8; iy < (y + 1) * h / 8; iy++) for (int ix = x * w / 8; ix < (x + 1) * w / 8; ix++)
            { var p = pixels[iy * w + ix]; if (p.A < 128) continue; r += p.R; g += p.G; b += p.B; n++; }
            if (n == 0) throw new InvalidDataException("Transparent fabric.");
            fabric[y * 8 + x] = new Color((int)(r / n), (int)(g / n), (int)(b / n));
        }
        static int Luma(Color p) => (p.R * 3 + p.G * 6 + p.B) / 10;
        double mean = fabric.Average(p => Luma(p));
        if (fabric.Max(Luma) - fabric.Min(Luma) < 8) throw new InvalidDataException("Fabric has no readable pattern.");
        int width = slot == "shirt" ? 256 : 192, height = slot == "shirt" ? 32 : 688;
        if (template.Width != width || template.Height != height) throw new InvalidDataException("Invalid tailoring template.");
        var output = new Color[width * height]; template.GetData(output); int changed = 0;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int index = y * width + x; var p = output[index];
            if (p.A == 0 || slot == "shirt" && (x < 128 || x == 128 && y is >= 2 and <= 4)) continue;
            int delta = Math.Clamp((int)Math.Round((Luma(fabric[(y % 8) * 8 + x % 8]) - mean) * .6), -36, 36);
            int value = Math.Clamp(Luma(p) + delta, 24, 250);
            var next = new Color(value, value, value, p.A); if (next != p) changed++; output[index] = next;
        }
        if (changed < 20) throw new InvalidDataException("Insufficient generated detail.");
        using var texture = new Texture2D(Game1.graphics.GraphicsDevice, width, height); texture.SetData(output);
        using var png = new MemoryStream(); texture.SaveAsPng(png, width, height);
        return (png.ToArray(), $"{(int)fabric.Average(p => p.R)} {(int)fabric.Average(p => p.G)} {(int)fabric.Average(p => p.B)}");
    }
}
