using System;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Presentation;

public sealed class WeatherUi
{
    internal readonly IWeatherPresentation Service;
    private readonly ITranslationHelper translations;
    public WeatherUi(IWeatherPresentation service, ITranslationHelper translations)
    {
        Service = service;
        this.translations = translations;
    }

    internal string Text(string key, object? tokens = null) => translations.Get(key, tokens).ToString();
    internal string RegionName(Region region) => Text("region." + region.ToString().ToLowerInvariant());
    internal string KindName(WeatherKind kind) => Text("weather." + kind.ToString().ToLowerInvariant());
    internal string WeatherName(Region region, int daysAhead, WeatherKind kind) => Service.GetSpecialWeather(region, daysAhead) switch
    {
        "Festival" => Text("special.festival"),
        "Wedding" => Text("special.wedding"),
        "GreenRain" => Text("special.greenrain"),
        _ => KindName(kind)
    };
    internal string Temperature(double celsius) => FormatTemperature(celsius, Service.UseFahrenheit);

    public static string FormatTemperature(double celsius, bool fahrenheit) =>
        Math.Round(fahrenheit ? celsius * 9d / 5d + 32d : celsius, MidpointRounding.AwayFromZero)
            .ToString("0", CultureInfo.CurrentCulture) + (fahrenheit ? "°F" : "°C");

    // Derive this every time so window size and UI scaling changes cannot leave a stale hit box.
    private Rectangle HudBounds => new(
        Math.Clamp((Game1.dayTimeMoneyBox?.xPositionOnScreen ?? Game1.uiViewport.Width - 300) - 56, 4, Math.Max(4, Game1.uiViewport.Width - 52)),
        Math.Clamp((Game1.dayTimeMoneyBox?.yPositionOnScreen ?? 8) + 8, 4, Math.Max(4, Game1.uiViewport.Height - 52)), 48, 48);
    public bool HitTestHud(int x, int y) => HudBounds.Contains(x, y);
    public void OpenJournal() => Game1.activeClickableMenu = new WeatherJournalMenu(this);

    public void DrawHud(SpriteBatch batch)
    {
        Rectangle bounds = HudBounds;
        IClickableMenu.drawTextureBox(batch, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White);
        batch.DrawString(Game1.smallFont, "W", new Vector2(bounds.X + 13, bounds.Y + 10), Game1.textColor);
        if (bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
            IClickableMenu.drawHoverText(batch, Text("hud.tooltip"), Game1.smallFont);
    }

    public string FormatTomorrowReport() => Text("tv.intro") + " " + string.Join(" ",
        Enum.GetValues<Region>().Select(FormatTomorrowReport));

    public string FormatTomorrowReport(Region region)
    {
        DayForecast forecast = Service.GetForecast(region, 1);
        return Text("tv.region", new
        {
            region = RegionName(region), weather = WeatherName(region, 1, forecast.MainKind),
            low = Temperature(forecast.MinTemperatureC), high = Temperature(forecast.MaxTemperatureC)
        });
    }
}
