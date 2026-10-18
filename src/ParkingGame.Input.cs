using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    const string GasAction="parking_gas",BrakeAction="parking_brake";
    InputEvent? gasBinding,brakeBinding;
    string bindingAction="",lastInput="";
    Label pedalStatus=null!;
    bool controllerCheck;
    double controllerCheckTime,maxLeft,maxRight;
    readonly HashSet<int> leftTriggerSteps=new(),rightTriggerSteps=new();
    void UpdateControllerCheck()
    {
        if(!controllerCheck) return;
        if(modernController.Connected)
        {
            double left=modernController.Axes[4],right=modernController.Axes[5];
            maxLeft=Math.Max(maxLeft,left);maxRight=Math.Max(maxRight,right);
            leftTriggerSteps.Add((int)(left*10));rightTriggerSteps.Add((int)(right*10));
        }
        controllerCheckTime+=GetProcessDeltaTime();
        if(controllerCheckTime<35) return;
        GD.Print($"HARDWARE CHECK: {modernController.Name}; L2 max {maxLeft:0.00}, {leftTriggerSteps.Count} steps; R2 max {maxRight:0.00}, {rightTriggerSteps.Count} steps");
        bool passed=modernController.Connected&&maxLeft>.7&&maxRight>.7&&leftTriggerSteps.Count>3&&rightTriggerSteps.Count>3;
        GD.Print(passed?"HARDWARE CHECK PASS: physical analog triggers received":"HARDWARE CHECK INCOMPLETE: both physical triggers need a slow full press");
        GetTree().Quit(passed?0:2);
    }
    bool CaptureDrivingMouse=>!Paused&&!autoCapture;
    bool NativeDriving=>modernController?.Connected==true && selectedController==ModernController.Device;
    float PedalStrength(string action)
    {
        bool gas=action==GasAction;
        if(NativeDriving && (gas?gasBinding:brakeBinding)==null)
            return Mathf.Clamp((modernController.Axes[gas?5:4]-.02f)/.98f,0,1);
        return Input.GetActionStrength(action);
    }

    void UpdateMouseCapture()=>Input.MouseMode=CaptureDrivingMouse?Input.MouseModeEnum.Captured:Input.MouseModeEnum.Visible;
    void SetupMenuActions()
    {
        // The engine's built-in accept action can contain keyboard events only.
        // Bind standard gamepad controls explicitly, including synthetic SDL IDs.
        foreach(var binding in new[]{("ui_accept",JoyButton.A),("ui_cancel",JoyButton.B),("ui_up",JoyButton.DpadUp),("ui_down",JoyButton.DpadDown),("ui_left",JoyButton.DpadLeft),("ui_right",JoyButton.DpadRight)})
        {
            if(!InputMap.HasAction(binding.Item1)) InputMap.AddAction(binding.Item1);
            var ev=new InputEventJoypadButton {Device=-1,ButtonIndex=binding.Item2};
            if(!InputMap.ActionHasEvent(binding.Item1,ev)) InputMap.ActionAddEvent(binding.Item1,ev);
        }
        foreach(var binding in new[]{("ui_left",JoyAxis.LeftX,-1f),("ui_right",JoyAxis.LeftX,1f),("ui_up",JoyAxis.LeftY,-1f),("ui_down",JoyAxis.LeftY,1f)})
            InputMap.ActionAddEvent(binding.Item1,new InputEventJoypadMotion {Device=-1,Axis=binding.Item2,AxisValue=binding.Item3});
    }
    void SetupPedalActions()
    {
        ConfigurePedal(GasAction,JoyAxis.TriggerRight,Key.W,MouseButton.Left,gasBinding);
        ConfigurePedal(BrakeAction,JoyAxis.TriggerLeft,Key.S,MouseButton.Right,brakeBinding);
    }
    static void ConfigurePedal(string action,JoyAxis axis,Key key,MouseButton mouse,InputEvent? binding)
    {
        if(!InputMap.HasAction(action)) InputMap.AddAction(action,.02f);
        InputMap.ActionEraseEvents(action);
        // Accept Steam's virtual gamepad even if it is not the first enumerated device.
        InputMap.ActionAddEvent(action,binding??new InputEventJoypadMotion {Device=-1,Axis=axis,AxisValue=1});
        InputMap.ActionAddEvent(action,new InputEventKey {PhysicalKeycode=key});
        // Steam's keyboard/mouse templates send clicks, not analog trigger axes.
        InputMap.ActionAddEvent(action,new InputEventMouseButton {ButtonIndex=mouse});
    }
    void ReadPedals()
    {
        Gas=PedalStrength(GasAction);
        Brake=PedalStrength(BrakeAction);
    }
    void BeginPedalBinding(string action)
    {
        bindingAction=action;
        lastInput=Tr(action==GasAction?"Press R2 / gas fully. Escape cancels.":"Press L2 / brake fully. Escape cancels.");
        UpdateDeviceStatus();
    }
    void ResetPedalBindings()
    {
        bindingAction="";gasBinding=null;brakeBinding=null;SetupPedalActions();SaveSettings();
        lastInput=Tr("Default triggers restored.");UpdateDeviceStatus();
    }
    public override void _Input(InputEvent ev)
    {
        // The standalone SDL reader has no window, so suppress its UI actions
        // while this Godot window is unfocused. Raw state is still available to diagnostics.
        if(ev is InputEventJoypadButton or InputEventJoypadMotion && !smokeMode && !controllerCheck && !GetWindow().HasFocus())
        {GetViewport().SetInputAsHandled();return;}
        if(bindingAction!="")
        {
            if(ev is InputEventKey {Pressed:true,Keycode:Key.Escape})
            {
                bindingAction="";lastInput=Tr("Binding cancelled.");GetViewport().SetInputAsHandled();return;
            }
            InputEvent? binding=ev switch
            {
                InputEventJoypadMotion m when Math.Abs(m.AxisValue)>.65f=>new InputEventJoypadMotion {Device=-1,Axis=m.Axis,AxisValue=Math.Sign(m.AxisValue)},
                InputEventJoypadButton b when b.Pressed=>new InputEventJoypadButton {Device=-1,ButtonIndex=b.ButtonIndex},
                InputEventKey k when k.Pressed&&!k.Echo=>new InputEventKey {PhysicalKeycode=k.PhysicalKeycode==Key.None?k.Keycode:k.PhysicalKeycode},
                InputEventMouseButton b when b.Pressed=>new InputEventMouseButton {ButtonIndex=b.ButtonIndex},
                _=>null
            };
            if(binding!=null)
            {
                if(bindingAction==GasAction) gasBinding=binding;else brakeBinding=binding;
                lastInput=string.Format(Tr("Bound {0}: {1}"),bindingAction==GasAction?Tr("gas"):Tr("brake"),binding.AsText());
                bindingAction="";SetupPedalActions();SaveSettings();UpdateDeviceStatus();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        bool padActivity=ev is InputEventJoypadButton {Pressed:true} ||
            ev is InputEventJoypadMotion m2 && (m2.Axis is JoyAxis.TriggerLeft or JoyAxis.TriggerRight?m2.AxisValue>.2:Math.Abs(m2.AxisValue)>.35);
        if(padActivity)
        {
            selectedController=ev.Device;
            lastInput=ev is InputEventJoypadMotion motion?$"Gamepad {ev.Device}: {motion.Axis} {motion.AxisValue:0.00}":$"Gamepad {ev.Device}: {ev.AsText()}";
        }
        else if(ev is InputEventMouseButton {Pressed:true} mouse) lastInput=$"Mouse button: {mouse.ButtonIndex} (digital)";
        else if(ev is InputEventKey {Pressed:true,Echo:false} key) lastInput=$"Keyboard: {key.AsText()} (digital)";
        if(ev is InputEventMouseMotion look && CaptureDrivingMouse)
        {
            if(Inspection) {Orbit(look.ScreenRelative.X*.005f,look.ScreenRelative.Y*.004f);return;}
            LookYaw-=look.ScreenRelative.X*.0025f;
            LookPitch=Mathf.Clamp(LookPitch-look.ScreenRelative.Y*.0025f,-.65f,.55f);
        }
    }
    void UpdateDeviceStatus()
    {
        if(!IsInstanceValid(deviceLabel)) return;
        var devices=Input.GetConnectedJoypads();
        deviceLabel.Text=modernController?.Connected==true?string.Format(Tr("Controller: {0} · SDL 3.4.16 compatibility driver"),modernController.Name):devices.Count>0?string.Format(Tr("Gamepads: {0}"),string.Join(", ",devices.Select(id=>$"{id}: {Input.GetJoyName(id)}"))):
            Tr("No gamepad detected. Steam may be emulating keyboard/mouse. Use a Gamepad layout for analog L2/R2; mouse-button pedals are digital.");
        if(lastInput!="") deviceLabel.Text+="\n"+lastInput;
        if(IsInstanceValid(pedalStatus)) pedalStatus.Text=string.Format(Tr("Input test · L2 brake {0:0}% · R2 gas {1:0}%"),PedalStrength(BrakeAction)*100,PedalStrength(GasAction)*100);
    }
    void SavePedalBindings(ConfigFile cfg)
    {
        if(gasBinding!=null) cfg.SetValue("input","gas_binding",gasBinding);
        if(brakeBinding!=null) cfg.SetValue("input","brake_binding",brakeBinding);
    }
    void LoadPedalBindings(ConfigFile cfg)
    {
        if(cfg.HasSectionKey("input","gas_binding")) gasBinding=cfg.GetValue("input","gas_binding").AsGodotObject() as InputEvent;
        if(cfg.HasSectionKey("input","brake_binding")) brakeBinding=cfg.GetValue("input","brake_binding").AsGodotObject() as InputEvent;
        SetupPedalActions();
    }
}
