using Godot;
namespace ParkingTrainer;

// Small vector diagrams remain sharp at every window size. Arrows show the
// travel direction, rather than requiring an unfamiliar parking symbol font.
public partial class ParkingDiagram : Control
{
    public Exercise Exercise;
    public override void _Ready() { MouseFilter=MouseFilterEnum.Ignore;Resized+=QueueRedraw; }
    public override void _Draw()
    {
        var scale=Size/new Vector2(220,130);
        DrawSetTransform(Vector2.Zero,0,scale);
        Color line=new("6b7f78"),car=new("879b92"),active=new("254c3d"),arrow=new("406a39");
        void Rect(float x,float y,float w,float h,Color color)=>DrawStyleBox(new StyleBoxFlat {BgColor=color,CornerRadiusTopLeft=4,CornerRadiusTopRight=4,CornerRadiusBottomLeft=4,CornerRadiusBottomRight=4},new(x,y,w,h));
        void Arrow(Vector2[] points)
        {
            DrawPolyline(points,arrow,3,true);var end=points[^1];var direction=(end-points[^2]).Normalized();
            DrawLine(end,end-direction.Rotated(.65f)*11,arrow,3,true);DrawLine(end,end-direction.Rotated(-.65f)*11,arrow,3,true);
        }
        if(Exercise==Exercise.Parallel)
        {
            DrawLine(new(12,111),new(208,111),line,3,true);
            Rect(14,77,49,24,car);Rect(159,77,47,24,car);
            DrawRect(new(76,73,69,32),line,false,1.5f);Rect(131,18,48,24,active);Rect(168,22,5,16,new("c9dfd1"));
            Arrow(new[]{new Vector2(145,48),new(120,54),new(101,72),new(96,90)});
        }
        else
        {
            foreach(float x in new[]{24f,81f,139f,196f}) DrawLine(new(x,53),new(x,116),line,2,true);
            DrawLine(new(24,116),new(196,116),line,2,true);
            Rect(38,66,28,44,car);Rect(153,66,28,44,car);
            if(Exercise==Exercise.ReverseBay)
            {
                Rect(146,12,48,24,active);Rect(183,16,5,16,new("c9dfd1"));
                Arrow(new[]{new Vector2(142,24),new(120,29),new(110,48),new(110,91)});
            }
            else
            {
                Rect(24,12,48,24,active);Rect(61,16,5,16,new("c9dfd1"));
                Arrow(new[]{new Vector2(77,24),new(99,29),new(110,48),new(110,91)});
            }
        }
    }
}
