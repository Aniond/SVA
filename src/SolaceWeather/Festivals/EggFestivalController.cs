using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SolaceWeather.Festivals;

/// <summary>Decorations and additional conversation for the existing, unmodified Egg Festival.</summary>
public sealed class EggFestivalController
{
    private readonly Func<bool> isEnabled;
    private readonly Func<WeatherSample> currentWeather;
    private readonly IMonitor monitor;
    private ConditionalWeakTable<Event, FestivalState> states = new();
    private bool reportedDrawError;

    private sealed class FestivalState
    {
        public readonly HashSet<string> RemarkedNpcs = new(StringComparer.Ordinal);
        public readonly List<Vector2> ShelterAnchors = new();
    }

    public EggFestivalController(IModHelper helper, IMonitor monitor, Func<bool> isEnabled, Func<WeatherSample> currentWeather)
    {
        this.isEnabled = isEnabled;
        this.currentWeather = currentWeather;
        this.monitor = monitor;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Reset();
    }

    public bool IsEggFestival => isEnabled() && Game1.CurrentEvent?.isSpecificFestival("spring13") == true;

    public void Reset()
    {
        states = new();
        reportedDrawError = false;
    }

    /// <summary>Called after Event.TryGetFestivalDialogueForYear successfully constructs its dialogue.</summary>
    public void AppendDialogue(Event festival, NPC npc, string key, Dialogue? dialogue)
    {
        if (!isEnabled() || !festival.isSpecificFestival("spring13") || dialogue is null
            || festival.eventSwitched || key != npc.Name || npc.Name == "Lewis")
            return;

        // Restrict this to authored conversational NPCs, never the host/start question.
        string? remark = GetRemark(npc.Name, currentWeather());
        if (remark is null || !states.GetValue(festival, _ => new FestivalState()).RemarkedNpcs.Add(npc.Name))
            return;

        // Keep the original object, parser state, translation key, side effects, and onFinish callback.
        // This is a plain additional page, not a replacement script or a stack of new conversations.
        dialogue.dialogues.Add(new DialogueLine(remark));
    }

    private static string? GetRemark(string npc, WeatherSample weather)
    {
        bool wet = weather.Kind is WeatherKind.Rain or WeatherKind.Storm || weather.RainIntensity > .05;
        bool cold = weather.Kind == WeatherKind.Snow || weather.TemperatureC < 7;
        bool windy = weather.Kind == WeatherKind.Wind || weather.Wind > .65;
        return npc switch
        {
            "Pierre" => wet ? "A little rain won't spoil the festival. Come browse under the awning!" : cold ? "Step under the awning and out of the wind. I've still got all my festival stock." : "The awning gives us a nice little patch of shade for browsing.",
            "Gus" => wet ? "Rain on the roof, something good to eat... make yourself comfortable under the canopy." : cold ? "Chilly festival weather! A bite to eat and a spot out of the breeze should help." : "There's room under the canopy if you'd like to enjoy your food in the shade.",
            "Abigail" => wet ? "Wet grass makes the egg hunt more interesting. I'm still going for the win!" : cold ? "Cold fingers aren't going to slow me down. Those eggs are mine!" : windy ? "I hope the wind doesn't roll any eggs away before I find them!" : "Perfect weather for an egg hunt. I hope you're ready!",
            "Penny" => wet ? "The children can wait under the canopy until it's time. They're far too excited to mind the rain." : cold ? "I've reminded the children to keep warm while we wait for the hunt." : "It's nice that there's a shady place for everyone to gather.",
            "Emily" => wet ? "Listen to the raindrops! Even the weather brought its own music to the festival." : cold ? "The air has a little bite today. Let's keep the festival spirits warm!" : windy ? "Look at the festival colors dancing in the breeze!" : "What a lovely day to celebrate spring together!",
            _ => null
        };
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || !IsEggFestival)
            return;
        Event festival = Game1.CurrentEvent;
        // Hide all added decoration as soon as the original main-event script begins.
        // In particular, no roof, pole, or shadow can obscure a collectible egg.
        if (festival.eventSwitched || !festival.playerControlSequence || festival.playerControlSequenceID == "eggHunt")
            return;
        try
        {
            FestivalState state = states.GetValue(festival, _ => new FestivalState());
            if (state.ShelterAnchors.Count == 0)
            {
                // Derive anchors from the actual festival actors, accommodating the game's yearly layouts.
                foreach (string name in new[] { "Pierre", "Gus" })
                {
                    NPC? actor = festival.actors.FirstOrDefault(p => p.Name == name);
                    if (actor is not null)
                        state.ShelterAnchors.Add(actor.Position + new Vector2(32, 48));
                }
            }
            foreach (Vector2 anchor in state.ShelterAnchors)
                DrawShelter(e.SpriteBatch, anchor);
        }
        catch (Exception ex)
        {
            if (!reportedDrawError)
                monitor.Log($"Egg Festival shelters could not be drawn; festival gameplay is unchanged. {ex.Message}", LogLevel.Warn);
            reportedDrawError = true;
        }
    }

    private static void DrawShelter(SpriteBatch batch, Vector2 worldAnchor)
    {
        Vector2 local = Game1.GlobalToLocal(Game1.viewport, worldAnchor);
        int x = (int)local.X - 112;
        int y = (int)local.Y - 152;
        Color wood = new(105, 71, 49);
        Color dark = new(58, 66, 58);
        Color fabric = new(95, 151, 136);
        Color cream = new(240, 226, 178);

        void Rect(int dx, int dy, int width, int height, Color color) =>
            batch.Draw(Game1.fadeToBlackRect, new Rectangle(x + dx, y + dy, width, height), color);

        // Two narrow posts flank a completely open 3-tile front; these are drawing only.
        Rect(12, 48, 8, 112, wood);
        Rect(204, 48, 8, 112, wood);
        Rect(8, 156, 16, 4, dark);
        Rect(200, 156, 16, 4, dark);
        // Pixel-stepped sloping roof, alternating canvas panels, and a hanging valance.
        for (int row = 0; row < 8; row++)
        {
            int inset = (7 - row) * 4;
            Rect(inset, row * 4, 224 - inset * 2, 4, fabric);
            for (int stripe = 0; stripe < 7; stripe++)
            {
                int left = Math.Max(inset, stripe * 32 + 12);
                int right = Math.Min(224 - inset, stripe * 32 + 24);
                if (right > left) Rect(left, row * 4, right - left, 4, cream);
            }
        }
        Rect(0, 32, 224, 4, dark);
        Rect(0, 36, 224, 12, fabric);
        for (int stripe = 0; stripe < 7; stripe++)
        {
            Rect(stripe * 32 + 12, 36, 12, 12, cream);
            Rect(stripe * 32 + 4, 48, 24, 4, fabric);
        }
    }
}
