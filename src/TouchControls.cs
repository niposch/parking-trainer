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
    public Rect2 HintHeader;
    readonly Dictionary<int,(string Target,Vector2 Position)> pointers=new();
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
    public void ReleaseAll() {pointers.Clear();Gas=Brake=Steering=0;QueueRedraw();}
    void Layout()
    {
        var scale=GetViewport().GetScreenTransform().Scale.Abs();unit=1/Mathf.Max(.01f,Mathf.Min(scale.X,scale.Y));
        var size=GetViewport().GetVisibleRect().Size;
        if(laidOutSize==size && Mathf.IsEqualApprox(unit,laidOutUnit) && regions.Count>0 && regions.ContainsKey("return")==Inspecting) return;
        laidOutSize=size;laidOutUnit=unit;regions.Clear();
        float m=12*unit,g=8*unit,b=46*unit;
        regions["pause"]=new(size.X-m-b,m,b,b);
        regions["camera"]=new(size.X-m-b*2-g,m,b,b);
        regions["tutor"]=new(size.X-m-b*3-g*2,m,b,b);
        if(Inspecting)
        {
            regions["return"]=new(size.X/2-90*unit,size.Y-m-b,180*unit,b);
            regions["zoom-in"]=new(size.X/2-90*unit-g-b,size.Y-m-b,b,b);
            regions["zoom-out"]=new(size.X/2+90*unit+g,size.Y-m-b,b,b);
        }
        else
        {
            float pedalWidth=Mathf.Min(72*unit,(size.X-3*m)*.19f),height=Mathf.Min(130*unit,size.Y*.34f);
            regions["gas"]=new(size.X-m-pedalWidth,size.Y-m-height,pedalWidth,height);
            regions["brake"]=new(size.X-m-pedalWidth*2-g,size.Y-m-height,pedalWidth,height);
            float steerWidth=Mathf.Min(220*unit,size.X-m*3-pedalWidth*2-g);
            regions["steer"]=new(m,size.Y-m-height,steerWidth,height);
            float rowWidth=4*b+72*unit+g*4,x=size.X/unit>=620 && size.X>size.Y?size.X-m-rowWidth:(size.X-rowWidth)/2,y=size.Y-m-height-g-b;
            foreach(string gear in new[]{"P","R","N","D"}) {regions[gear]=new(x,y,b,b);x+=b+g;}
            regions["parking"]=new(x,y,72*unit,b);
        }
        foreach(var style in new[]{normal,selected}) {style.CornerRadiusTopLeft=style.CornerRadiusTopRight=style.CornerRadiusBottomLeft=style.CornerRadiusBottomRight=(int)(8*unit);}
        QueueRedraw();
    }
    public void Pointer(int index,Vector2 position,bool pressed,bool cancelled=false)
    {
        if(!pressed || cancelled) {pointers.Remove(index);Recalculate();return;}
        if(!Active) return;
        Layout();
        if(HintHeader.HasPoint(position)) {Command("tutor");return;}
        string target=regions.FirstOrDefault(pair=>pair.Value.HasPoint(position)).Key??"look";
        if(target is "gas" or "brake" or "steer" or "look") pointers[index]=(target,position);
        else Command(target);
        Recalculate();
    }
    public void Drag(int index,Vector2 position)
    {
        if(!pointers.TryGetValue(index,out var pointer)) return;
        if(pointer.Target=="look") Look((position-pointer.Position)/unit);
        pointers[index]=(pointer.Target,position);Recalculate();
    }
    void Recalculate()
    {
        Gas=Brake=Steering=0;
        foreach(var pointer in pointers.Values)
        {
            if(pointer.Target=="look") continue;
            var rect=regions[pointer.Target];
            float value=Mathf.Clamp((rect.End.Y-pointer.Position.Y)/rect.Size.Y,0,1);
            if(pointer.Target=="gas") Gas=Mathf.Max(Gas,value);
            if(pointer.Target=="brake") Brake=Mathf.Max(Brake,value);
            if(pointer.Target=="steer") Steering=Mathf.Clamp((rect.GetCenter().X-pointer.Position.X)/(rect.Size.X*.45f),-1,1);
        }
        QueueRedraw();
    }
    public override void _Input(InputEvent input)
    {
        if(!Active) return;
        switch(input)
        {
            case InputEventScreenTouch touch: Pointer(touch.Index,touch.Position,touch.Pressed,touch.Canceled);break;
            case InputEventScreenDrag drag: Drag(drag.Index,drag.Position);break;
            case InputEventMouseButton mouse when mouse.Device!=InputEvent.DeviceIdEmulation && pointers.Keys.All(i=>i<0) && mouse.ButtonIndex==MouseButton.Left:
                Pointer(-1,mouse.Position,mouse.Pressed);break;
            case InputEventMouseMotion mouse when mouse.Device!=InputEvent.DeviceIdEmulation && pointers.ContainsKey(-1): Drag(-1,mouse.Position);break;
            default:return;
        }
        GetViewport().SetInputAsHandled();
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
            string label=name switch {"pause"=>"Menu","camera"=>"View","tutor"=>"Hint","parking"=>"P\nBRAKE","steer"=>"<  STEER  >","gas"=>"GAS","brake"=>"BRAKE","return"=>"Return to cockpit","zoom-in"=>"+","zoom-out"=>"-",_=>name};
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
                DrawCircle(centre+new Vector2(-Steering*rect.Size.X*.38f,rect.Size.Y*.23f),8*unit,new("cff59f"));
            }
            LabelAt(rect,label,name=="parking"?12:14,on?new("14262b"):new Color("f0f0e5"));
        }
    }
}
