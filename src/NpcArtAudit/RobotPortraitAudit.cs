using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace NpcArtAudit;
internal static class RobotPortraitAudit
{
    public static void Run(IModHelper helper,IMonitor monitor)
    {
        var menuField=typeof(Game1).GetField("_activeClickableMenu",BindingFlags.Static|BindingFlags.NonPublic)!;
        var oldMenu=Game1.activeClickableMenu;var oldDialogue=Game1.dialogueUp;var oldMove=Game1.player.CanMove;
        try
        {
            var panelType=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="AbigailModern").GetType("AbigailModern.PortraitPanel")!;
            var panels=panelType.GetField("Panels",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
            var script=helper.GameContent.Load<Dictionary<string,string>>("Data/Events/ScienceHouse").Single(p=>p.Key.StartsWith("10/",StringComparison.Ordinal)).Value;
            var messages=Event.ParseCommands(script).Select(ArgUtility.SplitBySpaceQuoteAware).Where(a=>a.Length>=2&&a[0]=="message").Select(a=>a[1]).ToArray();
            if(messages.Length!=8)throw new Exception("Native MarILDA dialogue count changed");
            var expressions=new[]{0,1,0,2,3,2,4,5};var positive=0;var negative=0;
            void Check(string text,int expected,string eventId="10",bool withRobot=true)
            {
                menuField.SetValue(null,null);Game1.dialogueUp=false;
                var scene=new Event{id=eventId};if(withRobot)scene.actors.Add(new NPC(new AnimatedSprite("Characters/robot",0,35,42),Vector2.Zero,2,"robot"));
                var args=new[]{"message",text};var command=scene.CurrentCommand;
                Event.DefaultCommands.Message(scene,args,new EventContext(scene,Game1.currentLocation,Game1.currentGameTime,args));
                var box=Game1.activeClickableMenu as DialogueBox??throw new Exception("Native message did not open");
                if(box.characterDialogue!=null||box.isQuestion||!box.dialogues.SequenceEqual(new[]{Game1.parseText(text)})||scene.CurrentCommand!=command)throw new Exception("Native message or command changed");
                var query=new object?[]{box,null};var found=(bool)panels.GetType().GetMethod("TryGetValue")!.Invoke(panels,query)!;
                if(found!=(expected>=0))throw new Exception("MarILDA portrait routing mismatch: expected expression "+expected);
                if(found)
                {
                    var panel=query[1]!;if((int)panel.GetType().GetProperty("Emotion")!.GetValue(panel)!!=expected)throw new Exception("Wrong MarILDA expression");
                    if((string)panel.GetType().GetProperty("Name")!.GetValue(panel)! !="MarILDA")throw new Exception("Wrong robot name");
                    var actual=(Texture2D)panel.GetType().GetProperty("Texture")!.GetValue(panel)!;var wanted=helper.GameContent.Load<Texture2D>("Portraits/robot");
                    var a=new Color[actual.Width*actual.Height];var b=new Color[wanted.Width*wanted.Height];actual.GetData(a);wanted.GetData(b);if(!a.SequenceEqual(b))throw new Exception("Wrong robot portrait pixels");
                    positive++;
                    if(expected==3||expected==5)
                    {
                        var device=Game1.graphics.GraphicsDevice;var targets=device.GetRenderTargets();
                        using var target=new RenderTarget2D(device,Game1.uiViewport.Width,Game1.uiViewport.Height);using var batch=new SpriteBatch(device);
                        try
                        {
                            device.SetRenderTarget(target);device.Clear(new Color(35,45,58));batch.Begin(samplerState:SamplerState.PointClamp);
                            box.transitioning=false;box.transitionInitialized=true;box.characterIndexInDialogue=box.getCurrentString().Length;box.draw(batch);panelType.GetMethod("Render")!.Invoke(null,new object[]{batch,box});
                            batch.End();device.SetRenderTargets(targets);using var output=File.Create(Path.Combine(helper.DirectoryPath,"robot-portrait-message-"+expected+"-runtime-preview.png"));target.SaveAsPng(output,target.Width,target.Height);
                        }
                        finally{device.SetRenderTargets(targets);}
                    }
                }
                else negative++;
            }
            for(var i=0;i<messages.Length;i++)Check(messages[i],expressions[i]);
            Check("An unrelated message in the same event.",-1);Check(messages[0],-1,"999");Check(messages[0],-1,"10",false);
            helper.Data.WriteJsonFile("robot-portrait-checks.json",new{Passed=true,NativeMessagesPreserved=positive,ExcludedMessages=negative,Expressions=6,EventCommandsUnchanged=true,FarmLoaded=false,FullEventPlayback=false,OtherLanguagesVerified=false});
            monitor.Log("MarILDA portrait routing passed eight native messages and three exclusions.",LogLevel.Info);
        }
        catch(Exception ex){helper.Data.WriteJsonFile("robot-portrait-checks.json",new{Passed=false,Error=ex.ToString()});monitor.Log("Robot portrait audit failed: "+ex,LogLevel.Error);}
        finally{menuField.SetValue(null,oldMenu);Game1.dialogueUp=oldDialogue;Game1.player.CanMove=oldMove;}
    }
}
