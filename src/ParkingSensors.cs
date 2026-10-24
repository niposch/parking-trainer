using Godot;
namespace ParkingTrainer;

internal partial class ParkingSensors : Control
{
    // 0-2 front left/centre/right, 3-5 rear left/centre/right,
    // 6-9 sides: front-left, front-right, rear-left, rear-right.
    readonly int[] levels=new int[10];
    internal int Level(int sector)=>levels[sector];
    public static int ProximityLevel(double distance)=>distance<.30?4:distance<.60?3:distance<1.0?2:distance<1.6?1:0;
    public void SetRanges(double[] front,double[] rear,double[]? sides=null)
    {
        bool changed=false;
        for(int i=0;i<10;i++)
        {
            double distance=i<3?front[i]:i<6?rear[i-3]:sides?[i-6]??2;
            int level=ProximityLevel(distance);
            if(level<levels[i] && ProximityLevel(distance-.04)>=levels[i]) level=levels[i];
            changed|=levels[i]!=level;levels[i]=level;
        }
        if(changed) QueueRedraw();
    }
    public override void _Ready() {MouseFilter=MouseFilterEnum.Ignore;Resized+=QueueRedraw;}
    public override void _Draw()
    {
        var centre=Size*.5f;float scale=Mathf.Min(Size.X/260,Size.Y/360);
        Vector2 P(float x,float y)=>centre+new Vector2(x,y)*scale;
        DrawStyleBox(new StyleBoxFlat {BgColor=new("d2dcd7"),CornerRadiusTopLeft=13,CornerRadiusTopRight=13,CornerRadiusBottomLeft=10,CornerRadiusBottomRight=10},new Rect2(P(-39,-76),new Vector2(78,152)*scale));
        DrawRect(new Rect2(P(-29,-49),new Vector2(58,34)*scale),new("42595e"));
        DrawRect(new Rect2(P(-29,36),new Vector2(58,25)*scale),new("42595e"));
        DrawRect(new Rect2(P(-29,-9),new Vector2(58,37)*scale),new("8a9f9d"));
        foreach(int side in new[]{-1,1})
        {
            DrawRect(new Rect2(P(side*44-5,-40),new Vector2(10,17)*scale),new("b2c5bf"));
            DrawRect(new Rect2(P(side*27-6,-72),new Vector2(12,7)*scale),new("f2e3ab"));
            DrawRect(new Rect2(P(side*27-6,65),new Vector2(12,7)*scale),new("e77868"));
        }
        Color[] colors={new("293b3f"),new("a6dc82"),new("f0d45c"),new("f89a48"),new("ff5345")};
        for(int i=0;i<6;i++)
        {
            bool front=i<3;int sector=i%3-1;
            var origin=P(sector*23,front?-77:77);
            float direction=(front?-Mathf.Pi/2:Mathf.Pi/2)+sector*(front?.60f:-.60f);
            for(int ring=0;ring<4;ring++)
            {
                float inner=(8+ring*16)*scale,outer=inner+12*scale;
                var polygon=new Vector2[18];
                for(int k=0;k<9;k++)
                {
                    float angle=direction-.27f+k*.54f/8;
                    var ray=Vector2.FromAngle(angle);polygon[k]=origin+ray*outer;polygon[17-k]=origin+ray*inner;
                }
                DrawColoredPolygon(polygon,levels[i]>=4-ring?colors[levels[i]]:colors[0]);
                if(sector!=0)
                {
                    // Lateral bars sit between the corner zones, which wrap 0.5 m along the body.
                    int side=levels[6+(front?0:2)+(sector>0?1:0)];
                    var sideRect=new Rect2(P(sector*(58+ring*13)-5,front?-59:3),new Vector2(10,56)*scale);
                    DrawRect(sideRect,side>=4-ring?colors[side]:colors[0]);
                }
            }
        }
    }
}
