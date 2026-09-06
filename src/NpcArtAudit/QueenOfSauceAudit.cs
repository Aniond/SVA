using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
namespace NpcArtAudit;
internal static class QueenOfSauceAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var menuField=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        var oldMenu=Game1.activeClickableMenu;var oldLocation=Game1.currentLocation;var oldAfter=Game1.afterDialogues;var oldDialogue=Game1.dialogueUp;var oldMove=Game1.player.CanMove;
        var recipes=Game1.player.cookingRecipes.Keys.ToDictionary(k=>k,k=>Game1.player.cookingRecipes[k]);var oldDay=Game1.dayOfMonth;
        var team=Game1.player.team;var oldRerunDay=team.lastDayQueenOfSauceRerunUpdated.Value;var oldRerunWeek=team.queenOfSauceRerunWeek.Value;
        var lights=Game1.currentLightSources.Keys.ToHashSet();
        void RestoreRecipes(){Game1.player.cookingRecipes.Clear();foreach(var p in recipes)Game1.player.cookingRecipes.Add(p.Key,p.Value);}
        try
        {
            Game1.currentLocation=new GameLocation("Maps/FarmHouse","FarmHouse");Game1.dayOfMonth=7;
            var panel=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            var table=panel.GetField("Panels",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
            object CheckPanel(DialogueBox box,int expression){var args=new object?[]{box,null};if(!(bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table,args)!)throw new Exception("Missing Queen of Sauce portrait");var p=args[1]!;if((string)p.GetType().GetProperty("Name")!.GetValue(p)! != "Queen of Sauce"||(int)p.GetType().GetProperty("Emotion")!.GetValue(p)! != expression)throw new Exception("Queen portrait identity or expression differs");return p;}
            string Normalize(string value)=>string.Concat(value.Where(c=>!char.IsWhiteSpace(c)));
            menuField.SetValue(null,null);Game1.dialogueUp=false;var tv=(TV)ItemRegistry.Create("(F)1468");tv.selectChannel(Game1.player,"The");
            var opening=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native opening absent");CheckPanel(opening,0);
            if(Normalize(opening.getCurrentString())!=Normalize(Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13127"))||Game1.afterDialogues?.Target!=tv)throw new Exception("Native opening or callback changed");
            var screen=helper.Reflection.GetField<TemporaryAnimatedSprite>(tv,"screen").GetValue();
            if(screen.sourceRect!=new Rectangle(602,361,42,28)||screen.animationLength!=2||screen.interval!=150)throw new Exception("Native chef TV geometry differs");
            var expectedText=(string[])typeof(TV).GetMethod("getWeeklyRecipe",BindingFlags.Instance|BindingFlags.NonPublic,null,Type.EmptyTypes,null)!.Invoke(tv,null)!;
            var expectedRecipes=Game1.player.cookingRecipes.Keys.ToDictionary(k=>k,k=>Game1.player.cookingRecipes[k]);RestoreRecipes();
            Game1.afterDialogues!();var recipe=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native recipe absent");var recipePanel=CheckPanel(recipe,2);
            if(!recipe.dialogues.Select(Normalize).SequenceEqual(expectedText.Select(Normalize))||Game1.afterDialogues?.Target!=tv)throw new Exception("Native recipe messages or callback changed");
            if(!Game1.player.cookingRecipes.Keys.ToDictionary(k=>k,k=>Game1.player.cookingRecipes[k]).OrderBy(p=>p.Key).SequenceEqual(expectedRecipes.OrderBy(p=>p.Key)))throw new Exception("Native learned-recipe result changed");
            var portrait=(Texture2D)recipePanel.GetType().GetProperty("Texture")!.GetValue(recipePanel)!;var expectedPortrait=helper.GameContent.Load<Texture2D>("Portraits/QueenOfSauce");
            var a=new Color[portrait.Width*portrait.Height];var b=new Color[expectedPortrait.Width*expectedPortrait.Height];portrait.GetData(a);expectedPortrait.GetData(b);if(!a.SequenceEqual(b))throw new Exception("Queen portrait pixels differ");
            var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
            try{device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);batch.Draw(screen.texture,new Rectangle(80,60,252,168),screen.sourceRect,Color.White);recipe.transitioning=false;recipe.transitionInitialized=true;recipe.characterIndexInDialogue=recipe.getCurrentString().Length;recipe.draw(batch);panel.GetMethod("Render")!.Invoke(null,new object[]{batch,recipe});batch.End();device.SetRenderTargets(targets);using var f=File.Create(Path.Combine(helper.DirectoryPath,"queen-of-sauce-runtime-preview.png"));target.SaveAsPng(f,target.Width,target.Height);}finally{device.SetRenderTargets(targets);}
            Game1.afterDialogues!();if(helper.Reflection.GetField<int>(tv,"currentChannel").GetValue()!=0)throw new Exception("Native TV turn-off failed");
            helper.Data.WriteJsonFile("queen-of-sauce-checks.json",new{Passed=true,NativeOpening=true,NativeRecipeMessages=true,NativeRecipeLearningPreserved=true,PortraitPixelsMatch=true,NativeTvTurnOff=true,FarmLoaded=false,FullFurnitureScene=false});
        }
        catch(Exception ex){helper.Data.WriteJsonFile("queen-of-sauce-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Queen of Sauce audit failed: "+ex,LogLevel.Error);}
        finally{RestoreRecipes();team.lastDayQueenOfSauceRerunUpdated.Value=oldRerunDay;team.queenOfSauceRerunWeek.Value=oldRerunWeek;Game1.dayOfMonth=oldDay;foreach(var key in Game1.currentLightSources.Keys.Where(k=>!lights.Contains(k)).ToArray())Game1.currentLightSources.Remove(key);menuField.SetValue(null,oldMenu);Game1.currentLocation=oldLocation;Game1.afterDialogues=oldAfter;Game1.dialogueUp=oldDialogue;Game1.player.CanMove=oldMove;}
    }
}


