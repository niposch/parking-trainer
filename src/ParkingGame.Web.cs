using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    static bool IsWeb=>OS.HasFeature("web");
    bool webHadMouseCapture,webCheck;
    Button? webUpdateButton;
    Label? webUpdateLabel;
    ulong webUpdatePoll;
    void SetupWebUpdateMenu(Node parent)
    {
        webUpdateButton=Button(parent,"Reload to update",()=>{
            SaveSettings();JavaScriptBridge.Eval("window.parkingUpdates?.reload()");
        });
        webUpdateButton.Visible=false;
        webUpdateLabel=Text(parent,"",13,new("52675f"));
        webUpdateLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        webUpdateLabel.Visible=false;
    }
    void RefreshWebUpdateMenu()
    {
        if(!IsWeb || webUpdateButton==null || webUpdateLabel==null || Time.GetTicksMsec()<webUpdatePoll) return;
        webUpdatePoll=Time.GetTicksMsec()+1000;
        bool available=JavaScriptBridge.Eval("window.parkingUpdates?.status === 'available'").AsBool();
        webUpdateButton.Visible=available;webUpdateLabel.Visible=available;
        if(available) webUpdateLabel.Text=Tr("A new version is available. Reloading ends this practice session.");
    }
    void UpdateWebFocus()
    {
        RefreshWebUpdateMenu();
        if(IsWeb && IsInstanceValid(displaySelector))
        {
            int actual=DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Fullscreen?1:0;
            if(DisplayMode!=actual) {DisplayMode=actual;displaySelector.Select(actual);}
        }
        if(!IsWeb || Paused || webCheck) {webHadMouseCapture=false;return;}
        bool captured=Input.MouseMode==Input.MouseModeEnum.Captured;
        // Escape can release pointer lock without delivering a key event.
        if(webHadMouseCapture && !captured) Pause(true);
        webHadMouseCapture=captured && !Paused;
    }
    async void RunWebCheck()
    {
        int passed=0;
        void Check(bool ok,string description)
        {
            if(!ok) throw new InvalidOperationException(description);
            passed++;GD.Print("WEB PASS: "+description);
        }
        try
        {
            await PhysicsFrames(4);
            Check(IsWeb,"Actual WebAssembly runtime");
            JavaScriptBridge.Eval("window.__parkingUpdateTestStatus=window.parkingUpdates.status;window.parkingUpdates.status='available'");
            webUpdatePoll=0;RefreshWebUpdateMenu();
            Check(webUpdateButton?.Visible==true && webUpdateLabel?.Visible==true,"Menu announces a background update and offers reload");
            JavaScriptBridge.Eval("window.parkingUpdates.status=window.__parkingUpdateTestStatus;delete window.__parkingUpdateTestStatus");
            webUpdatePoll=0;RefreshWebUpdateMenu();
            Check(webUpdateButton?.Visible==false && webUpdateLabel?.Visible==false,"Update entry stays hidden when already current");
            Check(Paused && Car.Gear==Gear.P && Car.ParkingBrake,"Starts secured and paused");
            Check(!modernController.Connected,"Windows SDL driver excluded");
            Check(mirrorViewports.Length==3 && surroundCamera.Projection==Camera3D.ProjectionType.Orthogonal,"Three mirrors and a metric surround view");
            Check(beep.Stream is AudioStreamGenerator && beep.Playing,"One procedural warning stream runs in the browser");
            Check(displaySelector.ItemCount==2,"Browser display choices");
            Pause(false);Tutor=false;
            var left=new InputEventJoypadMotion {Device=15,Axis=JoyAxis.TriggerLeft,AxisValue=.75f};
            var right=new InputEventJoypadMotion {Device=15,Axis=JoyAxis.TriggerRight,AxisValue=.25f};
            Check(left.GetActionStrength(BrakeAction)>.70 && right.GetActionStrength(GasAction)>.20 && left.GetActionStrength(GasAction)==0 && right.GetActionStrength(BrakeAction)==0,"Trigger events map independently to gas and brake");
            // Browser device polling owns joypad state; test the resulting actions
            // separately from hardware discovery. This does not certify a physical pad.
            Input.ActionPress(BrakeAction,.75f);Input.ActionPress(GasAction,.25f);
            await PhysicsFrames(2);ReadPedals();
            Check(Brake>.70 && Brake<.78 && Gas>.20 && Gas<.28,$"Independent simultaneous partial analog pedals (brake {Brake:0.00}, gas {Gas:0.00})");
            Input.ActionPress(BrakeAction,1);Input.ActionRelease(GasAction);
            Shift(Gear.D);Car.ParkingBrake=false;
            Input.ActionRelease(BrakeAction);
            await PhysicsFrames(90);
            Check(Car.Speed>0 && Car.Travel>0,"Automatic creep with no accelerator");
            Input.ActionPress(BrakeAction,1);
            await PhysicsFrames(30);Check(Car.Speed<.01,"Brake stops creep");
            float yaw=LookYaw;
            _UnhandledInput(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.DpadRight,Pressed=true});
            Check(LookYaw==yaw,"D-pad does not move the driving camera");
            _UnhandledInput(new InputEventKey {Keycode=Key.Tab,Pressed=true});await PhysicsFrames(4);
            Check(CameraMode==1 && surroundRect.Visible && sensorGraphic.Visible,"Surround camera and sensors together");
            _UnhandledInput(new InputEventKey {Keycode=Key.Tab,Pressed=true});await PhysicsFrames(4);
            Check(CameraMode==0 && rearRect.Visible && sensorGraphic.Visible,"Rear camera and sensors together");
            foreach(Exercise exercise in Enum.GetValues<Exercise>())
            foreach(Difficulty difficulty in Enum.GetValues<Difficulty>())
            foreach(bool mirror in new[]{false,true})
            {
                Exercise=exercise;Difficulty=difficulty;MirrorBay=mirror;NewScenario(false);StartDemo();
                int steps=0;while(Demo && steps++<4000) RunDemo(.1);
                Check(!Demo && Inspection && Scene.PositionError(Car.Pose)<.02 && Car.Gear==Gear.P && Car.ParkingBrake,$"{exercise}/{difficulty}/{mirror}: approach, park and inspect");
            }
            await PhysicsFrames(4);
            Check(inspectionCamera.Current && !eye.Current,"Third-person inspection camera active");
            double distance=Car.Travel;var pose=Car.Pose;Orbit(.25f,.1f);await PhysicsFrames(4);
            Check(Car.Pose==pose && Car.Travel==distance,"Inspection freezes driving");
            passed+=await CheckDemoSmoothness();
            passed+=await CheckTouchControls();
            passed+=await CheckParkingAssist();
            passed+=await CheckPhoneFixes();
            NewScenario(false);Pause(false);Tutor=false;
            for(int view=0;view<2;view++)
            {
                CameraMode=view;
                ulong started=Time.GetTicksMsec();int firstFrame=frames;
                while(Time.GetTicksMsec()-started<3000) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                double fps=(frames-firstFrame)*1000.0/(Time.GetTicksMsec()-started);
                GD.Print($"WEB PERFORMANCE: {(view==0?"rear":"surround")} + sensors + 3 mirrors: {fps:0.0} FPS at {GetViewport().GetVisibleRect().Size}.");
            }
            GD.Print($"WEB COMPLETE: {passed} browser runtime checks passed.");
            Notice=$"Browser checks passed: {passed}";
        }
        catch(Exception error) {GD.PushError("WEB FAILED: "+error);}
        finally
        {
            Exercise=Exercise.Parallel;Difficulty=Difficulty.Standard;MirrorBay=false;
            Input.ActionRelease(BrakeAction);Input.ActionRelease(GasAction);
            webCheck=false;smokeMode=false;NewScenario(false);
        }
    }
}
