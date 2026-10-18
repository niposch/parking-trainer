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
    internal string Notice="";
    double noticeTime,beepTime,successTime;
    AudioStreamPlayer beep=null!;
    int demoStep; double demoSegmentDistance,demoShiftWait;
    bool autoCapture,smokeMode,captureTaken; string capturePath="",captureView=""; int frames; ulong captureStarted;
    int selectedController=-1;
    ModernController modernController=null!;
    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("Parking Practice · Prototype 01");
        smokeMode=OS.GetCmdlineUserArgs().Contains("--smoke");
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
        foreach(int device in Input.GetConnectedJoypads()) {selectedController=device;break;}
        modernController=new ModernController();
        GD.Print("Controllers available: "+string.Join(", ",Input.GetConnectedJoypads().Select(id=>$"{id}:{Input.GetJoyName(id)}")));
        NewScenario(false); UpdateMenu();
        if(webCheck) RunWebCheck();
        else if(OS.GetCmdlineUserArgs().Contains("--smoke")) RunSmoke();
    }
    public override void _ExitTree() { Input.JoyConnectionChanged-=ControllerChanged;modernController?.Dispose(); }
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
        Scene=Scenario.Create(Exercise,Seed,Difficulty,MirrorBay);
        if(lot!=null) {RemoveChild(lot);lot.QueueFree();}
        lot=new Node3D { Name="PracticeLot" };AddChild(lot);WorldFactory.BuildLot(lot,Scene);
        Car=new DrivingModel {Pose=Scene.Spawn,Gear=Gear.P,ParkingBrake=true};
        Gas=0;Brake=0;
        Elapsed=0;successTime=0;Demo=false;Inspection=false;inspectionAcknowledged=false;demoStep=1;demoSegmentDistance=0;demoShiftWait=0;LookYaw=0;LookPitch=0;
        Notice=""; Paused=true;UpdateMouseCapture();
        eye.Position=PlayerCabin.EyeTarget(0,EyeHeight);
        if(autoCapture)
        {
            Paused=captureView is "menu" or "input" or "scenarios" or "options" or "controls"; Car.Gear=Gear.R;
            if(captureView=="reverse") CameraMode=0;
            if(captureView=="surround") { CameraMode=1; LookYaw=-.62f;LookPitch=-.40f; }
            if(captureView=="console") { CameraMode=0;LookYaw=-.62f;LookPitch=-.40f; }
            if(captureView=="sensors") {CameraMode=0;LookYaw=-.62f;LookPitch=-.40f;Car.Pose=Scene.Goal.Advance(1.35,0);}
            if(captureView=="inspection") {Car.Pose=Scene.Goal;Car.Gear=Gear.P;BeginInspection();}
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
        ApplyPose(); ShowMenuPage("home"); UpdateViews();
        if(autoCapture)
        {
            if(captureView=="scenarios") OpenScenarioSelection();
            if(captureView=="options") ShowMenuPage("options");
            if(captureView=="map") {Paused=true;ShowMenuPage("map");}
            if(captureView is "input" or "controls") ShowMenuPage("controls");
        }
        captureStarted=Time.GetTicksMsec();
    }
    internal void Pause(bool pause)
    {
        if(Paused==pause) return;
        Paused=pause; Gas=0; Brake=1; Car.Speed=0; Demo=false;
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
        if(key(Key.Escape)||pad(JoyButton.Start)||Paused&&pad(JoyButton.B))
        {if(Paused) BackOrResume();else Pause(true);GetViewport().SetInputAsHandled();return;}
        if(key(Key.F5)) {NewScenario(false);return;}
        if(key(Key.F6)) {NewScenario(true);return;}
        if(key(Key.F1)||pad(JoyButton.Back)) {Pause(true);return;}
        if(Paused) return;
        if(Inspection)
        {
            if(key(Key.Enter)||pad(JoyButton.B)) EndInspection();
            if(ev is InputEventMouseButton {Pressed:true} scroll) orbitDistance=Mathf.Clamp(orbitDistance+(scroll.ButtonIndex==MouseButton.WheelUp?-.5f:scroll.ButtonIndex==MouseButton.WheelDown?.5f:0),3.5f,10);
            return;
        }
        if(key(Key.Tab)||pad(JoyButton.Y)) {CameraMode=(CameraMode+1)%2;SaveSettings();}
        if(key(Key.T)||pad(JoyButton.X)) ToggleHint();
        if(key(Key.G)) DynamicGuides=!DynamicGuides;
        if(key(Key.Space)||pad(JoyButton.RightStick)) {Car.ParkingBrake=!Car.ParkingBrake;Message(Car.ParkingBrake?"Parking brake on":"Parking brake released");}
        if(key(Key.Key1)) Shift(Gear.P);
        if(key(Key.Key2)||pad(JoyButton.LeftShoulder)) Shift(Gear.R);
        if(key(Key.Key3)) Shift(Gear.N);
        if(key(Key.Key4)||pad(JoyButton.RightShoulder)) Shift(Gear.D);
        if(pad(JoyButton.A)) Shift((Gear)(((int)Car.Gear+1)%4));
        if(key(Key.C)) {LookYaw=Mathf.Pi-.25f;LookPitch=0;}
        if(key(Key.Home)) {LookYaw=0;LookPitch=0;}
        if(pad(JoyButton.B)||key(Key.V)) {LookYaw=-.62f;LookPitch=-.40f;}
        if(key(Key.F2)) StartDemo();
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
        if(!smokeMode && !autoCapture) modernController.Poll(delta,Input.GetConnectedJoypads().Count==0,connected=>
        {
            if(connected) selectedController=ModernController.Device;
            else if(selectedController==ModernController.Device) {selectedController=-1;Pause(true);Message("Controller disconnected. Reconnect and resume.");}
            UpdateDeviceStatus();
        });
        if(noticeTime>0) {noticeTime-=delta;if(noticeTime<=0) Notice="";}
        ContactFlash=Math.Max(0,ContactFlash-delta);
        if(!Paused && !autoCapture)
        {
            if(Inspection) Orbit(Dead(Axis(JoyAxis.RightX))*dt*1.9f,Dead(Axis(JoyAxis.RightY))*dt*1.35f);
            else if(!Demo)
            {
                ReadPedals();
                double steerInput=-Dead(Axis(JoyAxis.LeftX))+(TouchEnabled?touchControls.Steering:0)+(!NativeDriving&&Input.IsPhysicalKeyPressed(Key.A)?1:0)-(!NativeDriving&&Input.IsPhysicalKeyPressed(Key.D)?1:0);
                steerInput=Math.Clamp(steerInput,-1,1);
                if(RateSteering) Car.Steer=Math.Clamp(Car.Steer+steerInput*1.0*SteeringSensitivity*delta,-CarSpec.MaxSteer,CarSpec.MaxSteer);
                else Car.Steer=Mathf.MoveToward((float)Car.Steer,(float)(steerInput*CarSpec.MaxSteer),dt*SteeringSensitivity*.95f);
                if(Input.IsPhysicalKeyPressed(Key.Z)||PadHeld(JoyButton.LeftStick)) Car.Steer=Mathf.MoveToward((float)Car.Steer,0,dt);
                Car.SetSpeed(delta,Gas,Brake);
                if(!Car.Move(delta,Scene.Obstacles)) {ContactFlash=.6;Message("Contact. Stop, check the clearance, and correct.");}
                Elapsed+=delta;
                if(Scene.PositionError(Car.Pose)<.23 && DrivingModel.AngleError(Car.Pose.Yaw,Scene.Goal.Yaw)<.05 && Math.Abs(Car.Speed)<.025)
                    successTime+=delta;
                else successTime=0;
                CheckInspection();
            }
            if(!Inspection)
            {
                LookYaw-=Dead(Axis(JoyAxis.RightX))*dt*1.9f;
                LookPitch=Mathf.Clamp(LookPitch-Dead(Axis(JoyAxis.RightY))*dt*1.35f,-.65f,.55f);
            }
        }
        if(!Demo || Paused || autoCapture) {ApplyPose();UpdateViews(delta);}
        for(int i=0;i<3;i++) {FrontSectors[i]=Car.Sensor(Scene.Obstacles,true,i-1);RearSectors[i]=Car.Sensor(Scene.Obstacles,false,i-1);}
        FrontRange=FrontSectors.Min();RearRange=RearSectors.Min();
        UpdateHud(); Sound(delta);
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
        Car=new DrivingModel{Pose=Scene.Spawn,Gear=Gear.D,ParkingBrake=false};
        demoStep=1;demoSegmentDistance=0;demoShiftWait=0;Demo=true;Inspection=false;inspectionAcknowledged=false;Paused=false;Tutor=true;menu.Hide();
        UpdateMouseCapture();
    }
    public override async void _Process(double delta)
    {
        frames++;
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
        cfg.SetValue("preferences","hint_collapsed",HintCollapsed);cfg.SetValue("preferences","tutor",Tutor);cfg.SetValue("preferences","muted",Muted);cfg.SetValue("preferences","beeper_volume",BeeperVolume);
        cfg.SetValue("preferences","dynamic_guides",DynamicGuides);
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
        HintCollapsed=(bool)cfg.GetValue("preferences","hint_collapsed",true);
        Tutor=(bool)cfg.GetValue("preferences","tutor",true);Muted=(bool)cfg.GetValue("preferences","muted",false);
        BeeperVolume=Mathf.Clamp((float)cfg.GetValue("preferences","beeper_volume",1f),0,1);
        DynamicGuides=(bool)cfg.GetValue("preferences","dynamic_guides",true);
        Exercise=(Exercise)Math.Clamp((int)cfg.GetValue("practice","exercise",0),0,2);Difficulty=(Difficulty)Math.Clamp((int)cfg.GetValue("practice","difficulty",1),0,2);
        MirrorBay=(bool)cfg.GetValue("practice","mirrored_bay",false);
        DisplayMode=Math.Clamp((int)cfg.GetValue("display","mode",0),0,2);
        if(IsWeb) DisplayMode=0; // Fullscreen always requires a fresh browser gesture.
        windowedSize=cfg.GetValue("display","window_size",new Vector2I(1280,800)).AsVector2I();
        windowedPosition=cfg.GetValue("display","window_position",new Vector2I(100,80)).AsVector2I();
        LoadPedalBindings(cfg);
    }
}
