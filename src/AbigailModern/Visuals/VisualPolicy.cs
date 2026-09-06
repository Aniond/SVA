namespace AbigailModern.Visuals;
internal static class VisualPolicy
{
    public static bool Eligible(bool ready,bool location,bool cutscene,bool minigame,bool mapScreenshot)
        => ready&&location&&!cutscene&&!minigame&&!mapScreenshot;
    public static float Minutes(int time,double interval,double millisecondsPerTenMinutes)
    {
        double extra=double.IsFinite(interval)&&double.IsFinite(millisecondsPerTenMinutes)&&millisecondsPerTenMinutes>0
            ?Math.Clamp(interval*10/millisecondsPerTenMinutes,0,9.999):0;
        return Math.Clamp(time/100*60+time%100+(float)extra,0,1620);
    }
}
