using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AbigailModern.Visuals;

/// <summary>Owned tint and star primitives, drawn through the caller's active world batch.</summary>
internal sealed class NightEffect : IDisposable
{
    private Texture2D? pixel;
    private void Ensure(GraphicsDevice device)
    {
        if (pixel is {IsDisposed:false} && ReferenceEquals(pixel.GraphicsDevice,device)) return;
        Dispose(); pixel=new Texture2D(device,1,1); pixel.SetData(new[]{Color.White});
    }
    public int DrawMoon(SpriteBatch batch,VisualFrame frame,VisualSettings settings)
    {
        if (!settings.Enabled || !settings.MoonlightEnabled || frame.Viewport.Width<=0 || frame.Viewport.Height<=0) return 0;
        float alpha=NightPolicy.Moon(frame.Minutes,frame.Outdoors,frame.Raining,frame.Snowing,
            frame.Lightning||frame.GreenRain,settings.MoonlightStrength);
        if(alpha<=0)return 0;
        Ensure(batch.GraphicsDevice);
        batch.Draw(pixel!,new Rectangle(0,0,frame.Viewport.Width,frame.Viewport.Height),new Color(104,153,235)*alpha);
        return 1;
    }
    public int DrawStars(SpriteBatch batch,VisualFrame frame,VisualSettings settings)
    {
        if(!settings.Enabled||!settings.StarsEnabled||frame.SkyHeight<=0||frame.Viewport.Width<=0)return 0;
        float alpha=NightPolicy.Stars(frame.Minutes,frame.Outdoors,
            frame.Raining||frame.Snowing||frame.Lightning||frame.GreenRain,settings.StarsStrength);
        if(alpha<=0)return 0;
        Ensure(batch.GraphicsDevice);
        int height=Math.Min(frame.SkyHeight,frame.Viewport.Height),calls=0;
        double seconds=double.IsFinite(frame.Seconds)?frame.Seconds:0;
        // Fixed hash positions avoid touching the game's random state or sparkling on every frame.
        for(int i=0;i<96;i++)
        {
            uint hash=unchecked((uint)(i*747796405+2891336453));
            hash=unchecked((hash^(hash>>16))*2246822519u);
            float x=(hash&65535)/65536f*frame.Viewport.Width;
            float y=(hash>>16)/65536f*frame.Viewport.Height;
            int size=i%11==0?2:1;
            if(y+size>height)continue;
            float twinkle=.72f+.28f*(float)Math.Sin(seconds*.65+i*2.39996);
            batch.Draw(pixel!,new Rectangle((int)x,(int)y,size,size),new Color(211,229,255)*(alpha*twinkle));
            calls++;
        }
        return calls;
    }
    public void Dispose(){pixel?.Dispose();pixel=null;}
}
