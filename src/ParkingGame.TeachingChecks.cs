using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    async System.Threading.Tasks.Task<int> CheckTeachingTools()
    {
        int passed=0,previousTouch=TouchMode;
        void Check(bool ok,string name) {if(!ok) throw new InvalidOperationException(name);passed++;GD.Print("TEACH PASS: "+name);}
        SetTouchMode(2);ControlHints=true;Mode=TrainingMode.Guided;Tutor=true;padGlyphs=false;
        Exercise=Exercise.ReverseBay;Difficulty=Difficulty.Standard;MirrorBay=false;Drill=Drill.Park;Clutter=Clutter.None;OppositeRow=false;
        BriefingEnabled=true;lastBriefed="";NewScenario(false);Pause(false);await PhysicsFrames(4);
        Check(BriefingVisible && briefingBody.Text.Contains("Reverse into the marked bay") && briefingBody.Text.Contains("select R and reverse in"),"A new scenario explains its goal and which way to drive first");
        Tap(Key.Enter);await PhysicsFrames(2);
        Check(!BriefingVisible,"Enter dismisses the briefing");
        NewScenario(false);Pause(false);await PhysicsFrames(2);
        Check(!BriefingVisible,"Retrying the same kind of scenario skips the briefing");
        Drill=Drill.DriveOut;NewScenario(false);Pause(false);await PhysicsFrames(2);
        Check(BriefingVisible && briefingBody.Text.Contains("Start by driving forward"),"Drive-out briefing names the first gear");
        DismissBriefing();Drill=Drill.Park;BriefingEnabled=false;
        NewScenario(false);Pause(false);await PhysicsFrames(4);
        Check(Checkpoints.Count>=2 && checkpointMesh.Visible && checkpointLabel.Visible && checkpointLabel.Text.Contains("Stop around here"),"Guided mode shows the drive-up checkpoint in the world");
        Car.Gear=Gear.R;await PhysicsFrames(3);
        Check(trackMesh.Visible && trackMesh.Mesh!=null,"Predicted wheel tracks follow the current gear and steering");
        // Creeping back onto the kerb at the end of the bay is normal parking, not a contact.
        Input.ActionRelease(BrakeAction);Car.Pose=Scene.Goal;Car.Steer=0;Car.ParkingBrake=false;Car.Gear=Gear.R;
        for(int frame=0;frame<600 && Car.KerbTouches==0;frame++) await PhysicsFrames(1);
        await PhysicsFrames(30);
        Check(Car.KerbTouches==1 && Car.Contacts==0 && !contactBanner.Visible && !contactVignette.Visible && Notice.Contains("kerb gently"),"A gentle kerb touch at creeping speed gets a calm note, not a contact");
        // Reversing in fast from the aisle is a real kerb strike.
        Car.Pose=Scene.Goal.Advance(4,0);Car.Speed=0;Input.ActionPress(GasAction,1);
        for(int frame=0;frame<600 && Car.Contacts==0;frame++) await PhysicsFrames(1);
        Input.ActionRelease(GasAction);
        await PhysicsFrames(2);
        GD.Print($"Contact feedback: {Car.Contacts} · {ContactText} · banner {contactBanner.Visible} · edges {contactVignette.Visible}");
        Check(Car.Contacts==1 && contactBanner.Visible && ContactText.Contains("rear") && ContactText.Contains("kerb") && contactVignette.Visible,"A contact shows a banner naming the part and obstacle, with red screen edges");
        await PhysicsFrames(240);
        Check(Car.Contacts==1 && contactBanner.Visible,"Pressing against the kerb stays one contact and keeps the warning up");
        Car.Gear=Gear.P;Car.ParkingBrake=true;
        NewScenario(false);Pause(false);await PhysicsFrames(4);
        Check(controlStrip.Visible && promptLabel.Visible && promptLabel.Text.Contains("Hold S"),"Desktop key bar and first-step prompt appear while secured");
        Input.ActionPress(BrakeAction,1);await PhysicsFrames(4);
        Check(promptLabel.Text.Contains("press 4") && controlStrip.Brake>.9,"Prompt advances and the key bar shows the held brake");
        // Stop 18 cm right of the target: acceptable, but worth a correction.
        var goal=Scene.Goal;
        Car.Pose=goal with {X=goal.X+Math.Cos(goal.Yaw)*.18,Z=goal.Z-Math.Sin(goal.Yaw)*.18};Car.Gear=Gear.R;Car.ParkingBrake=false;Car.Speed=0;Car.Travel=5;
        await PhysicsFrames(120);
        Check(FixPlan!=null && CoachText.Contains("CORRECTION PLAN") && fixMesh.Mesh!=null,"Guided coaching draws and lists a correction for an off-centre stop");
        var p=Car.Pose;bool clear=true;
        foreach(var step in FixPlan!) {p=p.Advance(step.Distance,step.Steering);clear&=!DrivingModel.Collides(p,Scene.Obstacles);}
        Check(clear && Scene.PositionError(p)<1e-6 && DrivingModel.AngleError(p.Yaw,goal.Yaw)<1e-6,"Correction plan is collision-free and ends centred");
        Check(Segments(FixPlan!).Count>=2,"Correction plan has separate pull-out and shift steps");
        Car.Gear=Gear.P;Car.ParkingBrake=true;await PhysicsFrames(4);
        Check(Inspection && reportPanel.Visible && LastReport is {Grade:Grade.Acceptable} && reportBody.Text.Contains("cm right of centre") && reportBody.Text.Contains("Line gaps"),"Securing the car shows a graded report with left/right offsets");
        Check(reportDiagram.Trail.Count>0 || Trail.Count==0,"Report replay receives the recorded trail");
        EndInspection();Input.ActionRelease(BrakeAction);
        Mode=TrainingMode.Hints;NewScenario(false);Pause(false);Input.ActionPress(BrakeAction,1);
        Car.Pose=goal with {X=goal.X+Math.Cos(goal.Yaw)*.18,Z=goal.Z-Math.Sin(goal.Yaw)*.18};Car.Gear=Gear.R;Car.ParkingBrake=false;Car.Travel=5;
        await PhysicsFrames(200);
        Check(FixPlan==null && CoachText.Contains("press F"),"Hints mode offers, but does not force, a correction plan");
        Tap(Key.F);await PhysicsFrames(40);
        Check(FixPlan!=null,"F shows the correction plan on request");
        Input.ActionRelease(BrakeAction);
        Mode=TrainingMode.Test;NewScenario(false);Pause(false);await PhysicsFrames(4);
        Check(!tutorPanel.Visible && !hintIcon.Visible && !landmark.Visible && !promptLabel.Visible && CoachText=="","Test mode removes tutor, landmark, prompts and coaching");
        StartDemo();Check(!Demo,"Test mode has no demonstration");
        Mode=TrainingMode.Guided;
        foreach(Exercise exercise in Enum.GetValues<Exercise>())
        foreach(Drill drill in new[]{Drill.Correct,Drill.DriveOut})
        {
            if(!Scenario.Supports(exercise,drill)) continue;
            Exercise=exercise;Drill=drill;NewScenario(false);
            Check(Scene.Drill==drill && Car.Pose==Scene.Spawn && Scene.Assess(Car.Pose).Grade!=Grade.Good,$"{exercise}/{drill} starts in the space, not yet at the target");
            StartDemo();int safety=0;while(Demo && safety++<4000) RunDemo(.1);
            Check(!Demo && Scene.Assess(Car.Pose).Grade==Grade.Good && Car.Gear==Gear.P,$"{exercise}/{drill} demonstration finishes on the target");
        }
        Drill=Drill.Park;Clutter=Clutter.Busy;OppositeRow=true;
        foreach(Exercise exercise in Enum.GetValues<Exercise>())
        {
            Exercise=exercise;NewScenario(true);
            Check(lot.GetChildCount()>Scene.Obstacles.Count,$"{exercise}: busy layout builds every prop and marking");
        }
        OpenScenarioSelection();
        exerciseCards[(int)Exercise.TurnInRoad].EmitSignal(Godot.Button.SignalName.Pressed);
        Check(!baySideChoices.Visible && !aisleChoices.Visible && drillButtons[1].Disabled,"Turn in the road hides side/aisle choices and disables unsupported drills");
        exerciseCards[(int)Exercise.AngledBay].EmitSignal(Godot.Button.SignalName.Pressed);
        Check(baySideChoices.Visible && aisleChoices.Visible && !drillButtons[2].Disabled && drillButtons[1].Disabled,"Angled bays offer side, aisle and drive-out");
        StartRandomMix();
        Check(!Paused && Scenario.Supports(Exercise,Scene.Drill),"Random mix starts a supported scenario");
        _Input(new InputEventJoypadButton {Device=15,ButtonIndex=JoyButton.LeftShoulder,Pressed=true});
        _Input(new InputEventKey {Keycode=Key.Key2,PhysicalKeycode=Key.Key2,Pressed=true});
        Check(padGlyphs,"A key sent alongside controller input does not flip the labels back to keyboard");
        lastPadInput=0;_Input(new InputEventKey {Keycode=Key.Key2,PhysicalKeycode=Key.Key2,Pressed=true});
        Check(!padGlyphs,"Keyboard labels return once the controller has been idle");
        int previousController=selectedController;selectedController=15;
        void Button(JoyButton button) {_UnhandledInput(new InputEventJoypadButton {Device=15,ButtonIndex=button,Pressed=true});_UnhandledInput(new InputEventJoypadButton {Device=15,ButtonIndex=button,Pressed=false});}
        lastCommand.Clear();NewScenario(false);Pause(false);await PhysicsFrames(2);
        Button(JoyButton.Start);_UnhandledInput(new InputEventKey {Keycode=Key.Escape,Pressed=true});
        Check(Paused,"One press delivered as both a button and Esc opens the menu and keeps it open");
        Pause(false);lastCommand.Clear();bool brakeBefore=Car.ParkingBrake;
        Button(JoyButton.Y);
        Check(Car.ParkingBrake!=brakeBefore,"Y toggles the parking brake");
        _UnhandledInput(new InputEventKey {Keycode=Key.Space,Pressed=true});
        Check(Car.ParkingBrake!=brakeBefore,"A Steam key copy of Y does not toggle the parking brake back");
        Button(JoyButton.RightStick);
        Check(Car.ParkingBrake!=brakeBefore && LookYaw==0 && LookPitch==0,"Right-stick click only looks forward and never touches the parking brake");
        Input.ActionPress(BrakeAction,1);await PhysicsFrames(2);
        Button(JoyButton.RightShoulder);Check(Car.Gear==Gear.D,"RB selects D");
        Button(JoyButton.LeftShoulder);Check(Car.Gear==Gear.R,"LB selects R");
        Button(JoyButton.A);Check(Car.Gear==Gear.P,"A selects P directly instead of cycling");
        Button(JoyButton.A);Check(Car.Gear==Gear.P,"Pressing A again stays in P");
        Input.ActionRelease(BrakeAction);
        int camera=CameraMode;Button(JoyButton.X);Check(CameraMode!=camera,"X switches the camera screen");
        Button(JoyButton.B);Check(Math.Abs(LookYaw+.62f)<.01f,"B glances at the camera screen");
        Button(JoyButton.B);Check(LookYaw==0 && !Paused,"B again returns to the forward view without opening the menu");
        Button(JoyButton.Back);Check(Math.Abs(LookYaw-(Mathf.Pi-.25f))<.01f && !Paused,"View looks over the shoulder instead of opening the menu");
        bool collapsed=HintCollapsed;Button(JoyButton.DpadUp);Check(HintCollapsed!=collapsed,"D-pad up toggles the parking instructions");
        ToggleHint();selectedController=previousController;
        Pause(true);BeginPedalBinding(SteerAction);
        _Input(new InputEventJoypadMotion {Device=15,Axis=JoyAxis.RightX,AxisValue=-.9f});
        Check(steerBinding is {Axis:JoyAxis.RightX,AxisValue:-1} && bindingAction=="","Steering can be bound to another stick axis for unusual controller layouts");
        ResetPedalBindings();Check(steerBinding==null,"Reset restores the default steering axis");
        Check(PadLabels.For("DualSense Wireless Controller")==PadLabels.PlayStation && PadLabels.For("Nintendo Switch Pro Controller")==PadLabels.Nintendo && PadLabels.For("Steam Controller")==PadLabels.Xbox,"Prompts use the button names of the controller family");
        Check(GodotReadsGamepad(0x28de,0x1304,"Steam Controller")==Input.GetConnectedJoypads().Any(d=>JoyIds(d).Vendor==0x28de||Input.GetJoyName(d)=="Steam Controller"),"The compatibility driver only takes a Steam Controller that Godot cannot read");
        ShowMenuPage("howto");
        Check(menuPages["howto"].Visible && howToFirst.HasFocus(),"How-to-drive page is reachable and focused");
        Exercise=Exercise.Parallel;Drill=Drill.Park;Clutter=Clutter.None;OppositeRow=false;Difficulty=Difficulty.Standard;MirrorBay=false;
        SetTouchMode(previousTouch);NewScenario(false);
        return passed;
    }
}
