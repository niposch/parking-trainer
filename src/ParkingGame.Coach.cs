using Godot;
namespace ParkingTrainer;

// Guided shows every cue; Hints keeps the tutor but coaches only once stopped; Test removes all aids.
internal enum TrainingMode { Guided, Hints, Test }

public partial class ParkingGame
{
    internal TrainingMode Mode=TrainingMode.Guided;
    internal readonly List<(Pose Pose,bool Reverse)> Trail=new();
    internal double MaxSpeed,StationarySteer;
    internal int DirectionChanges;
    internal string CoachText="";
    internal List<PathStep>? FixPlan;
    int lastDirection;
    double stoppedTime,lastSteer,coachRefresh,tipTime;
    bool steerTipShown,fixRequested;
    Pose fixPlannedFrom;

    void ResetRun()
    {
        Trail.Clear();MaxSpeed=StationarySteer=0;DirectionChanges=lastDirection=0;stoppedTime=tipTime=coachRefresh=0;lastSteer=Car.Steer;
        steerTipShown=fixRequested=false;CoachText="";FixPlan=null;ShowFixPlan(null);
    }
    static double Distance(Pose a,Pose b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    // Rear-axle offset to the right of a reference pose, and distance ahead of it.
    static double Lateral(Pose p,Pose reference)=>(p.X-reference.X)*Math.Cos(reference.Yaw)-(p.Z-reference.Z)*Math.Sin(reference.Yaw);
    static double Ahead(Pose p,Pose reference)=>-(p.X-reference.X)*Math.Sin(reference.Yaw)-(p.Z-reference.Z)*Math.Cos(reference.Yaw);
    void RecordRun(double delta)
    {
        if(Trail.Count==0 || Distance(Trail[^1].Pose,Car.Pose)>.05) Trail.Add((Car.Pose,Car.Gear==Gear.R));
        if(Demo) return;
        MaxSpeed=Math.Max(MaxSpeed,Math.Abs(Car.Speed));
        int direction=Math.Abs(Car.Speed)>.05?Math.Sign(Car.Speed):0;
        if(direction!=0) {if(lastDirection!=0 && direction!=lastDirection) DirectionChanges++;lastDirection=direction;}
        bool stopped=Math.Abs(Car.Speed)<.01;
        if(stopped && Car.Gear!=Gear.P) StationarySteer+=Math.Abs(Car.Steer-lastSteer);
        lastSteer=Car.Steer;
        stoppedTime=stopped?stoppedTime+delta:0;
    }
    bool NearGoal(Assessment report)
    {
        if(Scene.Drill==Drill.Park && Scene.ClosestPath(Car.Pose)<Scene.ApproachEnd+(Scene.Path.Count-Scene.ApproachEnd)/2) return false;
        return Math.Abs(report.Depth)<4 && Math.Abs(report.Angle)<.6 && Math.Abs(report.Lateral)<1.5;
    }
    void ToggleFixPlan()
    {
        if(Mode==TrainingMode.Test) {Message("Correction plans are off in test mode.");return;}
        fixRequested=!fixRequested;
        if(!fixRequested) {FixPlan=null;ShowFixPlan(null);}
        coachRefresh=0;
    }
    void UpdateCoach(double delta)
    {
        tipTime=Math.Max(0,tipTime-delta);
        coachRefresh-=delta;if(coachRefresh>0) return;coachRefresh=.25;
        bool active=!Demo&&!Inspection&&!Paused&&Mode!=TrainingMode.Test;
        var report=Scene.Assess(Car.Pose);
        bool near=active&&!Scene.FreeGoal&&NearGoal(report);
        if(FixPlan!=null && (!near || report.Grade==Grade.Good || PlanDeviation(FixPlan)>.5))
        {
            FixPlan=null;ShowFixPlan(null);if(report.Grade==Grade.Good) fixRequested=false;
        }
        bool wanted=near && report.Grade!=Grade.Good && (Mode==TrainingMode.Guided || fixRequested);
        if(wanted && stoppedTime>.6 && (FixPlan==null || Distance(fixPlannedFrom,Car.Pose)>.03 || Math.Abs(fixPlannedFrom.Yaw-Car.Pose.Yaw)>.01))
        {
            FixPlan=PlanFix(Car.Pose);fixPlannedFrom=Car.Pose;ShowFixPlan(FixPlan);
        }
        CoachText=active?Coach(report,near):"";
    }
    double PlanDeviation(List<PathStep> plan)
    {
        double best=Distance(fixPlannedFrom,Car.Pose);
        foreach(var step in plan) best=Math.Min(best,Distance(step.Pose,Car.Pose));
        return best;
    }
    string Coach(Assessment report,bool near)
    {
        if(Mode==TrainingMode.Hints && stoppedTime<1.2 && !fixRequested) return "";
        string tip=Tr("Tip: turn the wheel while the car creeps. Steering at a standstill is harder and wears the tyres.");
        if(tipTime>0) return tip;
        if(!steerTipShown && StationarySteer>2.5) {steerTipShown=true;tipTime=6;return tip;}
        if(Math.Abs(Car.Speed)>1.4 && Scene.PositionError(Car.Pose)<9) return Tr("Slow down. Creep speed gives you time to look around and correct.");
        if(FixPlan!=null) return FixPlanText(FixPlan);
        if(near && Mode==TrainingMode.Guided && report.Grade!=Grade.Good && stoppedTime>.6)
            return PositionText(report)+"\n"+Tr("No simple correction fits from here. Pull out further, then re-park.");
        if(near) return PositionText(report);
        int index=Scene.ClosestPath(Car.Pose);
        if(Scene.Drill==Drill.Park && index<Scene.ApproachEnd-8) return stoppedTime>.6?SetupText():"";
        if(Scene.FreeGoal && report.Grade!=Grade.Repark && Math.Abs(Car.Speed)<.02) return Tr("You are in the target lane. Hold the brake, select P and apply the parking brake to finish.");
        // On the manoeuvre: compare with the demonstrated line at the nearest point.
        var reference=Scene.Path[index];
        double off=Lateral(Car.Pose,reference.Pose);
        if(Math.Abs(off)<.35) return "";
        string text=string.Format(Tr(off>0?"You are {0:0.0} m right of the demonstrated line.":"You are {0:0.0} m left of the demonstrated line."),Math.Abs(off));
        if(Math.Abs(reference.Steering-Car.Steer)>.2) text+=" "+string.Format(Tr("The demonstration has the wheel {0} here."),SteerWord(reference.Steering));
        return text;
    }
    string SteerWord(double steer)=>Math.Abs(steer)<.05?Tr("straight"):Math.Abs(steer)>.5?Tr(steer>0?"at full lock left":"at full lock right"):Tr(steer>0?"turned left":"turned right");
    string SetupText()
    {
        var parts=new List<string>();
        if(Exercise==Exercise.Parallel)
        {
            double side=Scene.Mirrored?Math.Min(SideSectors[0],SideSectors[2]):Math.Min(SideSectors[1],SideSectors[3]);
            if(side<.45) parts.Add(string.Format(Tr("Only {0:0.00} m from the parked cars. Leave about 0.5–1 m."),side));
            else if(side>1.3 && side<2) parts.Add(string.Format(Tr("{0:0.0} m from the parked cars. About 0.5–1 m makes the reverse easier."),side));
        }
        if(Distance(Car.Pose,Scene.SetupPose)<7)
        {
            double along=Ahead(Car.Pose,Scene.SetupPose),side=Lateral(Car.Pose,Scene.SetupPose);
            if(Math.Abs(along)<.4 && Math.Abs(side)<.4) parts.Add(Tr("Good stopping point: close to where the demonstration stops."));
            else parts.Add(string.Format(Tr("Compared with the demonstration's stopping point: {0}, {1}."),
                string.Format(Tr(along>0?"{0:0.0} m further along":"{0:0.0} m short"),Math.Abs(along)),
                string.Format(Tr(side>0?"{0:0.0} m further right":"{0:0.0} m further left"),Math.Abs(side))));
        }
        return string.Join("\n",parts);
    }
    internal string PositionText(Assessment report)
    {
        string centre=Math.Abs(report.Lateral)<.04?Tr("centred"):string.Format(Tr(report.Lateral>0?"{0:0} cm right of centre":"{0:0} cm left of centre"),Math.Abs(report.Lateral)*100);
        string depth=Math.Abs(report.Depth)<.08?Tr("depth good"):string.Format(Tr(report.Depth>0?"{0:0} cm too far forward":"{0:0} cm too far back"),Math.Abs(report.Depth)*100);
        double degrees=report.Angle*180/Math.PI;
        string angle=Math.Abs(degrees)<.5?Tr("straight"):string.Format(Tr(degrees>0?"nose {0:0.0}° to the left":"nose {0:0.0}° to the right"),Math.Abs(degrees));
        string text=string.Format(Tr("Position: {0} · {1} · {2}"),centre,depth,angle);
        if(report.KerbGap is {} kerb && Exercise is Exercise.Parallel or Exercise.ReverseCorner) text+="\n"+string.Format(Tr("Kerb gap {0:0.00} m"),kerb);
        if(report.Grade==Grade.Good) return text+"\n"+Tr("Good position. Hold the brake, select P and apply the parking brake.");
        if(Mode==TrainingMode.Hints && !fixRequested) text+="\n"+Tr("Stop and press F for a correction plan.");
        else if(Math.Abs(Car.Speed)>.01) text+="\n"+Tr("Stop to get a correction plan.");
        return text;
    }
    // Pull straight out (or not at all), shift back with two opposite arcs, then straighten
    // onto the target. The same construction as the correction drill, from the current pose.
    List<PathStep>? PlanFix(Pose from)
    {
        List<PathStep>? best=null;double bestLength=double.MaxValue;
        foreach(int inward in new[]{1,-1})
        foreach(double pull in new[]{0,.5,1,1.5,2,2.5,3,3.5,4,5,6})
        {
            var steps=new List<PathStep>();var p=from;
            Scenario.Trace(steps,ref p,-inward*pull,0);
            if(Scenario.Shift(p,Scene.Goal,inward) is not {} shift || shift.Straight*inward<-.05) continue;
            steps.AddRange(shift.Steps);p=shift.End;Scenario.Trace(steps,ref p,shift.Straight,0);
            double length=steps.Sum(q=>Math.Abs(q.Distance));
            if(length>=bestLength || steps.Any(q=>DrivingModel.Collides(q.Pose,Scene.Obstacles,.03))) continue;
            best=steps;bestLength=length;
        }
        return best;
    }
    internal static List<(int Direction,double Steering,double Length)> Segments(List<PathStep> plan)
    {
        var segments=new List<(int Direction,double Steering,double Length)>();
        foreach(var step in plan)
        {
            int direction=Math.Sign(step.Distance);
            if(segments.Count>0 && segments[^1].Direction==direction && segments[^1].Steering==step.Steering)
                segments[^1]=(direction,step.Steering,segments[^1].Length+Math.Abs(step.Distance));
            else segments.Add((direction,step.Steering,Math.Abs(step.Distance)));
        }
        return segments.Where(s=>s.Length>.02).ToList();
    }
    string FixPlanText(List<PathStep> plan)
    {
        var lines=new List<string> {Tr("CORRECTION PLAN · follow the cyan line")};
        int number=1;
        foreach(var s in Segments(plan))
            lines.Add(string.Format(Tr("{0}. {1}, wheel {2}: {3} {4:0.0} m"),number++,s.Direction>0?"D":"R",SteerWord(s.Steering),Tr(s.Direction>0?"forward":"back"),s.Length));
        lines.Add(Tr("Stop at each step and turn the wheel while creeping."));
        return string.Join("\n",lines);
    }
    void ShowFixPlan(List<PathStep>? plan)
    {
        if(!IsInstanceValid(fixMesh)) return;
        if(plan==null || plan.Count<2) {fixMesh.Mesh=null;return;}
        var mesh=new ImmediateMesh();
        var mat=WorldFactory.Material(new("5fd6f0"),true);mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles,mat);
        // Trace the body centre, which is what the driver sees moving over the ground.
        Vector3 Centre(Pose p) {var c=p.Point(0,CarSpec.CentreZ);return new((float)c.X,.035f,(float)c.Z);}
        var points=new List<Vector3> {Centre(fixPlannedFrom)};
        foreach(var step in plan) if(Centre(step.Pose).DistanceTo(points[^1])>.08) points.Add(Centre(step.Pose));
        for(int i=1;i<points.Count;i++)
        {
            var a=points[i-1];var b=points[i];var side=(b-a).Cross(Vector3.Up).Normalized()*.05f;
            foreach(var v in new[]{a-side,a+side,b+side,a-side,b+side,b-side}) mesh.SurfaceAddVertex(v);
        }
        mesh.SurfaceEnd();fixMesh.Mesh=mesh;
    }
}
