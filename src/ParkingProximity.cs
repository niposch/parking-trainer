namespace ParkingTrainer;

// Idealized bumper and lateral protection, using the body and mirror collision
// envelope. Overlapping regions prevent gaps at corners and sector handoffs.
// This calculates clearance; it does not simulate ultrasound propagation.
public static class ParkingProximity
{
    public static double Measure(Pose pose,IReadOnlyList<Obstacle> obstacles,bool front,int sector,double range=2)
    {
        if(sector is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(sector));
        if(range<=0) return 0;
        double minX=sector<0?-CarSpec.Width/2:sector>0?.22:-.40;
        double maxX=sector>0?CarSpec.Width/2:sector<0?-.22:.40;
        double minZ=front?-CarSpec.Front:CarSpec.CentreZ-.15;
        double maxZ=front?CarSpec.CentreZ+.15:CarSpec.Rear;
        double nearest=range*range,c=Math.Cos(pose.Yaw),s=Math.Sin(pose.Yaw);
        Span<(double X,double Z)> polygon=stackalloc (double,double)[12];
        Span<(double X,double Z)> scratch=stackalloc (double,double)[12];
        foreach(var obstacle in obstacles)
        {
            if(obstacle.Curb) continue;
            var box=obstacle.Shape;
            double dx=box.X-pose.X,dz=box.Z-pose.Z,cx=c*dx-s*dz,cz=s*dx+c*dz;
            double radius=Math.Sqrt(box.Width*box.Width+box.Length*box.Length)/2;
            if(Math.Abs(cx)>CarSpec.MirrorX+CarSpec.MirrorWidth/2+range+radius || cz < -CarSpec.Front-range-radius || cz > CarSpec.Rear+range+radius) continue;
            double bc=Math.Cos(box.Yaw-pose.Yaw),bs=Math.Sin(box.Yaw-pose.Yaw);
            for(int i=0;i<4;i++)
            {
                double x=(i is 0 or 3?-1:1)*box.Width/2,z=(i<2?-1:1)*box.Length/2;
                polygon[i]=(cx+bc*x+bs*z,cz-bs*x+bc*z);
            }
            int count=4;
            if(sector<=0) {count=Clip(polygon[..count],scratch,0,maxX,true);scratch[..count].CopyTo(polygon);}
            if(sector>=0) {count=Clip(polygon[..count],scratch,0,minX,false);scratch[..count].CopyTo(polygon);}
            if(front && sector!=0 && count>0)
            {
                // A mirror can remain the closest part of the car when the
                // obstacle is just aft of the front/rear body-region boundary.
                double mirror=sector*CarSpec.MirrorX;
                nearest=Math.Min(nearest,DistanceSquared(polygon[..count],mirror-CarSpec.MirrorWidth/2,mirror+CarSpec.MirrorWidth/2,CarSpec.MirrorZ-CarSpec.MirrorLength/2,CarSpec.MirrorZ+CarSpec.MirrorLength/2));
            }
            count=Clip(polygon[..count],scratch,1,front?maxZ:minZ,front);scratch[..count].CopyTo(polygon);
            if(count>0) nearest=Math.Min(nearest,DistanceSquared(polygon[..count],minX,maxX,minZ,maxZ));
            if(nearest<=1e-16) return 0;
        }
        return Math.Sqrt(nearest);
    }
    static int Clip(ReadOnlySpan<(double X,double Z)> input,Span<(double X,double Z)> output,int axis,double boundary,bool less)
    {
        if(input.Length==0) return 0;
        int n=0;var a=input[^1];double av=axis==0?a.X:a.Z;bool ai=less?av<=boundary:av>=boundary;
        foreach(var b in input)
        {
            double bv=axis==0?b.X:b.Z;bool bi=less?bv<=boundary:bv>=boundary;
            if(ai!=bi)
            {
                double t=(boundary-av)/(bv-av);
                output[n++]=(a.X+(b.X-a.X)*t,a.Z+(b.Z-a.Z)*t);
            }
            if(bi) output[n++]=b;
            a=b;av=bv;ai=bi;
        }
        return n;
    }
    static bool Separated(ReadOnlySpan<(double X,double Z)> polygon,double minX,double maxX,double minZ,double maxZ,double x,double z)
    {
        double low=double.MaxValue,high=double.MinValue;
        foreach(var p in polygon) {double q=p.X*x+p.Z*z;low=Math.Min(low,q);high=Math.Max(high,q);}
        double centre=(minX+maxX)*.5*x+(minZ+maxZ)*.5*z;
        double radius=(maxX-minX)*.5*Math.Abs(x)+(maxZ-minZ)*.5*Math.Abs(z);
        return high<centre-radius || low>centre+radius;
    }
    static double DistanceSquared(ReadOnlySpan<(double X,double Z)> polygon,double minX,double maxX,double minZ,double maxZ)
    {
        bool separated=Separated(polygon,minX,maxX,minZ,maxZ,1,0) || Separated(polygon,minX,maxX,minZ,maxZ,0,1);
        for(int i=0;!separated && i<polygon.Length;i++)
        {
            var a=polygon[i];var b=polygon[(i+1)%polygon.Length];
            separated=Separated(polygon,minX,maxX,minZ,maxZ,b.Z-a.Z,a.X-b.X);
        }
        if(!separated) return 0;
        double nearest=double.MaxValue;
        for(int i=0;i<polygon.Length;i++)
        {
            var a=polygon[i];var b=polygon[(i+1)%polygon.Length];
            double dx=Math.Max(minX-a.X,Math.Max(0,a.X-maxX)),dz=Math.Max(minZ-a.Z,Math.Max(0,a.Z-maxZ));
            nearest=Math.Min(nearest,dx*dx+dz*dz);
            for(int corner=0;corner<4;corner++)
            {
                double x=(corner<2?minX:maxX)-a.X,z=(corner%2==0?minZ:maxZ)-a.Z;
                double ex=b.X-a.X,ez=b.Z-a.Z,length=ex*ex+ez*ez;
                double t=length>1e-20?Math.Clamp((x*ex+z*ez)/length,0,1):0;
                double px=x-t*ex,pz=z-t*ez;
                nearest=Math.Min(nearest,px*px+pz*pz);
            }
        }
        return nearest;
    }
}
