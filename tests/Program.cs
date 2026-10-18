using ParkingTrainer;
int checks=0;
void Check(bool value,string name) { if(!value) throw new Exception(name); checks++; }
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
foreach(Exercise e in Enum.GetValues<Exercise>())
foreach(Difficulty difficulty in Enum.GetValues<Difficulty>())
for(int seed=0;seed<100;seed++)
{
    var s=Scenario.Create(e,seed,difficulty); var repeat=Scenario.Create(e,seed,difficulty);
    Check(s.Spawn==repeat.Spawn && s.Obstacles.SequenceEqual(repeat.Obstacles),"Deterministic scenario");
    Check(!DrivingModel.Collides(s.Spawn,s.Obstacles),"Clear spawn");
    var range=Scenario.SpaceRange(e,difficulty);double space=e==Exercise.Parallel?s.Gap:s.BayWidth;
    Check(space>=range.Min && space<=range.Max,"Difficulty retains its advertised space size");
    Check(s.ApproachEnd>200 && s.Path[s.ApproachEnd].Pose==s.SetupPose,"Approach is separate from the parking setup");
    Check(s.Path.Skip(1).Take(s.ApproachEnd).All(q=>q.Distance>0),"Approach requires driving forward before the parking maneuver");
    var lateral=(s.Spawn.X-s.SetupPose.X)*Math.Cos(s.SetupPose.Yaw)-(s.Spawn.Z-s.SetupPose.Z)*Math.Sin(s.SetupPose.Yaw);
    Check(Math.Abs(lateral)>.10,"Approach starts laterally offset from the parking reference setup");
    var p=s.Spawn;
    foreach(var q in s.Path.Skip(1))
    {
        p=p.Advance(q.Distance,q.Steering);
        Check(!DrivingModel.Collides(p,s.Obstacles),"Full body and mirror swept witness stays clear");
    }
    Check(s.PositionError(p)<1e-6 && DrivingModel.AngleError(p.Yaw,s.Goal.Yaw)<1e-6,"Feasible witness reaches goal");
}
var obstacle=new Obstacle(new(0,-5,2,2,0),"test");
var car=new DrivingModel{Gear=Gear.D,ParkingBrake=false,Pose=new(0,0,0),Speed=3};
Check(car.Sensor(new[]{obstacle},true,0)<.6,"Sensor measures from front bumper");
var sectorCar=new DrivingModel();
var frontLeft=new[]{new Obstacle(new(-.95,-4.45,.3,.3,0),"front-left")};
var rearLeft=new[]{new Obstacle(new(-.95,1.80,.3,.3,0),"rear-left")};
Check(sectorCar.Sensor(frontLeft,true,-1)<1.3,"Front left sensor fans outward toward front-left obstacle");
Check(sectorCar.Sensor(frontLeft,true,1)==2,"Front right sensor does not report an obstacle on the opposite side");
Check(sectorCar.Sensor(rearLeft,false,-1)<1.3,"Rear left sensor fans outward toward rear-left obstacle");
Check(sectorCar.Sensor(rearLeft,false,1)==2,"Rear right sensor does not report an obstacle on the opposite side");
var curb=new Obstacle(new(0,1.10,1.8,.20,0),"curb",.15,true);
Check(sectorCar.Sensor(new[]{curb},false,0)==2,"Low sidewalk does not appear on parking sensors");
Check(sectorCar.Sensor(new[]{curb,obstacle},true,0)==sectorCar.Sensor(new[]{obstacle},true,0),"Ignoring sidewalks preserves normal car detection");
Check(DrivingModel.Collides(new Pose(0,.15,0),new[]{curb}),"Sidewalk remains a physical collision obstacle");
car.Move(1,new[]{obstacle});
Check(car.Contacts==1 && !DrivingModel.Collides(car.Pose,new[]{obstacle}),"Swept collision stops without tunnelling");
Check(new Footprint(0,0,2,4,.4).Overlaps(new(0,0,2,4,-.3)),"Rotated SAT overlap");
Check(!new Footprint(0,0,2,4,.4).Overlaps(new(20,0,2,4,-.3)),"Separated SAT boxes");
Console.WriteLine($"PASS: {checks:N0} checks, including 900 procedural feasibility traces.");
