namespace ParkingTrainer;

public enum Exercise { Parallel, ReverseBay, ForwardBay }
public sealed record Obstacle(Footprint Shape,string Model,double Height=1.5,bool Curb=false);
public readonly record struct PathStep(Pose Pose,double Steering,double Distance);
public sealed class Scenario
{
    public const int GeneratorVersion=1;
    public Exercise Exercise;
    public int Seed;
    public double BayWidth, Gap;
    public Pose Goal,Spawn;
    public List<Obstacle> Obstacles=new();
    public List<PathStep> Path=new();

    public static Scenario Create(Exercise exercise,int seed)
    {
        var rng=new Random(seed); var scene=new Scenario { Exercise=exercise,Seed=seed };
        double neighborA=4.0+rng.NextDouble()*1.25,neighborB=3.9+rng.NextDouble()*1.2;
        double wA=1.72+rng.NextDouble()*.28,wB=1.72+rng.NextDouble()*.30;
        scene.BayWidth=2.7+rng.NextDouble()*.35;
        // Construct an exit from the goal and reverse it. The collision-checked trace
        // is a witness of feasibility, not merely a check that the final car fits.
        for(int attempt=0;attempt<24;attempt++)
        {
            scene.Obstacles.Clear(); scene.Path.Clear();
            var exit=new List<PathStep>();
            if(exercise==Exercise.Parallel)
            {
                scene.Gap=8.4+rng.NextDouble()*.25+attempt*.25;
                double curb=4.4, x=curb-CarSpec.Width/2-.26;
                scene.Goal=new Pose(x,1.325,0);
                scene.Obstacles.Add(new(new(x,-scene.Gap/2-neighborA/2,wA,neighborA,0),"suv",1.65));
                scene.Obstacles.Add(new(new(x,scene.Gap/2+neighborB/2,wB,neighborB,0),"hatchback-sports",1.48));
                scene.Obstacles.Add(new(new(curb+.4,0,.8,42,0),"curb",.15,true));
                var pose=scene.Goal; double r=CarSpec.Wheelbase/System.Math.Tan(CarSpec.MaxSteer);
                double alpha=System.Math.Acos(1-2.65/(2*r));
                Trace(exit,ref pose,r*alpha,CarSpec.MaxSteer);
                Trace(exit,ref pose,r*alpha,-CarSpec.MaxSteer);
            }
            else
            {
                double rear=exercise==Exercise.ReverseBay?-4.2:-1.55;
                scene.Goal=new Pose(0,rear,exercise==Exercise.ReverseBay?System.Math.PI:0);
                double offset=scene.BayWidth;
                scene.Obstacles.Add(new(new(-offset,-2.95,wA,neighborA,System.Math.PI),"sedan",1.5));
                scene.Obstacles.Add(new(new(offset,-2.95,wB,neighborB,System.Math.PI),"van",1.9));
                scene.Obstacles.Add(new(new(0,-6.3,24,.35,0),"curb",.15,true));
                var pose=scene.Goal;
                double lead=(exercise==Exercise.ReverseBay?4.5:2.2)+attempt*.18;
                Trace(exit,ref pose,exercise==Exercise.ReverseBay?lead:-lead,0);
                double arc=CarSpec.Wheelbase/System.Math.Tan(CarSpec.MaxSteer)*System.Math.PI/2;
                Trace(exit,ref pose,exercise==Exercise.ReverseBay?arc:-arc,CarSpec.MaxSteer);
            }
            if(exit.Any(p=>DrivingModel.Collides(p.Pose,scene.Obstacles,.045))) continue;
            scene.Spawn=exit[^1].Pose;
            scene.Path.Add(new(scene.Spawn,0,0));
            for(int i=exit.Count-1;i>=0;i--)
            {
                Pose previous=i==0?scene.Goal:exit[i-1].Pose;
                scene.Path.Add(new(previous,exit[i].Steering,-exit[i].Distance));
            }
            return scene;
        }
        throw new InvalidOperationException("Unable to construct a collision-free exercise.");
    }
    static void Trace(List<PathStep> path,ref Pose p,double distance,double steer)
    {
        int count=(int)System.Math.Ceiling(System.Math.Abs(distance)/.035);
        for(int i=0;i<count;i++) { p=p.Advance(distance/count,steer); path.Add(new(p,steer,distance/count)); }
    }
    public double PositionError(Pose p)
    {
        var a=p.Point(0,CarSpec.CentreZ); var b=Goal.Point(0,CarSpec.CentreZ);
        return System.Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    }
    public int ClosestPath(Pose pose)
    {
        double best=double.MaxValue; int index=0;
        for(int i=0;i<Path.Count;i++)
        {
            var p=Path[i].Pose;
            double d=(p.X-pose.X)*(p.X-pose.X)+(p.Z-pose.Z)*(p.Z-pose.Z)+2*DrivingModel.AngleError(p.Yaw,pose.Yaw);
            if(d<best) { best=d; index=i; }
        }
        return index;
    }
}
