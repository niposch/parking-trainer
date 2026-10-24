using Godot;
namespace ParkingTrainer;

public partial class ParkingGame : Node3D
{
    internal DrivingModel Car=new();
    internal Scenario Scene=null!;
    Node3D lot=null!,vehicle=null!;
    (MeshInstance3D Mesh,Vector3 Rotation,Vector3 Centre,Vector3 Offset)[] steeringMeshes=[];
    Node3D? driverWheel;
    double displayedSteer=double.NaN;
    internal Camera3D eye=null!;
    internal bool Paused=true,Tutor=true,DynamicGuides=true,RateSteering=true,Muted=false,Demo=false;
    internal int CameraMode=0,Seed=18;
    internal Exercise Exercise=Exercise.Parallel;
    internal float EyeHeight=1.20f,Fov=76,SteeringSensitivity=1,Deadzone=.10f,LookYaw=0,LookPitch=0;
    internal double Gas,Brake=1,FrontRange=2,RearRange=2,Elapsed=0,ContactFlash=0;
    internal double[] FrontSectors=new double[3],RearSectors=new double[3];
    // Lateral zones: front-left, front-right, rear-left, rear-right.
    internal double[] SideSectors=new double[4];
    internal string Notice="";
    double noticeTime,successTime;
    AudioStreamPlayer beep=null!;
    int demoStep; double demoSegmentDistance,demoShiftWait;
    bool autoCapture,smokeMode,captureTaken; string capturePath="",captureView=""; int frames; ulong captureStarted;
    int selectedController=-1;
    ModernController modernController=null!;
    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("Parking Practice · Prototype 01");
        smokeMode=OS.GetCmdlineUserArgs().Contains("--smoke") || OS.GetCmdlineUserArgs().Contains("--phone-check");
        webCheck=IsWeb && OS.GetCmdlineUserArgs().Contains("--web-check");
        smokeMode|=webCheck;
        controllerCheck=OS.GetCmdlineUserArgs().Contains("--controller-check");
        TranslationServer.SetLocale("en");
        System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
        foreach(string arg in OS.GetCmdlineUserArgs())
        {
            if(arg.StartsWith("--capture=")) {capturePath=arg[10..];autoCapture=true;}
            if(arg.StartsWith("--view=")) captureView=arg[7..];
            if(arg=="--mirror") MirrorBay=true;
            if(arg.StartsWith("--exercise=")) Exercise=(Exercise)int.Parse(arg[11..]);
            if(arg.StartsWith("--drill=")) Drill=(Drill)int.Parse(arg[8..]);
            if(arg.StartsWith("--clutter=")) Clutter=(Clutter)int.Parse(arg[10..]);
            if(arg=="--opposite-row") OppositeRow=true;
        }
        var environment=new WorldEnvironment { Environment=new Godot.Environment {
            BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new("9fbbc3"),
            AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.55f,
            TonemapMode=Godot.Environment.ToneMapper.Linear } };
        AddChild(environment);
        AddChild(new DirectionalLight3D { RotationDegrees=new(-52,-35,0),LightColor=new("fff4de"),LightEnergy=.70f,ShadowEnabled=true,DirectionalShadowMaxDistance=55 });
        vehicle=new Node3D { Name="PlayerVehicle" }; AddChild(vehicle);
        WorldFactory.Car(vehicle,"sedan",CarSpec.Width,CarSpec.Length,1.50,true);
        SetupMenuActions();SetupPedalActions();LoadSettings();SetupViews(); SetupHud();
        steeringMeshes=WorldFactory.Meshes(vehicle).Where(m=>m.HasMeta("steering_wheel") && m.GetMeta("steering_wheel").AsBool())
            .Select(m=>(m,m.GetMeta("base_rotation").AsVector3(),m.GetMeta("wheel_centre").AsVector3(),m.GetAabb().GetCenter())).ToArray();
        driverWheel=vehicle.FindChild("DriverWheel",true,false) as Node3D;
        if(!autoCapture&&!smokeMode&&!IsWeb) ApplyWindowMode(DisplayMode,false);
        beep=new AudioStreamPlayer(); AddChild(beep);SetupSensorAudio();
        Input.JoyConnectionChanged+=ControllerChanged;
        foreach(int device in Input.GetConnectedJoypads()) {selectedController=device;padGlyphs=true;break;}
        modernController=new ModernController();
        GD.Print("Controllers available: "+string.Join(", ",Input.GetConnectedJoypads().Select(id=>{var ids=JoyIds(id);return $"{id}:{Input.GetJoyName(id)} (VID {ids.Vendor:x4} PID {ids.Product:x4})";})));
        BriefingEnabled=!(smokeMode||autoCapture)||captureView=="briefing";
        NewScenario(false); UpdateMenu();
        if(webCheck) RunWebCheck();
        else if(OS.GetCmdlineUserArgs().Contains("--phone-check")) RunPhoneCheck();
        else if(OS.GetCmdlineUserArgs().Contains("--smoke")) RunSmoke();
    }
    public override void _ExitTree()
    {
        Input.JoyConnectionChanged-=ControllerChanged;modernController?.Dispose();
        if(IsInstanceValid(beep)) {var stream=beep.Stream;beep.Stop();beep.Stream=null;alarmPlayback?.Dispose();stream?.Dispose();}
    }
    void ControllerChanged(long device,bool connected)
    {
        if(!connected && device==selectedController) {selectedController=-1;Pause(true);Message("Controller disconnected. Reconnect and resume.");}
        else if(connected && selectedController<0) selectedController=(int)device;
        UpdateDeviceStatus();
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut && IsNodeReady() && !autoCapture && !smokeMode) Pause(true);
    }
    internal void NewScenario(bool newSeed)
    {
        touchControls?.ReleaseAll();
        if(newSeed) Seed=Random.Shared.Next(1,999999);
        Scene=Scenario.Create(new ScenarioSettings(Exercise,Difficulty,MirrorBay,Drill,Clutter,OppositeRow),Seed);
        if(lot!=null) {RemoveChild(lot);lot.QueueFree();}
        lot=new Node3D { Name="PracticeLot" };AddChild(lot);WorldFactory.BuildLot(lot,Scene,Mode!=TrainingMode.Test);
        Car=new DrivingModel {Pose=Scene.Spawn,Gear=Gear.P,ParkingBrake=true};
        ResetRun();LastReport=null;Checkpoints.Clear();QueueBriefing();contactBannerTime=ContactFlash=contactShake=0;ContactText="";contactMarker.Hide();
        Gas=0;Brake=0;
        Elapsed=0;successTime=0;Demo=false;Inspection=false;inspectionAcknowledged=false;demoStep=1;demoSegmentDistance=0;demoShiftWait=0;LookYaw=0;LookPitch=0;
        Notice=""; Paused=true;UpdateMouseCapture();
        eye.Position=PlayerCabin.EyeTarget(0,EyeHeight);
        if(autoCapture)
        {
            Paused=captureView is "menu" or "input" or "scenarios" or "options" or "controls" or "howto"; Car.Gear=Gear.R;
            if(captureView=="reverse") CameraMode=0;
            if(captureView.StartsWith("surround")) { CameraMode=1; LookYaw=-.62f;LookPitch=-.40f; }
            if(captureView=="surround-close") Car.Pose=Scene.Goal.Advance(.45,0);
            if(captureView=="console") { CameraMode=0;LookYaw=-.62f;LookPitch=-.40f; }
            if(captureView=="sensors") {CameraMode=0;LookYaw=-.62f;LookPitch=-.40f;Car.Pose=Scene.Goal.Advance(1.35,0);}
            if(captureView=="inspection") {Car.Pose=Scene.Goal;Car.Gear=Gear.P;BeginInspection();}
            if(captureView is "report" or "coach")
            {
                foreach(var step in Scene.Path) Trail.Add((step.Pose,step.Distance<0));
                var g=Scene.Goal;Car.Pose=new(g.X+Math.Cos(g.Yaw)*.18,g.Z-Math.Sin(g.Yaw)*.18,g.Yaw+.03);Car.Travel=5;
                if(captureView=="report") {Car.Gear=Gear.P;BeginInspection();}
                else {Car.Gear=Gear.R;stoppedTime=5;LookYaw=-Mathf.Pi*.8f;LookPitch=-.15f;}
            }
            if(captureView=="left-mirror") { LookYaw=1.05f;LookPitch=-.10f; }
            if(captureView=="shoulder") { LookYaw=-2.25f; LookPitch=-.12f; }
            if(captureView=="right-90") {LookYaw=-Mathf.Pi/2;LookPitch=0;Tutor=false;}
            if(captureView=="right-rear") {LookYaw=-Mathf.Pi*.75f;LookPitch=0;Tutor=false;}
            if(captureView=="door-left") {LookYaw=.65f;LookPitch=-.30f;Tutor=false;}
            if(captureView=="door-right") {LookYaw=-.80f;LookPitch=-.30f;Tutor=false;}
            if(captureView=="back") {LookYaw=-Mathf.Pi;LookPitch=0;Tutor=false;}
            if(captureView.StartsWith("asset")) {Car.Pose=new(8,8,0);Tutor=false;}
            Car.Steer=-.4;
            if(captureView=="approach") {Car.Gear=Gear.P;Car.Steer=0;}
            if(captureView=="touch") {SetTouchMode(1);Car.Gear=Gear.P;Car.Steer=0;}
        }
        ApplyPose(); ShowMenuPage(!seenHowTo&&!TouchEnabled&&!smokeMode&&!autoCapture&&!webCheck?"howto":"home"); UpdateViews();
        if(autoCapture)
        {
            if(captureView=="scenarios") OpenScenarioSelection();
            if(captureView=="options") ShowMenuPage("options");
            if(captureView=="map") {Paused=true;ShowMenuPage("map");}
            if(captureView is "input" or "controls") ShowMenuPage("controls");
            if(captureView=="howto") ShowMenuPage("howto");
            if(captureView=="briefing") ShowBriefing();
        }
        captureStarted=Time.GetTicksMsec();
    }
    internal void Pause(bool pause)
    {
        if(Paused==pause) return;
        Paused=pause; Gas=0; Brake=1; Car.Speed=0; Demo=false;
        if(!pause && briefingPending) ShowBriefing();
        touchControls?.ReleaseAll();RefreshTouchControls();
        UpdateMouseCapture();
        if(IsInstanceValid(menu)) {menu.Visible=pause;menuBackdrop.Visible=pause;if(pause) ShowMenuPage("home");}
        SaveSettings();
    }
    void Message(string message) {Notice=message;noticeTime=4;}
    public override void _UnhandledInput(InputEvent ev)
    {
        if(controllerCheck) return;
        bool key(Key code)=>ev is InputEventKey k && k.Pressed&&!k.Echo&&k.Keycode==code;
        bool pad(JoyButton code)=>ev is InputEventJoypadButton b&&b.Pressed&&b.Device==selectedController&&b.ButtonIndex==code;
        // Controller map: every driving function has exactly one button.
        //   Start menu · A P · LB R · RB D · Y parking brake · X camera screen
        //   B console glance · View over-the-shoulder · R3 look forward · L3 hold: centre wheel
        //   D-pad up: instructions · D-pad down: correction plan
        // Menus: D-pad/left stick, A select, B back. Report: A/B cockpit, D-pad ←/→ scrub, LB/RB zoom.
        if((key(Key.Escape)||pad(JoyButton.Start)||Paused&&pad(JoyButton.B)) && Once("menu",ev))
        {if(Paused) BackOrResume();else Pause(true);GetViewport().SetInputAsHandled();return;}
        if(BriefingVisible && !Paused && (key(Key.Enter)||key(Key.KpEnter)||pad(JoyButton.A)||pad(JoyButton.B))) {DismissBriefing();GetViewport().SetInputAsHandled();return;}
        if(key(Key.F1)) {Pause(true);return;}
        if(key(Key.F5)) {NewScenario(false);return;}
        if(key(Key.F6)) {NewScenario(true);return;}
        if(Paused) return;
        if(Inspection)
        {
            if((key(Key.Enter)||pad(JoyButton.B)||pad(JoyButton.A)) && Once("cockpit",ev)) EndInspection();
            if(key(Key.Left)||pad(JoyButton.DpadLeft)) reportDiagram.Scrub(-1);
            if(key(Key.Right)||pad(JoyButton.DpadRight)) reportDiagram.Scrub(1);
            float zoom=pad(JoyButton.LeftShoulder)?.5f:pad(JoyButton.RightShoulder)?-.5f:ev is InputEventMouseButton {Pressed:true} scroll?scroll.ButtonIndex==MouseButton.WheelUp?-.5f:scroll.ButtonIndex==MouseButton.WheelDown?.5f:0:0;
            orbitDistance=Mathf.Clamp(orbitDistance+zoom,3.5f,10);
            return;
        }
        if((key(Key.H)||key(Key.T)||pad(JoyButton.DpadUp)) && Once("hint",ev)) {ToggleHint();GetViewport().SetInputAsHandled();return;}
        if((key(Key.F)||pad(JoyButton.DpadDown)) && Once("plan",ev)) ToggleFixPlan();
        if((key(Key.Tab)||pad(JoyButton.X)) && Once("camera",ev)) {CameraMode=(CameraMode+1)%2;SaveSettings();}
        if(key(Key.G)) DynamicGuides=!DynamicGuides;
        if((key(Key.Space)||pad(JoyButton.Y)) && Once("parking",ev)) {Car.ParkingBrake=!Car.ParkingBrake;Message(Car.ParkingBrake?"Parking brake on":"Parking brake released");}
        if(key(Key.Key1)||pad(JoyButton.A)) Shift(Gear.P);
        if(key(Key.Key2)||pad(JoyButton.LeftShoulder)) Shift(Gear.R);
        if(key(Key.Key3)) Shift(Gear.N);
        if(key(Key.Key4)||pad(JoyButton.RightShoulder)) Shift(Gear.D);
        // Glances toggle back to the forward view on a second press.
        if((key(Key.C)||pad(JoyButton.Back)) && Once("shoulder",ev)) Glance(Mathf.Pi-.25f,0);
        if((key(Key.V)||pad(JoyButton.B)) && Once("console",ev)) Glance(-.62f,-.40f);
        if(key(Key.Home)||pad(JoyButton.RightStick)) {LookYaw=0;LookPitch=0;}
        if(key(Key.F2)) StartDemo();
    }
    void Glance(float yaw,float pitch)
    {
        bool there=Math.Abs(LookYaw-yaw)<.05f && Math.Abs(LookPitch-pitch)<.05f;
        LookYaw=there?0:yaw;LookPitch=there?0:pitch;
    }
    // Steam Input can deliver one physical press as both a gamepad button and a key;
    // acting on both would toggle twice. Ignore the other device's copy for 0.4 s.
    readonly Dictionary<string,(ulong Time,bool Pad)> lastCommand=new();
    bool Once(string command,InputEvent ev)
    {
        bool fromPad=ev is InputEventJoypadButton;ulong now=Time.GetTicksMsec();
        bool duplicate=lastCommand.TryGetValue(command,out var last) && last.Pad!=fromPad && now-last.Time<400;
        lastCommand[command]=(now,fromPad);
        return !duplicate;
    }
    void Shift(Gear gear)
    {
        ReadPedals();
        if(!Car.Select(gear,Brake)) Message("Stop and hold the brake to change gear.");
        else Message(string.Format(Tr("Selected {0}"),gear));
    }
    float Axis(JoyAxis axis)=>selectedController==ModernController.Device&&modernController.Connected?modernController.Axes[(int)axis]:selectedController>=0?Input.GetJoyAxis(selectedController,axis):0;
    bool PadHeld(JoyButton button)=>selectedController==ModernController.Device&&modernController.Connected?modernController.Buttons[(int)button]:selectedController>=0&&Input.IsJoyButtonPressed(selectedController,button);
    float Dead(float value) =>Math.Abs(value)<Deadzone?0:Math.Sign(value)*(Math.Abs(value)-Deadzone)/(1-Deadzone);
    public override void _PhysicsProcess(double delta)
    {
        float dt=(float)delta;
        if(!smokeMode && !autoCapture) modernController.Poll(delta,GodotReadsGamepad,connected=>
        {
            if(connected) selectedController=ModernController.Device;
            else if(selectedController==ModernController.Device) {selectedController=-1;Pause(true);Message("Controller disconnected. Reconnect and resume.");}
            UpdateDeviceStatus();
        });
        if(noticeTime>0) {noticeTime-=delta;if(noticeTime<=0) Notice="";}
        if(!Paused && !autoCapture)
        {
            if(Inspection) Orbit(Dead(Axis(JoyAxis.RightX))*dt*1.9f,Dead(Axis(JoyAxis.RightY))*dt*1.35f);
            else if(!Demo)
            {
                ReadPedals();
                double steerInput=SteerAxis()+(TouchEnabled?touchControls.Steering:0)+(!NativeDriving&&Input.IsPhysicalKeyPressed(Key.A)?1:0)-(!NativeDriving&&Input.IsPhysicalKeyPressed(Key.D)?1:0);
                steerInput=Math.Clamp(steerInput,-1,1);steerInputShown=(float)steerInput;
                if(RateSteering) Car.Steer=Math.Clamp(Car.Steer+steerInput*1.0*SteeringSensitivity*delta,-CarSpec.MaxSteer,CarSpec.MaxSteer);
                else Car.Steer=Mathf.MoveToward((float)Car.Steer,(float)(steerInput*CarSpec.MaxSteer),dt*SteeringSensitivity*.95f);
                if(Input.IsPhysicalKeyPressed(Key.Z)||PadHeld(JoyButton.LeftStick)) Car.Steer=Mathf.MoveToward((float)Car.Steer,0,dt);
                Car.SetSpeed(delta,Gas,Brake);
                int contactsBefore=Car.Contacts,kerbBefore=Car.KerbTouches;Car.Move(delta,Scene.Obstacles);
                if(Car.Contacts>contactsBefore) OnContact();
                else if(Car.KerbTouches>kerbBefore) {contactTouching=false;Message("Wheels touching the kerb gently. That is fine: stop here.");}
                Elapsed+=delta;
                if(Math.Abs(Car.Speed)<.025 && Scene.Assess(Car.Pose).Success)
                    successTime+=delta;
                else successTime=0;
                CheckInspection();
            }
            if(!Inspection) RecordRun(delta);
            if(!Inspection)
            {
                LookYaw-=Dead(Axis(JoyAxis.RightX))*dt*1.9f;
                LookPitch=Mathf.Clamp(LookPitch-Dead(Axis(JoyAxis.RightY))*dt*1.35f,-.65f,.55f);
            }
        }
        if(!Demo || Paused || autoCapture) {ApplyPose();UpdateViews(delta);}
        for(int i=0;i<3;i++) {FrontSectors[i]=Car.Sensor(Scene.Obstacles,true,i-1);RearSectors[i]=Car.Sensor(Scene.Obstacles,false,i-1);}
        for(int i=0;i<4;i++) SideSectors[i]=Car.SideSensor(Scene.Obstacles,i<2,i%2==0?-1:1);
        FrontRange=Math.Min(FrontSectors.Min(),Math.Min(SideSectors[0],SideSectors[1]));
        RearRange=Math.Min(RearSectors.Min(),Math.Min(SideSectors[2],SideSectors[3]));
        UpdateCoach(delta);UpdateWorldCues(delta);UpdateContactFeedback(delta);UpdateHud(); Sound(delta);
    }
    void ApplyPose()
    {
        vehicle.Position=new((float)Car.Pose.X,0,(float)Car.Pose.Z);vehicle.Rotation=new(0,(float)Car.Pose.Yaw,0);
        if(displayedSteer==Car.Steer) return;
        displayedSteer=Car.Steer;
        // These nodes and offsets are fixed. Avoid walking the entire mesh tree
        // through the WASM/.NET boundary on every 120 Hz physics tick.
        foreach(var wheel in steeringMeshes)
        {
            var rotation=wheel.Rotation;rotation.Y+=(float)Car.Steer;wheel.Mesh.Rotation=rotation;
            wheel.Mesh.Position=wheel.Centre-wheel.Mesh.Basis*wheel.Offset;
        }
        if(driverWheel!=null) driverWheel.Basis=new Basis(Vector3.Right,Mathf.Pi/2-.25f)*new Basis(Vector3.Up,(float)Car.Steer*12);
    }
    internal void StartDemo()
    {
        if(Mode==TrainingMode.Test) {Message("Demonstrations are off in test mode.");if(Paused) Pause(false);return;}
        Car=new DrivingModel{Pose=Scene.Spawn,Gear=Scene.Path.Count>1&&Scene.Path[1].Distance<0?Gear.R:Gear.D,ParkingBrake=false};
        ResetRun();
        demoStep=1;demoSegmentDistance=0;demoShiftWait=0;Demo=true;Inspection=false;inspectionAcknowledged=false;Paused=false;Tutor=true;menu.Hide();
        UpdateMouseCapture();
    }
    public override async void _Process(double delta)
    {
        frames++;
        RefreshHudLayout();
        FillAlarmBuffer();
        UpdateWebFocus();
        UpdateControllerCheck();
        if(Demo && !Paused && !autoCapture) {RunDemo(delta);ApplyPose();UpdateViews(delta);}
        if(autoCapture && captureView=="input") menuScroll.ScrollVertical=10000;
        if(autoCapture && !captureTaken && frames>=100 && Time.GetTicksMsec()-captureStarted>2500)
        {
            captureTaken=true;
            GD.Print($"Layout: window {GetWindow().Size}, viewport {GetViewport().GetVisibleRect().Size}, menu {menu.Size}, minimum {menu.GetCombinedMinimumSize()}");
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            var screenshot=GetViewport().GetTexture().GetImage();
            if(captureView=="surround-close") rearRect.GetViewport().GetTexture().GetImage().SavePng(capturePath.Replace(".png","-console.png"));
            var error=screenshot.SavePng(capturePath);GD.Print($"Capture {capturePath}: {error}; {Engine.GetFramesPerSecond()} FPS");GetTree().Quit();
        }
    }
    void SaveSettings()
    {
        if(autoCapture||smokeMode) return;
        if(!IsWeb && DisplayMode==0 && DisplayServer.GetName()!="headless" && DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Windowed)
        {windowedSize=DisplayServer.WindowGetSize();windowedPosition=DisplayServer.WindowGetPosition();}
        var cfg=new ConfigFile();cfg.SetValue("view","layout_version",2);cfg.SetValue("view","eye_height",EyeHeight);cfg.SetValue("view","fov",Fov);
        cfg.SetValue("input","rate_steering",RateSteering);cfg.SetValue("input","deadzone",Deadzone);
        cfg.SetValue("input","touch_controls",TouchMode);
        cfg.SetValue("preferences","hint_collapsed_v2",HintCollapsed);cfg.SetValue("preferences","tutor",Tutor);cfg.SetValue("preferences","muted",Muted);cfg.SetValue("preferences","beeper_volume",BeeperVolume);
        cfg.SetValue("preferences","dynamic_guides",DynamicGuides);
        if(steerBinding!=null) cfg.SetValue("input","steer_binding",steerBinding);
        cfg.SetValue("preferences","control_hints",ControlHints);cfg.SetValue("preferences","world_cues",WorldCues);cfg.SetValue("preferences","seen_how_to",seenHowTo);
        cfg.SetValue("practice","drill",(int)Drill);cfg.SetValue("practice","clutter",(int)Clutter);cfg.SetValue("practice","opposite_row",OppositeRow);cfg.SetValue("practice","mode",(int)Mode);
        cfg.SetValue("practice","exercise",(int)Exercise);cfg.SetValue("practice","difficulty",(int)Difficulty);cfg.SetValue("practice","mirrored_bay",MirrorBay);
        cfg.SetValue("display","mode",DisplayMode);cfg.SetValue("display","window_size",windowedSize);cfg.SetValue("display","window_position",windowedPosition);
        SavePedalBindings(cfg);
        cfg.Save("user://settings.cfg");
    }
    void LoadSettings()
    {
        if(autoCapture||smokeMode) return;
        var cfg=new ConfigFile();if(cfg.Load("user://settings.cfg")!=Error.Ok) return;
        if((int)cfg.GetValue("view","layout_version",0)!=2) return;
        EyeHeight=(float)cfg.GetValue("view","eye_height",1.20f);Fov=(float)cfg.GetValue("view","fov",76f);
        RateSteering=(bool)cfg.GetValue("input","rate_steering",true);Deadzone=(float)cfg.GetValue("input","deadzone",.10f);
        TouchMode=Math.Clamp((int)cfg.GetValue("input","touch_controls",0),0,2);
        HintCollapsed=(bool)cfg.GetValue("preferences","hint_collapsed_v2",false);
        Tutor=(bool)cfg.GetValue("preferences","tutor",true);Muted=(bool)cfg.GetValue("preferences","muted",false);
        BeeperVolume=Mathf.Clamp((float)cfg.GetValue("preferences","beeper_volume",1f),0,1);
        DynamicGuides=(bool)cfg.GetValue("preferences","dynamic_guides",true);
        Exercise=(Exercise)Math.Clamp((int)cfg.GetValue("practice","exercise",0),0,Enum.GetValues<Exercise>().Length-1);
        Drill=(Drill)Math.Clamp((int)cfg.GetValue("practice","drill",0),0,2);Clutter=(Clutter)Math.Clamp((int)cfg.GetValue("practice","clutter",0),0,2);
        OppositeRow=(bool)cfg.GetValue("practice","opposite_row",false);Mode=(TrainingMode)Math.Clamp((int)cfg.GetValue("practice","mode",0),0,2);
        if(cfg.HasSectionKey("input","steer_binding")) steerBinding=cfg.GetValue("input","steer_binding").AsGodotObject() as InputEventJoypadMotion;
        ControlHints=(bool)cfg.GetValue("preferences","control_hints",true);WorldCues=(bool)cfg.GetValue("preferences","world_cues",true);seenHowTo=(bool)cfg.GetValue("preferences","seen_how_to",false);Difficulty=(Difficulty)Math.Clamp((int)cfg.GetValue("practice","difficulty",1),0,2);
        MirrorBay=(bool)cfg.GetValue("practice","mirrored_bay",false);
        DisplayMode=Math.Clamp((int)cfg.GetValue("display","mode",0),0,2);
        if(IsWeb) DisplayMode=0; // Fullscreen always requires a fresh browser gesture.
        windowedSize=cfg.GetValue("display","window_size",new Vector2I(1280,800)).AsVector2I();
        windowedPosition=cfg.GetValue("display","window_position",new Vector2I(100,80)).AsVector2I();
        LoadPedalBindings(cfg);
    }
}
