using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace AbigailModern;

/// <summary>A larger view of the creator's complete outfit; editing stays in the original creator.</summary>
internal static class PlayerCreatorPreview
{
    private static readonly FieldInfo SubMenu = AccessTools.Field(typeof(TitleMenu), "_subMenu");
    private static readonly FieldInfo DisplayFarmer = AccessTools.Field(typeof(CharacterCustomization), "_displayFarmer");
    private static Preview? open;
    public static void Initialize(IModHelper helper, IMonitor monitor, string id)
    {
        new Harmony(id + ".PlayerCreatorPreview").Patch(AccessTools.Method(typeof(CharacterCustomization), "draw", new[] { typeof(SpriteBatch) }), postfix: new HarmonyMethod(typeof(PlayerCreatorPreview), nameof(DrawButton)));
        helper.Events.Input.ButtonPressed += (_, e) =>
        {
            if (open != null && (e.Button == SButton.Escape || e.Button == SButton.ControllerB))
            {
                // TitleMenu consumes its menu key before forwarding it to submenus.
                helper.Input.Suppress(e.Button); open.Close(); return;
            }
            var creator = CurrentCreator();
            if (creator == null || open != null) return;
            var point = e.Cursor.GetScaledScreenPixels();
            if (e.Button != SButton.ControllerY && (e.Button != SButton.MouseLeft || !ButtonBounds(creator.portraitBox).Contains((int)point.X, (int)point.Y))) return;
            helper.Input.Suppress(e.Button);
            try
            {
                open = new Preview(creator, Game1.activeClickableMenu is TitleMenu, (Farmer)DisplayFarmer.GetValue(creator)!);
                // The public title setter disposes the previous menu and deletes its exit delegate.
                // Retain the exact suspended creator, including its text focus and exit behavior.
                if (open.InTitle) SubMenu.SetValue(null, open); else Game1.activeClickableMenu = open;
                Game1.keyboardDispatcher.Subscriber = null;
                open.snapToDefaultClickableComponent();
            }
            catch (Exception ex) { open?.Dispose(); open = null; monitor.Log("Could not open outfit preview: " + ex.Message, LogLevel.Warn); }
        };
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            if (open != null && !ReferenceEquals(open.InTitle ? TitleMenu.subMenu : Game1.activeClickableMenu, open)) { open.Dispose(); open = null; }
        };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { open?.Dispose(); open = null; };
    }
    private static CharacterCustomization? CurrentCreator() => (Game1.activeClickableMenu is TitleMenu ? TitleMenu.subMenu : Game1.activeClickableMenu) as CharacterCustomization;
    internal static Rectangle ButtonBounds(Rectangle portrait) => new(portrait.Right - 32, portrait.Top + 4, 28, 28);
    internal static (Rectangle Panel, Rectangle Picture, Rectangle Left, Rectangle Right, Rectangle Close) Layout(int viewportWidth, int viewportHeight)
    {
        int w=Math.Min(520,viewportWidth-32), h=Math.Min(580,viewportHeight-32);
        var panel=new Rectangle((viewportWidth-w)/2,(viewportHeight-h)/2,w,h);
        int pictureHeight=Math.Min(384,Math.Max(96,h-180))/3*3, pictureWidth=pictureHeight*2/3;
        var picture=new Rectangle(panel.Center.X-pictureWidth/2,panel.Y+76,pictureWidth,pictureHeight);
        int controlWidth=(w-80)/3, controlY=panel.Bottom-80;
        return (panel,picture,new Rectangle(panel.X+24,controlY,controlWidth,48),new Rectangle(panel.X+40+controlWidth,controlY,controlWidth,48),new Rectangle(panel.X+56+controlWidth*2,controlY,controlWidth,48));
    }
    private static void DrawButton(CharacterCustomization __instance, SpriteBatch b)
    {
        var r = ButtonBounds(__instance.portraitBox);
        b.Draw(Game1.staminaRect, r, new Color(18, 53, 87, 235));
        var ink = new Color(232, 242, 255);
        // A small magnifying glass drawn without depending on another icon atlas.
        foreach (var line in new[] { new Rectangle(r.X+6,r.Y+5,12,2), new Rectangle(r.X+4,r.Y+7,2,10), new Rectangle(r.X+6,r.Y+17,12,2), new Rectangle(r.X+18,r.Y+7,2,10), new Rectangle(r.X+19,r.Y+19,3,3), new Rectangle(r.X+22,r.Y+22,3,3) }) b.Draw(Game1.staminaRect,line,ink);
        if (r.Contains(Game1.getMouseX(), Game1.getMouseY())) IClickableMenu.drawHoverText(b, "Outfit preview (Y)", Game1.smallFont);
    }
    private sealed class Preview : IClickableMenu, IDisposable
    {
        private readonly CharacterCustomization parent;
        private readonly Farmer farmer;
        private readonly object? subscriber;
        private readonly int parentFocus;
        private RenderTarget2D? picture;
        private bool disposed;
        public bool InTitle { get; }
        private readonly ClickableComponent left, right, close;
        private readonly Rectangle pictureBounds;
        public Preview(CharacterCustomization parent, bool title, Farmer source)
            : base((Game1.uiViewport.Width-520)/2, (Game1.uiViewport.Height-580)/2, 520, 580)
        {
            var layout=Layout(Game1.uiViewport.Width,Game1.uiViewport.Height);
            xPositionOnScreen=layout.Panel.X; yPositionOnScreen=layout.Panel.Y; width=layout.Panel.Width; height=layout.Panel.Height; pictureBounds=layout.Picture;
            this.parent = parent; InTitle = title; subscriber = Game1.keyboardDispatcher.Subscriber; parentFocus = parent.currentlySnappedComponent?.myID ?? -1;
            farmer = source.CreateFakeEventFarmer();
            // Clone equipment references too, so preview rendering cannot alter shared equipment state.
            farmer.shirtItem.Value = source.shirtItem.Value?.getOne() as StardewValley.Objects.Clothing;
            farmer.pantsItem.Value = source.pantsItem.Value?.getOne() as StardewValley.Objects.Clothing;
            farmer.hat.Value = source.hat.Value?.getOne() as StardewValley.Objects.Hat;
            farmer.boots.Value = source.boots.Value?.getOne() as StardewValley.Objects.Boots;
            farmer.faceDirection(source.FacingDirection); farmer.FarmerSprite.StopAnimation();
            left = new ClickableComponent(layout.Left, "Left") { myID=1, rightNeighborID=2 };
            right = new ClickableComponent(layout.Right, "Right") { myID=2, leftNeighborID=1, rightNeighborID=3 };
            close = new ClickableComponent(layout.Close, "Close") { myID=3, leftNeighborID=2 };
            allClickableComponents = new List<ClickableComponent> {left,right,close};
            try { Capture(); } catch { Dispose(); throw; }
        }
        private void Capture()
        {
            var device = Game1.graphics.GraphicsDevice;
            var targets=device.GetRenderTargets(); var viewport=device.Viewport; var scissor=device.ScissorRectangle;
            var blend=device.BlendState; var depth=device.DepthStencilState; var rasterizer=device.RasterizerState; var sampler=device.SamplerStates[0]; var texture=device.Textures[0];
            var factor=device.BlendFactor; var indices=device.Indices;
            var bindings=AccessTools.Field(typeof(GraphicsDevice),"_vertexBuffers").GetValue(device)!;
            var vertices=(VertexBufferBinding[])AccessTools.Method(bindings.GetType(),"Get",Type.EmptyTypes).Invoke(bindings,null)!;
            bool ui=FarmerRenderer.isDrawingForUI;
            try
            {
                picture ??= new RenderTarget2D(device,128,192,false,SurfaceFormat.Color,DepthFormat.None);
                device.SetRenderTarget(picture); device.Clear(Color.Transparent);
                FarmerRenderer.isDrawingForUI=true;
                using var batch=new SpriteBatch(device);
                batch.Begin(SpriteSortMode.FrontToBack,BlendState.AlphaBlend,SamplerState.PointClamp,DepthStencilState.None,RasterizerState.CullNone);
                farmer.FarmerRenderer.draw(batch,farmer.FarmerSprite.CurrentAnimationFrame,farmer.FarmerSprite.CurrentFrame,farmer.FarmerSprite.SourceRect,new Vector2(32,32),Vector2.Zero,.8f,Color.White,0,1,farmer);
                batch.End();
            }
            finally { FarmerRenderer.isDrawingForUI=ui; device.SetRenderTargets(targets); device.Viewport=viewport; device.ScissorRectangle=scissor; device.BlendState=blend; device.BlendFactor=factor; device.DepthStencilState=depth; device.RasterizerState=rasterizer; device.SetVertexBuffers(vertices); device.Indices=indices; device.SamplerStates[0]=sampler; device.Textures[0]=texture; }
        }
        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.staminaRect,new Rectangle(0,0,Game1.uiViewport.Width,Game1.uiViewport.Height),Color.Black*.65f);
            IClickableMenu.drawTextureBox(b,xPositionOnScreen,yPositionOnScreen,width,height,Color.White);
            b.DrawString(Game1.dialogueFont,"Outfit preview",new Vector2(xPositionOnScreen+44,yPositionOnScreen+24),new Color(232,242,255));
            b.Draw(Game1.staminaRect,pictureBounds,new Color(16,40,62));
            if (picture != null) b.Draw(picture,pictureBounds,Color.White);
            foreach(var control in allClickableComponents)
            {
                b.Draw(Game1.staminaRect,control.bounds,currentlySnappedComponent==control?new Color(57,99,141):new Color(25,60,96));
                var size=Game1.smallFont.MeasureString(control.name);
                b.DrawString(Game1.smallFont,control.name,new Vector2(control.bounds.Center.X-size.X/2,control.bounds.Center.Y-size.Y/2),new Color(232,242,255));
            }
            drawMouse(b);
        }
        private void Rotate(int delta) { farmer.faceDirection((farmer.FacingDirection+delta+4)%4); farmer.FarmerSprite.StopAnimation(); Capture(); }
        public override void receiveLeftClick(int x,int y,bool playSound=true) { if(left.containsPoint(x,y)) Rotate(-1); else if(right.containsPoint(x,y)) Rotate(1); else if(close.containsPoint(x,y)) Close(); }
        public override void receiveKeyPress(Keys key) { if(key==Keys.Escape) Close(); else if(key==Keys.Left) Rotate(-1); else if(key==Keys.Right) Rotate(1); }
        public override void receiveGamePadButton(Buttons button) { if(button==Buttons.B) Close(); else if(button==Buttons.LeftShoulder) Rotate(-1); else if(button==Buttons.RightShoulder) Rotate(1); else base.receiveGamePadButton(button); }
        public override void snapToDefaultClickableComponent() { currentlySnappedComponent=close; if(Game1.options.snappyMenus && Game1.options.gamepadControls) snapCursorToCurrentSnappedComponent(); }
        public override void gameWindowSizeChanged(Rectangle oldBounds,Rectangle newBounds) { Close(); }
        public override bool readyToClose() => false; // Parent title Back must not discard the suspended creator.
        public void Close()
        {
            if(disposed) return;
            if(InTitle) SubMenu.SetValue(null,parent); else Game1.activeClickableMenu=parent;
            if(parentFocus>=0) parent.setCurrentlySnappedComponentTo(parentFocus);
            Game1.keyboardDispatcher.Subscriber=(IKeyboardSubscriber?)subscriber;
            Dispose(); open=null;
        }
        public void Dispose()
        {
            if(disposed) return; disposed=true; picture?.Dispose();
            (AccessTools.Field(typeof(FarmerRenderer),"baseTexture").GetValue(farmer.FarmerRenderer) as Texture2D)?.Dispose();
            farmer.FarmerRenderer.unload();
        }
    }
}
