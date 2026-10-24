using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    async void RunPhoneCheck()
    {
        try {int count=await CheckPhoneFixes();GD.Print($"PHONE COMPLETE: {count} checks");GetTree().Quit();}
        catch(Exception error) {GD.PushError("PHONE FAILED: "+error);GetTree().Quit(1);}
    }
    async System.Threading.Tasks.Task<int> CheckPhoneFixes()
    {
        int passed=0;
        void Check(bool ok,string name) {if(!ok) throw new InvalidOperationException(name);passed++;GD.Print("PHONE PASS: "+name);}
        if(DisplayServer.GetName()=="headless") {GetWindow().Size=new(1280,800);LayoutHud();}
        int initialTouchMode=TouchMode;SetTouchMode(1);
        NewScenario(false);ShowMenuPage("options");
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        menuScroll.ScrollVertical=0;
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var point=steeringButton.GetGlobalRect().GetCenter();bool steeringBefore=RateSteering;
        int before=menuScroll.ScrollVertical;
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=true});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        for(int step=0;step<4;step++)
        {
            point.Y-=30*InterfaceLayout.Unit(GetViewport());
            Input.ParseInputEvent(new InputEventScreenDrag {Index=0,Position=point,Relative=new(0,-30*InterfaceLayout.Unit(GetViewport()))});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=false});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(menuScroll.ScrollVertical>before+Math.Min(40,menuScroll.GetVScrollBar().MaxValue-menuScroll.GetVScrollBar().Page)*.5,"Swiping on a menu button scrolls the actual container");
        Check(Paused && MenuPage=="options" && RateSteering==steeringBefore,"A menu swipe does not activate the button under the finger");
        menuScroll.ScrollVertical=0;
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        point=steeringButton.GetGlobalRect().GetCenter();
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=true});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=false});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(RateSteering!=steeringBefore,"Normal menu taps still activate their button after a swipe");
        RateSteering=steeringBefore;
        Check(beep.PlaybackType==AudioServer.PlaybackType.Stream,"Alarm uses the stable streamed mixer on native and web");
        Paused=false;Car.Gear=Gear.R;Car.ParkingBrake=true;Muted=false;BeeperVolume=1;FrontRange=2;RearRange=.29;Sound(.01);
        var playback=beep.GetStreamPlayback().GetInstanceId();var stream=beep.Stream;
        for(int i=0;i<100;i++) {RearRange=i%2==0?.31:.33;Sound(.01);}
        Check(alarm.Continuous && beep.GetStreamPlayback().GetInstanceId()==playback,"Red-zone jitter reuses one playback without restarts");
        foreach(double range in new[]{.7,.29,.35,1.2,.25,2,.28})
        {RearRange=range;Sound(.01);Check(beep.Stream==stream && beep.GetStreamPlayback().GetInstanceId()==playback,"Every distance level retains one uninterrupted stream");}
        RearRange=.35;Sound(.01);
        Check(!alarm.Continuous && alarm.Enabled,"Continuous warning releases into pulses after four centimetres of clearance");
        Check(mirrorArms[1]==null && mirrorCentres[1].Y<=1.25f && mirrorCentres[1].Z>=-1.82f,"Centre mirror is lower/inboard with no projecting support rod");
        AlarmChecks.Run((ok,name)=>Check(ok,name));
        if(!IsWeb)
        {
            var original=GetWindow().Size;
            try
            {
                foreach(string page in new[]{"home","options","scenarios"})
                {
                    ShowMenuPage(page);float portraitUnit=0,portraitButton=0;
                    foreach(var dimensions in new[]{new Vector2I(390,844),new Vector2I(844,390),new Vector2I(390,844),new Vector2I(844,390),new Vector2I(390,844)})
                    {
                        GetWindow().Size=dimensions;
                        for(int frame=0;frame<6;frame++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                        var bounds=menu.GetGlobalRect();var viewport=GetViewport().GetVisibleRect().Size;
                        Check(bounds.Position.X>=-1 && bounds.Position.Y>=-1 && bounds.End.X<=viewport.X+1 && bounds.End.Y<=viewport.Y+1,$"{page}: menu fits after resizing to {dimensions}");
                        if(dimensions.X<dimensions.Y)
                        {
                            if(portraitUnit==0) {portraitUnit=menu.Scale.X;portraitButton=resumeButton.Size.Y;}
                            Check(Math.Abs(menu.Scale.X-portraitUnit)<.001 && Math.Abs(resumeButton.Size.Y-portraitButton)<.01,$"{page}: returning to portrait preserves UI scale and button size");
                        }
                    }
                }
            }
            finally {GetWindow().Size=original;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);LayoutHud();}
        }
        SetTouchMode(2);Pause(false);Inspection=false;Demo=false;Tutor=true;HintCollapsed=false;ApplyHintLayout();UpdateShortcutPanel();
        Check(shortcutPanel.Visible,"Desktop driving always displays the keyboard shortcut panel");
        _UnhandledInput(new InputEventKey {Keycode=Key.H,Pressed=true});
        Check(HintCollapsed && !tutorPanel.Visible && shortcutPanel.Visible && !Paused,"H collapses parking instructions while shortcuts remain visible");
        _UnhandledInput(new InputEventKey {Keycode=Key.H,Pressed=true});
        Check(!HintCollapsed && tutorPanel.Visible && shortcutPanel.Visible,"H expands the same instructions as the question-mark button");
        Pause(true);UpdateShortcutPanel();
        Check(shortcutPanel.Visible && !shortcutPanel.GetGlobalRect().Intersects(menu.GetGlobalRect()),"Shortcuts remain visible beside the desktop pause menu");
        SetTouchMode(1);Pause(false);UpdateShortcutPanel();
        Check(!shortcutPanel.Visible,"Touch driving does not show the desktop shortcut window");
        SetTouchMode(initialTouchMode);Car.Gear=Gear.R;
        ApplyPose();UpdateViews();
        Vector2 Project(Vector3 local)=>surroundCamera.UnprojectPosition(vehicle.ToGlobal(local));
        int uncovered=0;
        for(int x=0;x<=24;x++) for(int z=0;z<=24;z++)
        {
            var point3=new Vector3(-3.9f+7.8f*x/24,.015f,(float)CarSpec.CentreZ-3.9f+7.8f*z/24);
            var pixel=Project(point3);
            if(pixel.X<0 || pixel.Y<0 || pixel.X>600 || pixel.Y>600) uncovered++;
        }
        Check(uncovered==0,"Surround view covers every point of the 7.8 m apron without seams or blind wedges");
        Check(surroundCamera.Projection==Camera3D.ProjectionType.Orthogonal,"Surround display uses uniform metric scale instead of fisheye projection");
        var ground=Project(new(1,.015f,0));var roof=Project(new(1,1.8f,0));
        Check(ground.DistanceTo(roof)<.01,"Raised obstacles do not stretch away from their ground footprint");
        Check(Math.Abs(Project(new(1.1f,0,0)).DistanceTo(Project(new(1,0,0)))-7.5f)<.01,"Ten centimetres of clearance occupies the same number of pixels everywhere");
        var beforeProjection=Project(new(1,.015f,0));
        Car.Pose=new(4,5,.9);ApplyPose();UpdateViews();
        Check(Project(new(1,.015f,0)).DistanceTo(beforeProjection)<.01,"Surround view stays car-centred and forward-up while turning");
        Check(surroundCamera.CullMask==47 && sensorGraphic.Visible,"Surround view includes reverse guides and retains the separate distance indicators");
        NewScenario(false);
        return passed;
    }
}
