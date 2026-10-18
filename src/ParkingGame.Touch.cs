using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal int TouchMode; // Auto, On, Off. Available on desktop as well as mobile.
    bool touchSeen;
    internal TouchControls touchControls=null!;
    OptionButton touchSelector=null!;
    bool TouchEnabled=>TouchMode==1 || TouchMode==0&&(touchSeen || DisplayServer.IsTouchscreenAvailable() || OS.HasFeature("mobile"));
    void SetupTouchControls()
    {
        var layer=new CanvasLayer {Layer=5};AddChild(layer);
        touchControls=new TouchControls();layer.AddChild(touchControls);touchControls.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        touchControls.Command=TouchCommand;
        touchControls.Look=delta=>
        {
            if(Inspection) Orbit(delta.X*.005f,delta.Y*.004f);
            else {LookYaw-=delta.X*.005f;LookPitch=Mathf.Clamp(LookPitch-delta.Y*.004f,-.65f,.55f);}
        };
        SetupPedalActions();RefreshTouchControls();
    }
    void SetTouchMode(int mode)
    {
        TouchMode=Math.Clamp(mode,0,2);touchControls?.ReleaseAll();
        SetupPedalActions();RefreshTouchControls();LayoutHud();UpdateMouseCapture();SaveSettings();
    }
    void RefreshTouchControls()
    {
        if(IsInstanceValid(touchControls)) touchControls.Configure(TouchEnabled,Paused,Inspection,Car.Gear,Car.ParkingBrake);
        if(IsInstanceValid(touchSelector)) touchSelector.Select(TouchMode);
    }
    void TouchCommand(string command)
    {
        if(command=="pause") {Pause(true);return;}
        if(command=="camera") {CameraMode=(CameraMode+1)%2;SaveSettings();return;}
        if(command=="tutor") {ToggleHint();return;}
        if(command=="return") {EndInspection();return;}
        if(command is "zoom-in" or "zoom-out") {orbitDistance=Mathf.Clamp(orbitDistance+(command=="zoom-in"?-.5f:.5f),3.5f,10);return;}
        if(Inspection) return;
        if(command=="parking") {Car.ParkingBrake=!Car.ParkingBrake;return;}
        if(Enum.TryParse<Gear>(command,out var gear)) Shift(gear);
    }
    async System.Threading.Tasks.Task<int> CheckTouchControls()
    {
        int passed=0,previousMode=TouchMode;bool previousSeen=touchSeen,previousCollapsed=HintCollapsed,previousTutor=Tutor;
        void Check(bool ok,string text) {if(!ok) throw new InvalidOperationException(text);passed++;GD.Print("TOUCH PASS: "+text);}
        Vector2 Point(string control,float x=.5f,float y=.5f) {var rect=touchControls.Region(control);return rect.Position+rect.Size*new Vector2(x,y);}
        try
        {
            if(DisplayServer.GetName()=="headless") {GetWindow().Size=new(1280,800);await PhysicsFrames(3);LayoutHud();}
            Tutor=true;HintCollapsed=false;ApplyHintLayout();UpdateHud();
            var expanded=tutorPanel.GetCombinedMinimumSize();
            ToggleHint();await PhysicsFrames(3);
            Check(Tutor && HintCollapsed && !tutorText.Visible && tutorPanel.Size.Y<expanded.Y,"Hint collapses to its header without disabling the tutor");
            ToggleHint();Check(tutorText.Visible && !HintCollapsed,"Hint expands again");
            Input.ActionRelease(GasAction);Input.ActionRelease(BrakeAction);
            NewScenario(false);SetTouchMode(1);Pause(false);
            Check(touchControls.Active && !CaptureDrivingMouse,"Manual On enables pointer controls without mouse capture");
            touchControls.Pointer(0,Point("brake",.5f,.25f),true);
            touchControls.Pointer(1,Point("gas",.5f,.75f),true);
            touchControls.Pointer(2,Point("steer",.1f),true);
            ReadPedals();
            Check(Brake>.70 && Brake<.80 && Gas>.20 && Gas<.30 && touchControls.Steering>.7,"Three fingers independently hold brake, gas and steering");
            float yaw=LookYaw;var look=GetViewport().GetVisibleRect().Size*new Vector2(.5f,.3f);
            touchControls.Pointer(3,look,true);touchControls.Drag(3,look+new Vector2(80,8));
            Check(LookYaw!=yaw && touchControls.Brake>.7 && touchControls.Gas>.2,"Look dragging preserves the held pedals");
            touchControls.Pointer(1,Vector2.Zero,false,true);
            Check(touchControls.Gas==0 && touchControls.Brake>.7,"Cancellation releases only the cancelled finger");
            touchControls.ReleaseAll();touchControls.Pointer(0,Point("brake",.5f,0),true);
            touchControls.Pointer(1,Point("D"),true);
            Check(Car.Gear==Gear.D && Car.ParkingBrake,"Touch gear selection obeys the brake interlock");
            touchControls.Pointer(1,Point("parking"),true);touchControls.Pointer(0,Vector2.Zero,false);
            await PhysicsFrames(90);
            Check(Car.Speed>0 && Gas==0,"Touch brake release permits automatic forward creep");
            touchControls.Pointer(0,Point("brake",.5f,0),true);await PhysicsFrames(30);
            Check(Math.Abs(Car.Speed)<.01,"Touch brake stops the car");
            touchControls.Pointer(1,Point("R"),true);touchControls.Pointer(0,Vector2.Zero,false);await PhysicsFrames(60);
            Check(Car.Gear==Gear.R && Car.Speed<0,"Reverse is selected separately from the brake pedal");
            touchControls.Pointer(0,Point("gas",.5f,0),true);Pause(true);
            Check(!touchControls.Active && touchControls.Gas==0 && touchControls.Brake==0 && touchControls.Steering==0,"Pausing clears every held control");
            Pause(false);touchControls.Pointer(0,Point("gas",.5f,0),true);touchControls.ResizeToViewport();
            Check(touchControls.Gas==0,"Window/orientation changes release held pedals");
            Car.Gear=Gear.P;Car.ParkingBrake=true;BeginInspection();RefreshTouchControls();
            float orbit=orbitYaw;touchControls.Pointer(0,look,true);touchControls.Drag(0,look+new Vector2(50,0));
            Check(orbitYaw!=orbit && Car.Speed==0,"Inspection supports touch orbit without driving");
            touchControls.Pointer(1,Point("return"),true);RefreshTouchControls();
            Check(!Inspection && Car.ParkingBrake,"Touch return restores the secured cockpit");
            SetTouchMode(0);touchSeen=false;
            _Input(new InputEventScreenTouch {Index=9,Position=look,Pressed=true});
            Check(TouchEnabled && touchControls.Active,"Auto detects a real touch event when device detection misses it");
            SetTouchMode(2);
            Check(!TouchEnabled && !touchControls.Active && InputMap.ActionGetEvents(GasAction).Any(e=>e is InputEventMouseButton),"Off overrides detection and restores mouse pedal bindings");
            Check(GetTree().Root.ContentScaleAspect==Window.ContentScaleAspectEnum.Expand,"Viewport expands instead of adding aspect-ratio bars");
            if(DisplayServer.GetName()!="headless")
            {
                var viewport=GetViewport().GetVisibleRect().Size;var scale=GetViewport().GetScreenTransform().Scale;
                Check((viewport*scale-new Vector2(GetWindow().Size.X,GetWindow().Size.Y)).Length()<2 && Math.Abs(scale.X-scale.Y)<.001,"Uniform viewport scaling covers the whole window");
            }
        }
        finally {HintCollapsed=previousCollapsed;Tutor=previousTutor;ApplyHintLayout();touchSeen=previousSeen;SetTouchMode(previousMode);NewScenario(false);}
        return passed;
    }
}
