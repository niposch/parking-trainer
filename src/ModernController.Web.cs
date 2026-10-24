#if PARKING_WEB
namespace ParkingTrainer;

// Browsers use Godot's Gamepad API backend. Never link the Windows SDL DLL.
internal sealed class ModernController : IDisposable
{
    public const int Device=15;
    public readonly float[] Axes=new float[6];
    public readonly bool[] Buttons=new bool[15];
    public string Name=>"";
    public ushort Vendor=>0;
    public ushort Product=>0;
    public bool Connected=>false;
    public string Error=>"";
    public void Poll(double dt,Func<ushort,ushort,string,bool> handled,Action<bool> connection) {}
    public void Dispose() {}
}
#endif
