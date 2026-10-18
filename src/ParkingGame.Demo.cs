using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    void FinishDemo()
    {
        Demo=false;Car.Speed=0;Car.Gear=Gear.P;Car.ParkingBrake=true;
        BeginInspection();Message("Demonstration complete. Inspect the parking position.");
    }
    // Consume time continuously, including steering and stopped gear changes.
    // A path point is a geometric boundary, never a pose to jump to on a timer.
    void RunDemo(double dt)
    {
        double time=Math.Max(0,dt);
        while(time>1e-12 && demoStep<Scene.Path.Count)
        {
            var step=Scene.Path[demoStep];double length=Math.Abs(step.Distance);
            if(length<1e-12) {Car.Pose=step.Pose;demoStep++;continue;}
            var neededGear=step.Distance>=0?Gear.D:Gear.R;
            if(Car.Gear!=neededGear)
            {
                Car.Speed=0;
                double used=Math.Min(time,1-demoShiftWait);demoShiftWait+=used;time-=used;
                if(demoShiftWait<1-1e-12) return;
                Car.Select(neededGear,1);demoShiftWait=0;
            }
            double turn=step.Steering-Car.Steer;
            if(Math.Abs(turn)>1e-12)
            {
                Car.Speed=0;
                double used=Math.Min(time,Math.Abs(turn)/.9);
                Car.Steer+=Math.Sign(turn)*used*.9;time-=used;
                if(Math.Abs(Car.Steer-step.Steering)>1e-12) return;
                Car.Steer=step.Steering;
            }
            if(time<=1e-12) return;
            double distance=Math.Min(time*.55,length-demoSegmentDistance);
            var next=Car.Pose.Advance(Math.Sign(step.Distance)*distance,step.Steering);
            if(DrivingModel.Collides(next,Scene.Obstacles))
            {Demo=false;Car.Speed=0;Message("Demonstration stopped for clearance.");return;}
            Car.Speed=Math.Sign(step.Distance)*.55;Car.Pose=next;Car.Travel+=distance;
            demoSegmentDistance+=distance;time-=distance/.55;
            if(demoSegmentDistance>=length-1e-12)
            {Car.Pose=step.Pose;demoSegmentDistance=0;demoStep++;}
        }
        if(demoStep>=Scene.Path.Count) FinishDemo();
    }
    async System.Threading.Tasks.Task<int> CheckDemoSmoothness()
    {
        int passed=0;
        void Check(bool ok,string text) {if(!ok) throw new InvalidOperationException(text);passed++;GD.Print("DEMO PASS: "+text);}
        try
        {
            StartDemo();Car.Steer=Scene.Path[1].Steering;
            bool continuous=true;
            for(int i=0;i<30;i++)
            {
                var before=Car.Pose;RunDemo(1.0/240);
                double distance=Math.Sqrt(Math.Pow(Car.Pose.X-before.X,2)+Math.Pow(Car.Pose.Z-before.Z,2));
                continuous&=Math.Abs(distance-.55/240)<1e-6;
            }
            Check(continuous,"Every 240 Hz playback sample moves a partial path segment without repeated poses");
            Pose? reference=null;bool independent=true;
            foreach(int fps in new[]{30,60,144,240})
            {
                StartDemo();double elapsed=0;
                while(elapsed<12-1e-10) {double dt=Math.Min(1.0/fps,12-elapsed);RunDemo(dt);elapsed+=dt;}
                if(reference is Pose expected) independent&=Math.Abs(Car.Pose.X-expected.X)<1e-8 && Math.Abs(Car.Pose.Z-expected.Z)<1e-8 && DrivingModel.AngleError(Car.Pose.Yaw,expected.Yaw)<1e-8;
                else reference=Car.Pose;
            }
            Check(independent,"Equal elapsed time gives the same path pose at 30, 60, 144 and 240 FPS");
            StartDemo();Car.Steer=Scene.Path[1].Steering;var start=Car.Pose;
            SetPhysicsProcess(false);
            for(int i=0;i<4;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(Car.Pose!=start && (vehicle.Position-new Vector3((float)Car.Pose.X,0,(float)Car.Pose.Z)).Length()<1e-5,"Demonstration and vehicle update on rendered frames with physics callbacks disabled");
            var rear=vehicle.GlobalTransform*new Transform3D(Basis.FromEuler(new(-.45f,Mathf.Pi,0)),new(.04f,1.10f,.72f));
            Check((rearCamera.GlobalPosition-rear.Origin).Length()<1e-5,"Rear camera stays synchronized with the rendered vehicle pose");
        }
        finally {SetPhysicsProcess(true);NewScenario(false);}
        return passed;
    }
}
