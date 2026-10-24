using Godot;
namespace ParkingTrainer;

// Button names by controller family; face buttons follow position (bottom, left, right).
public sealed record PadLabels(string Gas,string Brake,string Reverse,string Drive,string Park,string ParkingBrake,string Camera,string Console,string Shoulder,string Hint,string Plan)
{
    public static readonly PadLabels Xbox=new("RT","LT","LB","RB","A","Y","X","B","View","D-pad up","D-pad down");
    public static readonly PadLabels PlayStation=new("R2","L2","L1","R1","Cross","Triangle","Square","Circle","Create","D-pad up","D-pad down");
    public static readonly PadLabels Nintendo=new("ZR","ZL","L","R","B","X","Y","A","Minus","D-pad up","D-pad down");
    public static PadLabels For(string name)
    {
        name=name.ToLowerInvariant();
        if(new[]{"playstation","dualsense","dualshock","ps4","ps5","ps3","sony","wireless controller"}.Any(name.Contains)) return PlayStation;
        if(new[]{"nintendo","switch","pro controller","joy-con"}.Any(name.Contains)) return Nintendo;
        return Xbox;
    }
}

// Live key/button caps for desktop driving: the same at-a-glance state that the
// touch overlay gives on phones (pedals, wheel, gear and parking brake).
public partial class ControlStrip : Control
{
    public const float DesignWidth=590,DesignHeight=74;
    public float Gas,Brake,SteerInput;
    public double Steer;
    public Gear Gear;
    public bool ParkingBrake;
    public PadLabels? Labels;
    bool Controller=>Labels!=null;
    public override void _Ready() {MouseFilter=MouseFilterEnum.Ignore;CustomMinimumSize=new(DesignWidth,DesignHeight);}
    public void Set(float gas,float brake,float steerInput,double steer,Gear gear,bool parkingBrake,PadLabels? labels)
    {
        if(gas==Gas&&brake==Brake&&steerInput==SteerInput&&steer==Steer&&gear==Gear&&parkingBrake==ParkingBrake&&labels==Labels) return;
        Gas=gas;Brake=brake;SteerInput=steerInput;Steer=steer;Gear=gear;ParkingBrake=parkingBrake;Labels=labels;QueueRedraw();
    }
    public override void _Draw()
    {
        var font=ThemeDB.FallbackFont;
        Color plate=new(.02f,.045f,.05f,.55f),cap=new(.93f,.94f,.9f,.16f),text=new("f0f0e5"),accent=new("cff59f"),amber=new("ffb340"),ink=new("14262b");
        DrawStyleBox(new StyleBoxFlat {BgColor=plate,CornerRadiusTopLeft=10,CornerRadiusTopRight=10,CornerRadiusBottomLeft=10,CornerRadiusBottomRight=10},new(0,0,DesignWidth,DesignHeight));
        float x=12;
        void Key(string label,float width,float fill,Color active)
        {
            var rect=new Rect2(x,8,width,34);
            DrawStyleBox(new StyleBoxFlat {BgColor=cap,CornerRadiusTopLeft=6,CornerRadiusTopRight=6,CornerRadiusBottomLeft=6,CornerRadiusBottomRight=6},rect);
            if(fill>0)
            {
                float h=34*Math.Clamp(fill,0,1);
                DrawStyleBox(new StyleBoxFlat {BgColor=active,CornerRadiusTopLeft=6,CornerRadiusTopRight=6,CornerRadiusBottomLeft=6,CornerRadiusBottomRight=6},new(x,42-h,width,h));
            }
            DrawString(font,new(x,31),label,HorizontalAlignment.Center,width,15,fill>.5?ink:text);
            x+=width+5;
        }
        void Caption(float from,string label) {DrawString(font,new(from,63),label,HorizontalAlignment.Center,x-5-from,12,new Color(text,.85f));x+=12;}
        float start=x;
        Key(Labels?.Gas??"W",46,Gas,accent);Key(Labels?.Brake??"S",46,Brake,new("ff997a"));
        Caption(start,Tr("gas · brake"));
        start=x;
        if(Controller) Key(Tr("L stick"),78,Mathf.Abs(SteerInput),accent);
        else {Key("A",36,SteerInput>0?1:0,accent);Key("D",36,SteerInput<0?1:0,accent);}
        // Wheel position: a dial whose spoke turns with the steering wheel.
        var centre=new Vector2(x+17,25);DrawArc(centre,15,0,Mathf.Tau,24,text,2,true);
        var spoke=Vector2.Up.Rotated((float)(-Steer/CarSpec.MaxSteer)*Mathf.Pi*.75f);DrawLine(centre,centre+spoke*15,Math.Abs(Steer)<.02?accent:amber,3,true);
        x+=39;
        Caption(start,Tr("steer"));
        start=x;
        foreach(var gear in new[]{Gear.P,Gear.R,Gear.N,Gear.D})
        {
            string key=Labels!=null?gear switch {Gear.P=>$"{Labels.Park} P",Gear.R=>$"{Labels.Reverse} R",Gear.D=>$"{Labels.Drive} D",_=>"N"}:$"{(int)gear+1} {gear}";
            Key(key,Controller?(gear==Gear.N?30:62):44,Gear==gear?1:0,accent);
        }
        Caption(start,Labels!=null?string.Format(Tr("gear · hold {0} to shift"),Labels.Brake):Tr("gear · hold S to shift"));
        start=x;
        Key(Labels?.ParkingBrake??Tr("Space"),84,ParkingBrake?1:0,amber);
        Caption(start,Tr(ParkingBrake?"P-brake ON":"P-brake off"));
    }
}
