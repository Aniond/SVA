using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AbigailModern.Visuals;

/// <summary>Owned soft silhouette textures; the host owns the batch and ground draw stage.</summary>
internal sealed class ShadowEffect : IDisposable
{
    private const int Size = 128;
    private Texture2D? tree;
    private Texture2D? building;

    public int Draw(SpriteBatch batch,VisualFrame frame,VisualSettings settings)
    {
        if (!settings.Enabled || !settings.ShadowsEnabled) return 0;
        float opacity = ShadowPolicy.Opacity(frame.Minutes,settings.ShadowStrength,frame.Outdoors,
            frame.Raining || frame.GreenRain,frame.Snowing,frame.Lightning);
        int limit = ShadowPolicy.CasterLimit(settings.MaxCasters);
        if (opacity <= .001f || limit == 0) return 0;
        EnsureTextures(batch.GraphicsDevice);
        var sun = ShadowPolicy.Solar(frame.Minutes);
        float rotation = MathF.Atan2(sun.Y,sun.X)+MathF.PI/2;
        int drawn = 0;
        // Bound CPU traversal even for a badly oversized supplied caster list.
        int candidates = Math.Min(frame.Casters.Count,1024);
        for (int i=0;i<candidates && drawn<limit;i++)
        {
            var caster = frame.Casters[i];
            if (!float.IsFinite(caster.Foot.X) || !float.IsFinite(caster.Foot.Y)
                || !float.IsFinite(caster.Width) || caster.Width<=0) continue;
            float length = ShadowPolicy.ProjectedLength(caster.Height,sun.Length);
            if (length <= 0) continue;
            float width = Math.Clamp(caster.Width,4,512);
            Vector2 foot = caster.Foot-new Vector2(frame.Viewport.X,frame.Viewport.Y);
            Vector2 tip = foot+new Vector2(sun.X,sun.Y)*length;
            float margin = width*.6f+8;
            if (Math.Max(foot.X,tip.X)+margin<0 || Math.Min(foot.X,tip.X)-margin>frame.Viewport.Width
                || Math.Max(foot.Y,tip.Y)+margin<0 || Math.Min(foot.Y,tip.Y)-margin>frame.Viewport.Height) continue;
            Texture2D texture = caster.Kind == CasterKind.Tree ? tree! : building!;
            batch.Draw(texture,foot,null,new Color(24,30,45)*opacity,rotation,
                new Vector2(Size*.5f,Size-1),new Vector2(width/Size,length/(Size-1)),SpriteEffects.None,0);
            drawn++;
        }
        return drawn;
    }

    private void EnsureTextures(GraphicsDevice device)
    {
        if (tree is { IsDisposed:false } && building is { IsDisposed:false }
            && ReferenceEquals(tree.GraphicsDevice,device) && ReferenceEquals(building.GraphicsDevice,device)) return;
        Dispose();
        tree = CreateTexture(device,true);
        building = CreateTexture(device,false);
    }

    private static Texture2D CreateTexture(GraphicsDevice device,bool isTree)
    {
        var pixels = new Color[Size*Size];
        for (int y=0;y<Size;y++)
        for (int x=0;x<Size;x++)
        {
            float u=(x+.5f)/Size, v=(y+.5f)/Size;
            float distance;
            if (isTree)
            {
                // Three overlapping crowns above a narrow trunk, projected as one silhouette.
                distance=Math.Min(Ellipse(u,v,.50f,.30f,.34f,.25f),
                    Math.Min(Ellipse(u,v,.34f,.47f,.24f,.23f),Ellipse(u,v,.66f,.48f,.24f,.23f)));
                float trunk=Math.Max(Math.Abs(u-.5f)-.055f,Math.Abs(v-.78f)-.22f);
                distance=Math.Min(distance,trunk);
            }
            else
            {
                // A broad building body with a pitched roof tip instead of a contact disk.
                float halfWidth = v<.25f ? .12f+v*1.2f : .42f;
                distance=Math.Max(Math.Abs(u-.5f)-halfWidth,Math.Max(.06f-v,v-.98f));
            }
            float edge = Math.Clamp(.5f-distance/.065f,0,1);
            edge=edge*edge*(3-2*edge);
            // Distant edges become lighter, keeping contact close to the caster grounded.
            float alpha=edge*(.68f+.32f*v);
            pixels[y*Size+x]=Color.White*alpha;
        }
        var texture=new Texture2D(device,Size,Size,false,SurfaceFormat.Color);
        texture.SetData(pixels);
        return texture;
    }

    private static float Ellipse(float x,float y,float cx,float cy,float rx,float ry)
        => (MathF.Sqrt((x-cx)*(x-cx)/(rx*rx)+(y-cy)*(y-cy)/(ry*ry))-1)*Math.Min(rx,ry);

    public void Dispose()
    {
        tree?.Dispose();
        building?.Dispose();
        tree=null;
        building=null;
    }
}
