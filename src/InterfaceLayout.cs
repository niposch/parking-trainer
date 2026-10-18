using Godot;
namespace ParkingTrainer;

internal static class InterfaceLayout
{
    // Web canvases render in device pixels; touch targets and menus use CSS pixels.
    internal static float Unit(Viewport viewport)
    {
        float density=OS.HasFeature("web")?Mathf.Max(1,DisplayServer.ScreenGetScale()):1;
        var scale=viewport.GetScreenTransform().Scale.Abs();
        return density/Mathf.Max(.01f,Mathf.Min(scale.X,scale.Y));
    }
    internal static Dictionary<string,Rect2> TouchRegions(Vector2 size,float unit,bool inspecting)
    {
        var regions=new Dictionary<string,Rect2>();
        float m=20*unit,g=8*unit,b=56*unit;
        regions["pause"]=new(size.X-m-b,m,b,b);
        regions["camera"]=new(size.X-m-b*2-g,m,b,b);
        regions["tutor"]=new(size.X-m-b*3-g*2,m,b,b);
        regions["fullscreen"]=new(size.X-m-b*3-g*3-72*unit,m,72*unit,b);
        if(inspecting)
        {
            regions["return"]=new(size.X/2-90*unit,size.Y-m-b,180*unit,b);
            regions["zoom-in"]=new(size.X/2-90*unit-g-b,size.Y-m-b,b,b);
            regions["zoom-out"]=new(size.X/2+90*unit+g,size.Y-m-b,b,b);
        }
        else
        {
            float pedalWidth=Mathf.Min(96*unit,(size.X-3*m)*.27f),height=Mathf.Min(160*unit,size.Y*.42f);
            regions["gas"]=new(size.X-m-pedalWidth,size.Y-m-height,pedalWidth,height);
            regions["brake"]=new(size.X-m-pedalWidth*2-g,size.Y-m-height,pedalWidth,height);
            float steerWidth=Mathf.Min(240*unit,size.X-m*3-pedalWidth*2-g);
            regions["steer"]=new(m,size.Y-m-height,steerWidth,height);
            float gearGap=size.X/unit<384?4*unit:g,parkingWidth=size.X/unit<384?72*unit:88*unit;
            float gearSize=Mathf.Min(b,(size.X-2*m-parkingWidth-gearGap*4)/4);
            float rowWidth=4*gearSize+parkingWidth+gearGap*4,x=size.X>size.Y?m:(size.X-rowWidth)/2,y=size.Y-m-height-g-b;
            foreach(string gear in new[]{"P","R","N","D"}) {regions[gear]=new(x,y,gearSize,b);x+=gearSize+gearGap;}
            regions["parking"]=new(x,y,parkingWidth,b);
        }
        return regions;
    }
}
