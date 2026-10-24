using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    const string GasAction="parking_gas",BrakeAction="parking_brake";
    InputEvent? gasBinding,brakeBinding;
    // Generic pads with unusual layouts may report the steering stick on another axis.
    // The binding stores that axis and the sign it reports when pushed left.
    InputEventJoypadMotion? steerBinding;
    const string SteerAction="steer";
    double SteerAxis()=>steerBinding==null?-Dead(Axis(JoyAxis.LeftX)):Dead(Axis(steerBinding.Axis))*Math.Sign(steerBinding.AxisValue);
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
    bool CaptureDrivingMouse=>!Paused&&!autoCapture&&!webCheck&&!TouchEnabled;
    bool NativeDriving=>modernController?.Connected==true && selectedController==ModernController.Device;
    float PedalStrength(string action)
    {
        bool gas=action==GasAction;
        float touch=TouchEnabled&&!Paused&&IsInstanceValid(touchControls)?(gas?touchControls.Gas:touchControls.Brake):0;
        if(NativeDriving && (gas?gasBinding:brakeBinding)==null)
            return Mathf.Max(touch,Mathf.Clamp((modernController.Axes[gas?5:4]-.02f)/.98f,0,1));
        return Mathf.Max(touch,Input.GetActionStrength(action));
    }

    void UpdateMouseCapture()
    {
        var mode=CaptureDrivingMouse?Input.MouseModeEnum.Captured:Input.MouseModeEnum.Visible;
        if(Input.MouseMode!=mode) Input.MouseMode=mode;
    }
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
        ConfigurePedal(GasAction,JoyAxis.TriggerRight,Key.W,MouseButton.Left,gasBinding,!TouchEnabled);
        ConfigurePedal(BrakeAction,JoyAxis.TriggerLeft,Key.S,MouseButton.Right,brakeBinding,!TouchEnabled);
    }
    static void ConfigurePedal(string action,JoyAxis axis,Key key,MouseButton mouse,InputEvent? binding,bool allowMouse)
    {
        if(!InputMap.HasAction(action)) InputMap.AddAction(action,.02f);
        InputMap.ActionEraseEvents(action);
        // Accept Steam's virtual gamepad even if it is not the first enumerated device.
        if(!allowMouse && binding is InputEventMouseButton) binding=null;
        InputMap.ActionAddEvent(action,binding??new InputEventJoypadMotion {Device=-1,Axis=axis,AxisValue=1});
        InputMap.ActionAddEvent(action,new InputEventKey {PhysicalKeycode=key});
        // Steam's keyboard/mouse templates send clicks, not analog trigger axes.
        if(allowMouse) InputMap.ActionAddEvent(action,new InputEventMouseButton {ButtonIndex=mouse});
    }
    void ReadPedals()
    {
        Gas=PedalStrength(GasAction);
        Brake=PedalStrength(BrakeAction);
    }
    void BeginPedalBinding(string action)
    {
        bindingAction=action;
        lastInput=Tr(action==GasAction?"Press R2 / gas fully. Escape cancels.":action==SteerAction?"Push the steering stick fully LEFT. Escape cancels.":"Press L2 / brake fully. Escape cancels.");
        UpdateDeviceStatus();
    }
    void ResetPedalBindings()
    {
        bindingAction="";gasBinding=null;brakeBinding=null;steerBinding=null;SetupPedalActions();SaveSettings();
        lastInput=Tr("Default triggers and steering restored.");UpdateDeviceStatus();
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is InputEventScreenTouch touch && touch.Pressed && !touchSeen)
        {
            touchSeen=true;
            if(TouchMode==0)
            {
                SetTouchMode(0);
                if(!Paused) {touchControls.Pointer(touch.Index,touch.Position,true,touch.Canceled);GetViewport().SetInputAsHandled();return;}
            }
        }
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
            if(bindingAction==SteerAction)
            {
                if(ev is InputEventJoypadMotion stick && Math.Abs(stick.AxisValue)>.65f && stick.Axis is not (JoyAxis.TriggerLeft or JoyAxis.TriggerRight))
                {
                    steerBinding=new InputEventJoypadMotion {Device=-1,Axis=stick.Axis,AxisValue=Math.Sign(stick.AxisValue)};
                    lastInput=string.Format(Tr("Steering bound to axis {0}"),(int)stick.Axis);bindingAction="";SaveSettings();UpdateDeviceStatus();
                    GetViewport().SetInputAsHandled();
                }
                return;
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
            selectedController=ev.Device;padGlyphs=true;lastPadInput=Time.GetTicksMsec();
            lastInput=ev is InputEventJoypadMotion motion?$"Gamepad {ev.Device}: {motion.Axis} {motion.AxisValue:0.00}":$"Gamepad {ev.Device}: {ev.AsText()}";
        }
        else if(ev is InputEventMouseButton {Pressed:true} mouse) {lastInput=$"Mouse button: {mouse.ButtonIndex} (digital)";KeyboardActivity();}
        else if(ev is InputEventKey {Pressed:true,Echo:false} key) {lastInput=$"Keyboard: {key.AsText()} (digital)";KeyboardActivity();}
        if(ev is InputEventMouseMotion look && CaptureDrivingMouse)
        {
            if(Inspection) {Orbit(look.ScreenRelative.X*.005f,look.ScreenRelative.Y*.004f);return;}
            LookYaw-=look.ScreenRelative.X*.0025f;
            LookPitch=Mathf.Clamp(LookPitch-look.ScreenRelative.Y*.0025f,-.65f,.55f);
        }
        if(IsWeb && !Paused && ev is InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Left}) UpdateMouseCapture();
    }
    // Whether Godot's own joypad backend already reads this device. Steam Input's
    // virtual pad and the physical Valve controller have different product IDs, so
    // any Valve device counts as handled once Godot sees one.
    bool GodotReadsGamepad(ushort vendor,ushort product,string name)
    {
        foreach(int device in Input.GetConnectedJoypads())
        {
            var ids=JoyIds(device);
            if(ids.Vendor==vendor && ids.Product==product && vendor!=0) return true;
            if(vendor==0x28de && ids.Vendor==0x28de) return true;
            if(string.Equals(Input.GetJoyName(device).Trim(),name.Trim(),StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
    static (ushort Vendor,ushort Product) JoyIds(int device)
    {
        var info=Input.GetJoyInfo(device);
        if(info.ContainsKey("vendor_id") && info.ContainsKey("product_id")) return ((ushort)info["vendor_id"].AsInt32(),(ushort)info["product_id"].AsInt32());
        // SDL GUIDs carry vendor and product as little-endian words at bytes 4 and 8.
        string guid=Input.GetJoyGuid(device);
        ushort Word(int at)=>guid.Length>=at*2+4 && ushort.TryParse(guid.Substring(at*2+2,2)+guid.Substring(at*2,2),System.Globalization.NumberStyles.HexNumber,null,out var w)?w:(ushort)0;
        return (Word(4),Word(8));
    }
    // Steam Input layouts can send keys or clicks alongside the gamepad, so a key
    // only switches the labels back to keyboard after the controller has been idle.
    ulong lastPadInput;
    void KeyboardActivity() {if(Time.GetTicksMsec()-lastPadInput>2000) padGlyphs=false;}
    void UpdateDeviceStatus()
    {
        if(!IsInstanceValid(deviceLabel)) return;
        var devices=Input.GetConnectedJoypads();
        deviceLabel.Text=modernController?.Connected==true?string.Format(Tr("Controller: {0} · SDL 3.4.16 compatibility driver"),modernController.Name):devices.Count>0?string.Format(Tr("Gamepads: {0}"),string.Join(", ",devices.Select(id=>$"{id}: {Input.GetJoyName(id)}"))):
            Tr(IsWeb?"Press a button on your gamepad to connect. Use a browser-compatible controller for analog L2/R2. Keyboard W/S pedals are also available.":"No gamepad detected. Steam may be emulating keyboard/mouse. Use a Gamepad layout for analog L2/R2; mouse-button pedals are digital.");
        // Browsers report whether they recognised the pad's layout; a non-standard one may need rebinding.
        if(IsWeb && JavaScriptBridge.Eval("Array.from(navigator.getGamepads?navigator.getGamepads():[]).filter(Boolean).map(g=>g.id+' ['+(g.mapping||'non-standard')+' layout]').join('; ')").AsString() is {Length:>0} pads)
            deviceLabel.Text+="\n"+string.Format(Tr("Browser gamepads: {0}. With a non-standard layout, bind the pedals and steering below."),pads);
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
