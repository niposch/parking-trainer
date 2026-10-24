namespace ParkingTrainer;

// Idealized bumper and lateral protection, using the body and mirror collision
// envelope. Each part of an obstacle belongs to exactly one zone by where it
// lies around the car, and is measured to the complete envelope. The zones tile
// the plane, so the nearest zone is the true clearance without gaps, while a
// bumper corner does not repeat an obstacle that is beside the doors.
// This calculates clearance; it does not simulate ultrasound propagation.
public static class ParkingProximity
{
    // Corner zones wrap this far along the body; the centre zone spans this half-width.
    public const double CornerDepth=.50,CentreHalfWidth=.40;
    const double Inf=double.PositiveInfinity;
    // Bumper zones: sector -1 left corner, 0 centre, 1 right corner.
    public static double Measure(Pose pose,IReadOnlyList<Obstacle> obstacles,bool front,int sector,double range=2)
    {
        if(sector is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(sector));
        double minX=sector<0?-Inf:sector>0?CentreHalfWidth:-CentreHalfWidth;
        double maxX=sector<0?-CentreHalfWidth:sector>0?Inf:CentreHalfWidth;
        return front?Clearance(pose,obstacles,minX,maxX,-Inf,-CarSpec.Front+CornerDepth,range)
                    :Clearance(pose,obstacles,minX,maxX,CarSpec.Rear-CornerDepth,Inf,range);
    }
    // Lateral zones between the corners, split into front and rear halves at the body centre.
    public static double MeasureSide(Pose pose,IReadOnlyList<Obstacle> obstacles,bool front,int side,double range=2)
    {
        if(side is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(side));
        return Clearance(pose,obstacles,side<0?-Inf:0,side<0?0:Inf,
            front?-CarSpec.Front+CornerDepth:CarSpec.CentreZ,front?CarSpec.CentreZ:CarSpec.Rear-CornerDepth,range);
    }
    static double Clearance(Pose pose,IReadOnlyList<Obstacle> obstacles,double minX,double maxX,double minZ,double maxZ,double range)
    {
        if(range<=0) return 0;
        double nearest=range*range,c=Math.Cos(pose.Yaw),s=Math.Sin(pose.Yaw);
        Span<(double X,double Z)> polygon=stackalloc (double,double)[12];
        Span<(double X,double Z)> scratch=stackalloc (double,double)[12];
        foreach(var obstacle in obstacles)
        {
            if(!obstacle.Sensed) continue;
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
            count=Keep(polygon,scratch,count,0,minX,false);count=Keep(polygon,scratch,count,0,maxX,true);
            count=Keep(polygon,scratch,count,1,minZ,false);count=Keep(polygon,scratch,count,1,maxZ,true);
            if(count==0) continue;
            var part=polygon[..count];
            nearest=Math.Min(nearest,DistanceSquared(part,-CarSpec.Width/2,CarSpec.Width/2,-CarSpec.Front,CarSpec.Rear));
            nearest=Math.Min(nearest,Math.Min(Mirror(part,-1),Mirror(part,1)));
            if(nearest<=1e-16) return 0;
        }
        return Math.Sqrt(nearest);
    }
    static double Mirror(ReadOnlySpan<(double X,double Z)> polygon,int side)
    {
        double x=side*CarSpec.MirrorX;
        return DistanceSquared(polygon,x-CarSpec.MirrorWidth/2,x+CarSpec.MirrorWidth/2,CarSpec.MirrorZ-CarSpec.MirrorLength/2,CarSpec.MirrorZ+CarSpec.MirrorLength/2);
    }
    static int Keep(Span<(double X,double Z)> polygon,Span<(double X,double Z)> scratch,int count,int axis,double boundary,bool less)
    {
        if(double.IsInfinity(boundary) || count==0) return count;
        count=Clip(polygon[..count],scratch,axis,boundary,less);scratch[..count].CopyTo(polygon);
        return count;
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
