using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace AbigailModern;

// Decorates MarILDA's existing event messages; native text and progression remain intact.
internal static class RobotPortraits
{
    private static IModHelper Helper=null!;
    private static IMonitor Monitor=null!;
    private static readonly int[] Expressions={0,1,0,2,3,2,4,5};

    public static void Initialize(IModHelper helper,IMonitor monitor,string uniqueId)
    {
        Helper=helper;Monitor=monitor;
        new Harmony(uniqueId).Patch(AccessTools.Method(typeof(Event.DefaultCommands),nameof(Event.DefaultCommands.Message)),
            prefix:new HarmonyMethod(typeof(RobotPortraits),nameof(Before)),
            postfix:new HarmonyMethod(typeof(RobotPortraits),nameof(After)));
    }

    private static void Before(out IClickableMenu? __state)=>__state=Game1.activeClickableMenu;

    private static void After(Event @event,string[] args,IClickableMenu? __state)
    {
        if(@event.id!="10"||args.Length<2||ReferenceEquals(__state,Game1.activeClickableMenu)
            ||Game1.activeClickableMenu is not DialogueBox box||box.characterDialogue!=null||box.isQuestion)return;
        var actor=@event.getActorByName("robot");
        if(actor?.Sprite?.textureName.Value.Replace('\\','/').Equals("Characters/robot",StringComparison.OrdinalIgnoreCase)!=true)return;
        try
        {
            // Resolve the current language's native lines instead of matching English text.
            var script=Helper.GameContent.Load<Dictionary<string,string>>("Data/Events/ScienceHouse")
                .FirstOrDefault(p=>p.Key.StartsWith("10/",StringComparison.Ordinal)).Value;
            if(script==null)return;
            var messages=Event.ParseCommands(script).Select(ArgUtility.SplitBySpaceQuoteAware)
                .Where(a=>a.Length>=2&&a[0]=="message").Select(a=>a[1]).ToArray();
            if(messages.Length!=Expressions.Length)return;
            var index=Array.IndexOf(messages,args[1]);
            if(index>=0)PortraitPanel.Attach(box,Helper.GameContent.Load<Texture2D>("Portraits/robot"),"MarILDA",Expressions[index]);
        }
        catch(Exception ex){Monitor.Log($"Could not display MarILDA's portrait: {ex.Message}",LogLevel.Warn);}
    }
}
