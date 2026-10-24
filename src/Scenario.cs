namespace ParkingTrainer;

public enum Exercise { Parallel, ReverseBay, ForwardBay, AngledBay, Garage, ReverseCorner, TurnInRoad }
public enum Difficulty { Spacious, Standard, Tight }
// Park: approach and park. Correct: start badly parked and fix it. DriveOut: leave the space.
public enum Drill { Park, Correct, DriveOut }
public enum Clutter { None, Some, Busy }
public enum Grade { Good, Acceptable, Repark }
public sealed record Obstacle(Footprint Shape,string Model,double Height=1.5,bool Curb=false)
{
    // Curbs and objects below bumper height are invisible to the parking sensors but still collide.
    public bool Sensed=>!Curb && Height>=.45;
}
public readonly record struct PathStep(Pose Pose,double Steering,double Distance);
public readonly record struct Marking(double X1,double Z1,double X2,double Z2);
// A kerb or wall face used as a reference; the normal points away from it into the open space.
public readonly record struct Kerb(double X,double Z,double NX,double NZ);
public sealed record ScenarioSettings(Exercise Exercise,Difficulty Difficulty=Difficulty.Standard,bool Mirrored=false,Drill Drill=Drill.Park,Clutter Clutter=Clutter.None,bool OppositeRow=false);
// Lateral is positive to the right of the target, depth positive ahead of it (the car's own frame).
// Angle is positive when the car is rotated to its left. Line and kerb gaps are body clearances.
public sealed record Assessment(double Lateral,double Depth,double Angle,double? LeftGap,double? RightGap,double? KerbGap,Grade Grade)
{
    public bool Success=>Grade!=Grade.Repark;
}
public sealed class Scenario
{
    public const int GeneratorVersion=3;
    public ScenarioSettings Settings=new(Exercise.Parallel);
    public Exercise Exercise=>Settings.Exercise;
    public Difficulty Difficulty=>Settings.Difficulty;
    public Drill Drill=>Settings.Drill;
    public bool Mirrored=>Settings.Mirrored;
    public int Seed;
    public Pose SetupPose;
    public int ApproachEnd;
    // Gap: parallel space length. BayWidth: bay, garage, side-road or road width. Aisle: 0 without an opposite row.
    public double BayWidth, Gap, Aisle;
    public Pose Goal,Spawn;
    public List<Obstacle> Obstacles=new();
    public List<PathStep> Path=new();
    public List<Marking> Lines=new();
    public Kerb? Edge;
    public (double X,double Z)? Landmark;
    public List<string> Features=new();
    public double LateralTolerance=.12,DepthMin=-.25,DepthMax=.25,AngleTolerance=.035;
    // A lane target accepts any stopping point along it, so it is drawn as a zone.
    public bool FreeGoal=>DepthMax-DepthMin>2;
    public double Space=>Exercise==Exercise.Parallel?Gap:BayWidth;
    public bool BayLike=>Exercise is Exercise.ReverseBay or Exercise.ForwardBay or Exercise.AngledBay or Exercise.Garage;

    public static (double Min,double Max) SpaceRange(Exercise exercise,Difficulty difficulty)=>exercise switch
    {
        Exercise.Parallel=>difficulty switch {Difficulty.Spacious=>(8.0,8.3),Difficulty.Standard=>(7.0,7.3),_=>(6.45,6.65)},
        Exercise.Garage=>difficulty switch {Difficulty.Spacious=>(3.00,3.20),Difficulty.Standard=>(2.70,2.85),_=>(2.50,2.60)},
        Exercise.ReverseCorner=>difficulty switch {Difficulty.Spacious=>(7.0,7.5),Difficulty.Standard=>(6.0,6.4),_=>(5.2,5.5)},
        Exercise.TurnInRoad=>difficulty switch {Difficulty.Spacious=>(9.0,9.5),Difficulty.Standard=>(7.8,8.2),_=>(7.0,7.3)},
        _=>difficulty switch {Difficulty.Spacious=>(2.90,3.10),Difficulty.Standard=>(2.55,2.70),_=>(2.30,2.40)}
    };
    public static (double Min,double Max) AisleRange(Difficulty difficulty)=>difficulty switch {Difficulty.Spacious=>(7.0,7.5),Difficulty.Standard=>(6.0,6.5),_=>(5.4,5.8)};
    public static bool Supports(Exercise exercise,Drill drill)=>drill switch
    {
        Drill.Park=>true,
        Drill.Correct=>exercise is Exercise.Parallel or Exercise.ReverseBay or Exercise.ForwardBay or Exercise.Garage,
        _=>exercise is Exercise.Parallel or Exercise.ReverseBay or Exercise.ForwardBay or Exercise.AngledBay or Exercise.Garage
    };
    public static bool HasRow(Exercise exercise)=>exercise is Exercise.ReverseBay or Exercise.ForwardBay or Exercise.AngledBay;

    public static Scenario Create(Exercise exercise,int seed,Difficulty difficulty=Difficulty.Standard,bool mirrored=false)=>Create(new(exercise,difficulty,mirrored),seed);
    public static Scenario Create(ScenarioSettings settings,int seed)
    {
        settings=settings with {Drill=Supports(settings.Exercise,settings.Drill)?settings.Drill:Drill.Park,OppositeRow=settings.OppositeRow&&HasRow(settings.Exercise)};
        var rng=new Random(seed);var scene=new Scenario {Settings=settings,Seed=seed};
        double neighborA=4.0+rng.NextDouble()*1.25,neighborB=3.9+rng.NextDouble()*1.2;
        double wA=1.72+rng.NextDouble()*.28,wB=1.72+rng.NextDouble()*.30;
        var range=SpaceRange(settings.Exercise,settings.Difficulty);
        double space=range.Min+rng.NextDouble()*(range.Max-range.Min);
        var aisleRange=AisleRange(settings.Difficulty);
        double aisle=aisleRange.Min+rng.NextDouble()*(aisleRange.Max-aisleRange.Min);
        // Construct an exit from the goal and reverse it. The collision-checked trace
        // is a witness of feasibility, not merely a check that the final car fits.
        // Optional hazards get fresh placements first, then are thinned; the space never grows.
        for(int attempt=0;attempt<96;attempt++)
        {
            var clutter=attempt<48?settings.Clutter:attempt<72?(Clutter)Math.Min(1,(int)settings.Clutter):Clutter.None;
            scene.Reset();
            if(!new Builder(scene,rng,attempt,clutter,space,aisle,neighborA,neighborB,wA,wB).Build()) continue;
            if(settings.Mirrored) scene.Reflect();
            return scene;
        }
        throw new InvalidOperationException("Unable to construct a collision-free exercise.");
    }
    void Reset()
    {
        Obstacles.Clear();Path.Clear();Lines.Clear();Features.Clear();Edge=null;Landmark=null;Aisle=0;ApproachEnd=0;
        LateralTolerance=.12;DepthMin=-.25;DepthMax=.25;AngleTolerance=.035;
    }
    // Reflect the complete validated layout and controls, not the driver's car/cockpit.
    // Steering changes sign under reflection.
    void Reflect()
    {
        static Pose P(Pose p)=>new(-p.X,p.Z,-p.Yaw);
        Spawn=P(Spawn);Goal=P(Goal);SetupPose=P(SetupPose);
        Path=Path.Select(p=>new PathStep(P(p.Pose),-p.Steering,p.Distance)).ToList();
        Obstacles=Obstacles.Select(o=>o with {Shape=o.Shape with {X=-o.Shape.X,Yaw=-o.Shape.Yaw}}).ToList();
        Lines=Lines.Select(l=>new Marking(-l.X1,l.Z1,-l.X2,l.Z2)).ToList();
        if(Edge is {} e) Edge=new Kerb(-e.X,e.Z,-e.NX,e.NZ);
        if(Landmark is {} m) Landmark=(-m.X,m.Z);
    }
    internal static void Trace(List<PathStep> path,ref Pose p,double distance,double steer)
    {
        int count=(int)Math.Ceiling(Math.Abs(distance)/.035);
        for(int i=0;i<count;i++) { p=p.Advance(distance/count,steer); path.Add(new(p,steer,distance/count)); }
    }
    static double Wrap(double angle) {angle%=2*Math.PI;return angle<0?angle+2*Math.PI:angle;}
    static double LateralOf(Pose p,Pose goal)=>(p.X-goal.X)*Math.Cos(goal.Yaw)-(p.Z-goal.Z)*Math.Sin(goal.Yaw);
    static double AheadOf(Pose p,Pose goal)=>-(p.X-goal.X)*Math.Sin(goal.Yaw)-(p.Z-goal.Z)*Math.Cos(goal.Yaw);
    // Two opposite arcs in one gear (full lock by default) that put the rear axle on the
    // goal's centre line and heading. Straight is the remaining signed distance along it.
    public static (List<PathStep> Steps,Pose End,double Straight)? Shift(Pose start,Pose goal,int direction,double lockAngle=CarSpec.MaxSteer)
    {
        double k=Math.Tan(lockAngle)/CarSpec.Wheelbase;
        double turn=Math.Atan2(Math.Sin(goal.Yaw-start.Yaw),Math.Cos(goal.Yaw-start.Yaw));
        (double A,double B,int Sign)? best=null;
        foreach(int sign in new[]{1,-1})
        {
            double c=turn/(direction*sign*k);
            double lo=Math.Max(0,c),hi=lo+1.3/k;
            double Error(double a)=>LateralOf(start.Advance(direction*a,sign*lockAngle).Advance(direction*(a-c),-sign*lockAngle),goal);
            double previous=Error(lo),x0=lo;
            for(int i=1;i<=240;i++)
            {
                double x=lo+(hi-lo)*i/240,value=Error(x);
                if(Math.Abs(previous)<1e-12 || Math.Sign(value)!=Math.Sign(previous))
                {
                    double a=x0,b=x,fa=previous;
                    if(Math.Abs(previous)<1e-12) b=a;
                    for(int j=0;j<80 && b-a>1e-13;j++) {double m=(a+b)/2,fm=Error(m);if(Math.Sign(fm)==Math.Sign(fa)) {a=m;fa=fm;} else b=m;}
                    double root=(a+b)/2;
                    if(best==null || 2*root-c<best.Value.A+best.Value.B) best=(root,root-c,sign);
                    break;
                }
                previous=value;x0=x;
            }
        }
        if(best is not {} s) return null;
        var steps=new List<PathStep>();var p=start;
        Trace(steps,ref p,direction*s.A,s.Sign*lockAngle);
        Trace(steps,ref p,direction*s.B,-s.Sign*lockAngle);
        return (steps,p,-AheadOf(p,goal));
    }
    public Assessment Assess(Pose p)
    {
        var c=p.Point(0,CarSpec.CentreZ);var g=Goal.Point(0,CarSpec.CentreZ);
        double dx=c.X-g.X,dz=c.Z-g.Z,cy=Math.Cos(Goal.Yaw),sy=Math.Sin(Goal.Yaw);
        double lateral=dx*cy-dz*sy,depth=-dx*sy-dz*cy;
        double angle=Math.Atan2(Math.Sin(p.Yaw-Goal.Yaw),Math.Cos(p.Yaw-Goal.Yaw));
        var corners=DrivingModel.Body(p).Corners();
        double? left=null,right=null,kerb=null;
        if(BayLike && Drill!=Drill.DriveOut)
        {
            double min=corners.Min(q=>(q.X-g.X)*cy-(q.Z-g.Z)*sy),max=corners.Max(q=>(q.X-g.X)*cy-(q.Z-g.Z)*sy);
            left=BayWidth/2+min;right=BayWidth/2-max;
        }
        if(Edge is {} e) kerb=corners.Min(q=>(q.X-e.X)*e.NX+(q.Z-e.Z)*e.NZ);
        bool Within(double f)=>Math.Abs(lateral)<=LateralTolerance*f && depth>=DepthMin*f && depth<=DepthMax*f && Math.Abs(angle)<=AngleTolerance*f;
        double lines=Math.Min(left??1,right??1);
        // A body within a few centimetres of a line is effectively on it.
        var grade=Within(1)&&lines>=.08?Grade.Good:Within(1.75)&&lines>=.03?Grade.Acceptable:Grade.Repark;
        return new(lateral,depth,angle,left,right,kerb,grade);
    }
    public double PositionError(Pose p)
    {
        var a=p.Point(0,CarSpec.CentreZ); var b=Goal.Point(0,CarSpec.CentreZ);
        return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
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

    sealed class Builder(Scenario s,Random rng,int attempt,Clutter clutter,double space,double aisle,double nA,double nB,double wA,double wB)
    {
        Exercise E=>s.Exercise;
        bool Some=>clutter>=Clutter.Some;
        bool Busy=>clutter==Clutter.Busy;
        bool Roll(double some,double busy)=>rng.NextDouble()<(Busy?busy:Some?some:0);
        static readonly (string Model,double Height)[] Cars={("sedan",1.5),("van",1.9),("suv",1.65),("hatchback-sports",1.48)};
        static double Radius=>CarSpec.Wheelbase/Math.Tan(CarSpec.MaxSteer);
        bool Collides(Pose p)=>DrivingModel.Collides(p,s.Obstacles,.045);
        void Add(double x,double z,double width,double length,double yaw,string model,double height,bool curb=false)=>s.Obstacles.Add(new(new(x,z,width,length,yaw),model,height,curb));
        void Box(double x0,double z0,double x1,double z1,string model,double height,bool curb=false)=>Add((x0+x1)/2,(z0+z1)/2,Math.Abs(x1-x0),Math.Abs(z1-z0),0,model,height,curb);
        void Line(double x1,double z1,double x2,double z2)=>s.Lines.Add(new(x1,z1,x2,z2));
        void RandomCar(double x,double z,double yaw)
        {
            var car=Cars[rng.Next(Cars.Length)];
            Add(x,z,1.72+rng.NextDouble()*.28,3.9+rng.NextDouble()*1.2,yaw,car.Model,car.Height);
        }
        // Optional hazards never cover the target space itself.
        void Hazard(double x,double z,double width,double length,double yaw,string model,double height,string feature)
        {
            var o=new Obstacle(new(x,z,width,length,yaw),model,height);
            if(DrivingModel.Collides(s.Goal,new[]{o},.10)) return;
            s.Obstacles.Add(o);if(!s.Features.Contains(feature)) s.Features.Add(feature);
        }
        public bool Build()
        {
            switch(E)
            {
                case Exercise.Parallel: ParallelLayout(); break;
                case Exercise.ReverseCorner: CornerLayout(); break;
                case Exercise.TurnInRoad: return TurnInRoad();
                default: RowLayout(); break;
            }
            if(Collides(s.Goal)) return false;
            return s.Drill switch {Drill.Correct=>Correct(),Drill.DriveOut=>DriveOut(),_=>Park()};
        }
        void ParallelLayout()
        {
            s.Gap=space;s.BayWidth=2.5;
            double curb=4.4,x=curb-CarSpec.Width/2-.26;
            s.Goal=new Pose(x,1.325,0);
            s.LateralTolerance=.12;s.DepthMin=-.45;s.DepthMax=.45;s.AngleTolerance=.044;
            double frontX=x,frontYaw=0;
            if(Roll(.35,.5)) {frontX-=.10+rng.NextDouble()*.12;frontYaw=(rng.Next(2)*2-1)*(.03+rng.NextDouble()*.02);s.Features.Add("The car ahead is parked crooked");}
            Add(frontX,-s.Gap/2-nA/2,wA,nA,frontYaw,"suv",1.65);
            Add(x,s.Gap/2+nB/2,wB,nB,0,"hatchback-sports",1.48);
            Add(curb+.4,0,.8,42,0,"curb",.15,true);
            s.Edge=new Kerb(curb,0,-1,0);
            s.Landmark=new Pose(frontX,-s.Gap/2-nA/2,frontYaw).Point(-(wA/2-.09),nA/2+.025);
            if(Roll(.45,.7))
            {
                bool rack=rng.NextDouble()<.5;double length=rack?.45:.38;
                Hazard(frontX,-s.Gap/2+length/2,rack?1.3:.22,length,frontYaw,rack?"rack":"towbar",rack?1.05:.40,
                    rack?"Bike rack on the car ahead":"Tow bar sticks into the gap, below the sensors");
            }
            if(Roll(.5,.8)) Hazard(curb+.28,(rng.NextDouble()-.5)*s.Gap,.12,.12,0,"post",2.5,"Signpost at the edge of the pavement");
            if(Busy)
            {
                Hazard(curb-.38,s.Gap/2+nB+.9+rng.NextDouble(),.6,.6,0,"bin",1.1,"Wheelie bin at the kerb");
                Hazard(curb+.75,-s.Gap/2-1.5,.5,.5,0,"tree",3.2,"Tree on the pavement");
            }
        }
        // 90-degree and angled rows, and the garage, share one target placement.
        void RowLayout()
        {
            double w=space;s.BayWidth=w;
            bool garage=E==Exercise.Garage,angled=E==Exercise.AngledBay;
            double yaw=E switch {Exercise.ReverseBay or Exercise.Garage=>Math.PI,Exercise.AngledBay=>-Math.PI/6,_=>0};
            double cx=0,cz=garage?-3.075:-2.875;
            s.Goal=new Pose(cx-Math.Sin(yaw)*CarSpec.CentreZ,cz-Math.Cos(yaw)*CarSpec.CentreZ,yaw);
            if(angled) s.AngleTolerance=.044;
            // Bay axis from the aisle into the bay, and row spacing along the aisle.
            double bx=angled?Math.Sin(Math.PI/6):0,bz=angled?-Math.Cos(Math.PI/6):-1,pitch=angled?w/Math.Cos(Math.PI/6):w;
            (double X,double Z) Separator(double offset,double z)=>(cx+offset+bx*(z-cz)/bz,z);
            if(garage)
            {
                Box(-w/2-.2,-6.05,-w/2,-.15,"wall",2.4);Box(w/2,-6.05,w/2+.2,-.15,"wall",2.4);
                Box(-w/2-.2,-6.05,w/2+.2,-5.85,"wall",2.4);
                Box(-w/2-3.6,-.35,-w/2-.2,-.15,"wall",2.4);Box(w/2+.2,-.35,w/2+3.6,-.15,"wall",2.4);
                s.Edge=new Kerb(0,-5.85,0,1);s.Landmark=(-w/2,-.15);
                int side=rng.Next(2)*2-1;
                if(Roll(.6,.8)) Hazard(side*(w/2-.15),-4.9,.3,1.0,0,"shelf",1.3,"Shelf on the garage wall");
                if(Busy) Hazard(-side*(w/2-.15),-4.6,.3,1.7,0,"bike",1.0,"Bicycle against the wall");
                return;
            }
            for(int i=-4;i<=3;i++) {var a=Separator((i+.5)*pitch,-.15);var b=Separator((i+.5)*pitch,-5.85);Line(a.X,a.Z,b.X,b.Z);}
            Line(cx-3.5*pitch-3,-5.85,cx+3.5*pitch+3,-5.85);
            Add(0,-6.3,26,.35,0,"curb",.15,true);
            s.Edge=new Kerb(0,-6.125,0,1);
            double carYaw=angled?yaw:Math.PI;
            double crooked=0;int crookedSide=rng.Next(2)*2-1;
            if(Roll(.35,.5)) {crooked=.10+rng.NextDouble()*.12;s.Features.Add("A neighbour is parked over the line");}
            for(int i=-3;i<=3;i++)
            {
                if(i==0) continue;
                double x=cx+i*pitch,z=cz-.075,shift=i==crookedSide?-i*crooked:0,turn=i==crookedSide&&crooked>0?-i*.05:0;
                if(i==-1) {Add(x+shift,z,wA,nA,carYaw+turn,"sedan",1.5);var reference=s.Obstacles[^1].Shape;s.Landmark=new Pose(reference.X,reference.Z,reference.Yaw).Point(-(reference.Width/2-.09),reference.Length/2+.025);}
                else if(i==1) Add(x+shift,z,wB,nB,carYaw+turn,"van",1.9);
                else if(rng.NextDouble()<(Busy?.8:Some||s.Settings.OppositeRow?.5:0)) RandomCar(x,z,rng.NextDouble()<.5?carYaw:carYaw+Math.PI);
            }
            if(s.Settings.OppositeRow)
            {
                s.Aisle=aisle;double z0=-.15+aisle;
                for(int j=-4;j<=3;j++) Line((j+.5)*w,z0,(j+.5)*w,z0+5.7);
                Line(-3.5*w-3,z0+5.7,3.5*w+3,z0+5.7);
                Add(0,z0+6.025,26,.35,0,"curb",.15,true);
                for(int j=-3;j<=3;j++)
                    if(rng.NextDouble()<(j is 0 or 1?.85:.6)) RandomCar(j*w,z0+2.875,rng.NextDouble()<.5?0:Math.PI);
                s.Features.Add("Cars parked across a narrow aisle");
            }
            int pillarSide=rng.Next(2)*2-1;
            if(Roll(.5,.85)) {var p=Separator(pillarSide*pitch/2,-.55);Hazard(p.X,p.Z,.45,.45,0,"pillar",2.6,"Concrete pillar at the bay entrance");}
            if(Roll(.3,.6))
            {
                double along=CarSpec.Length/2+.45,across=(rng.Next(2)*2-1)*(w/2-.2);
                Hazard(cx+bx*along+(-bz)*across,cz+bz*along+bx*across,.2,.2,0,"lowpost",.40,"Low post at the back of the bay, below the sensors");
            }
            if(Roll(.3,.6)) Hazard((rng.Next(2)*2-1)*(1.5+rng.NextDouble())*pitch,.7,.6,.95,rng.NextDouble()*Math.PI,"trolley",1.0,"Shopping trolley left in the aisle");
            if(Busy) Hazard(cx+3.6*pitch,-1,.6,.6,0,"bin",1.1,"Bin at the end of the row");
        }
        // Reversing around a corner into a side road on the right of the approach.
        void CornerLayout()
        {
            double w=space,main=7.0,e=w/2,r=3.0;s.BayWidth=w;
            Box(-34,-main-3,34,-main,"curb",.15,true);
            Box(-34,0,-e-r,34,"curb",.15,true);Box(-e-r,r,-e,34,"curb",.15,true);
            Box(e+r,0,34,34,"curb",.15,true);Box(e,r,e+r,34,"curb",.15,true);
            // Chamfered kerb corners: one rotated slab whose face is the corner's hypotenuse.
            double d=r/Math.Sqrt(2)/2;
            Add(e+r/2+d/Math.Sqrt(2),r/2+d/Math.Sqrt(2),r/Math.Sqrt(2),r*Math.Sqrt(2),-Math.PI/4,"curb",.15,true);
            Add(-e-r/2-d/Math.Sqrt(2),r/2+d/Math.Sqrt(2),r/Math.Sqrt(2),r*Math.Sqrt(2),Math.PI/4,"curb",.15,true);
            for(double x=-30;x<30;x+=4) if(Math.Abs(x+1)>e+2) Line(x,-main/2,x+2,-main/2);
            s.Goal=new Pose(e-CarSpec.Width/2-.3,1.0+CarSpec.Front,0);
            s.Edge=new Kerb(e,0,-1,0);s.Landmark=(e+r/2,r/2);
            s.LateralTolerance=.15;s.DepthMin=-3;s.DepthMax=.4;s.AngleTolerance=.052;
            if(Roll(.5,.8))
            {
                double gap=1.6+rng.NextDouble(),front=s.Goal.Z+CarSpec.Rear+gap,length=3.9+rng.NextDouble()*1.2;
                Add(e-.95,front+length/2,1.8,length,0,"sedan",1.5);
                s.DepthMin=-(gap-.5);s.Features.Add("A car is parked further into the side road");
            }
            if(Roll(.4,.7)) {RandomCar((rng.NextDouble()-.5)*16,-main+1.05,rng.NextDouble()<.5?Math.PI/2:-Math.PI/2);s.Features.Add("Parked car on the far side of the main road");}
            if(Busy) Hazard(e+.7,r+1.2,.6,.6,0,"bin",1.1,"Bin on the corner pavement");
        }
        bool TurnInRoad()
        {
            double w=space;s.BayWidth=w;
            Box(-34,-w/2-3,34,-w/2,"curb",.15,true);Box(-34,w/2,34,w/2+3,"curb",.15,true);
            for(double x=-30;x<30;x+=4) Line(x,0,x+2,0);
            s.Goal=new Pose(0,-w/4,Math.PI/2);
            if(Roll(.5,.8)) Hazard(10+rng.NextDouble()*4,-w/2+1.0,1.85,4.4,Math.PI/2,"sedan",1.5,"Parked cars along the kerb");
            if(Roll(.3,.7)) Hazard(16+rng.NextDouble()*3,w/2-1.0,1.85,4.6,-Math.PI/2,"van",1.9,"Parked cars along the kerb");
            if(Busy) Hazard(4+rng.NextDouble()*4,-w/2-.35,.6,.6,0,"bin",1.1,"Bin at the kerb edge");
            var spawn=new Pose(-17,w/4+(rng.NextDouble()-.5)*.4,-Math.PI/2+(rng.NextDouble()-.5)*.06);
            var path=new List<PathStep>();var p=spawn;
            Trace(path,ref p,5+rng.NextDouble()*2,0);
            double bend=.05+rng.NextDouble()*.03;
            Trace(path,ref p,3,bend);Trace(path,ref p,3,-bend);Trace(path,ref p,2+attempt*.25,0);
            int approach=path.Count;s.SetupPose=p;
            // After the multi-point turn, settle into the opposite lane with a gentle lane change.
            if(!TurnTo(path,ref p,Math.PI/2,1,1,9,.12)) return false;
            if(Math.Abs(p.Z+w/4)>.3)
            {
                if(Shift(p,new Pose(p.X-50,-w/4,Math.PI/2),1,.12) is not {} lane) return false;
                path.AddRange(lane.Steps);p=lane.End;
            }
            Trace(path,ref p,3,0);
            if(path.Any(q=>Collides(q.Pose))) return false;
            s.Spawn=spawn;s.ApproachEnd=approach;s.Path.Add(new(spawn,0,0));s.Path.AddRange(path);
            s.Goal=p;s.Edge=new Kerb(0,-w/2,0,1);
            s.LateralTolerance=Math.Max(.3,(w/2-CarSpec.Width)/2);s.DepthMin=-30;s.DepthMax=30;s.AngleTolerance=.087;
            return true;
        }
        // Full-lock turn to a heading. Whenever the next step would come within `margin`
        // of an obstacle, change gear and swap lock, as in a multi-point turn.
        bool TurnTo(List<PathStep> path,ref Pose p,double target,int steerSign,int direction,int maxMoves,double margin=.06)
        {
            double k=Math.Tan(CarSpec.MaxSteer)/CarSpec.Wheelbase;
            int rate=direction*steerSign;
            for(int move=0;move<maxMoves;move++,direction=-direction)
            {
                double steer=steerSign*CarSpec.MaxSteer*(move%2==0?1:-1);
                int steps=0;
                while(true)
                {
                    double remaining=Wrap(rate*(target-p.Yaw));
                    if(remaining<1e-9 || remaining>2*Math.PI-1e-9) return true;
                    double d=Math.Min(.035,remaining/k);
                    var next=p.Advance(direction*d,steer);
                    if(DrivingModel.Collides(next,s.Obstacles,margin)) break;
                    p=next;path.Add(new(p,steer,direction*d));steps++;
                }
                if(steps==0 && move>0) return false;
            }
            return false;
        }
        // Forward from the parked goal to a pose heading along the aisle or road.
        List<PathStep>? Exit()
        {
            var exit=new List<PathStep>();var pose=s.Goal;
            int cycle=attempt%24;bool row=s.Aisle>0;
            switch(E)
            {
                case Exercise.Parallel:
                {
                    // Tighter spaces need the goal-end correction that a one-sweep
                    // demonstration omits. Keep the selected gap fixed.
                    double r=Radius,spare=(s.Gap-CarSpec.Length)/2-.08,back=spare*Math.Min(1,cycle/12.0);
                    Trace(exit,ref pose,-back,0);
                    double alpha=Math.Acos(1-2.35/(2*r));
                    Trace(exit,ref pose,r*alpha,CarSpec.MaxSteer);
                    Trace(exit,ref pose,r*alpha,-CarSpec.MaxSteer);
                    return exit;
                }
                case Exercise.ReverseBay: case Exercise.Garage:
                    Trace(exit,ref pose,(row?2.6:E==Exercise.Garage?5.0:4.5)+cycle*.18,0);
                    return TurnTo(exit,ref pose,1.5*Math.PI,1,1,row?7:2)?exit:null;
                case Exercise.ForwardBay:
                    Trace(exit,ref pose,-((row?1.5:2.2)+cycle*.18),0);
                    return TurnTo(exit,ref pose,-Math.PI/2,1,-1,row?7:2)?exit:null;
                case Exercise.AngledBay:
                    Trace(exit,ref pose,-((row?1.2:1.8)+cycle*.18),0);
                    return TurnTo(exit,ref pose,-Math.PI/2,1,-1,row?7:2)?exit:null;
                default:
                    Trace(exit,ref pose,1.0+cycle*.2,0);
                    return TurnTo(exit,ref pose,-Math.PI/2,-1,1,2)?exit:null;
            }
        }
        bool Park()
        {
            var exit=Exit();
            if(exit==null || exit.Any(p=>Collides(p.Pose))) return false;
            int maneuverSteps=exit.Count;
            s.SetupPose=exit[^1].Pose;
            var approach=s.SetupPose;
            // Append a backwards exit along the aisle, then reverse the complete
            // trace. Players start before the space and must drive up in D.
            Trace(exit,ref approach,-(8+rng.NextDouble()*3),0);
            double bend=(E is Exercise.Parallel or Exercise.ReverseCorner?1:-1)*(s.Aisle>0?.035+rng.NextDouble()*.02:.07+rng.NextDouble()*.04);
            Trace(exit,ref approach,-3.2,bend);
            Trace(exit,ref approach,-3.2,-bend);
            Trace(exit,ref approach,-1.8,(rng.NextDouble()-.5)*.14);
            if(exit.Skip(maneuverSteps).Any(p=>Collides(p.Pose))) return false;
            s.ApproachEnd=exit.Count-maneuverSteps;
            s.Spawn=exit[^1].Pose;
            s.Path.Add(new(s.Spawn,0,0));
            for(int i=exit.Count-1;i>=0;i--)
            {
                Pose previous=i==0?s.Goal:exit[i-1].Pose;
                s.Path.Add(new(previous,exit[i].Steering,-exit[i].Distance));
            }
            return true;
        }
        bool DriveOut()
        {
            var exit=Exit();
            if(exit==null) return false;
            var pose=exit[^1].Pose;Trace(exit,ref pose,4,0);
            if(exit.Any(p=>Collides(p.Pose))) return false;
            s.Spawn=s.SetupPose=s.Goal;s.ApproachEnd=0;
            s.Path.Add(new(s.Spawn,0,0));s.Path.AddRange(exit);
            s.Goal=pose;s.Edge=null;
            s.LateralTolerance=.6;s.DepthMin=-2;s.DepthMax=12;s.AngleTolerance=.1;
            return true;
        }
        // Start already in the space but off-centre and crooked. The witness pulls out
        // straight, then shifts back in with two opposite full-lock arcs. A parallel
        // correction starts at the front of the gap and shifts in reverse, rear first.
        bool Correct()
        {
            var goal=s.Goal;double scale=Math.Max(.6,1-attempt/48.0);
            double lateral=(rng.Next(2)*2-1)*(.22+rng.NextDouble()*.18)*scale;
            double turn=(rng.Next(2)*2-1)*(.03+rng.NextDouble()*.05)*scale;
            double depth=(rng.NextDouble()-.5)*.4*scale;
            int outward=E==Exercise.ForwardBay?-1:1;
            if(E==Exercise.Parallel) {lateral=-Math.Abs(lateral);depth=(s.Gap-CarSpec.Length)/2-.15;}
            else if(E==Exercise.Garage)
            {
                // Narrow garages leave little sideways room: the usual mistake there is stopping short.
                double room=Math.Max(0,(s.BayWidth-2*(CarSpec.MirrorX+CarSpec.MirrorWidth/2))/2-.07);
                lateral=Math.Sign(lateral)*Math.Min(Math.Abs(lateral),room);
                turn=Math.Sign(turn)*Math.Min(Math.Abs(turn),(room-Math.Abs(lateral))/2.5);
                if(Math.Abs(lateral)<.13) depth=outward*(.45+rng.NextDouble()*.2);
            }
            var centre=goal.Point(lateral,CarSpec.CentreZ-depth);
            double yaw=goal.Yaw+turn;
            var spawn=new Pose(centre.X-Math.Sin(yaw)*CarSpec.CentreZ,centre.Z-Math.Cos(yaw)*CarSpec.CentreZ,yaw);
            if(Collides(spawn)) return false;
            var path=new List<PathStep>();var p=spawn;
            int inward=-outward;
            if(E!=Exercise.Parallel) Trace(path,ref p,outward*((E==Exercise.Garage?5.0:1.2)+(attempt%12)*.25),0);
            var shift=Shift(p,goal,inward);
            if(shift is not {} plan) return false;
            if(E!=Exercise.Parallel && plan.Straight*inward<-1e-9) return false;
            path.AddRange(plan.Steps);p=plan.End;Trace(path,ref p,plan.Straight,0);
            if(path.Any(q=>Collides(q.Pose))) return false;
            s.Spawn=s.SetupPose=spawn;s.ApproachEnd=0;
            s.Path.Add(new(spawn,0,0));s.Path.AddRange(path);s.Goal=p;
            return true;
        }
    }
}
