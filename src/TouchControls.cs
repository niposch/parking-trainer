using Godot;
namespace ParkingTrainer;

// Pointer ownership is fixed at touch-down so pedals, steering and looking
// can be held by independent fingers without stealing one another's input.
internal partial class TouchControls : Control
{
    public Action<string> Command=delegate {};
    public Action<Vector2> Look=delegate {};
    public float Gas {get;private set;}
    public float Brake {get;private set;}
    public float Steering {get;private set;}
    public bool Active {get;private set;}
    public bool Inspecting {get;private set;}
    public Gear Gear;
    public bool ParkingBrake;
    public bool Fullscreen;
    public Rect2 HintHeader,Blocker;
    readonly Dictionary<int,(string Target,Vector2 Position)> pointers=new();
    // Steering is relative to where the finger first lands, with a dead zone, so a
    // thumb can rest on the pad without turning the wheel.
    readonly Dictionary<int,Vector2> anchors=new();
    public const float SteerDeadZone=.15f;
    readonly Dictionary<string,Rect2> regions=new();
    readonly StyleBoxFlat normal=new() {BgColor=new(.025f,.07f,.08f,.80f),BorderColor=new(.65f,.75f,.73f,.7f),BorderWidthBottom=1,BorderWidthTop=1,BorderWidthLeft=1,BorderWidthRight=1};
    readonly StyleBoxFlat selected=new() {BgColor=new(.70f,.87f,.52f,.9f)};
    float unit=1;
    float laidOutUnit;
    Vector2 laidOutSize;
    public override void _Ready() {MouseFilter=MouseFilterEnum.Ignore;Resized+=()=>{ReleaseAll();Layout();};Layout();}
    public void Configure(bool enabled,bool paused,bool inspection,Gear gear,bool parkingBrake)
    {
        bool next=enabled&&!paused;
        if(Active!=next || Inspecting!=inspection) {ReleaseAll();Active=next;Inspecting=inspection;Layout();}
        Visible=Active;
        if(Gear!=gear || ParkingBrake!=parkingBrake) {Gear=gear;ParkingBrake=parkingBrake;QueueRedraw();}
    }
    public Rect2 Region(string target) {Layout();return regions[target];}
    public void ResizeToViewport() {ReleaseAll();laidOutSize=Vector2.Zero;Layout();}
    public void ReleaseAll() {pointers.Clear();anchors.Clear();Gas=Brake=Steering=0;QueueRedraw();}
    void Layout()
    {
        unit=InterfaceLayout.Unit(GetViewport());
        var size=GetViewport().GetVisibleRect().Size;
        if(laidOutSize==size && Mathf.IsEqualApprox(unit,laidOutUnit) && regions.Count>0 && regions.ContainsKey("return")==Inspecting) return;
        laidOutSize=size;laidOutUnit=unit;regions.Clear();
        foreach(var pair in InterfaceLayout.TouchRegions(size,unit,Inspecting)) regions.Add(pair.Key,pair.Value);
        foreach(var style in new[]{normal,selected}) {style.CornerRadiusTopLeft=style.CornerRadiusTopRight=style.CornerRadiusBottomLeft=style.CornerRadiusBottomRight=(int)(8*unit);}
        QueueRedraw();
    }
    // Returns whether the touch was used; touches over Blocker (an open report or
    // card) belong to the interface underneath, so it can scroll and take taps.
    public bool Pointer(int index,Vector2 position,bool pressed,bool cancelled=false)
    {
        if(!pressed || cancelled) {bool owned=pointers.Remove(index);anchors.Remove(index);Recalculate();return owned;}
        if(!Active) return false;
        Layout();
        if(HintHeader.HasPoint(position)) {Command("tutor");return true;}
        if(Blocker.HasPoint(position) && !regions.Values.Any(r=>r.HasPoint(position))) return false;
        string target=regions.FirstOrDefault(pair=>pair.Value.HasPoint(position)).Key??"look";
        if(target is "gas" or "brake" or "steer" or "look") pointers[index]=(target,position);
        if(target=="steer") anchors[index]=position;
        else if(target is not ("gas" or "brake" or "look")) Command(target);
        Recalculate();
        return true;
    }
    public bool Drag(int index,Vector2 position)
    {
        if(!pointers.TryGetValue(index,out var pointer)) return false;
        if(pointer.Target=="look") Look((position-pointer.Position)/unit);
        pointers[index]=(pointer.Target,position);Recalculate();
        return true;
    }
    void Recalculate()
    {
        Gas=Brake=Steering=0;
        foreach(var (index,pointer) in pointers)
        {
            if(pointer.Target=="look") continue;
            var rect=regions[pointer.Target];
            float value=Mathf.Clamp((rect.End.Y-pointer.Position.Y)/rect.Size.Y,0,1);
            if(pointer.Target=="gas") Gas=Mathf.Max(Gas,value);
            if(pointer.Target=="brake") Brake=Mathf.Max(Brake,value);
            if(pointer.Target=="steer")
            {
                float offset=Mathf.Clamp((anchors[index].X-pointer.Position.X)/(rect.Size.X*.35f),-1,1);
                Steering=Mathf.Sign(offset)*Mathf.Max(0,(Mathf.Abs(offset)-SteerDeadZone)/(1-SteerDeadZone));
            }
        }
        QueueRedraw();
    }
    public override void _Input(InputEvent input)
    {
        if(!Active) return;
        bool used=input switch
        {
            InputEventScreenTouch touch=>Pointer(touch.Index,touch.Position,touch.Pressed,touch.Canceled),
            InputEventScreenDrag drag=>Drag(drag.Index,drag.Position),
            InputEventMouseButton mouse when mouse.Device!=InputEvent.DeviceIdEmulation && pointers.Keys.All(i=>i<0) && mouse.ButtonIndex==MouseButton.Left=>Pointer(-1,mouse.Position,mouse.Pressed),
            InputEventMouseMotion mouse when mouse.Device!=InputEvent.DeviceIdEmulation && pointers.ContainsKey(-1)=>Drag(-1,mouse.Position),
            _=>false
        };
        if(used) GetViewport().SetInputAsHandled();
    }
    void LabelAt(Rect2 rect,string text,int fontSize,Color color)
    {
        var font=GetThemeDefaultFont();int size=(int)(fontSize*unit);
        var lines=text.Split('\n');float lineHeight=font.GetHeight(size),y=rect.GetCenter().Y-(lines.Length-1)*lineHeight/2+font.GetAscent(size)-lineHeight/2;
        foreach(string line in lines) {DrawString(font,new(rect.Position.X,y),Tr(line),HorizontalAlignment.Center,rect.Size.X,size,color);y+=lineHeight;}
    }
    public override void _Draw()
    {
        Layout();
        foreach(var pair in regions)
        {
            string name=pair.Key;var rect=pair.Value;
            bool on=name==Gear.ToString() || name=="parking"&&ParkingBrake;
            DrawStyleBox(on?selected:normal,rect);
            string label=name switch {"pause"=>"Menu","camera"=>"View","tutor"=>"?","fullscreen"=>Fullscreen?"Exit\nfull":"Full\nscreen","parking"=>"P\nBRAKE","steer"=>"<  STEER  >","gas"=>"GAS","brake"=>"BRAKE","return"=>"Return to cockpit","zoom-in"=>"+","zoom-out"=>"-",_=>name};
            if(name is "gas" or "brake")
            {
                float value=name=="gas"?Gas:Brake;
                var fill=new Rect2(rect.Position+new Vector2(4*unit,rect.Size.Y*(1-value)),new(rect.Size.X-8*unit,Mathf.Max(0,rect.Size.Y*value-4*unit)));
                if(value>0) DrawRect(fill,name=="gas"?new(.45f,.70f,.28f,.7f):new Color(.9f,.55f,.24f,.75f));
                label+=$"\n{value*100:0}%";
            }
            if(name=="steer")
            {
                var centre=rect.GetCenter();DrawLine(centre+new Vector2(0,-rect.Size.Y*.28f),centre+new Vector2(0,rect.Size.Y*.28f),new(.6f,.7f,.68f),unit);
                // While held, show the resting zone around the finger's landing point.
                foreach(var (index,anchor) in anchors)
                {
                    float half=rect.Size.X*.35f*SteerDeadZone;
                    DrawRect(new(anchor.X-half,rect.Position.Y+4*unit,half*2,rect.Size.Y-8*unit),new(.8f,.96f,.62f,.18f));
                }
                DrawCircle(centre+new Vector2(-Steering*rect.Size.X*.38f,rect.Size.Y*.23f),8*unit,new("cff59f"));
            }
            LabelAt(rect,label,name is "parking" or "fullscreen"?14:16,on?new("14262b"):new Color("f0f0e5"));
        }
    }
}
