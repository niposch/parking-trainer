using Godot;
namespace ParkingTrainer;

// Small vector diagrams remain sharp at every window size. Arrows show the
// travel direction, rather than requiring an unfamiliar parking symbol font.
public partial class ParkingDiagram : Control
{
    public Exercise Exercise;
    public bool Mirrored;
    // The base sketch approaches facing right with the space below it: a right-side space.
    public bool FlipHorizontally=>Exercise switch
    {
        Exercise.TurnInRoad=>false,
        Exercise.Parallel or Exercise.ReverseCorner=>Mirrored,
        _=>!Mirrored
    };
    public override void _Ready() { MouseFilter=MouseFilterEnum.Ignore;Resized+=QueueRedraw; }
    public override void _Draw()
    {
        var scale=Size/new Vector2(220,130);
        // Flip for a left-side space, keeping labels driver-relative.
        DrawSetTransform(FlipHorizontally?new Vector2(Size.X,0):Vector2.Zero,0,new(FlipHorizontally?-scale.X:scale.X,scale.Y));
        Color line=new("6b7f78"),car=new("879b92"),active=new("254c3d"),arrow=new("406a39"),wall=new("4d5a55");
        void Rect(float x,float y,float w,float h,Color color)=>DrawStyleBox(new StyleBoxFlat {BgColor=color,CornerRadiusTopLeft=4,CornerRadiusTopRight=4,CornerRadiusBottomLeft=4,CornerRadiusBottomRight=4},new(x,y,w,h));
        void Arrow(Vector2[] points)
        {
            DrawPolyline(points,arrow,3,true);var end=points[^1];var direction=(end-points[^2]).Normalized();
            DrawLine(end,end-direction.Rotated(.65f)*11,arrow,3,true);DrawLine(end,end-direction.Rotated(-.65f)*11,arrow,3,true);
        }
        // The driven car faces right; the light strip marks its front.
        void Player(float x,float y) {Rect(x,y,48,24,active);Rect(x+37,y+4,5,16,new("c9dfd1"));}
        void Slanted(float x,float y,Color color)
        {
            var c=new Vector2(x,y);var along=new Vector2(.5f,.87f);var across=new Vector2(.87f,-.5f);
            DrawColoredPolygon(new[]{c-along*21-across*13,c-along*21+across*13,c+along*21+across*13,c+along*21-across*13},color);
        }
        switch(Exercise)
        {
            case Exercise.Parallel:
                DrawLine(new(12,111),new(208,111),line,3,true);
                Rect(14,77,49,24,car);Rect(159,77,47,24,car);
                DrawRect(new(76,73,69,32),line,false,1.5f);Player(131,18);
                Arrow(new[]{new Vector2(145,48),new(120,54),new(101,72),new(96,90)});
                break;
            case Exercise.AngledBay:
                foreach(float x in new[]{30f,80f,130f,180f}) DrawLine(new(x,56),new(x+35,116),line,2,true);
                DrawLine(new(30,116),new(215,116),line,2,true);
                Slanted(70,88,car);Slanted(170,88,car);Player(14,14);
                Arrow(new[]{new Vector2(66,26),new(96,32),new(112,56),new(126,84)});
                break;
            case Exercise.Garage:
                DrawLine(new(30,56),new(86,56),wall,4,true);DrawLine(new(134,56),new(190,56),wall,4,true);
                DrawLine(new(86,56),new(86,118),wall,4,true);DrawLine(new(134,56),new(134,118),wall,4,true);DrawLine(new(86,118),new(134,118),wall,4,true);
                Player(146,12);
                Arrow(new[]{new Vector2(142,24),new(120,29),new(110,48),new(110,96)});
                break;
            case Exercise.ReverseCorner:
                DrawPolyline(new[]{new Vector2(12,64),new(96,64),new(110,78),new(110,124)},line,3,true);
                DrawPolyline(new[]{new Vector2(208,64),new(164,64),new(150,78),new(150,124)},line,3,true);
                DrawLine(new(12,8),new(208,8),line,3,true);
                Player(160,30);
                Arrow(new[]{new Vector2(160,44),new(146,48),new(136,64),new(134,104)});
                break;
            case Exercise.TurnInRoad:
                DrawLine(new(12,14),new(208,14),line,3,true);DrawLine(new(12,118),new(208,118),line,3,true);
                for(float x=16;x<208;x+=24) DrawLine(new(x,66),new(x+12,66),line,1.5f,true);
                Player(18,84);
                Arrow(new[]{new Vector2(68,94),new(98,86),new(116,58),new(120,32)});
                Arrow(new[]{new Vector2(126,34),new(138,62),new(150,90)});
                Arrow(new[]{new Vector2(156,86),new(146,58),new(120,44),new(70,42)});
                break;
            default:
                foreach(float x in new[]{24f,81f,139f,196f}) DrawLine(new(x,53),new(x,116),line,2,true);
                DrawLine(new(24,116),new(196,116),line,2,true);
                Rect(38,66,28,44,car);Rect(153,66,28,44,car);
                if(Exercise==Exercise.ReverseBay)
                {
                    Player(146,12);
                    Arrow(new[]{new Vector2(142,24),new(120,29),new(110,48),new(110,91)});
                }
                else
                {
                    Player(24,12);
                    Arrow(new[]{new Vector2(77,24),new(99,29),new(110,48),new(110,91)});
                }
                break;
        }
    }
}
