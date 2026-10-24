using ParkingTrainer;
int checks=0;
if(args.Contains("--audio")) {AlarmChecks.Run((ok,name)=>{if(!ok) throw new Exception(name);checks++;});Console.WriteLine($"PASS: {checks} audio transition checks.");return;}
void Check(bool value,string name) { if(!value) throw new Exception(name); checks++; }
if(args.Contains("--proximity")) {ProximityChecks.Run(Check);Console.WriteLine($"PASS: {checks:N0} proximity checks.");return;}
foreach(var gear in new[]{Gear.D,Gear.R})
{
    var m=new DrivingModel{Gear=gear,ParkingBrake=false};
    for(int i=0;i<1200;i++) m.SetSpeed(1.0/120,0,0);
    Check(Math.Abs(m.Speed-CarSpec.Creep*(gear==Gear.D?1:-1))<.002,"Creep settles in selected direction");
    for(int i=0;i<3600;i++) m.SetSpeed(1.0/120,0,1);
    Check(m.Speed==0,"Brake stops and holds for 30 seconds without reversing");
    Check(!m.Select(Gear.R,0),"Gear interlock requires brake");
    Check(m.Select(Gear.N,1),"Stopped and braked shift permitted");
    for(int i=0;i<240;i++) m.SetSpeed(1.0/120,1,0);
    Check(m.Speed==0,"Neutral cannot accelerate");
}
var start=new Pose(2,-3,.3);
foreach(double steer in new[]{-.6,-.15,0,.3,.6})
{
    var end=start.Advance(5,steer).Advance(-5,steer);
    Check(Math.Abs(end.X-start.X)+Math.Abs(end.Z-start.Z)+Math.Abs(end.Yaw-start.Yaw)<1e-9,"Reversible exact integration");
    var numerical=start;
    double step=.0001;
    for(int i=0;i<50000;i++) numerical=new(numerical.X-step*Math.Sin(numerical.Yaw),numerical.Z-step*Math.Cos(numerical.Yaw),numerical.Yaw+step*Math.Tan(steer)/CarSpec.Wheelbase);
    var exact=start.Advance(5,steer);
    Check(Math.Sqrt(Math.Pow(numerical.X-exact.X,2)+Math.Pow(numerical.Z-exact.Z,2))<.001,"Independent Euler integration agrees to 1 mm over 5 m");
}
int traces=0;var features=new Dictionary<string,int>();
void Verify(ScenarioSettings settings,int seed)
{
    var s=Scenario.Create(settings,seed);var repeat=Scenario.Create(settings,seed);var e=settings.Exercise;string name=$"{settings} seed {seed}";
    Check(s.Spawn==repeat.Spawn && s.Obstacles.SequenceEqual(repeat.Obstacles) && s.Lines.SequenceEqual(repeat.Lines),"Deterministic scenario: "+name);
    Check(!DrivingModel.Collides(s.Spawn,s.Obstacles),"Clear spawn: "+name);
    var range=Scenario.SpaceRange(e,settings.Difficulty);
    Check(s.Space>=range.Min && s.Space<=range.Max,"Difficulty retains its advertised space size: "+name);
    if(s.Drill==Drill.Park)
    {
        Check(s.ApproachEnd>200 && s.Path[s.ApproachEnd].Pose==s.SetupPose,"Approach is separate from the parking setup: "+name);
        Check(s.Path.Skip(1).Take(s.ApproachEnd).All(q=>q.Distance>0),"Approach requires driving forward before the maneuver: "+name);
        var lateral=(s.Spawn.X-s.SetupPose.X)*Math.Cos(s.SetupPose.Yaw)-(s.Spawn.Z-s.SetupPose.Z)*Math.Sin(s.SetupPose.Yaw);
        Check(Math.Abs(lateral)>.10,"Approach starts laterally offset from the parking reference setup: "+name);
        if(e!=Exercise.TurnInRoad)
        {
            var target=DrivingModel.Body(s.Goal);double dx=target.X-s.Spawn.X,dz=target.Z-s.Spawn.Z;
            double side=dx*Math.Cos(s.Spawn.Yaw)-dz*Math.Sin(s.Spawn.Yaw);
            bool right=(e is Exercise.Parallel or Exercise.ReverseCorner)!=settings.Mirrored;
            Check(right?side>0:side<0,"Space is on the selected side of the approach: "+name);
        }
    }
    else Check(s.ApproachEnd==0 && s.Path[0].Pose==s.Spawn,"Drills start in the space: "+name);
    if(s.Drill==Drill.Correct) Check(s.Assess(s.Spawn).Grade!=Grade.Good,"Correction drill starts visibly off-centre: "+name);
    if(s.Drill==Drill.DriveOut) Check(s.Assess(s.Spawn).Grade==Grade.Repark && s.FreeGoal,"Drive-out target is a lane zone away from the space: "+name);
    var p=s.Spawn;
    foreach(var q in s.Path.Skip(1))
    {
        p=p.Advance(q.Distance,q.Steering);
        Check(!DrivingModel.Collides(p,s.Obstacles),"Full body and mirror swept witness stays clear: "+name);
    }
    Check(s.PositionError(p)<1e-6 && DrivingModel.AngleError(p.Yaw,s.Goal.Yaw)<1e-6,"Feasible witness reaches goal: "+name);
    Check(s.Assess(s.Goal).Grade==Grade.Good,"Target pose grades as good: "+name);
    foreach(var f in s.Features) features[f]=features.GetValueOrDefault(f)+1;
    traces++;
}
foreach(Exercise e in Enum.GetValues<Exercise>())
foreach(Difficulty difficulty in Enum.GetValues<Difficulty>())
foreach(bool mirrored in new[]{false,true})
for(int seed=0;seed<100;seed++)
{
    Verify(new(e,difficulty,mirrored),seed);
    if(mirrored)
    {
        var s=Scenario.Create(e,seed,difficulty,true);var original=Scenario.Create(e,seed,difficulty);
        Check(s.Spawn==new Pose(-original.Spawn.X,original.Spawn.Z,-original.Spawn.Yaw) && s.ApproachEnd==original.ApproachEnd,"Mirror preserves seed layout and approach length");
        Check(s.Path.Count==original.Path.Count && s.Obstacles.Zip(original.Obstacles).All(pair=>pair.First==pair.Second with {Shape=pair.Second.Shape with {X=-pair.Second.Shape.X,Yaw=-pair.Second.Shape.Yaw}}),"All obstacles and curbs reflect together");
        Check(s.Path.Zip(original.Path).All(pair=>pair.First.Pose==new Pose(-pair.Second.Pose.X,pair.Second.Pose.Z,-pair.Second.Pose.Yaw) && pair.First.Steering==-pair.Second.Steering && pair.First.Distance==pair.Second.Distance),"Complete mirrored path reverses steering without changing pedal direction");
    }
}
foreach(Exercise e in Enum.GetValues<Exercise>())
foreach(Drill drill in Enum.GetValues<Drill>())
foreach(Clutter clutter in Enum.GetValues<Clutter>())
foreach(bool row in Scenario.HasRow(e)?new[]{false,true}:new[]{false})
foreach(Difficulty difficulty in Enum.GetValues<Difficulty>())
{
    if(!Scenario.Supports(e,drill) || drill==Drill.Park && clutter==Clutter.None && !row) continue;
    for(int seed=0;seed<24;seed++) Verify(new(e,difficulty,seed%2==1,drill,clutter,row),seed);
}
Check(Scenario.Create(new ScenarioSettings(Exercise.TurnInRoad,Drill:Drill.Correct),3).Drill==Drill.Park,"Unsupported drills fall back to parking");
var graded=Scenario.Create(Exercise.ReverseBay,4);
var right=graded.Goal with {X=graded.Goal.X+Math.Cos(graded.Goal.Yaw)*.2,Z=graded.Goal.Z-Math.Sin(graded.Goal.Yaw)*.2};
var report=graded.Assess(right);
Check(Math.Abs(report.Lateral-.2)<1e-9 && report.Grade==Grade.Acceptable && report.RightGap<report.LeftGap,"Report measures a car 20 cm right of centre and grades it acceptable");
Check(graded.Assess(right with {Yaw=right.Yaw+.2}).Grade==Grade.Repark,"A crooked car must be re-parked");
var onLine=graded.Goal with {X=graded.Goal.X+Math.Cos(graded.Goal.Yaw)*(graded.BayWidth/2-CarSpec.Width/2-.01),Z=graded.Goal.Z-Math.Sin(graded.Goal.Yaw)*(graded.BayWidth/2-CarSpec.Width/2-.01)};
Check(graded.Assess(onLine).Grade==Grade.Repark,"A body 1 cm from the bay line must be re-parked");
var shifted=Scenario.Shift(new Pose(.3,2,.05),new Pose(0,0,0),-1);
Check(shifted is {} sh && Math.Abs(sh.End.X-sh.Straight*Math.Sin(0)-0)<1e-9 && Math.Abs(sh.End.Yaw)<1e-9,"Two-arc shift lands on the target line and heading");
foreach(var pair in features.OrderBy(f=>f.Key)) Console.WriteLine($"  feature {pair.Value,5}x  {pair.Key}");
var obstacle=new Obstacle(new(0,-5,2,2,0),"test");
var car=new DrivingModel{Gear=Gear.D,ParkingBrake=false,Pose=new(0,0,0),Speed=3};
Check(car.Sensor(new[]{obstacle},true,0)<.6,"Sensor measures from front bumper");
var sectorCar=new DrivingModel();
var frontLeft=new[]{new Obstacle(new(-.95,-4.45,.3,.3,0),"front-left")};
var rearLeft=new[]{new Obstacle(new(-.95,1.80,.3,.3,0),"rear-left")};
Check(sectorCar.Sensor(frontLeft,true,-1)<1.3,"Front left protection covers front-left obstacle");
Check(sectorCar.Sensor(frontLeft,true,1)==2,"Front right sensor does not report an obstacle on the opposite side");
Check(sectorCar.Sensor(rearLeft,false,-1)<1.3,"Rear left protection covers rear-left obstacle");
Check(sectorCar.Sensor(rearLeft,false,1)==2,"Rear right sensor does not report an obstacle on the opposite side");
var curb=new Obstacle(new(0,1.10,1.8,.20,0),"curb",.15,true);
Check(sectorCar.Sensor(new[]{curb},false,0)==2,"Low sidewalk does not appear on parking sensors");
Check(sectorCar.Sensor(new[]{curb,obstacle},true,0)==sectorCar.Sensor(new[]{obstacle},true,0),"Ignoring sidewalks preserves normal car detection");
Check(DrivingModel.Collides(new Pose(0,.15,0),new[]{curb}),"Sidewalk remains a physical collision obstacle");
car.Move(1,new[]{obstacle});
Check(car.Contacts==1 && !DrivingModel.Collides(car.Pose,new[]{obstacle}),"Swept collision stops without tunnelling");
for(int i=0;i<240;i++) {car.Speed=.5;car.Move(1.0/120,new[]{obstacle});}
Check(car.Contacts==1,"Pressing against the same obstacle counts as one contact");
car.Speed=-1;for(int i=0;i<60;i++) car.Move(1.0/120,new[]{obstacle});car.Speed=3;car.Move(1,new[]{obstacle});
Check(car.Contacts==2,"Backing away and hitting again counts a second contact");
var kerbCar=new DrivingModel{Pose=new(0,0,0),Gear=Gear.R,ParkingBrake=false,Speed=-.9};
var kerbLine=new[]{new Obstacle(new(0,CarSpec.Rear+.3,8,.2,0),"curb",.15,true)};
kerbCar.Move(1,kerbLine);
Check(kerbCar.KerbTouches==1 && kerbCar.Contacts==0,"Rolling onto a kerb at creeping speed is a gentle kerb touch");
kerbCar=new DrivingModel{Pose=new(0,0,0),Gear=Gear.R,ParkingBrake=false,Speed=-2.5};kerbCar.Move(1,kerbLine);
Check(kerbCar.Contacts==1 && kerbCar.KerbTouches==0,"Hitting a kerb fast is a contact");
Check(new Footprint(0,0,2,4,.4).Overlaps(new(0,0,2,4,-.3)),"Rotated SAT overlap");
Check(!new Footprint(0,0,2,4,.4).Overlaps(new(20,0,2,4,-.3)),"Separated SAT boxes");
ProximityChecks.Run(Check);
Console.WriteLine($"PASS: {checks:N0} checks, including {traces:N0} procedural feasibility traces.");
