using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace NpcArtAudit;

internal static class KrobusDisguiseAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var oldLocation=Game1.currentLocation;var oldMenu=Game1.activeClickableMenu;var oldDialogue=Game1.dialogueUp;var oldMove=Game1.player.CanMove;
        var menuField=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        try
        {
            var theater=new GameLocation("Maps/MovieTheater","MovieTheater");Game1.currentLocation=theater;
            var actor=new NPC(new AnimatedSprite("Characters/Krobus",0,16,24),Vector2.Zero,2,"Krobus"){currentLocation=theater};
            actor.ChooseAppearance();
            if(actor.Sprite.textureName.Value.Replace('\\','/')!="Characters/Krobus_Trenchcoat")throw new Exception("Native theater disguise was not selected");
            static void Equal(Texture2D actual,Texture2D expected){var a=new Color[actual.Width*actual.Height];var b=new Color[expected.Width*expected.Height];actual.GetData(a);expected.GetData(b);if(!a.SequenceEqual(b))throw new Exception("Texture pixel mismatch");}
            Equal(actor.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Krobus_Trenchcoat"));
            Equal(actor.Portrait,helper.GameContent.Load<Texture2D>("Portraits/Krobus"));
            var disguise=helper.GameContent.Load<Texture2D>("Portraits/Krobus_Trenchcoat");var nativeCell=new Color[64*64];actor.Portrait.GetData(0,new Rectangle(0,256,64,64),nativeCell,0,nativeCell.Length);
            var standalone=new Color[64*64];disguise.GetData(standalone);if(!nativeCell.SequenceEqual(standalone))throw new Exception("Standalone portrait differs from native expression8");
            var reactions=MovieTheater.GetReactionsForCharacter(actor)??throw new Exception("Missing native reactions");
            var lines=reactions.Reactions.SelectMany(r=>new[]{r.SpecialResponses?.BeforeMovie?.Text,r.SpecialResponses?.DuringMovie?.Text,r.SpecialResponses?.AfterMovie?.Text}).Where(t=>t!=null).Cast<string>().ToArray();
            helper.Data.WriteJsonFile("krobus-movie-lines.json",lines);
            var text=lines.FirstOrDefault()??throw new Exception("No native movie dialogue found");
            var dialogue=new Dialogue(actor,null,text);var box=new DialogueBox(dialogue);
            menuField.SetValue(null,box);box.transitioning=false;box.transitionInitialized=true;box.characterIndexInDialogue=box.getCurrentString().Length;
            if(dialogue.getPortraitIndex()!=8)throw new Exception("Native movie line did not select expression8");
            var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
            try
            {
                device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                for(var i=0;i<16;i++)batch.Draw(actor.Sprite.Texture,new Rectangle(50+(i%4)*80,30+(i/4)*100,64,96),new Rectangle((i%4)*16,(i/4)*24,16,24),Color.White);
                box.draw(batch);batch.End();device.SetRenderTargets(targets);using var stream=File.Create(Path.Combine(helper.DirectoryPath,"krobus-disguise-preview.png"));target.SaveAsPng(stream,target.Width,target.Height);
            }
            finally{device.SetRenderTargets(targets);}
            var sewer=new GameLocation("Maps/Sewer","Sewer");Game1.currentLocation=sewer;actor.currentLocation=sewer;actor.ChooseAppearance();
            if((actor.Sprite.overrideTextureName??actor.Sprite.textureName.Value).Replace('\\','/')!="Characters/Krobus")throw new Exception($"Normal appearance not restored: actorLocation={actor.currentLocation?.Name}, world={Game1.currentLocation?.Name}, texture={actor.Sprite.textureName.Value}, override={actor.Sprite.overrideTextureName}, appearance={actor.LastAppearanceId}");
            Equal(actor.Sprite.Texture,helper.GameContent.Load<Texture2D>("Characters/Krobus"));
            helper.Data.WriteJsonFile("krobus-disguise-checks.json",new{Passed=true,NativeAppearanceSelected=true,SpritePixelsMatch=true,MovieDialogueExpression=8,PortraitPixelsMatch=true,NormalAppearanceRestored=true,FarmLoaded=false,FullScreeningVerified=false,MovieDialogueLines=lines});
            monitor.Log("Krobus disguise checks passed: native appearance, movie expression8, matching pixels and normal appearance restored.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("krobus-disguise-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log($"Krobus disguise audit failed: {ex}",LogLevel.Error);}
        finally{Game1.currentLocation=oldLocation;menuField.SetValue(null,oldMenu);Game1.dialogueUp=oldDialogue;Game1.player.CanMove=oldMove;}
    }
}
