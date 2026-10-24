using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal bool Inspection;
    bool inspectionAcknowledged;
    float orbitYaw=-.65f,orbitPitch=.65f,orbitDistance=6.5f;
    Camera3D inspectionCamera=null!;
    void BeginInspection()
    {
        Inspection=true;Demo=false;Car.Speed=0;Gas=0;Brake=1;
        orbitYaw=-.65f;orbitPitch=.65f;orbitDistance=6.5f;
        FixPlan=null;ShowFixPlan(null);BuildReport();
        UpdateMouseCapture();
    }
    void EndInspection()
    {
        Inspection=false;inspectionAcknowledged=true;LookYaw=0;LookPitch=0;
    }
    void CheckInspection()
    {
        bool secured=Car.Gear==Gear.P && Car.ParkingBrake && Math.Abs(Car.Speed)<.025;
        if(!secured) inspectionAcknowledged=false;
        if(secured && Car.Travel>.05 && !inspectionAcknowledged) BeginInspection();
    }
    void Orbit(float x,float y)
    {
        orbitYaw-=x;orbitPitch=Mathf.Clamp(orbitPitch+y,.16f,1.35f);
    }
    void UpdateInspectionCamera()
    {
        eye.Current=!Inspection;inspectionCamera.Current=Inspection;
        if(!Inspection) return;
        var centre=new Vector3(0,.55f,(float)CarSpec.CentreZ);
        inspectionCamera.Position=centre+new Vector3(Mathf.Sin(orbitYaw)*Mathf.Cos(orbitPitch),Mathf.Sin(orbitPitch),Mathf.Cos(orbitYaw)*Mathf.Cos(orbitPitch))*orbitDistance;
        inspectionCamera.LookAt(vehicle.ToGlobal(centre),Vector3.Up);
    }
}
