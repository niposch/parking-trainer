using Godot;
namespace ParkingTrainer;

public partial class ParkingGame : Node3D
{
    internal DrivingModel Car=new();
    internal Scenario Scene=null!;
    Node3D lot=null!,vehicle=null!;
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
    int demoStep; double demoCarry;
    bool autoCapture,smokeMode,captureTaken; string capturePath="",captureView=""; int frames; ulong captureStarted;
    int selectedController=-1;
    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("Parking Practice · Prototype 01");
        smokeMode=OS.GetCmdlineUserArgs().Contains("--smoke");
        TranslationServer.SetLocale("en");
        System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("en");
        foreach(string arg in OS.GetCmdlineUserArgs())
        {
            if(arg.StartsWith("--capture=")) {capturePath=arg[10..];autoCapture=true;}
            if(arg.StartsWith("--view=")) captureView=arg[7..];
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
        LoadSettings();SetupViews(); SetupHud();
        beep=new AudioStreamPlayer(); AddChild(beep);
        Input.JoyConnectionChanged+=ControllerChanged;
        foreach(int device in Input.GetConnectedJoypads()) {selectedController=device;break;}
        GD.Print("Controllers available: "+string.Join(", ",Input.GetConnectedJoypads().Select(id=>$"{id}:{Input.GetJoyName(id)}")));
        NewScenario(false); UpdateMenu();
        if(OS.GetCmdlineUserArgs().Contains("--smoke")) RunSmoke();
    }
    public override void _ExitTree() { Input.JoyConnectionChanged-=ControllerChanged; }
    void ControllerChanged(long device,bool connected)
    {
        if(!connected && device==selectedController) {selectedController=-1;Pause(true);Message("Controller disconnected. Reconnect and resume.");}
        else if(connected && selectedController<0) selectedController=(int)device;
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut && IsNodeReady() && !autoCapture) Pause(true);
    }
    internal void NewScenario(bool newSeed)
    {
        if(newSeed) Seed=Random.Shared.Next(1,999999);
        Scene=Scenario.Create(Exercise,Seed);
        if(lot!=null) {RemoveChild(lot);lot.QueueFree();}
        lot=new Node3D { Name="PracticeLot" };AddChild(lot);WorldFactory.BuildLot(lot,Scene);
        Car=new DrivingModel {Pose=Scene.Spawn};
        Elapsed=0;successTime=0;Demo=false;demoStep=1;demoCarry=0;LookYaw=0;LookPitch=0;
        Notice=""; Paused=true;
        if(autoCapture)
        {
            Paused=captureView=="menu"; Car.Gear=Gear.R;
            if(captureView=="reverse") CameraMode=0;
            if(captureView=="surround") { CameraMode=1; LookYaw=-.62f;LookPitch=-.40f; }
            if(captureView=="console") { CameraMode=0;LookYaw=-.62f;LookPitch=-.40f; }
            if(captureView=="left-mirror") { LookYaw=1.05f;LookPitch=-.10f; }
            if(captureView=="shoulder") { LookYaw=-2.25f; LookPitch=-.12f; }
            Car.Steer=-.4;
        }
        ApplyPose(); UpdateMenu(); UpdateViews();
        captureStarted=Time.GetTicksMsec();
    }
    internal void Pause(bool pause)
    {
        if(Paused==pause) return;
        Paused=pause; Gas=0; Brake=1; Car.Speed=0; Demo=false;
        if(IsInstanceValid(menu)) {menu.Visible=pause;if(pause) resumeButton.GrabFocus();}
        if(!pause && Car.Gear==Gear.P) {Car.Gear=Scene.Exercise==Exercise.ForwardBay?Gear.D:Gear.R;Car.ParkingBrake=false;}
        SaveSettings();
    }
    void Message(string message) {Notice=message;noticeTime=4;}
    public override void _UnhandledInput(InputEvent ev)
    {
        bool key(Key code)=>ev is InputEventKey k && k.Pressed&&!k.Echo&&k.Keycode==code;
        bool pad(JoyButton code)=>ev is InputEventJoypadButton b&&b.Pressed&&b.Device==selectedController&&b.ButtonIndex==code;
        if(key(Key.Escape)||pad(JoyButton.Start)) {Pause(!Paused);GetViewport().SetInputAsHandled();return;}
        if(key(Key.F5)) {NewScenario(false);return;}
        if(key(Key.F6)) {NewScenario(true);return;}
        if(key(Key.F1)||pad(JoyButton.Back)) {Pause(true);return;}
        if(Paused) return;
        if(key(Key.Tab)||pad(JoyButton.Y)) {CameraMode=(CameraMode+1)%3;SaveSettings();}
        if(key(Key.T)||pad(JoyButton.X)) {Tutor=!Tutor;SaveSettings();}
        if(key(Key.G)) DynamicGuides=!DynamicGuides;
        if(key(Key.Space)||pad(JoyButton.RightStick)) {Car.ParkingBrake=!Car.ParkingBrake;Message(Car.ParkingBrake?"Parking brake on":"Parking brake released");}
        if(key(Key.Key1)) Shift(Gear.P);
        if(key(Key.Key2)||pad(JoyButton.LeftShoulder)) Shift(Gear.R);
        if(key(Key.Key3)) Shift(Gear.N);
        if(key(Key.Key4)||pad(JoyButton.RightShoulder)) Shift(Gear.D);
        if(pad(JoyButton.A)) Shift((Gear)(((int)Car.Gear+1)%4));
        if(pad(JoyButton.DpadLeft)) {LookYaw=1.05f;LookPitch=0;}
        if(pad(JoyButton.DpadRight)) {LookYaw=-1.05f;LookPitch=0;}
        if(pad(JoyButton.DpadDown)||key(Key.C)) {LookYaw=Mathf.Pi-.25f;LookPitch=0;}
        if(pad(JoyButton.DpadUp)||key(Key.Home)) {LookYaw=0;LookPitch=0;}
        if(pad(JoyButton.B)||key(Key.V)) {LookYaw=-.62f;LookPitch=-.40f;}
        if(key(Key.F2)) StartDemo();
    }
    void Shift(Gear gear)
    {
        if(!Car.Select(gear,Brake)) Message("Stop and hold the brake to change gear.");
        else Message(string.Format(Tr("Selected {0}"),gear));
    }
    float Axis(JoyAxis axis)=>selectedController>=0?Input.GetJoyAxis(selectedController,axis):0;
    float Dead(float value) =>Math.Abs(value)<Deadzone?0:Math.Sign(value)*(Math.Abs(value)-Deadzone)/(1-Deadzone);
    public override void _PhysicsProcess(double delta)
    {
        float dt=(float)delta;
        if(noticeTime>0) {noticeTime-=delta;if(noticeTime<=0) Notice="";}
        ContactFlash=Math.Max(0,ContactFlash-delta);
        if(!Paused && !autoCapture)
        {
            if(Demo) RunDemo(delta);
            else
            {
                Gas=Math.Clamp(Math.Max(Axis(JoyAxis.TriggerRight),Input.IsPhysicalKeyPressed(Key.W)?1:0),0,1);
                Brake=Math.Clamp(Math.Max(Axis(JoyAxis.TriggerLeft),Input.IsPhysicalKeyPressed(Key.S)?1:0),0,1);
                double steerInput=-Dead(Axis(JoyAxis.LeftX))+(Input.IsPhysicalKeyPressed(Key.A)?1:0)-(Input.IsPhysicalKeyPressed(Key.D)?1:0);
                steerInput=Math.Clamp(steerInput,-1,1);
                if(RateSteering) Car.Steer=Math.Clamp(Car.Steer+steerInput*1.0*SteeringSensitivity*delta,-CarSpec.MaxSteer,CarSpec.MaxSteer);
                else Car.Steer=Mathf.MoveToward((float)Car.Steer,(float)(steerInput*CarSpec.MaxSteer),dt*SteeringSensitivity*.95f);
                if(Input.IsPhysicalKeyPressed(Key.Z)||(selectedController>=0&&Input.IsJoyButtonPressed(selectedController,JoyButton.LeftStick))) Car.Steer=Mathf.MoveToward((float)Car.Steer,0,dt);
                Car.SetSpeed(delta,Gas,Brake);
                if(!Car.Move(delta,Scene.Obstacles)) {ContactFlash=.6;Message("Contact. Stop, check the clearance, and correct.");}
                Elapsed+=delta;
                if(Scene.PositionError(Car.Pose)<.23 && DrivingModel.AngleError(Car.Pose.Yaw,Scene.Goal.Yaw)<.05 && Math.Abs(Car.Speed)<.025)
                    successTime+=delta;
                else successTime=0;
            }
            LookYaw-=Dead(Axis(JoyAxis.RightX))*dt*1.9f;
            LookPitch=Mathf.Clamp(LookPitch-Dead(Axis(JoyAxis.RightY))*dt*1.35f,-.65f,.55f);
            if(Input.IsPhysicalKeyPressed(Key.Left)) LookYaw+=dt*1.8f;
            if(Input.IsPhysicalKeyPressed(Key.Right)) LookYaw-=dt*1.8f;
            if(Input.IsPhysicalKeyPressed(Key.Up)) LookPitch=Mathf.Min(.55f,LookPitch+dt);
            if(Input.IsPhysicalKeyPressed(Key.Down)) LookPitch=Mathf.Max(-.65f,LookPitch-dt);
        }
        ApplyPose();
        for(int i=0;i<3;i++) {FrontSectors[i]=Car.Sensor(Scene.Obstacles,true,i-1);RearSectors[i]=Car.Sensor(Scene.Obstacles,false,i-1);}
        FrontRange=FrontSectors.Min();RearRange=RearSectors.Min();
        UpdateViews(); UpdateHud(); Sound(delta);
    }
    void ApplyPose()
    {
        vehicle.Position=new((float)Car.Pose.X,0,(float)Car.Pose.Z);vehicle.Rotation=new(0,(float)Car.Pose.Yaw,0);
        foreach(var m in WorldFactory.Meshes(vehicle))
        {
            if(m.HasMeta("steering_wheel") && m.GetMeta("steering_wheel").AsBool())
            {
                var rotation=m.GetMeta("base_rotation").AsVector3();rotation.Y+=(float)Car.Steer;m.Rotation=rotation;
                m.Position=m.GetMeta("wheel_centre").AsVector3()-m.Basis*m.GetAabb().GetCenter();
            }
        }
        var wheel=vehicle.FindChild("DriverWheel",true,false) as Node3D;
        if(wheel!=null) wheel.Basis=new Basis(Vector3.Right,Mathf.Pi/2-.25f)*new Basis(Vector3.Up,(float)Car.Steer*12);
    }
    internal void StartDemo()
    {
        Car=new DrivingModel{Pose=Scene.Spawn,Gear=Scene.Exercise==Exercise.ForwardBay?Gear.D:Gear.R,ParkingBrake=false};
        demoStep=1;demoCarry=0;Demo=true;Paused=false;Tutor=true;menu.Hide();
    }
    void RunDemo(double dt)
    {
        if(demoStep>=Scene.Path.Count) {Demo=false;Car.Speed=0;Car.ParkingBrake=true;Message("Demonstration complete. Retry to practise it yourself.");return;}
        var step=Scene.Path[demoStep];
        Car.Steer=Mathf.MoveToward((float)Car.Steer,(float)step.Steering,(float)dt*.9f);
        if(Math.Abs(Car.Steer-step.Steering)>.01) {Car.Speed=0;return;}
        Car.Speed=Math.Sign(step.Distance)*.55;
        demoCarry+=dt*.55;
        while(demoStep<Scene.Path.Count && demoCarry>=Math.Abs(Scene.Path[demoStep].Distance))
        {
            step=Scene.Path[demoStep];
            if(Math.Abs(Car.Steer-step.Steering)>.01) {demoCarry=0;break;}
            var next=Car.Pose.Advance(step.Distance,step.Steering);
            if(DrivingModel.Collides(next,Scene.Obstacles)) {Demo=false;Car.Speed=0;Message("Demonstration stopped for clearance.");return;}
            Car.Pose=next;demoCarry-=Math.Abs(step.Distance);demoStep++;
        }
    }
    void Sound(double dt)
    {
        beepTime-=dt;
        double range=Math.Min(FrontRange,RearRange);
        if(Muted||Paused||range>=1.6||Car.Gear==Gear.P) {beep.Stop();return;}
        if(beepTime>0) return;
        bool front=FrontRange<RearRange;
        double duration=range<.30?.22:.075;
        int count=(int)(22050*duration);byte[] data=new byte[count*2];
        for(int i=0;i<count;i++)
        {
            double fade=Math.Min(1,Math.Min(i/80.0,(count-i)/80.0));
            short sample=(short)(Math.Sin(i*2*Math.PI*(front?1100:780)/22050)*4200*fade);
            data[i*2]=(byte)(sample&255);data[i*2+1]=(byte)(sample>>8);
        }
        beep.Stream=new AudioStreamWav { Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=22050,Data=data };
        beep.Play();beepTime=range<.30?.20:.16+.65*Math.Clamp((range-.3)/1.3,0,1);
    }
    public override async void _Process(double delta)
    {
        frames++;
        if(autoCapture && !captureTaken && frames>=100 && Time.GetTicksMsec()-captureStarted>2500)
        {
            captureTaken=true;
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            var screenshot=GetViewport().GetTexture().GetImage();
            var error=screenshot.SavePng(capturePath);GD.Print($"Capture {capturePath}: {error}; {Engine.GetFramesPerSecond()} FPS");GetTree().Quit();
        }
    }
    void SaveSettings()
    {
        if(autoCapture||smokeMode) return;
        var cfg=new ConfigFile();cfg.SetValue("view","layout_version",2);cfg.SetValue("view","eye_height",EyeHeight);cfg.SetValue("view","fov",Fov);
        cfg.SetValue("input","rate_steering",RateSteering);cfg.SetValue("input","deadzone",Deadzone);
        cfg.SetValue("preferences","tutor",Tutor);cfg.SetValue("preferences","muted",Muted);cfg.Save("user://settings.cfg");
    }
    void LoadSettings()
    {
        if(autoCapture||smokeMode) return;
        var cfg=new ConfigFile();if(cfg.Load("user://settings.cfg")!=Error.Ok) return;
        if((int)cfg.GetValue("view","layout_version",0)!=2) return;
        EyeHeight=(float)cfg.GetValue("view","eye_height",1.20f);Fov=(float)cfg.GetValue("view","fov",76f);
        RateSteering=(bool)cfg.GetValue("input","rate_steering",true);Deadzone=(float)cfg.GetValue("input","deadzone",.10f);
        Tutor=(bool)cfg.GetValue("preferences","tutor",true);Muted=(bool)cfg.GetValue("preferences","muted",false);
    }
}
