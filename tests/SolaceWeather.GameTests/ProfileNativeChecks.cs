using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void ProfileNativeChecks()
    {
        var phone = Phone(); // Enforces the disposable phone profile and SvaAudit ownership.
        var romance = PhoneGet<object>(phone, "romance");
        var checks = new Dictionary<string, bool>();
        var assembly = typeof(SolaceWeather.ModEntry).Assembly;
        var conversation = assembly.GetType("SolaceWeather.Relationships.NpcConversation", true)!;
        var reply = conversation.GetNestedType("DeliveryReplyBox", BindingFlags.NonPublic)!;
        var row = assembly.GetType("SolaceWeather.Relationships.QuestChoice", true)!;
        var empty = Array.CreateInstance(row, 0);
        var choices = Expression.Lambda(Expression.GetFuncType(empty.GetType()), Expression.Constant(empty, empty.GetType())).Compile();
        var speakerField = conversation.GetField("Speaker", BindingFlags.Static | BindingFlags.NonPublic)!;
        var priorSpeaker = speakerField.GetValue(null);
        try
        {
            foreach (string name in new[] { "Abigail", "Emily", "Haley", "Penny", "Alex", "Maru" })
            foreach (string reaction in new[] { "delighted", "concerned" })
            {
                speakerField.SetValue(null, name);
                var menu = (DialogueBox)Activator.CreateInstance(reply, PhoneFlags, null,
                    new object[] { "Offline reaction fixture", choices, Array.Empty<string>(), (Action<string>)(_ => { }), reaction }, null)!;
                string retained = (string)reply.GetField("expression", PhoneFlags)!.GetValue(menu)!;
                checks[name + reaction + "SurvivesNativeReplyConstructor"] = retained == reaction;
                int cell = SolaceWeather.Core.RomanceProfiles.Get(name)!.PortraitIndex(retained);
                var portrait = Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Portraits/" + name);
                checks[name + reaction + "ProductionPortraitCell"] = cell == (reaction == "delighted" ? 1 : 2)
                    && portrait.Width / 64 * (portrait.Height / 64) > cell;
            }
        }
        finally { speakerField.SetValue(null, priorSpeaker); }
        bool prior = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");
        try
        {
            foreach (bool upgraded in new[] { false, true })
            {
                Game1.MasterPlayer.mailReceived.Remove("pamHouseUpgrade");
                if (upgraded) Game1.MasterPlayer.mailReceived.Add("pamHouseUpgrade");
                foreach (string route in new[] { "GetContext", "GetPhoneContext" })
                {
                    var context = JsonSerializer.SerializeToElement(Call(romance, route, "Penny", "How is your family home?"));
                    var home = context.GetProperty("FamilyHome");
                    checks[route + upgraded + "HomeFlag"] = home.GetProperty("PamHouseUpgraded").GetBoolean() == upgraded;
                    checks[route + upgraded + "HomeName"] = home.GetProperty("PamHome").GetString() == (upgraded ? "upgraded house" : "trailer");
                    checks[route + upgraded + "NoResidenceOrPayerInference"] = home.GetProperty("Rule").GetString()!.Contains("not proof Penny still lives there");
                }
            }
        }
        finally
        {
            Game1.MasterPlayer.mailReceived.Remove("pamHouseUpgrade");
            if (prior) Game1.MasterPlayer.mailReceived.Add("pamHouseUpgrade");
        }
        checks["OriginalUpgradeFlagRestored"] = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade") == prior;
        Helper.Data.WriteJsonFile("profile-native-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks,
            Scope = "Actual reply constructor and production context builders; no provider calls, no save, no portrait framebuffer assertion" });
    }
}
