using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Presentation;

public sealed class WeatherJournalMenu : IClickableMenu
{
    private readonly WeatherUi ui;
    private Region region;
    private float scale;
    private readonly List<(Rectangle Bounds, Action Action)> controls = new();
    private int selected;
    private bool controllerMode;
    private readonly Dictionary<(Region Region, int DaysAhead), DayForecast> forecasts = new();
    private uint forecastDate;

    public WeatherJournalMenu(WeatherUi ui)
    {
        this.ui = ui;
        region = ui.Service.ActiveRegion;
        forecastDate = Game1.stats.DaysPlayed;
        Layout();
    }

    private void Layout()
    {
        scale = Math.Min(1f, Math.Min((Game1.uiViewport.Width - 24) / 960f, (Game1.uiViewport.Height - 24) / 720f));
        scale = Math.Max(0.1f, scale);
        width = (int)(960 * scale);
        height = (int)(720 * scale);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        controls.Clear();
        foreach (Region option in Enum.GetValues<Region>())
        {
            Region captured = option;
            controls.Add((Rect(28 + (int)option * 129, 91, 122, 46), () => region = captured));
        }
        controls.Add((Rect(28, 641, 180, 48), () => ui.Service.UseFahrenheit = !ui.Service.UseFahrenheit));
        controls.Add((Rect(225, 641, 445, 48), () =>
        {
            if (ui.Service.UnavailableReason == null)
                ui.Service.RequestEnabled(!(ui.Service.PendingEnabled ?? ui.Service.Enabled));
        }));
        controls.Add((Rect(890, 23, 42, 42), () => exitThisMenu()));
    }

    private Rectangle Rect(int x, int y, int w, int h) => new(xPositionOnScreen + (int)(x * scale), yPositionOnScreen + (int)(y * scale), (int)(w * scale), (int)(h * scale));
    private void Text(SpriteBatch batch, string text, int x, int y, float size = 1f, Color? color = null, int maxWidth = 880)
    {
        float fit = Math.Min(size, maxWidth / Math.Max(1, Game1.smallFont.MeasureString(text).X));
        Rectangle at = Rect(x, y, 1, 1);
        batch.DrawString(Game1.smallFont, text, new Vector2(at.X, at.Y), color ?? Game1.textColor, 0, Vector2.Zero, scale * fit, SpriteEffects.None, 1);
    }

    private void Button(SpriteBatch batch, int index, string label, bool active = false, bool disabled = false)
    {
        Rectangle box = controls[index].Bounds;
        bool hover = box.Contains(Game1.getMouseX(), Game1.getMouseY()) || controllerMode && selected == index;
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), box.X, box.Y, box.Width, box.Height,
            disabled ? Color.LightGray : active ? Color.LightGoldenrodYellow : hover ? Color.Wheat : Color.White, scale);
        float size = Math.Min(scale * .85f, (box.Width - 12 * scale) / Math.Max(1, Game1.smallFont.MeasureString(label).X));
        Vector2 measured = Game1.smallFont.MeasureString(label) * size;
        batch.DrawString(Game1.smallFont, label, new Vector2(box.Center.X - measured.X / 2, box.Center.Y - measured.Y / 2), Game1.textColor, 0, Vector2.Zero, size, SpriteEffects.None, 1);
    }

    public override void draw(SpriteBatch batch)
    {
        if (forecastDate != Game1.stats.DaysPlayed)
        {
            forecasts.Clear();
            forecastDate = Game1.stats.DaysPlayed;
        }
        batch.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * .65f);
        drawTextureBox(batch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, scale);
        Text(batch, ui.Text("journal.title"), 30, 25, 1.25f, maxWidth: 830);
        Text(batch, ui.Text(ui.Service.Enabled ? "journal.subtitle" : "journal.preview-subtitle"), 30, 63, .8f);
        foreach (Region option in Enum.GetValues<Region>()) Button(batch, (int)option, ui.RegionName(option), option == region);
        WeatherSample current = ui.Service.GetCurrent(region);
        Text(batch, ui.Text(ui.Service.Enabled ? "journal.current" : "journal.preview-current", new { region = ui.RegionName(region) }), 30, 159, 1.05f);
        Text(batch, ui.WeatherName(region, 0, current.Kind) + "  ·  " + ui.Temperature(current.TemperatureC), 30, 196, 1.3f);
        Text(batch, ui.Text("journal.details", new { rain = Math.Round(current.RainIntensity * 100), cloud = Math.Round(current.CloudCover * 100), wind = Math.Round(current.Wind * 100) }), 30, 240, .85f);
        Text(batch, ui.Text("journal.today"), 30, 284, 1.05f);
        IReadOnlyList<TimelineEntry> timeline = ui.Service.GetTodayTimeline(region);
        for (int i = 0; i < Math.Min(4, timeline.Count); i++)
        {
            TimelineEntry entry = timeline[i];
            int x = 30 + i * 229;
            Text(batch, ui.Text("period." + entry.PeriodKey), x, 322, .9f, maxWidth: 211);
            Text(batch, ui.WeatherName(region, 0, entry.Weather.Kind), x, 355, .95f, maxWidth: 211);
            Text(batch, ui.Temperature(entry.Weather.TemperatureC), x, 387, 1f, maxWidth: 211);
        }
        Text(batch, ui.Text("journal.outlook"), 30, 433, 1.05f);
        for (int days = 1; days <= 3; days++)
        {
            if (!forecasts.TryGetValue((region, days), out DayForecast? forecast))
                forecasts[(region, days)] = forecast = ui.Service.GetForecast(region, days);
            Text(batch, ui.Text("journal.forecast", new { day = ui.Text("day." + days), weather = ui.WeatherName(region, days, forecast.MainKind), low = ui.Temperature(forecast.MinTemperatureC), high = ui.Temperature(forecast.MaxTemperatureC) }), 30, 471 + (days - 1) * 32, .95f);
        }
        Text(batch, ui.Text("journal.uncertainty"), 30, 574, .75f);
        string status = ui.Service.UnavailableReason is string reason ? ui.Text(reason)
            : ui.Service.PendingEnabled is bool pending ? ui.Text(pending ? "status.pending-on" : "status.pending-off") : ui.Text(ui.Service.Enabled ? "status.on" : "status.off");
        Text(batch, status, 30, 608, .8f);
        Button(batch, 7, ui.Text(ui.Service.UseFahrenheit ? "units.fahrenheit" : "units.celsius"));
        bool desired = ui.Service.PendingEnabled ?? ui.Service.Enabled;
        Button(batch, 8, ui.Text(ui.Service.UnavailableReason != null ? "control.unavailable" : desired ? "control.disable" : "control.enable"), disabled: ui.Service.UnavailableReason != null);
        Button(batch, 9, "X");
        drawMouse(batch);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        controllerMode = false;
        foreach (var control in controls)
            if (control.Bounds.Contains(x, y)) { control.Action(); if (playSound) Game1.playSound("smallSelect"); return; }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Tab) { controllerMode = true; selected = (selected + 1) % controls.Count; }
        else if (key == Keys.Enter || key == Keys.Space) { controllerMode = true; controls[selected].Action(); }
        else base.receiveKeyPress(key);
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void receiveGamePadButton(Buttons button)
    {
        controllerMode = true;
        if (button == Buttons.B || button == Buttons.Start) exitThisMenu();
        else if (button == Buttons.A) controls[selected].Action();
        else if (button == Buttons.DPadRight || button == Buttons.DPadDown || button == Buttons.LeftThumbstickRight || button == Buttons.LeftThumbstickDown) selected = (selected + 1) % controls.Count;
        else if (button == Buttons.DPadLeft || button == Buttons.DPadUp || button == Buttons.LeftThumbstickLeft || button == Buttons.LeftThumbstickUp) selected = (selected + controls.Count - 1) % controls.Count;
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();
}
