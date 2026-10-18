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
            Check(Paused && Car.Gear==Gear.P,"Starts paused, secured, in first person");
            Check(mirrorScreens.All(m=>m.GetParent()==vehicle),"Mirrors are physically mounted on the car");
            Check(gearLabel.GetViewport()!=GetViewport() && rearRect.GetViewport()!=GetViewport(),"Instruments and parking feed render on cockpit screens");
            resumeButton.EmitSignal(Godot.Button.SignalName.Pressed);
            await PhysicsFrames(3);Check(!Paused,"Menu resume button starts practice");
            SendKey(Key.S,true);await PhysicsFrames(40);Tap(Key.Key4);await PhysicsFrames(3);
            Check(Car.Gear==Gear.D && Math.Abs(Car.Speed)<.001,"Keyboard brake and D selection");
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
            int cameraBefore=CameraMode;Tap(Key.Tab);await PhysicsFrames(3);Check(CameraMode==(cameraBefore+1)%3,"Parking display selection");
            bool tutorBefore=Tutor;Tap(Key.T);await PhysicsFrames(3);Check(Tutor!=tutorBefore,"Tutor toggle");
            Tap(Key.Escape);await PhysicsFrames(3);Check(Paused,"Pause input");SendKey(Key.S,false);
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
