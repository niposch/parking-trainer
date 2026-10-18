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
            var body=WorldFactory.Meshes(vehicle).Single(m=>m.Name=="body");
            var bodyBounds=vehicle.GlobalTransform.AffineInverse()*body.GlobalTransform*body.GetAabb();
            Check(Math.Abs(bodyBounds.Size.X-(float)CarSpec.Width)<.01 && Math.Abs(bodyBounds.Size.Z-(float)CarSpec.Length)<.01,"Single adapted exterior matches physical body width and length");
            Check(!WorldFactory.Meshes(vehicle).Any(m=>m.Name=="CabinShell"),"No overlapping replacement exterior shell");
            var forwardEye=PlayerCabin.EyeTarget(0,EyeHeight);var sideEye=PlayerCabin.EyeTarget(-Mathf.Pi/2,EyeHeight);var backEye=PlayerCabin.EyeTarget(-Mathf.Pi,EyeHeight);
            Check(forwardEye==sideEye && backEye.X>forwardEye.X+.30 && backEye.Z>forwardEye.Z+.15,"Rearward torso lean moves eye inboard and back without shifting the normal side view");
            Check(body.Mesh is ArrayMesh lining && lining.GetSurfaceCount()==2 && body.GetChildCount()==1,"One continuous painted body has an interior lining and exterior-only glass");
            var triangles=body.Mesh.GetFaces();
            bool BodyBlocks(Vector3 from,Vector3 to)
            {
                from=body.ToLocal(vehicle.ToGlobal(from));to=body.ToLocal(vehicle.ToGlobal(to));
                for(int i=0;i<triangles.Length;i+=3)
                    if(Geometry3D.SegmentIntersectsTriangle(from,to,triangles[i],triangles[i+1],triangles[i+2]).VariantType!=Variant.Type.Nil) return true;
                return false;
            }
            Check(!BodyBlocks(forwardEye,new(-.36f,EyeHeight,-5)),"Forward sightline passes through the windshield");
            Check(!BodyBlocks(sideEye,new(3,EyeHeight,-1.25f)),"90-degree passenger-side sightline passes through the side window");
            Check(!BodyBlocks(backEye,new(backEye.X,backEye.Y,3)),"Leaned rear sightline passes through the rear glass");
            Check(BodyBlocks(forwardEye,new(0,2,-1)),"Roof remains a solid occluder");
            Check(BodyBlocks(forwardEye,forwardEye+(new Vector3(.90f,1.15f,-.96f)-forwardEye)*2),"Broad B pillar remains a solid occluder");
            Check(new[]{-1f,1f}.All(side=>BodyBlocks(forwardEye,forwardEye+(new Vector3(side*.90f,.87f,-2.07f)-forwardEye)*2)),"Both door mirror attachment openings are closed by the body mesh");
            var skin=((ArrayMesh)body.Mesh).SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array().Select(v=>(vehicle.GlobalTransform.AffineInverse()*body.GlobalTransform)*v)
                .Where(v=>v.Y>.55f&&v.Y<.93f&&v.Z>-1.7f&&v.Z<-.65f&&Math.Abs(v.X)>.70f).ToArray();
            Check(skin.Length>0 && skin.All(v=>Math.Abs(Math.Abs(v.X)-.90f)<.015f),"Door skin remains flush instead of sinking inward below the window");
            Check(gearLabel.GetViewport()!=GetViewport() && rearRect.GetViewport()!=GetViewport(),"Instruments and parking feed render on cockpit screens");
            sensorGraphic.SetRanges(new[]{2.0,.25,.8},new[]{.5,1.2,2.0});
            Check(Enumerable.Range(0,6).Select(sensorGraphic.Level).SequenceEqual(new[]{0,4,2,3,1,0}),"Each front/rear sensor has independent stepped visual proximity");
            Check(ParkingSensors.ProximityLevel(.3)==3 && ParkingSensors.ProximityLevel(.6)==2 && ParkingSensors.ProximityLevel(1)==1 && ParkingSensors.ProximityLevel(1.6)==0,"Sensor display thresholds match the documented steps");
            Paused=false;Car.Gear=Gear.R;FrontRange=2;RearRange=.70;beepTime=0;Sound(.01);
            Check(beep.VolumeDb==0 && beepTime>.3 && beepTime<.4 && beep.Stream==sensorPulses[1],"Lower-pitched parking pulses retain the original loudness and cadence");
            RearRange=.20;Sound(.01);var loop=beep.Stream;Sound(.01);
            Check(beep.Stream==loop && ((AudioStreamWav)loop).LoopMode==AudioStreamWav.LoopModeEnum.Forward,"Close-distance warning reuses a smooth continuous tone instead of restarting");
            BeeperVolume=0;Sound(.01);Check(!beep.Playing,"Zero beeper volume silences the warning");
            BeeperVolume=1;Paused=true;Car.Gear=Gear.P;FrontRange=RearRange=2;
            Check(MenuPage=="home" && menuPages["home"].Visible && !menuPages["options"].Visible,"Pause opens a focused home page with options in a submenu");
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.DpadDown,Pressed=true});await PhysicsFrames(3);
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.DpadDown,Pressed=false});
            Check(GetViewport().GuiGetFocusOwner()!=resumeButton,"Controller D-pad navigates pause menu focus");
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.A,Pressed=true});await PhysicsFrames(3);
            Input.ParseInputEvent(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.A,Pressed=false});await PhysicsFrames(3);
            Check(MenuPage=="scenarios" && Paused,"Controller accept opens scenario selection");
            Check(!baySideChoices.Visible,"Parallel parking hides the bay-side controls");
            var currentScene=Scene;var currentPose=Car.Pose;
            exerciseCards[2].EmitSignal(Godot.Button.SignalName.Pressed);difficultyButtons[2].EmitSignal(Godot.Button.SignalName.Pressed);baySideButtons[1].EmitSignal(Godot.Button.SignalName.Pressed);
            Check(Scene==currentScene && Car.Pose==currentPose && !MirrorBay,"Editing scenario choices preserves the current attempt until Start");
            Check(baySideChoices.Visible && parkingDiagrams[1].Mirrored && parkingDiagrams[2].Mirrored && !parkingDiagrams[1].FlipHorizontally && !parkingDiagrams[2].FlipHorizontally,"Right-side choice reflects both bay diagrams");
            Tap(Key.Escape);await PhysicsFrames(3);
            Check(Paused && MenuPage=="home" && Scene==currentScene,"Escape backs out of scenario selection without resuming or replacing the attempt");
            ShowMenuPage("options");ShowMenuPage("controls");Tap(Key.Escape);await PhysicsFrames(3);
            Check(Paused && MenuPage=="options","Controls page goes back to Options");
            ShowMenuPage("map");await PhysicsFrames(3);
            Check(Paused && mapRect.Visible,"Teaching map has a dedicated paused page");
            Check(DrivingModel.Body(Car.Pose).Corners().All(p=>{var v=mapCamera.UnprojectPosition(new((float)p.X,0,(float)p.Z));return v.X>0&&v.X<mapViewport.Size.X&&v.Y>0&&v.Y<mapViewport.Size.Y;}),"Teaching map frames the complete car on the new approach");
            Tap(Key.Escape);await PhysicsFrames(3);Check(MenuPage=="options" && !mapRect.Visible,"Map Back returns to Options and hides the map");
            Tap(Key.Escape);await PhysicsFrames(3);Check(Paused && MenuPage=="home","Options goes back to pause home");
            OpenScenarioSelection();exerciseCards[2].EmitSignal(Godot.Button.SignalName.Pressed);difficultyButtons[2].EmitSignal(Godot.Button.SignalName.Pressed);baySideButtons[1].EmitSignal(Godot.Button.SignalName.Pressed);scenarioStart.EmitSignal(Godot.Button.SignalName.Pressed);await PhysicsFrames(3);
            Check(!Paused && Exercise==Exercise.ForwardBay && Difficulty==Difficulty.Tight && MirrorBay && Scene.Mirrored && Scene.BayWidth>=2.30 && Scene.BayWidth<=2.40,"Start applies parking type, physical difficulty and right-side bay");
            Check(Car.Gear==Gear.P && Car.ParkingBrake && Scene.ApproachEnd>200 && Scene.Path[1].Distance>0,"New selected scenario starts secured before a forward approach");
            Pause(true);
            if(DisplayServer.GetName()!="headless")
            {
                var previousSize=DisplayServer.WindowGetSize();
                ApplyWindowMode(1);await PhysicsFrames(8);Check(DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Fullscreen,"Borderless fullscreen uses the full-display window mode");
                ApplyWindowMode(2);await PhysicsFrames(8);Check(DisplayServer.WindowGetMode()==DisplayServer.WindowMode.ExclusiveFullscreen,"Fullscreen uses exclusive fullscreen mode");
                ApplyWindowMode(0);await PhysicsFrames(8);Check(DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Windowed && !DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.Borderless) && DisplayServer.WindowGetSize()==previousSize,"Windowed mode restores its size and title-bar decorations");
            }
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
            foreach(Difficulty difficulty in Enum.GetValues<Difficulty>())
            foreach(bool mirrored in exercise==Exercise.Parallel?new[]{false}:new[]{false,true})
            {
                MirrorBay=mirrored;Exercise=exercise;Difficulty=difficulty;NewScenario(false);StartDemo();
                Check(Car.Gear==Gear.D && Car.Pose==Scene.Spawn,$"{exercise}/{difficulty}/{(mirrored?"right":"left")} demonstration starts on the approach in D");
                bool sawReverse=false;int safety=0;
                while(Demo && safety++<4000) {RunDemo(.1);sawReverse|=Car.Gear==Gear.R;}
                Check(!Demo && Scene.PositionError(Car.Pose)<.02 && Car.Gear==Gear.P && Car.ParkingBrake,$"{exercise}/{difficulty}/{(mirrored?"right":"left")} demonstration finishes parked and secured");
                if(exercise!=Exercise.ForwardBay) Check(sawReverse,$"{exercise}/{difficulty}/{(mirrored?"right":"left")} demonstration shifts from approach into reverse parking");
            }
            await PhysicsFrames(4);
            GD.Print($"SMOKE COMPLETE: {passed} runtime input/UI/demonstration checks passed.");GetTree().Quit();
        }
        catch(Exception error) {GD.PushError("SMOKE FAILED: "+error);GetTree().Quit(1);}
    }
}
