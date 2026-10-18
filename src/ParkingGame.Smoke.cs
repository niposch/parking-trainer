using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    async System.Threading.Tasks.Task PhysicsFrames(int count)
    {
        for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    static void SendKey(Key key,bool down)
    {
        Input.ParseInputEvent(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=down});
    }
    static void Tap(Key key) {SendKey(key,true);SendKey(key,false);}
    async void RunSmoke()
    {
        int passed=0;
        void Check(bool condition,string name) {if(!condition) throw new InvalidOperationException(name);passed++;GD.Print("SMOKE PASS: "+name);}
        try
        {
            await PhysicsFrames(4);
            Check(Paused && Car.Gear==Gear.P && Car.ParkingBrake,"Starts paused in P with parking brake applied");
            Check(mirrorScreens.All(m=>m.GetParent()==vehicle),"Mirrors are physically mounted on the car");
            Check(gearLabel.GetViewport()!=GetViewport() && rearRect.GetViewport()!=GetViewport(),"Instruments and parking feed render on cockpit screens");
            sensorGraphic.SetRanges(new[]{2.0,.25,.8},new[]{.5,1.2,2.0});
            Check(Enumerable.Range(0,6).Select(sensorGraphic.Level).SequenceEqual(new[]{0,4,2,3,1,0}),"Each front/rear sensor has independent stepped visual proximity");
            Check(ParkingSensors.ProximityLevel(.3)==3 && ParkingSensors.ProximityLevel(.6)==2 && ParkingSensors.ProximityLevel(1)==1 && ParkingSensors.ProximityLevel(1.6)==0,"Sensor display thresholds match the documented steps");
            resumeButton.EmitSignal(Godot.Button.SignalName.Pressed);
            await PhysicsFrames(3);Check(!Paused,"Menu resume button starts practice");
            var secured=Car.Pose;await PhysicsFrames(120);
            Check(Car.Gear==Gear.P && Car.ParkingBrake && Car.Speed==0 && Car.Pose==secured,"Drive/Resume preserves P and parking brake without creeping");
            Input.ParseInputEvent(new InputEventMouseMotion {ScreenRelative=new(30,-10)});await PhysicsFrames(3);
            Check(LookYaw<-.05 && LookPitch>0 && CaptureDrivingMouse,"Mouse look activates in driving mode");
            if(DisplayServer.GetName()!="headless") Check(Input.MouseMode==Input.MouseModeEnum.Captured,"Windows cursor is captured while driving");
            float unchangedYaw=LookYaw,unchangedPitch=LookPitch;
            foreach(var direction in new[]{JoyButton.DpadLeft,JoyButton.DpadRight,JoyButton.DpadUp,JoyButton.DpadDown})
            {
                Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=direction,Pressed=true});await PhysicsFrames(3);
                Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=direction,Pressed=false});await PhysicsFrames(3);
            }
            SendKey(Key.Left,true);await PhysicsFrames(10);SendKey(Key.Left,false);
            Check(LookYaw==unchangedYaw && LookPitch==unchangedPitch,"D-pad and Steam-emulated arrow keys do not move the driving camera");
            foreach(var pedal in new[]{(Axis:JoyAxis.TriggerLeft,Action:BrakeAction),(Axis:JoyAxis.TriggerRight,Action:GasAction)})
            {
                Input.ParseInputEvent(new InputEventJoypadMotion {Device=15,Axis=pedal.Axis,AxisValue=.52f});await PhysicsFrames(3);
                Check(Input.GetActionStrength(pedal.Action)>.45 && Input.GetActionStrength(pedal.Action)<.6,$"Analog {pedal.Axis} accepted from a virtual gamepad that is not device zero");
                Input.ParseInputEvent(new InputEventJoypadMotion {Device=15,Axis=pedal.Axis,AxisValue=0});await PhysicsFrames(3);
                Check(Input.GetActionStrength(pedal.Action)==0,$"{pedal.Axis} releases to zero");
            }
            foreach(var pedal in new[]{(Button:MouseButton.Left,Action:GasAction),(Button:MouseButton.Right,Action:BrakeAction)})
            {
                Input.ParseInputEvent(new InputEventMouseButton {ButtonIndex=pedal.Button,Pressed=true});await PhysicsFrames(3);
                Check(Input.GetActionStrength(pedal.Action)==1,$"Steam keyboard/mouse {pedal.Button} pedal fallback");
                Input.ParseInputEvent(new InputEventMouseButton {ButtonIndex=pedal.Button,Pressed=false});await PhysicsFrames(3);
            }
            // Verify rebinding through the same event handler used by the setup menu.
            Pause(true);BeginPedalBinding(GasAction);
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.Misc1,Pressed=true});await PhysicsFrames(3);
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.Misc1,Pressed=false});await PhysicsFrames(3);
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.Misc1,Pressed=true});await PhysicsFrames(3);
            Check(Input.GetActionStrength(GasAction)==1,"Pedal rebinding accepts a gamepad button");
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.Misc1,Pressed=false});await PhysicsFrames(3);
            var bindings=new ConfigFile();SavePedalBindings(bindings);
            var reloaded=new ConfigFile();Check(reloaded.Parse(bindings.EncodeToText())==Error.Ok,"Pedal settings serialize successfully");
            gasBinding=null;LoadPedalBindings(reloaded);
            Check(gasBinding is InputEventJoypadButton {ButtonIndex:JoyButton.Misc1,Device:-1},"Saved pedal binding restores across device enumeration changes");
            ResetPedalBindings();Pause(false);
            SendKey(Key.S,true);await PhysicsFrames(40);Tap(Key.Key4);await PhysicsFrames(3);
            Check(Car.Gear==Gear.D && Car.ParkingBrake && Math.Abs(Car.Speed)<.001,"Gear selection preserves parking brake");
            Tap(Key.Space);await PhysicsFrames(3);Check(!Car.ParkingBrake,"Parking brake requires explicit release");
            var initial=Car.Pose;SendKey(Key.S,false);await PhysicsFrames(180);
            Check(Car.Speed>.3 && Scene.PositionError(Car.Pose)!=Scene.PositionError(initial),"Releasing the brake produces live forward creep");
            SendKey(Key.A,true);await PhysicsFrames(50);SendKey(Key.A,false);
            Check(Car.Steer>.2,"Steering input reaches the simulation");
            await PhysicsFrames(2);double held=Car.Steer;await PhysicsFrames(15);Check(Math.Abs(Car.Steer-held)<.001,"Rate steering holds when stick/key is released");
            SendKey(Key.S,true);await PhysicsFrames(100);Check(Car.Speed==0,"Live service brake stops the car");
            Tap(Key.Key2);await PhysicsFrames(3);Check(Car.Gear==Gear.R,"Live reverse selection with held brake");
            SendKey(Key.S,false);await PhysicsFrames(120);Check(Car.Speed<-.2,"Same released brake produces reverse creep");
            SendKey(Key.S,true);await PhysicsFrames(100);Tap(Key.Key1);Tap(Key.Space);await PhysicsFrames(3);
            Check(Car.Gear==Gear.P && Car.ParkingBrake,"Parking sequence secures the car");
            Check(Inspection && inspectionCamera.Current,"Securing the car after moving automatically enters third-person inspection");
            var inspectedPose=Car.Pose;float orbitBefore=orbitYaw;
            Input.ParseInputEvent(new InputEventMouseMotion {ScreenRelative=new(50,10)});SendKey(Key.W,true);await PhysicsFrames(30);SendKey(Key.W,false);
            Check(orbitYaw!=orbitBefore && Car.Pose==inspectedPose && Car.Speed==0,"Inspection orbits with the mouse while driving stays disabled");
            Tap(Key.Enter);await PhysicsFrames(5);
            Check(!Inspection && eye.Current && Car.ParkingBrake,"Return from inspection restores cockpit without releasing the parking brake");
            int cameraBefore=CameraMode;Tap(Key.Tab);await PhysicsFrames(3);Check(CameraMode==(cameraBefore+1)%2 && sensorGraphic.Visible && (rearRect.Visible||surroundRect.Visible),"Camera and stepped sensors remain visible together when changing display");
            bool tutorBefore=Tutor;Tap(Key.T);await PhysicsFrames(3);Check(Tutor!=tutorBefore,"Tutor toggle");
            Tap(Key.Escape);await PhysicsFrames(3);Check(Paused,"Pause input");SendKey(Key.S,false);
            float yawBefore=LookYaw;Input.ParseInputEvent(new InputEventMouseMotion {ScreenRelative=new(100,0)});await PhysicsFrames(3);
            Check(!CaptureDrivingMouse && LookYaw==yawBefore,"Pause releases mouse and suppresses mouse look");
            if(DisplayServer.GetName()!="headless") Check(Input.MouseMode==Input.MouseModeEnum.Visible,"Windows cursor is released in menu");
            NewScenario(true);await PhysicsFrames(3);
            Check(Car.Gear==Gear.P && Car.ParkingBrake && Car.Speed==0,"New variation resets gear and parking brake");
            foreach(Exercise exercise in Enum.GetValues<Exercise>())
            {
                Exercise=exercise;NewScenario(false);StartDemo();
                int safety=0;while(Demo && safety++<3000) RunDemo(.1);
                Check(!Demo && Scene.PositionError(Car.Pose)<.02,$"{exercise} demonstration reaches parking position");
            }
            await PhysicsFrames(4);
            GD.Print($"SMOKE COMPLETE: {passed} runtime input/UI/demonstration checks passed.");GetTree().Quit();
        }
        catch(Exception error) {GD.PushError("SMOKE FAILED: "+error);GetTree().Quit(1);}
    }
}
