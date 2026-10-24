using Godot;
namespace ParkingTrainer;

// Top-down replay of an attempt, drawn in the target's frame: the target's forward
// points up, so left and right match the written report. The view follows the
// replay car and closes in on the target as the car arrives.
public partial class ReportDiagram : Control
{
    public Scenario? Scene;
    public List<(Pose Pose,bool Reverse)> Trail=new();
    public Pose Final;
    public Grade Grade;
    public bool ShowGoal=true;
    double time,total,hold;
    double[] times=[];
    readonly List<double> gearChanges=new();
    Vector2 camera;float span;bool snap=true;
    const double ReplaySpeed=2.5,GearPause=.6,EndHold=2.5;
    public override void _Ready() {MouseFilter=MouseFilterEnum.Ignore;ClipContents=true;Resized+=QueueRedraw;}
    public double Progress=>total>0?Math.Min(1,time/total):1;
    public void Restart()
    {
        // Replay at a steady speed, pausing briefly at each change of direction.
        times=new double[Trail.Count];gearChanges.Clear();double t=0;
        for(int i=1;i<Trail.Count;i++)
        {
            var a=Trail[i-1].Pose;var b=Trail[i].Pose;
            t+=Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z))/ReplaySpeed;
            if(Trail[i].Reverse!=Trail[i-1].Reverse) {gearChanges.Add(t);t+=GearPause;}
            times[i]=t;
        }
        total=t+EndHold;time=0;hold=0;snap=true;QueueRedraw();
    }
    // Arrow keys scrub; autoplay resumes after a few seconds.
    public void Scrub(double seconds) {time=Math.Clamp(time+seconds,0,Math.Max(0,total-.01));hold=4;QueueRedraw();}
    public override void _Process(double delta)
    {
        if(!IsVisibleInTree()) return;
        if(hold>0) hold-=delta;
        else {time+=delta;if(time>=total) {time=0;snap=true;}}
        QueueRedraw();
    }
    Pose ReplayPose()
    {
        if(Trail.Count==0) return Final;
        if(time>=times[^1]) return Trail[^1].Pose;
        int hi=Array.BinarySearch(times,time);if(hi<0) hi=~hi;hi=Math.Clamp(hi,1,Trail.Count-1);
        double span=times[hi]-times[hi-1],f=span>1e-9?Math.Clamp((time-times[hi-1])/span,0,1):1;
        var a=Trail[hi-1].Pose;var b=Trail[hi].Pose;
        return new(a.X+(b.X-a.X)*f,a.Z+(b.Z-a.Z)*f,a.Yaw+Math.Atan2(Math.Sin(b.Yaw-a.Yaw),Math.Cos(b.Yaw-a.Yaw))*f);
    }
    public override void _Draw()
    {
        if(Scene==null) return;
        DrawStyleBox(new StyleBoxFlat {BgColor=new("5d666a"),CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8},new(Vector2.Zero,Size));
        var goal=Scene.Goal;var origin=goal.Point(0,CarSpec.CentreZ);
        double cy=Math.Cos(goal.Yaw),sy=Math.Sin(goal.Yaw);
        Vector2 Local(double x,double z)=>new((float)((x-origin.X)*cy-(z-origin.Z)*sy),(float)(-(x-origin.X)*sy-(z-origin.Z)*cy));
        var replay=ReplayPose();bool finished=Trail.Count==0||time>=times[^1];
        var replayCentre=replay.Point(0,CarSpec.CentreZ);var ghost=Local(replayCentre.X,replayCentre.Z);
        // Close framing of the target and final car, used as the replay car arrives.
        var close=DrivingModel.Body(goal).Corners().Concat(DrivingModel.Body(Final).Corners()).Select(c=>Local(c.X,c.Z)).ToArray();
        var lo=new Vector2(close.Min(p=>p.X)-2.5f,close.Min(p=>p.Y)-1.5f);var hi=new Vector2(close.Max(p=>p.X)+2.5f,close.Max(p=>p.Y)+1.5f);
        float closeSpan=Math.Max((hi.X-lo.X)/Size.X,(hi.Y-lo.Y)/Size.Y);
        float arrive=finished?1:Mathf.SmoothStep(0,1,1-Mathf.Clamp((ghost.Length()-2)/6,0,1));
        var targetCentre=ghost.Lerp((lo+hi)/2,arrive);
        float targetSpan=Mathf.Lerp(16f/Math.Min(Size.X,Size.Y),closeSpan,arrive);
        float ease=snap?1:1-Mathf.Exp(-(float)GetProcessDeltaTime()*5);snap=false;
        camera=camera.Lerp(targetCentre,ease);span=Mathf.Lerp(span,targetSpan,ease);
        float scale=1/span;
        Vector2 Screen(double x,double z) {var l=Local(x,z);return Size/2+new Vector2(l.X-camera.X,-(l.Y-camera.Y))*scale;}
        Vector2[] Quad((double X,double Z)[] corners)=>corners.Select(c=>Screen(c.X,c.Z)).ToArray();
        void Outline(Vector2[] q,Color color,float width)=>DrawPolyline(q.Append(q[0]).ToArray(),color,width,true);
        foreach(var o in Scene.Obstacles)
        {
            var color=o.Model switch {"curb"=>new Color("8f8e84"),"wall"=>new Color("c9c2b2"),_=>o.Sensed?new Color("a9b8b1"):new Color("f2c230")};
            DrawColoredPolygon(Quad(o.Shape.Corners()),color);
        }
        foreach(var line in Scene.Lines) DrawLine(Screen(line.X1,line.Z1),Screen(line.X2,line.Z2),new Color("eeeee3"),1.5f,true);
        if(ShowGoal)
        {
            Outline(Quad(DrivingModel.Body(goal).Corners()),new Color("c7f29f"),2);
            var demo=Scene.Path.Where((_,i)=>i%6==0).Select(p=>{var c=p.Pose.Point(0,CarSpec.CentreZ);return Screen(c.X,c.Z);}).ToArray();
            for(int i=1;i<demo.Length;i+=2) DrawLine(demo[i-1],demo[i],new Color(.78f,.95f,.62f,.75f),1.5f,true);
        }
        // Driven so far in full colour; the rest of the attempt stays faint.
        for(int i=1;i<Trail.Count;i++)
        {
            var a=Trail[i-1].Pose.Point(0,CarSpec.CentreZ);var b=Trail[i].Pose.Point(0,CarSpec.CentreZ);
            var color=Trail[i].Reverse?new Color("f0a35e"):new Color("7cc4ff");
            if(times[i]>time) color.A=.25f;
            DrawLine(Screen(a.X,a.Z),Screen(b.X,b.Z),color,2.5f,true);
        }
        if(finished)
        {
            var final=Quad(DrivingModel.Body(Final).Corners());
            var fill=Grade switch {Grade.Good=>new Color(.55f,.85f,.45f,.85f),Grade.Acceptable=>new Color(.95f,.8f,.35f,.85f),_=>new Color(.95f,.45f,.35f,.85f)};
            DrawColoredPolygon(final,fill);Outline(final,new Color("14262b"),1.5f);
        }
        else
        {
            int index=Array.BinarySearch(times,time);if(index<0) index=~index;
            bool reverse=Trail[Math.Clamp(index,0,Trail.Count-1)].Reverse;
            var car=Quad(DrivingModel.Body(replay).Corners());
            DrawColoredPolygon(car,reverse?new Color(.94f,.64f,.37f,.6f):new Color(.49f,.77f,1f,.6f));Outline(car,new Color("14262b"),1.5f);
            // Mark the front so direction is readable at a glance.
            DrawLine((car[0]+car[2])/2,(car[0]+car[1])/2,new Color("14262b"),2,true);
        }
        // Progress bar with a tick at each change of direction.
        float barY=Size.Y-26,barW=Size.X-16;
        DrawRect(new(8,barY,barW,3),new Color(1,1,1,.25f));
        DrawRect(new(8,barY,barW*(float)Progress,3),new Color("cff59f"));
        foreach(var change in gearChanges) DrawRect(new(8+barW*(float)(change/total)-1,barY-3,2,9),new Color("f0a35e"));
        var font=ThemeDB.FallbackFont;
        DrawString(font,new(8,Size.Y-8),Tr("blue: forward · orange: reverse · dashed: demonstration"),HorizontalAlignment.Left,-1,11,new Color("f0f0e5"));
    }
}
