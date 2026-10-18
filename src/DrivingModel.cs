namespace ParkingTrainer;

public enum Gear { P, R, N, D }

public readonly record struct Pose(double X, double Z, double Yaw)
{
    public (double X, double Z) Point(double x, double z) =>
        (X + System.Math.Cos(Yaw) * x + System.Math.Sin(Yaw) * z,
         Z - System.Math.Sin(Yaw) * x + System.Math.Cos(Yaw) * z);

    // Exact constant-curvature integration about the rear axle; positive steer is left.
    public Pose Advance(double distance, double steer)
    {
        double k = System.Math.Tan(steer) / CarSpec.Wheelbase;
        if (System.Math.Abs(k) < 1e-8)
            return new(X - distance * System.Math.Sin(Yaw), Z - distance * System.Math.Cos(Yaw), Yaw);
        double next = Yaw + distance * k;
        return new(X + (System.Math.Cos(next) - System.Math.Cos(Yaw)) / k,
                   Z - (System.Math.Sin(next) - System.Math.Sin(Yaw)) / k, next);
    }
}

public static class CarSpec
{
    public const double Wheelbase = 2.65, Width = 1.80, Length = 4.45;
    public const double Rear = 0.90, Front = 3.55, CentreZ = -1.325;
    public const double MaxSteer = 0.60, Creep = 0.92;
}

public readonly record struct Footprint(double X, double Z, double Width, double Length, double Yaw)
{
    public (double X, double Z)[] Corners()
    {
        var p = new Pose(X,Z,Yaw);
        return new[] { p.Point(-Width/2,-Length/2), p.Point(Width/2,-Length/2),
                       p.Point(Width/2,Length/2), p.Point(-Width/2,Length/2) };
    }
    public bool Overlaps(Footprint other, double margin = 0)
    {
        var a = Corners(); var b = other.Corners();
        foreach (double yaw in new[] { Yaw, other.Yaw })
        foreach (var axis in new[] { (X:System.Math.Cos(yaw),Z:-System.Math.Sin(yaw)),
                                    (X:System.Math.Sin(yaw),Z:System.Math.Cos(yaw)) })
        {
            double amin=double.MaxValue,amax=double.MinValue,bmin=double.MaxValue,bmax=double.MinValue;
            foreach(var p in a) { double d=p.X*axis.X+p.Z*axis.Z; amin=System.Math.Min(amin,d); amax=System.Math.Max(amax,d); }
            foreach(var p in b) { double d=p.X*axis.X+p.Z*axis.Z; bmin=System.Math.Min(bmin,d); bmax=System.Math.Max(bmax,d); }
            if(amax+margin <= bmin || bmax+margin <= amin) return false;
        }
        return true;
    }
    public double RayDistance(double x, double z, double dx, double dz, double range)
    {
        double c=System.Math.Cos(Yaw),s=System.Math.Sin(Yaw);
        double ox=c*(x-X)-s*(z-Z), oz=s*(x-X)+c*(z-Z);
        double vx=c*dx-s*dz, vz=s*dx+c*dz;
        double near=0,far=range;
        foreach(var q in new[] { (O:ox,V:vx,H:Width/2), (O:oz,V:vz,H:Length/2) })
        {
            if(System.Math.Abs(q.V)<1e-9) { if(System.Math.Abs(q.O)>q.H) return range; continue; }
            double t1=(-q.H-q.O)/q.V,t2=(q.H-q.O)/q.V;
            near=System.Math.Max(near,System.Math.Min(t1,t2)); far=System.Math.Min(far,System.Math.Max(t1,t2));
            if(near>far) return range;
        }
        return near;
    }
}

public sealed class DrivingModel
{
    public Pose Pose;
    public double Speed, Steer;
    public Gear Gear = Gear.P;
    public bool ParkingBrake = true;
    public int Contacts;
    public double Travel;
    public static Footprint Body(Pose p) { var c=p.Point(0,CarSpec.CentreZ); return new(c.X,c.Z,CarSpec.Width,CarSpec.Length,p.Yaw); }
    public static Footprint Mirror(Pose p, int side) { var c=p.Point(side*.99,-2.05); return new(c.X,c.Z,.20,.26,p.Yaw); }
    public static bool Collides(Pose p, IReadOnlyList<Obstacle> obstacles, double margin=0)
    {
        foreach(var o in obstacles)
            if(Body(p).Overlaps(o.Shape,margin) || Mirror(p,-1).Overlaps(o.Shape,margin) || Mirror(p,1).Overlaps(o.Shape,margin)) return true;
        return false;
    }
    public bool Select(Gear next, double brake)
    {
        if(System.Math.Abs(Speed)>.035 || brake<.25) return false;
        Gear=next; Speed=0; return true;
    }
    public void SetSpeed(double dt,double gas,double brake)
    {
        gas=System.Math.Clamp(gas,0,1); brake=System.Math.Clamp(brake,0,1);
        if(Gear==Gear.P || ParkingBrake) { Speed=0; return; }
        int direction=Gear==Gear.D?1:Gear==Gear.R?-1:0;
        // Idle drive balances resistance near the creep speed; service brake wins at rest.
        double drive=direction==0?0:System.Math.Clamp((CarSpec.Creep-System.Math.Abs(Speed))*1.6,-.28,1.48)+gas*2.8;
        if(brake>.05) drive*=1-brake; // brake override, not a reverse pedal
        double resistance=brake*6.5+(direction==0?.25:0);
        double magnitude=System.Math.Abs(Speed);
        if(magnitude<.002 && drive<=resistance) { Speed=0; return; }
        magnitude=System.Math.Max(0,magnitude+(drive-resistance)*dt);
        Speed=System.Math.Min(magnitude,3.5)*(direction!=0?direction:System.Math.Sign(Speed));
    }
    public bool Move(double dt,IReadOnlyList<Obstacle> obstacles)
    {
        double distance=Speed*dt;
        int steps=System.Math.Max(1,(int)System.Math.Ceiling(System.Math.Abs(distance)/.012));
        for(int i=0;i<steps;i++)
        {
            var next=Pose.Advance(distance/steps,Steer);
            if(Collides(next,obstacles)) { Speed=0; Contacts++; return false; }
            Pose=next; Travel+=System.Math.Abs(distance/steps);
        }
        return true;
    }
    public double Sensor(IReadOnlyList<Obstacle> obstacles,bool front,int sector,double range=2.0)
    {
        var origin=Pose.Point(sector*.64,front?-CarSpec.Front-.025:CarSpec.Rear+.025);
        double nearest=range;
        for(int i=-3;i<=3;i++)
        {
            double angle=Pose.Yaw+(front?0:System.Math.PI)+sector*.20+i*.105;
            foreach(var o in obstacles)
                nearest=System.Math.Min(nearest,o.Shape.RayDistance(origin.X,origin.Z,-System.Math.Sin(angle),-System.Math.Cos(angle),range));
        }
        return nearest;
    }
    public static double AngleError(double a,double b) => System.Math.Abs(System.Math.Atan2(System.Math.Sin(a-b),System.Math.Cos(a-b)));
}
