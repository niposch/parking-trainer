using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    const string GasAction="parking_gas",BrakeAction="parking_brake";
    InputEvent? gasBinding,brakeBinding;
    string bindingAction="",lastInput="";
    Label pedalStatus=null!;
    bool CaptureDrivingMouse=>!Paused&&!autoCapture;

    void UpdateMouseCapture()=>Input.MouseMode=CaptureDrivingMouse?Input.MouseModeEnum.Captured:Input.MouseModeEnum.Visible;
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
        Gas=Input.GetActionStrength(GasAction);
        Brake=Input.GetActionStrength(BrakeAction);
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
            LookYaw-=look.ScreenRelative.X*.0025f;
            LookPitch=Mathf.Clamp(LookPitch-look.ScreenRelative.Y*.0025f,-.65f,.55f);
        }
    }
    void UpdateDeviceStatus()
    {
        if(!IsInstanceValid(deviceLabel)) return;
        var devices=Input.GetConnectedJoypads();
        deviceLabel.Text=devices.Count>0?string.Format(Tr("Gamepads: {0}"),string.Join(", ",devices.Select(id=>$"{id}: {Input.GetJoyName(id)}"))):
            Tr("No gamepad detected. Steam may be emulating keyboard/mouse. Use a Gamepad layout for analog L2/R2; mouse-button pedals are digital.");
        if(lastInput!="") deviceLabel.Text+="\n"+lastInput;
        if(IsInstanceValid(pedalStatus)) pedalStatus.Text=string.Format(Tr("Input test · L2 brake {0:0}% · R2 gas {1:0}%"),Input.GetActionStrength(BrakeAction)*100,Input.GetActionStrength(GasAction)*100);
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
