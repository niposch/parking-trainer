#if !PARKING_WEB
using Godot;
using System.Runtime.InteropServices;
namespace ParkingTrainer;

// Isolated SDL 3.4.16 fallback for hardware newer than Godot 4.7's SDL 3.2.28.
// It emits standard Godot input events, preserving menus and pedal bindings.
internal sealed class ModernController : IDisposable
{
    public const int Device=15;
    const string Library="ParkingSDL3";
    public readonly float[] Axes=new float[6];
    public readonly bool[] Buttons=new bool[15];
    public string Name {get;private set;}="";
    public ushort Vendor {get;private set;}
    public ushort Product {get;private set;}
    public bool Connected=>pad!=IntPtr.Zero;
    public string Error {get;private set;}="";
    IntPtr pad;
    bool initialized;
    double scan;
    readonly HashSet<string> reported=new();
    public ModernController()
    {
        if(!OperatingSystem.IsWindows()) return;
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(ModernController).Assembly,(name,assembly,path)=>name==Library?
                NativeLibrary.Load(ProjectSettings.GlobalizePath("res://native/win-x64/SDL3.dll")):IntPtr.Zero);
            SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS","1");
            SDL_SetHint("SDL_JOYSTICK_HIDAPI_STEAM","1");
            initialized=SDL_Init(0x2000);
            if(initialized) SDL_SetGamepadEventsEnabled(false);
            if(!initialized) Error=Marshal.PtrToStringUTF8(SDL_GetError())??"SDL initialization failed";
            GD.Print($"Controller fallback SDL version {SDL_GetVersion()}, initialized: {initialized}");
        }
        catch(Exception ex) when(ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {Error=ex.Message;GD.PushWarning("Controller fallback unavailable: "+Error);}
    }
    // `handled` says whether Godot already reads a device (vendor, product, name):
    // this driver only takes gamepads Godot cannot see, so nothing is read twice.
    public void Poll(double dt,Func<ushort,ushort,string,bool> handled,Action<bool> connection)
    {
        if(!initialized) return;
        SDL_PumpEvents();
        SDL_UpdateGamepads();
        // Drain events; values are sampled below. Keep this DLL's queue bounded.
        SDL_FlushEvents(0,0xffff);
        if(Connected && (handled(Vendor,Product,Name) || !SDL_GamepadConnected(pad)))
        {
            Release();SDL_CloseGamepad(pad);pad=IntPtr.Zero;Name="";Vendor=Product=0;connection(false);
        }
        scan-=dt;
        if(!Connected && scan<=0)
        {
            scan=1;
            var ids=SDL_GetGamepads(out int count);
            try
            {
                for(int i=0;i<count && !Connected;i++)
                {
                    // Identify devices without opening them, so pads Godot owns are never touched.
                    uint id=(uint)Marshal.ReadInt32(ids,i*4);
                    ushort vendor=SDL_GetGamepadVendorForID(id),product=SDL_GetGamepadProductForID(id);
                    string name=Marshal.PtrToStringUTF8(SDL_GetGamepadNameForID(id))??"Gamepad";
                    if(handled(vendor,product,name))
                    {
                        if(reported.Add($"{vendor:x4}:{product:x4}")) GD.Print($"Controller fallback leaves {name} (VID {vendor:x4} PID {product:x4}) to Godot");
                        continue;
                    }
                    pad=SDL_OpenGamepad(id);Vendor=vendor;Product=product;Name=name;
                }
            }
            finally {SDL_free(ids);}
            if(Connected)
            {
                GD.Print($"Modern controller connected: {Name}; VID {SDL_GetGamepadVendor(pad):x4} PID {SDL_GetGamepadProduct(pad):x4}");
                connection(true);
            }
        }
        if(!Connected) return;
        for(int i=0;i<6;i++)
        {
            short raw=SDL_GetGamepadAxis(pad,i);
            float value=raw/(raw<0?32768f:32767f);
            if(Math.Abs(value-Axes[i])<.001f) continue;
            Axes[i]=value;Input.ParseInputEvent(new InputEventJoypadMotion {Device=Device,Axis=(JoyAxis)i,AxisValue=value});
        }
        for(int i=0;i<15;i++)
        {
            bool value=SDL_GetGamepadButton(pad,i);
            if(value==Buttons[i]) continue;
            Buttons[i]=value;Input.ParseInputEvent(new InputEventJoypadButton {Device=Device,ButtonIndex=(JoyButton)i,Pressed=value});
        }
    }
    void Release()
    {
        for(int i=0;i<6;i++) {Axes[i]=0;Input.ParseInputEvent(new InputEventJoypadMotion {Device=Device,Axis=(JoyAxis)i,AxisValue=0});}
        for(int i=0;i<15;i++) if(Buttons[i]) {Buttons[i]=false;Input.ParseInputEvent(new InputEventJoypadButton {Device=Device,ButtonIndex=(JoyButton)i,Pressed=false});}
    }
    public void Dispose()
    {
        if(pad!=IntPtr.Zero) SDL_CloseGamepad(pad);
        if(initialized) SDL_QuitSubSystem(0x2000);
        pad=IntPtr.Zero;initialized=false;
    }
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern int SDL_GetVersion();
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_Init(uint flags);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_SetHint([MarshalAs(UnmanagedType.LPUTF8Str)] string key,[MarshalAs(UnmanagedType.LPUTF8Str)] string value);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GetError();
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_PumpEvents();
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_UpdateGamepads();
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_SetGamepadEventsEnabled([MarshalAs(UnmanagedType.I1)] bool enabled);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_FlushEvents(uint min,uint max);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GetGamepads(out int count);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_OpenGamepad(uint id);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GetGamepadName(IntPtr gamepad);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern ushort SDL_GetGamepadVendor(IntPtr gamepad);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern ushort SDL_GetGamepadVendorForID(uint id);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern ushort SDL_GetGamepadProductForID(uint id);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_GetGamepadNameForID(uint id);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern ushort SDL_GetGamepadProduct(IntPtr gamepad);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_GamepadConnected(IntPtr gamepad);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern short SDL_GetGamepadAxis(IntPtr gamepad,int axis);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_GetGamepadButton(IntPtr gamepad,int button);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_CloseGamepad(IntPtr gamepad);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_free(IntPtr memory);
    [DllImport(Library,CallingConvention=CallingConvention.Cdecl)] static extern void SDL_QuitSubSystem(uint flags);
}
#endif
